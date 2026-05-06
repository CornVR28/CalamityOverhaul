using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Core;
using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.States
{
    /// <summary>
    /// 史莱姆雨：本体高高跃起、悬停于半空，从天而降的皇室凝胶粒覆盖玩家头顶。
    /// </summary>
    internal class KingSlimeSlimeRainState : KingSlimeStateBase
    {
        public override string StateName => "SlimeRain";
        public override KingSlimeStateIndex StateIndex => KingSlimeStateIndex.SlimeRain;

        private const int LiftTime = 26;
        private const int HoverTime = 90;

        public override void OnEnter(KingSlimeStateContext context) {
            base.OnEnter(context);
            context.LastAttackKind = KingSlimeStateIndex.SlimeRain;
            //悬停期间不与地形碰撞
            context.Npc.noTileCollide = true;
        }

        public override IKingSlimeState OnUpdate(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;

            //阶段一：上抛
            if (Timer < LiftTime) {
                if (Timer == 0) {
                    npc.velocity = new Vector2((player.Center.X - npc.Center.X) * 0.005f, -22f);
                    if (!VaultUtils.isServer) {
                        SoundEngine.PlaySound(SoundID.Item154, npc.Center);
                    }
                }
                npc.velocity.Y *= 0.93f;
                context.SquishY = -0.30f;
                context.SetChargeState(3, Timer / (float)LiftTime * 0.5f);
            }
            //阶段二：悬停 + 下雨
            else if (Timer < LiftTime + HoverTime) {
                int hoverT = Timer - LiftTime;
                float prog = MathHelper.Clamp(hoverT / (float)HoverTime, 0f, 1f);

                //缓慢追踪玩家正上方
                if (!VaultUtils.isClient) {
                    Vector2 desired = player.Center + new Vector2(0, -420);
                    Vector2 toDesired = desired - npc.Center;
                    npc.velocity = toDesired * 0.05f;
                }

                context.SquishY = MathHelper.Lerp(0f, 0.20f, prog);
                context.SetChargeState(3, 0.5f + 0.5f * prog);

                //每隔几帧投放一颗皇室凝胶
                int interval = context.IsEnraged ? 4 : 6;
                if (!VaultUtils.isClient && hoverT % interval == 0) {
                    SpawnDroplet(context);
                }

                if (!VaultUtils.isServer && hoverT % 6 == 0) {
                    Dust dust = Dust.NewDustDirect(npc.Center + Main.rand.NextVector2Circular(40, 40) - new Vector2(8, 8),
                        16, 16, DustID.RedTorch, 0, 0, 100, default, 1.4f);
                    dust.noGravity = true;
                    dust.velocity = Vector2.UnitY * 2f;
                }
            }
            //阶段三：下落回归
            else {
                context.ResetChargeState();
                npc.noTileCollide = false;
                npc.velocity.Y = MathHelper.Min(npc.velocity.Y + 1.0f, 24f);
                npc.velocity.X *= 0.97f;
                context.SquishY = MathHelper.Lerp(context.SquishY, -0.20f, 0.10f);

                if (npc.collideY && npc.velocity.Y >= 0f) {
                    KingSlimeRenderHelper.DoLandingShockwave(npc, context, 0.7f);
                    return new KingSlimeHopState();
                }
                //超时保护
                if (Timer > LiftTime + HoverTime + 240) {
                    return new KingSlimeHopState();
                }
            }

            Timer++;
            return null;
        }

        private static void SpawnDroplet(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;

            int dropletType = ModContent.ProjectileType<KingSlimeRoyalGelDropletProj>();
            int dmg = CWRRef.GetProjectileDamage(npc, ProjectileID.None);
            if (dmg < 18) dmg = 18;

            //投掷点：以玩家上空为中心，水平随机扩散
            float spreadX = Main.rand.NextFloat(-380f, 380f);
            Vector2 spawn = new Vector2(player.Center.X + spreadX, npc.Center.Y + Main.rand.NextFloat(-20f, 20f));
            //初速度：缓慢向下 + 微弱水平
            Vector2 vel = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(2f, 4f));
            Projectile.NewProjectile(npc.GetSource_FromAI(), spawn, vel,
                dropletType, dmg, 1f, Main.myPlayer);
        }

        public override void OnExit(KingSlimeStateContext context) {
            base.OnExit(context);
            context.Npc.noTileCollide = false;
            context.SquishY = 0f;
            context.ResetChargeState();
        }
    }
}
