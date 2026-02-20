using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines
{
    public class MechanizedWood : ModItem
    {
        public override void SetDefaults() {
            Item.width = Item.height = 32;
            Item.rare = ItemRarityID.Green;
            Item.maxStack = 9999;
        }
    }
}
