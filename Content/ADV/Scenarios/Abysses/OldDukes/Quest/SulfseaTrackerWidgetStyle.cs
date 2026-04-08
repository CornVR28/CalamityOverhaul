using CalamityOverhaul.Content.ADV.EntrustManager;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.Abysses.OldDukes.Quest
{
    /// <summary>
    /// 硫磺海/老公爵委托在屏幕左侧追踪窗口中的自定义样式——
    /// 深绿毒雾渐变背景、硫磺酸液脉冲边框、
    /// 气泡装饰、毒绿进度条
    /// </summary>
    internal class SulfseaTrackerWidgetStyle : IEntrustTrackerWidgetStyle
    {
        #region 色板（与 SulfseaTrackerStyle 一致）

        private static readonly Color BgDeep = new(12, 18, 8);
        private static readonly Color BgMid = new(28, 38, 15);
        private static readonly Color BorderBase = new(70, 100, 35);
        private static readonly Color BorderGlow = new(130, 160, 65);
        private static readonly Color TitleWarm = new(160, 190, 80);
        private static readonly Color TextBody = new(200, 220, 150);
        private static readonly Color AccentAcid = new(140, 180, 70);
        private static readonly Color AccentBubble = new(100, 140, 50);
        private static readonly Color BarStart = new(60, 100, 30);
        private static readonly Color BarEnd = new(160, 190, 80);

        #endregion

        private float pulseTimer;
        private float wavePhase;
        private float bubbleTimer;

        public void Update(Rectangle widgetRect, float slideProgress) {
            pulseTimer += 0.025f;
            if (pulseTimer > MathHelper.TwoPi) pulseTimer -= MathHelper.TwoPi;
            wavePhase += 0.02f;
            if (wavePhase > MathHelper.TwoPi) wavePhase -= MathHelper.TwoPi;
            bubbleTimer += 0.03f;
            if (bubbleTimer > MathHelper.TwoPi) bubbleTimer -= MathHelper.TwoPi;
        }

        public void Reset() {
            pulseTimer = 0f;
            wavePhase = 0f;
            bubbleTimer = 0f;
        }

        #region 面板绘制

        public void DrawWidgetBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;

            //阴影
            Rectangle shadow = rect;
            shadow.Offset(3, 3);
            sb.Draw(px, shadow, new Rectangle(0, 0, 1, 1), Color.Black * (alpha * 0.5f));

            //纵向渐变背景
            int segs = 10;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = rect.Y + (int)(t * rect.Height);
                int y2 = rect.Y + (int)(t2 * rect.Height);

                float breathing = MathF.Sin(pulseTimer + t * 1.4f) * 0.5f + 0.5f;
                Color c = Color.Lerp(BgDeep, BgMid, t * 0.5f + breathing * 0.15f) * (alpha * 0.92f);
                sb.Draw(px, new Rectangle(rect.X, y1, rect.Width, Math.Max(1, y2 - y1)),
                    new Rectangle(0, 0, 1, 1), c);
            }

            //瘴气脉冲叠加
            float miasma = MathF.Sin(pulseTimer * 1.5f) * 0.5f + 0.5f;
            Color miasmaC = new Color(45, 55, 20) * (alpha * 0.08f * miasma);
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), miasmaC);

            //横向毒波纹
            for (int i = 0; i < 3; i++) {
                float t = (i + 1) / 4f;
                float y = rect.Y + t * rect.Height;
                float wave = MathF.Sin(wavePhase * 2f + t * MathHelper.Pi) * 2f;
                int wy = (int)(y + wave);
                if (wy >= rect.Y && wy < rect.Bottom) {
                    sb.Draw(px, new Rectangle(rect.X + 3, wy, rect.Width - 6, 1),
                        new Rectangle(0, 0, 1, 1), AccentBubble * (alpha * 0.05f));
                }
            }
        }

        public void DrawWidgetFrame(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;

            float glow = MathF.Sin(wavePhase) * 0.3f + 0.7f;
            Color edgeC = Color.Lerp(BorderBase, BorderGlow, glow) * (alpha * 0.8f);

            //顶部双线
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 2), new Rectangle(0, 0, 1, 1), edgeC);
            sb.Draw(px, new Rectangle(rect.X, rect.Y + 2, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), edgeC * 0.35f);

            //底部
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), edgeC * 0.5f);

            //左侧强调线
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 2, rect.Height),
                new Rectangle(0, 0, 1, 1), edgeC * 0.88f);

            //右侧淡线
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height),
                new Rectangle(0, 0, 1, 1), edgeC * 0.3f);

            //角落星形装饰
            float ornAlpha = alpha * (0.5f + MathF.Sin(pulseTimer * 1.5f) * 0.25f);
            Color ornC = AccentAcid * ornAlpha;
            //左上
            sb.Draw(px, new Vector2(rect.X + 6, rect.Y + 6), null, ornC,
                0f, new Vector2(0.5f), new Vector2(4f, 1.2f), SpriteEffects.None, 0f);
            sb.Draw(px, new Vector2(rect.X + 6, rect.Y + 6), null, ornC * 0.8f,
                MathHelper.PiOver2, new Vector2(0.5f), new Vector2(4f, 1.2f), SpriteEffects.None, 0f);
            //右上
            sb.Draw(px, new Vector2(rect.Right - 6, rect.Y + 6), null, ornC,
                0f, new Vector2(0.5f), new Vector2(4f, 1.2f), SpriteEffects.None, 0f);
            sb.Draw(px, new Vector2(rect.Right - 6, rect.Y + 6), null, ornC * 0.8f,
                MathHelper.PiOver2, new Vector2(0.5f), new Vector2(4f, 1.2f), SpriteEffects.None, 0f);
        }

        public void DrawWidgetHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha) {
            var px = VaultAsset.placeholder2.Value;

            //标题栏背景
            Color hdrBg = new Color(8, 12, 6) * (alpha * 0.65f);
            BaseManagerStyle.FillRect(sb, headerRect, hdrBg);

            //菱形图标
            float iconX = headerRect.X + 10f;
            float iconY = headerRect.Y + headerRect.Height / 2f;
            float iconPulse = MathF.Sin(pulseTimer * 2f) * 0.3f + 0.7f;
            sb.Draw(px, new Vector2(iconX, iconY), null, AccentAcid * (alpha * iconPulse),
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(3.5f), SpriteEffects.None, 0f);

            //标题文字——超出宽度时截断加省略号
            var font = FontAssets.MouseText.Value;
            float maxTitleW = headerRect.Width - 30f;
            if (font.MeasureString(title).X * 0.72f > maxTitleW) {
                while (title.Length > 3 && font.MeasureString(title + "...").X * 0.72f > maxTitleW)
                    title = title[..^1];
                title += "...";
            }
            Color titleC = TitleWarm * alpha;
            Utils.DrawBorderString(sb, title,
                new Vector2(headerRect.X + 22f, headerRect.Y + (headerRect.Height - 16f) / 2f),
                titleC, 0.72f);

            //底部分隔线（渐变）
            int divW = headerRect.Width - 8;
            int segs = 16;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                int x1 = headerRect.X + 4 + (int)(t * divW);
                int x2 = headerRect.X + 4 + (int)((i + 1f) / segs * divW);
                Color c = Color.Lerp(AccentAcid * (alpha * 0.55f), AccentBubble * (alpha * 0.08f), t);
                sb.Draw(px, new Rectangle(x1, headerRect.Bottom - 1, Math.Max(1, x2 - x1), 1),
                    new Rectangle(0, 0, 1, 1), c);
            }
        }

        public void DrawWidgetProgress(SpriteBatch sb, Rectangle barRect, float progress,
            string progressText, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var font = FontAssets.MouseText.Value;

            //背景
            BaseManagerStyle.FillRect(sb, barRect, Color.Black * (alpha * 0.6f));

            //渐变填充
            int fillW = (int)(barRect.Width * MathHelper.Clamp(progress, 0f, 1f));
            if (fillW > 2) {
                Rectangle fill = new(barRect.X + 1, barRect.Y + 1, fillW - 2, barRect.Height - 2);
                int segs = 12;
                for (int i = 0; i < segs; i++) {
                    float t = i / (float)segs;
                    float t2 = (i + 1) / (float)segs;
                    int sx1 = fill.X + (int)(t * fill.Width);
                    int sx2 = fill.X + (int)(t2 * fill.Width);
                    Color c = Color.Lerp(BarStart, BarEnd, t);
                    float pulse = MathF.Sin(pulseTimer * 2f + t * MathHelper.Pi) * 0.2f + 0.8f;
                    sb.Draw(px, new Rectangle(sx1, fill.Y, Math.Max(1, sx2 - sx1), fill.Height),
                        new Rectangle(0, 0, 1, 1), c * (alpha * pulse));
                }

                //顶部发光
                sb.Draw(px, new Rectangle(fill.X, fill.Y - 1, fill.Width, 1),
                    new Rectangle(0, 0, 1, 1), AccentAcid * (alpha * 0.35f));
            }

            //边框
            Color borderC = AccentBubble * (alpha * 0.5f);
            sb.Draw(px, new Rectangle(barRect.X, barRect.Y, barRect.Width, 1),
                new Rectangle(0, 0, 1, 1), borderC);
            sb.Draw(px, new Rectangle(barRect.X, barRect.Bottom - 1, barRect.Width, 1),
                new Rectangle(0, 0, 1, 1), borderC);
            sb.Draw(px, new Rectangle(barRect.X, barRect.Y, 1, barRect.Height),
                new Rectangle(0, 0, 1, 1), borderC);
            sb.Draw(px, new Rectangle(barRect.Right - 1, barRect.Y, 1, barRect.Height),
                new Rectangle(0, 0, 1, 1), borderC);

            //进度文本
            if (!string.IsNullOrEmpty(progressText)) {
                float textW = font.MeasureString(progressText).X * 0.5f;
                Utils.DrawBorderString(sb, progressText,
                    new Vector2(barRect.Right - textW - 2f, barRect.Bottom + 2f),
                    AccentAcid * (alpha * 0.65f), 0.5f);
            }
        }

        public void DrawWidgetDivider(SpriteBatch sb, Vector2 start, Vector2 end, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            float w = end.X - start.X;
            int segs = 16;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float x = start.X + t * w;
                float nexX = start.X + (i + 1f) / segs * w;
                Color c = Color.Lerp(AccentAcid * (alpha * 0.55f), AccentBubble * (alpha * 0.05f), t);
                sb.Draw(px, new Rectangle((int)x, (int)start.Y, Math.Max(1, (int)(nexX - x)), 1),
                    new Rectangle(0, 0, 1, 1), c);
            }
        }

        public void DrawWidgetOverlay(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;

            //底部微弱毒雾辉光
            float glowStr = MathF.Sin(pulseTimer) * 0.12f + 0.12f;
            for (int i = 0; i < 3; i++) {
                int y = rect.Bottom - 3 + i;
                float fade = (3 - i) / 3f;
                Color c = AccentBubble * (alpha * glowStr * fade * 0.25f);
                sb.Draw(px, new Rectangle(rect.X + 2, y, rect.Width - 4, 1),
                    new Rectangle(0, 0, 1, 1), c);
            }

            //漂浮气泡点装饰
            for (int i = 0; i < 3; i++) {
                float phase = (bubbleTimer + i * MathHelper.TwoPi / 3f) % MathHelper.TwoPi;
                float yOff = MathF.Sin(phase) * 8f;
                float xPos = rect.X + 12f + i * (rect.Width - 24f) / 2f;
                float yPos = rect.Y + rect.Height * 0.5f + yOff;

                if (yPos > rect.Y + 5 && yPos < rect.Bottom - 5) {
                    float bSize = 2f + MathF.Sin(phase * 2f) * 0.8f;
                    Color bColor = AccentAcid * (alpha * 0.2f);
                    sb.Draw(px, new Vector2(xPos, yPos), null, bColor,
                        0f, new Vector2(0.5f), new Vector2(bSize), SpriteEffects.None, 0f);
                }
            }
        }

        #endregion

        #region 颜色

        public Color GetWidgetTitleColor(float alpha) => TitleWarm * alpha;

        public Color GetWidgetTextColor(float alpha) => TextBody * (alpha * 0.85f);

        public Color GetWidgetAccentColor(float alpha) => AccentAcid * alpha;

        #endregion

        #region 度量

        public int? GetPreferredWidth() => null;

        public int? GetMinHeight() => 90;

        #endregion
    }
}
