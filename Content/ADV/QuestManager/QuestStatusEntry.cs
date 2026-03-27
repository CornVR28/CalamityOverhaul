using CalamityOverhaul.Content.UIs.NotificationPopup;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;

namespace CalamityOverhaul.Content.ADV.QuestManager
{
    /// <summary>
    /// 委托管理器状态变更通知条目——显示状态标签 + 委托名称
    /// </summary>
    internal class QuestStatusEntry : NotificationEntry
    {
        public enum StatusKind
        {
            NewQuest,
            Tracked,
            Untracked,
            Suspended,
            Unsuspended,
            Completed,
        }

        private readonly string questTitle;
        private readonly StatusKind kind;

        public override float Width => 240f;
        public override float Height => 42f;
        public override int SlideTime => 18;
        public override int DisplayTime => 140;
        public override float Gap => 4f;

        public QuestStatusEntry(string questTitle, StatusKind kind) {
            this.questTitle = questTitle;
            this.kind = kind;
        }

        public override void DrawContent(SpriteBatch sb, Rectangle panelRect, float alpha) {
            Color borderC = GetBorderColor();
            DrawPanelBackground(sb, panelRect, Color.Black * 0.65f, borderC, alpha);

            float x = panelRect.X;
            float y = panelRect.Y;

            //状态标签
            string label = GetLabel();
            Color labelC = GetLabelColor();
            Utils.DrawBorderString(sb, label, new Vector2(x + 10, y + 5), labelC * alpha, 0.7f);

            //委托名称
            Utils.DrawBorderString(sb, questTitle, new Vector2(x + 10, y + 22),
                Color.White * (alpha * 0.9f), 0.75f);
        }

        private string GetLabel() => kind switch {
            StatusKind.NewQuest => QuestManagerNotification.LabelNewQuest.Value,
            StatusKind.Tracked => QuestManagerNotification.LabelTracked.Value,
            StatusKind.Untracked => QuestManagerNotification.LabelUntracked.Value,
            StatusKind.Suspended => QuestManagerNotification.LabelSuspended.Value,
            StatusKind.Unsuspended => QuestManagerNotification.LabelUnsuspended.Value,
            StatusKind.Completed => QuestManagerNotification.LabelCompleted.Value,
            _ => ""
        };

        private Color GetLabelColor() => kind switch {
            StatusKind.NewQuest => new Color(100, 220, 255),
            StatusKind.Tracked => new Color(100, 255, 180),
            StatusKind.Untracked => new Color(180, 180, 180),
            StatusKind.Suspended => new Color(220, 180, 80),
            StatusKind.Unsuspended => new Color(140, 220, 120),
            StatusKind.Completed => Color.Gold,
            _ => Color.White
        };

        private Color GetBorderColor() => kind switch {
            StatusKind.NewQuest => new Color(60, 160, 220),
            StatusKind.Completed => Color.Gold,
            _ => new Color(80, 120, 160)
        };
    }
}
