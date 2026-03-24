using InnoVault.Actors;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Skills.Sandevistans
{
    /// <summary>
    /// 斯安威斯坦技能 - 赛博朋克2077的弹时间系统。
    /// 负责管理残影的生成节奏与生命周期。
    /// </summary>
    internal class Sandevistan : ModSystem
    {
        /// <summary>
        /// 技能是否处于激活状态
        /// </summary>
        public static bool IsActive { get; set; }

        private static int spawnTimer;

        /// <summary>
        /// 每隔多少帧生成一个残影（越小残影越密集）
        /// </summary>
        public const int SpawnInterval = 3;

        /// <summary>
        /// 在玩家逻辑更新中调用，根据激活状态和玩家运动情况管理残影生成
        /// </summary>
        public static void Update(Player player) {
            if (!IsActive) {
                spawnTimer = 0;
                return;
            }

            // 玩家基本静止时不产生残影
            if (player.velocity.LengthSquared() < 1f) {
                return;
            }

            spawnTimer++;
            if (spawnTimer >= SpawnInterval) {
                spawnTimer = 0;
                SpawnGhost(player);
            }
        }

        public override void PostUpdatePlayers() {
            Update(Main.LocalPlayer);
        }

        /// <summary>
        /// 在玩家当前位置生成一个残影实体
        /// </summary>
        public static void SpawnGhost(Player player) {
            if (Main.dedServ) {
                return;
            }

            int index = ActorLoader.NewActor<SandevistanGhostActor>(player.Center, Microsoft.Xna.Framework.Vector2.Zero);
            if (index >= 0) {
                ActorLoader.Actors[index].OnSpawn(player);
            }
        }
    }
}
