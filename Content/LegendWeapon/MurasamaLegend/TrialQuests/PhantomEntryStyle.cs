using CalamityOverhaul.Content.ADV.QuestManager;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace CalamityOverhaul.Content.LegendWeapon.MurasamaLegend.TrialQuests
{
    /// <summary>
    /// 鬼妖村正委托条目样式——合金装备5幻痛(MGSV:TPP)主题：<br/>
    /// 军事暗绿/深灰基调、iDroid风格扫描线、菱形标记、
    /// 雷达脉冲边框、战术数据流装饰
    /// </summary>
    internal class PhantomEntryStyle : IQuestEntryStyle
    {
        #region 色板

        //军事暗色基调
        private static readonly Color BgDeep = new(8, 12, 10);
        private static readonly Color BgMid = new(16, 24, 18);
        private static readonly Color BgHover = new(22, 34, 26);
        private static readonly Color BgSelected = new(30, 44, 32);

        //iDroid风格色调
        private static readonly Color IDroidGreen = new(90, 195, 110);
        private static readonly Color IDroidDim = new(50, 110, 65);
        private static readonly Color IDroidBright = new(140, 255, 160);
        private static readonly Color ScanlineGreen = new(60, 155, 80);

        //Diamond Dogs标记色
        private static readonly Color DiamondOrange = new(210, 145, 45);
        private static readonly Color AlertRed = new(200, 55, 40);
        private static readonly Color CompletedGold = new(190, 170, 70);

        //文字
        private static readonly Color TitleGreen = new(155, 235, 165);
        private static readonly Color TitleComplete = new(180, 175, 75);

        #endregion

        private float scanTime;
        private float pulseTime;
        private float dataFlowTime;

        public void Update() {
            scanTime += 0.035f;
            pulseTime += 0.02f;
            dataFlowTime += 0.06f;
            const float wrap = MathHelper.TwoPi * 4f;
            if (scanTime > wrap) scanTime -= wrap;
            if (pulseTime > wrap) pulseTime -= wrap;
            if (dataFlowTime > 100f) dataFlowTime -= 100f;
        }

        public bool DrawEntryBackground(SpriteBatch sb, Rectangle entryRect, QuestEntryData entry,
            bool isSelected, bool isHovered, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var uv = new Rectangle(0, 0, 1, 1);

            //多段纵向渐变，模拟CRT显示器的非均匀亮度
            int segs = 12;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = entryRect.Y + (int)(t * entryRect.Height);
                int y2 = entryRect.Y + (int)(t2 * entryRect.Height);
                if (y2 <= y1) continue;

                Color baseC = isSelected ? BgSelected
                    : isHovered ? BgHover
                    : Color.Lerp(BgDeep, BgMid, t);

                //CRT亮度不均匀
                float crtBand = MathF.Sin(t * MathHelper.Pi * 3f) * 0.08f;
                Color c = Color.Lerp(baseC, IDroidDim, Math.Max(0f, crtBand)) * (alpha * 0.95f);

                sb.Draw(px, new Rectangle(entryRect.X, y1, entryRect.Width, y2 - y1), uv, c);
            }

            //水平扫描线（每3px一条，极低透明度，CRT感）
            for (int y = entryRect.Y; y < entryRect.Bottom; y += 3) {
                sb.Draw(px, new Rectangle(entryRect.X, y, entryRect.Width, 1),
                    uv, ScanlineGreen * (alpha * 0.04f));
            }

            //iDroid雷达脉冲扫线（从左到右循环的竖亮带）
            float radarPos = ((scanTime * 0.3f) % 1f);
            int radarX = entryRect.X + (int)(radarPos * entryRect.Width);
            int radarW = (int)(entryRect.Width * 0.15f);
            for (int dx = 0; dx < radarW; dx++) {
                int px2 = radarX + dx;
                if (px2 < entryRect.X || px2 >= entryRect.Right) continue;
                float fade = 1f - (float)dx / radarW;
                fade *= fade;
                sb.Draw(px, new Rectangle(px2, entryRect.Y, 1, entryRect.Height),
                    uv, IDroidGreen * (alpha * fade * 0.08f));
            }

            //左侧状态竖条（Diamond Dogs风格双线）
            Color statusC = GetAccentColor(entry.Status, 1f);
            float barPulse = MathF.Sin(pulseTime * 2.5f) * 0.25f + 0.75f;
            sb.Draw(px, new Rectangle(entryRect.X, entryRect.Y + 1, 2, entryRect.Height - 2),
                uv, statusC * (alpha * barPulse));
            sb.Draw(px, new Rectangle(entryRect.X + 3, entryRect.Y + 3, 1, entryRect.Height - 6),
                uv, statusC * (alpha * barPulse * 0.4f));

            //上边框：实线左段 + 断口 + 虚线右段（战术HUD风格）
            int breakX = entryRect.X + (int)(entryRect.Width * 0.35f);
            Color borderC = IDroidDim * (alpha * 0.6f);
            sb.Draw(px, new Rectangle(entryRect.X, entryRect.Y, breakX - entryRect.X, 1), uv, borderC);
            //虚线段
            for (int x = breakX + 8; x < entryRect.Right - 4; x += 6) {
                int w = Math.Min(3, entryRect.Right - 4 - x);
                if (w > 0) sb.Draw(px, new Rectangle(x, entryRect.Y, w, 1), uv, borderC * 0.5f);
            }
            //下边框（淡化）
            sb.Draw(px, new Rectangle(entryRect.X + 6, entryRect.Bottom - 1, entryRect.Width - 12, 1),
                uv, borderC * 0.3f);

            //右侧战术数据流（随机变化的细碎竖条）
            int dataRegionW = 6;
            for (int y = entryRect.Y + 2; y < entryRect.Bottom - 2; y += 4) {
                float noise = MathF.Sin(dataFlowTime * 1.3f + y * 0.7f)
                            * MathF.Sin(dataFlowTime * 0.8f + y * 0.3f);
                if (noise > 0.2f) {
                    float intensity = (noise - 0.2f) / 0.8f * 0.15f;
                    int w = 1 + (int)(noise * 3f);
                    sb.Draw(px, new Rectangle(entryRect.Right - dataRegionW - w, y, w, 2),
                        uv, IDroidGreen * (alpha * intensity));
                }
            }

            return true;
        }

        public float DrawEntryIcon(SpriteBatch sb, Vector2 titlePos, QuestEntryData entry, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            float cx = titlePos.X + 8f;
            float cy = titlePos.Y + 9f;

            //Diamond Dogs菱形标记
            float pulse = MathF.Sin(scanTime * 1.8f) * 0.25f + 0.75f;
            Color diamondC = entry.Status == QuestEntryStatus.Completed
                ? CompletedGold : DiamondOrange;

            //外框菱形
            sb.Draw(px, new Vector2(cx, cy), null, diamondC * (alpha * pulse),
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(6f), SpriteEffects.None, 0f);
            //内挖空
            sb.Draw(px, new Vector2(cx, cy), null, BgDeep * (alpha * 0.85f),
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(3.5f), SpriteEffects.None, 0f);
            //中心亮点
            sb.Draw(px, new Vector2(cx, cy), null, diamondC * (alpha * pulse * 0.6f),
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(1.5f), SpriteEffects.None, 0f);

            //雷达扩散环（周期性）
            float ringPhase = (scanTime * 0.6f) % MathHelper.TwoPi;
            float ringSize = 3f + ringPhase / MathHelper.TwoPi * 10f;
            float ringAlpha = 1f - ringPhase / MathHelper.TwoPi;
            sb.Draw(px, new Vector2(cx, cy), null, IDroidGreen * (alpha * ringAlpha * 0.15f),
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(ringSize), SpriteEffects.None, 0f);

            return 22f;
        }

        public void DrawEntryOverlay(SpriteBatch sb, Rectangle entryRect, QuestEntryData entry, float alpha) {
            var px = VaultAsset.placeholder2.Value;

            //右上角Mission等级标记（小菱形序列）
            int dots = 3;
            float dotStartX = entryRect.Right - 14 - (dots - 1) * 6;
            float dotY = entryRect.Y + 5f;
            for (int d = 0; d < dots; d++) {
                float dAlpha = MathF.Sin(scanTime * 1.2f + d * 0.8f) * 0.3f + 0.5f;
                sb.Draw(px, new Vector2(dotStartX + d * 6, dotY), null,
                    IDroidDim * (alpha * dAlpha), MathHelper.PiOver4,
                    new Vector2(0.5f), new Vector2(2f), SpriteEffects.None, 0f);
            }

            //底部偶发的轻微故障闪烁
            float glitch = MathF.Sin(dataFlowTime * 3.7f);
            if (glitch > 0.92f) {
                float gIntensity = (glitch - 0.92f) / 0.08f * 0.06f;
                sb.Draw(px, entryRect, new Rectangle(0, 0, 1, 1), IDroidGreen * (alpha * gIntensity));
            }
        }

        public Color GetAccentColor(QuestEntryStatus status, float alpha) {
            return status switch {
                QuestEntryStatus.Completed => CompletedGold * alpha,
                QuestEntryStatus.Failed => AlertRed * alpha,
                QuestEntryStatus.Suspended => new Color(100, 115, 105) * alpha,
                QuestEntryStatus.Tracked => IDroidBright * alpha,
                _ => IDroidGreen * alpha,
            };
        }

        public Color GetTitleColor(QuestEntryStatus status, float alpha) {
            return status switch {
                QuestEntryStatus.Completed => TitleComplete * (alpha * 0.85f),
                QuestEntryStatus.Failed => AlertRed * (alpha * 0.9f),
                _ => TitleGreen * alpha,
            };
        }

        public int? GetCustomEntryHeight() => null;

        public void Reset() {
            scanTime = 0f;
            pulseTime = 0f;
            dataFlowTime = 0f;
        }
    }
}
