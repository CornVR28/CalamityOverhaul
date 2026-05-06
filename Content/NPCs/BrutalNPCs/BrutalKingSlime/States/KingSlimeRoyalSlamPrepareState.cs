using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Core;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.States
{
    /// <summary>
    /// 皇家砸地——蓄力阶段：先蹲伏蓄力，再爆发式跃起，于跃起顶点借皇室凝胶光芒消隐，
    /// 在玩家头顶位置以爆裂光花重新现身后悬停蓄力，蓄满后切到 Falling 状态。
    /// 通过"蹲—跃—闪现—现身"四段过渡彻底消除原本"瞬移到玩家头顶"的突兀感。
    /// </summary>
    internal class KingSlimeRoyalSlamPrepareState : KingSlimeStateBase
    {
        public override string StateName => "RoyalSlamPrepare";
        public override KingSlimeStateIndex StateIndex => KingSlimeStateIndex.RoyalSlamPrepare;

        //子阶段：0 蹲伏蓄力 / 1 跃起冲天 / 2 顶点消隐 / 3 头顶炸裂现身 / 4 悬停蓄力
        private const int AnticipationTime = 16;
        private const int LeapTime = 28;
        private const int VanishTime = 10;
        private const int MaterializeTime = 12;
        private const int HoverChargeTime = 70;

        //目标悬停高度（玩家头顶之上）
        private const float HoverHeight = 380f;

        private int subPhase;
        private int phaseTimer;

        public override void OnEnter(KingSlimeStateContext context) {
            base.OnEnter(context);
            context.LastAttackKind = KingSlimeStateIndex.RoyalSlamPrepare;
            subPhase = 0;
            phaseTimer = 0;

            //跃起期间需自定义重力，避免地形/重力把演出节奏带乱
            context.Npc.noGravity = true;
            //蹲伏阶段保留地形碰撞，避免空中蹲伏的诡异画面
            context.Npc.noTileCollide = false;
        }

        public override IKingSlimeState OnUpdate(KingSlimeStateContext context) {
            phaseTimer++;
            Timer++;

            switch (subPhase) {
                case 0: HandleAnticipate(context); break;
                case 1: HandleLeap(context); break;
                case 2: HandleVanish(context); break;
                case 3: HandleMaterialize(context); break;
                case 4: {
                    var next = HandleHoverCharge(context);
                    if (next != null) return next;
                    break;
                }
            }

            return null;
        }

        #region 阶段0：蹲伏蓄力

        //深蹲——皇室凝胶被向下压实，外圈光屑向身体汇聚，提示玩家"大招要来了"
        private void HandleAnticipate(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;
            FaceTargetX(npc, player);

            //摩擦减速，让史莱姆王立刻"扎根"在地面
            npc.velocity.X *= 0.78f;
            //贴地：在 noGravity 下手动模拟轻微重力，遇地面立刻归零
            if (!npc.collideY) {
                npc.velocity.Y = MathHelper.Min(npc.velocity.Y + 0.6f, 16f);
            }
            else {
                npc.velocity.Y = 0f;
            }

            float t = MathHelper.Clamp(phaseTimer / (float)AnticipationTime, 0f, 1f);
            //深蹲压扁
            context.SquishY = MathHelper.SmoothStep(0f, 0.55f, t);
            //逐步亮起皇室描边，做出"力量积聚"的视觉前置
            context.SetChargeState(1, t * 0.30f);

            //外圈粒子向身体汇聚——皇室凝胶被吸引
            if (!VaultUtils.isServer && phaseTimer % 2 == 0) {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 0.7f);
                Vector2 spawn = npc.Center + dir * Main.rand.NextFloat(110f, 160f);
                Dust dust = Dust.NewDustDirect(spawn - new Vector2(8, 8), 16, 16,
                    DustID.PinkCrystalShard, 0, 0, 100, default, 1.4f);
                dust.noGravity = true;
                dust.velocity = (npc.Center - spawn).SafeNormalize(Vector2.Zero) * 6f;
            }

            //蓄力中段轻微低频低音，营造蓄势感
            if (!VaultUtils.isServer && phaseTimer == AnticipationTime / 2) {
                SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = -0.6f, Volume = 0.6f }, npc.Center);
            }

            if (phaseTimer >= AnticipationTime) {
                LaunchPowerLeap(context);
                subPhase = 1;
                phaseTimer = 0;
            }
        }

        private static void LaunchPowerLeap(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;

            //横向给一点偏移让弧线更生动，但主体是大幅垂直起跳
            float dx = player.Center.X - npc.Center.X;
            float vx = MathHelper.Clamp(dx * 0.020f, -7f, 7f);
            float vy = -19f;
            npc.velocity = new Vector2(vx, vy);

            //跃起期间忽略地形——避免被天花板卡住打断演出
            npc.noTileCollide = true;

            if (!VaultUtils.isServer) {
                //更厚重的起跳声（跳跃声 + 低频砸地音）
                SoundEngine.PlaySound(SoundID.Item154 with { Pitch = -0.4f, Volume = 1.1f }, npc.Center);
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Pitch = 0.3f, Volume = 0.8f }, npc.Center);

                //起跳瞬间：脚下溅射一圈尘土与凝胶光屑
                for (int i = 0; i < 18; i++) {
                    float ang = -MathHelper.PiOver2 + Main.rand.NextFloat(-1.05f, 1.05f);
                    Vector2 dir = ang.ToRotationVector2();
                    Dust dust = Dust.NewDustDirect(npc.Bottom - new Vector2(8, 12), 16, 12,
                        DustID.PinkCrystalShard, dir.X * Main.rand.NextFloat(3f, 6.5f),
                        dir.Y * Main.rand.NextFloat(2.5f, 5.5f), 100, default, 1.6f);
                    dust.noGravity = true;
                }

                //轻微震屏，让起跳更有重量感
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(
                    npc.Bottom, -Vector2.UnitY, 4f, 6f, 12, 1200f, "KingSlimeRoyalLeap"));
            }
        }

        #endregion

        #region 阶段1：跃起冲天

        //强力上跳，身体被纵向拉伸成水滴，沿途留下粒子尾迹
        private void HandleLeap(KingSlimeStateContext context) {
            NPC npc = context.Npc;

            //自定义重力——比较弱，保留滞空感；接近顶点时进一步衰减以"凝固"在峰值
            float t = MathHelper.Clamp(phaseTimer / (float)LeapTime, 0f, 1f);
            float gravity = MathHelper.Lerp(0.55f, 0.18f, t);
            npc.velocity.Y += gravity;
            //空气阻力
            npc.velocity.X *= 0.985f;

            //上升过程被纵向拉伸成水滴
            context.SquishY = MathHelper.SmoothStep(0.55f, -0.42f, t);
            //蓄力描边强度继续上升，让玩家追着主体往上看
            context.SetChargeState(1, MathHelper.Lerp(0.30f, 0.55f, t));

            //拖尾粒子——皇室凝胶残光
            if (!VaultUtils.isServer && phaseTimer % 2 == 0) {
                Vector2 spawn = npc.Center + Main.rand.NextVector2Circular(20f, 24f);
                Dust trail = Dust.NewDustDirect(spawn - new Vector2(8, 8), 16, 16,
                    DustID.PinkCrystalShard, 0, 0, 100, default, 1.5f);
                trail.noGravity = true;
                trail.velocity = -npc.velocity * 0.30f;
            }

            //跃起后半段：在玩家头顶预生成"传送门"标记，提前告诉玩家史莱姆王要从哪里下来
            if (!VaultUtils.isServer && t > 0.4f && phaseTimer % 2 == 0) {
                SpawnDestinationMarker(context, intensity: (t - 0.4f) / 0.6f);
            }

            if (phaseTimer >= LeapTime) {
                //进入消隐前先把速度归零，把镜头牢牢锁在跃起顶点
                npc.velocity = Vector2.Zero;
                if (!VaultUtils.isServer) {
                    SoundEngine.PlaySound(SoundID.Item67, npc.Center);
                    SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.3f, Volume = 0.8f }, npc.Center);
                }
                subPhase = 2;
                phaseTimer = 0;
            }
        }

        #endregion

        #region 阶段2：顶点消隐

        //顶点处皇室光辉将史莱姆王吸入虚空——内爆式收缩光环，外圈粒子涌入身体
        private void HandleVanish(KingSlimeStateContext context) {
            NPC npc = context.Npc;

            npc.velocity = Vector2.Zero;

            float t = MathHelper.Clamp(phaseTimer / (float)VanishTime, 0f, 1f);
            //淡出
            npc.alpha = (int)MathHelper.Lerp(0, 255, t);
            //先拉伸再急剧收缩，营造"被光吸走"的弹性形变
            context.SquishY = MathHelper.SmoothStep(-0.42f, 0.40f, t);
            //蓄力强度回落——视觉焦点交给"现身"那一刻
            context.SetChargeState(1, MathHelper.Lerp(0.55f, 0.15f, t));

            //内爆粒子：从外环涌入身体
            if (!VaultUtils.isServer) {
                int count = 4;
                for (int i = 0; i < count; i++) {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                    Vector2 spawn = npc.Center + dir * Main.rand.NextFloat(70f, 130f);
                    Dust dust = Dust.NewDustDirect(spawn - new Vector2(8, 8), 16, 16,
                        DustID.PinkCrystalShard, 0, 0, 100, default, 1.6f);
                    dust.noGravity = true;
                    dust.velocity = -dir * Main.rand.NextFloat(7f, 10f);
                    dust.fadeIn = 0.5f;
                }
            }

            //继续在头顶位置加强目标标记，让玩家清晰预判
            if (!VaultUtils.isServer && phaseTimer % 1 == 0) {
                SpawnDestinationMarker(context, intensity: 1f);
            }

            if (phaseTimer >= VanishTime) {
                ExecuteTeleport(context);
                subPhase = 3;
                phaseTimer = 0;
            }
        }

        //执行真正的位置传送——只在服务端/单人端写位置，客户端通过 netUpdate 跟随
        private static void ExecuteTeleport(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;

            Vector2 dest = player.Center + new Vector2(0, -HoverHeight);

            if (!VaultUtils.isClient) {
                npc.position = dest - npc.Size / 2f;
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
            }
            npc.alpha = 255;

            if (!VaultUtils.isServer) {
                //现身瞬间：响亮的高频闪光音 + 皇家爆裂粒子花
                SoundEngine.PlaySound(SoundID.Item67 with { Pitch = 0.35f, Volume = 1.2f }, dest);
                SoundEngine.PlaySound(SoundID.Item109 with { Pitch = 0.2f, Volume = 0.9f }, dest);

                int rays = 26;
                for (int i = 0; i < rays; i++) {
                    float ang = MathHelper.TwoPi / rays * i;
                    Vector2 dir = ang.ToRotationVector2();
                    Dust dust = Dust.NewDustDirect(dest - new Vector2(8, 8), 16, 16,
                        DustID.PinkCrystalShard,
                        dir.X * Main.rand.NextFloat(7f, 11f),
                        dir.Y * Main.rand.NextFloat(7f, 11f),
                        100, default, 1.8f);
                    dust.noGravity = true;
                }
                //再补一圈高速金色光屑做"皇室"点缀
                int sparks = 10;
                for (int i = 0; i < sparks; i++) {
                    float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 dir = ang.ToRotationVector2();
                    Dust spark = Dust.NewDustDirect(dest - new Vector2(8, 8), 16, 16,
                        DustID.GoldFlame, dir.X * 6f, dir.Y * 6f, 80, default, 1.6f);
                    spark.noGravity = true;
                }

                //轻微震屏，让现身和起跳形成节奏呼应
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(
                    dest, Vector2.UnitX, 5f, 6f, 14, 1500f, "KingSlimeRoyalAppear"));
            }
        }

        #endregion

        #region 阶段3：玩家头顶炸裂现身

        //淡入并轻微弹性回弹，把节奏交还给原版的悬停蓄力
        private void HandleMaterialize(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;

            //锁定在玩家头顶，避免现身时位置抖动
            Vector2 desired = player.Center + new Vector2(0, -HoverHeight);
            npc.Center = desired;
            npc.velocity = Vector2.Zero;

            float t = MathHelper.Clamp(phaseTimer / (float)MaterializeTime, 0f, 1f);
            //淡入
            npc.alpha = (int)MathHelper.Lerp(255, 0, t);
            //回弹震荡：从挤压回到平衡，再略微过冲
            float bounce = (float)Math.Sin(t * MathHelper.Pi) * 0.18f;
            context.SquishY = MathHelper.SmoothStep(0.40f, 0.10f, t) - bounce * (1f - t);
            //蓄力强度回升——衔接悬停蓄力
            context.SetChargeState(1, MathHelper.Lerp(0.25f, 0.50f, t));

            //现身后半段：身体周围有皇室光屑收束
            if (!VaultUtils.isServer && phaseTimer % 2 == 0) {
                Vector2 dir = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 spawn = npc.Center + dir * Main.rand.NextFloat(45f, 90f);
                Dust dust = Dust.NewDustDirect(spawn - new Vector2(8, 8), 16, 16,
                    DustID.PinkCrystalShard, 0, 0, 100, default, 1.4f);
                dust.noGravity = true;
                dust.velocity = (npc.Center - spawn).SafeNormalize(Vector2.Zero) * 4.5f;
            }

            if (phaseTimer >= MaterializeTime) {
                npc.alpha = 0;
                subPhase = 4;
                phaseTimer = 0;
            }
        }

        #endregion

        #region 阶段4：悬停蓄力

        //追踪玩家头顶位置，纵向被"皇室之力"压扁，蓄满后切到 Falling
        private IKingSlimeState HandleHoverCharge(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            Player player = context.Target;

            float prog = MathHelper.Clamp(phaseTimer / (float)HoverChargeTime, 0f, 1f);

            npc.alpha = 0;

            //追踪玩家上方位置——只在服务端/单人端写权威位置
            if (!VaultUtils.isClient) {
                Vector2 desired = player.Center + new Vector2(0, -HoverHeight);
                Vector2 toDesired = desired - npc.Center;
                if (toDesired.LengthSquared() > 36f) {
                    npc.velocity = toDesired * 0.08f;
                }
                else {
                    npc.velocity = Vector2.Zero;
                    npc.Center = desired;
                }
            }
            else {
                npc.velocity = Vector2.Zero;
            }

            //蓄力可视：纵向压扁，从现身阶段平滑过渡
            context.SquishY = MathHelper.SmoothStep(0.10f, 0.45f, prog);
            context.SetChargeState(1, MathHelper.Lerp(0.50f, 1f, prog));

            //蓄力中喷发蓝紫光屑
            if (!VaultUtils.isServer && phaseTimer % 4 == 0) {
                Vector2 dustOffset = Main.rand.NextVector2Circular(60, 60);
                Dust dust = Dust.NewDustDirect(npc.Center + dustOffset - new Vector2(8, 8),
                    16, 16, DustID.PinkCrystalShard, 0, 0, 100, default, 1.4f);
                dust.noGravity = true;
                dust.velocity = (npc.Center - dust.position).SafeNormalize(Vector2.Zero) * 4f;
            }

            if (phaseTimer >= HoverChargeTime) {
                return new KingSlimeRoyalSlamFallingState();
            }
            return null;
        }

        #endregion

        #region 工具

        //在玩家头顶生成一圈"传送门"预警粒子，提前告诉玩家史莱姆王将从此处坠下
        private static void SpawnDestinationMarker(KingSlimeStateContext context, float intensity) {
            Vector2 markerCenter = context.Target.Center + new Vector2(0, -HoverHeight);
            int ringCount = 6;
            float radius = MathHelper.Lerp(80f, 50f, MathHelper.Clamp(intensity, 0f, 1f));

            for (int i = 0; i < ringCount; i++) {
                float ang = MathHelper.TwoPi / ringCount * i + Main.GlobalTimeWrappedHourly * 4f;
                Vector2 markerPos = markerCenter + ang.ToRotationVector2() * radius;
                Dust marker = Dust.NewDustDirect(markerPos - new Vector2(4, 4), 8, 8,
                    DustID.PinkCrystalShard, 0, 0, 80, default, 0.9f + 0.4f * intensity);
                marker.noGravity = true;
                //粒子向中心汇聚，强度越高汇聚越快
                marker.velocity = (markerCenter - markerPos).SafeNormalize(Vector2.Zero)
                    * (1f + 1.8f * intensity);
            }
        }

        #endregion

        public override void OnExit(KingSlimeStateContext context) {
            base.OnExit(context);
            context.Npc.noTileCollide = false;
            context.Npc.noGravity = false;
            context.SquishY = 0f;
            context.Npc.alpha = 0;
        }
    }
}
