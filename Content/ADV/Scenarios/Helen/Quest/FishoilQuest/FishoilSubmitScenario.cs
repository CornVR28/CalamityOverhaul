using CalamityOverhaul.Content.ADV.ADVChoices;
using CalamityOverhaul.Content.ADV.ADVRewardPopups;
using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.ADV.DialogueBoxs.Styles;
using CalamityOverhaul.Content.ADV.QuestManager;
using CalamityOverhaul.Content.Items.Tools;
using CalamityOverhaul.Content.LegendWeapon.HalibutLegend;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Helen.Quest.FishoilQuest
{
    /// <summary>
    /// 鱼油提交场景——当玩家收集够300条鱼后触发，
    /// 比目鱼询问是否交鱼换油
    /// </summary>
    internal class FishoilSubmitScenario : ADVScenarioBase, ILocalizedModType
    {
        public static LocalizedText Rolename { get; private set; }
        public static LocalizedText SubmitLine1 { get; private set; }
        public static LocalizedText SubmitLine2 { get; private set; }
        public static LocalizedText QuestionLine { get; private set; }
        public static LocalizedText ChoiceGive { get; private set; }
        public static LocalizedText ChoiceRefuse { get; private set; }
        public static LocalizedText GiveResponse { get; private set; }
        public static LocalizedText RefuseResponse { get; private set; }

        private const string enjoy = " ";

        protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => SeaDialogueBox.Instance;

        public override void SetStaticDefaults() {
            Rolename = this.GetLocalization(nameof(Rolename), () => "比目鱼");
            SubmitLine1 = this.GetLocalization(nameof(SubmitLine1), () => "嗯，你收集到了足够的鱼");
            SubmitLine2 = this.GetLocalization(nameof(SubmitLine2), () => "正好够我提炼一瓶鱼油的量");
            QuestionLine = this.GetLocalization(nameof(QuestionLine), () => "你要把它们交给我吗？");
            ChoiceGive = this.GetLocalization(nameof(ChoiceGive), () => "好，拿去吧");
            ChoiceRefuse = this.GetLocalization(nameof(ChoiceRefuse), () => "我再想想");
            GiveResponse = this.GetLocalization(nameof(GiveResponse), () => "很好，稍等...给你，一瓶新鲜的鱼油");
            RefuseResponse = this.GetLocalization(nameof(RefuseResponse), () => "好吧，什么时候想好了再来找我");

            //委托任务条目的本地化也在此初始化
            FishoilQuestEntry.InitLocalization(this);
        }

        protected override void Build() {
            DialogueBoxBase.RegisterPortrait(Rolename.Value, ADVAsset.HelenADV);
            DialogueBoxBase.SetPortraitStyle(Rolename.Value, silhouette: false);
            DialogueBoxBase.RegisterPortrait(Rolename.Value + enjoy, ADVAsset.Helen_enjoyADV);
            DialogueBoxBase.SetPortraitStyle(Rolename.Value + enjoy, silhouette: false);

            Add(Rolename.Value, SubmitLine1.Value);
            Add(Rolename.Value + enjoy, SubmitLine2.Value);

            AddWithChoices(Rolename.Value, QuestionLine.Value, new List<Choice> {
                new Choice(ChoiceGive.Value, OnGive),
                new Choice(ChoiceRefuse.Value, OnRefuse),
            }, choiceBoxStyle: ADVChoiceBox.ChoiceBoxStyle.Default);
        }

        private void OnGive() {
            ScenarioManager.Reset<FishoilSubmit_Give>();
            ScenarioManager.Start<FishoilSubmit_Give>();
            Complete();
        }

        /// <summary>
        /// 交鱼子场景——消耗鱼、发放鱼油、标记完成
        /// </summary>
        private class FishoilSubmit_Give : ADVScenarioBase
        {
            public override string Key => nameof(FishoilSubmit_Give);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => SeaDialogueBox.Instance;

            protected override void Build() {
                Add(Rolename.Value + enjoy, GiveResponse.Value);
            }

            protected override void OnScenarioComplete() {
                Player player = Main.LocalPlayer;

                int availableFish = 0;
                for (int i = 0; i < player.inventory.Length; i++) {
                    Item item = player.inventory[i];
                    if (item != null && item.stack > 0 && FishoilQuestScenario.CandidateFishTypes.Contains(item.type)) {
                        availableFish += item.stack;
                        if (availableFish >= FishoilQuestEntry.FishRequired) {
                            break;
                        }
                    }
                }
                if (availableFish < FishoilQuestEntry.FishRequired) {
                    return;
                }

                //从背包中消耗300条候选鱼
                int remaining = FishoilQuestEntry.FishRequired;
                for (int i = 0; i < player.inventory.Length && remaining > 0; i++) {
                    Item item = player.inventory[i];
                    if (item == null || item.stack <= 0) continue;
                    if (!FishoilQuestScenario.CandidateFishTypes.Contains(item.type)) continue;
                    int consume = Math.Min(remaining, item.stack);
                    item.stack -= consume;
                    remaining -= consume;
                    if (item.stack <= 0) {
                        item.TurnToAir();
                    }
                }

                //发放鱼油奖励
                int fishoilType = ModContent.ItemType<Fishoil>();
                ADVRewardPopup.ShowReward(fishoilType, 5, "", appearDuration: 24, holdDuration: -1, giveDuration: 16, requireClick: true,
                    anchorProvider: () => {
                        var rect = DialogueUIRegistry.Current?.GetPanelRect() ?? Rectangle.Empty;
                        if (rect == Rectangle.Empty) {
                            return new Vector2(Main.screenWidth / 2f, Main.screenHeight * 0.45f);
                        }
                        return new Vector2(rect.Center.X, rect.Y - 70f);
                    }, offset: Vector2.Zero);

                //标记任务完成
                if (player.TryGetOverride<HalibutPlayer>(out var hp)) {
                    hp.ADVSave.FishoilQuestCompleted = true;
                    hp.ADVSave.FishoilQuestSuspended = false;
                }

                //更新委托管理器中的条目状态
                var manager = QuestManagerUI.Instance;
                var entry = manager?.GetEntry(FishoilQuestEntry.QuestKey);
                if (entry != null) {
                    entry.Status = QuestEntryStatus.Completed;
                    manager.MarkFilterDirty();
                }
            }
        }

        private void OnRefuse() {
            ScenarioManager.Reset<FishoilSubmit_Refuse>();
            ScenarioManager.Start<FishoilSubmit_Refuse>();
            Complete();
        }

        /// <summary>
        /// 拒绝子场景——将任务设为挂起
        /// </summary>
        private class FishoilSubmit_Refuse : ADVScenarioBase
        {
            public override string Key => nameof(FishoilSubmit_Refuse);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => SeaDialogueBox.Instance;

            protected override void Build() {
                Add(Rolename.Value, RefuseResponse.Value);
            }

            protected override void OnScenarioComplete() {
                //将任务设为挂起
                var manager = QuestManagerUI.Instance;
                var entry = manager?.GetEntry(FishoilQuestEntry.QuestKey);
                if (entry != null) {
                    var old = entry.Status;
                    entry.Status = QuestEntryStatus.Suspended;
                    if (old != entry.Status) {
                        entry.OnStatusChanged(old, entry.Status);
                    }
                    manager.MarkFilterDirty();
                }
                //持久化挂起状态
                if (Main.LocalPlayer.TryGetOverride<HalibutPlayer>(out var hp)) {
                    hp.ADVSave.FishoilQuestSuspended = true;
                }
            }
        }
    }
}
