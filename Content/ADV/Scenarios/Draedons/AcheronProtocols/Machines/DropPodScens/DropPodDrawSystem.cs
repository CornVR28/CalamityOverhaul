using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.DropPodScens
{
    /// <summary>
    /// 空降仓演出屏幕叠加效果——仅负责绘制屏幕空间的速度线等叠加特效，
    /// 空降仓主体、尾焰、灼烧光效等已移至 <see cref="DropPodActor"/> 在世界坐标中绘制
    /// </summary>
    internal class DropPodDrawSystem : ModSystem
    {
        //由DropPodActor同步过来的数据
        private static int dropTimer;
        private static float reentryHeat;

        /// <summary>
        /// 由 <see cref="DropPodActor"/> 每帧调用，同步坠落计时和灼烧强度
        /// </summary>
        internal static void SyncDropTimer(int timer, float heat) {
            dropTimer = timer;
            reentryHeat = heat;
        }

        public override void PostUpdateEverything() {
            if (!DropPodWorld.Active) {
                dropTimer = 0;
                reentryHeat = 0f;
            }
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch) {

        }

        public override void Unload() {
            dropTimer = 0;
            reentryHeat = 0f;
        }
    }
}
