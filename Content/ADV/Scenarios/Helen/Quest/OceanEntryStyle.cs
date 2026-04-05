using CalamityOverhaul.Content.ADV.QuestManager;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace CalamityOverhaul.Content.ADV.Scenarios.Helen.Quest.FishoilQuest
{
    /// <summary>
    /// 比目鱼委托在管理器列表中的自定义条目样式——
    /// 深海蓝色背景、海洋脉冲边框、冷色调标题
    /// </summary>
    internal class OceanEntryStyle : IQuestEntryStyle
    {
        #region 色板

        private static readonly Color BgDeep = new(4, 18, 30);
        private static readonly Color BgHover = new(10, 32, 50);
        private static readonly Color BgSelected = new(16, 44, 65);
        private static readonly Color AccentSea = new(30, 140, 190);
        private static readonly Color AccentBright = new(90, 210, 255);
        private static readonly Color TitleIce = new(180, 240, 255);
        private static readonly Color TitleComplete = new(60, 220, 140);
        private static readonly Color BorderBase = new(30, 100, 140);
        private static readonly Color BorderGlow = new(70, 180, 230);

        #endregion

        private float pulseTimer;
        private float wavePhase;

        public void Update() {
            pulseTimer += 0.03f;
            if (pulseTimer > MathHelper.TwoPi) pulseTimer -= MathHelper.TwoPi;
            wavePhase += 0.02f;
            if (wavePhase > MathHelper.TwoPi) wavePhase -= MathHelper.TwoPi;
        }

        public bool DrawEntryBackground(SpriteBatch sb, Rectangle entryRect, QuestEntryData entry,
            bool isSelected, bool isHovered, float alpha) {
            var px = VaultAsset.placeholder2.Value;

            int segs = 8;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = entryRect.Y + (int)(t * entryRect.Height);
                int y2 = entryRect.Y + (int)(t2 * entryRect.Height);

                float wave = MathF.Sin(pulseTimer * 1.2f + t * 3f) * 0.5f + 0.5f;
                Color bg = isSelected ? BgSelected : (isHovered ? BgHover : BgDeep);
                Color c = Color.Lerp(bg, Color.Lerp(bg, new Color(20, 60, 90), 0.3f), wave * 0.3f) * (alpha * 0.92f);

                sb.Draw(px, new Rectangle(entryRect.X, y1, entryRect.Width, Math.Max(1, y2 - y1)),
                    new Rectangle(0, 0, 1, 1), c);
            }

            //呼吸脉冲
            float pulse = MathF.Sin(pulseTimer * 2f) * 0.5f + 0.5f;
            sb.Draw(px, entryRect, new Rectangle(0, 0, 1, 1), AccentSea * (alpha * 0.05f * pulse));

            //左侧状态色带
            Color statusColor = GetAccentColor(entry.Status, alpha);
            sb.Draw(px, new Rectangle(entryRect.X, entryRect.Y + 2, 3, entryRect.Height - 4),
                new Rectangle(0, 0, 1, 1), statusColor);

            //边框
            float borderGlow = MathF.Sin(wavePhase) * 0.3f + 0.7f;
            Color borderC = Color.Lerp(BorderBase, BorderGlow, borderGlow) * (alpha * 0.5f);
            sb.Draw(px, new Rectangle(entryRect.X, entryRect.Y, entryRect.Width, 1),
                new Rectangle(0, 0, 1, 1), borderC);
            sb.Draw(px, new Rectangle(entryRect.X, entryRect.Bottom - 1, entryRect.Width, 1),
                new Rectangle(0, 0, 1, 1), borderC * 0.6f);

            return true;
        }

        public float DrawEntryIcon(SpriteBatch sb, Vector2 titlePos, QuestEntryData entry, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            //水滴形图标（用菱形近似）
            float iconX = titlePos.X + 6f;
            float iconY = titlePos.Y + 8f;
            float iconPulse = MathF.Sin(pulseTimer + 1f) * 0.3f + 0.7f;
            Color iconColor = AccentSea * (alpha * iconPulse);

            sb.Draw(px, new Vector2(iconX, iconY), null, iconColor,
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(5f), SpriteEffects.None, 0f);

            Color glowC = AccentBright * (alpha * iconPulse * 0.25f);
            sb.Draw(px, new Vector2(iconX, iconY), null, glowC,
                MathHelper.PiOver4, new Vector2(0.5f), new Vector2(9f), SpriteEffects.None, 0f);

            return 18f;
        }

        public void DrawEntryOverlay(SpriteBatch sb, Rectangle entryRect, QuestEntryData entry, float alpha) {
            //右上角微弱波光点
            var px = VaultAsset.placeholder2.Value;
            float ornAlpha = alpha * (0.3f + MathF.Sin(wavePhase * 1.5f) * 0.2f);
            Color ornC = AccentBright * ornAlpha;
            sb.Draw(px, new Vector2(entryRect.Right - 8, entryRect.Y + 6), null, ornC,
                wavePhase * 0.06f, new Vector2(0.5f), new Vector2(2f), SpriteEffects.None, 0f);
        }

        public Color GetAccentColor(QuestEntryStatus status, float alpha) {
            return status switch {
                QuestEntryStatus.Completed => TitleComplete * alpha,
                QuestEntryStatus.Failed => new Color(220, 60, 70) * alpha,
                QuestEntryStatus.Suspended => new Color(120, 140, 160) * alpha,
                QuestEntryStatus.Tracked => AccentBright * alpha,
                _ => AccentSea * alpha,
            };
        }

        public Color GetTitleColor(QuestEntryStatus status, float alpha) {
            return status switch {
                QuestEntryStatus.Completed => TitleComplete * (alpha * 0.8f),
                _ => TitleIce * alpha,
            };
        }

        public int? GetCustomEntryHeight() => null;

        public void Reset() {
            pulseTimer = 0f;
            wavePhase = 0f;
        }
    }
}
