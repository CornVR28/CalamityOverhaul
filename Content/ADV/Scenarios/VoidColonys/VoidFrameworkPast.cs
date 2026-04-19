using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys
{
    /// <summary>
    /// 过去虚空骨架，仅在入侵过去时由桥梁系统批量放置与清除
    /// 贴图暂时复用VoidFramework，后续再单独绘制过去风格贴图
    /// </summary>
    internal class VoidFrameworkPast : ModTile
    {
        public override string Texture => "CalamityOverhaul/Content/ADV/Scenarios/VoidColonys/VoidFramework";

        public override void SetStaticDefaults() {
            Main.tileSolid[Type] = true;
            Main.tileMergeDirt[Type] = false;
            Main.tileBlockLight[Type] = true;
            DustType = DustID.PurpleTorch;
            AddMapEntry(new Color(85, 70, 120));
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 2;
    }
}
