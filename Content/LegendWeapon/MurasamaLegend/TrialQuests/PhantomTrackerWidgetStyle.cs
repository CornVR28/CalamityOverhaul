using CalamityOverhaul.Content.ADV.QuestManager;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.MurasamaLegend.TrialQuests
{
    /// <summary>
    /// 鬼妖村正追踪窗口样式——MGSV iDroid战术终端HUD风格：<br/>
    /// 非对称角标框、雷达扫描线、菱形标记、CRT纵条纹，
    /// 以CP2077式层叠HUD取代传统矩形盒
    /// </summary>
    internal class PhantomTrackerWidgetStyle : IQuestTrackerWidgetStyle
    {
        #region 色板

        private static readonly Color TacDark = new(5, 9, 7);
        private static readonly Color TacMid = new(12, 22, 15);
        private static readonly Color IDroidGreen = new(90, 195, 110);
        private static readonly Color IDroidDim = new(45, 105, 60);
        private static readonly Color IDroidBright = new(140, 255, 160);
        private static readonly Color ScanGreen = new(60, 155, 80);
        private static readonly Color BorderGreen = new(70, 160, 90);
        private static readonly Color TitleGreen = new(155, 235, 165);
        private static readonly Color TextGreen = new(130, 200, 145);
        private static readonly Color DiamondOrange = new(210, 145, 45);
        private static readonly Color BarFillDark = new(40, 120, 60);
        private static readonly Color BarFillBright = new(100, 210, 90);

        #endregion

        private float scan;
        private float pulse;

        public void Update(Rectangle widgetRect, float slideProgress) {
            scan += 0.032f;
            pulse += 0.025f;
            if (scan > MathHelper.TwoPi * 4f) scan -= MathHelper.TwoPi * 4f;
            if (pulse > MathHelper.TwoPi) pulse -= MathHelper.TwoPi;
        }

        public void Reset() { scan = pulse = 0f; }

        public void DrawWidgetBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //偏移软投影
            sb.Draw(px, new Rectangle(rect.X + 2, rect.Y + 3, rect.Width, rect.Height),
                uv, Color.Black * (alpha * 0.45f));

            //纵向CRT渐变（非均匀亮度带）
            int segs = 12;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = rect.Y + (int)(t * rect.Height);
                int y2 = rect.Y + (int)(t2 * rect.Height);
                if (y2 <= y1) continue;
                float crt = MathF.Sin(t * MathHelper.Pi * 2.8f) * 0.07f;
                Color c = Color.Lerp(TacDark, TacMid, t * 0.6f + crt) * (alpha * 0.93f);
                sb.Draw(px, new Rectangle(rect.X, y1, rect.Width, y2 - y1), uv, c);
            }

            //CRT水平扫描线
            for (int y = rect.Y; y < rect.Bottom; y += 3) {
                sb.Draw(px, new Rectangle(rect.X, y, rect.Width, 1), uv, ScanGreen * (alpha * 0.03f));
            }

            //雷达横扫光带（从左到右）
            float radarT = (scan * 0.25f) % 1f;
            int radarX = rect.X + (int)(radarT * rect.Width);
            int radarW = (int)(rect.Width * 0.12f);
            for (int dx = 0; dx < radarW; dx++) {
                int x = radarX + dx;
                if (x < rect.X || x >= rect.Right) continue;
                float fade = 1f - (float)dx / radarW;
                fade *= fade;
                sb.Draw(px, new Rectangle(x, rect.Y, 1, rect.Height),
                    uv, IDroidGreen * (alpha * fade * 0.06f));
            }

            //低频呼吸脉冲
            float breath = MathF.Sin(pulse * 1.8f) * 0.5f + 0.5f;
            sb.Draw(px, rect, uv, IDroidDim * (alpha * 0.03f * breath));
        }

        public void DrawWidgetFrame(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);
            float p = MathF.Sin(pulse * 2f) * 0.3f + 0.7f;
            Color edge = BorderGreen * (alpha * p);

            //非对称HUD角标框——四角开放，只有角标线段
            int cornerL = 14; //角标线段长度

            //左上角标 ┌
            sb.Draw(px, new Rectangle(rect.X, rect.Y, cornerL, 1), uv, edge);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 1, cornerL), uv, edge);
            //右上角标 ┐
            sb.Draw(px, new Rectangle(rect.Right - cornerL, rect.Y, cornerL, 1), uv, edge * 0.7f);
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, cornerL), uv, edge * 0.7f);
            //左下角标 └
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, cornerL, 1), uv, edge * 0.5f);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - cornerL, 1, cornerL), uv, edge * 0.5f);
            //右下角标 ┘
            sb.Draw(px, new Rectangle(rect.Right - cornerL, rect.Bottom - 1, cornerL, 1), uv, edge * 0.35f);
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Bottom - cornerL, 1, cornerL), uv, edge * 0.35f);

            //左侧状态色带（实线，较粗，MGSV任务进度特征）
            float barPulse = MathF.Sin(pulse * 2.5f) * 0.2f + 0.8f;
            sb.Draw(px, new Rectangle(rect.X + 3, rect.Y + cornerL + 2, 2, rect.Height - cornerL * 2 - 4),
                uv, IDroidGreen * (alpha * 0.6f * barPulse));

            //左上角Diamond Dogs菱形标记
            sb.Draw(px, new Vector2(rect.X + 7, rect.Y + 7), null,
                DiamondOrange * (alpha * 0.55f * p), MathHelper.PiOver4,
                new Vector2(0.5f), new Vector2(3.5f), SpriteEffects.None, 0f);

            //顶部中段虚线（非封闭，角标间的间隔）
            for (int x = rect.X + cornerL + 4; x < rect.Right - cornerL - 4; x += 7) {
                int w = Math.Min(4, rect.Right - cornerL - 4 - x);
                if (w > 0) sb.Draw(px, new Rectangle(x, rect.Y, w, 1), uv, edge * 0.2f);
            }

            //底部信号虚线条纹
            for (int x = rect.X + cornerL + 4; x < rect.Right - cornerL - 4; x += 6) {
                int w = Math.Min(3, rect.Right - cornerL - 4 - x);
                float t = (float)(x - rect.X) / rect.Width;
                float fade = MathF.Sin(scan * 1.3f + t * MathHelper.TwoPi) * 0.35f + 0.25f;
                if (w > 0) sb.Draw(px, new Rectangle(x, rect.Bottom - 3, w, 1), uv,
                    IDroidDim * (alpha * fade));
            }
        }

        public void DrawWidgetHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //标题左侧竖向战术色带
            float p = MathF.Sin(pulse * 2f) * 0.2f + 0.8f;
            sb.Draw(px, new Rectangle(headerRect.X + 8, headerRect.Y + 4, 2, headerRect.Height - 6),
                uv, IDroidGreen * (alpha * 0.65f * p));

            //标题文字（iDroid清晰字体风格）
            Vector2 titlePos = new(headerRect.X + 15, headerRect.Y + (headerRect.Height - 14f) / 2f);
            Utils.DrawBorderString(sb, title, titlePos + new Vector2(0, 1),
                TacDark * (alpha * 0.5f), 0.76f);
            Utils.DrawBorderString(sb, title, titlePos, TitleGreen * alpha, 0.76f);

            //标题下方分隔——短实线+间距+长虚线（战术HUD分段感）
            int sepY = headerRect.Bottom - 1;
            int solidW = 20;
            sb.Draw(px, new Rectangle(headerRect.X + 10, sepY, solidW, 1),
                uv, IDroidGreen * (alpha * 0.5f));
            for (int x = headerRect.X + 10 + solidW + 6; x < headerRect.Right - 10; x += 6) {
                int w = Math.Min(3, headerRect.Right - 10 - x);
                if (w > 0) sb.Draw(px, new Rectangle(x, sepY, w, 1), uv, IDroidDim * (alpha * 0.3f));
            }
        }

        public void DrawWidgetProgress(SpriteBatch sb, Rectangle barRect, float progress,
            string progressText, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //进度条背景
            sb.Draw(px, new Rectangle(barRect.X + 1, barRect.Y, barRect.Width - 2, barRect.Height),
                uv, TacDark * (alpha * 0.9f));

            //分段式填充（MGSV风格——每段之间留1px缝隙）
            int fillW = (int)((barRect.Width - 2) * MathHelper.Clamp(progress, 0f, 1f));
            if (fillW > 1) {
                int segW = 5, gap = 1;
                for (int sx = 0; sx < fillW; sx += segW + gap) {
                    int sw = Math.Min(segW, fillW - sx);
                    float t = (float)sx / (barRect.Width - 2);
                    Color c = Color.Lerp(BarFillDark, BarFillBright, t);
                    float p = MathF.Sin(scan * 1.5f + t * MathHelper.Pi) * 0.15f + 0.85f;
                    sb.Draw(px, new Rectangle(barRect.X + 1 + sx, barRect.Y + 1, sw, barRect.Height - 2),
                        uv, c * (alpha * p));
                }
                //前端亮缘
                if (fillW > 3) {
                    sb.Draw(px, new Rectangle(barRect.X + fillW, barRect.Y, 1, barRect.Height),
                        uv, IDroidBright * (alpha * 0.45f));
                }
            }

            //上下薄线
            sb.Draw(px, new Rectangle(barRect.X + 1, barRect.Y, barRect.Width - 2, 1),
                uv, BorderGreen * (alpha * 0.4f));
            sb.Draw(px, new Rectangle(barRect.X + 1, barRect.Bottom - 1, barRect.Width - 2, 1),
                uv, BorderGreen * (alpha * 0.25f));

            if (!string.IsNullOrEmpty(progressText)) {
                var font = FontAssets.MouseText.Value;
                Vector2 sz = font.MeasureString(progressText) * 0.5f;
                Utils.DrawBorderString(sb, progressText,
                    new Vector2(barRect.X + barRect.Width / 2f - sz.X / 2f,
                        barRect.Y + barRect.Height / 2f - sz.Y / 2f),
                    IDroidBright * alpha, 0.5f);
            }
        }

        public void DrawWidgetDivider(SpriteBatch sb, Vector2 start, Vector2 end, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            //战术虚线
            float len = (end - start).Length();
            if (len < 1f) return;
            Vector2 dir = Vector2.Normalize(end - start);
            float rot = MathF.Atan2(dir.Y, dir.X);
            for (float c = 0; c < len; c += 7f) {
                float segLen = Math.Min(4f, len - c);
                float t = c / len;
                float fade = MathF.Sin(t * MathHelper.Pi) * 0.5f;
                sb.Draw(px, start + dir * c, new Rectangle(0, 0, 1, 1),
                    IDroidDim * (alpha * fade), rot, new Vector2(0, 0.5f),
                    new Vector2(segLen, 1f), SpriteEffects.None, 0f);
            }
        }

        public void DrawWidgetOverlay(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            //偶发CRT故障闪烁
            float glitch = MathF.Sin(scan * 3.7f);
            if (glitch > 0.93f) {
                float intensity = (glitch - 0.93f) / 0.07f * 0.04f;
                sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), IDroidGreen * (alpha * intensity));
            }
        }

        public Color GetWidgetTitleColor(float alpha) => TitleGreen * alpha;
        public Color GetWidgetTextColor(float alpha) => TextGreen * alpha;
        public Color GetWidgetAccentColor(float alpha) => IDroidGreen * alpha;

        public int? GetPreferredWidth() => 235;
        public int? GetMinHeight() => 80;
        public int? GetIdleCompactHeight(QuestEntryData entry) {
            if (entry.Progress <= 0f && entry.Status != QuestEntryStatus.Completed)
                return 38;
            return null;
        }
    }
}
