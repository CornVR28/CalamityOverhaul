using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Projectiles
{
    /// <summary>
    /// 皇室凝胶——史莱姆雨 / 冲撞拖尾 共用的小型敌对弹幕。
    /// <br/>携带轻微重力，撞地后短暂向四周溅出小水花后消失。
    /// </summary>
    internal class KingSlimeRoyalGelDropletProj : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        private ref float SplashTimer => ref Projectile.localAI[0];
        private bool Splashing => SplashTimer > 0f;

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI() {
            if (Splashing) {
                //溅射期间停止下落，逐渐缩小
                Projectile.velocity *= 0.4f;
                SplashTimer++;
                if (SplashTimer > 14f) Projectile.Kill();
                return;
            }

            //轻微重力
            Projectile.velocity.Y = MathHelper.Min(Projectile.velocity.Y + 0.18f, 14f);
            //空气阻力
            Projectile.velocity.X *= 0.995f;

            //旋转跟随速度
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            //下落粒子尾迹
            if (!VaultUtils.isServer && Main.rand.NextBool(2)) {
                Vector2 spawn = Projectile.Center + Main.rand.NextVector2Circular(4, 4);
                Dust dust = Dust.NewDustPerfect(spawn, DustID.Blood,
                    Projectile.velocity * 0.2f, 100, default, 1.0f);
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0.90f, 0.12f, 0.05f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            if (Splashing) return false;
            //开始溅射
            SplashTimer = 1f;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 16;

            if (!VaultUtils.isServer) {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit1, Projectile.Center);
                for (int i = 0; i < 8; i++) {
                    Vector2 vel = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-4f, -1f));
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Blood,
                        vel, 100, default, 1.3f);
                    dust.noGravity = true;
                }
            }
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Splashing) return false;
            return null;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D glow = CWRAsset.SoftGlow.Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float scaleMul;
            float coreAlpha;
            Color royal = new Color(200, 30, 30);
            Color core = Color.Red;

            if (Splashing) {
                float t = MathHelper.Clamp(SplashTimer / 14f, 0f, 1f);
                scaleMul = MathHelper.Lerp(1.4f, 0.4f, t);
                coreAlpha = 1f - t;
                Main.EntitySpriteDraw(glow, drawPos, null, royal * (0.7f * coreAlpha),
                    0f, glow.Size() / 2f, 1.4f * scaleMul, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(glow, drawPos, null, core * (0.6f * coreAlpha),
                    0f, glow.Size() / 2f, 0.7f * scaleMul, SpriteEffects.None, 0);
            }
            else {
                //飞行：拉长的水滴形
                float pulse = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 14f);
                Main.EntitySpriteDraw(glow, drawPos, null, royal * 0.65f,
                    0f, glow.Size() / 2f, 0.95f * pulse, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(glow, drawPos, null, core * 0.5f,
                    0f, glow.Size() / 2f, 0.5f * pulse, SpriteEffects.None, 0);
            }

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
