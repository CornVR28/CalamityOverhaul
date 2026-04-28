using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.UI
{
    /// <summary>
    /// SHPC枪体改造面板
    /// 中央悬浮枪体程序化轮廓，数据分析线向外延伸连接六个改件插槽
    /// 由 <see cref="SHPCUI"/> 在固定二级面板模式下调用（按钮索引2）
    /// </summary>
    internal static class SHPCModPanel
    {
        public const float PanelW = 300f;
        public const float PanelH = 260f;

        private const int SlotCount = 6;
        private const float SlotW = 56f;
        private const float SlotH = 22f;

        //六个插槽中心相对于枪体中心的偏移
        private static readonly Vector2[] SlotOffsets = {
            new(0f, -76f),    //BARREL  枪管  正上
            new(84f, -38f),   //OPTIC   瞄具  右上
            new(84f, 38f),    //POWER   能源  右下
            new(0f, 76f),     //STOCK   枪托  正下
            new(-84f, 38f),   //GRIP    握把  左下
            new(-84f, -38f),  //FRAME   机匣  左上
        };

        //数据线在枪体上的接出点（相对枪体中心，对应各槽方向）
        private static readonly Vector2[] ConnectPoints = {
            new(4f, -9f),    //BARREL  向上引出
            new(14f, -5f),   //OPTIC   右上引出
            new(6f, 5f),     //POWER   右下引出
            new(-2f, 9f),    //STOCK   向下引出
            new(-17f, 5f),   //GRIP    左下引出
            new(-17f, -5f),  //FRAME   左上引出
        };

        private static readonly string[] SlotLabels = {
            "BARREL", "OPTIC", "POWER", "STOCK", "GRIP", "FRAME"
        };

        public enum HitKind
        {
            None,
            Slot0, Slot1, Slot2, Slot3, Slot4, Slot5,
        }

        public ref struct Layout
        {
            public Rectangle Panel;
            public Vector2 GunCenter;
        }

        public static Rectangle GetSlotRect(in Layout layout, int idx) {
            Vector2 off = SlotOffsets[idx];
            return new Rectangle(
                (int)(layout.GunCenter.X + off.X - SlotW * 0.5f),
                (int)(layout.GunCenter.Y + off.Y - SlotH * 0.5f),
                (int)SlotW, (int)SlotH);
        }

        public static Layout Compute(Vector2 anchor, float midAngle, float panelAlpha) {
            Vector2 outDir = SHPCRenderer.AngleDir(midAngle);
            float slide = (1f - panelAlpha) * 14f;
            Vector2 panelPos = anchor + outDir * (SHPCTheme.InfoPanelGap + slide);
            panelPos.Y -= PanelH * 0.5f;
            Rectangle panel = new((int)panelPos.X, (int)panelPos.Y, (int)PanelW, (int)PanelH);
            Vector2 gunCenter = new(panel.X + PanelW * 0.5f, panel.Y + PanelH * 0.50f);
            return new Layout { Panel = panel, GunCenter = gunCenter };
        }

        public static HitKind HitTest(in Layout layout, Vector2 mouse) {
            for (int i = 0; i < SlotCount; i++) {
                if (GetSlotRect(layout, i).Contains((int)mouse.X, (int)mouse.Y)) {
                    return (HitKind)(i + 1);
                }
            }
            return HitKind.None;
        }

        public static void HandleClick(HitKind hit, Player owner) {
            //暂无实现，后续用于打开具体改件选择界面
        }

        public static void Draw(SpriteBatch sb, Texture2D px, in Layout layout,
            float panelAlpha, float globalAlpha, HitKind hover) {
            if (panelAlpha < 0.02f) {
                return;
            }
            float a = panelAlpha * globalAlpha;
            float time = (float)Main.GameUpdateCount / 60f;
            Rectangle rect = layout.Panel;
            Vector2 gun = layout.GunCenter;

            //投影
            SHPCRenderer.DrawFilledRect(sb, px,
                new Rectangle(rect.X + 3, rect.Y + 4, rect.Width, rect.Height),
                new Color(0, 0, 0) * (0.55f * a));

            //背景
            SHPCRenderer.DrawFilledRect(sb, px, rect, new Color(4, 14, 22) * (0.96f * a));

            //横向滚动扫描线
            DrawScanLines(sb, px, rect, time, a);

            //外框与四角L形装饰
            SHPCRenderer.DrawRectStroke(sb, px, rect, 1.2f, SHPCTheme.Border * (0.9f * a));
            SHPCRenderer.DrawCornerBrackets(sb, px, rect, 10f, 1.5f, SHPCTheme.BorderHi * (0.9f * a));

            //顶部青色色带
            SHPCRenderer.DrawFilledRect(sb, px,
                new Rectangle(rect.X, rect.Y, rect.Width, 3),
                SHPCTheme.Cyan * (0.85f * a));

            DynamicSpriteFont font = FontAssets.MouseText.Value;

            //面板标题
            Utils.DrawBorderString(sb, SHPCUI.Modify_Title.Value,
                new Vector2(rect.X + 10f, rect.Y + 7f), SHPCTheme.Text * a, 0.62f);
            Utils.DrawBorderString(sb, SHPCUI.Modify_Subtitle.Value,
                new Vector2(rect.X + 10f, rect.Y + 24f), SHPCTheme.TextDim * a, 0.40f);

            //右上滚动ID码，增强科技感
            string idCode = $"SYS#{(int)(time * 13f) % 9999:D4}";
            Vector2 idSz = font.MeasureString(idCode) * 0.42f;
            Utils.DrawBorderString(sb, idCode,
                new Vector2(rect.Right - 10f - idSz.X, rect.Y + 9f),
                SHPCTheme.Cyan * (0.70f * a), 0.42f);

            //中央旋转扫描环（枪体外围）
            float scanAngle = time * 0.75f;
            SHPCRenderer.DrawArcStroke(sb, px, gun, 26f,
                scanAngle, scanAngle + 2.0f, 1.2f,
                SHPCTheme.Cyan * (0.45f * a));
            SHPCRenderer.DrawArcStroke(sb, px, gun, 26f,
                scanAngle + MathHelper.Pi, scanAngle + MathHelper.Pi + 1.4f, 1.0f,
                SHPCTheme.Cyan * (0.22f * a));

            //枪体外轮廓参考环（静态底层装饰）
            SHPCRenderer.DrawRing(sb, px, gun, 28f, 0.8f, SHPCTheme.Border * (0.3f * a));

            //数据分析线（绘于枪体下方）
            DrawDataLines(sb, px, gun, hover, time, a);

            //六个改件槽位
            for (int i = 0; i < SlotCount; i++) {
                Rectangle slotRect = GetSlotRect(layout, i);
                bool isHover = hover == (HitKind)(i + 1);
                DrawSlot(sb, px, font, slotRect, SlotLabels[i], isHover, a);
            }

            //SHPC枪体轮廓（绘于最上层）
            DrawGunSilhouette(sb, px, gun, time, a);
        }

        private static void DrawScanLines(SpriteBatch sb, Texture2D px,
            Rectangle rect, float time, float a) {
            const int spacing = 18;
            float scroll = (time * 10f) % spacing;
            for (float y = rect.Y + scroll; y < rect.Bottom; y += spacing) {
                SHPCRenderer.DrawLine(sb, px,
                    new Vector2(rect.X + 1, y), new Vector2(rect.Right - 1, y),
                    0.7f, new Color(20, 60, 80) * (0.28f * a));
            }
        }

        private static void DrawDataLines(SpriteBatch sb, Texture2D px,
            Vector2 gun, HitKind hover, float time, float a) {
            for (int i = 0; i < SlotCount; i++) {
                bool isHover = hover == (HitKind)(i + 1);
                Vector2 start = gun + ConnectPoints[i];
                Vector2 slotCenter = gun + SlotOffsets[i];

                Color lineCol = isHover
                    ? SHPCTheme.CyanHi * (0.85f * a)
                    : SHPCTheme.Border * (0.55f * a);

                //折线：上下槽先横后竖，左右槽先竖后横
                Vector2 mid = (i == 0 || i == 3)
                    ? new Vector2(slotCenter.X, start.Y)
                    : new Vector2(start.X, slotCenter.Y);

                SHPCRenderer.DrawLine(sb, px, start, mid, 1.2f, lineCol);
                SHPCRenderer.DrawLine(sb, px, mid, slotCenter, 1.2f, lineCol);

                //折点处的菱形节点
                SHPCRenderer.DrawFilledRect(sb, px,
                    new Rectangle((int)(mid.X - 2), (int)(mid.Y - 2), 4, 4),
                    lineCol);

                //枪体接出点的小方块
                SHPCRenderer.DrawFilledRect(sb, px,
                    new Rectangle((int)(start.X - 2), (int)(start.Y - 2), 4, 4),
                    lineCol * 1.2f);

                //悬停时线上增加流动脉冲效果
                if (isHover) {
                    float t = (time * 1.6f) % 1f;
                    Vector2 pulseA = Vector2.Lerp(start, mid, t);
                    Vector2 pulseB = Vector2.Lerp(mid, slotCenter, t);
                    Vector2 pulsePos = t < 0.5f ? pulseA : pulseB;
                    SHPCRenderer.DrawDisc(sb, px, pulsePos, 2.2f, 2f,
                        SHPCTheme.CyanHi * (0.8f * a));
                }
            }
        }

        private static void DrawSlot(SpriteBatch sb, Texture2D px,
            DynamicSpriteFont font, Rectangle r, string label, bool isHover, float a) {
            //投影
            SHPCRenderer.DrawFilledRect(sb, px,
                new Rectangle(r.X + 2, r.Y + 2, r.Width, r.Height),
                new Color(0, 0, 0) * (0.4f * a));

            //背景
            Color bg = isHover
                ? new Color(12, 50, 70) * (0.92f * a)
                : new Color(6, 20, 30) * (0.85f * a);
            SHPCRenderer.DrawFilledRect(sb, px, r, bg);

            //描边
            Color border = isHover
                ? SHPCTheme.CyanHi * (0.95f * a)
                : SHPCTheme.Border * (0.75f * a);
            SHPCRenderer.DrawRectStroke(sb, px, r, 1.2f, border);

            //悬停时四角装饰
            if (isHover) {
                SHPCRenderer.DrawCornerBrackets(sb, px, r, 4f, 1.2f, SHPCTheme.CyanHi * a);
            }

            //左侧状态色条（空槽为暗色，已安装时可改为亮色）
            SHPCRenderer.DrawFilledRect(sb, px,
                new Rectangle(r.X, r.Y, 3, r.Height),
                (isHover ? SHPCTheme.Cyan : SHPCTheme.Border) * (0.8f * a));

            //槽位标签
            float labelScale = 0.38f;
            Vector2 labelSz = font.MeasureString(label) * labelScale;
            Utils.DrawBorderString(sb, label,
                new Vector2(r.X + 7f, r.Y + (r.Height - labelSz.Y) * 0.5f),
                (isHover ? SHPCTheme.Text : SHPCTheme.TextDim) * a, labelScale);

            //右侧空槽标记
            const string emptyMark = "--";
            const float emptyScale = 0.34f;
            Vector2 emptySz = font.MeasureString(emptyMark) * emptyScale;
            Utils.DrawBorderString(sb, emptyMark,
                new Vector2(r.Right - 6f - emptySz.X, r.Y + (r.Height - emptySz.Y) * 0.5f),
                SHPCTheme.TextDim * (0.55f * a), emptyScale);
        }

        private static void DrawGunSilhouette(SpriteBatch sb, Texture2D px,
            Vector2 gun, float time, float a) {
            float pulse = 0.88f + MathF.Sin(time * 2.6f) * 0.12f;
            Color body = SHPCTheme.Cyan * (pulse * a);
            Color detail = SHPCTheme.CyanHi * (0.65f * pulse * a);

            //枪体发光底衬
            SHPCRenderer.DrawDisc(sb, px, gun, 5f, 10f, SHPCTheme.Cyan * (0.12f * a));

            //机匣主体（填充+描边）
            Rectangle recv = new((int)(gun.X - 18f), (int)(gun.Y - 7f), 29, 14);
            SHPCRenderer.DrawFilledRect(sb, px, recv, new Color(4, 20, 32) * (0.85f * a));
            SHPCRenderer.DrawRectStroke(sb, px, recv, 1.4f, body);

            //枪管上轮廓线
            SHPCRenderer.DrawLine(sb, px,
                new Vector2(gun.X + 11f, gun.Y - 3f),
                new Vector2(gun.X + 33f, gun.Y - 3f),
                1.4f, body);

            //枪管下轮廓线
            SHPCRenderer.DrawLine(sb, px,
                new Vector2(gun.X + 11f, gun.Y + 3f),
                new Vector2(gun.X + 33f, gun.Y + 3f),
                1.4f, body);

            //枪口端盖（高亮色）
            SHPCRenderer.DrawLine(sb, px,
                new Vector2(gun.X + 33f, gun.Y - 5f),
                new Vector2(gun.X + 33f, gun.Y + 5f),
                2.0f, SHPCTheme.CyanHi * (pulse * a));

            //枪管内腔能量光柱
            SHPCRenderer.DrawLine(sb, px,
                new Vector2(gun.X + 12f, gun.Y),
                new Vector2(gun.X + 32f, gun.Y),
                2.8f, SHPCTheme.CyanHi * (0.30f * pulse * a));

            //瞄准镜座
            Rectangle scope = new((int)(gun.X - 10f), (int)(gun.Y - 14f), 20, 7);
            SHPCRenderer.DrawFilledRect(sb, px, scope, new Color(4, 20, 32) * (0.7f * a));
            SHPCRenderer.DrawRectStroke(sb, px, scope, 1.2f, detail);

            //弹匣
            Rectangle mag = new((int)(gun.X - 5f), (int)(gun.Y + 7f), 8, 13);
            SHPCRenderer.DrawFilledRect(sb, px, mag, new Color(4, 20, 32) * (0.65f * a));
            SHPCRenderer.DrawRectStroke(sb, px, mag, 1.2f, detail);

            //枪托后弧线
            SHPCRenderer.DrawLine(sb, px,
                new Vector2(gun.X - 18f, gun.Y - 4f),
                new Vector2(gun.X - 30f, gun.Y - 2f),
                1.4f, body);
            SHPCRenderer.DrawLine(sb, px,
                new Vector2(gun.X - 18f, gun.Y + 3f),
                new Vector2(gun.X - 30f, gun.Y + 1f),
                1.4f, body);
            SHPCRenderer.DrawLine(sb, px,
                new Vector2(gun.X - 30f, gun.Y - 2f),
                new Vector2(gun.X - 30f, gun.Y + 1f),
                1.4f, body);
        }
    }
}
