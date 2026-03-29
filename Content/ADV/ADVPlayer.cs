using CalamityOverhaul.Content.ADV.Scenarios;
using CalamityOverhaul.Content.ADV.Scenarios.Abysses.OldDukes.Campsites;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV
{
    internal class ADVPlayer : ModPlayer
    {
        public override void OnEnterWorld() {
            OldDukeCampsite.RequestOldDukeCampsiteData();
        }

        public override void PostUpdate() {
            if (Player.whoAmI != Main.myPlayer) {
                return;
            }

            //关于ADV场景的更新只在本地玩家上进行
            var advSave = Player.GetModPlayer<ADVSavePlayer>().ADVSave;
            foreach (var scenario in ADVScenarioBase.Instances) {
                scenario.Update(advSave, Player);
            }
        }
    }
}
