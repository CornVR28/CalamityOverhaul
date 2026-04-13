using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    //启示录Q技能：天体陨石下落
    internal class RevelationMeteorStrike : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        private const float ImpactDistance = 40f;

        private ref float TargetX => ref Projectile.ai[0];
        private ref float TargetY => ref Projectile.ai[1];
        private bool impacted;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 56;
            Projectile.height = 56;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 200;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanDamage() => false;

        public override void AI() {
            Vector2 target = new(TargetX, TargetY);
            if (target == Vector2.Zero) {
                target = Projectile.Center + Vector2.UnitY * 800f;
            }

            Vector2 toTarget = target - Projectile.Center;
            float steer = MathHelper.Clamp(toTarget.X * 0.0009f, -0.28f, 0.28f);
            Projectile.velocity.X += steer;
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.22f, -24f, 26f);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, 1f, 0.85f, 0.5f);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 backward = -forward;
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);

            if (Main.rand.NextBool()) {
                Vector2 plumeVelocity = backward * Main.rand.NextFloat(3f, 8f)
                    + side * Main.rand.NextFloat(-2f, 2f)
                    + Main.rand.NextVector2Circular(0.8f, 0.8f);
                Dust d = Dust.NewDustPerfect(Projectile.Center + backward * Main.rand.NextFloat(10f, 24f), DustID.GoldFlame, plumeVelocity, 90, default, 1.35f);
                d.noGravity = true;
            }

            if (Main.rand.NextBool(3)) {
                Vector2 emberVelocity = backward * Main.rand.NextFloat(1.5f, 4f)
                    + side * Main.rand.NextFloat(-3.2f, 3.2f);
                Dust ember = Dust.NewDustPerfect(Projectile.Center + side * Main.rand.NextFloat(-12f, 12f), DustID.Torch, emberVelocity, 110, new Color(255, 180, 90), 1.05f);
                ember.noGravity = true;
            }

            if (!impacted && Vector2.Distance(Projectile.Center, target) < ImpactDistance) {
                Impact(target);
            }
        }

        private void Impact(Vector2 target) {
            impacted = true;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target,
                Vector2.Zero,
                ModContent.ProjectileType<RevelationMeteorImpact>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner
            );

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1.1f, Pitch = -0.2f }, target);
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor) {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D glow = CWRAsset.SoftGlow.Value;
            Texture2D star = CWRAsset.StarTexture.Value;
            Texture2D starWhite = CWRAsset.StarTexture_White.Value;
            if (glow == null) return false;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 backward = -forward;
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 0.84f + (float)Math.Sin(time * 12f + Projectile.whoAmI * 0.37f) * 0.12f;
            float speedFactor = MathHelper.Clamp(Projectile.velocity.Length() / 24f, 0.65f, 1.2f);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }

                float factor = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 stretchScale = new(0.22f + factor * 0.3f, (0.9f + factor * 2.4f) * speedFactor);
                Color outerTrail = new Color(255, 135, 68, 0) * factor * 0.32f;
                Color innerTrail = new Color(255, 220, 150, 0) * factor * 0.2f;

                sb.Draw(glow, trailPos, null, outerTrail, Projectile.rotation, glow.Size() * 0.5f, stretchScale, SpriteEffects.None, 0f);
                sb.Draw(glow, trailPos + backward * (6f + factor * 8f), null, innerTrail, Projectile.rotation, glow.Size() * 0.5f, stretchScale * new Vector2(0.55f, 0.72f), SpriteEffects.None, 0f);
            }

            for (int i = 0; i < 4; i++) {
                float offsetFactor = i / 3f;
                Vector2 plumePos = drawPos + backward * (24f + i * 17f) + side * (float)Math.Sin(time * 8f + i * 1.2f) * (3f + i * 1.5f);
                Color plumeColor = Color.Lerp(new Color(255, 218, 145, 0), new Color(255, 112, 52, 0), offsetFactor) * (0.34f - offsetFactor * 0.06f);
                Vector2 plumeScale = new(0.4f - offsetFactor * 0.08f, (1.4f + i * 0.3f) * speedFactor);
                sb.Draw(glow, plumePos, null, plumeColor, Projectile.rotation, glow.Size() * 0.5f, plumeScale, SpriteEffects.None, 0f);
            }

            sb.Draw(glow, drawPos + backward * 10f, null, new Color(255, 128, 58, 0) * 0.52f, Projectile.rotation, glow.Size() * 0.5f,
                new Vector2(0.92f, 1.8f) * pulse * speedFactor, SpriteEffects.None, 0f);
            sb.Draw(glow, drawPos + backward * 2f, null, new Color(255, 205, 126, 0) * 0.68f, Projectile.rotation, glow.Size() * 0.5f,
                new Vector2(0.68f, 1.15f) * pulse, SpriteEffects.None, 0f);

            if (star != null) {
                sb.Draw(star, drawPos + forward * 2f, null, new Color(255, 180, 90, 0) * 0.72f, Projectile.rotation + time * 4.5f,
                    star.Size() * 0.5f, 0.5f * pulse, SpriteEffects.None, 0f);
                sb.Draw(star, drawPos + backward * 4f, null, new Color(255, 244, 220, 0) * 0.38f, -Projectile.rotation * 0.75f,
                    star.Size() * 0.5f, 0.34f * pulse, SpriteEffects.None, 0f);
            }

            if (starWhite != null) {
                sb.Draw(starWhite, drawPos + forward * 6f, null, Color.White with { A = 0 } * (0.82f * pulse), Projectile.rotation - time * 6.8f,
                    starWhite.Size() * 0.5f, 0.16f + pulse * 0.08f, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
