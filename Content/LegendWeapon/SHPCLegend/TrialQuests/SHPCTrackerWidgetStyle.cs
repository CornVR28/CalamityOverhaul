using CalamityOverhaul.Content.ADV.EntrustManager;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.TrialQuests
{
    /// <summary>
    /// SHPC追踪窗口样式——赛博朋克2077式极简HUD：<br/>
    /// 完全无背景面板与外框，只保留霓虹蓝雪佛龙头部记号、标题下方的实线+点阵下划线、
    /// 以及一条贴近文字的扁平进度细线。视觉重量全部交给排版与文字本身。
    /// </summary>
    internal class SHPCTrackerWidgetStyle : IEntrustTrackerWidgetStyle
    {
        #region 色板

        private static readonly Color NeonBlue = new(120, 180, 255);
        private static readonly Color NeonBlueBright = new(190, 225, 255);
        private static readonly Color NeonBlueDim = new(60, 110, 180);
        private static readonly Color TitleSky = new(210, 230, 255);
        private static readonly Color TextSky = new(180, 205, 235);
        private static readonly Color ShadowInk = new(2, 4, 10);

        #endregion

        private float pulse;
        private float blink;

        public void Update(Rectangle widgetRect, float slideProgress) {
            pulse += 0.038f;
            blink += 0.06f;
            if (pulse > MathHelper.TwoPi) pulse -= MathHelper.TwoPi;
            if (blink > MathHelper.TwoPi) blink -= MathHelper.TwoPi;
        }

        public void Reset() { pulse = 0f; blink = 0f; }

        //极简：不绘制背景
        public void DrawWidgetBackground(SpriteBatch sb, Rectangle rect, float alpha) { }

        //极简：不绘制外框
        public void DrawWidgetFrame(SpriteBatch sb, Rectangle rect, float alpha) { }

        public void DrawWidgetHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);
            var font = FontAssets.MouseText.Value;

            //头部记号——雪佛龙 ❯（两段斜线拼成的尖角，CP2077终端命令提示符意象）
            int symX = headerRect.X + 6;
            int symY = headerRect.Y + headerRect.Height / 2;
            DrawChevron(sb, px, uv, symX, symY, alpha);

            //标题文字——细微深色投影 + 主体高光蓝
            const float titleScale = 0.85f;
            int textX = headerRect.X + 20;
            float textY = headerRect.Y + (headerRect.Height - 14f) / 2f;
            Vector2 titlePos = new(textX, textY);

            Utils.DrawBorderString(sb, title, titlePos + new Vector2(0, 1), ShadowInk * (alpha * 0.55f), titleScale);
            Utils.DrawBorderString(sb, title, titlePos, TitleSky * alpha, titleScale);

            //下划线：紧贴文字宽度的实线 + 向右延伸的点阵
            int titlePixelW = (int)(font.MeasureString(title).X * titleScale);
            int underY = headerRect.Bottom - 2;
            int solidLen = Math.Clamp(titlePixelW + 4, 18, headerRect.Width - 28);
            float p = MathF.Sin(pulse * 2f) * 0.18f + 0.82f;
            sb.Draw(px, new Rectangle(textX, underY, solidLen, 1), uv, NeonBlue * (alpha * 0.85f * p));

            //点阵延续——从实线末端开始，每4px一个2px小段
            int dotStart = textX + solidLen + 4;
            int dotEnd = headerRect.Right - 8;
            for (int x = dotStart; x < dotEnd; x += 4) {
                int w = Math.Min(2, dotEnd - x);
                if (w <= 0) break;
                float t = (float)(x - dotStart) / Math.Max(1, dotEnd - dotStart);
                float fade = (1f - t) * 0.7f;
                sb.Draw(px, new Rectangle(x, underY, w, 1), uv, NeonBlueDim * (alpha * fade));
            }
        }

        //雪佛龙 "❯" 由两条45°小线段拼成
        private void DrawChevron(SpriteBatch sb, Texture2D px, Rectangle uv, int cx, int cy, float alpha) {
            float p = MathF.Sin(pulse * 2.2f) * 0.25f + 0.75f;
            Color c = NeonBlueBright * (alpha * p);
            //投影
            Color s = ShadowInk * (alpha * 0.45f);
            sb.Draw(px, new Vector2(cx, cy + 1), uv, s, MathHelper.PiOver4, new Vector2(0f, 0.5f), new Vector2(6f, 1f), SpriteEffects.None, 0f);
            sb.Draw(px, new Vector2(cx, cy + 1), uv, s, -MathHelper.PiOver4, new Vector2(0f, 0.5f), new Vector2(6f, 1f), SpriteEffects.None, 0f);
            //上斜（向右下）
            sb.Draw(px, new Vector2(cx, cy - 4), uv, c, MathHelper.PiOver4, new Vector2(0f, 0.5f), new Vector2(6f, 1f), SpriteEffects.None, 0f);
            //下斜（向右上）
            sb.Draw(px, new Vector2(cx, cy + 4), uv, c, -MathHelper.PiOver4, new Vector2(0f, 0.5f), new Vector2(6f, 1f), SpriteEffects.None, 0f);
        }

        public void DrawWidgetProgress(SpriteBatch sb, Rectangle barRect, float progress,
            string progressText, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //超扁平 2px 进度细线
            const int barH = 2;
            int y = barRect.Y + (barRect.Height - barH) / 2;
            int trackW = barRect.Width;

            //轨道——极淡的背景线（不是实心填充）
            sb.Draw(px, new Rectangle(barRect.X, y, trackW, barH), uv, NeonBlueDim * (alpha * 0.22f));

            //填充
            int fillW = (int)(trackW * MathHelper.Clamp(progress, 0f, 1f));
            if (fillW > 0) {
                sb.Draw(px, new Rectangle(barRect.X, y, fillW, barH), uv, NeonBlueBright * (alpha * 0.92f));
                //尖端高光（向上下各延伸1px模拟扫描头）
                if (fillW > 1) {
                    sb.Draw(px, new Rectangle(barRect.X + fillW - 1, y - 1, 1, barH + 2), uv,
                        Color.White * (alpha * 0.6f));
                }
            }

            //四分位刻度（向下凸出 1px）
            for (int i = 1; i < 4; i++) {
                int tx = barRect.X + (int)(trackW * (i / 4f));
                sb.Draw(px, new Rectangle(tx, y + barH, 1, 2), uv, NeonBlueDim * (alpha * 0.45f));
            }

            //进度文字——靠右上方，0.5倍小字
            if (!string.IsNullOrEmpty(progressText)) {
                var font = FontAssets.MouseText.Value;
                Vector2 sz = font.MeasureString(progressText) * 0.5f;
                Utils.DrawBorderString(sb, progressText,
                    new Vector2(barRect.Right - sz.X - 1f, y - sz.Y - 1f),
                    NeonBlueBright * alpha, 0.5f);
            }
        }

        public void DrawWidgetDivider(SpriteBatch sb, Vector2 start, Vector2 end, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            float len = (end - start).Length();
            if (len < 1f) return;
            Vector2 dir = (end - start) / len;
            float rot = MathF.Atan2(dir.Y, dir.X);

            //极淡的等距点阵分隔
            for (float c = 0; c < len; c += 5f) {
                float segLen = Math.Min(2f, len - c);
                sb.Draw(px, start + dir * c, new Rectangle(0, 0, 1, 1),
                    NeonBlueDim * (alpha * 0.32f), rot, new Vector2(0, 0.5f),
                    new Vector2(segLen, 1f), SpriteEffects.None, 0f);
            }
        }

        //极简：不绘制覆盖特效
        public void DrawWidgetOverlay(SpriteBatch sb, Rectangle rect, float alpha) { }

        public Color GetWidgetTitleColor(float alpha) => TitleSky * alpha;
        public Color GetWidgetTextColor(float alpha) => TextSky * (alpha * 0.95f);
        public Color GetWidgetAccentColor(float alpha) => NeonBlue * alpha;

        public int? GetPreferredWidth() => 230;
        public int? GetMinHeight() => 50;
        public int? GetIdleCompactHeight(EntrustEntryData entry) {
            if (entry.Progress <= 0f && entry.Status != QuestEntryStatus.Completed)
                return 44;
            return null;
        }
    }
}
