using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    /// <summary>
    /// 独立天雷弹幕：Q键随时召唤的神圣雷击
    /// 自动寻找最近敌人劈击，无敌人时劈向光标位置
    /// </summary>
    internal class DivineThunderStrike : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        private Player Owner => Main.player[Projectile.owner];

        //天雷数据
        private struct LightningBolt
        {
            public List<Vector2> Segments;
            public float Intensity;
        }

        private readonly List<LightningBolt> bolts = [];
        private bool initialized;
        private Vector2 strikeTarget;
        private const float SeekRange = 800f;

        public override void SetDefaults() {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 25;
        }

        public override bool? CanDamage() => false; //伤害在初始化时直接施加

        public override void AI() {
            if (!initialized) {
                initialized = true;
                InitializeStrike();
            }

            //动态照明
            float alpha = Projectile.timeLeft / 25f;
            Lighting.AddLight(strikeTarget, 1.5f * alpha, 1.3f * alpha, 0.7f * alpha);
        }

        private void InitializeStrike() {
            //寻找最近敌人
            NPC target = null;
            float closestDist = SeekRange;
            foreach (NPC npc in Main.npc) {
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                float d = Vector2.Distance(npc.Center, Owner.Center);
                if (d < closestDist) {
                    closestDist = d;
                    target = npc;
                }
            }

            if (target != null) {
                strikeTarget = target.Center;
                //天雷伤害
                Owner.ApplyDamageToNPC(target, Projectile.damage, 6f, 0, true);
            }
            else {
                //无敌人时劈向光标
                strikeTarget = Main.MouseWorld;
            }

            Projectile.Center = strikeTarget;

            //主闪电
            Vector2 skyPos = new(strikeTarget.X + Main.rand.NextFloat(-50f, 50f), strikeTarget.Y - 900f);
            bolts.Add(new LightningBolt {
                Segments = GenerateLightningPath(skyPos, strikeTarget),
                Intensity = 1f
            });

            //1~2条分叉
            int branchCount = Main.rand.Next(1, 3);
            var mainSegs = bolts[0].Segments;
            for (int b = 0; b < branchCount; b++) {
                int branchIdx = Main.rand.Next(mainSegs.Count / 4, mainSegs.Count * 2 / 3);
                Vector2 branchEnd = strikeTarget + Main.rand.NextVector2Circular(100f, 60f);
                bolts.Add(new LightningBolt {
                    Segments = GenerateLightningPath(mainSegs[branchIdx], branchEnd),
                    Intensity = 0.5f
                });
            }

            //雷声
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 1f, Pitch = Main.rand.NextFloat(-0.3f, 0.2f) }, strikeTarget);

            //落点粒子
            for (int i = 0; i < 12; i++) {
                Vector2 vel = Main.rand.NextVector2Circular(5f, 4f);
                vel.Y = -Math.Abs(vel.Y);
                int dustType = Main.rand.NextBool(3) ? DustID.SilverFlame : DustID.GoldFlame;
                Dust dust = Dust.NewDustPerfect(strikeTarget, dustType, vel, 80, default, 1.8f);
                dust.noGravity = true;
            }
        }

        private static List<Vector2> GenerateLightningPath(Vector2 from, Vector2 to) {
            List<Vector2> points = [from];
            Vector2 dir = (to - from).SafeNormalize(Vector2.UnitY);
            Vector2 perp = dir.RotatedBy(MathHelper.PiOver2);
            float totalDist = Vector2.Distance(from, to);
            int segCount = Math.Max(8, (int)(totalDist / 28f));
            for (int i = 1; i < segCount; i++) {
                float t = i / (float)segCount;
                Vector2 basePos = Vector2.Lerp(from, to, t);
                float zigzag = MathF.Sin(t * MathHelper.Pi) * 40f;
                float offset = (Main.rand.NextFloat() - 0.5f) * 2f * zigzag;
                points.Add(basePos + perp * offset);
            }
            points.Add(to);
            return points;
        }

        public override bool PreDraw(ref Color lightColor) {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D pixel = CWRAsset.Placeholder_White.Value;
            if (pixel == null) return false;

            float alpha = Projectile.timeLeft / 25f;
            //前几帧更亮
            float flashBoost = Projectile.timeLeft > 20 ? 1.5f : 1f;
            float finalAlpha = alpha * flashBoost;

            foreach (var bolt in bolts) {
                if (bolt.Segments == null || bolt.Segments.Count < 2) continue;
                float boltAlpha = finalAlpha * bolt.Intensity;

                for (int i = 0; i < bolt.Segments.Count - 1; i++) {
                    Vector2 start = bolt.Segments[i] - Main.screenPosition;
                    Vector2 end = bolt.Segments[i + 1] - Main.screenPosition;

                    //内核(白色)
                    DrawSegment(sb, pixel, start, end, 2.5f, new Color(255, 255, 240, 0) * boltAlpha);
                    //中间(金色)
                    DrawSegment(sb, pixel, start, end, 5f, new Color(255, 215, 80, 0) * boltAlpha * 0.7f);
                    //外层(蓝白光晕)
                    DrawSegment(sb, pixel, start, end, 10f, new Color(180, 200, 255, 0) * boltAlpha * 0.3f);
                }
            }
            return false;
        }

        private static void DrawSegment(SpriteBatch sb, Texture2D pixel, Vector2 start, Vector2 end, float thickness, Color color) {
            Vector2 diff = end - start;
            float length = diff.Length();
            if (length < 1f) return;
            sb.Draw(pixel, start, new Rectangle(0, 0, 1, 1), color, diff.ToRotation(),
                Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
        }
    }
}
