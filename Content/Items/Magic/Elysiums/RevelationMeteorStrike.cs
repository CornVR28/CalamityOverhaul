using CalamityOverhaul.Common;
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

        private const float ImpactDistance = 52f;
        private const float BaseVisualScale = 1.95f;
        private const float BaseShaderSize = 172f;

        private ref float TargetX => ref Projectile.ai[0];
        private ref float TargetY => ref Projectile.ai[1];
        private bool impacted;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 84;
            Projectile.height = 84;
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
            float visualScale = BaseVisualScale * (0.92f + speedFactor * 0.34f);

            DrawCelestialBodyShader(sb, drawPos, time, speedFactor, visualScale);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    continue;
                }

                float factor = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 stretchScale = new((0.32f + factor * 0.42f) * visualScale, (1.35f + factor * 3.6f) * speedFactor * visualScale);
                Color outerTrail = new Color(255, 135, 68, 0) * factor * 0.4f;
                Color innerTrail = new Color(255, 220, 150, 0) * factor * 0.24f;

                sb.Draw(glow, trailPos, null, outerTrail, Projectile.rotation, glow.Size() * 0.5f, stretchScale, SpriteEffects.None, 0f);
                sb.Draw(glow, trailPos + backward * (12f + factor * 18f) * visualScale, null, innerTrail, Projectile.rotation, glow.Size() * 0.5f,
                    stretchScale * new Vector2(0.58f, 0.74f), SpriteEffects.None, 0f);
            }

            for (int i = 0; i < 5; i++) {
                float offsetFactor = i / 3f;
                Vector2 plumePos = drawPos + backward * (42f + i * 28f) * visualScale + side * (float)Math.Sin(time * 8f + i * 1.2f) * (6f + i * 2.5f) * visualScale;
                Color plumeColor = Color.Lerp(new Color(255, 218, 145, 0), new Color(255, 112, 52, 0), MathHelper.Clamp(offsetFactor * 0.75f, 0f, 1f)) * (0.4f - offsetFactor * 0.05f);
                Vector2 plumeScale = new((0.52f - offsetFactor * 0.07f) * visualScale, (1.8f + i * 0.42f) * speedFactor * visualScale);
                sb.Draw(glow, plumePos, null, plumeColor, Projectile.rotation, glow.Size() * 0.5f, plumeScale, SpriteEffects.None, 0f);
            }

            sb.Draw(glow, drawPos + backward * 18f * visualScale, null, new Color(255, 128, 58, 0) * 0.58f, Projectile.rotation, glow.Size() * 0.5f,
                new Vector2(1.28f, 2.62f) * pulse * speedFactor * visualScale, SpriteEffects.None, 0f);
            sb.Draw(glow, drawPos + backward * 5f * visualScale, null, new Color(255, 205, 126, 0) * 0.76f, Projectile.rotation, glow.Size() * 0.5f,
                new Vector2(0.96f, 1.58f) * pulse * visualScale, SpriteEffects.None, 0f);
            sb.Draw(glow, drawPos + forward * 2f * visualScale, null, new Color(255, 245, 225, 0) * 0.34f, Projectile.rotation, glow.Size() * 0.5f,
                new Vector2(1.15f, 1.15f) * pulse * visualScale, SpriteEffects.None, 0f);

            if (star != null) {
                sb.Draw(star, drawPos + forward * 4f * visualScale, null, new Color(255, 180, 90, 0) * 0.78f, Projectile.rotation + time * 4.5f,
                    star.Size() * 0.5f, 0.92f * pulse * visualScale, SpriteEffects.None, 0f);
                sb.Draw(star, drawPos + backward * 6f * visualScale, null, new Color(255, 244, 220, 0) * 0.42f, -Projectile.rotation * 0.75f,
                    star.Size() * 0.5f, 0.64f * pulse * visualScale, SpriteEffects.None, 0f);
            }

            if (starWhite != null) {
                sb.Draw(starWhite, drawPos + forward * 9f * visualScale, null, Color.White with { A = 0 } * (0.9f * pulse), Projectile.rotation - time * 6.8f,
                    starWhite.Size() * 0.5f, (0.28f + pulse * 0.12f) * visualScale, SpriteEffects.None, 0f);
            }

            return false;
        }

        private void DrawCelestialBodyShader(SpriteBatch sb, Vector2 drawPos, float time, float speedFactor, float visualScale) {
            Effect shader = EffectLoader.CelestialStar?.Value;
            Texture2D canvas = CWRAsset.Placeholder_White?.Value;
            Texture2D noise = CWRAsset.Extra_193?.Value;
            if (shader == null || canvas == null || noise == null) {
                return;
            }

            shader.CurrentTechnique = shader.Techniques["CelestialBody"];
            shader.Parameters["uTime"]?.SetValue(time);
            shader.Parameters["fadeAlpha"]?.SetValue(1f);
            shader.Parameters["fallSpeed"]?.SetValue(Projectile.velocity.Length());
            shader.Parameters["coreColor"]?.SetValue(new Vector3(1f, 0.985f, 0.94f));
            shader.Parameters["surfaceColor"]?.SetValue(new Vector3(1f, 0.84f, 0.46f));
            shader.Parameters["coronaColor"]?.SetValue(new Vector3(1f, 0.44f, 0.18f));
            shader.Parameters["trailColor"]?.SetValue(new Vector3(1f, 0.24f, 0.09f));
            shader.Parameters["sphereRadius"]?.SetValue(0.235f);
            shader.Parameters["coronaWidth"]?.SetValue(0.12f);
            shader.Parameters["intensity"]?.SetValue(1.5f + speedFactor * 0.45f);
            shader.Parameters["impactProgress"]?.SetValue(0f);
            shader.Parameters["impactRadius"]?.SetValue(0f);
            shader.Parameters["uNoiseTex"]?.SetValue(noise);

            Main.graphics.GraphicsDevice.Textures[1] = noise;
            Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            shader.CurrentTechnique.Passes[0].Apply();

            sb.Draw(canvas, drawPos, null, Color.White,
                Projectile.rotation, canvas.Size() * 0.5f, BaseShaderSize * visualScale,
                SpriteEffects.None, 0f);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
