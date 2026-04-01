using CalamityOverhaul.Common;
using CalamityOverhaul.Content.PRTTypes;
using InnoVault;
using InnoVault.PRT;
using InnoVault.Trails;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces
{
    /// <summary>
    /// 赛博追踪能量光束弹幕
    /// <br/>Cyberspace 系统的攻击手段 —— 带微追踪的拖尾能量光束
    /// <br/>蓝/黄/青三种随机颜色主题，光球头部，方形科幻粒子拖尾
    /// <br/>使用 <see cref="Trail"/> 条带 + CyberTraceBeam.fx 着色器渲染
    /// </summary>
    internal class CyberTraceBeamProj : ModProjectile, IPrimitiveDrawable, IAdditiveDrawable
    {
        public override string Texture => CWRConstant.Placeholder;

        #region 常量与配置

        private const int TrailCacheLen = 40;
        private const int MaxLife = 180;
        private const float Speed = 14f;
        private const float HomingStrength = 0.025f;
        private const float HomingRange = 900f;
        private const int ParticleInterval = 3;

        #endregion

        #region 颜色主题

        private struct ColorTheme
        {
            public Color Core;
            public Color Glow;
            public Color Aura;
            public Color ParticleMain;
            public Color ParticleEdge;

            public Vector3 CoreVec => Core.ToVector3();
            public Vector3 GlowVec => Glow.ToVector3();
            public Vector3 AuraVec => Aura.ToVector3();
        }

        private static readonly ColorTheme[] Themes = {
            // 蓝色主题
            new() {
                Core = new Color(100, 180, 255),
                Glow = new Color(30, 100, 230),
                Aura = new Color(10, 40, 120),
                ParticleMain = new Color(80, 160, 255),
                ParticleEdge = new Color(30, 80, 200),
            },
            // 黄色主题
            new() {
                Core = new Color(255, 220, 80),
                Glow = new Color(230, 170, 30),
                Aura = new Color(150, 100, 15),
                ParticleMain = new Color(255, 200, 60),
                ParticleEdge = new Color(200, 150, 20),
            },
            // 青色主题
            new() {
                Core = new Color(80, 255, 230),
                Glow = new Color(20, 200, 180),
                Aura = new Color(10, 120, 110),
                ParticleMain = new Color(60, 240, 210),
                ParticleEdge = new Color(15, 170, 150),
            },
        };

        #endregion

        #region 实例字段

        private Trail trail;
        private Vector2[] trailPositions;
        private int themeIndex;
        private ColorTheme theme;
        private float fadeAlpha;
        private int particleTimer;

        #endregion

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = TrailCacheLen;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 3;
            Projectile.timeLeft = MaxLife;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.extraUpdates = 2;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI() {
            // 初始化颜色主题（仅第一帧）
            if (Projectile.localAI[0] == 0f) {
                themeIndex = (int)Projectile.ai[0] % Themes.Length;
                if (themeIndex < 0) themeIndex = 0;
                theme = Themes[themeIndex];
                Projectile.localAI[0] = 1f;
            }

            // 微追踪：寻找最近NPC并柔和偏转
            NPC target = Projectile.Center.FindClosestNPC(HomingRange);
            if (target != null) {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
                float currentAngle = Projectile.velocity.ToRotation();
                float targetAngle = desired.ToRotation();
                float newAngle = MathHelper.Lerp(currentAngle, targetAngle, HomingStrength);
                // 限制单帧最大偏转角度，确保弧度自然
                float angleDiff = MathHelper.WrapAngle(targetAngle - currentAngle);
                float maxTurn = 0.04f;
                float clampedDiff = MathHelper.Clamp(angleDiff, -maxTurn, maxTurn);
                newAngle = currentAngle + clampedDiff;
                Projectile.velocity = newAngle.ToRotationVector2() * Projectile.velocity.Length();
            }

            // 维持速度
            if (Projectile.velocity.Length() < Speed) {
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * Speed;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            // 淡入/淡出
            float life = 1f - (float)Projectile.timeLeft / MaxLife;
            if (life < 0.08f) {
                fadeAlpha = life / 0.08f;
            }
            else if (Projectile.timeLeft < 20) {
                fadeAlpha = Projectile.timeLeft / 20f;
            }
            else {
                fadeAlpha = 1f;
            }

            // 飞行发光
            Lighting.AddLight(Projectile.Center, theme.Core.ToVector3() * 0.6f * fadeAlpha);

            // 方形科幻粒子
            particleTimer++;
            if (particleTimer >= ParticleInterval && Main.netMode != NetmodeID.Server) {
                particleTimer = 0;
                SpawnCyberParticles();
            }
        }

        private void SpawnCyberParticles() {
            Vector2 perpDir = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            float spread = 8f;

            for (int i = 0; i < 2; i++) {
                Vector2 offset = perpDir * Main.rand.NextFloat(-spread, spread);
                Vector2 spawnPos = Projectile.Center + offset;
                Vector2 particleVel = -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1f, 3f)
                    + perpDir * Main.rand.NextFloat(-1.5f, 1.5f);

                float scale = Main.rand.NextFloat(0.6f, 1.2f);
                int lifeTime = Main.rand.Next(15, 35);

                PRTLoader.AddParticle(new PRT_CyberSquare(
                    spawnPos, particleVel,
                    theme.ParticleMain, theme.ParticleEdge,
                    scale, lifeTime
                ));
            }
        }

        #region Trail绘制

        private float WidthFunction(float progress) {
            // progress: 0=当前头部, 1=最远尾部
            // 头部圆弧收窄 + 中段饱满 + 尾端渐细
            float headTaper = MathF.Pow(MathF.Sin(MathHelper.Clamp(progress * 8f, 0f, MathHelper.Pi)), 0.5f);
            float bodyFade = 1f - MathF.Pow(progress, 2.5f);
            float width = headTaper * bodyFade;
            return MathHelper.Clamp(width, 0.02f, 1f) * 28f;
        }

        private Color ColorFunction(Vector2 _) => Color.White;

        void IPrimitiveDrawable.DrawPrimitives() {
            if (Projectile.oldPos == null || fadeAlpha < 0.01f)
                return;

            Effect shader = EffectLoader.CyberTraceBeam?.Value;
            if (shader == null) return;
            Texture2D noise = CWRAsset.Extra_193?.Value;
            if (noise == null) return;

            // 构建拖尾位置数组
            trailPositions ??= new Vector2[TrailCacheLen];
            int validCount = 0;
            for (int i = 0; i < TrailCacheLen; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    trailPositions[i] = Projectile.Center;
                }
                else {
                    trailPositions[i] = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                    validCount++;
                }
            }

            if (validCount < 3) return;

            trail ??= new Trail(trailPositions, WidthFunction, ColorFunction);
            trail.TrailPositions = trailPositions;

            // 确保主题已初始化
            if (Projectile.localAI[0] == 0f) return;
            theme = Themes[themeIndex];

            shader.Parameters["transformMatrix"]?.SetValue(VaultUtils.GetTransfromMatrix());
            shader.Parameters["uTime"]?.SetValue(Cyberspace.Active ? Cyberspace.EffectTime : (float)Main.timeForVisualEffects * 0.04f);
            shader.Parameters["fadeAlpha"]?.SetValue(MathHelper.Clamp(fadeAlpha, 0f, 1f));
            shader.Parameters["headProgress"]?.SetValue(0f); // 头部在along=0的位置（oldPos[0]是当前位置）
            shader.Parameters["coreColor"]?.SetValue(theme.CoreVec);
            shader.Parameters["glowColor"]?.SetValue(theme.GlowVec);
            shader.Parameters["auraColor"]?.SetValue(theme.AuraVec);
            shader.Parameters["trailLength"]?.SetValue(MathHelper.Clamp(validCount / (float)TrailCacheLen, 0.1f, 1f));
            shader.Parameters["uNoiseTex"]?.SetValue(noise);

            GraphicsDevice device = Main.graphics.GraphicsDevice;
            device.BlendState = BlendState.Additive;
            trail.DrawTrail(shader);
            device.BlendState = BlendState.AlphaBlend;
        }

        #endregion

        #region 光球头部绘制

        void IAdditiveDrawable.DrawAdditiveAfterNon(SpriteBatch spriteBatch) {
            if (fadeAlpha < 0.01f) return;

            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow == null) return;

            // 确保主题已初始化
            if (Projectile.localAI[0] == 0f) return;
            theme = Themes[themeIndex];

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float pulse = 0.85f + 0.15f * MathF.Sin((float)Main.timeForVisualEffects * 0.15f);
            float alpha = fadeAlpha * pulse;

            // 外层柔和光晕（大范围、低不透明度）
            float outerScale = 1.8f * Projectile.scale;
            Color outerColor = theme.Aura * alpha * 0.3f;
            outerColor.A = 0;
            spriteBatch.Draw(glow, drawPos, null, outerColor, 0f,
                glow.Size() * 0.5f, outerScale, SpriteEffects.None, 0f);

            // 中层辉光
            float midScale = 1.0f * Projectile.scale;
            Color midColor = theme.Core * alpha * 0.6f;
            midColor.A = 0;
            spriteBatch.Draw(glow, drawPos, null, midColor, 0f,
                glow.Size() * 0.5f, midScale, SpriteEffects.None, 0f);

            // 核心白热点
            float coreScale = 0.4f * Projectile.scale;
            Color coreColor = Color.White * alpha * 0.8f;
            coreColor.A = 0;
            spriteBatch.Draw(glow, drawPos, null, coreColor, 0f,
                glow.Size() * 0.5f, coreScale, SpriteEffects.None, 0f);
        }

        #endregion

        public override bool PreDraw(ref Color lightColor) => false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            // 命中时爆发粒子
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 8; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(4f, 4f);
                float scale = Main.rand.NextFloat(0.8f, 1.6f);
                PRTLoader.AddParticle(new PRT_CyberSquare(
                    target.Center + vel * 2f, vel,
                    theme.ParticleMain, theme.ParticleEdge,
                    scale, Main.rand.Next(20, 40)
                ));
            }
        }

        public override void OnKill(int timeLeft) {
            // 消散时的最终粒子爆发
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < 12; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(3f, 3f) + Projectile.velocity * 0.2f;
                float scale = Main.rand.NextFloat(0.5f, 1.3f);
                PRTLoader.AddParticle(new PRT_CyberSquare(
                    Projectile.Center, vel,
                    theme.ParticleMain, theme.ParticleEdge,
                    scale, Main.rand.Next(25, 50)
                ));
            }
        }
    }
}
