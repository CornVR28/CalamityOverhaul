using System;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors.States
{
    /// <summary>
    /// 飞行状态——阿波利娅跳跃/飞行越过障碍物或深坑，使用单帧Jump纹理。
    /// 水平方向在起飞时锁定，避免穿越目标时来回抖动。
    /// 着陆需要最小滞空时间和下降速度，避免碰到物块边缘时反复起降
    /// </summary>
    internal class ApolliaFlyingState : IApolliaState
    {
        private const float FlySpeed = 3.5f;
        private const float LiftSpeed = -3f;
        private const float MaxFlyHeight = 240f;
        private const int MaxFlyDuration = 180;
        private const int MinAirTime = 25;
        private const float MinLandingFallSpeed = 1f;

        private readonly Vector2 moveTarget;
        private float startY;
        private int timer;
        private int flyDir;

        /// <param name="target">飞行要趋近的世界坐标目标点</param>
        public ApolliaFlyingState(Vector2 target) {
            moveTarget = target;
        }

        public void Enter(ApolliaActor actor) {
            timer = 0;
            startY = actor.Position.Y;
            actor.UseJumpTexture = true;
            actor.OnGround = false;
            actor.Velocity = new Vector2(0, LiftSpeed);
            actor.JetTrailActive = true;

            //起飞时锁定水平方向，避免穿越目标时来回拍打
            flyDir = Math.Sign(moveTarget.X - actor.Center.X);
            if (flyDir == 0) flyDir = actor.WalkDirection;

            actor.WalkDirection = flyDir;

            SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.4f, Pitch = 0.3f }, actor.Center);
        }

        public IApolliaState Update(ApolliaActor actor) {
            timer++;

            //方向始终使用起飞时锁定的方向
            actor.WalkDirection = flyDir;

            //尾焰粒子
            actor.SpawnJetParticle();
            if (Main.rand.NextBool(2)) {
                actor.SpawnJetParticle();
            }

            //水平飞行
            actor.Position.X += FlySpeed * flyDir;

            //垂直：先升后降
            if (actor.Position.Y > startY - MaxFlyHeight && timer < MaxFlyDuration / 2) {
                actor.Velocity.Y = MathHelper.Lerp(actor.Velocity.Y, LiftSpeed, 0.1f);
            }
            else {
                actor.Velocity.Y = Math.Min(actor.Velocity.Y + 0.3f, 8f);
            }

            actor.Position.Y += actor.Velocity.Y;

            //超时强制着陆
            if (timer >= MaxFlyDuration) {
                return LandAndTransition(actor);
            }

            //着陆检测——必须同时满足：最小滞空时间、正在下降且速度足够
            if (timer >= MinAirTime && actor.Velocity.Y >= MinLandingFallSpeed) {
                actor.SnapToGround();
                if (actor.OnGround) {
                    return LandAndTransition(actor);
                }
            }

            return null;
        }

        public void Exit(ApolliaActor actor) {
            actor.UseJumpTexture = false;
            actor.Velocity = Vector2.Zero;
            actor.StopJetTrail();
        }

        private static IApolliaState LandAndTransition(ApolliaActor actor) {
            if (!VaultUtils.isServer) {
                for (int i = 0; i < 4; i++) {
                    Vector2 vel = new(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-2f, -0.5f));
                    Dust dust = Dust.NewDustDirect(
                        actor.Center + new Vector2(Main.rand.NextFloat(-8, 8), 20),
                        1, 1, DustID.Smoke, vel.X, vel.Y, 150, default, Main.rand.NextFloat(0.6f, 1f));
                    dust.noGravity = true;
                }
            }

            SoundEngine.PlaySound(SoundID.Run with { Volume = 0.25f, Pitch = 0.2f }, actor.Center);

            return actor.CurrentCommand switch {
                HeroCommand.Hold => new ApolliaIdleState(),
                _ => new ApolliaWalkingState(),
            };
        }
    }
}
