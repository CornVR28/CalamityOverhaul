using CalamityOverhaul.Content.ADV.MainMenuOvers;
using CalamityOverhaul.Content.ADV.Scenarios;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.ADV
{
    /// <summary>
    /// ADV系统专用的玩家存档ModPlayer，负责ADVSave数据和场景数据的保存与加载
    /// </summary>
    internal class ADVSavePlayer : ModPlayer
    {
        public ADVSave ADVSave { get; private set; } = new();

        public override void SaveData(TagCompound tag) {
            try {
                tag["ADVSave"] = ADVSave.SaveData();

                if (ADVSave.EternalBlazingNow) {
                    MenuSave.UnlockEternalBlazingNowPortrait(Player);
                }

                foreach (var scenario in ADVScenarioBase.Instances) {
                    scenario.SaveData(tag);
                }
            } catch { }
        }

        public override void LoadData(TagCompound tag) {
            try {
                if (tag.TryGet<TagCompound>("ADVSave", out var advTag)) {
                    ADVSave.LoadData(advTag);
                }

                if (ADVSave.EternalBlazingNow) {
                    MenuSave.UnlockEternalBlazingNowPortrait(Player);
                }

                foreach (var scenario in ADVScenarioBase.Instances) {
                    scenario.LoadData(tag);
                }
            } catch { }
        }

        /// <summary>
        /// 从旧版HalibutSave迁移ADV数据（向后兼容）
        /// </summary>
        internal void MigrateFromLegacy(TagCompound adcTag, TagCompound fullTag) {
            ADVSave.LoadData(adcTag);

            if (ADVSave.EternalBlazingNow) {
                MenuSave.UnlockEternalBlazingNowPortrait(Player);
            }

            foreach (var scenario in ADVScenarioBase.Instances) {
                scenario.LoadData(fullTag);
            }
        }
    }
}
