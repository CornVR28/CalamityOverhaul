using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Skills.Sandevistans
{
    /// <summary>
    /// 斯安威斯坦技能 - 赛博朋克2077的弹时间系统。
    /// 负责管理残影的生成节奏与生命周期，以及屏幕效果的强度管理。
    /// </summary>
    internal class Sandevistan : ModSystem
    {
        /// <summary>
        /// 技能是否处于激活状态
        /// </summary>
        public static bool IsActive { get; set; }

        /// <summary>
        /// 屏幕后处理效果强度（0~1），带渐入渐出过渡
        /// </summary>
        public static float ScreenEffectIntensity { get; private set; }

        private static int spawnTimer;

        private const float FadeInSpeed = 0.05f;
        private const float FadeOutSpeed = 0.03f;

        /// <summary>
        /// 每隔多少帧生成一个残影（越小残影越密集）
        /// </summary>
        public const int SpawnInterval = 3;

        /// <summary>
        /// 在玩家逻辑更新中调用，根据激活状态和玩家运动情况管理残影生成
        /// </summary>
        public static void Update(Player player) {
            //屏幕效果强度平滑渐变
            if (IsActive) {
                ScreenEffectIntensity = MathHelper.Min(ScreenEffectIntensity + FadeInSpeed, 1f);
            }
            else {
                ScreenEffectIntensity = MathHelper.Max(ScreenEffectIntensity - FadeOutSpeed, 0f);
            }

            if (!IsActive) {
                spawnTimer = 0;
                return;
            }

            //玩家基本静止时不产生残影
            if (player.velocity.LengthSquared() < 1f) {
                return;
            }

            spawnTimer++;
            if (spawnTimer >= 4) {
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

            int index = ActorLoader.NewActor<SandevistanGhostActor>(player.Center, Vector2.Zero);
            if (index >= 0) {
                ActorLoader.Actors[index].OnSpawn(player);
            }
        }
    }
}
