using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
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

        //面板参数
        private const float PanelWidth = 620f;
        private const float PanelHeight = 580f;
        private const int BorderThickness = 3;

        //银河系参数
        private const int StarCount = 800;//恒星粒子数量
        private const int GalaxyArmCount = 4;//旋臂数量
        private const float GalaxyRadius = 240f;//银河系半径(像素)
        private const float GalaxyCoreRadius = 30f;//银核半径
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
        private static bool extinctionStarsMarked;//恒星是否已经开始标记
        private static float extinctionWaveRadius;//灭绝令红色波纹的当前半径

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
            public bool ExtinctionMarked;//是否被灭绝令标记
            public float ExtinctionLerp;//灭绝令变红的插值进度0~1

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
            extinctionStarsMarked = false;
            extinctionWaveRadius = 0f;
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
                    //灭绝令波纹从外环向内扩展，标记途经的恒星
                    extinctionWaveRadius = GalaxyRadius * CWRUtils.EaseOutCubic(extinctionProgress) * 0.9f;
                    UpdateExtinctionStarMarking();
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

        /// <summary>
        /// 灭绝令波纹扫过恒星时将其标记为红色
        /// 波纹从银河系外环向核心推进，途经的恒星依次变红
        /// </summary>
        private static void UpdateExtinctionStarMarking() {
            float waveNormalized = extinctionWaveRadius / GalaxyRadius;
            foreach (var star in galaxyStars) {
                //已经标记的恒星继续推进变红插值
                if (star.ExtinctionMarked) {
                    star.ExtinctionLerp = MathF.Min(star.ExtinctionLerp + 0.04f, 1f);
                    continue;
                }
                //波纹从外向内，外侧恒星先被标记
                //恒星的径向距离越大越先被标记
                float starOuterDistance = 1f - star.RadialDistance;
                if (starOuterDistance < waveNormalized && star.RadialDistance > 0.12f) {
                    //核心区域(泰拉附近)的恒星不被标记
                    star.ExtinctionMarked = true;
                    star.ExtinctionLerp = 0f;
                }
            }
        }

        #endregion

        #region 绘制

        /// <summary>
        /// 获取面板矩形区域
        /// </summary>
        private static Rectangle GetPanelRect() {
            int x = (int)(Main.screenWidth * 0.5f - PanelWidth * 0.5f);
            int y = (int)(Main.screenHeight * 0.32f - PanelHeight * 0.5f);
            return new Rectangle(x, y, (int)PanelWidth, (int)PanelHeight);
        }

        /// <summary>
        /// 获取星图中心的屏幕坐标
        /// </summary>
        private static Vector2 GetMapCenter() {
            Rectangle panel = GetPanelRect();
            return new Vector2(panel.X + panel.Width * 0.5f, panel.Y + panel.Height * 0.5f);
        }

        public override void Draw(SpriteBatch sb) {
            if (fadeProgress <= 0.01f) return;
            if (glitchFrameSkip > 0 && Main.rand.NextBool(3)) return;//模拟画面撕裂跳帧

            float alpha = fadeProgress;
            //全息闪烁
            float flicker = MathF.Sin(hologramFlicker * 1.5f) * 0.08f + 0.92f;
            alpha *= flicker;

            Vector2 center = GetMapCenter();
            Rectangle panelRect = GetPanelRect();

            //绘制厚实的面板底板和边框
            DrawPanelBackground(sb, panelRect, alpha);
            DrawPanelBorder(sb, panelRect, alpha);

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

            //绘制扫描线（在边框内）
            DrawScanLineEffect(sb, panelRect, alpha);

            //绘制干扰噪点
            if (glitchIntensity > 0.02f) {
                DrawGlitchNoise(sb, panelRect, alpha);
            }

            //最顶层绘制面板标题
            DrawPanelHeader(sb, panelRect, alpha);
        }

        #endregion

        #region 面板背景和边框

        /// <summary>
        /// 绘制厚实的不透明面板底板
        /// </summary>
        private static void DrawPanelBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            //主背景：深色不透明，避免游戏内容透出
            sb.Draw(pixel, rect, new Rectangle(0, 0, 1, 1), new Color(6, 8, 16) * (alpha * 0.92f));

            //内层渐变增加深度感
            Rectangle innerRect = new(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8);
            sb.Draw(pixel, innerRect, new Rectangle(0, 0, 1, 1), new Color(10, 14, 25) * (alpha * 0.6f));

            //底部微弱的渐变高光
            Rectangle bottomGlow = new(rect.X + 6, rect.Bottom - 40, rect.Width - 12, 36);
            sb.Draw(pixel, bottomGlow, new Rectangle(0, 0, 1, 1), new Color(20, 35, 55) * (alpha * 0.25f));
        }

        /// <summary>
        /// 绘制科技感的厚实边框
        /// </summary>
        private static void DrawPanelBorder(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            Color techColor = new Color(60, 160, 220);
            float pulse = MathF.Sin(hologramFlicker * 2f) * 0.15f + 0.85f;
            Color borderColor = techColor * (alpha * 0.85f * pulse);
            Color borderDim = techColor * (alpha * 0.5f);

            //外层粗边框
            sb.Draw(pixel, new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 2, BorderThickness), borderColor);
            sb.Draw(pixel, new Rectangle(rect.X - 1, rect.Bottom - BorderThickness + 1, rect.Width + 2, BorderThickness), borderColor * 0.8f);
            sb.Draw(pixel, new Rectangle(rect.X - 1, rect.Y - 1, BorderThickness, rect.Height + 2), borderColor * 0.9f);
            sb.Draw(pixel, new Rectangle(rect.Right - BorderThickness + 1, rect.Y - 1, BorderThickness, rect.Height + 2), borderColor * 0.9f);

            //内层细边框（双层效果）
            sb.Draw(pixel, new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, 1), borderDim);
            sb.Draw(pixel, new Rectangle(rect.X + 4, rect.Bottom - 5, rect.Width - 8, 1), borderDim * 0.7f);
            sb.Draw(pixel, new Rectangle(rect.X + 4, rect.Y + 4, 1, rect.Height - 8), borderDim * 0.8f);
            sb.Draw(pixel, new Rectangle(rect.Right - 5, rect.Y + 4, 1, rect.Height - 8), borderDim * 0.8f);

            //四角装饰（增大的L形科技角标）
            float cornerSize = 20f;
            Color cornerColor = new Color(80, 200, 255) * (alpha * 0.7f);
            DrawCornerDecor(sb, new Vector2(rect.X + 8, rect.Y + 8), cornerColor, cornerSize, -MathHelper.PiOver2);
            DrawCornerDecor(sb, new Vector2(rect.Right - 8, rect.Y + 8), cornerColor, cornerSize, 0f);
            DrawCornerDecor(sb, new Vector2(rect.X + 8, rect.Bottom - 8), cornerColor, cornerSize, MathHelper.Pi);
            DrawCornerDecor(sb, new Vector2(rect.Right - 8, rect.Bottom - 8), cornerColor, cornerSize, MathHelper.PiOver2);

            //角落内侧辅助装饰线
            Color auxColor = techColor * (alpha * 0.3f);
            sb.Draw(pixel, new Rectangle(rect.X + 8, rect.Y + 8, 30, 1), auxColor);
            sb.Draw(pixel, new Rectangle(rect.X + 8, rect.Y + 8, 1, 30), auxColor);
            sb.Draw(pixel, new Rectangle(rect.Right - 38, rect.Y + 8, 30, 1), auxColor);
            sb.Draw(pixel, new Rectangle(rect.Right - 9, rect.Y + 8, 1, 30), auxColor);
            sb.Draw(pixel, new Rectangle(rect.X + 8, rect.Bottom - 9, 30, 1), auxColor);
            sb.Draw(pixel, new Rectangle(rect.X + 8, rect.Bottom - 38, 1, 30), auxColor);
            sb.Draw(pixel, new Rectangle(rect.Right - 38, rect.Bottom - 9, 30, 1), auxColor);
            sb.Draw(pixel, new Rectangle(rect.Right - 9, rect.Bottom - 38, 1, 30), auxColor);
        }

        /// <summary>
        /// 绘制面板顶部标题栏
        /// </summary>
        private static void DrawPanelHeader(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            Color techColor = new Color(60, 160, 220);

            //标题栏背景
            Rectangle headerRect = new(rect.X + 5, rect.Y + 5, rect.Width - 10, 28);
            sb.Draw(pixel, headerRect, new Rectangle(0, 0, 1, 1), new Color(12, 22, 38) * (alpha * 0.8f));

            //标题栏底线
            sb.Draw(pixel, new Rectangle(headerRect.X, headerRect.Bottom, headerRect.Width, 2), techColor * (alpha * 0.5f));
            sb.Draw(pixel, new Rectangle(headerRect.X, headerRect.Bottom + 3, headerRect.Width, 1), techColor * (alpha * 0.2f));

            //标题文本
            string title = "◢ GALACTIC STRATEGIC MAP ◣";
            var font = FontAssets.MouseText.Value;
            Vector2 titleSize = font.MeasureString(title) * 0.45f;
            Vector2 titlePos = new(
                headerRect.X + (headerRect.Width - titleSize.X) * 0.5f,
                headerRect.Y + (headerRect.Height - titleSize.Y) * 0.5f
            );

            //标题发光
            for (int i = 0; i < 4; i++) {
                float angle = MathHelper.TwoPi * i / 4f;
                Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 1f;
                Utils.DrawBorderString(sb, title, titlePos + offset, techColor * (alpha * 0.4f), 0.45f);
            }
            Utils.DrawBorderString(sb, title, titlePos, Color.White * alpha, 0.45f);
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

            Texture2D softGlow = CWRAsset.SoftGlow?.Value;

            //绘制恒星
            foreach (var star in galaxyStars) {
                //根据径向距离决定是否可见（从核心向外渐次展现）
                if (star.RadialDistance > reveal) continue;

                Vector2 pos = star.GetPosition(galaxyRotation, GalaxyRadius);
                Vector2 screenPos = center + pos;

                //亮度闪烁
                float flicker = MathF.Sin(globalTimer * 2f + star.BrightnessPhase) * 0.2f + 0.8f;
                float finalBrightness = star.Brightness * flicker * galaxyAlpha;

                //灭绝令变红：被标记的恒星颜色从原色渐变到红色并脉动
                Color starColor = star.BaseColor;
                if (star.ExtinctionMarked && star.ExtinctionLerp > 0.01f) {
                    float redPulse = MathF.Sin(extinctionFlashTimer * 3f + star.BrightnessPhase) * 0.25f + 0.75f;
                    Color extinctionRed = new Color(255, 50, 30);
                    starColor = Color.Lerp(starColor, extinctionRed, star.ExtinctionLerp * redPulse);
                    //被标记的恒星略微增大以强调
                    finalBrightness *= MathHelper.Lerp(1f, 1.3f, star.ExtinctionLerp);
                }
                starColor *= finalBrightness;

                //绘制恒星光点
                float size = star.Size;
                sb.Draw(pixel, screenPos, new Rectangle(0, 0, 1, 1),
                    starColor, 0f, new Vector2(0.5f), new Vector2(size), SpriteEffects.None, 0f);

                //较亮的恒星用SoftGlow绘制柔和光晕
                if (star.Brightness > 0.6f && softGlow != null) {
                    Color glowColor = starColor * 0.25f;
                    glowColor.A = 0;
                    float glowScale = size * 0.12f;
                    sb.Draw(softGlow, screenPos, null,
                        glowColor, 0f, new Vector2(softGlow.Width * 0.5f, softGlow.Height * 0.5f),
                        glowScale, SpriteEffects.None, 0f);
                } else if (star.Brightness > 0.7f) {
                    Color glowColor = starColor * 0.3f;
                    glowColor.A = 0;
                    sb.Draw(pixel, screenPos, new Rectangle(0, 0, 1, 1),
                        glowColor, 0f, new Vector2(0.5f), new Vector2(size * 3f), SpriteEffects.None, 0f);
                }
            }
        }

        private static void DrawGalaxyCore(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D softGlow = CWRAsset.SoftGlow?.Value;

            //使用SoftGlow绘制银核发光
            if (softGlow != null) {
                Color coreColor = new Color(255, 240, 200);
                Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);

                for (int i = 4; i >= 0; i--) {
                    float layerScale = GalaxyCoreRadius * (0.02f + i * 0.015f);
                    float layerAlpha = alpha * (0.35f - i * 0.05f);
                    float pulse = MathF.Sin(globalTimer * 1.5f + i * 0.5f) * 0.12f + 0.88f;
                    layerAlpha *= pulse;

                    Color layerColor = Color.Lerp(coreColor, new Color(180, 210, 255), i * 0.2f);
                    layerColor.A = 0;

                    sb.Draw(softGlow, center, null, layerColor * layerAlpha, 0f,
                        glowOrigin, layerScale, SpriteEffects.None, 0f);
                }
            } else {
                //后备方案：像素矩形近似
                Texture2D pixel = VaultAsset.placeholder2.Value;
                if (pixel == null) return;
                Color coreColor = new Color(255, 240, 200);
                for (int i = 5; i >= 0; i--) {
                    float layerRadius = GalaxyCoreRadius * (0.3f + i * 0.3f);
                    float layerAlpha = alpha * (0.4f - i * 0.05f);
                    float pulse = MathF.Sin(globalTimer * 1.5f + i * 0.5f) * 0.1f + 0.9f;
                    layerAlpha *= pulse;
                    Color layerColor = Color.Lerp(coreColor, new Color(180, 200, 255), i * 0.15f);
                    layerColor.A = 0;
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
            float swarmCenterAngle = -MathHelper.PiOver4;
            float swarmDistance = GalaxyRadius * MathHelper.Lerp(1.6f, 0.9f, swarmApproachProgress);
            float pulse = MathF.Sin(swarmPulseTimer * 2f) * 0.3f + 0.7f;

            Texture2D softGlow = CWRAsset.SoftGlow?.Value;

            //使用SoftGlow在虫群前沿绘制暗红色脉动光斑
            if (softGlow != null) {
                int glowCount = 8;
                float arcSpread = MathHelper.ToRadians(80f);
                Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);

                for (int i = 0; i < glowCount; i++) {
                    float t = i / (float)glowCount;
                    float angle = swarmCenterAngle - arcSpread / 2f + arcSpread * t;
                    Vector2 glowPos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (swarmDistance - 5f);

                    Color redGlow = new Color(180, 25, 35);
                    redGlow.A = 0;
                    float glowAlpha = alpha * 0.35f * pulse * MathF.Sin(t * MathHelper.Pi);
                    float glowScale = 0.4f + MathF.Sin(swarmPulseTimer * 3f + t * 5f) * 0.08f;

                    sb.Draw(softGlow, glowPos, null, redGlow * glowAlpha, 0f,
                        glowOrigin, glowScale, SpriteEffects.None, 0f);
                }
            } else {
                //后备方案
                Texture2D pixel = VaultAsset.placeholder2.Value;
                if (pixel == null) return;
                int arcSegments = 20;
                float arcSpread = MathHelper.ToRadians(80f);
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
        }

        #endregion

        #region 灭绝令效果

        private static void DrawExtinctionOverlay(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            float flash = MathF.Sin(extinctionFlashTimer) * 0.3f + 0.7f;

            //绘制灭绝令扩展波纹（从外环向内的红色脉冲圆环）
            if (extinctionWaveRadius > 5f) {
                Texture2D softGlow = CWRAsset.SoftGlow?.Value;
                float waveRingRadius = GalaxyRadius - extinctionWaveRadius;
                if (waveRingRadius > 0f && waveRingRadius < GalaxyRadius) {
                    //波纹前沿用SoftGlow绘制脉动光点
                    int wavePoints = 24;
                    float wavePulse = MathF.Sin(extinctionFlashTimer * 4f) * 0.3f + 0.7f;
                    for (int i = 0; i < wavePoints; i++) {
                        float angle = MathHelper.TwoPi * i / wavePoints;
                        Vector2 pos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * waveRingRadius;

                        if (softGlow != null) {
                            Color waveColor = new Color(255, 40, 20);
                            waveColor.A = 0;
                            float waveAlpha = alpha * 0.2f * wavePulse;
                            Vector2 origin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);
                            sb.Draw(softGlow, pos, null, waveColor * waveAlpha, 0f,
                                origin, 0.2f, SpriteEffects.None, 0f);
                        } else {
                            Color dotColor = new Color(255, 50, 30) * (alpha * 0.3f * wavePulse);
                            dotColor.A = 0;
                            sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1),
                                dotColor, angle, new Vector2(0.5f),
                                new Vector2(8f, 3f), SpriteEffects.None, 0f);
                        }
                    }
                }
            }

            //灭绝令警告文本（持续显示在面板底部区域）
            if (extinctionProgress > 0.05f) {
                Rectangle panelRect = GetPanelRect();
                float textFade = extinctionProgress < 0.3f
                    ? extinctionProgress / 0.3f
                    : 1f;
                float textAlpha = alpha * flash * textFade;
                string warningText = "◢ EXTINCTION PROTOCOL ACTIVE ◣";
                var font = FontAssets.MouseText.Value;
                Vector2 textSize = font.MeasureString(warningText) * 0.55f;
                Vector2 textPos = new(
                    panelRect.X + (panelRect.Width - textSize.X) * 0.5f,
                    panelRect.Bottom - 30f
                );

                //红色警告背景条
                Rectangle warnBg = new(panelRect.X + 6, panelRect.Bottom - 36, panelRect.Width - 12, 26);
                sb.Draw(pixel, warnBg, new Rectangle(0, 0, 1, 1), new Color(80, 10, 10) * (textAlpha * 0.5f));

                //发光底色
                for (int g = 0; g < 4; g++) {
                    float gAngle = MathHelper.TwoPi * g / 4f;
                    Vector2 gOffset = new Vector2(MathF.Cos(gAngle), MathF.Sin(gAngle)) * 1.2f;
                    Utils.DrawBorderString(sb, warningText, textPos + gOffset,
                        new Color(200, 50, 30) * (textAlpha * 0.4f), 0.55f);
                }
                Utils.DrawBorderString(sb, warningText, textPos, Color.White * textAlpha, 0.55f);
            }
        }

        #endregion

        #region 泰拉标记

        private static void DrawTerraMarker(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            //泰拉位于银河系中心附近
            Vector2 terraPos = center + new Vector2(3f, 2f);
            float blink = MathF.Sin(terraBlinkTimer) * 0.3f + 0.7f;
            float markerAlpha = alpha * blink;

            //根据灭绝令状态决定颜色
            bool inDanger = extinctionProgress > 0.5f;
            Color terraColor = inDanger
                ? Color.Lerp(new Color(100, 255, 120), new Color(255, 60, 40),
                    MathF.Sin(extinctionFlashTimer * 2f) * 0.5f + 0.5f)
                : new Color(100, 255, 120);

            Texture2D softGlow = CWRAsset.SoftGlow?.Value;

            //使用SoftGlow绘制泰拉标记光斑
            if (softGlow != null) {
                Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);

                //外层呼吸光环
                float ringPulse = (MathF.Sin(terraBlinkTimer * 0.7f) + 1f) * 0.5f;
                Color ringGlow = terraColor;
                ringGlow.A = 0;
                float ringScale = 0.22f + ringPulse * 0.12f;
                sb.Draw(softGlow, terraPos, null,
                    ringGlow * (markerAlpha * 0.3f * (1f - ringPulse * 0.3f)),
                    0f, glowOrigin, ringScale, SpriteEffects.None, 0f);

                //内核亮点
                Color coreGlow = terraColor;
                coreGlow.A = 0;
                sb.Draw(softGlow, terraPos, null,
                    coreGlow * (markerAlpha * 0.7f),
                    0f, glowOrigin, 0.08f, SpriteEffects.None, 0f);

                //灭绝令危险时叠加红色大光斑
                if (inDanger) {
                    float dangerPulse = MathF.Sin(extinctionFlashTimer * 3f) * 0.4f + 0.6f;
                    Color dangerGlow = new Color(255, 40, 20);
                    dangerGlow.A = 0;
                    sb.Draw(softGlow, terraPos, null,
                        dangerGlow * (alpha * 0.25f * dangerPulse),
                        0f, glowOrigin, 0.35f, SpriteEffects.None, 0f);
                }
            } else {
                //后备方案
                sb.Draw(pixel, terraPos, new Rectangle(0, 0, 1, 1),
                    terraColor * markerAlpha, 0f, new Vector2(0.5f),
                    new Vector2(4f), SpriteEffects.None, 0f);

                if (inDanger) {
                    Color dangerColor = new Color(255, 60, 40);
                    dangerColor.A = 0;
                    float dangerAlpha = alpha * MathF.Sin(extinctionFlashTimer * 2f) * 0.5f + 0.5f;
                    sb.Draw(pixel, terraPos, new Rectangle(0, 0, 1, 1),
                        dangerColor * dangerAlpha, 0f, new Vector2(0.5f),
                        new Vector2(8f), SpriteEffects.None, 0f);
                }
            }

            //标注文字"TERRA"
            float textAlpha = alpha * 0.7f;
            Utils.DrawBorderString(sb, "TERRA", terraPos + new Vector2(12, -10),
                terraColor * textAlpha, 0.45f);
        }

        #endregion

        #region 角落装饰

        private static void DrawCornerDecor(SpriteBatch sb, Vector2 pos, Color color, float size, float rotation) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            //主L形线
            sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), color, rotation,
                new Vector2(0.5f), new Vector2(size, size * 0.2f), SpriteEffects.None, 0f);
            sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), color * 0.7f, rotation + MathHelper.PiOver2,
                new Vector2(0.5f), new Vector2(size, size * 0.2f), SpriteEffects.None, 0f);

            //内侧次级短线
            sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), color * 0.4f, rotation,
                new Vector2(0.5f), new Vector2(size * 0.5f, size * 0.12f), SpriteEffects.None, 0f);
        }

        #endregion

        #region 扫描线和干扰

        private static void DrawScanLineEffect(SpriteBatch sb, Rectangle panelRect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            //主扫描线在面板范围内移动
            float scanY = panelRect.Y + 38f + scanLineProgress * (panelRect.Height - 42f);

            Color scanColor = new Color(60, 180, 255) * (alpha * 0.2f);
            sb.Draw(pixel, new Vector2(panelRect.X + 6, scanY), new Rectangle(0, 0, 1, 1),
                scanColor, 0f, Vector2.Zero, new Vector2(panelRect.Width - 12, 2f), SpriteEffects.None, 0f);

            //辅助扫描线
            float scan2 = (scanLineProgress + 0.5f) % 1f;
            float scan2Y = panelRect.Y + 38f + scan2 * (panelRect.Height - 42f);
            sb.Draw(pixel, new Vector2(panelRect.X + 6, scan2Y), new Rectangle(0, 0, 1, 1),
                scanColor * 0.4f, 0f, Vector2.Zero, new Vector2(panelRect.Width - 12, 1f), SpriteEffects.None, 0f);
        }

        private static void DrawGlitchNoise(SpriteBatch sb, Rectangle panelRect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            int noiseCount = (int)(glitchIntensity * 50f);

            for (int i = 0; i < noiseCount; i++) {
                float x = panelRect.X + Main.rand.NextFloat(panelRect.Width);
                float y = panelRect.Y + Main.rand.NextFloat(panelRect.Height);
                float w = Main.rand.NextFloat(5f, 50f);
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
