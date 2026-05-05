using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Core;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.States
{
    /// <summary>
    /// 跳跃状态：地面短跳/中跳追踪玩家，每次落地形成皇室冲击波。
    /// 累计若干次小跳后切换到主动技能。
    /// </summary>
    internal class KingSlimeHopState : KingSlimeStateBase
    {
        public override string StateName => "Hop";
        public override KingSlimeStateIndex StateIndex => KingSlimeStateIndex.Hop;

        //跳跃阶段：0=蹲伏蓄力 1=空中 2=落地缓冲
        private int phase;
        private int phaseTimer;
        private int hopsThisChain;
        private int maxHopsBeforeAttack;

        public override void OnEnter(KingSlimeStateContext context) {
            base.OnEnter(context);
            phase = 0;
            phaseTimer = 0;
            hopsThisChain = 0;
            //一阶段连跳3次后强制使用技能；二阶段更激进，连跳2次
            maxHopsBeforeAttack = context.IsEnraged ? 2 : 3;
        }

        public override IKingSlimeState OnUpdate(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;
            FaceTargetX(npc, player);

            IKingSlimeState next = null;
            switch (phase) {
                case 0: HandleSquish(context); break;
                case 1: HandleAirborne(context); break;
                case 2: next = HandleLanded(context); break;
            }

            phaseTimer++;
            Timer++;
            return next;
        }

        //蹲伏蓄力——主体被压扁
        private void HandleSquish(KingSlimeStateContext context) {
            NPC npc = context.Npc;

            //蹲伏期间的减速
            if (npc.velocity.X != 0f) {
                npc.velocity.X *= 0.85f;
            }

            //可视压扁度，由 PostDraw 读取做缩放
            int squishLength = context.IsEnraged ? 18 : 22;
            float t = MathHelper.Clamp(phaseTimer / (float)squishLength, 0f, 1f);
            context.SquishY = MathHelper.SmoothStep(0f, 0.35f, t);

            if (phaseTimer >= squishLength) {
                LaunchHop(context);
                phase = 1;
                phaseTimer = 0;
            }
        }

        private void LaunchHop(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;

            //大跳间隔——每3跳一次大跳，阶段二每跳都偏大
            bool bigHop = context.IsEnraged || hopsThisChain >= maxHopsBeforeAttack - 1;

            float vx = MathHelper.Clamp((player.Center.X - npc.Center.X) * 0.025f,
                bigHop ? -14f : -10f, bigHop ? 14f : 10f);
            float vy = bigHop ? -16f : -12f;
            //如果玩家处在高处，上抛幅度更大
            float dy = player.Center.Y - npc.Center.Y;
            if (dy < -200f) vy -= 4f;

            npc.velocity = new Vector2(vx, vy);

            if (!VaultUtils.isServer) {
                SoundEngine.PlaySound(SoundID.Item154, npc.Center);
            }
        }

        //空中：仅追加少量水平加速，等待落地
        private void HandleAirborne(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;

            //轻微空中追踪
            float aim = (player.Center.X - npc.Center.X) * 0.0015f;
            npc.velocity.X = MathHelper.Clamp(npc.velocity.X + aim, -16f, 16f);

            //空中可视压扁逐渐回弹到拉伸
            context.SquishY = MathHelper.Lerp(context.SquishY, -0.08f, 0.10f);

            //被地面挡住即视为落地
            bool landed = npc.collideY && npc.velocity.Y >= 0f;
            if (landed) {
                phase = 2;
                phaseTimer = 0;
                npc.velocity.X *= 0.4f;
                hopsThisChain++;
                KingSlimeRenderHelper.DoLandingShockwave(npc, context,
                    hopsThisChain >= maxHopsBeforeAttack ? 0.9f : 0.6f);
            }
        }

        //落地后短暂回弹缓冲——可视拉伸→恢复
        private IKingSlimeState HandleLanded(KingSlimeStateContext context) {
            NPC npc = context.Npc;

            //落地后地面摩擦
            npc.velocity.X *= 0.85f;

            //可视拉伸→恢复
            int recoverLen = context.IsEnraged ? 12 : 16;
            float t = MathHelper.Clamp(phaseTimer / (float)recoverLen, 0f, 1f);
            context.SquishY = MathHelper.SmoothStep(-0.18f, 0f, t);

            if (phaseTimer >= recoverLen) {
                if (hopsThisChain >= maxHopsBeforeAttack) {
                    //只在服务端/单人端进行随机选择，避免多端desync
                    if (!VaultUtils.isClient) {
                        return ChooseNextAttack(context);
                    }
                    return null;
                }
                //继续下一跳
                phase = 0;
                phaseTimer = 0;
            }
            return null;
        }

        public override void OnExit(KingSlimeStateContext context) {
            base.OnExit(context);
            context.SquishY = 0f;
        }

        private static IKingSlimeState ChooseNextAttack(KingSlimeStateContext context) {
            int roll = Main.rand.Next(100);

            if (context.IsEnraged) {
                //避免连续重复
                if (context.LastAttackKind == KingSlimeStateIndex.RoyalSlamPrepare) {
                    if (roll < 35) return new KingSlimeCrownBarrageState();
                    if (roll < 65) return new KingSlimeSlimeRainState();
                    return new KingSlimeTeleDashState();
                }
                if (context.LastAttackKind == KingSlimeStateIndex.CrownBarrage) {
                    if (roll < 35) return new KingSlimeRoyalSlamPrepareState();
                    if (roll < 65) return new KingSlimeSlimeRainState();
                    return new KingSlimeTeleDashState();
                }
                if (context.LastAttackKind == KingSlimeStateIndex.SlimeRain) {
                    if (roll < 35) return new KingSlimeRoyalSlamPrepareState();
                    if (roll < 70) return new KingSlimeCrownBarrageState();
                    return new KingSlimeTeleDashState();
                }
                if (context.LastAttackKind == KingSlimeStateIndex.TeleDash) {
                    if (roll < 35) return new KingSlimeRoyalSlamPrepareState();
                    if (roll < 70) return new KingSlimeCrownBarrageState();
                    return new KingSlimeSlimeRainState();
                }

                if (roll < 25) return new KingSlimeRoyalSlamPrepareState();
                if (roll < 55) return new KingSlimeCrownBarrageState();
                if (roll < 80) return new KingSlimeSlimeRainState();
                return new KingSlimeTeleDashState();
            }

            //一阶段：无 TeleDash
            if (context.LastAttackKind == KingSlimeStateIndex.RoyalSlamPrepare) {
                if (roll < 55) return new KingSlimeCrownBarrageState();
                return new KingSlimeSlimeRainState();
            }
            if (context.LastAttackKind == KingSlimeStateIndex.CrownBarrage) {
                if (roll < 55) return new KingSlimeRoyalSlamPrepareState();
                return new KingSlimeSlimeRainState();
            }
            if (context.LastAttackKind == KingSlimeStateIndex.SlimeRain) {
                if (roll < 55) return new KingSlimeRoyalSlamPrepareState();
                return new KingSlimeCrownBarrageState();
            }

            if (roll < 35) return new KingSlimeRoyalSlamPrepareState();
            if (roll < 70) return new KingSlimeCrownBarrageState();
            return new KingSlimeSlimeRainState();
        }
    }
}
