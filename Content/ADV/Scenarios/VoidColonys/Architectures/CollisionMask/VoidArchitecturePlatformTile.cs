using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.CollisionMask
{
    /// <summary>
    /// 虚空聚落建筑的隐形平台碰撞方块
    /// 作为可停留可穿过的walkable deck，用于连接桥与部分建筑的阳台或走廊
    /// 原理上是Terraria原生平台，继承所有下跳、上穿、抓钩挂接行为
    /// </summary>
    internal class VoidArchitecturePlatformTile : ModTile
    {
        public override string Texture => CWRConstant.Placeholder;

        public override void SetStaticDefaults() {
            //参考原版木平台的定义：solidTop + framedImportant + platform sets
            Main.tileSolidTop[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileTable[Type] = true;
            Main.tileLavaDeath[Type] = false;
            Main.tileFrameImportant[Type] = true;
            Main.tileLighted[Type] = false;
            Main.tileBlockLight[Type] = false;
            Main.tileNoFail[Type] = true;

            TileID.Sets.Platforms[Type] = true;
            TileID.Sets.AvoidedByMeteorLanding[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.StyleAlch);
            TileObjectData.newTile.CoordinateHeights = [16];
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 0;
            TileObjectData.newTile.StyleWrapLimit = 27;
            TileObjectData.newTile.StyleMultiplier = 27;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.Origin = new Point16(0, 0);
            TileObjectData.addTile(Type);

            MinPick = int.MaxValue;
            DustType = -1;
            HitSound = null;
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => false;

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch) { }

        public override bool CanExplode(int i, int j) => false;

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) {
            blockDamaged = false;
            return false;
        }
    }
}
