using CalamityOverhaul.Content.UIs.NotificationPopup;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.QuestManager
{
    /// <summary>
    /// 委托管理器状态变更通知——瘦装饰器，将通知委托给统一的 <see cref="NotificationPopupSystem"/>
    /// </summary>
    internal class QuestManagerNotification : ModSystem, ILocalizedModType
    {
        /// <summary>通知类型</summary>
        internal enum NotifyKind
        {
            NewQuest,
            Tracked,
            Untracked,
            Suspended,
            Unsuspended,
            Completed,
        }

        public string LocalizationCategory => "UI";

        #region 本地化

        public static LocalizedText LabelNewQuest { get; private set; }
        public static LocalizedText LabelTracked { get; private set; }
        public static LocalizedText LabelUntracked { get; private set; }
        public static LocalizedText LabelSuspended { get; private set; }
        public static LocalizedText LabelUnsuspended { get; private set; }
        public static LocalizedText LabelCompleted { get; private set; }

        public override void SetStaticDefaults() {
            LabelNewQuest = this.GetLocalization(nameof(LabelNewQuest), () => "新委托");
            LabelTracked = this.GetLocalization(nameof(LabelTracked), () => "已关注");
            LabelUntracked = this.GetLocalization(nameof(LabelUntracked), () => "取消关注");
            LabelSuspended = this.GetLocalization(nameof(LabelSuspended), () => "已挂起");
            LabelUnsuspended = this.GetLocalization(nameof(LabelUnsuspended), () => "已恢复");
            LabelCompleted = this.GetLocalization(nameof(LabelCompleted), () => "委托完成");
        }

        #endregion

        /// <summary>外部调用：添加一条状态变更通知</summary>
        public static void Notify(string questTitle, NotifyKind kind) {
            var statusKind = (QuestStatusEntry.StatusKind)(int)kind;
            NotificationPopupSystem.Add(new QuestStatusEntry(questTitle, statusKind));
        }
    }
}
