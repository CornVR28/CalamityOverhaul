using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    /// <summary>
    /// 天启四骑士
    /// 启示录期间围绕玩家盘旋，作为四种被动能力的视觉化实体
    /// </summary>
    internal class ApocalypseHorseman : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        private Player Owner => Main.player[Projectile.owner];
        private int HorsemanIndex => (int)Projectile.ai[0];
        private HorsemanStyle Style => HorsemanCatalog.Get(HorsemanIndex);

        private static readonly string[] TexturePaths = [
            "CalamityOverhaul/Content/Items/Magic/Elysiums/PlagueKnight",
            "CalamityOverhaul/Content/Items/Magic/Elysiums/WarriorKnight",
            "CalamityOverhaul/Content/Items/Magic/Elysiums/FamineKnight",
            "CalamityOverhaul/Content/Items/Magic/Elysiums/DeathKnight"
        ];

        private Vector2 introOrigin;
        private bool introInitialized;
        private bool arrivalBurstPlayed;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 68;
            Projectile.height = 68;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI() {
            if (!Owner.active || Owner.dead) {
                Projectile.Kill();
                return;
            }

            if (!Owner.TryGetModPlayer<ElysiumPlayer>(out var ep) || !ep.IsRevelationActive || !ep.HasHorseman(HorsemanIndex)) {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            float time = Main.GlobalTimeWrappedHourly;
            float phase = time * Style.AngularSpeed + HorsemanIndex * 1.618f + Projectile.ai[1] * 0.031f;
            Vector2 orbitOffset = GetOrbitOffset(phase, out float depth);
            Vector2 orbitTarget = Owner.Center + orbitOffset;

            if (!introInitialized) {
                introInitialized = true;
                Vector2 introDirection = (HorsemanIndex * MathHelper.PiOver2 - MathHelper.PiOver4).ToRotationVector2();
                introOrigin = Owner.Center + introDirection * Style.EntryRadius + new Vector2(0f, -Style.EntryHeight);
                Projectile.Center = introOrigin;
            }

            Projectile.localAI[0]++;
            float introProgress = MathHelper.Clamp(Projectile.localAI[0] / Style.IntroDuration, 0f, 1f);
            float easedIntro = MathHelper.SmoothStep(0f, 1f, introProgress);
            Vector2 targetPosition = introProgress < 1f
                ? Vector2.SmoothStep(introOrigin, orbitTarget, easedIntro)
                : orbitTarget;

            Vector2 previousCenter = Projectile.Center;
            float followSpeed = introProgress < 1f ? 0.36f : 0.18f;
            Projectile.Center = Vector2.Lerp(Projectile.Center, targetPosition, followSpeed);
            Projectile.velocity = Projectile.Center - previousCenter;

            if (!arrivalBurstPlayed && introProgress >= 1f) {
                arrivalBurstPlayed = true;
                SpawnArrivalBurst();
            }

            Projectile.spriteDirection = Projectile.velocity.X < 0f ? 1 : -1;
            Projectile.rotation = 0f;

            float depthLerp = (depth + 1f) * 0.5f;
            float scale = MathHelper.Lerp(Style.ScaleMin, Style.ScaleMax, depthLerp);
            Projectile.scale = scale * MathHelper.Lerp(0.8f, 1f, easedIntro);

            float lightScale = MathHelper.Lerp(0.55f, 1.15f, depthLerp) * (0.85f + (float)Math.Sin(time * 4.2f + HorsemanIndex) * 0.15f);
            Lighting.AddLight(Projectile.Center, Style.PrimaryColor.ToVector3() * lightScale * 0.55f);

            if (Main.rand.NextBool(introProgress < 1f ? 2 : 6)) {
                Vector2 velocity = Projectile.velocity * 0.22f + Main.rand.NextVector2Circular(2.2f, 2.2f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Style.DustType, velocity, 90, Style.PrimaryColor, 1.1f + depthLerp * 0.35f);
                dust.noGravity = true;
                dust.fadeIn = 0.9f;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            string texturePath = HorsemanIndex >= 0 && HorsemanIndex < TexturePaths.Length ? TexturePaths[HorsemanIndex] : TexturePaths[0];
            Asset<Texture2D> asset = ModContent.Request<Texture2D>(texturePath);
            Texture2D texture = asset.Value;
            Texture2D glowTexture = CWRAsset.SoftGlow.Value;
            SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 origin = texture.Size() * 0.5f;
            Color glow = Style.PrimaryColor;
            Color accent = Style.SecondaryColor;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;
            float auraPulse = 0.78f + (float)Math.Sin(time * (3.5f + HorsemanIndex * 0.3f)) * 0.12f;

            if (glowTexture != null) {
                Main.spriteBatch.Draw(glowTexture, drawPos, null, new Color(glow.R, glow.G, glow.B, 0) * 0.38f,
                    0f, glowTexture.Size() * 0.5f, Projectile.scale * Style.GlowScale * auraPulse, effects, 0f);

                Main.spriteBatch.Draw(glowTexture, drawPos, null, new Color(accent.R, accent.G, accent.B, 0) * 0.18f,
                    0f, glowTexture.Size() * 0.5f, Projectile.scale * (Style.GlowScale * 0.72f + 0.12f), effects, 0f);
            }

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--) {
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float factor = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Main.spriteBatch.Draw(texture, trailPos, null, glow * factor * Style.TrailOpacity, 0f, origin,
                    Projectile.scale * (0.84f + factor * 0.16f), effects, 0f);
            }

            Vector2 emblemOffset = (time * (1.7f + HorsemanIndex * 0.12f)).ToRotationVector2() * (12f + HorsemanIndex * 3f);
            if (glowTexture != null) {
                Main.spriteBatch.Draw(glowTexture, drawPos + emblemOffset, null, new Color(glow.R, glow.G, glow.B, 0) * 0.24f,
                    0f, glowTexture.Size() * 0.5f, Projectile.scale * 0.35f, effects, 0f);
                Main.spriteBatch.Draw(glowTexture, drawPos - emblemOffset * 0.8f, null, new Color(accent.R, accent.G, accent.B, 0) * 0.18f,
                    0f, glowTexture.Size() * 0.5f, Projectile.scale * 0.25f, effects, 0f);
            }

            Main.spriteBatch.Draw(texture, drawPos, null, glow * 0.22f, 0f, origin, Projectile.scale * 1.1f, effects, 0f);
            Main.spriteBatch.Draw(texture, drawPos, null, Color.White, 0f, origin, Projectile.scale, effects, 0f);
            return false;
        }

        private Vector2 GetOrbitOffset(float phase, out float depth) {
            Vector2 offset;
            switch (HorsemanIndex) {
                case 0: {
                    float swirl = phase * 1.85f;
                    offset = new Vector2(
                        (float)Math.Cos(phase) * Style.OrbitRadiusX + (float)Math.Cos(swirl) * 26f,
                        (float)Math.Sin(phase * 1.16f) * Style.OrbitRadiusY * 0.46f - 78f + (float)Math.Sin(swirl * 1.5f) * 15f
                    );
                    depth = (float)Math.Sin(phase * 1.22f + 0.7f);
                    break;
                }
                case 1: {
                    float surge = (float)Math.Pow((Math.Sin(phase * 1.4f) + 1f) * 0.5f, 3f);
                    float radiusX = Style.OrbitRadiusX + surge * 54f;
                    float slashOffset = (float)Math.Sin(phase * 3.1f) * 14f;
                    offset = new Vector2(
                        (float)Math.Cos(phase) * radiusX,
                        (float)Math.Sin(phase * 0.92f) * Style.OrbitRadiusY * 0.38f - 58f + slashOffset
                    );
                    depth = (float)Math.Sin(phase * 1.45f);
                    break;
                }
                case 2: {
                    offset = new Vector2(
                        (float)Math.Sin(phase * 0.72f) * Style.OrbitRadiusX * 0.72f + (float)Math.Cos(phase * 2.15f) * 19f,
                        (float)Math.Cos(phase * 1.28f) * Style.OrbitRadiusY * 0.58f - 96f + (float)Math.Sin(phase * 0.46f) * 11f
                    );
                    depth = (float)Math.Cos(phase * 0.88f - 0.35f);
                    break;
                }
                default: {
                    float sine = (float)Math.Sin(phase);
                    float cosine = (float)Math.Cos(phase);
                    float denom = 1f + sine * sine;
                    offset = new Vector2(
                        Style.OrbitRadiusX * 0.92f * cosine / denom,
                        Style.OrbitRadiusY * 0.52f * sine * cosine / denom - 112f + (float)Math.Sin(phase * 0.52f) * 8f
                    );
                    depth = (float)Math.Sin(phase * 0.54f + 1.1f);
                    break;
                }
            }

            offset.X += Owner.direction * 10f;
            return offset;
        }

        private void SpawnArrivalBurst() {
            for (int i = 0; i < 26; i++) {
                Vector2 velocity = Main.rand.NextVector2Circular(5.2f, 5.2f) + Projectile.velocity * 0.3f;
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Style.DustType, velocity, 80, Style.PrimaryColor, 1.45f);
                dust.noGravity = true;
                dust.fadeIn = 1f;
            }

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.95f, Pitch = -0.35f + HorsemanIndex * 0.14f }, Projectile.Center);
        }
    }
}
