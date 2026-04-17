using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.VoidColonys
{
    /// <summary>
    /// 虚空骨架 - 实验室深层结构材料，深色机械方块
    /// </summary>
    internal class VoidFramework : ModTile
    {
        public override void SetStaticDefaults() {
            Main.tileSolid[Type] = true;
            Main.tileMergeDirt[Type] = true;
            Main.tileBlockLight[Type] = true;
            DustType = DustID.PurpleTorch;
            AddMapEntry(new Color(60, 45, 90));
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }
}
