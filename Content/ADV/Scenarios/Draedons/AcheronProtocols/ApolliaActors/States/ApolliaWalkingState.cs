using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors.States
{
    /// <summary>
    /// 行走状态——根据指令驱动阿波利娅的移动行为：
    /// Follow：跟随玩家；Aggressive：接近最近敌人；Hold/Defensive：走向驻守点后切换到空闲
    /// 遇到墙壁或深坑时自动切换到飞行状态越过障碍
    /// </summary>
    internal class ApolliaWalkingState : IApolliaState
    {
        private const float WalkSpeed = 1.8f;
        private const float ArrivalDistance = 60f;
        private const int WalkFrameInterval = 6;
        private const int TotalFrames = 11;

        /// <summary>可选的移动目标点（Hold/Defensive用）</summary>
        private readonly Vector2? targetPosition;

        private int frameCounter;
        private int stepSoundTimer;

        public ApolliaWalkingState() { }

        /// <summary>走向指定目标点</summary>
        public ApolliaWalkingState(Vector2 target) {
            targetPosition = target;
        }

        public void Enter(ApolliaActor actor) {
            frameCounter = 0;
            stepSoundTimer = 0;
            actor.FrameIndex = 1;
            actor.UseJumpTexture = false;
        }

        public IApolliaState Update(ApolliaActor actor) {
            stepSoundTimer++;

            //确定移动目标
            Vector2 moveTarget = ResolveTarget(actor);
            float distX = Math.Abs(actor.Center.X - moveTarget.X);
            int dir = Math.Sign(moveTarget.X - actor.Center.X);
            if (dir == 0) dir = -1;
            actor.WalkDirection = dir;

            //到达判定
            if (distX <= ArrivalDistance) {
                return ResolveArrivedState(actor);
            }

            //障碍检测——遇墙或深坑时飞行越过
            if (actor.OnGround && (actor.IsWallAhead(dir) || actor.IsGapAhead(dir))) {
                return new ApolliaFlyingState(moveTarget);
            }

            //行走动画帧(帧1~10)
            frameCounter++;
            if (frameCounter >= WalkFrameInterval) {
                frameCounter = 0;
                actor.FrameIndex++;
                if (actor.FrameIndex >= TotalFrames) {
                    actor.FrameIndex = 1;
                }
            }

            //移动
            actor.Position.X += WalkSpeed * dir;

            //地面吸附
            actor.SnapToGround();

            //脚步声
            if (stepSoundTimer % 20 == 0) {
                SoundEngine.PlaySound(SoundID.Run with { Volume = 0.3f, Pitch = 0.4f }, actor.Center);
            }

            return null;
        }

        public void Exit(ApolliaActor actor) { }

        private Vector2 ResolveTarget(ApolliaActor actor) {
            if (targetPosition.HasValue) {
                return targetPosition.Value;
            }

            return actor.CurrentCommand switch {
                HeroCommand.Aggressive => FindNearestEnemyCenter(actor) ?? Main.LocalPlayer.Center,
                _ => Main.LocalPlayer.Center,
            };
        }

        private static IApolliaState ResolveArrivedState(ApolliaActor actor) {
            return actor.CurrentCommand switch {
                HeroCommand.Hold or HeroCommand.Defensive => new ApolliaIdleState(),
                _ => new ApolliaArrivedState(),
            };
        }

        private static Vector2? FindNearestEnemyCenter(ApolliaActor actor) {
            float closest = float.MaxValue;
            Vector2? result = null;
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                float dist = Vector2.DistanceSquared(actor.Center, npc.Center);
                if (dist < closest) {
                    closest = dist;
                    result = npc.Center;
                }
            }
            return result;
        }
    }
}
