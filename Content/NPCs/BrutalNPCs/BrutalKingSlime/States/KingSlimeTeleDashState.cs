using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Core;
using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Projectiles;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.States
{
    /// <summary>
    /// 瞬移横扫（仅暴怒阶段）：传送至玩家侧面，蓄力后高速横向冲撞穿过玩家，沿途留下皇室凝胶余波。
    /// </summary>
    internal class KingSlimeTeleDashState : KingSlimeStateBase
    {
        public override string StateName => "TeleDash";
        public override KingSlimeStateIndex StateIndex => KingSlimeStateIndex.TeleDash;

        private const int FlashTime = 18;
        private const int ChargeTime = 28;
        private const int DashTime = 28;
        private const int RecoverTime = 18;

        private int dashCount;
        private int dashesTotal;
        //当前冲刺阶段：0=闪烁渐隐 1=已传送&蓄力 2=冲刺中 3=恢复
        private int dashPhase;
        private int phaseTimer;
        private int dashSign;

        public override void OnEnter(KingSlimeStateContext context) {
            base.OnEnter(context);
            context.LastAttackKind = KingSlimeStateIndex.TeleDash;
            context.Npc.noTileCollide = true;
            dashCount = 0;
            dashesTotal = 2;
            dashPhase = 0;
            phaseTimer = 0;
        }

        public override IKingSlimeState OnUpdate(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;

            phaseTimer++;
            Timer++;

            switch (dashPhase) {
                case 0: HandleFlash(context); break;
                case 1: HandleCharge(context); break;
                case 2:
                    HandleDash(context);
                    break;
                case 3:
                    var n = HandleRecover(context);
                    if (n != null) return n;
                    break;
            }

            return null;
        }

        private void HandleFlash(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;

            npc.velocity *= 0.85f;
            npc.alpha = (int)MathHelper.Lerp(0, 230, phaseTimer / (float)FlashTime);
            context.SetChargeState(4, phaseTimer / (float)FlashTime * 0.5f);

            if (phaseTimer >= FlashTime) {
                if (!VaultUtils.isClient) {
                    //从玩家左/右侧水平方向传送
                    int side = Main.rand.NextBool() ? 1 : -1;
                    dashSign = -side;
                    Vector2 spawn = player.Center + new Vector2(side * 520, -10);
                    npc.position = spawn - npc.Size / 2f;
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }
                if (!VaultUtils.isServer) {
                    SoundEngine.PlaySound(SoundID.Item67, npc.Center);
                }
                dashPhase = 1;
                phaseTimer = 0;
            }
        }

        private void HandleCharge(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;

            npc.alpha = (int)MathHelper.Lerp(230, 0, MathHelper.Clamp(phaseTimer / 16f, 0f, 1f));
            npc.velocity *= 0.85f;

            float prog = MathHelper.Clamp(phaseTimer / (float)ChargeTime, 0f, 1f);
            context.SetChargeState(4, 0.5f + 0.5f * prog);
            context.SquishY = MathHelper.SmoothStep(0f, -0.30f, prog);

            //冲撞方向 = 朝向玩家
            Vector2 dir = (player.Center - npc.Center).SafeNormalize(Vector2.UnitX * dashSign);
            //仅取水平分量，做侧扫攻击
            dir = new Vector2(Math.Sign(dir.X) != 0 ? Math.Sign(dir.X) : dashSign, 0f);
            context.DashDirection = dir;

            if (!VaultUtils.isServer && phaseTimer % 3 == 0) {
                Dust dust = Dust.NewDustDirect(npc.Center + Main.rand.NextVector2Circular(40, 40) - new Vector2(8, 8),
                    16, 16, DustID.RedTorch, 0, 0, 100, default, 1.6f);
                dust.noGravity = true;
                dust.velocity = -dir * 4f;
            }

            if (phaseTimer >= ChargeTime) {
                npc.velocity = context.DashDirection * 32f;
                if (!VaultUtils.isServer) {
                    SoundEngine.PlaySound(SoundID.Item62, npc.Center);
                }
                dashPhase = 2;
                phaseTimer = 0;
            }
        }

        private void HandleDash(KingSlimeStateContext context) {
            NPC npc = context.Npc;

            //保持冲刺速度
            npc.velocity = context.DashDirection * 32f;
            context.SquishY = -0.40f;
            context.SetChargeState(4, 1f);

            //每隔几帧产生一颗小型皇室凝胶残波
            if (!VaultUtils.isClient && phaseTimer % 4 == 0) {
                int dropletType = ModContent.ProjectileType<KingSlimeRoyalGelDropletProj>();
                int dmg = CWRRef.GetProjectileDamage(npc, ProjectileID.None);
                if (dmg < 22) dmg = 22;
                Vector2 spawn = npc.Center + new Vector2(Main.rand.NextFloat(-12, 12), Main.rand.NextFloat(-30, 30));
                Vector2 vel = new Vector2(0f, 1f);
                Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, vel,
                    dropletType, dmg, 1f, Main.myPlayer, ai0: 1f);
            }

            if (phaseTimer >= DashTime) {
                dashPhase = 3;
                phaseTimer = 0;
            }
        }

        private IKingSlimeState HandleRecover(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            npc.velocity *= 0.85f;
            float t = MathHelper.Clamp(phaseTimer / (float)RecoverTime, 0f, 1f);
            context.SquishY = MathHelper.SmoothStep(-0.30f, 0f, t);
            context.SetChargeState(4, 1f - t);

            if (phaseTimer >= RecoverTime) {
                dashCount++;
                if (dashCount >= dashesTotal) {
                    return new KingSlimeHopState();
                }
                dashPhase = 0;
                phaseTimer = 0;
            }
            return null;
        }

        public override void OnExit(KingSlimeStateContext context) {
            base.OnExit(context);
            context.Npc.noTileCollide = false;
            context.Npc.alpha = 0;
            context.SquishY = 0f;
        }
    }
}
