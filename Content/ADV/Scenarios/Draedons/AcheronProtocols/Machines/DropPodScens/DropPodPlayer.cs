using InnoVault.Actors;
using InnoVault.GameSystem;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Graphics;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.DropPodScens
{
    /// <summary>
    /// 空降仓演出玩家覆写——在空降仓子世界中隐藏玩家绘制，管理坠落状态，
    /// 并生成 <see cref="DropPodActor"/> 实体来处理空降仓的世界坐标逻辑和绘制
    /// </summary>
    internal class DropPodPlayer : PlayerOverride
    {
        /// <summary>
        /// 坠落演出是否激活
        /// </summary>
        public bool DropPodActive;
        /// <summary>
        /// 坠落累计计时器
        /// </summary>
        public int DropTimer;
        public override void PostUpdate() {
            if (!DropPodWorld.Active) {
                if (DropPodActive) {
                    DropPodActive = false;
                    DropTimer = 0;
                }
                return;
            }

            DropPodActive = true;
            DropTimer++;

            //锁定玩家位置在世界中央，无重力
            Player.position = new Vector2(
                DropPodWorld.Instance.Width * 16 / 2f - Player.width / 2f,
                DropPodWorld.Instance.Height * 16 / 2f - Player.height / 2f);
            Player.velocity = Vector2.Zero;
            Player.fallStart = (int)(Player.position.Y / 16f);
        }

        /// <summary>
        /// 隐藏玩家绘制——参考HalibutPlayer和CrabulonPlayer的模式
        /// </summary>
        public override bool PreDrawPlayers(ref Camera camera, ref IEnumerable<Player> players) {
            if (!DropPodActive) return true;

            //移除本玩家的绘制
            players = players.Where(p => p.whoAmI != Player.whoAmI);
            return true;
        }
    }
}
