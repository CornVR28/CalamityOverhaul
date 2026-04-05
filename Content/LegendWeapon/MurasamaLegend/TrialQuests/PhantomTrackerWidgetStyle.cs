using CalamityOverhaul.Content.ADV.QuestManager;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.MurasamaLegend.TrialQuests
{
    /// <summary>
    /// 鬼妖村正追踪窗口样式——MGSV iDroid战术终端风格：<br/>
    /// 军绿色渐变背景、扫描线纹理、雷达脉冲边框、战术进度条
    /// </summary>
    internal class PhantomTrackerWidgetStyle : IQuestTrackerWidgetStyle
    {
        #region 色板

        private static readonly Color BgDeep = new(6, 10, 8);
        private static readonly Color BgMid = new(14, 24, 16);
        private static readonly Color IDroidGreen = new(90, 195, 110);
        private static readonly Color IDroidDim = new(50, 110, 65);
        private static readonly Color IDroidBright = new(140, 255, 160);
        private static readonly Color ScanlineGreen = new(60, 155, 80);
        private static readonly Color BorderGreen = new(70, 160, 90);
        private static readonly Color BorderBright = new(110, 220, 130);
        private static readonly Color TitleGreen = new(155, 235, 165);
        private static readonly Color TextGreen = new(130, 200, 145);
        private static readonly Color ProgressFillStart = new(55, 140, 70);
        private static readonly Color ProgressFillEnd = new(120, 220, 100);

        #endregion

        private float scanTimer;
        private float pulseTimer;

        public void Update(Rectangle widgetRect, float slideProgress) {
            scanTimer += 0.03f;
            if (scanTimer > MathHelper.TwoPi * 4f) scanTimer -= MathHelper.TwoPi * 4f;
            pulseTimer += 0.025f;
            if (pulseTimer > MathHelper.TwoPi) pulseTimer -= MathHelper.TwoPi;
        }

        public void Reset() {
            scanTimer = 0f;
            pulseTimer = 0f;
        }

        public void DrawWidgetBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //阴影
            Rectangle shadow = rect;
            shadow.Offset(3, 4);
            sb.Draw(px, shadow, uv, Color.Black * (alpha * 0.55f));

            //多段渐变 + CRT扫描线
            int segs = 10;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = rect.Y + (int)(t * rect.Height);
                int y2 = rect.Y + (int)(t2 * rect.Height);
                float crt = MathF.Sin(t * MathHelper.Pi * 2.5f) * 0.06f;
                Color c = Color.Lerp(BgDeep, BgMid, t + crt) * alpha;
                sb.Draw(px, new Rectangle(rect.X, y1, rect.Width, Math.Max(1, y2 - y1)), uv, c);
            }

            //CRT扫描线
            for (int y = rect.Y; y < rect.Bottom; y += 3) {
                sb.Draw(px, new Rectangle(rect.X, y, rect.Width, 1), uv, ScanlineGreen * (alpha * 0.035f));
            }

            //呼吸脉冲
            float pulse = MathF.Sin(pulseTimer * 2f) * 0.5f + 0.5f;
            sb.Draw(px, rect, uv, IDroidDim * (alpha * 0.04f * pulse));
        }

        public void DrawWidgetFrame(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);
            float pulse = MathF.Sin(pulseTimer * 2f) * 0.3f + 0.7f;
            Color edge = Color.Lerp(BorderGreen, BorderBright, pulse) * (alpha * 0.75f);

            //外框（左上和右下角各留2px缺口，战术HUD风格）
            //top
            sb.Draw(px, new Rectangle(rect.X + 2, rect.Y, rect.Width - 4, 1), uv, edge);
            sb.Draw(px, new Rectangle(rect.X + 2, rect.Y + 1, rect.Width - 4, 1), uv, edge * 0.3f);
            //bottom
            sb.Draw(px, new Rectangle(rect.X + 2, rect.Bottom - 1, rect.Width - 4, 1), uv, edge * 0.6f);
            //left
            sb.Draw(px, new Rectangle(rect.X, rect.Y + 2, 1, rect.Height - 4), uv, edge * 0.85f);
            sb.Draw(px, new Rectangle(rect.X + 1, rect.Y + 2, 1, rect.Height - 4), uv, edge * 0.2f);
            //right
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y + 2, 1, rect.Height - 4), uv, edge * 0.85f);

            //角标菱形装饰（左上角）
            sb.Draw(px, new Vector2(rect.X + 5, rect.Y + 5), null, IDroidGreen * (alpha * 0.5f),
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(3f), SpriteEffects.None, 0f);

            //底部iDroid信号条纹（渐变虚线）
            int dashW = 4, gap = 2;
            for (int x = rect.X + 4; x < rect.Right - 4; x += dashW + gap) {
                float t = (float)(x - rect.X) / rect.Width;
                float fade = MathF.Sin(scanTimer * 1.2f + t * MathHelper.TwoPi) * 0.4f + 0.4f;
                int w = Math.Min(dashW, rect.Right - 4 - x);
                if (w > 0)
                    sb.Draw(px, new Rectangle(x, rect.Bottom - 4, w, 1), uv,
                        IDroidDim * (alpha * fade * 0.5f));
            }
        }

        public void DrawWidgetHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha) {
            //iDroid风格标题：正文 + 右侧等级标签
            Vector2 titlePos = new(headerRect.X + 10, headerRect.Y + (headerRect.Height - 16f) / 2f);

            //标题底色辉光
            Color glow = IDroidGreen * (alpha * 0.35f);
            for (int i = 0; i < 4; i++) {
                float ang = MathHelper.TwoPi * i / 4f;
                Vector2 off = ang.ToRotationVector2() * 1f;
                Utils.DrawBorderString(sb, title, titlePos + off, glow, 0.8f);
            }
            Utils.DrawBorderString(sb, title, titlePos, TitleGreen * alpha, 0.8f);
        }

        public void DrawWidgetProgress(SpriteBatch sb, Rectangle barRect, float progress,
            string progressText, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //进度条背景
            sb.Draw(px, barRect, uv, new Color(6, 14, 10) * (alpha * 0.9f));

            //填充（分段渐变 + 脉冲扫描效果）
            int fillW = (int)(barRect.Width * MathHelper.Clamp(progress, 0f, 1f));
            if (fillW > 2) {
                Rectangle fill = new(barRect.X + 1, barRect.Y + 1, fillW - 2, barRect.Height - 2);
                int segs = 10;
                for (int i = 0; i < segs; i++) {
                    float t = i / (float)segs;
                    float t2 = (i + 1) / (float)segs;
                    int sx1 = fill.X + (int)(t * fill.Width);
                    int sx2 = fill.X + (int)(t2 * fill.Width);
                    if (sx2 <= sx1) continue;
                    Color c = Color.Lerp(ProgressFillStart, ProgressFillEnd, t);
                    float p = MathF.Sin(scanTimer * 1.5f + t * MathHelper.Pi) * 0.2f + 0.8f;
                    sb.Draw(px, new Rectangle(sx1, fill.Y, sx2 - sx1, fill.Height), uv, c * (alpha * p));
                }

                //填充前端亮线
                if (fillW > 4) {
                    sb.Draw(px, new Rectangle(fill.Right - 1, fill.Y, 1, fill.Height),
                        uv, IDroidBright * (alpha * 0.5f));
                }
            }

            //边框
            Color border = IDroidDim * (alpha * 0.65f);
            sb.Draw(px, new Rectangle(barRect.X, barRect.Y, barRect.Width, 1), uv, border);
            sb.Draw(px, new Rectangle(barRect.X, barRect.Bottom - 1, barRect.Width, 1), uv, border * 0.7f);
            sb.Draw(px, new Rectangle(barRect.X, barRect.Y, 1, barRect.Height), uv, border * 0.85f);
            sb.Draw(px, new Rectangle(barRect.Right - 1, barRect.Y, 1, barRect.Height), uv, border * 0.85f);

            //进度文本
            if (!string.IsNullOrEmpty(progressText)) {
                var font = FontAssets.MouseText.Value;
                Vector2 textSize = font.MeasureString(progressText) * 0.55f;
                Vector2 textPos = new(barRect.X + barRect.Width / 2f - textSize.X / 2f,
                    barRect.Y + barRect.Height / 2f - textSize.Y / 2f);
                Utils.DrawBorderString(sb, progressText, textPos, IDroidBright * alpha, 0.55f);
            }
        }

        public void DrawWidgetDivider(SpriteBatch sb, Vector2 start, Vector2 end, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            //虚线分隔符（战术HUD风格）
            float length = (end - start).Length();
            if (length < 1f) return;
            Vector2 dir = Vector2.Normalize(end - start);
            float rotation = MathF.Atan2(dir.Y, dir.X);
            int dashLen = 5, gapLen = 3;
            float cursor = 0f;
            while (cursor < length) {
                float segLen = Math.Min(dashLen, length - cursor);
                float t = cursor / length;
                Color c = Color.Lerp(IDroidDim * (alpha * 0.6f), IDroidDim * (alpha * 0.1f), t);
                sb.Draw(px, start + dir * cursor, new Rectangle(0, 0, 1, 1), c, rotation,
                    new Vector2(0, 0.5f), new Vector2(segLen, 1f), SpriteEffects.None, 0f);
                cursor += dashLen + gapLen;
            }
        }

        public void DrawWidgetOverlay(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            //iDroid信号闪烁覆盖层
            float flicker = MathF.Sin(scanTimer * 3.2f);
            if (flicker > 0.85f) {
                float intensity = (flicker - 0.85f) / 0.15f * 0.03f;
                sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), IDroidGreen * (alpha * intensity));
            }
        }

        public Color GetWidgetTitleColor(float alpha) => TitleGreen * alpha;
        public Color GetWidgetTextColor(float alpha) => TextGreen * alpha;
        public Color GetWidgetAccentColor(float alpha) => IDroidGreen * alpha;

        public int? GetPreferredWidth() => 235;
        public int? GetMinHeight() => 100;
    }
}
