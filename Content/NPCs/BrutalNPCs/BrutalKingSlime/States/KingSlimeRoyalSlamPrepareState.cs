using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Core;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.States
{
    /// <summary>
    /// 皇家砸地——蓄力阶段：传送至玩家头顶上方悬停蓄力，蓄满后切到 Falling 状态
    /// </summary>
    internal class KingSlimeRoyalSlamPrepareState : KingSlimeStateBase
    {
        public override string StateName => "RoyalSlamPrepare";
        public override KingSlimeStateIndex StateIndex => KingSlimeStateIndex.RoyalSlamPrepare;

        private const int TeleportFlashTime = 24;
        private const int HoverChargeTime = 70;
        private bool teleported;

        public override void OnEnter(KingSlimeStateContext context) {
            base.OnEnter(context);
            teleported = false;
            context.LastAttackKind = KingSlimeStateIndex.RoyalSlamPrepare;
            //此期间忽略地形以保证悬停
            context.Npc.noTileCollide = true;
        }

        public override IKingSlimeState OnUpdate(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;

            //阶段一：闪烁渐隐——准备传送
            if (Timer < TeleportFlashTime) {
                npc.alpha = (int)MathHelper.Lerp(0, 230, Timer / (float)TeleportFlashTime);
                npc.velocity *= 0.85f;
            }
            //阶段二：传送到玩家头顶并淡入
            else if (!teleported) {
                if (!VaultUtils.isClient) {
                    Vector2 above = player.Center + new Vector2(0, -380);
                    npc.position = above - npc.Size / 2f;
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }
                teleported = true;
                if (!VaultUtils.isServer) {
                    SoundEngine.PlaySound(SoundID.Item67, npc.Center);
                }
                npc.alpha = 230;
            }
            //阶段三：悬停蓄力，可视压扁，描边光辉随蓄力增长
            else {
                int chargeT = Timer - TeleportFlashTime;
                float prog = MathHelper.Clamp(chargeT / (float)HoverChargeTime, 0f, 1f);

                //淡入
                npc.alpha = (int)MathHelper.Lerp(230, 0, MathHelper.Clamp(chargeT / 16f, 0f, 1f));
                npc.velocity = Vector2.Zero;

                //追踪玩家上方位置
                if (!VaultUtils.isClient) {
                    Vector2 desired = player.Center + new Vector2(0, -380);
                    Vector2 toDesired = desired - npc.Center;
                    if (toDesired.LengthSquared() > 36f) {
                        npc.velocity = toDesired * 0.08f;
                    }
                    else {
                        npc.velocity = Vector2.Zero;
                        npc.Center = desired;
                    }
                }

                //蓄力可视：纵向被"皇室之力"压扁
                context.SquishY = MathHelper.SmoothStep(0.15f, 0.45f, prog);
                context.SetChargeState(1, prog);

                //蓄力中喷发蓝紫光屑
                if (!VaultUtils.isServer && Timer % 4 == 0) {
                    Vector2 dustOffset = Main.rand.NextVector2Circular(60, 60);
                    Dust dust = Dust.NewDustDirect(npc.Center + dustOffset - new Vector2(8, 8),
                        16, 16, DustID.PinkCrystalShard, 0, 0, 100, default, 1.4f);
                    dust.noGravity = true;
                    dust.velocity = (npc.Center - dust.position).SafeNormalize(Vector2.Zero) * 4f;
                }

                if (chargeT >= HoverChargeTime) {
                    return new KingSlimeRoyalSlamFallingState();
                }
            }

            Timer++;
            return null;
        }

        public override void OnExit(KingSlimeStateContext context) {
            base.OnExit(context);
            context.Npc.noTileCollide = false;
            context.SquishY = 0f;
            context.Npc.alpha = 0;
        }
    }
}
