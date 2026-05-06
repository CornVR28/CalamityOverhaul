using CalamityOverhaul.Common;
using InnoVault.Trails;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Projectiles
{
    /// <summary>
    /// 皇冠光柱：从皇冠位置（Center + ai0/ai1 偏移）射向 Center 落点的"皇室光束"。
    /// <br/>分为：警示锁定（前期）→ 命中光柱（中期）→ 残辉淡出（后期）。
    /// <br/>使用 Trail 顶点条带 + KingSlimeRoyalBeam 自定义着色器渲染，红金皇室主题。
    /// </summary>
    internal class KingSlimeCrownBeamProj : ModProjectile, IPrimitiveDrawable, IAdditiveDrawable
    {
        public override string Texture => CWRConstant.Placeholder;

        private const int WarnTime = 28;
        private const int StrikeTime = 12;
        private const int FadeTime = 12;
        private const int TotalTime = WarnTime + StrikeTime + FadeTime;
        private const int PointCount = 28;
        private const float BeamHitWidth = 28f;

        //皇室红金主题色（Vector3 仅在着色器参数赋值时构造）
        private static readonly Color RoyalCoreColor = new(255, 248, 210); //白热核心
        private static readonly Color RoyalGoldColor = new(255, 198, 70);  //皇室金辉
        private static readonly Color RoyalRedColor = new(220, 35, 30);    //深皇红外晕

        private Trail trail;
        private Vector2[] beamPoints;
        private float seed;

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

        /// <summary>0=警示 1=命中 2=淡出</summary>
        private int Phase {
            get {
                int t = TotalTime - Projectile.timeLeft;
                if (t < WarnTime) return 0;
                if (t < WarnTime + StrikeTime) return 1;
                return 2;
            }
        }

        private float WarnProgress => MathHelper.Clamp((TotalTime - Projectile.timeLeft) / (float)WarnTime, 0f, 1f);

        private float StrikeProgress => MathHelper.Clamp((TotalTime - Projectile.timeLeft - WarnTime) / (float)StrikeTime, 0f, 1f);

        private float FadeProgress => MathHelper.Clamp((TotalTime - Projectile.timeLeft - WarnTime - StrikeTime) / (float)FadeTime, 0f, 1f);

        public override void OnSpawn(IEntitySource source) {
            seed = Main.rand.NextFloat(0f, 100f);
        }

        public override void AI() {
            int phase = Phase;
            Vector2 start = BeamStart();
            Vector2 end = BeamEnd();

            //顶点路径：均匀填充直线，每帧刷新一次（终点会偏移很小，但保持稳健）
            beamPoints ??= new Vector2[PointCount];
            for (int i = 0; i < PointCount; i++) {
                float t = i / (float)(PointCount - 1);
                beamPoints[i] = Vector2.Lerp(start, end, t);
            }

            if (phase == 0) {
                //警示阶段：皇冠端持续溢出蓄力金尘
                if (!VaultUtils.isServer) {
                    if (Main.rand.NextBool(2)) {
                        Vector2 dustVel = Main.rand.NextVector2Circular(2f, 2f);
                        Dust dust = Dust.NewDustPerfect(start + Main.rand.NextVector2Circular(8f, 8f),
                            DustID.GoldFlame, dustVel, 100, default, 1.2f);
                        dust.noGravity = true;
                    }
                }

                //微光照亮：警示偏暗的金光
                Lighting.AddLight(start, 0.6f * 1.0f, 0.45f * 1.0f, 0.15f * 1.0f);
            }
            else if (phase == 1) {
                //命中阶段：沿光束打入大量金红粒子，并加强照明
                Vector2 dir = (end - start).SafeNormalize(Vector2.UnitY);
                Vector2 mid = (start + end) * 0.5f;
                Lighting.AddLight(mid, 1.0f, 0.55f, 0.25f);
                Lighting.AddLight(end, 1.0f, 0.65f, 0.30f);
                Lighting.AddLight(start, 1.0f, 0.7f, 0.35f);

                if (!VaultUtils.isServer) {
                    for (int i = 0; i < 3; i++) {
                        float t = Main.rand.NextFloat();
                        Vector2 pos = Vector2.Lerp(start, end, t)
                            + dir.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-14f, 14f);
                        int dustId = Main.rand.NextBool(3) ? DustID.RedTorch : DustID.GoldFlame;
                        Dust dust = Dust.NewDustPerfect(pos, dustId,
                            dir * Main.rand.NextFloat(2f, 6f), 100, default, 1.4f);
                        dust.noGravity = true;
                    }

                    //落点处不规则爆裂尘
                    if (StrikeProgress < 0.4f) {
                        for (int i = 0; i < 2; i++) {
                            Vector2 burst = Main.rand.NextVector2Circular(6f, 6f);
                            Dust dust = Dust.NewDustPerfect(end + burst,
                                Main.rand.NextBool() ? DustID.RedTorch : DustID.GoldFlame,
                                burst.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(3f, 8f),
                                100, default, 1.6f);
                            dust.noGravity = true;
                        }
                    }
                }
            }
            else {
                //淡出阶段：余辉
                float a = 1f - FadeProgress;
                Lighting.AddLight((start + end) * 0.5f, 0.6f * a, 0.4f * a, 0.2f * a);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (Phase != 1) return false;
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(
                new Vector2(targetHitbox.X, targetHitbox.Y),
                new Vector2(targetHitbox.Width, targetHitbox.Height),
                BeamStart(), BeamEnd(), BeamHitWidth, ref _);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        private float WidthFunction(float progress) {
            //progress 0=皇冠端, 1=落点端
            //核心宽度按阶段不同
            int phase = Phase;
            float baseWidth;
            float taper = MathF.Sin(MathHelper.Clamp(progress * MathHelper.Pi, 0f, MathHelper.Pi));
            taper = 0.55f + 0.45f * taper;

            if (phase == 0) {
                //警示阶段：很窄
                baseWidth = 8f + 6f * WarnProgress;
            }
            else if (phase == 1) {
                //命中阶段：粗光柱，初瞬暴胀
                float surge = 1f + 0.6f * (1f - MathHelper.SmoothStep(0f, 0.35f, StrikeProgress));
                float pulse = 1f + 0.07f * MathF.Sin((float)Main.timeForVisualEffects * 0.32f + progress * 8f);
                baseWidth = 38f * surge * pulse;
            }
            else {
                //淡出阶段：渐缩
                baseWidth = MathHelper.Lerp(34f, 12f, FadeProgress);
            }

            return baseWidth * taper;
        }

        private static Color ColorFunction(Vector2 _) => Color.White;

        void IPrimitiveDrawable.DrawPrimitives() {
            if (beamPoints == null) return;

            Effect shader = EffectLoader.KingSlimeRoyalBeam?.Value;
            if (shader == null) return;
            Texture2D noise = CWRAsset.Extra_193?.Value;
            if (noise == null) return;

            trail ??= new Trail(beamPoints, WidthFunction, ColorFunction);
            trail.TrailPositions = beamPoints;

            int phase = Phase;
            float phaseFloat = phase; //0/1/2
            float fadeAlpha;
            if (phase == 0) {
                fadeAlpha = MathHelper.Lerp(0.3f, 1.0f, WarnProgress);
            }
            else if (phase == 1) {
                fadeAlpha = 1.25f;
            }
            else {
                fadeAlpha = MathHelper.Lerp(1.0f, 0f, FadeProgress);
            }

            shader.Parameters["transformMatrix"]?.SetValue(VaultUtils.GetTransfromMatrix());
            shader.Parameters["uTime"]?.SetValue((float)Main.timeForVisualEffects * 0.04f);
            shader.Parameters["fadeAlpha"]?.SetValue(MathHelper.Clamp(fadeAlpha, 0f, 1.4f));
            shader.Parameters["phase"]?.SetValue(phaseFloat);
            shader.Parameters["warnProg"]?.SetValue(WarnProgress);
            shader.Parameters["strikeProg"]?.SetValue(StrikeProgress);
            shader.Parameters["fadeProg"]?.SetValue(FadeProgress);
            shader.Parameters["seed"]?.SetValue(seed);
            shader.Parameters["coreColor"]?.SetValue(RoyalCoreColor.ToVector3());
            shader.Parameters["goldColor"]?.SetValue(RoyalGoldColor.ToVector3());
            shader.Parameters["redColor"]?.SetValue(RoyalRedColor.ToVector3());
            shader.Parameters["uNoiseTex"]?.SetValue(noise);

            GraphicsDevice device = Main.graphics.GraphicsDevice;
            device.BlendState = BlendState.Additive;
            trail.DrawTrail(shader);
            device.BlendState = BlendState.AlphaBlend;
        }

        void IAdditiveDrawable.DrawAdditiveAfterNon(SpriteBatch spriteBatch) {
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            Texture2D star = CWRAsset.StarTexture?.Value;
            if (glow == null) return;

            int phase = Phase;
            Vector2 start = BeamStart();
            Vector2 end = BeamEnd();
            Vector2 startScreen = start - Main.screenPosition;
            Vector2 endScreen = end - Main.screenPosition;
            Vector2 glowOrigin = glow.Size() * 0.5f;

            if (phase == 0) {
                //警示阶段：皇冠端持续聚光，落点端微闪锁定环
                float wp = WarnProgress;
                float pulse = 0.65f + 0.35f * MathF.Sin((float)Main.timeForVisualEffects * 0.3f);

                spriteBatch.Draw(glow, startScreen, null,
                    RoyalGoldColor * (0.55f * wp * pulse), 0f,
                    glowOrigin, 1.1f + 0.6f * wp, SpriteEffects.None, 0f);
                spriteBatch.Draw(glow, startScreen, null,
                    RoyalCoreColor * (0.6f * wp), 0f,
                    glowOrigin, 0.55f + 0.3f * wp, SpriteEffects.None, 0f);

                //锁定端：小红圈预警
                spriteBatch.Draw(glow, endScreen, null,
                    RoyalRedColor * (0.45f * wp * pulse), 0f,
                    glowOrigin, 0.65f + 0.45f * wp, SpriteEffects.None, 0f);
            }
            else if (phase == 1) {
                //命中阶段：双端皇室爆闪，落点尤其暴烈
                float sp = StrikeProgress;
                float burst = 1f - MathHelper.SmoothStep(0f, 0.3f, sp);
                float corePulse = 1f + 0.25f * MathF.Sin((float)Main.timeForVisualEffects * 0.5f);

                //皇冠端
                spriteBatch.Draw(glow, startScreen, null,
                    RoyalRedColor * 0.7f, 0f,
                    glowOrigin, 2.0f + burst * 1.0f, SpriteEffects.None, 0f);
                spriteBatch.Draw(glow, startScreen, null,
                    RoyalGoldColor * 0.95f * corePulse, 0f,
                    glowOrigin, 1.2f + burst * 0.6f, SpriteEffects.None, 0f);
                spriteBatch.Draw(glow, startScreen, null,
                    RoyalCoreColor * 1.1f * corePulse, 0f,
                    glowOrigin, 0.7f + burst * 0.4f, SpriteEffects.None, 0f);

                //落点端：暴闪
                spriteBatch.Draw(glow, endScreen, null,
                    RoyalRedColor * 0.85f, 0f,
                    glowOrigin, 3.4f + burst * 2.4f, SpriteEffects.None, 0f);
                spriteBatch.Draw(glow, endScreen, null,
                    RoyalGoldColor * 1.1f * corePulse, 0f,
                    glowOrigin, 2.0f + burst * 1.4f, SpriteEffects.None, 0f);
                spriteBatch.Draw(glow, endScreen, null,
                    RoyalCoreColor * 1.3f * corePulse, 0f,
                    glowOrigin, 1.0f + burst * 0.8f, SpriteEffects.None, 0f);

                //星形十字耀斑（仅命中初期）
                if (star != null && burst > 0.05f) {
                    Vector2 starOrigin = star.Size() * 0.5f;
                    float starScale = 0.6f + burst * 1.1f;
                    spriteBatch.Draw(star, endScreen, null,
                        RoyalCoreColor * burst * 0.95f,
                        Main.GlobalTimeWrappedHourly * 1.2f,
                        starOrigin, starScale, SpriteEffects.None, 0f);
                    spriteBatch.Draw(star, endScreen, null,
                        RoyalGoldColor * burst * 0.7f,
                        -Main.GlobalTimeWrappedHourly * 0.8f,
                        starOrigin, starScale * 0.75f, SpriteEffects.None, 0f);
                }
            }
            else {
                //淡出阶段：金红余辉柔散
                float a = 1f - FadeProgress;
                spriteBatch.Draw(glow, endScreen, null,
                    RoyalRedColor * a * 0.55f, 0f,
                    glowOrigin, 2.0f * a, SpriteEffects.None, 0f);
                spriteBatch.Draw(glow, endScreen, null,
                    RoyalGoldColor * a * 0.6f, 0f,
                    glowOrigin, 1.0f * a, SpriteEffects.None, 0f);
                spriteBatch.Draw(glow, startScreen, null,
                    RoyalGoldColor * a * 0.4f, 0f,
                    glowOrigin, 0.85f * a, SpriteEffects.None, 0f);
            }
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
