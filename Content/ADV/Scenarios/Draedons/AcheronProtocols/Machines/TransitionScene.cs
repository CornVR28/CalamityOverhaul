using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines
{
    /// <summary>
    /// 科幻风格过渡加载界面，灵感来自星际战士2的加载画面
    /// 全屏覆盖，带进度条、动态粒子、电路脉冲、数据流和底部滚动提示文字
    /// </summary>
    internal class TransitionScene : UIHandle, ILocalizedModType
    {
        public override LayersModeEnum LayersMode => LayersModeEnum.Mod_MenuLoad;
        public override float RenderPriority => 2;
        public string LocalizationCategory => "UI";

        public static TransitionScene Instance => UIHandleLoader.GetUIHandleOfType<TransitionScene>();

        #region 本地化

        public static LocalizedText LoadingText { get; private set; }
        public static LocalizedText InitializingText { get; private set; }
        public static LocalizedText[] HintTexts { get; private set; }

        #endregion

        #region 状态控制

        private bool _active;
        public override bool Active => _active || fadeAlpha > 0.001f;

        private float fadeAlpha;
        private bool isFadingIn;
        private bool isFadingOut;

        /// <summary>
        /// 当前加载进度，0~1
        /// </summary>
        private float currentProgress;

        /// <summary>
        /// 目标进度（可由外部设置）
        /// </summary>
        private float targetProgress;

        /// <summary>
        /// 假进度模式下的自动递增速度
        /// </summary>
        private bool fakeProgressMode = true;
        private float fakeProgressSpeed = 0.003f;

        #endregion

        #region 动画计时器

        private float globalTime;
        private float scanLineTimer;
        private float circuitPulseTimer;
        private float dataStreamTimer;
        private float hexGridPhase;
        private float hologramFlicker;
        private float progressGlowPhase;
        private float warningPulse;

        #endregion

        #region 粒子系统

        private readonly List<TechParticle> particles = [];
        private int particleSpawnTimer;
        private readonly List<DataStreamLine> dataStreams = [];
        private int dataStreamSpawnTimer;
        private readonly List<CircuitNode> circuitNodes = [];
        private int circuitNodeSpawnTimer;
        private readonly List<HexCell> hexCells = [];
        private int hexCellSpawnTimer;

        #endregion

        #region 提示文字系统

        private int currentHintIndex;
        private float hintFadeProgress;
        private float hintDisplayTimer;
        private const float HintDisplayDuration = 300f;
        private const float HintFadeDuration = 40f;
        private enum HintState { FadeIn, Display, FadeOut }
        private HintState hintState = HintState.FadeIn;

        #endregion

        #region 进度条布局

        private const float ProgressBarWidth = 500f;
        private const float ProgressBarHeight = 6f;
        private const float ProgressBarY = 0.72f;

        #endregion

        #region 装饰几何元素

        private float aquilaRotation;
        private float outerRingRotation;
        private float innerRingRotation;

        #endregion

#if DEBUG
        private Rectangle debugCloseButtonRect;
        private bool debugCloseHovering;
        private float debugCloseHoverAnim;
#endif

        public override void SetStaticDefaults() {
            LoadingText = this.GetLocalization(nameof(LoadingText), () => "数据链接中");
            InitializingText = this.GetLocalization(nameof(InitializingText), () => "正在初始化协议");

            HintTexts = [
                this.GetLocalization("Hint0", () => "不要问为什么杀异形，而是要问为何不杀"),
                this.GetLocalization("Hint1", () => "阿切隆协议是嘉登最高级别的应急方案之一"),
                this.GetLocalization("Hint2", () => "战术人形无所畏惧，因为恐惧本身就是他们的武器"),
                this.GetLocalization("Hint3", () => "嘉登的机械造物已经超越了现实的极限"),
                this.GetLocalization("Hint4", () => "没有大到无法接受的牺牲，没有小到可以原谅的背叛"),
                this.GetLocalization("Hint5", () => "任何足够先进的技术，皆与魔法无异"),
                this.GetLocalization("Hint6", () => "灾厄之中蕴藏着超越一切的可能性"),
                this.GetLocalization("Hint7", () => "嘉登从不做无意义的事，每一步都是精密计算的结果"),
                this.GetLocalization("Hint8", () => "数据即是力量，知识即是武装"),
                this.GetLocalization("Hint9", () => "机械不会背叛，但操控机械的人或许会"),
            ];
        }

        #region 公开接口

        /// <summary>
        /// 显示加载界面
        /// </summary>
        /// <param name="useFakeProgress">是否使用假进度</param>
        public static void Show(bool useFakeProgress = true) {
            var inst = Instance;
            if (inst == null) return;
            inst._active = true;
            inst.isFadingIn = true;
            inst.isFadingOut = false;
            inst.fakeProgressMode = useFakeProgress;
            inst.currentProgress = 0f;
            inst.targetProgress = 0f;
            inst.ResetAnimations();
        }

        /// <summary>
        /// 隐藏加载界面（淡出）
        /// </summary>
        public static void Hide() {
            var inst = Instance;
            if (inst == null) return;
            inst.isFadingOut = true;
            inst.isFadingIn = false;
        }

        /// <summary>
        /// 设置目标进度（非假进度模式下使用）
        /// </summary>
        public static void SetProgress(float progress) {
            var inst = Instance;
            if (inst == null) return;
            inst.fakeProgressMode = false;
            inst.targetProgress = Math.Clamp(progress, 0f, 1f);
        }

        /// <summary>
        /// 检查加载界面是否正在显示
        /// </summary>
        public static bool IsShowing => Instance?._active ?? false;

        #endregion

        private void ResetAnimations() {
            globalTime = 0f;
            scanLineTimer = 0f;
            circuitPulseTimer = 0f;
            dataStreamTimer = 0f;
            hexGridPhase = 0f;
            hologramFlicker = 0f;
            progressGlowPhase = 0f;
            warningPulse = 0f;
            particles.Clear();
            dataStreams.Clear();
            circuitNodes.Clear();
            hexCells.Clear();
            particleSpawnTimer = 0;
            dataStreamSpawnTimer = 0;
            circuitNodeSpawnTimer = 0;
            hexCellSpawnTimer = 0;
            currentHintIndex = Main.rand?.Next(HintTexts?.Length ?? 1) ?? 0;
            hintFadeProgress = 0f;
            hintDisplayTimer = 0f;
            hintState = HintState.FadeIn;
            aquilaRotation = 0f;
            outerRingRotation = 0f;
            innerRingRotation = 0f;
        }

        public override void Update() {
            float dt = 0.016f;
            globalTime += dt;

            //淡入淡出
            if (isFadingIn) {
                fadeAlpha += 0.04f;
                if (fadeAlpha >= 1f) {
                    fadeAlpha = 1f;
                    isFadingIn = false;
                }
            }

            if (isFadingOut) {
                fadeAlpha -= 0.03f;
                if (fadeAlpha <= 0f) {
                    fadeAlpha = 0f;
                    isFadingOut = false;
                    _active = false;
                    ResetAnimations();
                    return;
                }
            }

            if (fadeAlpha <= 0.001f) return;

            //动画计时器
            scanLineTimer += 0.035f;
            circuitPulseTimer += 0.02f;
            dataStreamTimer += 0.04f;
            hexGridPhase += 0.012f;
            hologramFlicker += 0.08f;
            progressGlowPhase += 0.06f;
            warningPulse += 0.05f;

            if (scanLineTimer > MathHelper.TwoPi) scanLineTimer -= MathHelper.TwoPi;
            if (circuitPulseTimer > MathHelper.TwoPi) circuitPulseTimer -= MathHelper.TwoPi;
            if (dataStreamTimer > MathHelper.TwoPi) dataStreamTimer -= MathHelper.TwoPi;
            if (hexGridPhase > MathHelper.TwoPi) hexGridPhase -= MathHelper.TwoPi;
            if (hologramFlicker > MathHelper.TwoPi) hologramFlicker -= MathHelper.TwoPi;

            //装饰元素旋转
            aquilaRotation += 0.002f;
            outerRingRotation += 0.008f;
            innerRingRotation -= 0.012f;

            //进度更新
            if (fakeProgressMode) {
                //假进度：缓慢递增，到90%减速，永远不会自行到100%
                float speedMod = currentProgress < 0.6f ? 1f : (currentProgress < 0.85f ? 0.4f : 0.08f);
                currentProgress += fakeProgressSpeed * speedMod;
                currentProgress = Math.Min(currentProgress, 0.98f);
            }
            else {
                //真进度：平滑插值到目标值
                currentProgress += (targetProgress - currentProgress) * 0.08f;
                if (Math.Abs(currentProgress - targetProgress) < 0.001f)
                    currentProgress = targetProgress;
            }

            //更新粒子
            UpdateParticles();

            //更新提示文字
            UpdateHintText();

#if DEBUG
            //调试关闭按钮
            int btnW = 80, btnH = 28;
            debugCloseButtonRect = new Rectangle(Main.screenWidth - btnW - 16, 16, btnW, btnH);
            debugCloseHovering = debugCloseButtonRect.Contains(Main.mouseX, Main.mouseY);
            debugCloseHoverAnim = MathHelper.Lerp(debugCloseHoverAnim, debugCloseHovering ? 1f : 0f, 0.15f);
            if (debugCloseHovering && Main.mouseLeft && Main.mouseLeftRelease) {
                Hide();
            }
#endif
        }

        private void UpdateParticles() {
            //通用科技粒子
            particleSpawnTimer++;
            if (particleSpawnTimer >= 3 && particles.Count < 60) {
                particleSpawnTimer = 0;
                SpawnTechParticle();
            }
            for (int i = particles.Count - 1; i >= 0; i--) {
                if (particles[i].Update()) particles.RemoveAt(i);
            }

            //数据流线条
            dataStreamSpawnTimer++;
            if (dataStreamSpawnTimer >= 12 && dataStreams.Count < 20) {
                dataStreamSpawnTimer = 0;
                SpawnDataStream();
            }
            for (int i = dataStreams.Count - 1; i >= 0; i--) {
                if (dataStreams[i].Update()) dataStreams.RemoveAt(i);
            }

            //电路节点
            circuitNodeSpawnTimer++;
            if (circuitNodeSpawnTimer >= 30 && circuitNodes.Count < 10) {
                circuitNodeSpawnTimer = 0;
                SpawnCircuitNode();
            }
            for (int i = circuitNodes.Count - 1; i >= 0; i--) {
                if (circuitNodes[i].Update()) circuitNodes.RemoveAt(i);
            }

            //六边形网格高亮
            hexCellSpawnTimer++;
            if (hexCellSpawnTimer >= 8 && hexCells.Count < 25) {
                hexCellSpawnTimer = 0;
                SpawnHexCell();
            }
            for (int i = hexCells.Count - 1; i >= 0; i--) {
                if (hexCells[i].Update()) hexCells.RemoveAt(i);
            }
        }

        private void SpawnTechParticle() {
            int sw = Main.screenWidth;
            int sh = Main.screenHeight;
            //从屏幕边缘或中心区域生成
            Vector2 pos;
            Vector2 vel;
            int spawnType = Main.rand.Next(3);
            if (spawnType == 0) {
                //从底部上升
                pos = new Vector2(Main.rand.NextFloat(sw), sh + 10);
                vel = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-2f, -0.8f));
            }
            else if (spawnType == 1) {
                //从侧面飘入
                bool left = Main.rand.NextBool();
                pos = new Vector2(left ? -10 : sw + 10, Main.rand.NextFloat(sh));
                vel = new Vector2(left ? Main.rand.NextFloat(0.5f, 1.5f) : Main.rand.NextFloat(-1.5f, -0.5f), Main.rand.NextFloat(-0.5f, 0.5f));
            }
            else {
                //在屏幕中生成
                pos = new Vector2(Main.rand.NextFloat(sw * 0.1f, sw * 0.9f), Main.rand.NextFloat(sh * 0.1f, sh * 0.9f));
                vel = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(0.2f, 0.8f);
            }

            Color baseColor = Main.rand.Next(4) switch {
                0 => new Color(60, 180, 255),
                1 => new Color(80, 220, 255),
                2 => new Color(40, 140, 200),
                _ => new Color(100, 200, 255)
            };

            float life = Main.rand.NextFloat(2f, 5f);
            particles.Add(new TechParticle {
                Position = pos,
                Velocity = vel,
                Life = life,
                MaxLife = life,
                Size = Main.rand.NextFloat(1.5f, 4f),
                Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                RotationSpeed = Main.rand.NextFloat(-0.03f, 0.03f),
                BaseColor = baseColor
            });
        }

        private void SpawnDataStream() {
            int sw = Main.screenWidth;
            int sh = Main.screenHeight;
            bool horizontal = Main.rand.NextBool();
            Vector2 start, end;
            if (horizontal) {
                float y = Main.rand.NextFloat(sh * 0.05f, sh * 0.95f);
                start = new Vector2(-20, y);
                end = new Vector2(sw + 20, y + Main.rand.NextFloat(-30, 30));
            }
            else {
                float x = Main.rand.NextFloat(sw * 0.05f, sw * 0.95f);
                start = new Vector2(x, -20);
                end = new Vector2(x + Main.rand.NextFloat(-30, 30), sh + 20);
            }

            float dsLife = Main.rand.NextFloat(1f, 2.5f);
            dataStreams.Add(new DataStreamLine {
                Start = start,
                End = end,
                Life = dsLife,
                MaxLife = dsLife,
                Progress = 0f,
                Speed = Main.rand.NextFloat(0.01f, 0.03f),
                Thickness = Main.rand.NextFloat(1f, 2.5f),
                LineColor = new Color(60, 180, 255) * Main.rand.NextFloat(0.15f, 0.4f)
            });
        }

        private void SpawnCircuitNode() {
            int sw = Main.screenWidth;
            int sh = Main.screenHeight;
            Vector2 pos = new(Main.rand.NextFloat(sw * 0.05f, sw * 0.95f), Main.rand.NextFloat(sh * 0.05f, sh * 0.95f));
            int branchCount = Main.rand.Next(2, 5);
            var branches = new List<Vector2>();
            for (int i = 0; i < branchCount; i++) {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float len = Main.rand.NextFloat(40, 120);
                //直角化：只水平或垂直延伸
                if (Main.rand.NextBool()) {
                    branches.Add(pos + new Vector2(Main.rand.NextBool() ? len : -len, 0));
                }
                else {
                    branches.Add(pos + new Vector2(0, Main.rand.NextBool() ? len : -len));
                }
            }
            float cnLife = Main.rand.NextFloat(2f, 4f);
            circuitNodes.Add(new CircuitNode {
                Center = pos,
                Branches = branches,
                Life = cnLife,
                MaxLife = cnLife,
                PulsePhase = Main.rand.NextFloat(MathHelper.TwoPi)
            });
        }

        private void SpawnHexCell() {
            int sw = Main.screenWidth;
            int sh = Main.screenHeight;
            float hcLife = Main.rand.NextFloat(1.5f, 3.5f);
            hexCells.Add(new HexCell {
                Position = new Vector2(Main.rand.NextFloat(sw), Main.rand.NextFloat(sh)),
                Size = Main.rand.NextFloat(20f, 50f),
                Life = hcLife,
                MaxLife = hcLife,
                Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                PulsePhase = Main.rand.NextFloat(MathHelper.TwoPi)
            });
        }

        private void UpdateHintText() {
            if (HintTexts == null || HintTexts.Length == 0) return;

            switch (hintState) {
                case HintState.FadeIn:
                    hintFadeProgress += 1f / HintFadeDuration;
                    if (hintFadeProgress >= 1f) {
                        hintFadeProgress = 1f;
                        hintState = HintState.Display;
                        hintDisplayTimer = 0f;
                    }
                    break;
                case HintState.Display:
                    hintDisplayTimer++;
                    if (hintDisplayTimer >= HintDisplayDuration) {
                        hintState = HintState.FadeOut;
                    }
                    break;
                case HintState.FadeOut:
                    hintFadeProgress -= 1f / HintFadeDuration;
                    if (hintFadeProgress <= 0f) {
                        hintFadeProgress = 0f;
                        currentHintIndex = (currentHintIndex + 1) % HintTexts.Length;
                        hintState = HintState.FadeIn;
                    }
                    break;
            }
        }

        #region 绘制

        public override void Draw(SpriteBatch spriteBatch) {
            if (fadeAlpha <= 0.001f) return;

            float alpha = fadeAlpha;
            int sw = Main.screenWidth;
            int sh = Main.screenHeight;
            Texture2D pixel = VaultAsset.placeholder2.Value;

            //全屏深色背景
            DrawBackground(spriteBatch, pixel, sw, sh, alpha);

            //六边形网格闪烁
            DrawHexCells(spriteBatch, pixel, alpha);

            //电路节点
            DrawCircuitNodes(spriteBatch, pixel, alpha);

            //数据流线条
            DrawDataStreams(spriteBatch, pixel, alpha);

            //扫描线
            DrawScanLines(spriteBatch, pixel, sw, sh, alpha);

            //中心装饰图案（类鹰徽/齿轮环）
            DrawCenterEmblem(spriteBatch, pixel, sw, sh, alpha);

            //粒子
            DrawParticles(spriteBatch, pixel, alpha);

            //进度条
            DrawProgressBar(spriteBatch, pixel, sw, sh, alpha);

            //加载状态文字
            DrawLoadingText(spriteBatch, sw, sh, alpha);

            //底部提示文字
            DrawHintText(spriteBatch, sw, sh, alpha);

            //全息闪烁叠加
            DrawHologramOverlay(spriteBatch, pixel, sw, sh, alpha);

            //四角装饰
            DrawCornerDecorations(spriteBatch, pixel, sw, sh, alpha);

            //顶部和底部边框线
            DrawEdgeBorders(spriteBatch, pixel, sw, sh, alpha);

#if DEBUG
            //调试关闭按钮
            Color dbgBg = Color.Lerp(new Color(20, 40, 60), new Color(40, 80, 120), debugCloseHoverAnim) * (alpha * 0.9f);
            spriteBatch.Draw(pixel, debugCloseButtonRect, new Rectangle(0, 0, 1, 1), dbgBg);
            DrawRectBorder(spriteBatch, pixel, debugCloseButtonRect, new Color(80, 200, 255) * (alpha * (0.5f + debugCloseHoverAnim * 0.3f)), 1);
            Vector2 dbgTextSize = FontAssets.MouseText.Value.MeasureString("[X] Close") * 0.7f;
            Vector2 dbgTextPos = new(debugCloseButtonRect.X + (debugCloseButtonRect.Width - dbgTextSize.X) / 2f,
                debugCloseButtonRect.Y + (debugCloseButtonRect.Height - dbgTextSize.Y) / 2f);
            Utils.DrawBorderString(spriteBatch, "[X] Close", dbgTextPos,
                Color.Lerp(new Color(150, 200, 230), new Color(220, 240, 255), debugCloseHoverAnim) * alpha, 0.7f);
#endif
        }

        private void DrawBackground(SpriteBatch sb, Texture2D pixel, int sw, int sh, float alpha) {
            //渐变背景：从深蓝黑到稍亮的深蓝
            int segments = 40;
            for (int i = 0; i < segments; i++) {
                float t = i / (float)segments;
                int y1 = (int)(t * sh);
                int y2 = (int)((i + 1f) / segments * sh);

                Color top = new Color(4, 6, 14);
                Color mid = new Color(8, 14, 28);
                Color bottom = new Color(5, 8, 18);

                Color c;
                if (t < 0.5f) {
                    c = Color.Lerp(top, mid, t * 2f);
                }
                else {
                    c = Color.Lerp(mid, bottom, (t - 0.5f) * 2f);
                }

                //加入微弱的脉动
                float pulse = MathF.Sin(circuitPulseTimer * 0.5f + t * 2f) * 0.5f + 0.5f;
                c = Color.Lerp(c, new Color(10, 20, 40), pulse * 0.08f);

                sb.Draw(pixel, new Rectangle(0, y1, sw, Math.Max(1, y2 - y1)), new Rectangle(0, 0, 1, 1), c * alpha);
            }

            //极暗的横向噪波条纹
            for (int i = 0; i < sh; i += 4) {
                float noise = MathF.Sin(i * 0.5f + globalTime * 3f) * 0.5f + 0.5f;
                if (noise > 0.7f) {
                    sb.Draw(pixel, new Rectangle(0, i, sw, 1), new Rectangle(0, 0, 1, 1),
                        new Color(20, 40, 70) * (alpha * 0.03f * noise));
                }
            }
        }

        private void DrawHexCells(SpriteBatch sb, Texture2D pixel, float alpha) {
            foreach (var hex in hexCells) {
                float lifeRatio = hex.Life / hex.MaxLife;
                float fadeIn = Math.Min(lifeRatio * 4f, 1f);
                float fadeOut = Math.Min((1f - lifeRatio) * 3f, 1f);
                float cellAlpha = fadeIn * fadeOut * alpha * 0.25f;

                float pulse = MathF.Sin(hex.PulsePhase + globalTime * 2f) * 0.5f + 0.5f;
                Color c = new Color(30, 100, 180) * (cellAlpha * (0.3f + pulse * 0.7f));

                //绘制六边形轮廓（用6条线段近似）
                int sides = 6;
                for (int i = 0; i < sides; i++) {
                    float a1 = hex.Rotation + MathHelper.TwoPi * i / sides;
                    float a2 = hex.Rotation + MathHelper.TwoPi * (i + 1) / sides;
                    Vector2 p1 = hex.Position + a1.ToRotationVector2() * hex.Size;
                    Vector2 p2 = hex.Position + a2.ToRotationVector2() * hex.Size;
                    DrawLine(sb, pixel, p1, p2, c, 1f);
                }
            }
        }

        private void DrawCircuitNodes(SpriteBatch sb, Texture2D pixel, float alpha) {
            foreach (var node in circuitNodes) {
                float lifeRatio = node.Life / node.MaxLife;
                float fadeIn = Math.Min(lifeRatio * 3f, 1f);
                float fadeOut = Math.Min((1f - lifeRatio) * 3f, 1f);
                float nodeAlpha = fadeIn * fadeOut * alpha;

                float pulse = MathF.Sin(node.PulsePhase + globalTime * 3f) * 0.5f + 0.5f;
                Color lineColor = new Color(50, 160, 240) * (nodeAlpha * 0.35f * (0.4f + pulse * 0.6f));
                Color nodeColor = new Color(80, 220, 255) * (nodeAlpha * 0.6f);

                //中心节点
                sb.Draw(pixel, node.Center, new Rectangle(0, 0, 1, 1), nodeColor,
                    0f, new Vector2(0.5f), new Vector2(4f), SpriteEffects.None, 0f);

                //分支线条（带脉冲点）
                foreach (var branch in node.Branches) {
                    DrawLine(sb, pixel, node.Center, branch, lineColor, 1.5f);

                    //脉冲光点沿分支移动
                    float pulsePos = (globalTime * 0.8f + node.PulsePhase) % 1f;
                    Vector2 pulsePoint = Vector2.Lerp(node.Center, branch, pulsePos);
                    sb.Draw(pixel, pulsePoint, new Rectangle(0, 0, 1, 1),
                        new Color(100, 220, 255) * (nodeAlpha * 0.8f * pulse),
                        0f, new Vector2(0.5f), new Vector2(3f), SpriteEffects.None, 0f);

                    //分支末端节点
                    sb.Draw(pixel, branch, new Rectangle(0, 0, 1, 1),
                        nodeColor * 0.5f, 0f, new Vector2(0.5f), new Vector2(2.5f), SpriteEffects.None, 0f);
                }
            }
        }

        private void DrawDataStreams(SpriteBatch sb, Texture2D pixel, float alpha) {
            foreach (var stream in dataStreams) {
                float lifeRatio = stream.Life / stream.MaxLife;
                float streamAlpha = lifeRatio * alpha;

                //流动的发光线段
                float headPos = stream.Progress;
                float tailPos = Math.Max(0, headPos - 0.15f);

                Vector2 dir = stream.End - stream.Start;
                Vector2 headPoint = stream.Start + dir * headPos;
                Vector2 tailPoint = stream.Start + dir * tailPos;

                Color headColor = stream.LineColor * streamAlpha;
                Color tailColor = stream.LineColor * (streamAlpha * 0.1f);

                //主线（暗底）
                DrawLine(sb, pixel, stream.Start, stream.End, stream.LineColor * (streamAlpha * 0.08f), stream.Thickness * 0.5f);

                //亮头
                DrawLine(sb, pixel, tailPoint, headPoint, headColor, stream.Thickness);

                //发光头部
                sb.Draw(pixel, headPoint, new Rectangle(0, 0, 1, 1),
                    new Color(100, 220, 255, 0) * streamAlpha,
                    0f, new Vector2(0.5f), new Vector2(stream.Thickness * 3f), SpriteEffects.None, 0f);
            }
        }

        private void DrawScanLines(SpriteBatch sb, Texture2D pixel, int sw, int sh, float alpha) {
            //主扫描线
            float scanY = sh * 0.5f + MathF.Sin(scanLineTimer) * sh * 0.4f;
            for (int i = -3; i <= 3; i++) {
                float y = scanY + i * 2.5f;
                if (y < 0 || y > sh) continue;
                float intensity = 1f - Math.Abs(i) / 4f;
                Color scanColor = new Color(60, 180, 255) * (alpha * 0.08f * intensity);
                sb.Draw(pixel, new Rectangle(0, (int)y, sw, 2), new Rectangle(0, 0, 1, 1), scanColor);
            }

            //次级扫描线（从右到左）
            float scanX = sw * (1f - ((scanLineTimer * 0.3f) % 1f));
            for (int i = -2; i <= 2; i++) {
                float x = scanX + i * 2f;
                if (x < 0 || x > sw) continue;
                float intensity = 1f - Math.Abs(i) / 3f;
                Color scanColor = new Color(40, 140, 200) * (alpha * 0.04f * intensity);
                sb.Draw(pixel, new Rectangle((int)x, 0, 2, sh), new Rectangle(0, 0, 1, 1), scanColor);
            }

            //CRT 栅格线
            for (int y = 0; y < sh; y += 3) {
                sb.Draw(pixel, new Rectangle(0, y, sw, 1), new Rectangle(0, 0, 1, 1),
                    Color.Black * (alpha * 0.06f));
            }
        }

        private void DrawCenterEmblem(SpriteBatch sb, Texture2D pixel, int sw, int sh, float alpha) {
            Vector2 center = new(sw * 0.5f, sh * 0.42f);
            float baseAlpha = alpha * 0.4f;
            float pulse = MathF.Sin(circuitPulseTimer * 1.5f) * 0.5f + 0.5f;

            //外环（大齿轮样式）
            float outerRadius = 110f;
            int outerSegments = 64;
            int teeth = 16;
            Color outerColor = new Color(40, 120, 200) * (baseAlpha * (0.3f + pulse * 0.3f));
            for (int i = 0; i < outerSegments; i++) {
                float a1 = outerRingRotation + MathHelper.TwoPi * i / outerSegments;
                float a2 = outerRingRotation + MathHelper.TwoPi * (i + 1) / outerSegments;

                //齿轮齿的凸起
                float toothPhase = (float)i / outerSegments * teeth;
                float toothOffset = MathF.Sin(toothPhase * MathHelper.TwoPi) > 0.5f ? 8f : 0f;

                Vector2 p1 = center + a1.ToRotationVector2() * (outerRadius + toothOffset);
                Vector2 p2 = center + a2.ToRotationVector2() * (outerRadius + toothOffset);
                DrawLine(sb, pixel, p1, p2, outerColor, 2f);
            }

            //中环（虚线旋转）
            float midRadius = 80f;
            int midSegments = 48;
            Color midColor = new Color(60, 160, 240) * (baseAlpha * (0.4f + pulse * 0.2f));
            for (int i = 0; i < midSegments; i++) {
                if (i % 3 == 0) continue; //跳过产生虚线效果
                float a1 = innerRingRotation + MathHelper.TwoPi * i / midSegments;
                float a2 = innerRingRotation + MathHelper.TwoPi * (i + 1) / midSegments;
                Vector2 p1 = center + a1.ToRotationVector2() * midRadius;
                Vector2 p2 = center + a2.ToRotationVector2() * midRadius;
                DrawLine(sb, pixel, p1, p2, midColor, 1.5f);
            }

            //内环（实线）
            float innerRadius = 50f;
            int innerSegments = 40;
            Color innerColor = new Color(80, 200, 255) * (baseAlpha * (0.5f + pulse * 0.3f));
            for (int i = 0; i < innerSegments; i++) {
                float a1 = -outerRingRotation * 0.5f + MathHelper.TwoPi * i / innerSegments;
                float a2 = -outerRingRotation * 0.5f + MathHelper.TwoPi * (i + 1) / innerSegments;
                Vector2 p1 = center + a1.ToRotationVector2() * innerRadius;
                Vector2 p2 = center + a2.ToRotationVector2() * innerRadius;
                DrawLine(sb, pixel, p1, p2, innerColor, 1.5f);
            }

            //中心十字准星
            float crossSize = 20f;
            Color crossColor = new Color(100, 220, 255) * (baseAlpha * 0.7f);
            DrawLine(sb, pixel, center + new Vector2(-crossSize, 0), center + new Vector2(-6, 0), crossColor, 2f);
            DrawLine(sb, pixel, center + new Vector2(6, 0), center + new Vector2(crossSize, 0), crossColor, 2f);
            DrawLine(sb, pixel, center + new Vector2(0, -crossSize), center + new Vector2(0, -6), crossColor, 2f);
            DrawLine(sb, pixel, center + new Vector2(0, 6), center + new Vector2(0, crossSize), crossColor, 2f);

            //中心发光点
            if (CWRAsset.SoftGlow?.IsLoaded ?? false) {
                Texture2D glow = CWRAsset.SoftGlow.Value;
                float glowScale = 0.8f + pulse * 0.3f;
                Color glowColor = new Color(80, 200, 255, 0) * (baseAlpha * 0.6f);
                sb.Draw(glow, center, null, glowColor, 0f, glow.Size() / 2f, glowScale, SpriteEffects.None, 0f);
            }

            //四方向刻度标记
            for (int i = 0; i < 8; i++) {
                float angle = aquilaRotation + MathHelper.TwoPi * i / 8f;
                Vector2 inner2 = center + angle.ToRotationVector2() * (outerRadius + 12f);
                Vector2 outer2 = center + angle.ToRotationVector2() * (outerRadius + 22f);
                float tickAlpha = (i % 2 == 0) ? 0.6f : 0.3f;
                float tickWidth = (i % 2 == 0) ? 2f : 1f;
                DrawLine(sb, pixel, inner2, outer2, new Color(60, 160, 240) * (baseAlpha * tickAlpha), tickWidth);
            }

            //旋转数据标记环
            for (int i = 0; i < 12; i++) {
                float angle = outerRingRotation * 2f + MathHelper.TwoPi * i / 12f;
                Vector2 markPos = center + angle.ToRotationVector2() * (outerRadius + 30f);
                float markPulse = MathF.Sin(globalTime * 3f + i * 0.5f) * 0.5f + 0.5f;
                Color markColor = new Color(50, 150, 230) * (baseAlpha * 0.3f * markPulse);
                sb.Draw(pixel, markPos, new Rectangle(0, 0, 1, 1), markColor,
                    angle, new Vector2(0.5f), new Vector2(6f, 2f), SpriteEffects.None, 0f);
            }
        }

        private void DrawParticles(SpriteBatch sb, Texture2D pixel, float alpha) {
            foreach (var p in particles) {
                float lifeRatio = p.Life / p.MaxLife;
                float fadeIn = Math.Min(lifeRatio * 3f, 1f);
                float fadeOut = Math.Min((1f - lifeRatio) * 3f, 1f);
                float pAlpha = fadeIn * fadeOut * alpha;
                float size = p.Size * (0.6f + lifeRatio * 0.4f);

                Color color = p.BaseColor * pAlpha;
                sb.Draw(pixel, p.Position, new Rectangle(0, 0, 1, 1), color, p.Rotation,
                    new Vector2(0.5f), new Vector2(size), SpriteEffects.None, 0f);

                //辉光层
                sb.Draw(pixel, p.Position, new Rectangle(0, 0, 1, 1), color * 0.3f, p.Rotation,
                    new Vector2(0.5f), new Vector2(size * 2.5f), SpriteEffects.None, 0f);
            }
        }

        private void DrawProgressBar(SpriteBatch sb, Texture2D pixel, int sw, int sh, float alpha) {
            float barY = sh * ProgressBarY;
            float barX = (sw - ProgressBarWidth) * 0.5f;

            //进度条外框
            Rectangle outerRect = new((int)(barX - 3), (int)(barY - 3), (int)(ProgressBarWidth + 6), (int)(ProgressBarHeight + 6));
            Color borderColor = new Color(50, 140, 220) * (alpha * 0.6f);
            DrawRectBorder(sb, pixel, outerRect, borderColor, 1);

            //进度条背景轨道
            Rectangle trackRect = new((int)barX, (int)barY, (int)ProgressBarWidth, (int)ProgressBarHeight);
            sb.Draw(pixel, trackRect, new Rectangle(0, 0, 1, 1), new Color(10, 20, 35) * (alpha * 0.9f));

            //进度条填充（渐变）
            int fillWidth = (int)(ProgressBarWidth * currentProgress);
            if (fillWidth > 0) {
                for (int i = 0; i < fillWidth; i++) {
                    float t = i / ProgressBarWidth;
                    Color fillColor = Color.Lerp(new Color(30, 100, 200), new Color(80, 220, 255), t);

                    //流光叠加
                    float shimmer = MathF.Sin(progressGlowPhase - t * 8f) * 0.5f + 0.5f;
                    fillColor = Color.Lerp(fillColor, new Color(150, 240, 255), shimmer * 0.3f);

                    sb.Draw(pixel, new Rectangle((int)barX + i, (int)barY, 1, (int)ProgressBarHeight),
                        new Rectangle(0, 0, 1, 1), fillColor * (alpha * 0.9f));
                }

                //进度头部发光
                Vector2 headPos = new(barX + fillWidth, barY + ProgressBarHeight * 0.5f);
                if (CWRAsset.SoftGlow?.IsLoaded ?? false) {
                    Texture2D glow = CWRAsset.SoftGlow.Value;
                    float glowPulse = MathF.Sin(progressGlowPhase * 2f) * 0.5f + 0.5f;
                    Color glowColor = new Color(100, 220, 255, 0) * (alpha * (0.4f + glowPulse * 0.4f));
                    sb.Draw(glow, headPos, null, glowColor, 0f, glow.Size() / 2f, 0.25f, SpriteEffects.None, 0f);
                }
            }

            //进度百分比文字
            string percentText = $"{(int)(currentProgress * 100)}%";
            Vector2 percentSize = FontAssets.MouseText.Value.MeasureString(percentText) * 0.8f;
            Vector2 percentPos = new(barX + ProgressBarWidth + 16, barY + ProgressBarHeight * 0.5f - percentSize.Y * 0.5f);
            Color percentColor = Color.Lerp(new Color(100, 180, 230), new Color(150, 240, 255), MathF.Sin(globalTime * 2f) * 0.5f + 0.5f);
            Utils.DrawBorderString(sb, percentText, percentPos, percentColor * alpha, 0.8f);

            //小刻度标记
            for (int i = 1; i < 10; i++) {
                float markX = barX + ProgressBarWidth * i / 10f;
                float markHeight = (i == 5) ? 5f : 3f;
                Color markColor = new Color(50, 140, 220) * (alpha * 0.4f);
                sb.Draw(pixel, new Rectangle((int)markX, (int)(barY + ProgressBarHeight + 2), 1, (int)markHeight),
                    new Rectangle(0, 0, 1, 1), markColor);
            }
        }

        private void DrawLoadingText(SpriteBatch sb, int sw, int sh, float alpha) {
            float barY = sh * ProgressBarY;

            //加载状态文字（进度条上方）
            string statusText = LoadingText.Value;
            //动态省略号
            int dots = ((int)(globalTime * 4f)) % 4;
            statusText += new string('.', dots);

            Vector2 statusSize = FontAssets.MouseText.Value.MeasureString(statusText) * 0.85f;
            Vector2 statusPos = new((sw - statusSize.X) * 0.5f, barY - 30f);

            //文字光晕
            Color glowColor = new Color(60, 180, 255) * (alpha * 0.2f);
            for (int i = 0; i < 4; i++) {
                float angle = MathHelper.TwoPi * i / 4f + globalTime;
                Vector2 off = angle.ToRotationVector2() * 2f;
                Utils.DrawBorderString(sb, statusText, statusPos + off, glowColor, 0.85f);
            }

            Color textColor = new Color(180, 220, 255) * alpha;
            Utils.DrawBorderString(sb, statusText, statusPos, textColor, 0.85f);

            //附加初始化文字
            if (currentProgress < 0.5f) {
                string initText = InitializingText.Value;
                int initDots = ((int)(globalTime * 3f)) % 4;
                initText += new string('.', initDots);
                Vector2 initSize = FontAssets.MouseText.Value.MeasureString(initText) * 0.65f;
                Vector2 initPos = new((sw - initSize.X) * 0.5f, barY + 18f);
                Color initColor = new Color(80, 160, 220) * (alpha * 0.5f * (1f - currentProgress * 2f));
                Utils.DrawBorderString(sb, initText, initPos, initColor, 0.65f);
            }
        }

        private void DrawHintText(SpriteBatch sb, int sw, int sh, float alpha) {
            if (HintTexts == null || HintTexts.Length == 0) return;

            string hint = HintTexts[currentHintIndex].Value;
            float hintAlpha = hintFadeProgress * alpha;

            //引号装饰
            string displayText = $"「{hint}」";

            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(displayText) * 0.75f;
            Vector2 textPos = new((sw - textSize.X) * 0.5f, sh - 60f);

            //文字发光底层
            Color glowColor = new Color(40, 120, 180) * (hintAlpha * 0.15f);
            for (int i = 0; i < 4; i++) {
                float angle = MathHelper.TwoPi * i / 4f;
                Vector2 off = angle.ToRotationVector2() * 1.5f;
                Utils.DrawBorderString(sb, displayText, textPos + off, glowColor, 0.75f);
            }

            //主文字（淡蓝灰色，带轻微闪烁）
            float textFlicker = 0.9f + MathF.Sin(globalTime * 5f) * 0.1f;
            Color hintColor = new Color(140, 180, 210) * (hintAlpha * textFlicker);
            Utils.DrawBorderString(sb, displayText, textPos, hintColor, 0.75f);

            //提示文字下方的短横线装饰
            float lineWidth = textSize.X * 0.3f;
            Vector2 lineCenter = new(sw * 0.5f, sh - 40f);
            DrawLine(sb, VaultAsset.placeholder2.Value,
                lineCenter - new Vector2(lineWidth * 0.5f, 0),
                lineCenter + new Vector2(lineWidth * 0.5f, 0),
                new Color(50, 140, 200) * (hintAlpha * 0.4f), 1f);
        }

        private void DrawHologramOverlay(SpriteBatch sb, Texture2D pixel, int sw, int sh, float alpha) {
            //全局全息闪烁叠加
            float flicker = MathF.Sin(hologramFlicker * 1.5f) * 0.5f + 0.5f;
            if (flicker > 0.85f) {
                Color overlayColor = new Color(20, 60, 100) * (alpha * 0.04f * (flicker - 0.85f) * 6.67f);
                sb.Draw(pixel, new Rectangle(0, 0, sw, sh), new Rectangle(0, 0, 1, 1), overlayColor);
            }

            //偶尔的故障条
            if (MathF.Sin(globalTime * 7f) > 0.95f) {
                int glitchY = (int)(MathF.Sin(globalTime * 23f) * 0.5f + 0.5f) * sh;
                int glitchH = Main.rand?.Next(2, 8) ?? 4;
                Color glitchColor = new Color(80, 200, 255) * (alpha * 0.12f);
                sb.Draw(pixel, new Rectangle(0, glitchY, sw, glitchH), new Rectangle(0, 0, 1, 1), glitchColor);
            }
        }

        private void DrawCornerDecorations(SpriteBatch sb, Texture2D pixel, int sw, int sh, float alpha) {
            float pulse = MathF.Sin(circuitPulseTimer * 1.8f) * 0.5f + 0.5f;
            Color cornerColor = new Color(60, 170, 240) * (alpha * (0.4f + pulse * 0.3f));
            float len = 40f;
            float thickness = 2f;

            //左上
            DrawLine(sb, pixel, new Vector2(10, 10), new Vector2(10 + len, 10), cornerColor, thickness);
            DrawLine(sb, pixel, new Vector2(10, 10), new Vector2(10, 10 + len), cornerColor, thickness);

            //右上
            DrawLine(sb, pixel, new Vector2(sw - 10, 10), new Vector2(sw - 10 - len, 10), cornerColor, thickness);
            DrawLine(sb, pixel, new Vector2(sw - 10, 10), new Vector2(sw - 10, 10 + len), cornerColor, thickness);

            //左下
            DrawLine(sb, pixel, new Vector2(10, sh - 10), new Vector2(10 + len, sh - 10), cornerColor, thickness);
            DrawLine(sb, pixel, new Vector2(10, sh - 10), new Vector2(10, sh - 10 - len), cornerColor, thickness);

            //右下
            DrawLine(sb, pixel, new Vector2(sw - 10, sh - 10), new Vector2(sw - 10 - len, sh - 10), cornerColor, thickness);
            DrawLine(sb, pixel, new Vector2(sw - 10, sh - 10), new Vector2(sw - 10, sh - 10 - len), cornerColor, thickness);

            //角点发光
            Vector2[] corners = [
                new(10, 10), new(sw - 10, 10),
                new(10, sh - 10), new(sw - 10, sh - 10)
            ];
            foreach (var corner in corners) {
                sb.Draw(pixel, corner, new Rectangle(0, 0, 1, 1),
                    cornerColor * 0.8f, MathHelper.PiOver4, new Vector2(0.5f), new Vector2(5f), SpriteEffects.None, 0f);
            }
        }

        private void DrawEdgeBorders(SpriteBatch sb, Texture2D pixel, int sw, int sh, float alpha) {
            float pulse = MathF.Sin(circuitPulseTimer) * 0.5f + 0.5f;
            Color borderColor = new Color(40, 120, 200) * (alpha * (0.3f + pulse * 0.2f));

            //顶部边框
            sb.Draw(pixel, new Rectangle(0, 0, sw, 2), new Rectangle(0, 0, 1, 1), borderColor);
            //底部边框
            sb.Draw(pixel, new Rectangle(0, sh - 2, sw, 2), new Rectangle(0, 0, 1, 1), borderColor);

            //顶部内线
            sb.Draw(pixel, new Rectangle(50, 6, sw - 100, 1), new Rectangle(0, 0, 1, 1), borderColor * 0.4f);
            //底部内线
            sb.Draw(pixel, new Rectangle(50, sh - 7, sw - 100, 1), new Rectangle(0, 0, 1, 1), borderColor * 0.4f);
        }

        #endregion

        #region 绘制工具

        private static void DrawLine(SpriteBatch sb, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness) {
            Vector2 diff = end - start;
            float length = diff.Length();
            if (length < 0.5f) return;
            float angle = MathF.Atan2(diff.Y, diff.X);
            sb.Draw(pixel, start, new Rectangle(0, 0, 1, 1), color, angle,
                new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        private static void DrawRectBorder(SpriteBatch sb, Texture2D pixel, Rectangle rect, Color color, int thickness) {
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), new Rectangle(0, 0, 1, 1), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), new Rectangle(0, 0, 1, 1), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), new Rectangle(0, 0, 1, 1), color);
            sb.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), new Rectangle(0, 0, 1, 1), color);
        }

        #endregion

        #region 粒子数据结构

        private struct TechParticle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
            public float Size;
            public float Rotation;
            public float RotationSpeed;
            public Color BaseColor;

            public bool Update() {
                Life -= 0.016f;
                Position += Velocity;
                Velocity *= 0.995f;
                Rotation += RotationSpeed;
                return Life <= 0f;
            }
        }

        private struct DataStreamLine
        {
            public Vector2 Start;
            public Vector2 End;
            public float Life;
            public float MaxLife;
            public float Progress;
            public float Speed;
            public float Thickness;
            public Color LineColor;

            public bool Update() {
                Life -= 0.016f;
                Progress += Speed;
                if (Progress > 1.15f) Progress = -0.15f;
                return Life <= 0f;
            }
        }

        private struct CircuitNode
        {
            public Vector2 Center;
            public List<Vector2> Branches;
            public float Life;
            public float MaxLife;
            public float PulsePhase;

            public bool Update() {
                Life -= 0.016f;
                return Life <= 0f;
            }
        }

        private struct HexCell
        {
            public Vector2 Position;
            public float Size;
            public float Life;
            public float MaxLife;
            public float Rotation;
            public float PulsePhase;

            public bool Update() {
                Life -= 0.016f;
                return Life <= 0f;
            }
        }

        #endregion
    }
}
