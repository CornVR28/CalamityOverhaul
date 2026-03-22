using CalamityOverhaul.Content.ADV.QuestManager;
using CalamityOverhaul.Content.LegendWeapon.HalibutLegend;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.SupCal.Quest
{
    /// <summary>
    /// 硫火女巫委托线——将三段女巫委托注册到 <see cref="QuestManagerUI"/>，
    /// 并根据 <see cref="ADVSave"/> 实时同步状态
    /// </summary>
    internal class SupCalQuestLine : ModSystem, ILocalizedModType
    {
        public string LocalizationCategory => "ADV";

        #region 常量 Key

        private const string PALLBEARER_KEY = "SupCal_Pallbearer";
        private const string DOG_KEY = "SupCal_DoG";
        private const string YHARON_KEY = "SupCal_Yharon";

        #endregion

        #region 本地化文本

        public static LocalizedText QuestCategory { get; private set; }

        public static LocalizedText PallbearerTitle { get; private set; }
        public static LocalizedText PallbearerSummary { get; private set; }

        public static LocalizedText DoGTitle { get; private set; }
        public static LocalizedText DoGSummary { get; private set; }

        public static LocalizedText YharonTitle { get; private set; }
        public static LocalizedText YharonSummary { get; private set; }

        #endregion

        public override void SetStaticDefaults() {
            QuestCategory = this.GetLocalization(nameof(QuestCategory), () => "硫火女巫");

            PallbearerTitle = this.GetLocalization(nameof(PallbearerTitle), () => "委托：猎杀亵渎天神");
            PallbearerSummary = this.GetLocalization(nameof(PallbearerSummary), () => "使用扶柩者击杀亵渎天神，贡献度需达到80%");

            DoGTitle = this.GetLocalization(nameof(DoGTitle), () => "委托：猎杀神明吞噬者");
            DoGSummary = this.GetLocalization(nameof(DoGSummary), () => "使用刻心者击杀神明吞噬者，贡献度需达到80%");

            YharonTitle = this.GetLocalization(nameof(YharonTitle), () => "委托：猎杀焚世龙");
            YharonSummary = this.GetLocalization(nameof(YharonSummary), () => "使用鬼面刀击杀焚世之龙，贡献度需达到75%");
        }

        public override void PostUpdateWorld() {
            if (Main.dedServ || Main.gameMenu) return;

            var manager = QuestManagerUI.Instance;
            if (manager == null) return;

            if (!Main.LocalPlayer.TryGetOverride<HalibutPlayer>(out var hp)) return;
            var save = hp.ADVSave;

            SyncQuest(manager, save, PALLBEARER_KEY,
                PallbearerTitle, PallbearerSummary,
                prerequisite: save.SupCalMoonLordReward,
                accepted: save.SupCalQuestAccepted,
                declined: save.SupCalQuestDeclined,
                completed: save.SupCalQuestReward,
                priority: 30);

            SyncQuest(manager, save, DOG_KEY,
                DoGTitle, DoGSummary,
                prerequisite: save.SupCalQuestReward,
                accepted: save.SupCalDoGQuestAccepted,
                declined: save.SupCalDoGQuestDeclined,
                completed: save.SupCalDoGQuestReward,
                priority: 20);

            SyncQuest(manager, save, YHARON_KEY,
                YharonTitle, YharonSummary,
                prerequisite: save.SupCalDoGQuestReward,
                accepted: save.SupCalYharonQuestAccepted,
                declined: save.SupCalYharonQuestDeclined,
                completed: save.SupCalYharonQuestReward,
                priority: 10);
        }

        /// <summary>
        /// 同步单条委托的注册与状态<br/>
        /// 前置未满足或已拒绝 → 从管理器移除<br/>
        /// 已接受 → 注册并设为 Active<br/>
        /// 已完成 → 标记 Completed
        /// </summary>
        private static void SyncQuest(
            QuestManagerUI manager, ADVSave save, string key,
            LocalizedText title, LocalizedText summary,
            bool prerequisite, bool accepted, bool declined, bool completed,
            int priority)
        {
            //前置未达成或已拒绝 → 不显示
            if (!prerequisite || declined) {
                manager.UnregisterQuest(key);
                return;
            }

            //未接受（委托尚未交付） → 不显示
            if (!accepted) {
                manager.UnregisterQuest(key);
                return;
            }

            //确保已注册
            var entry = manager.GetEntry(key);
            if (entry == null) {
                entry = new QuestEntryData(key, title, summary, QuestCategory) {
                    Priority = priority,
                    EntryStyle = new BrimstoneEntryStyle(),
                    TrackerStyle = new BrimstoneTrackerWidgetStyle()
                };
                manager.RegisterQuest(entry);
            }

            //同步状态
            var newStatus = completed ? QuestEntryStatus.Completed : entry.Status;
            //仅在完成时强制设状态，其余由用户交互控制（关注/挂起等）
            if (completed && entry.Status != QuestEntryStatus.Completed) {
                var old = entry.Status;
                entry.Status = QuestEntryStatus.Completed;
                entry.Progress = 1f;
                entry.OnStatusChanged(old, QuestEntryStatus.Completed);
                manager.MarkFilterDirty();
            }
            else if (!completed && entry.Status == QuestEntryStatus.Completed) {
                //极端情况：存档回退等导致状态不一致时修正
                entry.Status = QuestEntryStatus.Active;
                entry.Progress = 0f;
                manager.MarkFilterDirty();
            }
        }
    }
}
