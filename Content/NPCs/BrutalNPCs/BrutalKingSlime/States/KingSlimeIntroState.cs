using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Core;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.States
{
    /// <summary>
    /// 进场状态：从空中下坠到地面，期间持续皇家蓄力光辉，落地震屏后切到 Hop
    /// </summary>
    internal class KingSlimeIntroState : KingSlimeStateBase
    {
        public override string StateName => "Intro";
        public override KingSlimeStateIndex StateIndex => KingSlimeStateIndex.Intro;

        private const int IntroLength = 60;
        private bool startedFall;

        public override void OnEnter(KingSlimeStateContext context) {
            base.OnEnter(context);
            NPC npc = context.Npc;
            npc.alpha = 255;
            npc.dontTakeDamage = true;
            npc.velocity = Vector2.Zero;
            startedFall = false;
        }

        public override IKingSlimeState OnUpdate(KingSlimeStateContext context) {
            NPC npc = context.Npc;

            //逐步淡入并蓄力，期间维持悬浮
            if (Timer < 30) {
                npc.alpha = (int)MathHelper.Lerp(255, 0, Timer / 30f);
                npc.velocity *= 0.85f;
                context.SetChargeState(1, Timer / 30f);
            }
            else if (!startedFall) {
                //开始下坠
                startedFall = true;
                npc.alpha = 0;
                npc.velocity.Y = 6f;
                if (!VaultUtils.isServer) {
                    SoundEngine.PlaySound(SoundID.Item14, npc.Center);
                }
            }
            else {
                npc.velocity.Y = MathHelper.Min(npc.velocity.Y + 0.6f, 22f);
                context.SetChargeState(1, 1f);

                //落地：清除蓄力，切到主循环
                if (npc.collideY || (npc.velocity.Y < 0.05f && Timer > IntroLength)) {
                    if (!VaultUtils.isServer) {
                        SoundEngine.PlaySound(SoundID.NPCDeath1, npc.Center);
                    }
                    npc.dontTakeDamage = false;
                    KingSlimeRenderHelper.DoLandingShockwave(npc, context, 1.0f);
                    return new KingSlimeHopState();
                }
            }

            Timer++;

            //超时保护
            if (Timer > 240) {
                npc.dontTakeDamage = false;
                return new KingSlimeHopState();
            }
            return null;
        }

        public override void OnExit(KingSlimeStateContext context) {
            base.OnExit(context);
            context.Npc.dontTakeDamage = false;
            context.Npc.alpha = 0;
        }
    }
}
