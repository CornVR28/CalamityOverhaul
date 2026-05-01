using CalamityOverhaul.Content.ADV.EntrustManager;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.MurasamaLegend.TrialQuests
{
    /// <summary>
    /// 鬼妖村正追踪窗口样式——MGR:R(合金装备崛起复仇)赤红血狂HUD:<br/>
    /// 黑曜+猩红着色器底图 / 利刃斩击扫光 / 数据腐蚀 /
    /// 锐角金属刀刃边框 / 充能式分段进度条
    /// </summary>
    internal class PhantomTrackerWidgetStyle : IEntrustTrackerWidgetStyle
    {
        #region 色板

        private static readonly Color BgVoid = new(7, 3, 4);
        private static readonly Color BgDeep = new(20, 7, 10);

        private static readonly Color CrimsonDeep = new(140, 18, 26);
        private static readonly Color CrimsonMid = new(195, 35, 45);
        private static readonly Color CrimsonBright = new(240, 70, 80);
        private static readonly Color BladeMode = new(255, 60, 70);
        private static readonly Color BloodFlash = new(255, 130, 110);

        //文字
        private static readonly Color TitleBlade = new(245, 220, 220);
        private static readonly Color TextBlade = new(220, 200, 200);

        //进度条
        private static readonly Color BarFillDark = new(120, 14, 22);
        private static readonly Color BarFillBright = new(245, 75, 80);

        #endregion

        private float scan;
        private float pulse;
        private float shaderTime;

        public void Update(Rectangle widgetRect, float slideProgress) {
            scan += 0.034f;
            pulse += 0.026f;
            shaderTime += 0.016f;
            if (scan > MathHelper.TwoPi * 4f) scan -= MathHelper.TwoPi * 4f;
            if (pulse > MathHelper.TwoPi) pulse -= MathHelper.TwoPi;
            if (shaderTime > 10000f) shaderTime -= 10000f;
        }

        public void Reset() { scan = pulse = 0f; shaderTime = 0f; }

        public void DrawWidgetBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //偏移软投影
            sb.Draw(px, new Rectangle(rect.X + 3, rect.Y + 4, rect.Width, rect.Height),
                uv, Color.Black * (alpha * 0.55f));

            float pulse01 = MathF.Sin(pulse * 1.8f) * 0.5f + 0.5f;

            //GPU着色器路径
            if (MurasamaPhantomShaderPanel.Available) {
                MurasamaPhantomShaderPanel.Draw(sb, rect, alpha * 0.95f, pulse01,
                    shaderTime, 10, 1f, 0.85f, BladeMode);
            }
            else {
                DrawFallbackBackground(sb, rect, alpha);
            }
        }

        //无shader环境下的CPU降级背景
        private void DrawFallbackBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            int segs = 14;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = rect.Y + (int)(t * rect.Height);
                int y2 = rect.Y + (int)(t2 * rect.Height);
                if (y2 <= y1) continue;
                float crt = MathF.Sin(t * MathHelper.Pi * 2.8f) * 0.07f;
                Color c = Color.Lerp(BgVoid, BgDeep, t * 0.6f + crt) * (alpha * 0.93f);
                sb.Draw(px, new Rectangle(rect.X, y1, rect.Width, y2 - y1), uv, c);
            }

            for (int y = rect.Y; y < rect.Bottom; y += 3) {
                sb.Draw(px, new Rectangle(rect.X, y, rect.Width, 1), uv, CrimsonMid * (alpha * 0.04f));
            }

            //斩击扫光
            float bladeT = (scan * 0.20f) % 1f;
            int bladeX = rect.X + (int)(bladeT * (rect.Width + 60)) - 30;
            int bladeW = (int)(rect.Width * 0.18f);
            for (int dx = 0; dx < bladeW; dx++) {
                int x = bladeX + dx;
                if (x < rect.X || x >= rect.Right) continue;
                float fade = 1f - (float)dx / bladeW;
                fade *= fade;
                sb.Draw(px, new Rectangle(x, rect.Y, 1, rect.Height),
                    uv, CrimsonBright * (alpha * fade * 0.10f));
            }

            float breath = MathF.Sin(pulse * 1.8f) * 0.5f + 0.5f;
            sb.Draw(px, rect, uv, CrimsonDeep * (alpha * 0.04f * breath));
        }

        public void DrawWidgetFrame(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);
            float p = MathF.Sin(pulse * 2f) * 0.3f + 0.7f;
            Color edge = CrimsonMid * (alpha * p);
            Color edgeBright = BloodFlash * (alpha * p * 0.85f);

            //L型角标(更长更锐利,刀刃风格)
            int cornerL = 18;

            //左上 ┌
            sb.Draw(px, new Rectangle(rect.X, rect.Y, cornerL, 2), uv, edge);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 2, cornerL), uv, edge);
            //刀尖外延伸高光
            sb.Draw(px, new Rectangle(rect.X - 2, rect.Y, 5, 1), uv, edgeBright);
            sb.Draw(px, new Rectangle(rect.X, rect.Y - 2, 1, 5), uv, edgeBright);

            //右上 ┐(略弱)
            sb.Draw(px, new Rectangle(rect.Right - cornerL, rect.Y, cornerL, 2), uv, edge * 0.85f);
            sb.Draw(px, new Rectangle(rect.Right - 2, rect.Y, 2, cornerL), uv, edge * 0.85f);
            sb.Draw(px, new Rectangle(rect.Right - 3, rect.Y, 5, 1), uv, edgeBright * 0.7f);

            //左下 └
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 2, cornerL, 2), uv, edge * 0.7f);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - cornerL, 2, cornerL), uv, edge * 0.7f);
            sb.Draw(px, new Rectangle(rect.X - 2, rect.Bottom - 1, 5, 1), uv, edgeBright * 0.55f);

            //右下 ┘(最弱)
            sb.Draw(px, new Rectangle(rect.Right - cornerL, rect.Bottom - 2, cornerL, 2), uv, edge * 0.55f);
            sb.Draw(px, new Rectangle(rect.Right - 2, rect.Bottom - cornerL, 2, cornerL), uv, edge * 0.55f);

            //左侧战术粗带——MGR:R冷酷红条
            float barPulse = MathF.Sin(pulse * 2.5f) * 0.20f + 0.80f;
            sb.Draw(px, new Rectangle(rect.X + 4, rect.Y + cornerL + 2, 3, rect.Height - cornerL * 2 - 4),
                uv, BladeMode * (alpha * 0.70f * barPulse));
            //粗带高光
            sb.Draw(px, new Rectangle(rect.X + 7, rect.Y + cornerL + 4, 1, rect.Height - cornerL * 2 - 8),
                uv, BloodFlash * (alpha * 0.50f * barPulse));

            //左上的Murasama标识——锐三角(取代原Diamond Dogs菱形)
            DrawMurasamaSigil(sb, rect.X + 9, rect.Y + 9, alpha, p);

            //顶部中段虚线(角标之间的间隔填充)
            for (int x = rect.X + cornerL + 4; x < rect.Right - cornerL - 4; x += 7) {
                int w = Math.Min(4, rect.Right - cornerL - 4 - x);
                if (w > 0) sb.Draw(px, new Rectangle(x, rect.Y, w, 1), uv, edge * 0.25f);
            }

            //底部信号脉动虚线
            for (int x = rect.X + cornerL + 4; x < rect.Right - cornerL - 4; x += 6) {
                int w = Math.Min(3, rect.Right - cornerL - 4 - x);
                float t = (float)(x - rect.X) / rect.Width;
                float fade = MathF.Sin(scan * 1.3f + t * MathHelper.TwoPi) * 0.35f + 0.30f;
                if (w > 0) sb.Draw(px, new Rectangle(x, rect.Bottom - 3, w, 1), uv,
                    CrimsonDeep * (alpha * fade));
            }
        }

        //鬼妖村正"刀刃印"——三角刀尖造型
        private void DrawMurasamaSigil(SpriteBatch sb, int cx, int cy, float alpha, float p) {
            var px = VaultAsset.placeholder2.Value;
            //外三角(刀刃)
            sb.Draw(px, new Vector2(cx, cy), null, BladeMode * (alpha * 0.75f * p),
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(5f), SpriteEffects.None, 0f);
            //内黑(刀镡)
            sb.Draw(px, new Vector2(cx, cy), null, BgVoid * (alpha * 0.95f),
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(2.6f), SpriteEffects.None, 0f);
            //中央亮点
            sb.Draw(px, new Vector2(cx, cy), null, BloodFlash * (alpha * p * 0.9f),
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(1.1f), SpriteEffects.None, 0f);
        }

        public void DrawWidgetHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //标题左侧竖向血条
            float p = MathF.Sin(pulse * 2f) * 0.20f + 0.80f;
            sb.Draw(px, new Rectangle(headerRect.X + 10, headerRect.Y + 4, 3, headerRect.Height - 6),
                uv, BladeMode * (alpha * 0.75f * p));

            //标题文字(刀刃银白,带血色阴影)
            Vector2 titlePos = new(headerRect.X + 18, headerRect.Y + (headerRect.Height - 14f) / 2f);
            //血色拖尾阴影
            Utils.DrawBorderString(sb, title, titlePos + new Vector2(2, 1),
                CrimsonDeep * (alpha * 0.65f), 0.76f);
            Utils.DrawBorderString(sb, title, titlePos + new Vector2(0, 1),
                BgVoid * (alpha * 0.5f), 0.76f);
            //主体文字
            Utils.DrawBorderString(sb, title, titlePos, TitleBlade * alpha, 0.76f);

            //标题下方分段——短实线(粗) + 间距 + 长虚线
            int sepY = headerRect.Bottom - 1;
            int solidW = 24;
            sb.Draw(px, new Rectangle(headerRect.X + 12, sepY, solidW, 1),
                uv, BladeMode * (alpha * 0.60f));
            //实线右端的小三角箭头(MGR:R HUD箭头)
            sb.Draw(px, new Vector2(headerRect.X + 12 + solidW + 1, sepY + 0.5f), null,
                BladeMode * (alpha * 0.60f), 0f, new Vector2(0f, 0.5f),
                new Vector2(3f, 1f), SpriteEffects.None, 0f);
            for (int x = headerRect.X + 12 + solidW + 8; x < headerRect.Right - 10; x += 6) {
                int w = Math.Min(3, headerRect.Right - 10 - x);
                if (w > 0) sb.Draw(px, new Rectangle(x, sepY, w, 1), uv, CrimsonDeep * (alpha * 0.40f));
            }
        }

        public void DrawWidgetProgress(SpriteBatch sb, Rectangle barRect, float progress,
            string progressText, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //进度条背景
            sb.Draw(px, new Rectangle(barRect.X + 1, barRect.Y, barRect.Width - 2, barRect.Height),
                uv, BgVoid * (alpha * 0.92f));

            //上下薄线(刀身边沿)
            sb.Draw(px, new Rectangle(barRect.X + 1, barRect.Y, barRect.Width - 2, 1),
                uv, CrimsonMid * (alpha * 0.55f));
            sb.Draw(px, new Rectangle(barRect.X + 1, barRect.Bottom - 1, barRect.Width - 2, 1),
                uv, CrimsonMid * (alpha * 0.30f));

            //分段填充——MGR:R充能式分段
            int fillW = (int)((barRect.Width - 2) * MathHelper.Clamp(progress, 0f, 1f));
            if (fillW > 1) {
                int segW = 5, gap = 1;
                for (int sx = 0; sx < fillW; sx += segW + gap) {
                    int sw = Math.Min(segW, fillW - sx);
                    float t = (float)sx / (barRect.Width - 2);
                    Color c = Color.Lerp(BarFillDark, BarFillBright, t);
                    float p = MathF.Sin(scan * 1.5f + t * MathHelper.Pi) * 0.18f + 0.82f;
                    sb.Draw(px, new Rectangle(barRect.X + 1 + sx, barRect.Y + 1, sw, barRect.Height - 2),
                        uv, c * (alpha * p));
                }
                //前端炽亮缘(刀光)
                if (fillW > 3) {
                    sb.Draw(px, new Rectangle(barRect.X + fillW, barRect.Y, 1, barRect.Height),
                        uv, BloodFlash * (alpha * 0.85f));
                    //顶端多1px强光,模拟充能完成边缘
                    sb.Draw(px, new Rectangle(barRect.X + fillW - 1, barRect.Y, 1, barRect.Height),
                        uv, BladeMode * (alpha * 0.55f));
                }
            }

            //满级附加血气脉冲
            if (progress >= 0.999f) {
                float fullPulse = MathF.Sin(pulse * 4f) * 0.3f + 0.7f;
                sb.Draw(px, new Rectangle(barRect.X + 1, barRect.Y, barRect.Width - 2, barRect.Height),
                    uv, BloodFlash * (alpha * 0.18f * fullPulse));
            }

            if (!string.IsNullOrEmpty(progressText)) {
                var font = FontAssets.MouseText.Value;
                Vector2 sz = font.MeasureString(progressText) * 0.5f;
                Utils.DrawBorderString(sb, progressText,
                    new Vector2(barRect.X + barRect.Width / 2f - sz.X / 2f,
                        barRect.Y + barRect.Height / 2f - sz.Y / 2f),
                    BloodFlash * alpha, 0.5f);
            }
        }

        public void DrawWidgetDivider(SpriteBatch sb, Vector2 start, Vector2 end, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            float len = (end - start).Length();
            if (len < 1f) return;
            Vector2 dir = Vector2.Normalize(end - start);
            float rot = MathF.Atan2(dir.Y, dir.X);
            //血气虚线
            for (float c = 0; c < len; c += 7f) {
                float segLen = Math.Min(4f, len - c);
                float t = c / len;
                float fade = MathF.Sin(t * MathHelper.Pi) * 0.55f;
                sb.Draw(px, start + dir * c, new Rectangle(0, 0, 1, 1),
                    CrimsonDeep * (alpha * fade), rot, new Vector2(0, 0.5f),
                    new Vector2(segLen, 1f), SpriteEffects.None, 0f);
            }
        }

        public void DrawWidgetOverlay(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //偶发血红故障闪烁
            float glitch = MathF.Sin(scan * 3.7f);
            if (glitch > 0.93f) {
                float intensity = (glitch - 0.93f) / 0.07f * 0.06f;
                sb.Draw(px, rect, uv, CrimsonBright * (alpha * intensity));
            }

            //顶部细密血滴(从顶部边沿垂下,模拟刀刃滴血)
            float dropT = (scan * 0.5f) % 1f;
            int dropCount = 4;
            for (int i = 0; i < dropCount; i++) {
                float phase = (dropT + i / (float)dropCount) % 1f;
                if (phase > 0.55f) continue;
                float xRel = ((i * 41) % 100) / 100f;
                int x = rect.X + 14 + (int)(xRel * (rect.Width - 28));
                int dropY = rect.Y + (int)(phase * 14f);
                float dAlpha = (1f - phase / 0.55f) * 0.55f;
                sb.Draw(px, new Rectangle(x, rect.Y, 1, dropY - rect.Y), uv, CrimsonMid * (alpha * dAlpha * 0.4f));
                sb.Draw(px, new Rectangle(x, dropY, 1, 2), uv, CrimsonBright * (alpha * dAlpha));
            }
        }

        public Color GetWidgetTitleColor(float alpha) => TitleBlade * alpha;
        public Color GetWidgetTextColor(float alpha) => TextBlade * alpha;
        public Color GetWidgetAccentColor(float alpha) => BladeMode * alpha;

        public int? GetPreferredWidth() => 235;
        public int? GetMinHeight() => 80;
        public int? GetIdleCompactHeight(EntrustEntryData entry) {
            if (entry.Progress <= 0f && entry.Status != QuestEntryStatus.Completed)
                return 46;
            return null;
        }
    }
}
