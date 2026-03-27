using CalamityOverhaul.Common;
using CalamityOverhaul.Content.QuestLogs.Core;
using CalamityOverhaul.Content.UIs.NotificationPopup;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Localization;

namespace CalamityOverhaul.Content.QuestLogs
{
    /// <summary>
    /// 任务书完成通知条目——带图标的任务完成弹窗
    /// </summary>
    internal class QuestCompletionEntry : NotificationEntry
    {
        private readonly QuestNode node;

        public override float Width => 260f;
        public override float Height => 60f;
        public override int SlideTime => 20;
        public override int DisplayTime => 180;
        public override float Gap => 5f;

        public QuestCompletionEntry(QuestNode node) {
            this.node = node;
        }

        public override bool OnClick() {
            SoundEngine.PlaySound(CWRSound.ButtonZero with { Volume = 0.6f, Pitch = -0.2f });
            return true;
        }

        public override void DrawContent(SpriteBatch sb, Rectangle panelRect, float alpha) {
            string titleText = QuestNotificationSystem.Text1?.Value ?? "任务完成";
            string nameText = node.DisplayName?.Value ?? "";

            DrawPanelBackground(sb, panelRect, Color.Black * 0.7f, Color.Gold, alpha);

            float padding = 10;
            float x = panelRect.X;
            float y = panelRect.Y;
            float iconSize = panelRect.Height - padding * 2;

            //图标
            var icon = node.GetIconTexture();
            if (icon != null) {
                Rectangle? frame = node.GetIconSourceRect(icon);
                Rectangle src = frame ?? icon.Frame();
                float scale = iconSize / MathHelper.Max(src.Width, src.Height);
                Vector2 iconCenter = new(x + padding + iconSize / 2, y + padding + iconSize / 2);
                sb.Draw(icon, iconCenter, src, Color.White * alpha, 0f,
                    src.Size() / 2, scale, SpriteEffects.None, 0f);
            }

            //文字
            float textX = x + padding * 2 + iconSize;
            Utils.DrawBorderString(sb, titleText, new Vector2(textX, y + 8), Color.Gold * alpha, 0.8f);
            Utils.DrawBorderString(sb, nameText, new Vector2(textX, y + 30), Color.White * alpha, 0.9f);
        }
    }
}
