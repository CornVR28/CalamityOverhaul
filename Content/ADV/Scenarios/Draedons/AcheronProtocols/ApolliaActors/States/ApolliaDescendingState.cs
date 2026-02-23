using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors.States
{
    /// <summary>
    /// 从天而降状态——阿波利娅从高空降落到地面
    /// </summary>
    internal class ApolliaDescendingState : IApolliaState
    {
        private const int Duration = 50;

        private Vector2 startPos;
        private Vector2 targetPos;
        private int timer;

        /// <param name="target">降落目标地面位置</param>
        public ApolliaDescendingState(Vector2 target) {
            targetPos = target;
            startPos = target - new Vector2(0, 800);
        }

        public void Enter(ApolliaActor actor) {
            timer = 0;
            actor.Position = startPos;
            actor.FrameIndex = 0;
            actor.DescendAlpha = 0f;
            actor.GlowIntensity = 0f;

            //启动运镜——参数由 CutsceneCamera.UpdateFocus 每帧自动推导
            actor.Camera.Start(targetPos, posLerp: 0.03f, zoom: 1f, zoomLerp: 0.02f);
        }

        public IApolliaState Update(ApolliaActor actor) {
            timer++;
            float progress = MathHelper.Clamp((float)timer / Duration, 0f, 1f);
            float eased = 1f - (1f - progress) * (1f - progress); //EaseOutQuad

            //位置插值
            actor.Position = Vector2.Lerp(startPos, targetPos, eased);

            //淡入
            actor.DescendAlpha = MathHelper.Clamp(progress * 3f, 0f, 1f);

            //降落粒子
            if (timer % 3 == 0 && !VaultUtils.isServer) {
                Vector2 dustPos = actor.Center + Main.rand.NextVector2Circular(15, 8);
                Dust dust = Dust.NewDustDirect(dustPos, 1, 1, DustID.Electric, 0, 2, 150, default, 0.6f);
                dust.noGravity = true;
                dust.velocity *= 0.4f;
            }

            if (progress >= 1f) {
                //着陆效果
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = 0.2f }, actor.Center);

                if (!VaultUtils.isServer) {
                    for (int i = 0; i < 12; i++) {
                        Vector2 vel = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-4f, -1f));
                        Dust dust = Dust.NewDustDirect(
                            actor.Center + new Vector2(Main.rand.NextFloat(-15, 15), 20),
                            1, 1, DustID.Smoke, vel.X, vel.Y, 150, default, Main.rand.NextFloat(1f, 1.8f));
                        dust.noGravity = true;
                    }
                }

                actor.GlowIntensity = 0.8f;
                actor.DescendAlpha = 1f;

                //着陆后根据指令决定下一状态
                return actor.CurrentCommand switch {
                    HeroCommand.Hold or HeroCommand.Defensive => new ApolliaIdleState(),
                    _ => new ApolliaWalkingState(),
                };
            }

            return null;
        }

        public void Exit(ApolliaActor actor) { }
    }
}
