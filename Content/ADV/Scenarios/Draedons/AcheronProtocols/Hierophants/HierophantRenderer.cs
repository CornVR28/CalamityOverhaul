using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using static CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants.HierophantUtils;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants
{
    /// <summary>
    /// 所有绘制逻辑集中于此——4 段 IK 长腿、身体、肩甲、镰刀臂与挥刀拖影
    /// </summary>
    internal static class HierophantRenderer
    {
        private const string TexBase = "CalamityOverhaul/Content/ADV/Scenarios/Draedons/AcheronProtocols/Hierophants/";

        public static bool Draw(Hierophant boss, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (boss.Legs == null) return false;

            NPC npc = boss.NPC;
            if (Main.zenithWorld) drawColor = Main.DiscoColor;

            Texture2D body = npc.GetNpcTexture();
            Texture2D legTex1 = RequestTex(TexBase + "Leg1");
            Texture2D legTex2 = RequestTex(TexBase + "Leg2");
            Texture2D footTex = RequestTex(TexBase + "Foot");

            DrawLegs(boss, drawColor, legTex1, legTex2, footTex);
            DrawScytheArms(boss, screenPos, drawColor, body);

            return false;
        }

        #region 4-Segment Legs

        /// <summary>
        /// 绘制 4 段 IK 腿：Hip → Knee(高拱) → Shin → Foot
        /// <para>
        /// 骨骼拓扑为倒 V 形——膝盖被推到身体上方，营造巨型蛛形纲/虫圣的高拱感。
        /// </para>
        /// </summary>
        private static void DrawLegs(Hierophant boss, Color drawColor, Texture2D t1, Texture2D t2, Texture2D tFoot) {
            NPC npc = boss.NPC;
            foreach (var leg in boss.Legs) {
                float s = npc.scale * leg.Scale;

                // 4 段骨骼长度——总长极长，膝盖被高高支起
                float femurLen = 110f * s;   // 股节（髋→膝）
                float tibiaLen = 160f * s;   // 胫节（膝→踝）
                float metaLen  = 100f * s;   // 跗节（踝→脚尖）

                // 髋关节位置（身体侧面偏下）
                float bodyRot = boss.Direction > 0 ? npc.rotation : (-MathHelper.Pi + npc.rotation);
                int side = Math.Sign(leg.Offset.X);
                Vector2 hipPos = npc.Center + (new Vector2(side * 60f, 80f) * npc.scale).RotatedBy(bodyRot);

                // 计算 IK
                SolveArcLeg(hipPos, leg.StandPoint, femurLen, tibiaLen, metaLen, side,
                    out Vector2 knee, out Vector2 ankle);

                // 段 1：股节 Hip → Knee（向上拱起，用 Leg1 纹理）
                Main.EntitySpriteDraw(t1, hipPos - Main.screenPosition, null, drawColor,
                    (knee - hipPos).ToRotation(), new Vector2(8, t1.Height / 2f), s, SpriteEffects.None);

                // 段 2：胫节 Knee → Ankle（向下，用 Leg2 纹理）
                Main.EntitySpriteDraw(t2, knee - Main.screenPosition, null, drawColor,
                    (ankle - knee).ToRotation(), new Vector2(12, t2.Height / 2f), s, SpriteEffects.None);

                // 段 3：跗节 Ankle → Foot（用 Leg1 纹理缩小）
                Main.EntitySpriteDraw(t1, ankle - Main.screenPosition, null, drawColor,
                    (leg.StandPoint - ankle).ToRotation(), new Vector2(8, t1.Height / 2f), s * 0.8f, SpriteEffects.None);

                // 段 4：脚爪
                float footRot = (leg.StandPoint - ankle).ToRotation();
                float footDir = side > 0 ? 1f : -1f;
                Main.EntitySpriteDraw(tFoot, leg.StandPoint - Main.screenPosition, null, drawColor,
                    footRot + footDir * MathHelper.ToRadians(20f),
                    new Vector2(54, tFoot.Height / 2f), s * 0.9f,
                    side > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);

                // 膝盖关节光点
                if (!Main.dedServ) {
                    Lighting.AddLight(knee, 0.3f, 0.15f, 0.05f);
                }
            }
        }

        /// <summary>
        /// 3 骨骼 IK 求解——膝盖强制拱起到身体上方。
        /// <para>
        /// 策略：先用二骨 IK 解 (hip→ankle) 由 femur+tibia 确定 knee，
        /// 然后 ankle 由 (knee→foot) 中 metatarsus 长度截取。
        /// 膝盖方向被强制推向身体外上方（side 决定左右）。
        /// </para>
        /// </summary>
        private static void SolveArcLeg(Vector2 hip, Vector2 foot, float femur, float tibia, float meta,
            int side, out Vector2 knee, out Vector2 ankle) {
            // 先求 ankle = foot 沿脚→膝方向往回退 meta 距离的估算点
            // 第一步：估算 ankle 在 foot 正上方
            Vector2 ankleEstimate = foot + new Vector2(0, -meta * 0.5f);

            // 用 hip → ankleEstimate 做二骨 IK 求 knee
            knee = SolveTwoBoneKnee(hip, ankleEstimate, femur, tibia, side);

            // 用实际 knee 位置，沿 knee→foot 方向放置 ankle
            float kneeToFoot = Distance(knee, foot);
            if (kneeToFoot > tibia + meta) {
                // 超出最大伸展——拉伸
                Vector2 dir = (foot - knee).SafeNormalize(Vector2.UnitY);
                ankle = knee + dir * tibia;
            }
            else if (kneeToFoot < MathF.Abs(tibia - meta)) {
                // 过于折叠
                ankle = knee + new Vector2(0, tibia);
            }
            else {
                // 正常二骨 IK：knee→ankle→foot 由 tibia+meta 构成
                ankle = SolveTwoBoneElbow(knee, foot, tibia, meta, -side);
            }
        }

        /// <summary>
        /// 经典二骨 IK：给定 start、end 和两段骨长，求关节位置。
        /// <paramref name="bendDir"/> 控制弯曲方向（+1 = 向右/上方弯，-1 = 向左/下方弯）。
        /// 膝盖总是被推向上方。
        /// </summary>
        private static Vector2 SolveTwoBoneKnee(Vector2 start, Vector2 end, float lenA, float lenB, int bendDir) {
            Vector2 diff = end - start;
            float dist = diff.Length();

            if (dist > lenA + lenB) {
                return start + diff.SafeNormalize(Vector2.UnitY) * lenA;
            }
            if (dist < MathF.Abs(lenA - lenB)) {
                return start + new Vector2(bendDir * lenA * 0.5f, -lenA * 0.87f);
            }

            float a = (lenA * lenA - lenB * lenB + dist * dist) / (2f * dist);
            float hSq = lenA * lenA - a * a;
            float h = hSq > 0 ? MathF.Sqrt(hSq) : 0f;

            Vector2 along = diff / dist;
            Vector2 perp = new(-along.Y, along.X);

            // 强制膝盖朝上方弯曲（perp.Y < 0 = 上方）
            // 使用 bendDir 和 Y 分量来决定
            Vector2 joint1 = start + along * a + perp * h;
            Vector2 joint2 = start + along * a - perp * h;

            // 选择 Y 更小（更高）的那个作为膝盖
            if (joint1.Y < joint2.Y)
                return joint1;
            return joint2;
        }

        /// <summary>
        /// 二骨 IK 用于下半腿（膝→踝→脚），关节（踝）偏向下方/外侧
        /// </summary>
        private static Vector2 SolveTwoBoneElbow(Vector2 start, Vector2 end, float lenA, float lenB, int bendDir) {
            Vector2 diff = end - start;
            float dist = diff.Length();

            if (dist > lenA + lenB) {
                return start + diff.SafeNormalize(Vector2.UnitY) * lenA;
            }
            if (dist < MathF.Abs(lenA - lenB)) {
                return start + new Vector2(bendDir * 10f, lenA);
            }

            float a = (lenA * lenA - lenB * lenB + dist * dist) / (2f * dist);
            float hSq = lenA * lenA - a * a;
            float h = hSq > 0 ? MathF.Sqrt(hSq) : 0f;

            Vector2 along = diff / dist;
            Vector2 perp = new(-along.Y, along.X);

            Vector2 j1 = start + along * a + perp * h;
            Vector2 j2 = start + along * a - perp * h;

            // 选择更偏外侧（bendDir 方向）的
            return (j1.X * bendDir > j2.X * bendDir) ? j1 : j2;
        }

        #endregion

        #region Scythe Arms

        private static void DrawScytheArms(Hierophant boss, Vector2 screenPos, Color drawColor, Texture2D body) {
            NPC npc = boss.NPC;
            Texture2D armTex = RequestTex(TexBase + "CannonConnect");
            Texture2D bladeTex = RequestTex(TexBase + "Cannon");
            Texture2D shoulder = RequestTex(TexBase + "Shoulder");

            float bodyRot = boss.Direction > 0 ? npc.rotation : (npc.rotation + MathHelper.Pi);
            SpriteEffects dirFlip = boss.Direction > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
            SpriteEffects bodyFlip = boss.Direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            DrawSingleScythe(boss, boss.RightScythe, armTex, bladeTex, drawColor, bodyRot, dirFlip, true);

            Main.EntitySpriteDraw(body, npc.Center - screenPos, null, drawColor,
                npc.rotation, body.Size() / 2f, npc.scale, bodyFlip);

            Main.EntitySpriteDraw(shoulder, npc.Center - screenPos, null, drawColor,
                npc.rotation, shoulder.Size() / 2f, npc.scale, bodyFlip);

            DrawSingleScythe(boss, boss.LeftScythe, armTex, bladeTex, drawColor, bodyRot, dirFlip, false);
        }

        private static void DrawSingleScythe(Hierophant boss, HierophantArm arm, Texture2D armTex,
            Texture2D bladeTex, Color drawColor, float bodyRot, SpriteEffects dirFlip, bool flipBlade) {
            NPC npc = boss.NPC;

            Vector2 armBase = (arm.Offset * new Vector2(boss.Direction, 1f) * npc.scale).RotatedBy(bodyRot) + npc.Center;
            Main.EntitySpriteDraw(armTex, armBase - Main.screenPosition, null, drawColor,
                arm.Seg1Rot, new Vector2(12, armTex.Height / 2f), npc.scale, SpriteEffects.None);

            SpriteEffects bladeFlip = flipBlade
                ? (dirFlip == SpriteEffects.None ? SpriteEffects.FlipVertically : SpriteEffects.None)
                : dirFlip;
            Main.EntitySpriteDraw(bladeTex, arm.Seg1End - Main.screenPosition, null, drawColor,
                arm.Seg2Rot, new Vector2(12, bladeTex.Height / 2f), npc.scale, bladeFlip);

            DrawSlashTrail(boss, arm, bladeTex, bladeFlip, npc.scale);
        }

        private static void DrawSlashTrail(Hierophant boss, HierophantArm arm,
            Texture2D bladeTex, SpriteEffects bladeFlip, float scale) {
            var ctrl = boss.CombatController;
            if (ctrl.CurrentSlash == HierophantCombatController.SlashPhase.Idle) return;

            float slashProgress = 1f - (float)ctrl.SlashTimer / ctrl.GetSlashMaxDuration();
            if (slashProgress is not (> 0.3f and < 0.8f)) return;

            Color trailColor = Color.OrangeRed * 0.4f;
            for (int i = 1; i <= 3; i++) {
                float offset = i * 0.08f;
                Main.EntitySpriteDraw(bladeTex, arm.Seg1End - Main.screenPosition, null,
                    trailColor * (1f - i * 0.25f),
                    arm.Seg2Rot - offset * boss.Direction,
                    new Vector2(12, bladeTex.Height / 2f), scale, bladeFlip);
            }
        }

        #endregion
    }
}
