using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using static CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants.HierophantUtils;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants
{
    /// <summary>
    /// 所有绘制逻辑集中于此——腿部 IK、身体、肩甲、镰刀臂与挥刀拖影
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

        #region Legs

        private static void DrawLegs(Hierophant boss, Color drawColor, Texture2D t1, Texture2D t2, Texture2D t3) {
            NPC npc = boss.NPC;
            foreach (var leg in boss.Legs) {
                float l1 = 92f * npc.scale * leg.Scale;
                float l2 = 140f * npc.scale * leg.Scale;
                float l3 = 144f * npc.scale * leg.Scale;

                float bodyRot = boss.Direction > 0 ? npc.rotation : (-MathHelper.Pi + npc.rotation);
                Vector2 start = npc.Center + (new Vector2(Math.Sign(leg.Offset.X) * 40f, 120f) * npc.scale).RotatedBy(bodyRot);

                CalculateLegJoints(start, leg.StandPoint, l1, l2, l3, out Vector2 p1, out _);
                Vector2 p2 = GetCircleIntersection(p1, l2, leg.StandPoint, l3);
                Vector2 p3 = p2 + (leg.StandPoint - p2).SafeNormalize() * l3;

                Main.EntitySpriteDraw(t1, start - Main.screenPosition, null, drawColor,
                    (p1 - start).ToRotation(), new Vector2(8, 26), npc.scale * leg.Scale, SpriteEffects.None);
                Main.EntitySpriteDraw(t2, p1 - Main.screenPosition, null, drawColor,
                    (p2 - p1).ToRotation(), new Vector2(12, 18), npc.scale * leg.Scale, SpriteEffects.None);

                float footDir = leg.Offset.X > 0 ? 1f : -1f;
                Main.EntitySpriteDraw(t3, p2 - Main.screenPosition, null, drawColor,
                    (p3 - p2).ToRotation() + footDir * MathHelper.ToRadians(24f),
                    new Vector2(54, t3.Height / 2f), npc.scale * leg.Scale,
                    leg.Offset.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            }
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

        #region Leg IK

        public static Vector2 CalculateLegJoints(Vector2 center, Vector2 legStandPoint,
            float l1, float l2, float l3, out Vector2 p1, out Vector2 p2) {
            p1 = Vector2.Zero;
            p2 = Vector2.Zero;
            if (l1 <= 0 || l2 <= 0 || l3 <= 0) return center;

            Vector2 d = legStandPoint - center;
            float dist = d.Length();
            Vector2 target = legStandPoint;

            if (dist > l1 + l2 + l3) {
                target = center + Vector2.Normalize(d) * (l1 + l2 + l3);
            }

            Vector2 downDir = new(0, 1);
            Vector2 targetDir = d.Length() > 0 ? Vector2.Normalize(d) : downDir;

            float maxDeflection = MathHelper.ToRadians(68f);
            float angleToTarget = MathF.Atan2(targetDir.Y, targetDir.X) - MathF.PI / 2f;
            float deflection = MathHelper.Clamp(angleToTarget, -maxDeflection, maxDeflection);

            float cos = MathF.Cos(deflection);
            float sin = MathF.Sin(deflection);
            Vector2 firstDir = new(downDir.X * cos - downDir.Y * sin, downDir.X * sin + downDir.Y * cos);

            p1 = center + l1 * firstDir;

            float y2 = target.Y - l3;
            float deltaY = y2 - p1.Y;
            float deltaX;
            try {
                deltaX = MathF.Sqrt(l2 * l2 - deltaY * deltaY);
            }
            catch {
                deltaX = 0;
                y2 = p1.Y - l2;
            }

            float x2Pos = p1.X + deltaX;
            float x2Neg = p1.X - deltaX;
            float x2 = MathF.Abs(x2Pos - target.X) < MathF.Abs(x2Neg - target.X) ? x2Pos : x2Neg;

            p2 = new Vector2(x2, y2);

            float distP2 = Vector2.Distance(p2, target);
            if (MathF.Abs(distP2 - l3) > 0.001f) {
                p2 = new Vector2(p1.X, p1.Y - l2);
                target = new Vector2(p2.X, p2.Y + l3);
            }

            return target;
        }

        #endregion
    }
}
