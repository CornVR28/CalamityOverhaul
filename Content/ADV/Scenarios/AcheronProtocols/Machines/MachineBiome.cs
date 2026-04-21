using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.AcheronProtocols.Machines
{
    internal class MachineBiome : ModBiome
    {
        public override int Music => -1;
        public override ModWaterStyle WaterStyle => ModContent.GetInstance<MachineWater>();
        public override int BiomeTorchItemType => TorchID.Green;
        public override SceneEffectPriority Priority => SceneEffectPriority.Environment;
        public override bool IsBiomeActive(Player player) => MachineWorld.Active;
    }
}
