using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.GalacticCrisises
{
    /// <summary>
    /// 亚空间传送门弹幕
    /// 大静默后星际跃迁技术失效，只能走亚空间通道
    /// 表现为一道被撕裂的虚空裂缝，周围充满灵能残响和扭曲的时空碎片
    /// 阶段：裂隙撕开 → 传送门膨胀稳定 → 灵能涌动待命 → 淡出关闭
    /// </summary>
    internal class SubspacePortal : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        #region 阶段与计时

        private enum PortalPhase
        {
            /// <summary>裂隙撕开，从一条细线膨胀为椭圆裂口</summary>
            Tearing,
            /// <summary>传送门稳定，全力运转，大量粒子涌出</summary>
            Stabilized,
            /// <summary>淡出关闭</summary>
            Closing
        }

        private PortalPhase Phase {
            get => (PortalPhase)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }
        private ref float PhaseTimer => ref Projectile.ai[1];
        private ref float GlobalTimer => ref Projectile.localAI[0];

        private const int TearDuration = 80;
        private const int StabilizedDuration = 600;
        private const int CloseDuration = 60;

        #endregion

        #region 视觉参数

        /// <summary>传送门当前开口进度 0~1</summary>
        private float openProgress;
        /// <summary>传送门半径（完全展开时）</summary>
        private const float MaxPortalRadius = 120f;
        /// <summary>传送门椭圆的宽高比（高大于宽，竖椭圆裂缝）</summary>
        private const float EllipseRatioX = 0.55f;
        private const float EllipseRatioY = 1.0f;

        //内部粒子列表（不通过全局PRT系统，直接在弹幕内管理绘制的局部装饰粒子）
        private readonly List<VoidTendril> tendrils = [];
        private readonly List<WarpRing> warpRings = [];

        //声音
        private bool playedOpenSound;
        private bool playedStableSound;

        #endregion

        public override void SetDefaults() {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TearDuration + StabilizedDuration + CloseDuration + 60;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            GlobalTimer++;
            PhaseTimer++;

            switch (Phase) {
                case PortalPhase.Tearing:
                    UpdateTearing();
                    break;
                case PortalPhase.Stabilized:
                    UpdateStabilized();
                    break;
                case PortalPhase.Closing:
                    UpdateClosing();
                    break;
            }

            //持续光照
            float lightIntensity = openProgress * 0.8f;
            Lighting.AddLight(Projectile.Center, 0.3f * lightIntensity, 0.1f * lightIntensity, 0.7f * lightIntensity);

            //更新局部效果
            UpdateTendrils();
            UpdateWarpRings();
        }

        #region 阶段逻辑

        private void UpdateTearing() {
            float t = PhaseTimer / TearDuration;
            openProgress = CWRUtils.EaseOutCubic(Math.Min(t, 1f));

            //音效
            if (!playedOpenSound && PhaseTimer == 1) {
                playedOpenSound = true;
                SoundEngine.PlaySound(SoundID.DD2_EtherianPortalOpen with {
                    Volume = 1.2f,
                    Pitch = -0.6f,
                    MaxInstances = 1
                }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal with {
                    Volume = 0.6f,
                    Pitch = -0.8f,
                    MaxInstances = 1
                }, Projectile.Center);
            }

            //撕裂阶段的粒子：从中心线向外扩散的灵能碎片
            if (!VaultUtils.isServer) {
                SpawnTearParticles(t);
            }

            if (PhaseTimer >= TearDuration) {
                Phase = PortalPhase.Stabilized;
                PhaseTimer = 0;
            }
        }

        private void UpdateStabilized() {
            openProgress = 1f;

            if (!playedStableSound) {
                playedStableSound = true;
                SoundEngine.PlaySound(SoundID.DD2_EtherianPortalSpawnEnemy with {
                    Volume = 0.8f,
                    Pitch = -0.4f,
                    MaxInstances = 1
                }, Projectile.Center);
            }

            if (!VaultUtils.isServer) {
                SpawnStabilizedParticles();
                SpawnVoidAbsorptionParticles();
                SpawnEdgeSparkles();

                //周期性生成虚空触须
                if (PhaseTimer % 40 == 0) {
                    SpawnTendril();
                }

                //周期性生成扭曲环
                if (PhaseTimer % 25 == 0) {
                    SpawnWarpRing();
                }
            }

            if (PhaseTimer >= StabilizedDuration) {
                Phase = PortalPhase.Closing;
                PhaseTimer = 0;
            }
        }

        private void UpdateClosing() {
            float t = PhaseTimer / (float)CloseDuration;
            openProgress = 1f - CWRUtils.EaseInQuad(Math.Min(t, 1f));

            if (!VaultUtils.isServer && PhaseTimer % 3 == 0) {
                SpawnClosingParticles();
            }

            if (PhaseTimer >= CloseDuration) {
                Projectile.Kill();
            }
        }

        #endregion

        #region 粒子生成

        /// <summary>撕裂阶段：从中心线炸开的灵能裂缝粒子</summary>
        private void SpawnTearParticles(float tearProgress) {
            int count = (int)(3 + tearProgress * 8);
            float currentRadius = MaxPortalRadius * openProgress;

            for (int i = 0; i < count; i++) {
                //沿椭圆边缘生成
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float edgeX = MathF.Cos(angle) * currentRadius * EllipseRatioX;
                float edgeY = MathF.Sin(angle) * currentRadius * EllipseRatioY;
                Vector2 spawnPos = Projectile.Center + new Vector2(edgeX, edgeY);

                //向外扩散的速度
                Vector2 outward = (spawnPos - Projectile.Center).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2f, 6f);

                //灵能紫色火花
                PRTLoader.AddParticle(new PRT_Spark(
                    spawnPos,
                    outward,
                    false,
                    Main.rand.Next(15, 30),
                    Main.rand.NextFloat(0.8f, 1.5f),
                    Color.Lerp(new Color(120, 40, 200), new Color(80, 20, 160), Main.rand.NextFloat())
                ));
            }

            //中心裂缝闪光
            if (Main.rand.NextBool(3)) {
                PRTLoader.AddParticle(new PRT_Light(
                    Projectile.Center + Main.rand.NextVector2Circular(10f * tearProgress, 30f * tearProgress),
                    Vector2.Zero,
                    Main.rand.NextFloat(0.8f, 1.6f),
                    new Color(180, 100, 255),
                    Main.rand.Next(10, 20),
                    1f, 1.5f
                ));
            }
        }

        /// <summary>稳定阶段：传送门边缘持续涌出的亚空间能量</summary>
        private void SpawnStabilizedParticles() {
            //边缘涌出的灵能烟雾
            if (GlobalTimer % 2 == 0) {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = MaxPortalRadius * Main.rand.NextFloat(0.7f, 1.1f);
                float ex = MathF.Cos(angle) * radius * EllipseRatioX;
                float ey = MathF.Sin(angle) * radius * EllipseRatioY;
                Vector2 edgePos = Projectile.Center + new Vector2(ex, ey);

                Vector2 tangent = new Vector2(-MathF.Sin(angle), MathF.Cos(angle));
                Vector2 vel = tangent * Main.rand.NextFloat(1f, 3f) +
                              (Projectile.Center - edgePos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(-1f, 0.5f);

                PRTLoader.AddParticle(new PRT_Smoke(
                    edgePos,
                    vel,
                    Color.Lerp(new Color(80, 30, 150), new Color(40, 10, 80), Main.rand.NextFloat()),
                    Main.rand.Next(40, 80),
                    Main.rand.NextFloat(0.4f, 0.9f),
                    Main.rand.NextFloat(0.3f, 0.6f),
                    Main.rand.NextFloat(-0.02f, 0.02f),
                    false,
                    Main.rand.NextFloat(-0.01f, 0.01f)
                ));
            }

            //内部深处的虚空闪烁
            if (GlobalTimer % 4 == 0) {
                float innerAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                float innerR = MaxPortalRadius * Main.rand.NextFloat(0.1f, 0.5f);
                Vector2 innerPos = Projectile.Center + new Vector2(
                    MathF.Cos(innerAngle) * innerR * EllipseRatioX,
                    MathF.Sin(innerAngle) * innerR * EllipseRatioY
                );

                PRTLoader.AddParticle(new PRT_Light(
                    innerPos,
                    Main.rand.NextVector2Circular(0.5f, 0.5f),
                    Main.rand.NextFloat(0.3f, 0.7f),
                    Color.Lerp(new Color(60, 0, 120), new Color(100, 50, 180), Main.rand.NextFloat()),
                    Main.rand.Next(15, 30),
                    0.8f, 1.2f
                ));
            }
        }

        /// <summary>稳定阶段：从远处被吸入传送门的虚空碎片</summary>
        private void SpawnVoidAbsorptionParticles() {
            if (GlobalTimer % 3 != 0) return;

            for (int i = 0; i < 2; i++) {
                //从远处随机位置生成，被吸向传送门中心
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(200f, 350f);
                Vector2 spawnPos = Projectile.Center + angle.ToRotationVector2() * dist;
                Vector2 vel = (Projectile.Center - spawnPos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(3f, 7f);

                PRTLoader.AddParticle(new PRT_Spark(
                    spawnPos,
                    vel,
                    false,
                    Main.rand.Next(30, 60),
                    Main.rand.NextFloat(0.5f, 1.2f),
                    Color.Lerp(new Color(150, 80, 255), new Color(60, 20, 180), Main.rand.NextFloat())
                ));
            }

            //偶尔吸入较大的闪光碎片
            if (Main.rand.NextBool(4)) {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(250f, 400f);
                Vector2 spawnPos = Projectile.Center + angle.ToRotationVector2() * dist;
                Vector2 vel = (Projectile.Center - spawnPos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 4f);

                PRTLoader.AddParticle(new PRT_Sparkle(
                    spawnPos,
                    vel,
                    new Color(200, 150, 255),
                    new Color(120, 60, 200),
                    Main.rand.NextFloat(0.4f, 0.8f),
                    Main.rand.Next(40, 70),
                    Main.rand.NextFloat(0.01f, 0.03f),
                    1.2f
                ));
            }
        }

        /// <summary>稳定阶段：传送门边缘的星光闪烁</summary>
        private void SpawnEdgeSparkles() {
            if (GlobalTimer % 6 != 0) return;

            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            float r = MaxPortalRadius * Main.rand.NextFloat(0.85f, 1.15f);
            Vector2 pos = Projectile.Center + new Vector2(
                MathF.Cos(angle) * r * EllipseRatioX,
                MathF.Sin(angle) * r * EllipseRatioY
            );

            PRTLoader.AddParticle(new PRT_Sparkle(
                pos,
                Main.rand.NextVector2Circular(0.5f, 0.5f),
                Color.White,
                new Color(180, 120, 255),
                Main.rand.NextFloat(0.3f, 0.6f),
                Main.rand.Next(15, 30),
                Main.rand.NextFloat(0.02f, 0.05f),
                0.8f
            ));
        }

        /// <summary>关闭阶段：传送门坍缩时的能量爆散</summary>
        private void SpawnClosingParticles() {
            float currentRadius = MaxPortalRadius * openProgress;

            for (int i = 0; i < 4; i++) {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float ex = MathF.Cos(angle) * currentRadius * EllipseRatioX;
                float ey = MathF.Sin(angle) * currentRadius * EllipseRatioY;
                Vector2 pos = Projectile.Center + new Vector2(ex, ey);

                //坍缩时向中心收缩
                Vector2 vel = (Projectile.Center - pos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(3f, 8f);

                PRTLoader.AddParticle(new PRT_Spark(
                    pos,
                    vel,
                    false,
                    Main.rand.Next(10, 20),
                    Main.rand.NextFloat(0.6f, 1.3f),
                    Color.Lerp(new Color(200, 100, 255), new Color(255, 200, 255), Main.rand.NextFloat())
                ));
            }
        }

        #endregion

        #region 触须与扭曲环

        private void SpawnTendril() {
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            tendrils.Add(new VoidTendril {
                BaseAngle = angle,
                Length = 0f,
                MaxLength = Main.rand.NextFloat(60f, 140f),
                Width = Main.rand.NextFloat(3f, 8f),
                Life = 0,
                MaxLife = Main.rand.Next(50, 90),
                WavePhase = Main.rand.NextFloat(MathHelper.TwoPi),
                WaveSpeed = Main.rand.NextFloat(0.08f, 0.15f),
                WaveAmplitude = Main.rand.NextFloat(8f, 20f)
            });
        }

        private void UpdateTendrils() {
            for (int i = tendrils.Count - 1; i >= 0; i--) {
                var t = tendrils[i];
                t.Life++;
                float lifeRatio = t.Life / (float)t.MaxLife;

                if (lifeRatio < 0.3f) {
                    t.Length = MathHelper.Lerp(0, t.MaxLength, lifeRatio / 0.3f);
                }
                else if (lifeRatio > 0.7f) {
                    t.Length = MathHelper.Lerp(t.MaxLength, 0, (lifeRatio - 0.7f) / 0.3f);
                }

                t.WavePhase += t.WaveSpeed;
                tendrils[i] = t;

                if (t.Life >= t.MaxLife) {
                    tendrils.RemoveAt(i);
                }
            }
        }

        private void SpawnWarpRing() {
            warpRings.Add(new WarpRing {
                Radius = MaxPortalRadius * 0.3f,
                MaxRadius = MaxPortalRadius * Main.rand.NextFloat(1.2f, 1.8f),
                Life = 0,
                MaxLife = Main.rand.Next(30, 50),
                Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                RotSpeed = Main.rand.NextFloat(-0.03f, 0.03f)
            });
        }

        private void UpdateWarpRings() {
            for (int i = warpRings.Count - 1; i >= 0; i--) {
                var r = warpRings[i];
                r.Life++;
                float t = r.Life / (float)r.MaxLife;
                r.Radius = MathHelper.Lerp(MaxPortalRadius * 0.3f, r.MaxRadius, CWRUtils.EaseOutCubic(t));
                r.Rotation += r.RotSpeed;
                warpRings[i] = r;

                if (r.Life >= r.MaxLife) {
                    warpRings.RemoveAt(i);
                }
            }
        }

        #endregion

        #region 绘制

        public override bool PreDraw(ref Color lightColor) {
            if (openProgress <= 0.01f) return false;

            SpriteBatch sb = Main.spriteBatch;
            Texture2D pixel = VaultAsset.placeholder2.Value;
            Texture2D softGlow = CWRAsset.SoftGlow.Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            float currentRadius = MaxPortalRadius * openProgress;

            //===第1层：远景虚空光晕===
            DrawVoidHalo(sb, softGlow, center, currentRadius);

            //===第2层：扭曲环===
            DrawWarpRings(sb, pixel, center);

            //===第3层：传送门椭圆主体（深渊黑洞）===
            DrawPortalBody(sb, pixel, softGlow, center, currentRadius);

            //===第4层：边缘灵能光圈===
            DrawEdgeGlow(sb, pixel, center, currentRadius);

            //===第5层：虚空触须===
            DrawTendrils(sb, pixel, center, currentRadius);

            //===第6层：中心亮点===
            DrawCenterFlare(sb, softGlow, center);

            return false;
        }

        /// <summary>远景虚空光晕：巨大的暗紫色光球，营造空间扭曲感</summary>
        private void DrawVoidHalo(SpriteBatch sb, Texture2D softGlow, Vector2 center, float radius) {
            float pulse = MathF.Sin(GlobalTimer * 0.03f) * 0.15f + 0.85f;
            float haloScale = (radius * 3f / softGlow.Width) * pulse;

            Color haloColor = new Color(40, 10, 80, 0) * (openProgress * 0.25f);
            sb.Draw(softGlow, center, null, haloColor, 0f, softGlow.Size() / 2f, haloScale, SpriteEffects.None, 0f);

            //第二层更暗的光晕
            Color haloColor2 = new Color(20, 5, 50, 0) * (openProgress * 0.15f);
            sb.Draw(softGlow, center, null, haloColor2, 0f, softGlow.Size() / 2f, haloScale * 1.5f, SpriteEffects.None, 0f);
        }

        /// <summary>传送门主体：用大量同心椭圆像素绘制深邃的虚空</summary>
        private void DrawPortalBody(SpriteBatch sb, Texture2D pixel, Texture2D softGlow, Vector2 center, float radius) {
            //多层同心椭圆，从外到内颜色由暗紫过渡到纯黑
            int layers = 40;
            for (int i = layers; i >= 0; i--) {
                float t = i / (float)layers;
                float layerRadius = radius * t;
                float rx = layerRadius * EllipseRatioX;
                float ry = layerRadius * EllipseRatioY;

                if (rx < 1f || ry < 1f) continue;

                //颜色：外层暗紫，内层漆黑
                Color layerColor;
                if (t > 0.7f) {
                    //外缘：暗紫色，半透明
                    float edgeFade = (t - 0.7f) / 0.3f;
                    layerColor = Color.Lerp(new Color(50, 15, 100), new Color(30, 5, 60), edgeFade) * (openProgress * (1f - edgeFade * 0.5f));
                }
                else if (t > 0.3f) {
                    //中层：深紫到黑
                    float midT = (t - 0.3f) / 0.4f;
                    layerColor = Color.Lerp(new Color(10, 2, 20), new Color(50, 15, 100), midT) * openProgress;
                }
                else {
                    //核心：纯黑深渊
                    layerColor = new Color(3, 0, 8) * openProgress;
                }

                //用填充椭圆的方式绘制
                DrawFilledEllipse(sb, pixel, center, rx, ry, layerColor);
            }

            //核心处的微弱灵能涌动
            float corePulse = MathF.Sin(GlobalTimer * 0.06f) * 0.3f + 0.7f;
            float coreScale = (radius * 0.25f / softGlow.Width) * corePulse;
            Color coreColor = new Color(100, 40, 200, 0) * (openProgress * 0.3f * corePulse);
            sb.Draw(softGlow, center, null, coreColor, GlobalTimer * 0.01f, softGlow.Size() / 2f, coreScale, SpriteEffects.None, 0f);
        }

        /// <summary>边缘灵能光圈：沿椭圆边缘绘制的高亮灵能弧线</summary>
        private void DrawEdgeGlow(SpriteBatch sb, Texture2D pixel, Vector2 center, float radius) {
            int segments = 120;
            float time = GlobalTimer * 0.04f;

            for (int i = 0; i < segments; i++) {
                float angle = MathHelper.TwoPi * i / segments;
                float rx = radius * EllipseRatioX;
                float ry = radius * EllipseRatioY;

                Vector2 pos = center + new Vector2(MathF.Cos(angle) * rx, MathF.Sin(angle) * ry);

                //沿边缘的亮度波动
                float wave = MathF.Sin(angle * 3f + time) * 0.5f + 0.5f;
                float secondWave = MathF.Sin(angle * 7f - time * 1.3f) * 0.3f + 0.7f;
                float brightness = wave * secondWave;

                Color edgeColor = Color.Lerp(
                    new Color(140, 60, 255, 0),
                    new Color(200, 150, 255, 0),
                    brightness
                ) * (openProgress * brightness * 0.7f);

                float size = 2f + brightness * 3f;
                sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), edgeColor, 0f, new Vector2(0.5f), size, SpriteEffects.None, 0f);

                //外层辉光
                if (brightness > 0.6f) {
                    Color outerGlow = new Color(100, 40, 200, 0) * (openProgress * (brightness - 0.6f) * 0.5f);
                    sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), outerGlow, 0f, new Vector2(0.5f), size * 3f, SpriteEffects.None, 0f);
                }
            }
        }

        /// <summary>虚空触须：从传送门边缘伸出的不规则能量丝</summary>
        private void DrawTendrils(SpriteBatch sb, Texture2D pixel, Vector2 center, float radius) {
            foreach (var tendril in tendrils) {
                float lifeRatio = tendril.Life / (float)tendril.MaxLife;
                float alpha = MathF.Sin(lifeRatio * MathHelper.Pi);

                //触须起点在椭圆边缘
                float startRx = radius * EllipseRatioX;
                float startRy = radius * EllipseRatioY;
                Vector2 startPos = center + new Vector2(
                    MathF.Cos(tendril.BaseAngle) * startRx,
                    MathF.Sin(tendril.BaseAngle) * startRy
                );

                //沿法线方向伸出，带波浪扭曲
                Vector2 outDir = new Vector2(MathF.Cos(tendril.BaseAngle), MathF.Sin(tendril.BaseAngle));
                int segs = 16;
                Vector2 prev = startPos;

                for (int i = 1; i <= segs; i++) {
                    float t = i / (float)segs;
                    float segDist = tendril.Length * t;
                    float wave = MathF.Sin(tendril.WavePhase + t * MathHelper.TwoPi) * tendril.WaveAmplitude * t;
                    Vector2 lateral = new Vector2(-outDir.Y, outDir.X);

                    Vector2 segPos = startPos + outDir * segDist + lateral * wave;

                    float segAlpha = alpha * (1f - t) * openProgress;
                    float segWidth = tendril.Width * (1f - t * 0.7f);

                    Color tendrilColor = Color.Lerp(
                        new Color(150, 80, 255, 0),
                        new Color(60, 20, 120, 0),
                        t
                    ) * segAlpha;

                    //绘制线段
                    Vector2 dir = segPos - prev;
                    float len = dir.Length();
                    if (len > 0.5f) {
                        float rot = dir.ToRotation();
                        sb.Draw(pixel, prev, new Rectangle(0, 0, 1, 1), tendrilColor, rot,
                            new Vector2(0f, 0.5f), new Vector2(len, segWidth), SpriteEffects.None, 0f);
                    }

                    prev = segPos;
                }
            }
        }

        /// <summary>扭曲环：从传送门向外扩散的时空涟漪</summary>
        private void DrawWarpRings(SpriteBatch sb, Texture2D pixel, Vector2 center) {
            foreach (var ring in warpRings) {
                float lifeRatio = ring.Life / (float)ring.MaxLife;
                float alpha = MathF.Sin(lifeRatio * MathHelper.Pi) * openProgress;

                int segments = 80;
                for (int i = 0; i < segments; i++) {
                    float angle = MathHelper.TwoPi * i / segments + ring.Rotation;
                    float rx = ring.Radius * EllipseRatioX;
                    float ry = ring.Radius * EllipseRatioY;

                    Vector2 pos = center + new Vector2(MathF.Cos(angle) * rx, MathF.Sin(angle) * ry);

                    //断续效果
                    float segBrightness = MathF.Sin(angle * 5f + ring.Rotation * 10f) * 0.5f + 0.5f;

                    Color ringColor = new Color(100, 50, 200, 0) * (alpha * 0.3f * segBrightness);
                    sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), ringColor, 0f, new Vector2(0.5f), 1.5f, SpriteEffects.None, 0f);
                }
            }
        }

        /// <summary>中心裂缝亮点</summary>
        private void DrawCenterFlare(SpriteBatch sb, Texture2D softGlow, Vector2 center) {
            float flare = MathF.Sin(GlobalTimer * 0.08f) * 0.4f + 0.6f;
            float flareScale = openProgress * 0.15f * flare;

            Color flareColor = new Color(200, 140, 255, 0) * (openProgress * flare * 0.5f);
            sb.Draw(softGlow, center, null, flareColor, 0f, softGlow.Size() / 2f, flareScale, SpriteEffects.None, 0f);

            //十字光芒
            Texture2D starTex = CWRAsset.StarTexture_White.Value;
            float starScale = openProgress * 0.5f * flare;
            Color starColor = new Color(180, 120, 255, 0) * (openProgress * 0.35f * flare);
            sb.Draw(starTex, center, null, starColor, GlobalTimer * 0.01f, starTex.Size() / 2f, starScale, SpriteEffects.None, 0f);
        }

        /// <summary>绘制填充椭圆</summary>
        private static void DrawFilledEllipse(SpriteBatch sb, Texture2D pixel, Vector2 center, float rx, float ry, Color color) {
            if (rx < 1 || ry < 1) return;

            int halfH = (int)ry;
            for (int y = -halfH; y <= halfH; y++) {
                float normalizedY = y / ry;
                float xSpan = rx * MathF.Sqrt(Math.Max(0, 1f - normalizedY * normalizedY));
                int drawWidth = (int)(xSpan * 2f);
                if (drawWidth <= 0) continue;

                Rectangle lineRect = new Rectangle(
                    (int)(center.X - xSpan),
                    (int)(center.Y + y),
                    drawWidth,
                    1
                );
                sb.Draw(pixel, lineRect, new Rectangle(0, 0, 1, 1), color);
            }
        }

        #endregion

        #region 内部数据结构

        private struct VoidTendril
        {
            public float BaseAngle;
            public float Length;
            public float MaxLength;
            public float Width;
            public int Life;
            public int MaxLife;
            public float WavePhase;
            public float WaveSpeed;
            public float WaveAmplitude;
        }

        private struct WarpRing
        {
            public float Radius;
            public float MaxRadius;
            public int Life;
            public int MaxLife;
            public float Rotation;
            public float RotSpeed;
        }

        #endregion
    }
}
