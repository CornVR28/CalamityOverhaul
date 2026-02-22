using InnoVault.GameSystem;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Graphics;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.LandingScens
{
    /// <summary>
    /// 机械世界空降仓着陆演出——玩家覆写
    /// 在着陆阶段隐藏玩家绘制，锁定玩家操控，直到玩家从空降仓弹出
    /// </summary>
    internal class MachineWorldLandingPlayer : PlayerOverride
    {
        /// <summary>
        /// 着陆演出是否处于激活状态（玩家尚未弹出）
        /// </summary>
        public bool LandingActive;

        /// <summary>
        /// 弹出动画是否正在播放
        /// </summary>
        public bool EjectAnimating;

        /// <summary>
        /// 弹出动画计时器
        /// </summary>
        public int EjectTimer;

        /// <summary>
        /// 玩家是否在本帧按下了左键（在清除controlUseItem之前捕获）
        /// 供MachineWorldLandingActor读取
        /// </summary>
        public bool ClickedThisFrame;

        /// <summary>
        /// 弹出动画总时长
        /// </summary>
        private const int EjectDuration = 60;

        /// <summary>
        /// 弹出速度
        /// </summary>
        private Vector2 ejectVelocity;

        /// <summary>
        /// 弹出起始位置
        /// </summary>
        private Vector2 ejectStartPos;

        public override void PostUpdate() {
            if (!MachineWorld.Active) {
                if (LandingActive || EjectAnimating) {
                    LandingActive = false;
                    EjectAnimating = false;
                    EjectTimer = 0;
                }
                return;
            }

            if (LandingActive && !EjectAnimating) {
                //在清除控制之前捕获点击状态
                ClickedThisFrame = Player.controlUseItem;
                //锁定玩家位置和速度，禁止移动
                Player.velocity = Vector2.Zero;
                Player.fallStart = (int)(Player.position.Y / 16f);
                //禁止玩家操作
                Player.controlLeft = false;
                Player.controlRight = false;
                Player.controlUp = false;
                Player.controlDown = false;
                Player.controlJump = false;
                Player.controlUseItem = false;
            }
            else {
                ClickedThisFrame = false;
            }

            if (EjectAnimating) {
                EjectTimer++;

                float progress = (float)EjectTimer / EjectDuration;
                float easedProgress = CWRUtils.EaseOutCubic(progress);

                //弹出阶段：玩家从空降仓位置向上弹射
                if (EjectTimer <= EjectDuration / 2) {
                    //上升阶段
                    float upProgress = (float)EjectTimer / (EjectDuration / 2f);
                    float easedUp = CWRUtils.EaseOutCubic(upProgress);
                    Player.position = ejectStartPos + ejectVelocity * easedUp * 0.5f;
                    Player.velocity = ejectVelocity * (1f - easedUp * 0.8f);
                    Player.fallStart = (int)(Player.position.Y / 16f);
                }
                else {
                    //恢复阶段：让物理引擎接管
                    float fadeProgress = (float)(EjectTimer - EjectDuration / 2) / (EjectDuration / 2f);
                    if (fadeProgress < 0.3f) {
                        Player.fallStart = (int)(Player.position.Y / 16f);
                    }
                }

                if (EjectTimer >= EjectDuration) {
                    EjectAnimating = false;
                    LandingActive = false;
                    EjectTimer = 0;
                }
            }
        }

        /// <summary>
        /// 触发弹出动画
        /// </summary>
        public void TriggerEject(Vector2 podCenter) {
            if (!LandingActive || EjectAnimating) return;

            EjectAnimating = true;
            EjectTimer = 0;
            //将玩家移动到空降仓中心位置（避免被物块阻挡）
            Player.position = podCenter - Player.Size / 2f;
            ejectStartPos = Player.position;
            //向上弹射，带轻微随机水平偏移
            ejectVelocity = new Vector2(
                Main.rand.NextFloat(-3f, 3f),
                -14f);
        }

        /// <summary>
        /// 隐藏玩家绘制——在着陆阶段玩家不可见
        /// </summary>
        public override bool PreDrawPlayers(ref Camera camera, ref IEnumerable<Player> players) {
            if (LandingActive && !EjectAnimating) {
                players = players.Where(p => p.whoAmI != Player.whoAmI);
            }
            return true;
        }
    }
}
