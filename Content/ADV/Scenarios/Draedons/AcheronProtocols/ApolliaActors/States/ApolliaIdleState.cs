using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors.States
{
    /// <summary>
    /// 空闲/驻守状态——阿波利娅站在原地，面向玩家。
    /// 当指令切换为Follow或Aggressive时重新进入行走
    /// </summary>
    internal class ApolliaIdleState : IApolliaState
    {
        public void Enter(ApolliaActor actor) {
            actor.FrameIndex = 0;
            actor.UseJumpTexture = false;
        }

        public IApolliaState Update(ApolliaActor actor) {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return null;

            //面向玩家
            int dir = Math.Sign(player.Center.X - actor.Center.X);
            if (dir == 0) dir = -1;
            actor.WalkDirection = dir;

            //地面吸附
            actor.SnapToGround();

            //指令变更时切换状态
            switch (actor.CurrentCommand) {
                case HeroCommand.Follow:
                    float dist = Math.Abs(actor.Center.X - player.Center.X);
                    if (dist > 80f) {
                        return new ApolliaWalkingState();
                    }
                    break;

                case HeroCommand.Aggressive:
                    return new ApolliaWalkingState();

                //Hold/Defensive 保持空闲
            }

            return null;
        }

        public void Exit(ApolliaActor actor) { }
    }
}
