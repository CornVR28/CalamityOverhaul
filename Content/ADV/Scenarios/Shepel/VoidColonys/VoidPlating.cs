using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.VoidColonys
{
    /// <summary>
    /// 虚空镀层 - 实验室主要建筑材料，浅色机械方块
    /// </summary>
    internal class VoidPlating : ModTile
    {
        public override void SetStaticDefaults() {
            Main.tileSolid[Type] = true;
            Main.tileMergeDirt[Type] = true;
            Main.tileBlockLight[Type] = true;
            DustType = DustID.PurpleTorch;
            AddMapEntry(new Color(120, 100, 160));
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }
}
