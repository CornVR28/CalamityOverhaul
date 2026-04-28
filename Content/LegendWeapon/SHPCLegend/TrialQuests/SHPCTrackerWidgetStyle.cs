using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.EntrustManager;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.TrialQuests
{
    /// <summary>
    /// SHPC追踪窗口样式——赛博朋克2077 HUD终端风格：<br/>
    /// CyberPanel着色器驱动背景（蜂窝边框/CRT扫描线/故障位移/数据流），
    /// 非对称角标框、霓虹蓝进度条、数据流分隔线
    /// </summary>
    internal class SHPCTrackerWidgetStyle : IEntrustTrackerWidgetStyle
    {
        #region 色板

        private static readonly Color TacDark = new(8, 5, 18);
        private static readonly Color TacMid = new(16, 10, 34);
        private static readonly Color NeonBlue = new(60, 120, 255);
        private static readonly Color NeonBlueDim = new(40, 60, 180);
        private static readonly Color NeonBright = new(140, 200, 255);
        private static readonly Color ScanBlue = new(30, 60, 180);
        private static readonly Color BorderBlue = new(50, 100, 220);
        private static readonly Color TitleBlue = new(180, 210, 255);
        private static readonly Color TextBlue = new(140, 170, 230);
        private static readonly Color BarFillDark = new(35, 70, 190);
        private static readonly Color BarFillBright = new(90, 160, 255);

        #endregion

        private const int EdgePad = 8;

        private float sweepTimer;
        private float pulse;

        public void Update(Rectangle widgetRect, float slideProgress) {
            sweepTimer += 0.004f;
            pulse += 0.025f;
            if (sweepTimer > 100f) sweepTimer -= 100f;
            if (pulse > MathHelper.TwoPi) pulse -= MathHelper.TwoPi;
        }

        public void Reset() {
            sweepTimer = 0f;
            pulse = 0f;
        }

        public void DrawWidgetBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //偏移软投影
            sb.Draw(px, new Rectangle(rect.X + 2, rect.Y + 3, rect.Width, rect.Height),
                uv, Color.Black * (alpha * 0.45f));

            if (EffectLoader.CyberPanel?.Value != null) {
                Effect effect = EffectLoader.CyberPanel.Value;

                Rectangle extRect = rect;
                extRect.Inflate(EdgePad, EdgePad);

                effect.Parameters["uTime"]?.SetValue(sweepTimer);
                effect.Parameters["uAlpha"]?.SetValue(alpha * 0.95f);
                effect.Parameters["uResolution"]?.SetValue(new Vector2(extRect.Width, extRect.Height));
                effect.Parameters["uEdgePad"]?.SetValue((float)EdgePad);

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
                DrawFallbackBackground(sb, px, rect, alpha);
            }
        }

        /// <summary>降级背景：渐变 + 扫描线 + 六角点阵 + 扫掠光带 + 暗角</summary>
        private void DrawFallbackBackground(SpriteBatch sb, Texture2D px, Rectangle rect, float alpha) {
            var uv = new Rectangle(0, 0, 1, 1);

            //纵向渐变（8段平滑，深紫色调）
            int segs = 8;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = rect.Y + (int)(t * rect.Height);
                int y2 = rect.Y + (int)(t2 * rect.Height);
                Color c = Color.Lerp(TacDark, TacMid, t * 0.7f) * (alpha * 0.95f);
                sb.Draw(px, new Rectangle(rect.X, y1, rect.Width, Math.Max(1, y2 - y1)), uv, c);
            }

            //CRT水平扫描线
            for (int y = rect.Y; y < rect.Bottom; y += 3)
                sb.Draw(px, new Rectangle(rect.X, y, rect.Width, 1), uv, ScanBlue * (alpha * 0.04f));

            //六角点阵（错行排列）
            int dotSpX = 16, dotSpY = 14;
            Color dotColor = new Color(30, 18, 65) * (alpha * 0.15f);
            for (int row = 0; row < rect.Height / dotSpY; row++) {
                int dy = rect.Y + row * dotSpY + 5;
                if (dy >= rect.Bottom - 4) continue;
                int offX = (row % 2 == 0) ? 0 : dotSpX / 2;
                for (int col = 0; col < rect.Width / dotSpX + 1; col++) {
                    int dx = rect.X + col * dotSpX + offX + 3;
                    if (dx >= rect.Right - 3) continue;
                    sb.Draw(px, new Rectangle(dx, dy, 1, 1), uv, dotColor);
                }
            }

            //全息扫掠光带
            float scanY = rect.Y + (sweepTimer * 0.1f % 1f) * rect.Height;
            for (int dy = -4; dy <= 4; dy++) {
                int py = (int)scanY + dy;
                if (py < rect.Y || py >= rect.Bottom) continue;
                float fade = 1f - MathF.Abs(dy) / 5f;
                sb.Draw(px, new Rectangle(rect.X + 3, py, rect.Width - 6, 1),
                    uv, NeonBlueDim * (alpha * 0.10f * fade * fade));
            }

            //暗角（左右两侧渐暗）
            int vigW = 16;
            for (int v = 0; v < vigW; v += 4) {
                float vFade = (1f - (float)v / vigW) * 0.09f;
                Color vColor = new Color(4, 2, 10) * (alpha * vFade);
                sb.Draw(px, new Rectangle(rect.X + v, rect.Y, 2, rect.Height), uv, vColor);
                sb.Draw(px, new Rectangle(rect.Right - v - 2, rect.Y, 2, rect.Height), uv, vColor);
            }
        }

        public void DrawWidgetFrame(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);
            float p = MathF.Sin(pulse * 2f) * 0.25f + 0.75f;
            Color edge = BorderBlue * (alpha * p);
            int cornerL = 12;

            //非对称角标框（四角L形开放式）
            //左上 ┌
            sb.Draw(px, new Rectangle(rect.X, rect.Y, cornerL, 1), uv, edge);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 1, cornerL), uv, edge);
            //右上 ┐
            sb.Draw(px, new Rectangle(rect.Right - cornerL, rect.Y, cornerL, 1), uv, edge * 0.7f);
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, cornerL), uv, edge * 0.7f);
            //左下 └
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, cornerL, 1), uv, edge * 0.5f);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - cornerL, 1, cornerL), uv, edge * 0.5f);
            //右下 ┘
            sb.Draw(px, new Rectangle(rect.Right - cornerL, rect.Bottom - 1, cornerL, 1), uv, edge * 0.35f);
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Bottom - cornerL, 1, cornerL), uv, edge * 0.35f);

            //左侧状态色带（霓虹蓝呼吸脉冲）
            float barPulse = MathF.Sin(pulse * 2.5f) * 0.2f + 0.8f;
            sb.Draw(px, new Rectangle(rect.X + 3, rect.Y + cornerL + 2, 2, rect.Height - cornerL * 2 - 4),
                uv, NeonBlue * (alpha * 0.55f * barPulse));

            //顶部中段虚线（角标间隔填充）
            for (int x = rect.X + cornerL + 4; x < rect.Right - cornerL - 4; x += 7) {
                int w = Math.Min(4, rect.Right - cornerL - 4 - x);
                if (w > 0) sb.Draw(px, new Rectangle(x, rect.Y, w, 1), uv, edge * 0.18f);
            }
            //底部信号虚线
            for (int x = rect.X + cornerL + 4; x < rect.Right - cornerL - 4; x += 6) {
                int w = Math.Min(3, rect.Right - cornerL - 4 - x);
                float t = (float)(x - rect.X) / rect.Width;
                float fade = MathF.Sin(pulse * 1.3f + t * MathHelper.TwoPi) * 0.3f + 0.25f;
                if (w > 0) sb.Draw(px, new Rectangle(x, rect.Bottom - 3, w, 1),
                    uv, NeonBlueDim * (alpha * fade));
            }
        }

        public void DrawWidgetHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //标题左侧竖向霓虹色带
            float p = MathF.Sin(pulse * 2f) * 0.2f + 0.8f;
            sb.Draw(px, new Rectangle(headerRect.X + 8, headerRect.Y + 4, 2, headerRect.Height - 6),
                uv, NeonBlue * (alpha * 0.6f * p));

            //标题文字（投影+正文，CP2077终端字体感）
            Vector2 titlePos = new(headerRect.X + 15, headerRect.Y + (headerRect.Height - 14f) / 2f);
            Utils.DrawBorderString(sb, title, titlePos + new Vector2(0, 1),
                TacDark * (alpha * 0.45f), 0.76f);
            Utils.DrawBorderString(sb, title, titlePos, TitleBlue * alpha, 0.76f);

            //标题下方分隔：短实线 + 间距 + 长虚线
            int sepY = headerRect.Bottom - 1;
            int solidW = 18;
            sb.Draw(px, new Rectangle(headerRect.X + 10, sepY, solidW, 1),
                uv, NeonBlue * (alpha * 0.48f));
            for (int x = headerRect.X + 10 + solidW + 5; x < headerRect.Right - 10; x += 6) {
                int w = Math.Min(3, headerRect.Right - 10 - x);
                if (w > 0) sb.Draw(px, new Rectangle(x, sepY, w, 1), uv, NeonBlueDim * (alpha * 0.28f));
            }
        }

        public void DrawWidgetProgress(SpriteBatch sb, Rectangle barRect, float progress,
            string progressText, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //进度条背景
            sb.Draw(px, new Rectangle(barRect.X + 1, barRect.Y, barRect.Width - 2, barRect.Height),
                uv, TacDark * (alpha * 0.92f));

            //分段式填充（CP2077风格，每段间留1px缝隙）
            int fillW = (int)((barRect.Width - 2) * MathHelper.Clamp(progress, 0f, 1f));
            if (fillW > 1) {
                int segW = 5, gap = 1;
                for (int sx = 0; sx < fillW; sx += segW + gap) {
                    int sw = Math.Min(segW, fillW - sx);
                    float t = (float)sx / (barRect.Width - 2);
                    Color c = Color.Lerp(BarFillDark, BarFillBright, t);
                    float p = MathF.Sin(pulse * 1.5f + t * MathHelper.Pi) * 0.15f + 0.85f;
                    sb.Draw(px, new Rectangle(barRect.X + 1 + sx, barRect.Y + 1, sw, barRect.Height - 2),
                        uv, c * (alpha * p));
                }
                //前端亮缘
                if (fillW > 3) {
                    sb.Draw(px, new Rectangle(barRect.X + fillW, barRect.Y, 1, barRect.Height),
                        uv, NeonBright * (alpha * 0.4f));
                }
            }

            //上下薄线
            sb.Draw(px, new Rectangle(barRect.X + 1, barRect.Y, barRect.Width - 2, 1),
                uv, BorderBlue * (alpha * 0.38f));
            sb.Draw(px, new Rectangle(barRect.X + 1, barRect.Bottom - 1, barRect.Width - 2, 1),
                uv, BorderBlue * (alpha * 0.22f));

            if (!string.IsNullOrEmpty(progressText)) {
                var font = FontAssets.MouseText.Value;
                Vector2 sz = font.MeasureString(progressText) * 0.5f;
                Utils.DrawBorderString(sb, progressText,
                    new Vector2(barRect.X + barRect.Width / 2f - sz.X / 2f,
                        barRect.Y + barRect.Height / 2f - sz.Y / 2f),
                    NeonBright * alpha, 0.5f);
            }
        }

        public void DrawWidgetDivider(SpriteBatch sb, Vector2 start, Vector2 end, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            float len = (end - start).Length();
            if (len < 1f) return;
            Vector2 dir = Vector2.Normalize(end - start);
            float rot = MathF.Atan2(dir.Y, dir.X);
            for (float c = 0; c < len; c += 7f) {
                float segLen = Math.Min(4f, len - c);
                float t = c / len;
                float fade = MathF.Sin(t * MathHelper.Pi) * 0.45f;
                Color col = Color.Lerp(NeonBlue, NeonBlueDim, t * 0.6f) * (alpha * fade);
                sb.Draw(px, start + dir * c, new Rectangle(0, 0, 1, 1),
                    col, rot, new Vector2(0, 0.5f), new Vector2(segLen, 1f), SpriteEffects.None, 0f);
            }
        }

        public void DrawWidgetOverlay(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            //偶发CRT故障闪烁
            float glitch = MathF.Sin(pulse * 3.7f);
            if (glitch > 0.93f) {
                float intensity = (glitch - 0.93f) / 0.07f * 0.04f;
                sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), NeonBlue * (alpha * intensity));
            }
        }

        public Color GetWidgetTitleColor(float alpha) => TitleBlue * alpha;
        public Color GetWidgetTextColor(float alpha) => TextBlue * alpha;
        public Color GetWidgetAccentColor(float alpha) => NeonBlue * alpha;

        public int? GetPreferredWidth() => 240;
        public int? GetMinHeight() => 80;
        public int? GetIdleCompactHeight(EntrustEntryData entry) {
            if (entry.Progress <= 0f && entry.Status != QuestEntryStatus.Completed)
                return 46;
            return null;
        }
    }
}
