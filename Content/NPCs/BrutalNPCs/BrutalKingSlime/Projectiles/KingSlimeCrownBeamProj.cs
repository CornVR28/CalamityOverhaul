using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Projectiles
{
    /// <summary>
    /// 皇冠光柱：从皇冠位置（Center + ai0/ai1 偏移）射向 Center 落点的"皇室光束"。
    /// <br/>分为：警示锁定线（前期）→ 命中光柱（后期），命中带短暂存在。
    /// </summary>
    internal class KingSlimeCrownBeamProj : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        private const int WarnTime = 28;
        private const int StrikeTime = 12;
        private const int FadeTime = 12;
        private const int TotalTime = WarnTime + StrikeTime + FadeTime;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
        }

        public override void SetDefaults() {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalTime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        private Vector2 BeamStart() => Projectile.Center + new Vector2(Projectile.ai[0], Projectile.ai[1]);
        private Vector2 BeamEnd() => Projectile.Center;

        private int Phase {
            get {
                int t = TotalTime - Projectile.timeLeft;
                if (t < WarnTime) return 0;       //警示
                if (t < WarnTime + StrikeTime) return 1;  //命中
                return 2;                                 //淡出
            }
        }

        public override void AI() {
            int phase = Phase;
            if (phase == 1) {
                Vector2 start = BeamStart();
                Vector2 end = BeamEnd();
                Vector2 mid = (start + end) * 0.5f;
                float light = 0.8f;
                Lighting.AddLight(mid, 0.8f * light, 0.7f * light, 1.0f * light);

                //命中阶段散落黄色亮粒
                if (!VaultUtils.isServer) {
                    Vector2 dir = (end - start).SafeNormalize(Vector2.UnitY);
                    for (int i = 0; i < 2; i++) {
                        float t = Main.rand.NextFloat();
                        Vector2 pos = Vector2.Lerp(start, end, t) + dir.RotatedBy(MathHelper.PiOver2)
                            * Main.rand.NextFloat(-12f, 12f);
                        Dust dust = Dust.NewDustPerfect(pos, DustID.GoldFlame,
                            dir * Main.rand.NextFloat(2f, 5f), 100, default, 1.4f);
                        dust.noGravity = true;
                    }
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Phase != 1) return false;
            Vector2 start = BeamStart();
            Vector2 end = BeamEnd();
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(
                new Vector2(targetHitbox.X, targetHitbox.Y),
                new Vector2(targetHitbox.Width, targetHitbox.Height),
                start, end, 24f, ref _);
        }

        public override bool PreDraw(ref Color lightColor) {
            int phase = Phase;
            int t = TotalTime - Projectile.timeLeft;

            Vector2 start = BeamStart();
            Vector2 end = BeamEnd();
            Vector2 dir = (end - start).SafeNormalize(Vector2.UnitY);
            float length = (end - start).Length();
            float rotation = dir.ToRotation();

            SpriteBatch sb = Main.spriteBatch;
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D glowTex = CWRAsset.SoftGlow.Value;
            Texture2D lineTex = CWRAsset.LightShot.Value;

            if (phase == 0) {
                //警示线：细瘦渐入虚线
                float prog = t / (float)WarnTime;
                float warnAlpha = MathHelper.SmoothStep(0f, 1f, prog) * 0.85f;
                Color warnColor = Color.Lerp(new Color(255, 220, 110), new Color(255, 90, 40),
                    0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 22f));

                int dashes = 22;
                for (int i = 0; i < dashes; i++) {
                    float ti = i / (float)dashes;
                    float visMask = (((i + (int)(Main.GlobalTimeWrappedHourly * 14f)) % 2 == 0) ? 1f : 0.35f);
                    Vector2 pos = Vector2.Lerp(start, end, ti) - Main.screenPosition;
                    Main.EntitySpriteDraw(lineTex, pos, null, warnColor * warnAlpha * visMask,
                        rotation, new Vector2(0, lineTex.Height / 2f),
                        new Vector2(length / dashes / 256f * 0.7f, 0.10f * (0.5f + 0.5f * prog)),
                        SpriteEffects.None, 0);
                }

                //起点皇冠光斑
                Main.EntitySpriteDraw(glowTex, start - Main.screenPosition, null,
                    new Color(255, 230, 130) * (warnAlpha * 0.9f),
                    0f, glowTex.Size() / 2f, 0.7f + prog * 0.6f, SpriteEffects.None, 0);
                //终点警示斑
                Main.EntitySpriteDraw(glowTex, end - Main.screenPosition, null,
                    new Color(255, 100, 50) * (warnAlpha * 0.9f),
                    0f, glowTex.Size() / 2f, 0.5f + prog * 0.5f, SpriteEffects.None, 0);
            }
            else if (phase == 1) {
                //命中阶段：皇室光柱
                int strikeT = t - WarnTime;
                float strikeProg = strikeT / (float)StrikeTime;
                float coreAlpha = MathHelper.SmoothStep(1f, 0.6f, strikeProg);

                int segs = 24;
                Color core = new Color(255, 245, 180);
                Color edge = new Color(255, 200, 80);
                for (int i = 0; i < segs; i++) {
                    float ti = i / (float)segs;
                    Vector2 pos = Vector2.Lerp(start, end, ti) - Main.screenPosition;
                    float widthScale = 0.30f + 0.10f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 30f + ti * 8f);
                    Main.EntitySpriteDraw(lineTex, pos, null, core * coreAlpha,
                        rotation, new Vector2(0, lineTex.Height / 2f),
                        new Vector2(length / segs / 256f * 1.05f, widthScale),
                        SpriteEffects.None, 0);
                    Main.EntitySpriteDraw(lineTex, pos, null, edge * coreAlpha * 0.7f,
                        rotation, new Vector2(0, lineTex.Height / 2f),
                        new Vector2(length / segs / 256f * 1.10f, widthScale * 1.6f),
                        SpriteEffects.None, 0);
                }

                //端点爆闪
                float burstScale = 1.4f + 0.4f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 25f);
                Main.EntitySpriteDraw(glowTex, end - Main.screenPosition, null, core * coreAlpha * 1.2f,
                    0f, glowTex.Size() / 2f, burstScale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(glowTex, start - Main.screenPosition, null, edge * coreAlpha,
                    0f, glowTex.Size() / 2f, 0.9f, SpriteEffects.None, 0);
            }
            else {
                //淡出
                int fadeT = t - WarnTime - StrikeTime;
                float fadeProg = fadeT / (float)FadeTime;
                float a = 1f - fadeProg;
                Color core = new Color(255, 200, 100);

                int segs = 16;
                for (int i = 0; i < segs; i++) {
                    float ti = i / (float)segs;
                    Vector2 pos = Vector2.Lerp(start, end, ti) - Main.screenPosition;
                    Main.EntitySpriteDraw(lineTex, pos, null, core * a * 0.6f,
                        rotation, new Vector2(0, lineTex.Height / 2f),
                        new Vector2(length / segs / 256f * 1.0f, 0.30f * a),
                        SpriteEffects.None, 0);
                }
                Main.EntitySpriteDraw(glowTex, end - Main.screenPosition, null, core * a * 0.6f,
                    0f, glowTex.Size() / 2f, 1.0f * a, SpriteEffects.None, 0);
            }

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
