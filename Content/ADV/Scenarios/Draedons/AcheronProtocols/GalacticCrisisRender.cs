using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols
{
    /// <summary>
    /// 银河危机剧情演出渲染器
    /// 负责绘制全息星图投影：银河系、虫群阴影、灭绝令覆盖等视觉效果
    /// 使用阶段式动画控制，方便后续扩展新的演出阶段
    /// </summary>
    internal class GalacticCrisisRender : UIHandle
    {
        public override bool Active => active || fadeProgress > 0.01f;
        public override float RenderPriority => 0.88f;

        #region 动画阶段定义

        /// <summary>
        /// 演出阶段枚举，按剧情顺序排列
        /// </summary>
        internal enum AnimPhase
        {
            //无动画
            None = 0,
            //星图淡入，银河系缓缓展现
            GalaxyReveal,
            //虫群阴影从银河系外侧逼近，触须伸入
            SwarmApproach,
            //灭绝令激活，大片区域变为红色/灰色死域
            ExtinctionProtocol,
            //场景闲置，维持当前状态
            Idle,
            //整体淡出
            FadeOut,
        }

        #endregion

        #region 状态字段

        //全局控制
        private static bool active;
        private static float fadeProgress;
        private static AnimPhase currentPhase = AnimPhase.None;
        private static float phaseTimer;
        private static float phaseProgress;//当前阶段的归一化进度0~1

        //全息投影通用效果
        private static float hologramFlicker;
        private static float scanLineProgress;
        private static float globalTimer;

        //银河系参数
        private const int StarCount = 600;//恒星粒子数量
        private const int GalaxyArmCount = 4;//旋臂数量
        private const float GalaxyRadius = 180f;//银河系半径(像素)
        private const float GalaxyCoreRadius = 25f;//银核半径
        private static readonly List<GalaxyStar> galaxyStars = [];
        private static float galaxyRotation;//银河系整体缓慢旋转角度
        private static float galaxyRevealProgress;//银河系展现进度

        //虫群参数
        private const int SwarmTendrilCount = 12;//触须数量
        private const int SwarmParticleCount = 200;//虫群粒子数量
        private static readonly List<SwarmTendril> swarmTendrils = [];
        private static readonly List<SwarmParticle> swarmParticles = [];
        private static float swarmApproachProgress;//虫群逼近进度
        private static float swarmPulseTimer;//虫群脉动计时

        //灭绝令参数
        private static float extinctionProgress;//灭绝令覆盖进度
        private static float extinctionFlashTimer;//红色警告闪烁计时

        //信号干扰(Glitch)参数
        private static float glitchIntensity;//全局干扰强度
        private static float glitchTimer;
        private static int glitchFrameSkip;//跳帧模拟画面撕裂

        //泰拉标记
        private static float terraBlinkTimer;

        #endregion

        #region 恒星粒子

        private class GalaxyStar
        {
            public float ArmAngle;//所属旋臂的基础角度
            public float RadialDistance;//距离银核的径向距离(0~1归一化)
            public float AngleOffset;//在旋臂中的角度偏移
            public float Brightness;//亮度
            public float BrightnessPhase;//亮度闪烁相位
            public float Size;//大小
            public Color BaseColor;//基础颜色

            public Vector2 GetPosition(float rotation, float radius) {
                //对数螺旋公式：angle = baseAngle + k * ln(r)
                float r = RadialDistance * radius;
                float spiralTightness = 2.8f;
                float angle = ArmAngle + AngleOffset + spiralTightness * MathF.Log(MathF.Max(RadialDistance, 0.05f)) + rotation;
                return new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r);
            }
        }

        #endregion

        #region 虫群触须

        private class SwarmTendril
        {
            public float BaseAngle;//触须从银河系外侧伸入的角度
            public float Length;//当前长度(0~1)
            public float MaxLength;//最大长度
            public float Width;//宽度
            public float WavePhase;//蠕动相位
            public float WaveSpeed;//蠕动速度
            public float WaveAmplitude;//蠕动振幅
            public int SegmentCount;//段数
        }

        #endregion

        #region 虫群粒子

        private class SwarmParticle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
            public float Size;
            public float Alpha;
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 启动演出，初始化所有数据
        /// </summary>
        internal static void Activate() {
            active = true;
            fadeProgress = 0f;
            currentPhase = AnimPhase.None;
            phaseTimer = 0f;
            phaseProgress = 0f;
            hologramFlicker = 0f;
            scanLineProgress = 0f;
            globalTimer = 0f;
            galaxyRotation = 0f;
            galaxyRevealProgress = 0f;
            swarmApproachProgress = 0f;
            swarmPulseTimer = 0f;
            extinctionProgress = 0f;
            extinctionFlashTimer = 0f;
            glitchIntensity = 0f;
            glitchTimer = 0f;
            glitchFrameSkip = 0;
            terraBlinkTimer = 0f;

            GenerateGalaxy();
            GenerateSwarmTendrils();
            swarmParticles.Clear();
        }

        /// <summary>
        /// 切换到指定的动画阶段
        /// </summary>
        internal static void SetPhase(AnimPhase phase) {
            if (currentPhase == phase) return;
            currentPhase = phase;
            phaseTimer = 0f;
            phaseProgress = 0f;

            //阶段切换时播放对应音效
            switch (phase) {
                case AnimPhase.GalaxyReveal:
                    SoundEngine.PlaySound(SoundID.DD2_EtherianPortalSpawnEnemy with {
                        Volume = 0.5f, Pitch = 0.4f, MaxInstances = 1
                    });
                    break;
                case AnimPhase.SwarmApproach:
                    SoundEngine.PlaySound(SoundID.Zombie105 with {
                        Volume = 0.3f, Pitch = -0.6f, MaxInstances = 1
                    });
                    break;
                case AnimPhase.ExtinctionProtocol:
                    SoundEngine.PlaySound(SoundID.Item117 with {
                        Volume = 0.6f, Pitch = -0.3f, MaxInstances = 1
                    });
                    break;
            }
        }

        /// <summary>
        /// 清理所有资源
        /// </summary>
        internal static void Deactivate() {
            active = false;
            currentPhase = AnimPhase.FadeOut;
            phaseTimer = 0f;
        }

        internal static void ForceCleanup() {
            active = false;
            fadeProgress = 0f;
            currentPhase = AnimPhase.None;
            galaxyStars.Clear();
            swarmTendrils.Clear();
            swarmParticles.Clear();
        }

        #endregion

        #region 生成数据

        private static void GenerateGalaxy() {
            galaxyStars.Clear();
            //颜色调色板：蓝白色为主，核心偏黄
            Color[] armColors = [
                new Color(180, 200, 255),//蓝白
                new Color(200, 220, 255),//浅蓝
                new Color(160, 190, 255),//蓝
                new Color(220, 210, 255),//淡紫
            ];

            for (int i = 0; i < StarCount; i++) {
                int armIndex = i % GalaxyArmCount;
                float armAngle = MathHelper.TwoPi * armIndex / GalaxyArmCount;
                //径向分布：更多的恒星集中在内侧
                float radial = MathF.Pow(Main.rand.NextFloat(), 0.6f);
                //角度扰动：外侧扰动更大，模拟旋臂的扩散
                float angleJitter = Main.rand.NextFloat(-0.4f, 0.4f) * (0.3f + radial * 0.7f);

                float brightness = Main.rand.NextFloat(0.3f, 1f);
                //外侧恒星稍暗
                brightness *= MathHelper.Lerp(1f, 0.5f, radial);
                //核心区域恒星更亮
                if (radial < 0.15f) {
                    brightness = MathHelper.Lerp(brightness, 1f, 0.6f);
                }

                Color baseColor = armColors[armIndex];
                //核心区域偏黄
                if (radial < 0.2f) {
                    baseColor = Color.Lerp(baseColor, new Color(255, 240, 200), (0.2f - radial) * 4f);
                }

                galaxyStars.Add(new GalaxyStar {
                    ArmAngle = armAngle,
                    RadialDistance = radial,
                    AngleOffset = angleJitter,
                    Brightness = brightness,
                    BrightnessPhase = Main.rand.NextFloat(MathHelper.TwoPi),
                    Size = Main.rand.NextFloat(0.8f, 2.5f) * MathHelper.Lerp(1f, 0.4f, radial),
                    BaseColor = baseColor
                });
            }
        }

        private static void GenerateSwarmTendrils() {
            swarmTendrils.Clear();
            //虫群从银河系右上方逼近(模拟从银河旋臂外侧入侵)
            float swarmCenterAngle = -MathHelper.PiOver4;

            for (int i = 0; i < SwarmTendrilCount; i++) {
                float angleSpread = MathHelper.ToRadians(60f);
                float angle = swarmCenterAngle + Main.rand.NextFloat(-angleSpread, angleSpread);

                swarmTendrils.Add(new SwarmTendril {
                    BaseAngle = angle,
                    Length = 0f,
                    MaxLength = Main.rand.NextFloat(0.4f, 0.85f),
                    Width = Main.rand.NextFloat(8f, 25f),
                    WavePhase = Main.rand.NextFloat(MathHelper.TwoPi),
                    WaveSpeed = Main.rand.NextFloat(1.5f, 3f),
                    WaveAmplitude = Main.rand.NextFloat(5f, 15f),
                    SegmentCount = Main.rand.Next(12, 24)
                });
            }
        }

        #endregion

        #region 逻辑更新

        public override void LogicUpdate() {
            if (!active && fadeProgress <= 0.01f) return;

            globalTimer += 0.016f;
            hologramFlicker += 0.06f;
            scanLineProgress += 0.025f;
            if (hologramFlicker > MathHelper.TwoPi) hologramFlicker -= MathHelper.TwoPi;
            if (scanLineProgress > 1f) scanLineProgress -= 1f;

            //银河系缓慢旋转
            galaxyRotation += 0.0008f;

            //更新阶段
            phaseTimer += 1f;
            UpdatePhase();

            //更新全息干扰
            UpdateGlitch();

            //更新虫群粒子
            UpdateSwarmParticles();

            //泰拉闪烁
            terraBlinkTimer += 0.05f;

            //整体淡入淡出
            if (active && currentPhase != AnimPhase.FadeOut) {
                fadeProgress = MathF.Min(fadeProgress + 0.03f, 1f);
            }
            else if (currentPhase == AnimPhase.FadeOut) {
                fadeProgress = MathF.Max(fadeProgress - 0.04f, 0f);
                if (fadeProgress <= 0.01f) {
                    ForceCleanup();
                }
            }

            //安全检测
            if (!DraedonEffect.IsActive && active) {
                Deactivate();
            }
        }

        private static void UpdatePhase() {
            switch (currentPhase) {
                case AnimPhase.GalaxyReveal:
                    //银河系在120帧(2秒)内完全展现
                    galaxyRevealProgress = MathF.Min(galaxyRevealProgress + 0.008f, 1f);
                    phaseProgress = galaxyRevealProgress;
                    break;

                case AnimPhase.SwarmApproach:
                    //虫群逼近持续进行
                    swarmApproachProgress = MathF.Min(swarmApproachProgress + 0.004f, 1f);
                    swarmPulseTimer += 0.04f;
                    phaseProgress = swarmApproachProgress;

                    //虫群触须生长
                    foreach (var tendril in swarmTendrils) {
                        tendril.Length = MathF.Min(tendril.Length + 0.006f * Main.rand.NextFloat(0.5f, 1.5f), tendril.MaxLength * swarmApproachProgress);
                        tendril.WavePhase += tendril.WaveSpeed * 0.016f;
                    }

                    //虫群越近干扰越强
                    glitchIntensity = MathHelper.Lerp(0.02f, 0.15f, swarmApproachProgress);

                    //生成虫群粒子
                    SpawnSwarmParticles();
                    break;

                case AnimPhase.ExtinctionProtocol:
                    //灭绝令覆盖在90帧(1.5秒)内展开
                    extinctionProgress = MathF.Min(extinctionProgress + 0.011f, 1f);
                    extinctionFlashTimer += 0.08f;
                    phaseProgress = extinctionProgress;
                    //灭绝令激活时干扰短暂增强
                    if (extinctionProgress < 0.3f) {
                        glitchIntensity = MathHelper.Lerp(0.3f, 0.1f, extinctionProgress / 0.3f);
                    }
                    break;

                case AnimPhase.Idle:
                    //闲置状态维持轻微的虫群脉动和旋转
                    swarmPulseTimer += 0.03f;
                    break;

                case AnimPhase.FadeOut:
                    break;
            }
        }

        private static void UpdateGlitch() {
            glitchTimer += 0.1f;
            //随机产生画面干扰
            if (Main.rand.NextFloat() < glitchIntensity * 0.3f) {
                glitchFrameSkip = Main.rand.Next(1, 4);
            }
            if (glitchFrameSkip > 0) {
                glitchFrameSkip--;
            }
        }

        private static void SpawnSwarmParticles() {
            if (swarmParticles.Count >= SwarmParticleCount) return;
            if (!Main.rand.NextBool(3)) return;

            float swarmCenterAngle = -MathHelper.PiOver4;
            float spawnAngle = swarmCenterAngle + Main.rand.NextFloat(-0.8f, 0.8f);
            float spawnDist = GalaxyRadius * (1.1f + Main.rand.NextFloat(0.3f));
            Vector2 spawnPos = new Vector2(MathF.Cos(spawnAngle), MathF.Sin(spawnAngle)) * spawnDist;

            //向银核方向移动
            Vector2 toCenter = -spawnPos;
            toCenter.Normalize();
            float speed = Main.rand.NextFloat(0.3f, 1.0f);
            Vector2 velocity = toCenter * speed + new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextFloat(-0.2f, 0.2f));

            swarmParticles.Add(new SwarmParticle {
                Position = spawnPos,
                Velocity = velocity,
                Life = 0f,
                MaxLife = Main.rand.NextFloat(80f, 200f),
                Size = Main.rand.NextFloat(1f, 3f),
                Alpha = Main.rand.NextFloat(0.3f, 0.8f)
            });
        }

        private static void UpdateSwarmParticles() {
            for (int i = swarmParticles.Count - 1; i >= 0; i--) {
                var p = swarmParticles[i];
                p.Life++;
                p.Position += p.Velocity;
                if (p.Life >= p.MaxLife) {
                    swarmParticles.RemoveAt(i);
                }
            }
        }

        #endregion

        #region 绘制

        /// <summary>
        /// 获取星图中心的屏幕坐标
        /// </summary>
        private static Vector2 GetMapCenter() {
            return new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.38f);
        }

        public override void Draw(SpriteBatch sb) {
            if (fadeProgress <= 0.01f) return;
            if (glitchFrameSkip > 0 && Main.rand.NextBool(3)) return;//模拟画面撕裂跳帧

            float alpha = fadeProgress;
            //全息闪烁
            float flicker = MathF.Sin(hologramFlicker * 1.5f) * 0.08f + 0.92f;
            alpha *= flicker;

            Vector2 center = GetMapCenter();

            //绘制背景暗色遮罩（让星图更清晰）
            DrawBackgroundDim(sb, center, alpha);

            //绘制银河系
            if (galaxyRevealProgress > 0.01f) {
                DrawGalaxy(sb, center, alpha);
            }

            //绘制虫群
            if (swarmApproachProgress > 0.01f || currentPhase == AnimPhase.Idle) {
                DrawSwarm(sb, center, alpha);
            }

            //绘制灭绝令效果
            if (extinctionProgress > 0.01f) {
                DrawExtinctionOverlay(sb, center, alpha);
            }

            //绘制泰拉标记
            if (galaxyRevealProgress > 0.5f) {
                DrawTerraMarker(sb, center, alpha);
            }

            //绘制全息边框
            DrawHologramFrame(sb, center, alpha);

            //绘制扫描线
            DrawScanLineEffect(sb, center, alpha);

            //绘制干扰噪点
            if (glitchIntensity > 0.02f) {
                DrawGlitchNoise(sb, center, alpha);
            }
        }

        #endregion

        #region 背景遮罩

        private static void DrawBackgroundDim(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            float radius = GalaxyRadius + 80f;
            //画一个圆形暗域，不用纯矩形
            int segments = 40;
            for (int i = 0; i < segments; i++) {
                float t = i / (float)segments;
                float innerR = radius * t;
                float outerR = radius * (t + 1f / segments);
                float dimAlpha = alpha * 0.4f * (1f - t * 0.5f);

                int ringSegments = 32;
                for (int j = 0; j < ringSegments; j++) {
                    float angle = MathHelper.TwoPi * j / ringSegments;
                    float nextAngle = MathHelper.TwoPi * (j + 1) / ringSegments;

                    Vector2 p = center + new Vector2(MathF.Cos(angle) * innerR, MathF.Sin(angle) * innerR);
                    float size = (outerR - innerR) + 2f;

                    sb.Draw(pixel, p, new Rectangle(0, 0, 1, 1),
                        new Color(5, 5, 15) * dimAlpha, angle, Vector2.Zero,
                        new Vector2(size, size), SpriteEffects.None, 0f);
                }
            }
        }

        #endregion

        #region 银河系绘制

        private static void DrawGalaxy(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            float reveal = CWRUtils.EaseOutCubic(galaxyRevealProgress);
            float galaxyAlpha = alpha * reveal;

            //绘制银核发光
            DrawGalaxyCore(sb, center, galaxyAlpha);

            //绘制恒星
            foreach (var star in galaxyStars) {
                //根据径向距离决定是否可见（从核心向外渐次展现）
                if (star.RadialDistance > reveal) continue;

                Vector2 pos = star.GetPosition(galaxyRotation, GalaxyRadius);
                Vector2 screenPos = center + pos;

                //亮度闪烁
                float flicker = MathF.Sin(globalTimer * 2f + star.BrightnessPhase) * 0.2f + 0.8f;
                float finalBrightness = star.Brightness * flicker * galaxyAlpha;

                Color starColor = star.BaseColor * finalBrightness;

                //绘制恒星光点
                float size = star.Size;
                sb.Draw(pixel, screenPos, new Rectangle(0, 0, 1, 1),
                    starColor, 0f, new Vector2(0.5f), new Vector2(size), SpriteEffects.None, 0f);

                //较亮的恒星加一层柔和的光晕
                if (star.Brightness > 0.7f) {
                    Color glowColor = starColor * 0.3f;
                    glowColor.A = 0;
                    sb.Draw(pixel, screenPos, new Rectangle(0, 0, 1, 1),
                        glowColor, 0f, new Vector2(0.5f), new Vector2(size * 3f), SpriteEffects.None, 0f);
                }
            }
        }

        private static void DrawGalaxyCore(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            //多层叠加模拟银核发光
            Color coreColor = new Color(255, 240, 200);
            for (int i = 5; i >= 0; i--) {
                float layerRadius = GalaxyCoreRadius * (0.3f + i * 0.3f);
                float layerAlpha = alpha * (0.4f - i * 0.05f);

                //脉动效果
                float pulse = MathF.Sin(globalTimer * 1.5f + i * 0.5f) * 0.1f + 0.9f;
                layerAlpha *= pulse;

                Color layerColor = Color.Lerp(coreColor, new Color(180, 200, 255), i * 0.15f);
                layerColor.A = 0;

                //用多个小矩形近似圆形
                int circleSegments = 16;
                for (int j = 0; j < circleSegments; j++) {
                    float angle = MathHelper.TwoPi * j / circleSegments;
                    Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * layerRadius * 0.5f;
                    sb.Draw(pixel, center + offset, new Rectangle(0, 0, 1, 1),
                        layerColor * layerAlpha, angle, new Vector2(0.5f),
                        new Vector2(layerRadius * 0.8f, layerRadius * 0.3f), SpriteEffects.None, 0f);
                }
            }
        }

        #endregion

        #region 虫群绘制

        private static void DrawSwarm(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            float swarmAlpha = alpha * MathF.Min(swarmApproachProgress * 2f, 1f);

            //绘制虫群主体阴影(一个从银河系外侧压入的暗色团块)
            DrawSwarmShadowMass(sb, center, swarmAlpha);

            //绘制触须
            foreach (var tendril in swarmTendrils) {
                DrawSwarmTendril(sb, center, tendril, swarmAlpha);
            }

            //绘制虫群粒子
            DrawSwarmParticles(sb, center, swarmAlpha);

            //绘制虫群边缘的暗红色脉动光晕
            DrawSwarmEdgeGlow(sb, center, swarmAlpha);
        }

        private static void DrawSwarmShadowMass(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            float swarmCenterAngle = -MathHelper.PiOver4;
            //虫群主体在银河系外侧，随进度压入
            float swarmDistance = GalaxyRadius * MathHelper.Lerp(1.6f, 0.9f, swarmApproachProgress);
            Vector2 swarmCenter = center + new Vector2(MathF.Cos(swarmCenterAngle), MathF.Sin(swarmCenterAngle)) * swarmDistance;

            //虫群主体大小随进度增大
            float massRadius = GalaxyRadius * MathHelper.Lerp(0.6f, 1.2f, swarmApproachProgress);

            //多层不透明度叠加，模拟不规则的黑暗团块
            Color shadowColor = new Color(8, 3, 8);
            int layers = 8;
            for (int i = 0; i < layers; i++) {
                float t = i / (float)layers;
                float layerRadius = massRadius * (1f - t * 0.6f);
                float layerAlpha = alpha * (0.6f - t * 0.06f);

                //蠕动变形
                float wobble = MathF.Sin(swarmPulseTimer * 2f + t * 3f) * 8f * (1f - t);

                //用多个偏移的矩形模拟不规则边缘
                int chunks = 12 + i * 2;
                for (int j = 0; j < chunks; j++) {
                    float angle = MathHelper.TwoPi * j / chunks + t * 0.3f;
                    float dist = (layerRadius + wobble) * (0.6f + Main.rand.NextFloat(0.0f, 0.05f));
                    Vector2 chunkPos = swarmCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist;
                    float chunkSize = layerRadius * 0.4f / (1f + t);

                    sb.Draw(pixel, chunkPos, new Rectangle(0, 0, 1, 1),
                        shadowColor * layerAlpha, angle + globalTimer * 0.2f, new Vector2(0.5f),
                        new Vector2(chunkSize, chunkSize * 0.6f), SpriteEffects.None, 0f);
                }
            }
        }

        private static void DrawSwarmTendril(SpriteBatch sb, Vector2 center, SwarmTendril tendril, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;
            if (tendril.Length <= 0.01f) return;

            //触须从银河系外侧向核心伸入
            float startDist = GalaxyRadius * 1.3f;
            float endDist = GalaxyRadius * (1.3f - tendril.Length * 1.5f);

            Vector2 startPos = center + new Vector2(MathF.Cos(tendril.BaseAngle), MathF.Sin(tendril.BaseAngle)) * startDist;
            Vector2 endPos = center + new Vector2(MathF.Cos(tendril.BaseAngle), MathF.Sin(tendril.BaseAngle)) * endDist;

            Color tendrilColor = new Color(15, 5, 12);
            Color edgeColor = new Color(80, 15, 20);

            for (int seg = 0; seg < tendril.SegmentCount; seg++) {
                float t = seg / (float)tendril.SegmentCount;
                if (t > tendril.Length / tendril.MaxLength) break;

                Vector2 segPos = Vector2.Lerp(startPos, endPos, t);

                //蠕动偏移
                float perpAngle = tendril.BaseAngle + MathHelper.PiOver2;
                float waveOffset = MathF.Sin(tendril.WavePhase + t * 8f) * tendril.WaveAmplitude * t;
                segPos += new Vector2(MathF.Cos(perpAngle), MathF.Sin(perpAngle)) * waveOffset;

                //宽度从粗到细
                float segWidth = tendril.Width * (1f - t * 0.7f);
                float segAlpha = alpha * (0.8f - t * 0.3f);

                //绘制触须段
                float segAngle = tendril.BaseAngle + MathHelper.Pi;
                sb.Draw(pixel, segPos, new Rectangle(0, 0, 1, 1),
                    tendrilColor * segAlpha, segAngle, new Vector2(0.5f),
                    new Vector2(segWidth * 2f, segWidth * 0.5f), SpriteEffects.None, 0f);

                //边缘暗红色光晕
                Color glowC = edgeColor;
                glowC.A = 0;
                float glowPulse = MathF.Sin(swarmPulseTimer * 3f + t * 5f) * 0.3f + 0.5f;
                sb.Draw(pixel, segPos, new Rectangle(0, 0, 1, 1),
                    glowC * (segAlpha * 0.4f * glowPulse), segAngle, new Vector2(0.5f),
                    new Vector2(segWidth * 3.5f, segWidth * 1.2f), SpriteEffects.None, 0f);
            }
        }

        private static void DrawSwarmParticles(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            foreach (var p in swarmParticles) {
                float lifeRatio = p.Life / p.MaxLife;
                float fade = MathF.Sin(lifeRatio * MathHelper.Pi);
                float particleAlpha = p.Alpha * fade * alpha;

                Vector2 screenPos = center + p.Position;
                Color particleColor = new Color(60, 10, 20) * particleAlpha;

                sb.Draw(pixel, screenPos, new Rectangle(0, 0, 1, 1),
                    particleColor, globalTimer + p.Life * 0.1f, new Vector2(0.5f),
                    new Vector2(p.Size * 1.5f, p.Size * 0.5f), SpriteEffects.None, 0f);
            }
        }

        private static void DrawSwarmEdgeGlow(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            float swarmCenterAngle = -MathHelper.PiOver4;
            float swarmDistance = GalaxyRadius * MathHelper.Lerp(1.6f, 0.9f, swarmApproachProgress);

            //在虫群前沿绘制暗红色脉动弧线
            int arcSegments = 20;
            float arcSpread = MathHelper.ToRadians(80f);
            float pulse = MathF.Sin(swarmPulseTimer * 2f) * 0.3f + 0.7f;

            for (int i = 0; i < arcSegments; i++) {
                float t = i / (float)arcSegments;
                float angle = swarmCenterAngle - arcSpread / 2f + arcSpread * t;
                Vector2 arcPos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (swarmDistance - 10f);

                Color glowColor = new Color(120, 20, 30);
                glowColor.A = 0;
                float arcAlpha = alpha * 0.5f * pulse * MathF.Sin(t * MathHelper.Pi);

                sb.Draw(pixel, arcPos, new Rectangle(0, 0, 1, 1),
                    glowColor * arcAlpha, angle, new Vector2(0.5f),
                    new Vector2(15f, 4f), SpriteEffects.None, 0f);
            }
        }

        #endregion

        #region 灭绝令效果

        private static void DrawExtinctionOverlay(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            float eased = CWRUtils.EaseOutCubic(extinctionProgress);
            //灭绝令从外环向内覆盖
            float coverRadius = GalaxyRadius * eased * 0.85f;

            //红色/灰色死域覆盖
            Color deathColor = new Color(80, 20, 15);
            Color flashColor = new Color(200, 50, 30);
            float flash = MathF.Sin(extinctionFlashTimer) * 0.3f + 0.7f;

            //从外到内的覆盖环
            int rings = 12;
            for (int i = 0; i < rings; i++) {
                float ringT = i / (float)rings;
                float ringRadius = GalaxyRadius * (1f - ringT * eased);

                if (ringRadius > GalaxyRadius || ringRadius < GalaxyRadius - coverRadius) continue;

                float ringAlpha = alpha * 0.35f * flash;

                int segments = 24;
                for (int j = 0; j < segments; j++) {
                    float angle = MathHelper.TwoPi * j / segments;
                    Vector2 pos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * ringRadius;

                    Color c = Color.Lerp(deathColor, flashColor, ringT * flash);
                    c.A = 0;

                    sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1),
                        c * ringAlpha, angle, new Vector2(0.5f),
                        new Vector2(18f, 5f), SpriteEffects.None, 0f);
                }
            }

            //灭绝令文本闪烁(仅在初期阶段)
            if (extinctionProgress < 0.5f && extinctionProgress > 0.05f) {
                float textAlpha = alpha * flash * MathF.Sin(extinctionProgress / 0.5f * MathHelper.Pi);
                string warningText = "◢ EXTINCTION PROTOCOL ACTIVE ◣";
                var font = FontAssets.MouseText.Value;
                Vector2 textSize = font.MeasureString(warningText) * 0.6f;
                Vector2 textPos = center + new Vector2(-textSize.X * 0.5f, GalaxyRadius + 30f);

                //发光底色
                for (int g = 0; g < 4; g++) {
                    float gAngle = MathHelper.TwoPi * g / 4f;
                    Vector2 gOffset = new Vector2(MathF.Cos(gAngle), MathF.Sin(gAngle)) * 1.5f;
                    Utils.DrawBorderString(sb, warningText, textPos + gOffset,
                        new Color(200, 50, 30) * (textAlpha * 0.5f), 0.6f);
                }
                Utils.DrawBorderString(sb, warningText, textPos, Color.White * textAlpha, 0.6f);
            }
        }

        #endregion

        #region 泰拉标记

        private static void DrawTerraMarker(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            //泰拉位于银河系中心附近
            Vector2 terraPos = center + new Vector2(3f, 2f);//轻微偏移
            float blink = MathF.Sin(terraBlinkTimer) * 0.3f + 0.7f;
            float markerAlpha = alpha * blink;

            //绿色标记点
            Color terraColor = new Color(100, 255, 120);

            //闪烁的点
            sb.Draw(pixel, terraPos, new Rectangle(0, 0, 1, 1),
                terraColor * markerAlpha, 0f, new Vector2(0.5f),
                new Vector2(4f), SpriteEffects.None, 0f);

            //光环
            Color ringColor = terraColor;
            ringColor.A = 0;
            float ringPulse = (MathF.Sin(terraBlinkTimer * 0.7f) + 1f) * 0.5f;
            float ringRadius = 8f + ringPulse * 6f;

            int segments = 16;
            for (int i = 0; i < segments; i++) {
                float angle = MathHelper.TwoPi * i / segments;
                Vector2 pos = terraPos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * ringRadius;
                sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1),
                    ringColor * (markerAlpha * 0.5f * (1f - ringPulse * 0.5f)),
                    angle, new Vector2(0.5f), new Vector2(5f, 1.5f), SpriteEffects.None, 0f);
            }

            //如果灭绝令激活，泰拉标记变红
            if (extinctionProgress > 0.5f) {
                float dangerAlpha = alpha * MathF.Sin(extinctionFlashTimer * 2f) * 0.5f + 0.5f;
                Color dangerColor = new Color(255, 60, 40);
                dangerColor.A = 0;

                sb.Draw(pixel, terraPos, new Rectangle(0, 0, 1, 1),
                    dangerColor * dangerAlpha, 0f, new Vector2(0.5f),
                    new Vector2(8f), SpriteEffects.None, 0f);
            }

            //标注文字"TERRA"
            float textAlpha = alpha * 0.7f;
            Utils.DrawBorderString(sb, "TERRA", terraPos + new Vector2(10, -8),
                terraColor * textAlpha, 0.4f);
        }

        #endregion

        #region 全息边框

        private static void DrawHologramFrame(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            float frameSize = GalaxyRadius + 40f;
            Rectangle frameRect = new(
                (int)(center.X - frameSize),
                (int)(center.Y - frameSize),
                (int)(frameSize * 2f),
                (int)(frameSize * 2f)
            );

            Color techColor = new Color(60, 160, 220) * (alpha * 0.5f);

            //绘制四边淡蓝色线
            int thickness = 2;
            sb.Draw(pixel, new Rectangle(frameRect.X, frameRect.Y, frameRect.Width, thickness), techColor);
            sb.Draw(pixel, new Rectangle(frameRect.X, frameRect.Bottom - thickness, frameRect.Width, thickness), techColor * 0.7f);
            sb.Draw(pixel, new Rectangle(frameRect.X, frameRect.Y, thickness, frameRect.Height), techColor * 0.85f);
            sb.Draw(pixel, new Rectangle(frameRect.Right - thickness, frameRect.Y, thickness, frameRect.Height), techColor * 0.85f);

            //四角装饰
            float cornerSize = 15f;
            Color cornerColor = new Color(80, 200, 255) * (alpha * 0.6f);
            DrawCornerDecor(sb, new Vector2(frameRect.X + 6, frameRect.Y + 6), cornerColor, cornerSize, -MathHelper.PiOver2);
            DrawCornerDecor(sb, new Vector2(frameRect.Right - 6, frameRect.Y + 6), cornerColor, cornerSize, 0f);
            DrawCornerDecor(sb, new Vector2(frameRect.X + 6, frameRect.Bottom - 6), cornerColor, cornerSize, MathHelper.Pi);
            DrawCornerDecor(sb, new Vector2(frameRect.Right - 6, frameRect.Bottom - 6), cornerColor, cornerSize, MathHelper.PiOver2);
        }

        private static void DrawCornerDecor(SpriteBatch sb, Vector2 pos, Color color, float size, float rotation) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), color, rotation,
                new Vector2(0.5f), new Vector2(size, size * 0.2f), SpriteEffects.None, 0f);
            sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), color * 0.7f, rotation + MathHelper.PiOver2,
                new Vector2(0.5f), new Vector2(size, size * 0.2f), SpriteEffects.None, 0f);
        }

        #endregion

        #region 扫描线和干扰

        private static void DrawScanLineEffect(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            float frameSize = GalaxyRadius + 40f;
            float scanY = center.Y - frameSize + scanLineProgress * frameSize * 2f;

            Color scanColor = new Color(60, 180, 255) * (alpha * 0.25f);
            sb.Draw(pixel, new Vector2(center.X - frameSize, scanY), new Rectangle(0, 0, 1, 1),
                scanColor, 0f, Vector2.Zero, new Vector2(frameSize * 2f, 2f), SpriteEffects.None, 0f);

            //辅助扫描线
            float scan2 = (scanLineProgress + 0.5f) % 1f;
            float scan2Y = center.Y - frameSize + scan2 * frameSize * 2f;
            sb.Draw(pixel, new Vector2(center.X - frameSize, scan2Y), new Rectangle(0, 0, 1, 1),
                scanColor * 0.4f, 0f, Vector2.Zero, new Vector2(frameSize * 2f, 1f), SpriteEffects.None, 0f);
        }

        private static void DrawGlitchNoise(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            float frameSize = GalaxyRadius + 40f;
            int noiseCount = (int)(glitchIntensity * 40f);

            for (int i = 0; i < noiseCount; i++) {
                float x = center.X - frameSize + Main.rand.NextFloat(frameSize * 2f);
                float y = center.Y - frameSize + Main.rand.NextFloat(frameSize * 2f);
                float w = Main.rand.NextFloat(5f, 40f);
                float h = Main.rand.NextFloat(1f, 3f);

                Color noiseColor = Main.rand.NextBool()
                    ? new Color(80, 200, 255) * (alpha * 0.15f)
                    : new Color(200, 50, 50) * (alpha * 0.1f);

                sb.Draw(pixel, new Vector2(x, y), new Rectangle(0, 0, 1, 1),
                    noiseColor, 0f, Vector2.Zero, new Vector2(w, h), SpriteEffects.None, 0f);
            }
        }

        #endregion
    }
}
