using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    //陨石冲击波
    internal class RevelationMeteorImpact : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        private const int MaxTime = 34;
        private const float DamageRadius = 420f;
        private bool dealtDamage;

        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = MaxTime;
        }

        public override bool? CanDamage() => false;

        public override void AI() {
            float progress = 1f - Projectile.timeLeft / (float)MaxTime;
            float radius = MathHelper.Lerp(40f, DamageRadius, progress);

            if (!dealtDamage) {
                dealtDamage = true;
                foreach (NPC npc in Main.npc) {
                    if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                    if (Vector2.Distance(npc.Center, Projectile.Center) <= DamageRadius) {
                        Main.player[Projectile.owner].ApplyDamageToNPC(npc, Projectile.damage, Projectile.knockBack, 0, true);
                    }
                }
            }

            if (Projectile.timeLeft % 4 == 0) {
                for (int i = 0; i < 10; i++) {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 9f);
                    Dust d = Dust.NewDustPerfect(Projectile.Center + vel * 2f, DustID.GoldFlame, vel, 80, default, 1.25f);
                    d.noGravity = true;
                }
            }

            if (Projectile.timeLeft % 3 == 0) {
                for (int i = 0; i < 4; i++) {
                    Vector2 sparkVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(6f, 13f);
                    Dust spark = Dust.NewDustPerfect(Projectile.Center + sparkVel * 1.8f, DustID.Torch, sparkVel, 110, new Color(255, 160, 90), 1.1f);
                    spark.noGravity = true;
                }
            }

            Lighting.AddLight(Projectile.Center, 1.2f, 0.95f, 0.55f);
        }

        public override bool PreDraw(ref Color lightColor) {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D glow = CWRAsset.SoftGlow.Value;
            Texture2D star = CWRAsset.StarTexture_White.Value;
            if (glow == null) return false;

            float progress = 1f - Projectile.timeLeft / (float)MaxTime;
            float alpha = 1f - progress;
            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 0.86f + (float)Math.Sin(time * 14f + Projectile.whoAmI) * 0.1f;
            float outer = MathHelper.Lerp(0.55f, 2.9f, progress);
            float middle = MathHelper.Lerp(0.35f, 2f, progress);
            float inner = MathHelper.Lerp(0.18f, 1.2f, progress);

            Vector2 pos = Projectile.Center - Main.screenPosition;
            sb.Draw(glow, pos, null, new Color(255, 138, 62, 0) * alpha * 0.52f, time * 0.65f, glow.Size() * 0.5f, new Vector2(outer * 1.08f, outer * 0.92f), SpriteEffects.None, 0f);
            sb.Draw(glow, pos, null, new Color(255, 214, 130, 0) * alpha * 0.72f, -time * 0.9f, glow.Size() * 0.5f, middle * pulse, SpriteEffects.None, 0f);
            sb.Draw(glow, pos, null, new Color(255, 245, 220, 0) * alpha * 0.58f, 0f, glow.Size() * 0.5f, inner, SpriteEffects.None, 0f);

            if (star != null) {
                sb.Draw(star, pos, null, Color.White with { A = 0 } * alpha * 0.45f, time * 2.1f, star.Size() * 0.5f, 0.45f + progress * 0.6f, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
