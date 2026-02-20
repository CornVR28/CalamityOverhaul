using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines
{
    internal class MachineBiome : ModBiome
    {
        public override int Music => -1;
        public override ModWaterStyle WaterStyle => ModContent.GetInstance<MachineWater>();
        public override int BiomeTorchItemType => TorchID.Green;
        public override SceneEffectPriority Priority => SceneEffectPriority.Environment;
        public override string BestiaryIcon => "DimensionalRelease/Content/Distortions/DistortionBiome_Icon";
        public override string BackgroundPath => "DimensionalRelease/Content/Machines/MachineBiome_Background";
        public override string MapBackground => "DimensionalRelease/Content/Machines/MachineBiome_Background";
        public override bool IsBiomeActive(Player player) => MachineWorld.Active;
    }
}
