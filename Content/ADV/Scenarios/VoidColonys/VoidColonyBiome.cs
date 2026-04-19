using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys
{
    /// <summary>
    /// 虚空聚落生物群系 - 当玩家处于虚空聚落维度时激活
    /// </summary>
    internal class VoidColonyBiome : ModBiome
    {
        public override int Music => -1;
        public override ModWaterStyle WaterStyle => ModContent.GetInstance<VoidWater>();
        public override int BiomeTorchItemType => TorchID.Purple;
        public override SceneEffectPriority Priority => SceneEffectPriority.Environment;
        public override bool IsBiomeActive(Player player) => VoidColony.Active;
    }
}
