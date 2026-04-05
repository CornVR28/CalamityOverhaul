using CalamityOverhaul.Common;
using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using InnoVault.Trails;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
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

        #region 超驱配色（高温红炽+白热）

        private static readonly ColorTheme OverdriveTheme = new() {
            Core = new Color(255, 255, 220),
            Glow = new Color(255, 40, 15),
            Aura = new Color(200, 10, 0),
            ParticleMain = new Color(255, 200, 50),
            ParticleEdge = new Color(255, 30, 5),
        };

        #endregion

        #region 实例字段

        private Trail trail;
        private Vector2[] trailPositions;
        private int themeIndex;
        private ColorTheme theme;
        private float fadeAlpha;
        private int particleTimer;

        /// <summary>超驱混合量 0-1，在领域内平滑过渡到1</summary>
        private float overdriveAmount;
        /// <summary>故障爆发计时器（帧），到0时触发爆发</summary>
        private int glitchBurstTimer;
        /// <summary>当前故障爆发强度 0-1，触发后指数衰减</summary>
        private float glitchBurstIntensity;

        /// <summary>当前帧有效拖尾顶点数，供 WidthFunction 使用</summary>
        private int currentValidCount;

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
            NPC target = Projectile.Center.FindClosestNPC(120f);
            if (target != null && Projectile.numHits == 0) {
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

            // ---- 超驱检测与过渡
            bool insideDomain = Cyberspace.IsInsideDomain(Projectile.Center);
            float targetOD = insideDomain ? 1f : 0f;
            float prevOD = overdriveAmount;
            overdriveAmount = MathHelper.Lerp(overdriveAmount, targetOD, 0.055f); // ~0.4s过渡
            if (overdriveAmount < 0.005f) overdriveAmount = 0f;

            // 首次进入超驱阈值时，给 burstTimer 一个随机初始值，避免立即触发
            if (prevOD <= 0.3f && overdriveAmount > 0.3f) {
                glitchBurstTimer = Main.rand.Next(10, 25);
            }

            // 间歇性故障爆发（高频黑墙撕裂）
            if (overdriveAmount > 0.3f) {
                glitchBurstTimer--;
                if (glitchBurstTimer <= 0) {
                    glitchBurstIntensity = 1f;
                    glitchBurstTimer = Main.rand.Next(20, 40);
                }
            }
            glitchBurstIntensity *= 0.85f;
            if (glitchBurstIntensity < 0.01f) glitchBurstIntensity = 0f;

            // 飞行发光（超驱时高温红炽光）
            Color lightCol = overdriveAmount > 0.1f
                ? Color.Lerp(theme.Core, OverdriveTheme.Core, overdriveAmount)
                : theme.Core;
            Lighting.AddLight(Projectile.Center, lightCol.ToVector3() * (0.6f + overdriveAmount * 0.8f) * fadeAlpha);

            // 方形科幻粒子（超驱时极密集）
            int interval = overdriveAmount > 0.3f ? 1 : ParticleInterval;
            particleTimer++;
            if (particleTimer >= interval && Main.netMode != NetmodeID.Server) {
                particleTimer = 0;
                SpawnCyberParticles();
            }
        }

        private void SpawnCyberParticles() {
            Vector2 perpDir = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            float od = overdriveAmount;
            float spread = 8f + od * 16f;
            int count = od > 0.3f ? 6 : 2;

            // 超驱时混合配色（高温红白）
            Color mainCol = Color.Lerp(theme.ParticleMain, OverdriveTheme.ParticleMain, od);
            Color edgeCol = Color.Lerp(theme.ParticleEdge, OverdriveTheme.ParticleEdge, od);

            for (int i = 0; i < count; i++) {
                Vector2 offset = perpDir * Main.rand.NextFloat(-spread, spread);
                Vector2 spawnPos = Projectile.Center + offset;
                Vector2 particleVel = -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1f, 4f + od * 4f)
                    + perpDir * Main.rand.NextFloat(-2f - od * 2f, 2f + od * 2f);

                float scale = Main.rand.NextFloat(0.6f, 1.4f + od * 1.2f);
                int lifeTime = Main.rand.Next(15, 35 + (int)(od * 20f));

                PRTLoader.AddParticle(new PRT_CyberSquare(
                    spawnPos, particleVel,
                    mainCol, edgeCol,
                    scale, lifeTime
                ));
            }

            // 超驱时大量横向散射粒子（高温红炽强调）
            if (od > 0.3f && glitchBurstIntensity > 0.1f) {
                int burstCount = 4 + (int)(glitchBurstIntensity * 6f);
                for (int i = 0; i < burstCount; i++) {
                    Vector2 burstVel = perpDir * Main.rand.NextFloat(-8f, 8f)
                        + Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-2f, 2f);
                    PRTLoader.AddParticle(new PRT_CyberSquare(
                        Projectile.Center, burstVel,
                        OverdriveTheme.ParticleEdge, OverdriveTheme.ParticleMain,
                        Main.rand.NextFloat(1.0f, 2.4f), Main.rand.Next(8, 18)
                    ));
                }
            }
        }

        #region Trail绘制

        private float WidthFunction(float progress) {
            // 将 tailTaper 的衰减范围压缩至有效顶点区间，
            // 使拖尾在实际末端处自然收为 0，避免刚发射时的断尾切口
            float validRatio = MathF.Max((float)currentValidCount / TrailCacheLen, 0.05f);
            float tailProgress = MathHelper.Clamp(progress / validRatio, 0f, 1f);

            float noseRise = MathF.Min(tailProgress / 0.06f, 1f);
            noseRise = MathF.Sin(noseRise * MathHelper.PiOver2);
            float tailTaper = 1f - MathF.Pow(tailProgress, 2.0f);
            float width = noseRise * tailTaper;
            // 超驱时光束更粗壮（30→50像素宽）
            return MathF.Max(width, 0f) * (30f + overdriveAmount * 20f);
        }

        private Color ColorFunction(Vector2 _) => Color.White;

        void IPrimitiveDrawable.DrawPrimitives() {
            if (Projectile.oldPos == null || fadeAlpha < 0.01f)
                return;

            Effect shader = EffectLoader.CyberTraceBeam?.Value;
            if (shader == null) return;
            Texture2D noise = CWRAsset.Extra_193?.Value;
            if (noise == null) return;

            // 构建拖尾位置数组，无效位置用最后有效位置填充（尾部收束）
            trailPositions ??= new Vector2[TrailCacheLen];
            int validCount = 0;
            Vector2 lastValidPos = Projectile.Center;
            for (int i = 0; i < TrailCacheLen; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    trailPositions[i] = lastValidPos;
                }
                else {
                    trailPositions[i] = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                    lastValidPos = trailPositions[i];
                    validCount++;
                }
            }
            currentValidCount = validCount;

            if (validCount < 3) return;

            trail ??= new Trail(trailPositions, WidthFunction, ColorFunction);
            trail.TrailPositions = trailPositions;

            // 确保主题已初始化
            if (Projectile.localAI[0] == 0f) return;
            theme = Themes[themeIndex];

            shader.Parameters["transformMatrix"]?.SetValue(VaultUtils.GetTransfromMatrix());
            shader.Parameters["uTime"]?.SetValue(Cyberspace.Active ? Cyberspace.EffectTime : (float)Main.timeForVisualEffects * 0.04f);
            shader.Parameters["fadeAlpha"]?.SetValue(MathHelper.Clamp(fadeAlpha, 0f, 1f));
            shader.Parameters["coreColor"]?.SetValue(theme.CoreVec);
            shader.Parameters["glowColor"]?.SetValue(theme.GlowVec);
            shader.Parameters["auraColor"]?.SetValue(theme.AuraVec);
            shader.Parameters["uNoiseTex"]?.SetValue(noise);
            // 超驱参数
            shader.Parameters["overdriveAmount"]?.SetValue(overdriveAmount);
            shader.Parameters["glitchBurst"]?.SetValue(glitchBurstIntensity);
            shader.Parameters["odCoreColor"]?.SetValue(OverdriveTheme.CoreVec);
            shader.Parameters["odGlowColor"]?.SetValue(OverdriveTheme.GlowVec);
            shader.Parameters["odAuraColor"]?.SetValue(OverdriveTheme.AuraVec);

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

            // 超驱混合色
            float od = overdriveAmount;
            Color drawAura = Color.Lerp(theme.Aura, OverdriveTheme.Aura, od);

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float pulse = 0.9f + 0.1f * MathF.Sin((float)Main.timeForVisualEffects * 0.15f);
            // 超驱时脉冲剧烈震荡
            pulse += od * 0.3f * MathF.Sin((float)Main.timeForVisualEffects * 0.5f);
            pulse += od * glitchBurstIntensity * 0.2f * MathF.Sin((float)Main.timeForVisualEffects * 1.2f);
            float alpha = fadeAlpha * pulse;
            Vector2 glowOrigin = glow.Size() * 0.5f;

            // 外层柔和bloom光晕（超驱时巨大炽热光晕）
            float outerScale = (2.0f + od * 2.5f) * Projectile.scale;
            Color outerColor = drawAura * alpha * (0.25f + od * 0.5f);
            spriteBatch.Draw(glow, drawPos, null, outerColor, 0f,
                glowOrigin, outerScale, SpriteEffects.None, 0f);

            // 结束当前批次，切换到Immediate模式应用能量球着色器
            spriteBatch.End();

            Effect orbShader = EffectLoader.CyberEnergyOrb?.Value;
            Texture2D noise = CWRAsset.Extra_193?.Value;
            if (orbShader != null && noise != null) {
                float timeVal = Cyberspace.Active
                    ? Cyberspace.EffectTime
                    : (float)Main.timeForVisualEffects * 0.04f;

                orbShader.Parameters["uTime"]?.SetValue(timeVal);
                orbShader.Parameters["fadeAlpha"]?.SetValue(alpha);
                // 传入超驱预混合色（与CyberChargeOrbProj保持一致）
                Color orbCore = Color.Lerp(theme.Core, OverdriveTheme.Core, od);
                Color orbGlow = Color.Lerp(theme.Glow, OverdriveTheme.Glow, od);
                Color orbAura = Color.Lerp(theme.Aura, OverdriveTheme.Aura, od);
                orbShader.Parameters["coreColor"]?.SetValue(orbCore.ToVector3());
                orbShader.Parameters["glowColor"]?.SetValue(orbGlow.ToVector3());
                orbShader.Parameters["auraColor"]?.SetValue(orbAura.ToVector3());
                orbShader.Parameters["orbScale"]?.SetValue(pulse);
                orbShader.Parameters["uNoiseTex"]?.SetValue(noise);
                // 超驱参数
                orbShader.Parameters["overdriveAmount"]?.SetValue(od);
                orbShader.Parameters["glitchBurst"]?.SetValue(glitchBurstIntensity);
                orbShader.Parameters["odCoreColor"]?.SetValue(OverdriveTheme.CoreVec);
                orbShader.Parameters["odGlowColor"]?.SetValue(OverdriveTheme.GlowVec);
                orbShader.Parameters["odAuraColor"]?.SetValue(OverdriveTheme.AuraVec);

                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                orbShader.CurrentTechnique.Passes[0].Apply();

                float orbDrawScale = (1.1f + od * 0.8f) * Projectile.scale;
                spriteBatch.Draw(glow, drawPos, null, Color.White, 0f,
                    glowOrigin, orbDrawScale, SpriteEffects.None, 0f);

                spriteBatch.End();
            }

            // 恢复调用者期望的批次状态（Additive + Deferred）
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        #endregion

        public override bool PreDraw(ref Color lightColor) => false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Main.netMode == NetmodeID.Server) return;
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.5f, Pitch = 0.3f }, target.Center);
            float od = overdriveAmount;
            int count = od > 0.3f ? 22 : 8;
            Color mainCol = Color.Lerp(theme.ParticleMain, OverdriveTheme.ParticleMain, od);
            Color edgeCol = Color.Lerp(theme.ParticleEdge, OverdriveTheme.ParticleEdge, od);
            for (int i = 0; i < count; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(5f + od * 6f, 5f + od * 6f);
                float scale = Main.rand.NextFloat(0.8f, 2.0f + od * 1.2f);
                PRTLoader.AddParticle(new PRT_CyberSquare(
                    target.Center + vel * 2f, vel,
                    mainCol, edgeCol,
                    scale, Main.rand.Next(20, 40)
                ));
            }
        }

        public override void OnKill(int timeLeft) {
            if (Main.netMode == NetmodeID.Server) return;
            float od = overdriveAmount;
            int count = od > 0.3f ? 30 : 12;
            Color mainCol = Color.Lerp(theme.ParticleMain, OverdriveTheme.ParticleMain, od);
            Color edgeCol = Color.Lerp(theme.ParticleEdge, OverdriveTheme.ParticleEdge, od);
            for (int i = 0; i < count; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(4f + od * 6f, 4f + od * 6f) + Projectile.velocity * 0.3f;
                float scale = Main.rand.NextFloat(0.5f, 1.5f + od * 1.2f);
                PRTLoader.AddParticle(new PRT_CyberSquare(
                    Projectile.Center, vel,
                    mainCol, edgeCol,
                    scale, Main.rand.Next(25, 50)
                ));
            }
        }
    }
}
