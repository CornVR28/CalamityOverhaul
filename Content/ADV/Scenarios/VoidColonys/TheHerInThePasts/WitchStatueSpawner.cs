using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TheHerInThePasts
{
    /// <summary>
    /// 硫火女巫留影雕像生成器
    /// 进入虚空聚落后在世界出生点附近生成唯一一尊雕像，供过去时代显现
    /// </summary>
    internal class WitchStatueSpawner : ModSystem
    {
        //距离出生点的水平偏移，像素
        private const float OffsetX = 480f;

        public override void PostUpdateEverything() {
            if (Main.dedServ) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (!VoidColony.Active) return;

            //已有实例则不再生成
            if (ActorLoader.GetActiveActors<WitchStatueActor>().Count > 0) return;

            //使用世界出生点作为基准位置
            int spawnTileX = Main.spawnTileX;
            int spawnTileY = Main.spawnTileY;
            Vector2 basePos = new Vector2(spawnTileX * 16f + OffsetX, spawnTileY * 16f);

            //向下吸附到地面：找到第一个实心物块
            int probeY = spawnTileY;
            int guard = 0;
            while (guard++ < 200) {
                if (WorldGen.SolidTile(spawnTileX + (int)(OffsetX / 16f), probeY)) break;
                probeY++;
            }
            basePos.Y = probeY * 16f - 4f;

            ActorLoader.NewActor<WitchStatueActor>(basePos, Vector2.Zero);
        }
    }
}
