using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Extras.Styles
{
    public class HotwindExtraStyle : IExtraStyle
    {
        private float flowTimer;
        private float pulseTimer;
        private float shaderTime;
        private const int EdgePad = 16;

        public void UpdateStyle() {
            flowTimer += 0.025f;
            if (flowTimer > MathHelper.TwoPi) flowTimer -= MathHelper.TwoPi;
            pulseTimer += 0.025f;
            shaderTime += 0.004f;
            if (shaderTime > 100f) shaderTime -= 100f;
        }

        #region 面板背景

        public void DrawBackground(SpriteBatch sb, ExtraMain extra, Rectangle panelRect) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float alpha = extra.PanelAlpha;

            //着色器面板
            if (EffectLoader.HotwindPanel?.Value != null) {
                Effect effect = EffectLoader.HotwindPanel.Value;
                Rectangle extRect = panelRect;
                extRect.Inflate(EdgePad, EdgePad);

                effect.Parameters["uTime"]?.SetValue(shaderTime);
                effect.Parameters["uAlpha"]?.SetValue(alpha * 0.97f);
                effect.Parameters["uResolution"]?.SetValue(new Vector2(extRect.Width, extRect.Height));
                effect.Parameters["uEdgePad"]?.SetValue((float)EdgePad);
                effect.Parameters["uNightMode"]?.SetValue(0f);

                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.AnisotropicClamp, DepthStencilState.None,
                    RasterizerState.CullNone, effect, Main.UIScaleMatrix);

                sb.Draw(px, extRect, Color.White);

                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.AnisotropicClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.UIScaleMatrix);
            }
            else {
                DrawFallbackBackground(sb, px, panelRect, alpha);
            }

            //角落铆钉
            float pulse = MathF.Sin(pulseTimer * 2f) * 0.5f + 0.5f;
            DrawCornerRivet(sb, new Vector2(panelRect.X + 12, panelRect.Y + 12), pulse, alpha);
            DrawCornerRivet(sb, new Vector2(panelRect.Right - 12, panelRect.Y + 12), pulse, alpha);
            DrawCornerRivet(sb, new Vector2(panelRect.X + 12, panelRect.Bottom - 12), pulse * 0.7f, alpha);
            DrawCornerRivet(sb, new Vector2(panelRect.Right - 12, panelRect.Bottom - 12), pulse * 0.7f, alpha);
        }

        private void DrawFallbackBackground(SpriteBatch sb, Texture2D px, Rectangle rect, float alpha) {
            Color top = new Color(28, 18, 10);
            Color mid = new Color(18, 10, 6);
            Color bot = new Color(10, 6, 4);

            int segs = 20;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1f) / segs;
                int y1 = rect.Y + (int)(t * rect.Height);
                int y2 = rect.Y + (int)(t2 * rect.Height);
                Color c = t < 0.5f
                    ? Color.Lerp(top, mid, t * 2f)
                    : Color.Lerp(mid, bot, (t - 0.5f) * 2f);
                sb.Draw(px, new Rectangle(rect.X, y1, rect.Width, Math.Max(1, y2 - y1)), c * alpha);
            }

            //扫描线
            Color scanC = new Color(30, 18, 8);
            for (int y = rect.Y; y < rect.Bottom; y += 3)
                sb.Draw(px, new Rectangle(rect.X + 2, y, rect.Width - 4, 1), scanC * (alpha * 0.08f));

            //暗角
            int vigW = 30;
            for (int v = 0; v < vigW; v += 3) {
                float fade = (1f - v / (float)vigW);
                fade *= fade;
                Color vc = Color.Black * (alpha * 0.18f * fade);
                sb.Draw(px, new Rectangle(rect.X + v, rect.Y, 2, rect.Height), vc);
                sb.Draw(px, new Rectangle(rect.Right - v - 2, rect.Y, 2, rect.Height), vc);
            }

            //脉冲光覆盖
            float pulse = MathF.Sin(pulseTimer * 2f) * 0.5f + 0.5f;
            Color pulseC = new Color(160, 70, 30);
            sb.Draw(px, rect, pulseC * (0.03f * pulse * alpha));
        }

        #endregion

        #region 标题与标签栏

        public void DrawTitle(SpriteBatch sb, ExtraMain extra, Rectangle panelRect, float alpha) {
            var font = FontAssets.DeathText.Value;
            var smallFont = FontAssets.MouseText.Value;

            //大标题 "EXTRA"
            string title = ExtraMain.ExtraText.Value;
            Vector2 titleSize = font.MeasureString(title) * 0.65f;
            Vector2 titlePos = new Vector2(panelRect.X + 25, panelRect.Y + 12);
            Utils.DrawBorderStringFourWay(sb, font, title,
                titlePos.X, titlePos.Y, new Color(235, 185, 125) * alpha, Color.Black * alpha,
                Vector2.Zero, 0.65f);

            //完成度
            float progress = extra.GetGalleryProgress();
            string progressText = $"{ExtraMain.GalleryText.Value}  {progress * 100:F2}%";
            Vector2 progPos = new Vector2(titlePos.X + titleSize.X + 20, titlePos.Y + titleSize.Y - 18);
            Utils.DrawBorderString(sb, progressText, progPos,
                new Color(180, 150, 110) * alpha, 0.8f);
        }

        public void DrawTabBar(SpriteBatch sb, ExtraMain extra, Rectangle panelRect, ExtraTab activeTab, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            var font = FontAssets.MouseText.Value;
            Rectangle tabBarRect = GetTabBarRect(panelRect);

            string[] tabNames = [ExtraMain.GalleryText.Value, ExtraMain.SceneText.Value, ExtraMain.MusicText.Value, ExtraMain.StaffText.Value];
            ExtraTab[] tabs = [ExtraTab.Gallery, ExtraTab.Scene, ExtraTab.Music, ExtraTab.Staff];
            bool[] available = [true, false, false, false]; //首期只有Gallery可用

            float totalWidth = 0;
            float[] widths = new float[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++) {
                widths[i] = font.MeasureString(tabNames[i]).X + 30;
                totalWidth += widths[i];
            }

            float startX = tabBarRect.Right - totalWidth;

            for (int i = 0; i < tabNames.Length; i++) {
                Rectangle tabRect = new Rectangle((int)startX, tabBarRect.Y, (int)widths[i], tabBarRect.Height);
                bool isActive = (tabs[i] == activeTab);
                bool isHovered = tabRect.Contains(Main.MouseScreen.ToPoint());
                bool isAvailable = available[i];

                Color textColor;
                if (isActive) {
                    //选中标签：热风红
                    textColor = new Color(220, 80, 50);
                }
                else if (!isAvailable) {
                    //不可用：暗灰
                    textColor = new Color(80, 80, 90);
                }
                else if (isHovered) {
                    textColor = new Color(255, 200, 130);
                }
                else {
                    textColor = new Color(180, 160, 140);
                }

                Utils.DrawBorderString(sb, tabNames[i],
                    new Vector2(tabRect.X + tabRect.Width / 2, tabRect.Y + tabRect.Height / 2),
                    textColor * alpha, 0.95f, 0.5f, 0.5f);

                //选中标签底部指示线
                if (isActive) {
                    sb.Draw(px, new Rectangle(tabRect.X + 5, tabRect.Bottom - 2, tabRect.Width - 10, 2),
                        new Color(220, 100, 50) * (alpha * 0.8f));
                }

                startX += widths[i];
            }

            //标签栏底部分隔线
            sb.Draw(px, new Rectangle(panelRect.X + 15, tabBarRect.Bottom, panelRect.Width - 30, 1),
                new Color(90, 60, 30) * (alpha * 0.5f));
        }

        #endregion

        #region CG缩略图

        public void DrawCGThumbnail(SpriteBatch sb, CGEntry entry, Rectangle thumbRect, bool isHovered, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //投影
            Rectangle shadowR = thumbRect;
            shadowR.Offset(2, 3);
            sb.Draw(px, shadowR, Color.Black * (0.4f * alpha));

            //尝试绘制缩略图纹理
            Texture2D thumb = entry.GetThumbnail();
            if (thumb != null) {
                Rectangle? srcRect = entry.GetThumbnailSourceRect(thumb);
                sb.Draw(thumb, thumbRect, srcRect, Color.White * alpha);
            }
            else {
                //没有缩略图时用深色填充
                sb.Draw(px, thumbRect, new Color(25, 18, 12) * alpha);
            }

            //边框（凹凸光照：上左亮，下右暗）
            int bw = isHovered ? 2 : 1;
            Color hlEdge = isHovered ? Color.White : new Color(200, 140, 70);
            Color shEdge = Color.Black;
            sb.Draw(px, new Rectangle(thumbRect.X, thumbRect.Y, thumbRect.Width, bw), hlEdge * (0.5f * alpha));
            sb.Draw(px, new Rectangle(thumbRect.X, thumbRect.Y, bw, thumbRect.Height), hlEdge * (0.4f * alpha));
            sb.Draw(px, new Rectangle(thumbRect.Right - bw, thumbRect.Y, bw, thumbRect.Height), shEdge * (0.5f * alpha));
            sb.Draw(px, new Rectangle(thumbRect.X, thumbRect.Bottom - bw, thumbRect.Width, bw), shEdge * (0.5f * alpha));

            //悬浮时的光亮覆盖
            if (isHovered) {
                sb.Draw(px, thumbRect, Color.White * (0.08f * alpha));
            }
        }

        public void DrawLockedCG(SpriteBatch sb, Rectangle thumbRect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            var font = FontAssets.DeathText.Value;

            //投影
            Rectangle shadowR = thumbRect;
            shadowR.Offset(2, 3);
            sb.Draw(px, shadowR, Color.Black * (0.35f * alpha));

            //三段深色纵向渐变
            Color top = new Color(22, 16, 12);
            Color mid = new Color(14, 10, 8);
            Color bot = new Color(8, 6, 4);
            int segs = 6;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1f) / segs;
                int y1 = thumbRect.Y + (int)(t * thumbRect.Height);
                int y2 = thumbRect.Y + (int)(t2 * thumbRect.Height);
                Color c = t < 0.5f
                    ? Color.Lerp(top, mid, t * 2f)
                    : Color.Lerp(mid, bot, (t - 0.5f) * 2f);
                sb.Draw(px, new Rectangle(thumbRect.X, y1, thumbRect.Width, Math.Max(1, y2 - y1)), c * alpha);
            }

            //扫描线效果
            for (int y = thumbRect.Y; y < thumbRect.Bottom; y += 3) {
                sb.Draw(px, new Rectangle(thumbRect.X + 1, y, thumbRect.Width - 2, 1),
                    new Color(30, 20, 10) * (alpha * 0.06f));
            }

            //中央 "?" 字符
            string q = "?";
            Vector2 qSize = font.MeasureString(q) * 0.6f;
            Vector2 qPos = new Vector2(
                thumbRect.X + thumbRect.Width / 2f,
                thumbRect.Y + thumbRect.Height / 2f);
            float qPulse = MathF.Sin(pulseTimer * 1.5f) * 0.15f + 0.85f;
            Color qColor = new Color(80, 55, 35) * (alpha * qPulse);
            Utils.DrawBorderString(sb, q, qPos, qColor, 0.6f, 0.5f, 0.5f);

            //边框（暗色调）
            Color edgeC = new Color(50, 35, 22);
            sb.Draw(px, new Rectangle(thumbRect.X, thumbRect.Y, thumbRect.Width, 1), edgeC * (0.5f * alpha));
            sb.Draw(px, new Rectangle(thumbRect.X, thumbRect.Y, 1, thumbRect.Height), edgeC * (0.4f * alpha));
            sb.Draw(px, new Rectangle(thumbRect.Right - 1, thumbRect.Y, 1, thumbRect.Height), Color.Black * (0.4f * alpha));
            sb.Draw(px, new Rectangle(thumbRect.X, thumbRect.Bottom - 1, thumbRect.Width, 1), Color.Black * (0.4f * alpha));
        }

        #endregion

        #region 滚动条

        public void DrawScrollbar(SpriteBatch sb, Rectangle trackRect, float scrollRatio, float viewRatio, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //轨道暗底
            sb.Draw(px, trackRect, new Color(10, 8, 5) * (0.6f * alpha));

            //滑块
            float thumbH = Math.Max(20, trackRect.Height * viewRatio);
            float thumbY = trackRect.Y + (trackRect.Height - thumbH) * scrollRatio;
            Rectangle thumbRect = new Rectangle(trackRect.X, (int)thumbY, trackRect.Width, (int)thumbH);

            //滑块金属渐变
            Color topC = new Color(180, 100, 40);
            Color botC = new Color(120, 60, 22);
            int steps = 4;
            for (int i = 0; i < steps; i++) {
                float t = i / (float)steps;
                float t2 = (i + 1f) / steps;
                int y1 = thumbRect.Y + (int)(t * thumbRect.Height);
                int y2 = thumbRect.Y + (int)(t2 * thumbRect.Height);
                Color c = Color.Lerp(topC, botC, t);
                sb.Draw(px, new Rectangle(thumbRect.X, y1, thumbRect.Width, Math.Max(1, y2 - y1)), c * alpha);
            }

            //滑块高光
            sb.Draw(px, new Rectangle(thumbRect.X, thumbRect.Y, thumbRect.Width, 1),
                Color.White * (0.2f * alpha));
        }

        #endregion

        #region 全屏查看器

        public void DrawFullscreenViewer(SpriteBatch sb, CGEntry entry, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //全屏遮罩
            sb.Draw(px, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                Color.Black * (0.85f * alpha));

            //CG大图
            Texture2D fullImg = entry.GetFullImage();
            if (fullImg != null) {
                Rectangle? srcRect = entry.GetFullImageSourceRect(fullImg);
                Rectangle src = srcRect ?? fullImg.Bounds;

                //按屏幕比例适配缩放
                float maxW = Main.screenWidth * 0.85f;
                float maxH = Main.screenHeight * 0.85f;
                float scale = Math.Min(maxW / src.Width, maxH / src.Height);
                int drawW = (int)(src.Width * scale);
                int drawH = (int)(src.Height * scale);
                Rectangle drawRect = new Rectangle(
                    (Main.screenWidth - drawW) / 2,
                    (Main.screenHeight - drawH) / 2,
                    drawW, drawH);

                sb.Draw(fullImg, drawRect, srcRect, Color.White * alpha);

                //边框
                Color edgeC = new Color(180, 110, 50);
                sb.Draw(px, new Rectangle(drawRect.X - 2, drawRect.Y - 2, drawRect.Width + 4, 2), edgeC * (0.6f * alpha));
                sb.Draw(px, new Rectangle(drawRect.X - 2, drawRect.Y, 2, drawRect.Height), edgeC * (0.4f * alpha));
                sb.Draw(px, new Rectangle(drawRect.Right, drawRect.Y, 2, drawRect.Height), Color.Black * (0.5f * alpha));
                sb.Draw(px, new Rectangle(drawRect.X - 2, drawRect.Bottom, drawRect.Width + 4, 2), Color.Black * (0.5f * alpha));
            }

            //底部CG名称
            string name = entry.DisplayName?.Value ?? "???";
            Utils.DrawBorderString(sb, name,
                new Vector2(Main.screenWidth / 2f, Main.screenHeight * 0.94f),
                new Color(235, 200, 150) * alpha, 0.9f, 0.5f, 0.5f);
        }

        public void DrawViewerArrow(SpriteBatch sb, Rectangle arrowRect, bool isLeft, bool isHovered, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            Color bgC = isHovered ? new Color(80, 55, 30) : new Color(40, 28, 15);
            Color arrowC = isHovered ? Color.White : new Color(220, 170, 110);

            //按钮背景
            sb.Draw(px, arrowRect, bgC * (0.7f * alpha));

            //箭头符号
            string arrow = isLeft ? "<" : ">";
            Utils.DrawBorderString(sb, arrow,
                arrowRect.Center.ToVector2(),
                arrowC * alpha, 1.2f, 0.5f, 0.5f);
        }

        #endregion

        #region 按钮

        public void DrawBackButton(SpriteBatch sb, Rectangle panelRect, bool isHovered, float alpha) {
            Rectangle btnRect = GetBackButtonRect(panelRect);
            DrawMetallicButton(sb, btnRect, ExtraMain.BackText.Value, isHovered, alpha);
        }

        private void DrawMetallicButton(SpriteBatch sb, Rectangle rect, string text, bool hover, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float pulse = MathF.Sin(pulseTimer * 3f) * 0.5f + 0.5f;

            //投影
            Rectangle shadowR = rect;
            shadowR.Offset(2, 3);
            sb.Draw(px, shadowR, Color.Black * (0.4f * alpha));

            //纵向金属渐变
            Color topC = hover ? new Color(220, 145, 65) : new Color(185, 110, 45);
            Color botC = hover ? new Color(150, 85, 30) : new Color(120, 65, 22);
            int steps = 8;
            for (int i = 0; i < steps; i++) {
                float t = i / (float)steps;
                float t2 = (i + 1f) / steps;
                int y1 = rect.Y + (int)(t * rect.Height);
                int y2 = rect.Y + (int)(t2 * rect.Height);
                Color c = Color.Lerp(topC, botC, t);
                sb.Draw(px, new Rectangle(rect.X, y1, rect.Width, Math.Max(1, y2 - y1)), c * alpha);
            }

            //顶部反光条
            sb.Draw(px, new Rectangle(rect.X + 3, rect.Y + 1, rect.Width - 6, 1),
                Color.White * (0.25f * alpha));

            //光照边缘
            Color hlC = Color.Lerp(new Color(255, 200, 120), new Color(255, 230, 170), pulse);
            if (hover) hlC = Color.White;
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1), hlC * (0.6f * alpha));
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 1, rect.Height), hlC * (0.4f * alpha));
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), Color.Black * (0.3f * alpha));
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), Color.Black * (0.4f * alpha));

            //文字
            Color textC = hover ? Color.White : new Color(255, 245, 230);
            Utils.DrawBorderString(sb, text,
                new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2),
                textC * alpha, 0.85f, 0.5f, 0.5f);
        }

        #endregion

        #region 通用装饰

        private void DrawCornerRivet(SpriteBatch sb, Vector2 pos, float pulse, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            Color baseC = new Color(190, 110, 50);
            Color glowC = new Color(160, 80, 35);

            sb.Draw(px, pos, new Rectangle(0, 0, 1, 1), glowC * (0.2f * pulse * alpha), 0f,
                new Vector2(0.5f, 0.5f), new Vector2(14f, 14f), SpriteEffects.None, 0f);
            sb.Draw(px, pos, new Rectangle(0, 0, 1, 1), baseC * (0.8f * pulse * alpha), 0f,
                new Vector2(0.5f, 0.5f), new Vector2(5f, 5f), SpriteEffects.None, 0f);
            sb.Draw(px, pos + new Vector2(-1, -1), new Rectangle(0, 0, 1, 1),
                Color.White * (0.3f * pulse * alpha), 0f,
                new Vector2(0.5f, 0.5f), new Vector2(2f, 2f), SpriteEffects.None, 0f);
        }

        #endregion

        #region 布局查询

        public Rectangle GetTabBarRect(Rectangle panelRect) =>
            new Rectangle(panelRect.X + 15, panelRect.Y + 8, panelRect.Width - 30, 40);

        public Rectangle GetBackButtonRect(Rectangle panelRect) =>
            new Rectangle(panelRect.Right - 100, panelRect.Bottom - 45, 80, 30);

        public Rectangle GetLeftArrowRect() =>
            new Rectangle(40, Main.screenHeight / 2 - 25, 50, 50);

        public Rectangle GetRightArrowRect() =>
            new Rectangle(Main.screenWidth - 90, Main.screenHeight / 2 - 25, 50, 50);

        public Vector4 GetPadding() => new Vector4(20, 50, 20, 55);

        #endregion
    }
}
