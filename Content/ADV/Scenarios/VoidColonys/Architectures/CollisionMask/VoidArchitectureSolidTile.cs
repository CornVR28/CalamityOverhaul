using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.CollisionMask
{
    /// <summary>
    /// 虚空聚落建筑的隐形实心碰撞方块
    /// 仅用于构建Actor贴图区域的物理碰撞，永不显示、永不掉落、不可破坏
    /// 由<see cref="ArchitectureTilePlacer"/>在过去时代显现期间代为放置与清除
    /// </summary>
    internal class VoidArchitectureSolidTile : ModTile
    {
        //复用模组通用透明占位图，保证Tile资源能正确加载但本身不显像
        public override string Texture => CWRConstant.Placeholder;

        public override void SetStaticDefaults() {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = false;
            Main.tileLighted[Type] = false;
            Main.tileFrameImportant[Type] = false;
            Main.tileMergeDirt[Type] = false;
            Main.tileNoAttach[Type] = true;
            Main.tileNoFail[Type] = true;
            //使用默认坚不可摧，避免玩家误挖破坏建筑轮廓
            MinPick = int.MaxValue;
            TileID.Sets.DrawsWalls[Type] = false;
            TileID.Sets.AvoidedByMeteorLanding[Type] = true;
            DustType = -1;
            HitSound = null;
        }

        //建筑本体由Actor + shader负责绘制，此Tile彻底不参与绘制
        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => false;

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch) { }

        public override bool CanExplode(int i, int j) => false;

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) {
            blockDamaged = false;
            return false;
        }

        public override bool Slope(int i, int j) => false;
    }
}
