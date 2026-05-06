using CalamityOverhaul.Content.ADV.EntrustManager;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.HalibutLegend.TrialQuests
{
    /// <summary>
    /// 比目鱼传奇追踪窗口样式——深海极简HUD：<br/>
    /// 不再复用ADV的<see cref="Content.ADV.Scenarios.Helen.Quest.OceanTrackerWidgetStyle"/>，
    /// 也不再绘制焦散面板与气泡背景，仅保留：发光菱形珠粒头部记号、
    /// 标题下方的实线 + 三段水波点 ~ 装饰、贴近文字的细青色进度线。
    /// </summary>
    internal class HalibutTrackerWidgetStyle : IEntrustTrackerWidgetStyle
    {
        #region 色板

        private static readonly Color SeaCyan = new(60, 200, 240);
        private static readonly Color SeaCyanBright = new(165, 235, 255);
        private static readonly Color SeaCyanDim = new(28, 110, 160);
        private static readonly Color BioGlow = new(60, 220, 200);
        private static readonly Color TitleIce = new(195, 240, 255);
        private static readonly Color TextSea = new(170, 220, 240);
        private static readonly Color ShadowInk = new(2, 8, 16);

        #endregion

        private float pulse;
        private float wave;

        public void Update(Rectangle widgetRect, float slideProgress) {
            pulse += 0.03f;
            wave += 0.045f;
            if (pulse > MathHelper.TwoPi) pulse -= MathHelper.TwoPi;
            if (wave > MathHelper.TwoPi) wave -= MathHelper.TwoPi;
        }

        public void Reset() { pulse = 0f; wave = 0f; }

        public void DrawWidgetBackground(SpriteBatch sb, Rectangle rect, float alpha) { }
        public void DrawWidgetFrame(SpriteBatch sb, Rectangle rect, float alpha) { }

        public void DrawWidgetHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);
            var font = FontAssets.MouseText.Value;

            //头部记号——发光菱形珠粒（带柔和的生物荧光halo）
            DrawPearlSigil(sb, px, uv, headerRect.X + 9, headerRect.Y + headerRect.Height / 2, alpha);

            //标题文字——水下散射投影 + 主体冰青
            const float titleScale = 0.85f;
            int textX = headerRect.X + 20;
            float textY = headerRect.Y + (headerRect.Height - 14f) / 2f;
            Vector2 titlePos = new(textX, textY);

            Utils.DrawBorderString(sb, title, titlePos + new Vector2(0, 1),
                ShadowInk * (alpha * 0.55f), titleScale);
            Utils.DrawBorderString(sb, title, titlePos, TitleIce * alpha, titleScale);

            //下划线——实线 + 三段水波点 ~
            int titlePixelW = (int)(font.MeasureString(title).X * titleScale);
            int underY = headerRect.Bottom - 2;
            int solidLen = Math.Clamp(titlePixelW + 4, 18, headerRect.Width - 40);
            float p = MathF.Sin(pulse * 1.6f) * 0.18f + 0.82f;

            //渐变细线（左浓右淡）
            int segs = Math.Max(8, solidLen / 3);
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int x1 = textX + (int)(t * solidLen);
                int x2 = textX + (int)(t2 * solidLen);
                int w = Math.Max(1, x2 - x1);
                Color c = Color.Lerp(SeaCyan, BioGlow, t * 0.4f) * (alpha * 0.85f * p);
                sb.Draw(px, new Rectangle(x1, underY, w, 1), uv, c);
            }

            //实线之后：3 个起伏的小水波点（y随正弦微抖）
            int waveStartX = textX + solidLen + 5;
            for (int k = 0; k < 3; k++) {
                int wx = waveStartX + k * 6;
                if (wx > headerRect.Right - 6) break;
                int wy = underY + (int)(MathF.Sin(wave * 2f + k * 1.1f) * 1.2f);
                float fade = (1f - k / 3f) * 0.7f;
                sb.Draw(px, new Rectangle(wx, wy, 2, 1), uv, SeaCyanBright * (alpha * fade));
            }
        }

        //菱形珠粒：旋转45°的小方块 + 内核 + halo光晕
        private void DrawPearlSigil(SpriteBatch sb, Texture2D px, Rectangle uv, int cx, int cy, float alpha) {
            float p = MathF.Sin(pulse * 2f) * 0.25f + 0.75f;
            //外halo
            sb.Draw(px, new Vector2(cx, cy), uv, BioGlow * (alpha * 0.18f * p),
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(11f), SpriteEffects.None, 0f);
            //外圈菱形（青蓝）
            sb.Draw(px, new Vector2(cx, cy), uv, SeaCyan * (alpha * 0.85f * p),
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(6f), SpriteEffects.None, 0f);
            //内核（更亮）
            sb.Draw(px, new Vector2(cx, cy), uv, SeaCyanBright * (alpha * p),
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(2.6f), SpriteEffects.None, 0f);
        }

        public void DrawWidgetProgress(SpriteBatch sb, Rectangle barRect, float progress,
            string progressText, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //2px细线
            const int barH = 2;
            int y = barRect.Y + (barRect.Height - barH) / 2;
            int trackW = barRect.Width;

            //轨道——非常淡的深海底色线
            sb.Draw(px, new Rectangle(barRect.X, y, trackW, barH), uv, SeaCyanDim * (alpha * 0.32f));

            //填充——青→生物荧光的轻微渐变
            int fillW = (int)(trackW * MathHelper.Clamp(progress, 0f, 1f));
            if (fillW > 0) {
                int segs = Math.Max(6, fillW / 4);
                for (int i = 0; i < segs; i++) {
                    float t = i / (float)segs;
                    float t2 = (i + 1) / (float)segs;
                    int x1 = barRect.X + (int)(t * fillW);
                    int x2 = barRect.X + (int)(t2 * fillW);
                    int w = Math.Max(1, x2 - x1);
                    Color c = Color.Lerp(SeaCyan, BioGlow, t * 0.6f) * (alpha * 0.92f);
                    sb.Draw(px, new Rectangle(x1, y, w, barH), uv, c);
                }
                //尖端水面闪光
                if (fillW > 1) {
                    sb.Draw(px, new Rectangle(barRect.X + fillW - 1, y - 1, 1, barH + 2), uv,
                        SeaCyanBright * (alpha * 0.7f));
                }
            }

            if (!string.IsNullOrEmpty(progressText)) {
                var font = FontAssets.MouseText.Value;
                Vector2 sz = font.MeasureString(progressText) * 0.5f;
                Utils.DrawBorderString(sb, progressText,
                    new Vector2(barRect.Right - sz.X - 1f, y - sz.Y - 1f),
                    SeaCyanBright * alpha, 0.5f);
            }
        }

        public void DrawWidgetDivider(SpriteBatch sb, Vector2 start, Vector2 end, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            float len = (end - start).Length();
            if (len < 1f) return;
            Vector2 dir = (end - start) / len;
            float rot = MathF.Atan2(dir.Y, dir.X);

            //中心亮、两端淡——水流式渐变虚线
            for (float c = 0; c < len; c += 5f) {
                float t = c / len;
                float fade = MathF.Sin(t * MathHelper.Pi) * 0.55f;
                float segLen = Math.Min(2f, len - c);
                sb.Draw(px, start + dir * c, new Rectangle(0, 0, 1, 1),
                    SeaCyanDim * (alpha * fade), rot, new Vector2(0, 0.5f),
                    new Vector2(segLen, 1f), SpriteEffects.None, 0f);
            }
        }

        public void DrawWidgetOverlay(SpriteBatch sb, Rectangle rect, float alpha) { }

        public Color GetWidgetTitleColor(float alpha) => TitleIce * alpha;
        public Color GetWidgetTextColor(float alpha) => TextSea * (alpha * 0.95f);
        public Color GetWidgetAccentColor(float alpha) => SeaCyan * alpha;

        public int? GetPreferredWidth() => 230;
        public int? GetMinHeight() => 50;
        public int? GetIdleCompactHeight(EntrustEntryData entry) {
            if (entry.Progress <= 0f && entry.Status != QuestEntryStatus.Completed)
                return 44;
            return null;
        }
    }
}
