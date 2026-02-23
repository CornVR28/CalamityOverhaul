using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors.States
{
    /// <summary>
    /// 到达状态——阿波利娅站在玩家面前
    /// 若玩家远离则重新进入行走状态
    /// </summary>
    internal class ApolliaArrivedState : IApolliaState
    {
        /// <summary>玩家离阿波利娅超过此距离时重新进入行走</summary>
        private const float ReWalkDistance = 120f;

        public void Enter(ApolliaActor actor) {
            actor.FrameIndex = 0;

            //运镜聚焦在两者中点偏上，保持2x缩放
            actor.Camera.TargetZoom = 2f;
            actor.Camera.ZoomLerpSpeed = 0.02f;
            actor.Camera.PositionLerpSpeed = 0.04f;
        }

        public IApolliaState Update(ApolliaActor actor) {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return null;

            float distToPlayer = Math.Abs(actor.Center.X - player.Center.X);

            //面向玩家
            int dir = Math.Sign(player.Center.X - actor.Center.X);
            if (dir == 0) dir = -1;
            actor.WalkDirection = dir;

            //运镜焦点：两者中点偏上
            Vector2 midPoint = (actor.Center + player.Center) * 0.5f + new Vector2(0, -20);
            actor.Camera.FocusTarget = midPoint;

            //玩家远离时重新进入行走
            if (distToPlayer > ReWalkDistance) {
                return new ApolliaWalkingState();
            }

            return null;
        }

        public void Exit(ApolliaActor actor) { }
    }
}
