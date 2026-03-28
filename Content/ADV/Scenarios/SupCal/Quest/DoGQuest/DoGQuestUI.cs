using CalamityOverhaul.Content.ADV.ADVQuestTracker;
using CalamityOverhaul.Content.Items.Melee;
using InnoVault.UIHandles;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.SupCal.Quest.DoGQuest
{
    /// <summary>
    /// 神明吞噬者任务UI
    /// </summary>
    internal class DoGQuestUI : BaseQuestAcceptUI
    {
        public override string LocalizationCategory => "ADV";
        public static DoGQuestUI Instance => UIHandleLoader.GetUIHandleOfType<DoGQuestUI>();

        protected override void SetupLocalizedTexts() {
            QuestTitle = this.GetLocalization(nameof(QuestTitle), () => "委托：神明吞噬者");
            QuestDesc = this.GetLocalization(nameof(QuestDesc), () => "使用刻心者击杀神明吞噬者");
            AcceptText = this.GetLocalization(nameof(AcceptText), () => "接受");
            DeclineText = this.GetLocalization(nameof(DeclineText), () => "拒绝");
        }

        protected override bool ShouldShowQuest() {
            if (!Main.LocalPlayer.TryGetADVSave(out var save)) {
                return false;
            }

            //如果玩家已经接受/拒绝/完成了任务，就不再显示UI
            if (save.SupCalDoGQuestReward
                || save.SupCalDoGQuestAccepted
                || save.SupCalDoGQuestDeclined) {
                return false;
            }

            //前置任务必须完成
            if (!save.SupCalQuestReward) {
                return false;
            }

            Item heldItem = Main.LocalPlayer.GetItem();
            return heldItem.type == ModContent.ItemType<Heartcarver>()
                && save.SupCalQuestReward
                && !save.SupCalDoGQuestReward
                && !save.SupCalDoGQuestDeclined;
        }

        protected override void OnQuestAccepted() {
            if (Main.LocalPlayer.TryGetADVSave(out var save)) {
                save.SupCalDoGQuestAccepted = true;
            }
        }

        protected override void OnQuestDeclined() {
            if (Main.LocalPlayer.TryGetADVSave(out var save)) {
                save.SupCalDoGQuestDeclined = true;
            }
        }
    }
}
