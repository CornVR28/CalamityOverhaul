using CalamityOverhaul.Content.ADV.QuestManager;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Helen.Quest.FishoilQuest
{
    /// <summary>
    /// 鱼油委托任务条目——在任务管理器中作为标准委托管理，
    /// 追踪窗口显示收集进度，达到目标后触发提交场景
    /// </summary>
    internal class FishoilQuestEntry : QuestEntryData
    {

        public const string QuestKey = "FishoilQuest";
        public const int FishRequired = 300;

        public static LocalizedText QuestTitle { get; private set; }
        public static LocalizedText QuestSummary { get; private set; }
        public static LocalizedText QuestCategory { get; private set; }
        public static LocalizedText ProgressFormat { get; private set; }
        public static LocalizedText TrackerCollecting { get; private set; }
        public static LocalizedText TrackerReady { get; private set; }
        public static LocalizedText StatusSuspended { get; private set; }
        public static LocalizedText StatusCompleted { get; private set; }
        public static LocalizedText StatusSubmittable { get; private set; }
        public static LocalizedText StatusCollectingFormat { get; private set; }

        /// <summary>当前背包中的鱼总数（每帧刷新）</summary>
        private int currentFishCount;
        /// <summary>提交场景是否正在进行</summary>
        private bool submissionActive;

        private readonly OceanTrackerWidgetStyle oceanStyle = new();

        public FishoilQuestEntry()
            : base(QuestKey, null, null, null) {
        }

        /// <summary>
        /// 初始化本地化文本，需在 SetStaticDefaults 阶段外部调用
        /// </summary>
        public static void InitLocalization(ILocalizedModType host) {
            QuestTitle = host.GetLocalization(nameof(QuestTitle), () => "鱼油采集");
            QuestSummary = host.GetLocalization(nameof(QuestSummary), () => "收集300条普通鱼交给比目鱼，换取一瓶鱼油");
            QuestCategory = host.GetLocalization(nameof(QuestCategory), () => "比目鱼");
            ProgressFormat = host.GetLocalization(nameof(ProgressFormat), () => "{0}/{1}");
            TrackerCollecting = host.GetLocalization(nameof(TrackerCollecting), () => "还需收集 {0} 条鱼");
            TrackerReady = host.GetLocalization(nameof(TrackerReady), () => "鱼已收集完毕，请关注任务以提交");
            StatusSuspended = host.GetLocalization(nameof(StatusSuspended), () => "已挂起");
            StatusCompleted = host.GetLocalization(nameof(StatusCompleted), () => "已完成");
            StatusSubmittable = host.GetLocalization(nameof(StatusSubmittable), () => "可提交");
            StatusCollectingFormat = host.GetLocalization(nameof(StatusCollectingFormat), () => "收集中 ({0}/{1})");
        }

        /// <summary>创建并配置一个新的任务条目实例</summary>
        public static FishoilQuestEntry Create() {
            var entry = new FishoilQuestEntry {
                TitleText = QuestTitle,
                SummaryText = QuestSummary,
                CategoryText = QuestCategory,
                Priority = 10,
                IsNew = true,
            };
            entry.TrackerStyle = entry.oceanStyle;
            entry.EntryStyle = new OceanEntryStyle();
            entry.OnUnsuspended = entry.ClearSuspendedFlag;
            return entry;
        }

        private void ClearSuspendedFlag() {
            if (Main.LocalPlayer.TryGetADVSave(out var save)) {
                save.FishoilQuestSuspended = false;
            }
        }

        public override void OnUpdate() {
            //统计背包里的候选鱼数量
            Player player = Main.LocalPlayer;
            currentFishCount = 0;
            for (int i = 0; i < player.inventory.Length; i++) {
                Item item = player.inventory[i];
                if (item != null && item.stack > 0
                    && FishoilQuestScenario.CandidateFishTypes.Contains(item.type)) {
                    currentFishCount += item.stack;
                }
            }

            Progress = Math.Clamp(currentFishCount / (float)FishRequired, 0f, 1f);
            ProgressLabel ??= QuestTitle;

            //场景完成后重置标记
            if (submissionActive && !ScenarioManager.IsActive()) {
                submissionActive = false;
            }

            //当任务处于关注状态且鱼够了，自动触发提交场景
            if (Status == QuestEntryStatus.Tracked
                && currentFishCount >= FishRequired
                && !submissionActive
                && !ScenarioManager.IsActive()) {
                ScenarioManager.Reset<FishoilSubmitScenario>();
                if (ScenarioManager.Start<FishoilSubmitScenario>()) {
                    submissionActive = true;
                }
            }
        }

        public override void OnStatusChanged(QuestEntryStatus oldStatus, QuestEntryStatus newStatus) {
            //从挂起恢复为关注时，清除持久化挂起标记
            if (oldStatus == QuestEntryStatus.Suspended && newStatus == QuestEntryStatus.Tracked) {
                ClearSuspendedFlag();
                //如果鱼够了，重新触发提交场景
                if (currentFishCount >= FishRequired && !submissionActive && !ScenarioManager.IsActive()) {
                    ScenarioManager.Reset<FishoilSubmitScenario>();
                    if (ScenarioManager.Start<FishoilSubmitScenario>()) {
                        submissionActive = true;
                    }
                }
            }
        }

        public override List<string> GetTrackerDetails() {
            if (currentFishCount >= FishRequired) {
                return [TrackerReady.Value];
            }
            int remaining = FishRequired - currentFishCount;
            return [string.Format(TrackerCollecting.Value, remaining)];
        }

        public override bool DrawTrackerContent(SpriteBatch sb, Rectangle contentRect, float alpha) {
            var font = FontAssets.MouseText.Value;
            float textScale = 0.6f;
            int yOffset = 0;

            //收集进度文本
            var details = GetTrackerDetails();
            foreach (string line in details) {
                Color textColor = currentFishCount >= FishRequired
                    ? new Color(100, 255, 200) * alpha
                    : new Color(180, 230, 250) * alpha;
                Utils.DrawBorderString(sb, line,
                    new Vector2(contentRect.X, contentRect.Y + yOffset),
                    textColor, textScale);
                yOffset += (int)(font.MeasureString("A").Y * textScale) + 2;
            }

            yOffset += 4;

            //进度条
            Rectangle barRect = new(contentRect.X, contentRect.Y + yOffset, contentRect.Width, 12);
            string progressText = string.Format(ProgressFormat.Value, Math.Min(currentFishCount, FishRequired), FishRequired);
            oceanStyle.DrawWidgetProgress(sb, barRect, Progress, progressText, alpha);

            yOffset += 18;

            //底部当前状态提示
            string statusHint = Status switch {
                QuestEntryStatus.Suspended => StatusSuspended.Value,
                QuestEntryStatus.Completed => StatusCompleted.Value,
                _ => currentFishCount >= FishRequired
                    ? StatusSubmittable.Value
                    : string.Format(StatusCollectingFormat.Value, currentFishCount, FishRequired)
            };
            Color statusColor = Status switch {
                QuestEntryStatus.Suspended => new Color(160, 140, 100) * alpha,
                QuestEntryStatus.Completed => new Color(60, 220, 140) * alpha,
                _ => currentFishCount >= FishRequired
                    ? new Color(100, 255, 200) * alpha
                    : new Color(120, 200, 235) * (alpha * 0.7f)
            };
            Utils.DrawBorderString(sb, statusHint,
                new Vector2(contentRect.X, contentRect.Y + yOffset),
                statusColor, 0.5f);

            return true;
        }
    }
}
