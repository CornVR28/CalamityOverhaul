using CalamityOverhaul.Content.ADV.EntrustManager;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.SupCal.Quest
{
    /// <summary>
    /// 硫火女巫委托在屏幕左侧追踪窗口中的自定义样式——
    /// 深红渐变背景、硫火脉冲边框、余烬粒子、
    /// 火焰色进度条
    /// </summary>
    internal class BrimstoneTrackerWidgetStyle : IEntrustTrackerWidgetStyle
    {
        #region 色板

        private static readonly Color BgDeep = new(28, 14, 14);
        private static readonly Color BgMid = new(55, 25, 20);
        private static readonly Color BorderBase = new(140, 50, 30);
        private static readonly Color BorderGlow = new(255, 120, 50);
        private static readonly Color TitleWarm = new(255, 220, 180);
        private static readonly Color TextBody = new(220, 180, 160);
        private static readonly Color AccentFire = new(220, 80, 30);
        private static readonly Color AccentEmber = new(255, 140, 60);
        private static readonly Color BarStart = new(180, 50, 50);
        private static readonly Color BarEnd = new(255, 140, 60);
        private static readonly Color CompletedGreen = new(60, 220, 140);

        #endregion

        private float pulseTimer;
        private float shimmerPhase;

        public void Update(Rectangle widgetRect, float slideProgress) {
            pulseTimer += 0.03f;
            if (pulseTimer > MathHelper.TwoPi) pulseTimer -= MathHelper.TwoPi;
            shimmerPhase += 0.025f;
            if (shimmerPhase > MathHelper.TwoPi) shimmerPhase -= MathHelper.TwoPi;
        }

        public void Reset() {
            pulseTimer = 0f;
            shimmerPhase = 0f;
        }

        #region 面板绘制

        public void DrawWidgetBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;

            //阴影
            Rectangle shadow = rect;
            shadow.Offset(3, 3);
            sb.Draw(px, shadow, new Rectangle(0, 0, 1, 1), Color.Black * (alpha * 0.45f));

            //纵向渐变背景
            int segs = 10;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = rect.Y + (int)(t * rect.Height);
                int y2 = rect.Y + (int)(t2 * rect.Height);

                float wave = MathF.Sin(pulseTimer * 1.2f + t * 2f) * 0.5f + 0.5f;
                Color c = Color.Lerp(BgDeep, BgMid, t * 0.4f + wave * 0.15f) * (alpha * 0.92f);
                sb.Draw(px, new Rectangle(rect.X, y1, rect.Width, Math.Max(1, y2 - y1)),
                    new Rectangle(0, 0, 1, 1), c);
            }

            //脉冲叠加
            float pulse = MathF.Sin(pulseTimer * 2f) * 0.5f + 0.5f;
            Color pulseC = AccentFire * (alpha * 0.08f * pulse);
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), pulseC);
        }

        public void DrawWidgetFrame(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;

            float glow = MathF.Sin(shimmerPhase) * 0.3f + 0.7f;
            Color edgeC = Color.Lerp(BorderBase, BorderGlow, glow) * (alpha * 0.8f);

            //顶部双线
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 2), new Rectangle(0, 0, 1, 1), edgeC);
            sb.Draw(px, new Rectangle(rect.X, rect.Y + 2, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), edgeC * 0.4f);

            //底部
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), edgeC * 0.5f);

            //左侧强调线
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 2, rect.Height),
                new Rectangle(0, 0, 1, 1), edgeC * 0.9f);

            //右侧淡线
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height),
                new Rectangle(0, 0, 1, 1), edgeC * 0.3f);

            //角落装饰
            float ornAlpha = alpha * (0.5f + MathF.Sin(pulseTimer * 1.5f) * 0.3f);
            Color ornC = AccentEmber * ornAlpha;
            sb.Draw(px, new Vector2(rect.X + 5, rect.Y + 5), null, ornC,
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(3f), SpriteEffects.None, 0f);
            sb.Draw(px, new Vector2(rect.Right - 5, rect.Y + 5), null, ornC,
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(3f), SpriteEffects.None, 0f);
        }

        public void DrawWidgetHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha) {
            var px = VaultAsset.placeholder2.Value;

            //标题栏背景（稍暗于主体）
            Color hdrBg = new Color(20, 10, 10) * (alpha * 0.6f);
            BaseManagerStyle.FillRect(sb, headerRect, hdrBg);

            //硫火菱形图标
            float iconX = headerRect.X + 10f;
            float iconY = headerRect.Y + headerRect.Height / 2f;
            float iconPulse = MathF.Sin(pulseTimer + 1f) * 0.3f + 0.7f;
            sb.Draw(px, new Vector2(iconX, iconY), null, AccentFire * (alpha * iconPulse),
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(4f), SpriteEffects.None, 0f);

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
                Color c = Color.Lerp(AccentFire * (alpha * 0.6f), AccentFire * (alpha * 0.1f), t);
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
                    float pulse = MathF.Sin(pulseTimer * 2f + t * MathHelper.Pi) * 0.25f + 0.75f;
                    sb.Draw(px, new Rectangle(sx1, fill.Y, Math.Max(1, sx2 - sx1), fill.Height),
                        new Rectangle(0, 0, 1, 1), c * (alpha * pulse));
                }

                //顶部发光
                Color glowC = AccentEmber * (alpha * 0.4f);
                sb.Draw(px, new Rectangle(fill.X, fill.Y - 1, fill.Width, 1),
                    new Rectangle(0, 0, 1, 1), glowC);
            }

            //边框
            Color borderC = AccentFire * (alpha * 0.5f);
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
                    AccentEmber * (alpha * 0.7f), 0.5f);
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
                Color c = Color.Lerp(AccentFire * (alpha * 0.6f), AccentFire * (alpha * 0.05f), t);
                sb.Draw(px, new Rectangle((int)x, (int)start.Y, Math.Max(1, (int)(nexX - x)), 1),
                    new Rectangle(0, 0, 1, 1), c);
            }
        }

        public void DrawWidgetOverlay(SpriteBatch sb, Rectangle rect, float alpha) {
            //底部微弱的硫火辉光
            var px = VaultAsset.placeholder2.Value;
            float glowStr = MathF.Sin(pulseTimer) * 0.15f + 0.15f;
            for (int i = 0; i < 4; i++) {
                int y = rect.Bottom - 4 + i;
                float fade = (4 - i) / 4f;
                Color c = AccentFire * (alpha * glowStr * fade * 0.3f);
                sb.Draw(px, new Rectangle(rect.X + 2, y, rect.Width - 4, 1),
                    new Rectangle(0, 0, 1, 1), c);
            }
        }

        #endregion

        #region 颜色

        public Color GetWidgetTitleColor(float alpha) => TitleWarm * alpha;

        public Color GetWidgetTextColor(float alpha) => TextBody * (alpha * 0.85f);

        public Color GetWidgetAccentColor(float alpha) => AccentEmber * alpha;

        #endregion

        #region 度量

        public int? GetPreferredWidth() => null; //使用默认宽度

        public int? GetMinHeight() => 90;

        #endregion
    }
}
