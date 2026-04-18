using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ObjectData;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime
{
    /// <summary>
    /// 骇客时间物块赛博滤镜 RT 渲染
    /// <br/>由于物块绘制无 PreDraw/PostDraw 钩子无法像 NPC 那样直接套着色器
    /// <br/>改用取巧办法：把选中/悬停物块按原始帧重绘到一张小 RT，以 RT 自然的透明像素作为轮廓掩码
    /// <br/>再对 RT 套用 HackTimeNPCHighlight 着色器，加法合成回屏幕
    /// <br/>支持多物块（如 3x2 的桌子选中一格会整体高亮）
    /// </summary>
    internal static class HackTimeTileCyberPass
    {
        //缓存 RT，随尺寸变化重建
        private static RenderTarget2D _rt;
        //RT 的最大边长上限，防止异常包围盒申请过大纹理（树木可能很高故放宽到 512）
        private const int MaxRtSize = 512;
        //包围盒外扩像素，给描边辉光留空间
        private const int EdgePadding = 6;

        /// <summary>
        /// 在 EndEntityDraw 内调用，入口处 SpriteBatch 未处于 Begin 状态
        /// </summary>
        public static void Draw(SpriteBatch sb, GraphicsDevice gd) {
            float effectStr = HackTime.Intensity;
            if (effectStr < 0.02f) return;

            Effect shader = HackTimeAssets.HackTimeNPCHighlight;
            if (shader == null) return;

            //优先绘制悬停（冷青），再绘制选中（红色），选中覆盖悬停
            DrawPassForHovered(sb, gd, shader, effectStr);
            DrawPassForSelected(sb, gd, shader, effectStr);
        }

        private static void DrawPassForHovered(SpriteBatch sb, GraphicsDevice gd, Effect shader, float effectStr) {
            int hx = HackTimeTargeting.HoveredTileX;
            int hy = HackTimeTargeting.HoveredTileY;
            if (hx < 0 || hy < 0) return;
            if (hx >= Main.maxTilesX || hy >= Main.maxTilesY) return;

            Tile hoverTile = Main.tile[hx, hy];
            if (!hoverTile.HasTile) return;

            Rectangle bounds = TileScannable.GetTileWorldBounds(hx, hy);

            //悬停物块若与选中物块为同一物体则跳过，避免重复描边
            if (HackTime.CurrentScanTarget is TileScannable sel) {
                Vector2 c = sel.WorldCenter;
                if (c.X >= bounds.X && c.X <= bounds.Right
                    && c.Y >= bounds.Y && c.Y <= bounds.Bottom) return;
            }

            RenderAndComposite(sb, gd, shader, bounds, effectStr, isSelected: false);
        }

        private static void DrawPassForSelected(SpriteBatch sb, GraphicsDevice gd, Effect shader, float effectStr) {
            if (HackTime.CurrentScanTarget is not TileScannable tileScan) return;
            if (!tileScan.IsValid) return;

            int tx = (int)(tileScan.WorldCenter.X / 16f);
            int ty = (int)(tileScan.WorldCenter.Y / 16f);
            Rectangle bounds = TileScannable.GetTileWorldBounds(tx, ty);

            RenderAndComposite(sb, gd, shader, bounds, effectStr, isSelected: true);
        }

        /// <summary>
        /// 将包围盒范围内所有 HasTile 的物块重绘到 RT，套用着色器合成回屏幕
        /// </summary>
        private static void RenderAndComposite(SpriteBatch sb, GraphicsDevice gd, Effect shader,
            Rectangle worldBounds, float effectStr, bool isSelected) {

            //扩展包围盒为描边预留空间
            int rtW = Math.Min(worldBounds.Width + EdgePadding * 2, MaxRtSize);
            int rtH = Math.Min(worldBounds.Height + EdgePadding * 2, MaxRtSize);
            if (rtW <= 0 || rtH <= 0) return;

            EnsureRT(gd, rtW, rtH);
            if (_rt == null) return;

            //保存当前渲染目标用于后续还原
            RenderTargetBinding[] prevTargets = gd.GetRenderTargets();

            gd.SetRenderTarget(_rt);
            gd.Clear(Color.Transparent);

            //阶段1：按原始帧把物块重绘到 RT，透明像素自然形成轮廓掩码
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            RedrawTileRegion(sb, worldBounds);
            sb.End();

            //还原渲染目标
            if (prevTargets.Length > 0) {
                gd.SetRenderTarget((RenderTarget2D)prevTargets[0].RenderTarget);
            }
            else {
                gd.SetRenderTarget(null);
            }

            //阶段2：套着色器把 RT 绘回屏幕（加法混合，叠加在已绘制的原始物块之上）
            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / rtW, 1f / rtH));
            shader.Parameters["intensity"]?.SetValue(effectStr);
            shader.Parameters["isSelected"]?.SetValue(isSelected ? 1f : 0f);
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);

            Vector2 screenPos = new(
                worldBounds.X - EdgePadding - Main.screenPosition.X,
                worldBounds.Y - EdgePadding - Main.screenPosition.Y);

            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();
            sb.Draw(_rt, screenPos, Color.White);
            sb.End();
        }

        /// <summary>
        /// 按原始 FrameX/FrameY 将包围盒范围内的物块重绘到当前 RT
        /// <br/>RT 左上角对应包围盒左上角再偏移 EdgePadding 像素
        /// </summary>
        private static void RedrawTileRegion(SpriteBatch sb, Rectangle worldBounds) {
            int tx0 = worldBounds.X / 16;
            int ty0 = worldBounds.Y / 16;
            int tw = (worldBounds.Width + 15) / 16;
            int th = (worldBounds.Height + 15) / 16;
            //worldBounds 可能有负值偏移（树木扩展了分枝/树冠范围），用实际像素偏移修正
            int baseOffsetX = EdgePadding - (worldBounds.X - tx0 * 16);
            int baseOffsetY = EdgePadding - (worldBounds.Y - ty0 * 16);

            for (int i = 0; i < tw; i++) {
                for (int j = 0; j < th; j++) {
                    int x = tx0 + i;
                    int y = ty0 + j;
                    if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) continue;
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile) continue;

                    int type = tile.TileType;
                    Main.instance.LoadTiles(type);
                    Texture2D tex = TextureAssets.Tile[type]?.Value;
                    if (tex == null) continue;

                    //树木 trunk 使用 20x20 源帧，且需要 -2 像素偏移对齐
                    if (TileScannable.IsTreeTile(type)) {
                        Rectangle treeSrc = new(tile.TileFrameX, tile.TileFrameY, 20, 20);
                        Vector2 treeDst = new(i * 16 + baseOffsetX - 2, j * 16 + baseOffsetY - 2);
                        sb.Draw(tex, treeDst, treeSrc, Color.White);
                        //在 trunk 顶部绘制树冠，在侧分枝位置绘制分枝
                        TryDrawTreeExtras(sb, type, x, y, i * 16 + baseOffsetX, j * 16 + baseOffsetY);
                        continue;
                    }

                    //计算源矩形尺寸，frameImportant 走 TileObjectData，其他按 16x16
                    int srcW = 16;
                    int srcH = 16;
                    TileObjectData data = TileObjectData.GetTileData(type, 0);
                    if (data != null) {
                        srcW = data.CoordinateWidth;
                        //在多格物件内定位当前格所属的行，以取正确的高度
                        int subY = FindSubRow(data, tile.TileFrameY);
                        srcH = data.CoordinateHeights[subY];
                    }

                    Rectangle src = new(tile.TileFrameX, tile.TileFrameY, srcW, srcH);
                    //RT 内局部坐标：格偏移 * 16 + 修正后的基准偏移
                    Vector2 dst = new(i * 16 + baseOffsetX, j * 16 + baseOffsetY);
                    sb.Draw(tex, dst, src, Color.White);
                }
            }
        }

        /// <summary>
        /// 为树 trunk 在 RT 内补绘树冠和分枝
        /// <br/>vanilla 编码：frameX=22 左分枝节点/树冠节点, frameX=44 右分枝节点
        /// <br/>frameY>=198 且 frameX=22 时表示该 trunk 上方应绘制树冠
        /// <br/>使用 TextureAssets.TreeTop[0] / TreeBranch[0]（Forest 风格），不完美但足够产生轮廓掩码
        /// </summary>
        private static void TryDrawTreeExtras(SpriteBatch sb, int type, int tileX, int tileY, int rtBaseX, int rtBaseY) {
            Tile tile = Main.tile[tileX, tileY];
            int fx = tile.TileFrameX;
            int fy = tile.TileFrameY;

            //树顶标记：该 trunk tile 记录了树顶，在其上方 64px 处绘制树冠
            if (fx == 22 && fy >= 198) {
                Texture2D topTex = SafeGetTexture(TextureAssets.TreeTop, type);
                if (topTex != null) {
                    //80x80 树冠，居中对齐 trunk（-32 像素），向上 -64 像素
                    Rectangle topSrc = new(0, 0, 80, 80);
                    Vector2 topDst = new(rtBaseX - 32, rtBaseY - 80 + 16);
                    sb.Draw(topTex, topDst, topSrc, Color.White);
                }
            }

            //左分枝：frameX=22 && fy 在 0..132 范围内（非树顶标记）
            if (fx == 22 && fy < 198 && fy % 22 == 0) {
                Texture2D branchTex = SafeGetTexture(TextureAssets.TreeBranch, type);
                if (branchTex != null) {
                    Rectangle branchSrc = new(0, 0, 40, 40);
                    //左分枝贴在 trunk 左侧
                    Vector2 branchDst = new(rtBaseX - 40, rtBaseY - 12);
                    sb.Draw(branchTex, branchDst, branchSrc, Color.White);
                }
            }
            //右分枝：frameX=44
            else if (fx == 44 && fy < 198 && fy % 22 == 0) {
                Texture2D branchTex = SafeGetTexture(TextureAssets.TreeBranch, type);
                if (branchTex != null) {
                    //右分枝使用第 2 列(x=42 偏移 40 宽度帧)
                    Rectangle branchSrc = new(42, 0, 40, 40);
                    Vector2 branchDst = new(rtBaseX + 16, rtBaseY - 12);
                    sb.Draw(branchTex, branchDst, branchSrc, Color.White);
                }
            }
        }

        /// <summary>
        /// 从 TextureAssets 数组安全取纹理，index 0 作为默认样式
        /// </summary>
        private static Texture2D SafeGetTexture(ReLogic.Content.Asset<Texture2D>[] arr, int type) {
            if (arr == null || arr.Length == 0) return null;
            //区分 Palm / 原版 Trees / 其他 vanity tree 不同索引规则过于繁琐
            //统一用 index 0，足以形成轮廓掩码让着色器描边
            var asset = arr[0];
            return asset?.Value;
        }

        /// <summary>
        /// 根据 TileFrameY 反推当前格在多格物件中的子行索引
        /// <br/>多格物件每行高度可能不同，需要逐行累加判断
        /// </summary>
        private static int FindSubRow(TileObjectData data, int frameY) {
            int rows = data.Height;
            if (rows <= 1) return 0;
            int totalHeight = 0;
            for (int r = 0; r < rows; r++) {
                totalHeight += data.CoordinateHeights[r] + data.CoordinatePadding;
            }
            if (totalHeight <= 0) return 0;
            int frameYInObject = frameY % totalHeight;
            int acc = 0;
            for (int r = 0; r < rows; r++) {
                int rh = data.CoordinateHeights[r] + data.CoordinatePadding;
                if (frameYInObject < acc + rh) return r;
                acc += rh;
            }
            return rows - 1;
        }

        private static void EnsureRT(GraphicsDevice gd, int w, int h) {
            if (_rt != null && !_rt.IsDisposed && _rt.Width == w && _rt.Height == h) return;
            _rt?.Dispose();
            _rt = new RenderTarget2D(gd, w, h, false, SurfaceFormat.Color,
                DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
        }
    }
}
