using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.AcheronProtocols.Machines
{
    public class MachineWaterflow : ModWaterfallStyle { }
    internal class MachineWater : ModWaterStyle
    {
        public override int ChooseWaterfallStyle() => ModContent.GetInstance<MachineWaterflow>().Slot;
        public override int GetSplashDust() => DustID.Water_Desert;
        public override int GetDropletGore() => GoreID.WaterDripDesert;
        public override Color BiomeHairColor() => Color.Yellow;
        public override byte GetRainVariant() => (byte)Main.rand.Next(3);
        public override Asset<Texture2D> GetRainTexture() => ModContent.Request<Texture2D>("DimensionalRelease/Content/Distortions/MachineRain");
    }
}
