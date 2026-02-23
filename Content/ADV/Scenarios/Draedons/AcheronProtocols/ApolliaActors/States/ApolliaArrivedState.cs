using System;
using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors.States
{
    /// <summary>
    /// 到达状态——阿波利娅站在目标附近，根据指令决定后续行为：
    /// Follow：玩家远离时重新行走；
    /// Hold/Defensive：切换到空闲；
    /// Aggressive：有敌人时重新行走追敌
    /// </summary>
    internal class ApolliaArrivedState : IApolliaState
    {
        private const float ReWalkDistance = 120f;

        public void Enter(ApolliaActor actor) {
            actor.FrameIndex = 0;
            actor.UseJumpTexture = false;

            if (actor.InCutscene) {
                actor.Camera.TargetZoom = 2f;
                actor.Camera.ZoomLerpSpeed = 0.02f;
                actor.Camera.PositionLerpSpeed = 0.04f;
            }
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

            //演出运镜：焦点在两者中点偏上
            if (actor.InCutscene) {
                Vector2 midPoint = (actor.Center + player.Center) * 0.5f + new Vector2(0, -20);
                actor.Camera.FocusTarget = midPoint;
            }

            switch (actor.CurrentCommand) {
                case HeroCommand.Hold:
                case HeroCommand.Defensive:
                    return new ApolliaIdleState();

                case HeroCommand.Aggressive:
                    //有敌人时追击
                    if (HasNearbyEnemy(actor)) {
                        return new ApolliaWalkingState();
                    }
                    break;

                default: //Follow
                    float distToPlayer = Math.Abs(actor.Center.X - player.Center.X);
                    if (distToPlayer > ReWalkDistance) {
                        return new ApolliaWalkingState();
                    }
                    break;
            }

            return null;
        }

        public void Exit(ApolliaActor actor) { }

        private static bool HasNearbyEnemy(ApolliaActor actor) {
            float range = 800f * 800f;
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                if (Vector2.DistanceSquared(actor.Center, npc.Center) < range) return true;
            }
            return false;
        }
    }
}
