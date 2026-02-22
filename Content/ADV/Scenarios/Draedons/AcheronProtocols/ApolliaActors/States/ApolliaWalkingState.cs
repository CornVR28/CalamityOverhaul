using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors.States
{
    /// <summary>
    /// 行走状态——阿波利娅走向玩家，运镜逐渐放大
    /// </summary>
    internal class ApolliaWalkingState : IApolliaState
    {
        private const float WalkSpeed = 1.8f;
        private const float ArrivalDistance = 60f;
        private const int WalkFrameInterval = 6;
        private const int TotalFrames = 11;

        private int frameCounter;
        private int timer;

        public void Enter(ApolliaActor actor) {
            timer = 0;
            frameCounter = 0;
            actor.FrameIndex = 1;

            //运镜过渡到跟踪模式
            actor.Camera.PositionLerpSpeed = 0.025f;
        }

        public IApolliaState Update(ApolliaActor actor) {
            timer++;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return null;

            Vector2 playerCenter = player.Center;
            float distToPlayer = Math.Abs(actor.Center.X - playerCenter.X);
            int dir = Math.Sign(playerCenter.X - actor.Center.X);
            if (dir == 0) dir = -1;
            actor.WalkDirection = dir;

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

            //运镜：跟踪两者中点，距离越近越放大
            Vector2 midPoint = (actor.Center + playerCenter) * 0.5f;
            actor.Camera.FocusTarget = midPoint;

            float zoomFactor = MathHelper.Clamp(1f - (distToPlayer - ArrivalDistance) / 400f, 0f, 1f);
            float eased = zoomFactor < 0.5f ? 2f * zoomFactor * zoomFactor
                : 1f - MathF.Pow(-2f * zoomFactor + 2f, 2f) / 2f;
            actor.Camera.TargetZoom = MathHelper.Lerp(1f, 1.5f, eased);
            actor.Camera.ZoomLerpSpeed = 0.015f;

            //到达
            if (distToPlayer <= ArrivalDistance) {
                return new ApolliaArrivedState();
            }

            //脚步声
            if (timer % 20 == 0) {
                SoundEngine.PlaySound(SoundID.Run with { Volume = 0.3f, Pitch = 0.4f }, actor.Center);
            }

            return null;
        }

        public void Exit(ApolliaActor actor) { }
    }
}
