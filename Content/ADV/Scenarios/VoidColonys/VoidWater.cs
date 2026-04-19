using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys
{
    public class VoidWaterflow : ModWaterfallStyle { }

    /// <summary>
    /// 虚空聚落水体样式 - 带有亚空间能量色调的液体
    /// </summary>
    internal class VoidWater : ModWaterStyle
    {
        public override int ChooseWaterfallStyle() => ModContent.GetInstance<VoidWaterflow>().Slot;
        public override int GetSplashDust() => DustID.PurpleTorch;
        public override int GetDropletGore() => GoreID.WaterDripCorrupt;
        public override Color BiomeHairColor() => new Color(140, 80, 200);
        public override byte GetRainVariant() => (byte)Main.rand.Next(3);
    }
}
