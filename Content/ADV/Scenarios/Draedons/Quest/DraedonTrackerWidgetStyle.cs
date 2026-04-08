using CalamityOverhaul.Content.ADV.EntrustManager;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.Quest
{
    /// <summary>
    /// 嘉登委托在屏幕左侧追踪窗口中的自定义样式——
    /// 冷蓝科技渐变背景、扫描线动画、薄荷青脉冲边框、
    /// 数据流进度条
    /// </summary>
    internal class DraedonTrackerWidgetStyle : IEntrustTrackerWidgetStyle
    {
        #region 色板（与 DraedonManagerStyle 一致）

        private static readonly Color BgDeep = new(4, 8, 20);
        private static readonly Color BgMid = new(10, 18, 36);
        private static readonly Color PrimaryBright = new(140, 210, 255);
        private static readonly Color PrimaryMid = new(60, 150, 220);
        private static readonly Color PrimaryDim = new(30, 80, 140);
        private static readonly Color AccentCyan = new(80, 255, 220);
        private static readonly Color TextBody = new(180, 210, 230);
        private static readonly Color BarStart = new(30, 120, 200);
        private static readonly Color BarEnd = new(80, 255, 220);
        private static readonly Color CompletedGreen = new(60, 220, 140);

        #endregion

        private float pulseTimer;
        private float scanLineY;

        public void Update(Rectangle widgetRect, float slideProgress) {
            pulseTimer += 0.03f;
            if (pulseTimer > MathHelper.TwoPi) pulseTimer -= MathHelper.TwoPi;
            scanLineY += 0.8f;
            if (scanLineY > widgetRect.Height + 10) scanLineY = -4f;
        }

        public void Reset() {
            pulseTimer = 0f;
            scanLineY = 0f;
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
                Color c = Color.Lerp(BgDeep, BgMid, t * 0.5f) * (alpha * 0.95f);
                sb.Draw(px, new Rectangle(rect.X, y1, rect.Width, Math.Max(1, y2 - y1)),
                    new Rectangle(0, 0, 1, 1), c);
            }

            //扫描线效果（一条半透明亮线从上往下扫）
            int scanY = rect.Y + (int)scanLineY;
            if (scanY >= rect.Y && scanY < rect.Bottom - 2) {
                float scanAlpha = alpha * 0.12f;
                sb.Draw(px, new Rectangle(rect.X + 2, scanY, rect.Width - 4, 1),
                    new Rectangle(0, 0, 1, 1), AccentCyan * scanAlpha);
                sb.Draw(px, new Rectangle(rect.X + 2, scanY + 1, rect.Width - 4, 1),
                    new Rectangle(0, 0, 1, 1), PrimaryMid * (scanAlpha * 0.5f));
            }

            //脉冲叠加
            float pulse = MathF.Sin(pulseTimer * 2f) * 0.5f + 0.5f;
            Color pulseC = PrimaryDim * (alpha * 0.06f * pulse);
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), pulseC);

            //横向微弱网格线（科技感）
            int gridSpacing = 8;
            for (int y = rect.Y + gridSpacing; y < rect.Bottom; y += gridSpacing) {
                sb.Draw(px, new Rectangle(rect.X + 2, y, rect.Width - 4, 1),
                    new Rectangle(0, 0, 1, 1), PrimaryDim * (alpha * 0.04f));
            }
        }

        public void DrawWidgetFrame(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;

            float glow = MathF.Sin(pulseTimer) * 0.3f + 0.7f;
            Color edgeC = Color.Lerp(PrimaryMid, AccentCyan, glow * 0.4f) * (alpha * 0.75f);

            //顶部双线
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 2), new Rectangle(0, 0, 1, 1), edgeC);
            sb.Draw(px, new Rectangle(rect.X, rect.Y + 2, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), PrimaryDim * (alpha * 0.3f));

            //底部
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), edgeC * 0.4f);

            //左侧强调线（渐变衰减）
            int leftH = (int)(rect.Height * (0.6f + glow * 0.3f));
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 2, leftH),
                new Rectangle(0, 0, 1, 1), edgeC * 0.9f);
            if (rect.Height - leftH > 0) {
                sb.Draw(px, new Rectangle(rect.X, rect.Y + leftH, 2, rect.Height - leftH),
                    new Rectangle(0, 0, 1, 1), PrimaryDim * (alpha * 0.2f));
            }

            //右侧淡线
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height),
                new Rectangle(0, 0, 1, 1), PrimaryDim * (alpha * 0.25f));

            //角落科技装饰
            float ornAlpha = alpha * (0.4f + MathF.Sin(pulseTimer * 1.5f) * 0.25f);
            Color ornC = AccentCyan * ornAlpha;
            //左上角 L 形
            sb.Draw(px, new Rectangle(rect.X + 4, rect.Y + 4, 6, 1),
                new Rectangle(0, 0, 1, 1), ornC);
            sb.Draw(px, new Rectangle(rect.X + 4, rect.Y + 4, 1, 6),
                new Rectangle(0, 0, 1, 1), ornC);
            //右上角 L 形（镜像）
            sb.Draw(px, new Rectangle(rect.Right - 10, rect.Y + 4, 6, 1),
                new Rectangle(0, 0, 1, 1), ornC);
            sb.Draw(px, new Rectangle(rect.Right - 5, rect.Y + 4, 1, 6),
                new Rectangle(0, 0, 1, 1), ornC);
        }

        public void DrawWidgetHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha) {
            var px = VaultAsset.placeholder2.Value;

            //标题栏背景
            Color hdrBg = new Color(6, 12, 28) * (alpha * 0.7f);
            BaseManagerStyle.FillRect(sb, headerRect, hdrBg);

            //左侧数据点图标（小方块脉冲）
            float iconX = headerRect.X + 10f;
            float iconY = headerRect.Y + headerRect.Height / 2f;
            float iconPulse = MathF.Sin(pulseTimer * 2f + 0.5f) * 0.3f + 0.7f;
            sb.Draw(px, new Vector2(iconX, iconY), null, AccentCyan * (alpha * iconPulse),
                0f, new Vector2(0.5f), new Vector2(4f), SpriteEffects.None, 0f);

            //标题文字
            var font = FontAssets.MouseText.Value;
            float maxTitleW = headerRect.Width - 30f;
            if (font.MeasureString(title).X * 0.72f > maxTitleW) {
                while (title.Length > 3 && font.MeasureString(title + "...").X * 0.72f > maxTitleW)
                    title = title[..^1];
                title += "...";
            }

            float headerBlink = 0.85f + MathF.Sin(pulseTimer * 1.5f) * 0.15f;
            Utils.DrawBorderString(sb, title,
                new Vector2(headerRect.X + 22f, headerRect.Y + (headerRect.Height - 16f) / 2f),
                PrimaryBright * (alpha * headerBlink), 0.72f);

            //底部分隔线（渐变，左亮右暗）
            int divW = headerRect.Width - 8;
            int segs = 16;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                int x1 = headerRect.X + 4 + (int)(t * divW);
                int x2 = headerRect.X + 4 + (int)((i + 1f) / segs * divW);
                Color c = Color.Lerp(AccentCyan * (alpha * 0.5f), PrimaryDim * (alpha * 0.08f), t);
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
                    //数据流脉冲效果
                    float dataPulse = MathF.Sin(pulseTimer * 3f - t * MathHelper.Pi * 2f) * 0.2f + 0.8f;
                    sb.Draw(px, new Rectangle(sx1, fill.Y, Math.Max(1, sx2 - sx1), fill.Height),
                        new Rectangle(0, 0, 1, 1), c * (alpha * dataPulse));
                }

                //顶部发光线
                sb.Draw(px, new Rectangle(fill.X, fill.Y - 1, fill.Width, 1),
                    new Rectangle(0, 0, 1, 1), AccentCyan * (alpha * 0.35f));
            }

            //边框
            Color borderC = PrimaryMid * (alpha * 0.45f);
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
                    AccentCyan * (alpha * 0.65f), 0.5f);
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
                Color c = Color.Lerp(PrimaryMid * (alpha * 0.5f), PrimaryDim * (alpha * 0.05f), t);
                sb.Draw(px, new Rectangle((int)x, (int)start.Y, Math.Max(1, (int)(nexX - x)), 1),
                    new Rectangle(0, 0, 1, 1), c);
            }
        }

        public void DrawWidgetOverlay(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;

            //顶部微弱的青光辉弧
            float glowStr = MathF.Sin(pulseTimer * 0.8f) * 0.12f + 0.12f;
            for (int i = 0; i < 3; i++) {
                int y = rect.Y + 3 + i;
                float fade = (3 - i) / 3f;
                Color c = AccentCyan * (alpha * glowStr * fade * 0.25f);
                sb.Draw(px, new Rectangle(rect.X + 3, y, rect.Width - 6, 1),
                    new Rectangle(0, 0, 1, 1), c);
            }

            //底部微弱蓝光
            for (int i = 0; i < 3; i++) {
                int y = rect.Bottom - 3 + i;
                float fade = (3 - i) / 3f;
                Color c = PrimaryDim * (alpha * glowStr * fade * 0.2f);
                sb.Draw(px, new Rectangle(rect.X + 3, y, rect.Width - 6, 1),
                    new Rectangle(0, 0, 1, 1), c);
            }
        }

        #endregion

        #region 颜色

        public Color GetWidgetTitleColor(float alpha) => PrimaryBright * alpha;

        public Color GetWidgetTextColor(float alpha) => TextBody * (alpha * 0.85f);

        public Color GetWidgetAccentColor(float alpha) => AccentCyan * alpha;

        #endregion

        #region 度量

        public int? GetPreferredWidth() => null;

        public int? GetMinHeight() => 90;

        #endregion
    }
}
