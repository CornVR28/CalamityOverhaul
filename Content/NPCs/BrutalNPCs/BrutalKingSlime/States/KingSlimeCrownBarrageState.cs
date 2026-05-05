using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Core;
using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.States
{
    /// <summary>
    /// 皇冠齐射：保持原地小幅蹲伏蓄力，从皇冠位置依次射出多束皇室光束
    /// </summary>
    internal class KingSlimeCrownBarrageState : KingSlimeStateBase
    {
        public override string StateName => "CrownBarrage";
        public override KingSlimeStateIndex StateIndex => KingSlimeStateIndex.CrownBarrage;

        private const int ChargeTime = 36;
        private int volleyCount;
        private int volleyTotal;
        private int volleyTimer;

        public override void OnEnter(KingSlimeStateContext context) {
            base.OnEnter(context);
            context.LastAttackKind = KingSlimeStateIndex.CrownBarrage;
            volleyCount = 0;
            volleyTotal = context.IsEnraged ? 5 : 4;
            volleyTimer = 0;
        }

        public override IKingSlimeState OnUpdate(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;
            FaceTargetX(npc, player);

            //蓄力：身体压扁，皇冠光辉积累
            if (Timer < ChargeTime) {
                npc.velocity.X *= 0.85f;
                float prog = MathHelper.Clamp(Timer / (float)ChargeTime, 0f, 1f);
                context.SetChargeState(2, prog);
                context.SquishY = MathHelper.SmoothStep(0f, 0.30f, prog);

                if (!VaultUtils.isServer && Timer % 3 == 0) {
                    Vector2 dustOffset = new Vector2(Main.rand.NextFloat(-30, 30),
                        -npc.height * 0.5f + Main.rand.NextFloat(-12, 12));
                    Dust dust = Dust.NewDustDirect(npc.Center + dustOffset - new Vector2(8, 8),
                        16, 16, DustID.GoldFlame, 0, 0, 100, default, 1.4f);
                    dust.noGravity = true;
                    dust.velocity = (npc.Center + new Vector2(0, -npc.height * 0.45f) - dust.position)
                        .SafeNormalize(Vector2.Zero) * 3.5f;
                }
            }
            //发射阶段：每隔若干帧从皇冠射出一束
            else {
                npc.velocity.X *= 0.85f;
                context.SquishY = MathHelper.Lerp(context.SquishY, 0f, 0.10f);
                int interval = context.IsEnraged ? 18 : 24;
                if (volleyCount < volleyTotal && volleyTimer % interval == 0) {
                    FireCrownBeam(context);
                    volleyCount++;
                }
                volleyTimer++;

                //发射结束后再硬直十几帧
                if (volleyCount >= volleyTotal && volleyTimer > volleyTotal * interval + 18) {
                    return new KingSlimeHopState();
                }
            }

            Timer++;
            return null;
        }

        private static void FireCrownBeam(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;
            Vector2 crownPos = npc.Center + new Vector2(0, -npc.height * 0.45f);

            if (!VaultUtils.isServer) {
                SoundEngine.PlaySound(SoundID.Item122, npc.Center);
            }

            if (VaultUtils.isClient) return;

            int beamType = ModContent.ProjectileType<KingSlimeCrownBeamProj>();
            int dmg = CWRRef.GetProjectileDamage(npc, ProjectileID.CursedFlameHostile);
            if (dmg < 22) dmg = 22;

            //预测玩家位置：抬高一些，让光束偏向玩家头顶
            Vector2 predict = player.Center + player.velocity * 12f;
            //光束目标位置 = 预测点正上方天空
            Vector2 strikePos = new Vector2(predict.X, predict.Y - 50);

            //ai0/ai1 编码光束起点（皇冠位置）相对落点的偏移，让 PreDraw 绘制连线
            Vector2 offset = crownPos - strikePos;
            Projectile.NewProjectile(npc.GetSource_FromAI(), strikePos, Vector2.Zero,
                beamType, dmg, 3f, Main.myPlayer, ai0: offset.X, ai1: offset.Y);
        }

        public override void OnExit(KingSlimeStateContext context) {
            base.OnExit(context);
            context.SquishY = 0f;
        }
    }
}
