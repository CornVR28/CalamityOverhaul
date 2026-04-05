using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.QuestManager
{
    /// <summary>
    /// 海洋风格追踪窗口样式——深海蓝渐变背景、波浪边框脉冲、
    /// 气泡装饰点缀，适用于比目鱼任务线的委托追踪面板
    /// </summary>
    internal class OceanTrackerWidgetStyle : IQuestTrackerWidgetStyle
    {
        #region 色板

        private static readonly Color DeepBg = new(4, 18, 30);
        private static readonly Color MidBg = new(10, 42, 60);
        private static readonly Color EdgeBright = new(30, 140, 190);
        private static readonly Color EdgePulse = new(90, 210, 255);
        private static readonly Color InnerGlow = new(120, 220, 255);
        private static readonly Color TitleColor = new(140, 230, 255);
        private static readonly Color TextColor = new(180, 230, 250);
        private static readonly Color AccentColor = new(70, 180, 230);
        private static readonly Color ProgressFillStart = new(30, 120, 180);
        private static readonly Color ProgressFillEnd = new(80, 200, 240);

        #endregion

        #region 动画状态

        private float pulseTimer;
        private float waveTimer;

        #endregion

        #region 生命周期

        public void Update(Rectangle widgetRect, float slideProgress) {
            pulseTimer += 0.03f;
            if (pulseTimer > MathHelper.TwoPi) pulseTimer -= MathHelper.TwoPi;
            waveTimer += 0.02f;
            if (waveTimer > MathHelper.TwoPi) waveTimer -= MathHelper.TwoPi;
        }

        public void Reset() {
            pulseTimer = 0f;
            waveTimer = 0f;
        }

        #endregion

        #region 面板绘制

        public void DrawWidgetBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //阴影
            Rectangle shadow = rect;
            shadow.Offset(3, 4);
            sb.Draw(px, shadow, new Rectangle(0, 0, 1, 1), Color.Black * (alpha * 0.5f));

            //多段纵向渐变背景，带波浪脉冲
            int segs = 12;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = rect.Y + (int)(t * rect.Height);
                int y2 = rect.Y + (int)(t2 * rect.Height);
                float osc = MathF.Sin(pulseTimer * 1.2f + t * 3f) * 0.5f + 0.5f;
                Color c = Color.Lerp(Color.Lerp(DeepBg, MidBg, osc), new Color(20, 90, 120), t * 0.55f);
                sb.Draw(px, new Rectangle(rect.X, y1, rect.Width, Math.Max(1, y2 - y1)),
                    new Rectangle(0, 0, 1, 1), c * alpha);
            }

            //呼吸光晕
            float pulse = MathF.Sin(Main.GlobalTimeWrappedHourly * 2f) * 0.5f + 0.5f;
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), new Color(30, 120, 150) * (alpha * 0.08f * pulse));
        }

        public void DrawWidgetFrame(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float pulse = MathF.Sin(Main.GlobalTimeWrappedHourly * 2f) * 0.5f + 0.5f;
            Color edge = Color.Lerp(EdgeBright, EdgePulse, pulse) * (alpha * 0.85f);

            //外框
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 2), new Rectangle(0, 0, 1, 1), edge);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), new Rectangle(0, 0, 1, 1), edge * 0.7f);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 2, rect.Height), new Rectangle(0, 0, 1, 1), edge * 0.85f);
            sb.Draw(px, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), new Rectangle(0, 0, 1, 1), edge * 0.85f);

            //内框光晕
            Rectangle inner = rect;
            inner.Inflate(-5, -5);
            Color innerC = InnerGlow * (alpha * 0.18f * pulse);
            sb.Draw(px, new Rectangle(inner.X, inner.Y, inner.Width, 1), new Rectangle(0, 0, 1, 1), innerC);
            sb.Draw(px, new Rectangle(inner.X, inner.Bottom - 1, inner.Width, 1), new Rectangle(0, 0, 1, 1), innerC * 0.65f);
            sb.Draw(px, new Rectangle(inner.X, inner.Y, 1, inner.Height), new Rectangle(0, 0, 1, 1), innerC * 0.85f);
            sb.Draw(px, new Rectangle(inner.Right - 1, inner.Y, 1, inner.Height), new Rectangle(0, 0, 1, 1), innerC * 0.85f);

            //底部波浪装饰线
            DrawWaveLine(sb, rect.X + 6, rect.Bottom - 6, rect.Width - 12, alpha * 0.4f);
        }

        public void DrawWidgetHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha) {
            Vector2 titlePos = new(headerRect.X + 8, headerRect.Y + (headerRect.Height - 16f) / 2f);

            //标题辉光
            Color glow = TitleColor * (alpha * 0.7f);
            for (int i = 0; i < 4; i++) {
                float ang = MathHelper.TwoPi * i / 4f;
                Vector2 off = ang.ToRotationVector2() * 1.2f;
                Utils.DrawBorderString(sb, title, titlePos + off, glow * 0.5f, 0.82f);
            }
            Utils.DrawBorderString(sb, title, titlePos, Color.White * alpha, 0.82f);
        }

        public void DrawWidgetProgress(SpriteBatch sb, Rectangle barRect, float progress,
            string progressText, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //进度条背景
            sb.Draw(px, barRect, new Rectangle(0, 0, 1, 1), new Color(6, 24, 40) * (alpha * 0.9f));

            //填充
            int fillW = (int)(barRect.Width * MathHelper.Clamp(progress, 0f, 1f));
            if (fillW > 2) {
                Rectangle fill = new(barRect.X + 1, barRect.Y + 1, fillW - 2, barRect.Height - 2);
                int segs = 8;
                for (int i = 0; i < segs; i++) {
                    float t = i / (float)segs;
                    float t2 = (i + 1) / (float)segs;
                    int sx1 = fill.X + (int)(t * fill.Width);
                    int sx2 = fill.X + (int)(t2 * fill.Width);
                    Color c = Color.Lerp(ProgressFillStart, ProgressFillEnd, t);
                    float p = MathF.Sin(pulseTimer + t * MathHelper.Pi) * 0.2f + 0.8f;
                    sb.Draw(px, new Rectangle(sx1, fill.Y, Math.Max(1, sx2 - sx1), fill.Height),
                        new Rectangle(0, 0, 1, 1), c * (alpha * p));
                }
            }

            //进度条边框
            Color border = AccentColor * (alpha * 0.6f);
            sb.Draw(px, new Rectangle(barRect.X, barRect.Y, barRect.Width, 1), new Rectangle(0, 0, 1, 1), border);
            sb.Draw(px, new Rectangle(barRect.X, barRect.Bottom - 1, barRect.Width, 1), new Rectangle(0, 0, 1, 1), border * 0.7f);
            sb.Draw(px, new Rectangle(barRect.X, barRect.Y, 1, barRect.Height), new Rectangle(0, 0, 1, 1), border * 0.85f);
            sb.Draw(px, new Rectangle(barRect.Right - 1, barRect.Y, 1, barRect.Height), new Rectangle(0, 0, 1, 1), border * 0.85f);

            //进度文本
            if (!string.IsNullOrEmpty(progressText)) {
                var font = FontAssets.MouseText.Value;
                Vector2 textSize = font.MeasureString(progressText) * 0.55f;
                Vector2 textPos = new(barRect.X + barRect.Width / 2f - textSize.X / 2f,
                    barRect.Y + barRect.Height / 2f - textSize.Y / 2f);
                Utils.DrawBorderString(sb, progressText, textPos, Color.White * alpha, 0.55f);
            }
        }

        public void DrawWidgetDivider(SpriteBatch sb, Vector2 start, Vector2 end, float alpha) {
            DrawGradientLine(sb, start, end, AccentColor * (alpha * 0.7f), AccentColor * (alpha * 0.05f), 1.2f);
        }

        public void DrawWidgetOverlay(SpriteBatch sb, Rectangle rect, float alpha) {
            //顶部微弱的波纹覆盖
            float wave = MathF.Sin(waveTimer * 2f) * 0.3f + 0.7f;
            Texture2D px = VaultAsset.placeholder2.Value;
            sb.Draw(px, new Rectangle(rect.X + 4, rect.Y + 2, rect.Width - 8, 1),
                new Rectangle(0, 0, 1, 1), InnerGlow * (alpha * 0.06f * wave));
        }

        #endregion

        #region 颜色

        public Color GetWidgetTitleColor(float alpha) => TitleColor * alpha;
        public Color GetWidgetTextColor(float alpha) => TextColor * alpha;
        public Color GetWidgetAccentColor(float alpha) => AccentColor * alpha;

        #endregion

        #region 度量

        public int? GetPreferredWidth() => 230;
        public int? GetMinHeight() => 100;

        #endregion

        #region 工具方法

        private void DrawWaveLine(SpriteBatch sb, int x, int y, int width, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            int segments = width / 4;
            for (int i = 0; i < segments; i++) {
                float t = i / (float)segments;
                int px2 = x + (int)(t * width);
                int py = y + (int)(MathF.Sin(waveTimer * 3f + t * MathHelper.TwoPi * 2f) * 2f);
                float fade = MathF.Sin(t * MathHelper.Pi) * 0.7f + 0.3f;
                sb.Draw(px, new Rectangle(px2, py, 4, 1), new Rectangle(0, 0, 1, 1),
                    AccentColor * (alpha * fade));
            }
        }

        private static void DrawGradientLine(SpriteBatch sb, Vector2 start, Vector2 end,
            Color startColor, Color endColor, float thickness) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            Vector2 edge = end - start;
            float length = edge.Length();
            if (length < 1f) return;
            edge.Normalize();
            float rotation = MathF.Atan2(edge.Y, edge.X);
            int segments = Math.Max(1, (int)(length / 11f));
            for (int i = 0; i < segments; i++) {
                float t = i / (float)segments;
                Vector2 segPos = start + edge * (length * t);
                float segLength = length / segments;
                Color c = Color.Lerp(startColor, endColor, t);
                sb.Draw(pixel, segPos, new Rectangle(0, 0, 1, 1), c, rotation,
                    new Vector2(0, 0.5f), new Vector2(segLength, thickness), SpriteEffects.None, 0);
            }
        }

        #endregion
    }
}
