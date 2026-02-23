using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors.States
{
    /// <summary>
    /// 飞行状态——阿波利娅跳跃/飞行越过障碍物或深坑，使用单帧Jump纹理。
    /// 到达目标上空或脚下重新检测到地面后着陆并切换回行走
    /// </summary>
    internal class ApolliaFlyingState : IApolliaState
    {
        private const float FlySpeed = 3.5f;
        private const float LiftSpeed = -3f;
        private const float MaxFlyHeight = 240f;
        private const int MaxFlyDuration = 180;

        private readonly Vector2 moveTarget;
        private float startY;
        private int timer;

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

            SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.4f, Pitch = 0.3f }, actor.Center);
        }

        public IApolliaState Update(ApolliaActor actor) {
            timer++;

            int dir = Math.Sign(moveTarget.X - actor.Center.X);
            if (dir == 0) dir = actor.WalkDirection;
            actor.WalkDirection = dir;

            //尾焰粒子
            actor.SpawnJetParticle();
            if (Main.rand.NextBool(2)) {
                actor.SpawnJetParticle();
            }

            //水平飞行
            actor.Position.X += FlySpeed * dir;

            //垂直：先升后降
            if (actor.Position.Y > startY - MaxFlyHeight && timer < MaxFlyDuration / 2) {
                //上升阶段
                actor.Velocity.Y = MathHelper.Lerp(actor.Velocity.Y, LiftSpeed, 0.1f);
            }
            else {
                //下降阶段——应用重力
                actor.Velocity.Y = Math.Min(actor.Velocity.Y + 0.3f, 8f);
            }

            actor.Position.Y += actor.Velocity.Y;

            //着陆检测——脚下有地面且正在下降
            if (actor.Velocity.Y > 0) {
                actor.SnapToGround();
                if (actor.OnGround) {
                    return LandAndTransition(actor);
                }
            }

            //超时保护
            if (timer >= MaxFlyDuration) {
                return LandAndTransition(actor);
            }

            //水平到达目标附近且脚下有地面
            float distX = Math.Abs(actor.Center.X - moveTarget.X);
            if (distX < 40f && actor.Velocity.Y > 0) {
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
            //着陆粒子
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

            //根据指令继续行走或切换到空闲
            return actor.CurrentCommand switch {
                HeroCommand.Hold => new ApolliaIdleState(),
                _ => new ApolliaWalkingState(),
            };
        }
    }
}
