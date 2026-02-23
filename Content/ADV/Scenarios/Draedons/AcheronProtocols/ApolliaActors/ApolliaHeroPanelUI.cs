using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines;
using CalamityOverhaul.Content.ADV.UIEffect;
using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors
{
    /// <summary>
    /// 阿波利娅英雄单位面板UI——星流风格
    /// 左侧屏幕图标触发，展示HP/伤害/指令/立绘
    /// </summary>
    internal class ApolliaHeroPanelUI : UIHandle, ILocalizedModType
    {
        #region 常量与尺寸

        private const float PanelWidth = 360f;
        private const float PanelHeight = 480f;
        private const float IconSize = 42f;
        private const float IconMarginLeft = 12f;
        private const float IconMarginTop = 200f;
        private const float PortraitAreaHeight = 180f;
        private const float HPBarHeight = 14f;
        private const float CommandBtnHeight = 28f;
        private const float CommandBtnWidth = 72f;
        private const float SectionPadding = 10f;

        #endregion

        #region 状态

        public static ApolliaHeroPanelUI Instance => UIHandleLoader.GetUIHandleOfType<ApolliaHeroPanelUI>();
        public string LocalizationCategory => "UI";

        /// <summary>英雄面板是否已解锁（对话完成后启用）</summary>
        internal bool Unlocked;

        /// <summary>面板是否展开</summary>
        private bool panelOpen;

        /// <summary>面板淡入淡出</summary>
        private float panelAlpha;

        /// <summary>图标淡入淡出</summary>
        private float iconAlpha;

        /// <summary>图标呼吸计时器</summary>
        private float iconPulseTimer;

        /// <summary>英雄数据引用</summary>
        internal ApolliaHeroData HeroData = new();

        public override bool Active => Unlocked && MachineWorld.Active;

        #endregion

        #region 动画

        private float starFlowTimer;
        private float nebulaPulseTimer;
        private float shimmerTimer;
        private float dataStreamTimer;
        private float hexGridTimer;

        #endregion

        #region 粒子

        private readonly List<HeroUnitPRT> orbitParticles = [];
        private int orbitSpawnTimer;
        private readonly List<StarDustPRT> panelDusts = [];
        private int dustSpawnTimer;

        #endregion

        #region 交互区域

        private Rectangle iconRect;
        private Rectangle panelRect;
        private Rectangle portraitRect;
        private Rectangle hpBarRect;
        private Rectangle statsRect;
        private Rectangle commandRect;
        private readonly Rectangle[] commandBtnRects = new Rectangle[4];

        private bool hoveringIcon;
        private bool hoveringPanel;
        private int hoveringCommandIndex = -1;

        //拖拽
        private bool isDragging;
        private Vector2 dragOffset;

        #endregion

        #region 本地化

        private static LocalizedText TitleText;
        private static LocalizedText HPLabel;
        private static LocalizedText DamageLabel;
        private static LocalizedText DefenseLabel;
        private static LocalizedText CommandLabel;
        private static LocalizedText CmdFollow;
        private static LocalizedText CmdHold;
        private static LocalizedText CmdAggressive;
        private static LocalizedText CmdDefensive;

        #endregion

        public override void SetStaticDefaults() {
            TitleText = this.GetLocalization(nameof(TitleText), () => "阿波利娅");
            HPLabel = this.GetLocalization(nameof(HPLabel), () => "生命值");
            DamageLabel = this.GetLocalization(nameof(DamageLabel), () => "伤害");
            DefenseLabel = this.GetLocalization(nameof(DefenseLabel), () => "防御");
            CommandLabel = this.GetLocalization(nameof(CommandLabel), () => "作战指令");
            CmdFollow = this.GetLocalization(nameof(CmdFollow), () => "跟随");
            CmdHold = this.GetLocalization(nameof(CmdHold), () => "驻守");
            CmdAggressive = this.GetLocalization(nameof(CmdAggressive), () => "进攻");
            CmdDefensive = this.GetLocalization(nameof(CmdDefensive), () => "防御");
        }

        #region Update

        public override void Update() {
            //图标始终可见（解锁后）
            iconPulseTimer += 0.03f;
            if (iconPulseTimer > MathHelper.TwoPi) iconPulseTimer -= MathHelper.TwoPi;

            iconAlpha = Unlocked ? Math.Min(1f, iconAlpha + 0.08f) : Math.Max(0f, iconAlpha - 0.08f);

            //面板淡入淡出
            if (panelOpen) {
                panelAlpha = Math.Min(1f, panelAlpha + 0.1f);
            }
            else {
                panelAlpha = Math.Max(0f, panelAlpha - 0.1f);
            }

            //图标位置（左侧屏幕中部）
            float iconX = IconMarginLeft;
            float iconY = IconMarginTop;
            iconRect = new Rectangle((int)iconX, (int)iconY, (int)IconSize, (int)IconSize);

            //面板位置
            if (DrawPosition == Vector2.Zero) {
                DrawPosition = new Vector2(IconMarginLeft + IconSize + 8, IconMarginTop - 40);
            }

            //动画计时器
            starFlowTimer += 0.035f;
            nebulaPulseTimer += 0.02f;
            shimmerTimer += 0.028f;
            dataStreamTimer += 0.04f;
            hexGridTimer += 0.015f;

            if (starFlowTimer > MathHelper.TwoPi) starFlowTimer -= MathHelper.TwoPi;
            if (nebulaPulseTimer > MathHelper.TwoPi) nebulaPulseTimer -= MathHelper.TwoPi;
            if (shimmerTimer > MathHelper.TwoPi) shimmerTimer -= MathHelper.TwoPi;
            if (dataStreamTimer > MathHelper.TwoPi) dataStreamTimer -= MathHelper.TwoPi;
            if (hexGridTimer > MathHelper.TwoPi) hexGridTimer -= MathHelper.TwoPi;

            //计算子区域
            Vector2 topLeft = DrawPosition;
            panelRect = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)PanelWidth, (int)PanelHeight);

            int curY = panelRect.Y + 40;
            portraitRect = new Rectangle(panelRect.X + (int)SectionPadding, curY, panelRect.Width - (int)(SectionPadding * 2), (int)PortraitAreaHeight);
            curY += (int)PortraitAreaHeight + 8;

            hpBarRect = new Rectangle(panelRect.X + (int)SectionPadding + 50, curY + 16, panelRect.Width - (int)(SectionPadding * 2) - 60, (int)HPBarHeight);
            curY += 40;

            statsRect = new Rectangle(panelRect.X + (int)SectionPadding, curY, panelRect.Width - (int)(SectionPadding * 2), 60);
            curY += 68;

            commandRect = new Rectangle(panelRect.X + (int)SectionPadding, curY, panelRect.Width - (int)(SectionPadding * 2), 80);

            //指令按钮布局 2x2
            int btnStartX = commandRect.X + 10;
            int btnStartY = commandRect.Y + 24;
            int btnGapX = (int)CommandBtnWidth + 8;
            int btnGapY = (int)CommandBtnHeight + 6;
            for (int i = 0; i < 4; i++) {
                int col = i % 2;
                int row = i / 2;
                commandBtnRects[i] = new Rectangle(
                    btnStartX + col * btnGapX,
                    btnStartY + row * btnGapY,
                    (int)CommandBtnWidth, (int)CommandBtnHeight);
            }

            //交互检测
            Vector2 mousePos = new Vector2(Main.mouseX, Main.mouseY);
            hoveringIcon = iconRect.Contains(mousePos.ToPoint());

            if (panelAlpha > 0.01f) {
                hoveringPanel = panelRect.Contains(mousePos.ToPoint());
                hoveringCommandIndex = -1;
                if (!isDragging) {
                    for (int i = 0; i < 4; i++) {
                        if (commandBtnRects[i].Contains(mousePos.ToPoint())) {
                            hoveringCommandIndex = i;
                            break;
                        }
                    }
                }
            }
            else {
                hoveringPanel = false;
                hoveringCommandIndex = -1;
            }

            if (hoveringIcon || (hoveringPanel && panelAlpha > 0.3f)) {
                player.mouseInterface = true;
            }

            //图标点击
            if (hoveringIcon && UIHandleLoader.keyLeftPressState == KeyPressState.Pressed) {
                panelOpen = !panelOpen;
                SoundEngine.PlaySound(panelOpen ? SoundID.MenuOpen : SoundID.MenuClose);
            }

            //指令按钮点击
            if (panelAlpha > 0.5f && hoveringCommandIndex >= 0
                && UIHandleLoader.keyLeftPressState == KeyPressState.Pressed) {
                HeroData.CurrentCommand = (HeroCommand)hoveringCommandIndex;
                SoundEngine.PlaySound(SoundID.MenuTick);
            }

            //面板拖拽
            HandleDragging(mousePos);

            //限制面板位置
            if (panelOpen) {
                DrawPosition.X = MathHelper.Clamp(DrawPosition.X, 10, Main.screenWidth - PanelWidth - 10);
                DrawPosition.Y = MathHelper.Clamp(DrawPosition.Y, 10, Main.screenHeight - PanelHeight - 10);
            }

            //粒子更新
            UpdateParticles();
        }

        private void HandleDragging(Vector2 mousePos) {
            if (panelAlpha < 0.3f) {
                isDragging = false;
                return;
            }

            if (hoveringPanel && hoveringCommandIndex < 0
                && UIHandleLoader.keyLeftPressState == KeyPressState.Pressed
                && !isDragging && !hoveringIcon) {
                //仅在标题区域允许拖拽
                Rectangle titleDragArea = new(panelRect.X, panelRect.Y, panelRect.Width, 38);
                if (titleDragArea.Contains(mousePos.ToPoint())) {
                    isDragging = true;
                    dragOffset = DrawPosition - mousePos;
                }
            }

            if (isDragging) {
                DrawPosition = mousePos + dragOffset;
                if (UIHandleLoader.keyLeftPressState == KeyPressState.Released) {
                    isDragging = false;
                }
            }
        }

        private void UpdateParticles() {
            if (panelAlpha < 0.2f) return;

            //轨道粒子
            orbitSpawnTimer++;
            if (orbitSpawnTimer >= 35 && orbitParticles.Count < 8) {
                orbitSpawnTimer = 0;
                Vector2 center = new(portraitRect.Center.X, portraitRect.Center.Y);
                float radius = Main.rand.NextFloat(50f, 100f);
                orbitParticles.Add(new HeroUnitPRT(center, radius));
            }
            for (int i = orbitParticles.Count - 1; i >= 0; i--) {
                orbitParticles[i].Center = new Vector2(portraitRect.Center.X, portraitRect.Center.Y);
                if (orbitParticles[i].Update()) {
                    orbitParticles.RemoveAt(i);
                }
            }

            //星尘粒子
            dustSpawnTimer++;
            if (dustSpawnTimer >= 40 && panelDusts.Count < 6) {
                dustSpawnTimer = 0;
                Vector2 start = new(
                    Main.rand.NextFloat(panelRect.X + 20, panelRect.Right - 20),
                    Main.rand.NextFloat(panelRect.Y + 60, panelRect.Bottom - 20));
                panelDusts.Add(new StarDustPRT(start));
            }
            Vector2 panelPos = new(panelRect.X, panelRect.Y);
            Vector2 panelSize = new(panelRect.Width, panelRect.Height);
            for (int i = panelDusts.Count - 1; i >= 0; i--) {
                if (panelDusts[i].Update(panelPos, panelSize)) {
                    panelDusts.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Draw

        public override void Draw(SpriteBatch spriteBatch) {
            //图标
            if (iconAlpha > 0.01f) {
                DrawIcon(spriteBatch);
            }

            //面板
            if (panelAlpha > 0.01f) {
                DrawPanel(spriteBatch);
            }
        }

        #region 图标绘制

        private void DrawIcon(SpriteBatch sb) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float alpha = iconAlpha;
            float pulse = MathF.Sin(iconPulseTimer * 1.8f) * 0.5f + 0.5f;

            //图标底盘——深蓝菱形
            Color bgColor = Color.Lerp(new Color(8, 6, 20), new Color(15, 12, 35), pulse) * (alpha * 0.92f);
            sb.Draw(px, iconRect, new Rectangle(0, 0, 1, 1), bgColor);

            //金色边框
            Color borderColor = Color.Lerp(new Color(180, 140, 50), new Color(240, 200, 100), pulse) * (alpha * 0.85f);
            sb.Draw(px, new Rectangle(iconRect.X, iconRect.Y, iconRect.Width, 2), new Rectangle(0, 0, 1, 1), borderColor);
            sb.Draw(px, new Rectangle(iconRect.X, iconRect.Bottom - 2, iconRect.Width, 2), new Rectangle(0, 0, 1, 1), borderColor * 0.7f);
            sb.Draw(px, new Rectangle(iconRect.X, iconRect.Y, 2, iconRect.Height), new Rectangle(0, 0, 1, 1), borderColor * 0.9f);
            sb.Draw(px, new Rectangle(iconRect.Right - 2, iconRect.Y, 2, iconRect.Height), new Rectangle(0, 0, 1, 1), borderColor * 0.9f);

            //中心星辰标记
            Vector2 iconCenter = iconRect.Center.ToVector2();
            DrawHeroStarMark(sb, iconCenter, alpha * (0.7f + pulse * 0.3f), 10f);

            //悬停高亮
            if (hoveringIcon) {
                Color hover = new Color(255, 220, 120) * (alpha * 0.15f);
                sb.Draw(px, iconRect, new Rectangle(0, 0, 1, 1), hover);
            }
        }

        #endregion

        #region 面板绘制

        private void DrawPanel(SpriteBatch sb) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float alpha = panelAlpha;

            //阴影
            Rectangle shadow = panelRect;
            shadow.Offset(4, 6);
            sb.Draw(px, shadow, new Rectangle(0, 0, 1, 1), new Color(3, 0, 10) * (alpha * 0.55f));

            //深空渐变背景——与对话框区别：使用更深邃的靛蓝+微妙的紫红星云色
            DrawPanelBackground(sb, alpha);

            //六角网格纹理——区别化设计元素
            DrawHexGrid(sb, panelRect, alpha * 0.5f);

            //数据流光带
            DrawDataStreams(sb, panelRect, alpha * 0.6f);

            //内部微光
            float innerPulse = MathF.Sin(shimmerTimer * 0.9f) * 0.5f + 0.5f;
            Rectangle inner = panelRect;
            inner.Inflate(-4, -4);
            sb.Draw(px, inner, new Rectangle(0, 0, 1, 1), new Color(150, 120, 50) * (alpha * 0.04f * innerPulse));

            //边框
            DrawPanelFrame(sb, panelRect, alpha, innerPulse);

            //粒子层
            foreach (var dust in panelDusts) {
                dust.Draw(sb, alpha * 0.5f);
            }
            foreach (var orbit in orbitParticles) {
                orbit.Draw(sb, alpha * 0.6f);
            }

            //标题
            DrawTitle(sb, alpha);

            //立绘区域
            DrawPortraitSection(sb, alpha);

            //HP条
            DrawHPBar(sb, alpha);

            //属性面板
            DrawStatsSection(sb, alpha);

            //指令面板
            DrawCommandSection(sb, alpha);
        }

        private void DrawPanelBackground(SpriteBatch sb, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            int segs = 40;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = panelRect.Y + (int)(t * panelRect.Height);
                int y2 = panelRect.Y + (int)(t2 * panelRect.Height);
                Rectangle r = new(panelRect.X, y1, panelRect.Width, Math.Max(1, y2 - y1));

                //靛蓝深空色——比对话框更偏冷色调，区别化
                Color deep = new Color(4, 3, 14);
                Color mid = new Color(8, 7, 24);
                Color warm = new Color(16, 12, 36);

                float nebula = MathF.Sin(nebulaPulseTimer * 0.6f + t * 2.2f) * 0.5f + 0.5f;
                Color blended = Color.Lerp(deep, mid, nebula);
                //底部微微偏暖
                Color c = Color.Lerp(blended, warm, t * t * 0.4f);
                c *= alpha * 0.95f;

                sb.Draw(px, r, new Rectangle(0, 0, 1, 1), c);
            }

            //星云色斑叠加——紫红色调，与对话框的蓝紫区分
            float nebulaBreath = MathF.Sin(nebulaPulseTimer * 1.1f) * 0.5f + 0.5f;
            Color nebulaSpot = new Color(25, 8, 35) * (alpha * 0.18f * nebulaBreath);
            sb.Draw(px, panelRect, new Rectangle(0, 0, 1, 1), nebulaSpot);
        }

        /// <summary>
        /// 六角网格——英雄面板独有的科技纹理
        /// </summary>
        private void DrawHexGrid(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            int cols = 12;
            int rows = 16;
            float cellW = rect.Width / (float)cols;
            float cellH = rect.Height / (float)rows;

            for (int row = 0; row < rows; row++) {
                for (int col = 0; col < cols; col++) {
                    float phase = hexGridTimer + (row * 0.4f + col * 0.3f);
                    float brightness = MathF.Sin(phase) * 0.5f + 0.5f;
                    if (brightness < 0.3f) continue;

                    float x = rect.X + col * cellW + (row % 2 == 0 ? 0 : cellW * 0.5f);
                    float y = rect.Y + row * cellH;

                    Color dotColor = new Color(120, 100, 60) * (alpha * 0.035f * brightness);
                    sb.Draw(px, new Vector2(x, y), null, dotColor, 0f, new Vector2(0.5f),
                        new Vector2(2f), SpriteEffects.None, 0f);
                }
            }
        }

        /// <summary>
        /// 数据流光带——横向流动的蓝金色细线
        /// </summary>
        private void DrawDataStreams(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            int lineCount = 3;

            for (int i = 0; i < lineCount; i++) {
                float t = (i + 1f) / (lineCount + 1f);
                float y = rect.Y + t * rect.Height;
                float flowOffset = (dataStreamTimer * (0.5f + i * 0.2f)) % 1f;

                int streamW = 60;
                int streamX = rect.X + (int)(flowOffset * (rect.Width - streamW));

                for (int dx = 0; dx < streamW; dx++) {
                    float localT = dx / (float)streamW;
                    float intensity = MathF.Sin(localT * MathHelper.Pi);
                    //蓝金色混合
                    Color streamColor = Color.Lerp(
                        new Color(80, 120, 200),
                        new Color(220, 180, 80),
                        localT) * (alpha * 0.12f * intensity);
                    sb.Draw(px, new Rectangle(streamX + dx, (int)y, 1, 1), new Rectangle(0, 0, 1, 1), streamColor);
                }
            }
        }

        private void DrawPanelFrame(SpriteBatch sb, Rectangle rect, float alpha, float pulse) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //外框：蓝金色混合——与对话框的纯金色区分
            Color outerEdge = Color.Lerp(
                new Color(100, 130, 180),
                new Color(220, 190, 100),
                pulse * 0.6f) * (alpha * 0.75f);

            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 2), new Rectangle(0, 0, 1, 1), outerEdge);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), new Rectangle(0, 0, 1, 1), outerEdge * 0.65f);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 2, rect.Height), new Rectangle(0, 0, 1, 1), outerEdge * 0.85f);
            sb.Draw(px, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), new Rectangle(0, 0, 1, 1), outerEdge * 0.85f);

            //内框
            Rectangle innerFrame = rect;
            innerFrame.Inflate(-5, -5);
            Color innerC = new Color(200, 180, 120) * (alpha * 0.12f * pulse);
            sb.Draw(px, new Rectangle(innerFrame.X, innerFrame.Y, innerFrame.Width, 1), new Rectangle(0, 0, 1, 1), innerC);
            sb.Draw(px, new Rectangle(innerFrame.X, innerFrame.Bottom - 1, innerFrame.Width, 1), new Rectangle(0, 0, 1, 1), innerC * 0.6f);
            sb.Draw(px, new Rectangle(innerFrame.X, innerFrame.Y, 1, innerFrame.Height), new Rectangle(0, 0, 1, 1), innerC * 0.8f);
            sb.Draw(px, new Rectangle(innerFrame.Right - 1, innerFrame.Y, 1, innerFrame.Height), new Rectangle(0, 0, 1, 1), innerC * 0.8f);

            //角落星标
            DrawHeroStarMark(sb, new Vector2(rect.X + 10, rect.Y + 10), alpha * 0.8f, 5f);
            DrawHeroStarMark(sb, new Vector2(rect.Right - 10, rect.Y + 10), alpha * 0.8f, 5f);
            DrawHeroStarMark(sb, new Vector2(rect.X + 10, rect.Bottom - 10), alpha * 0.55f, 5f);
            DrawHeroStarMark(sb, new Vector2(rect.Right - 10, rect.Bottom - 10), alpha * 0.55f, 5f);

            //顶部流光
            float flowT = (shimmerTimer * 0.7f) % 1f;
            int hlW = 60;
            int hlX = rect.X + (int)(flowT * (rect.Width - hlW));
            Color hlColor = new Color(180, 200, 255) * (alpha * 0.25f);
            for (int dx = 0; dx < hlW; dx++) {
                float localT = dx / (float)hlW;
                float intensity = MathF.Sin(localT * MathHelper.Pi);
                sb.Draw(px, new Rectangle(hlX + dx, rect.Y, 1, 2), new Rectangle(0, 0, 1, 1), hlColor * intensity);
            }
        }

        private void DrawTitle(SpriteBatch sb, float alpha) {
            string title = $"✦ {TitleText.Value} ✦";
            Vector2 titleSize = FontAssets.MouseText.Value.MeasureString(title) * 0.88f;
            Vector2 titlePos = new(panelRect.X + panelRect.Width * 0.5f - titleSize.X * 0.5f, panelRect.Y + 12);

            //光晕
            Color glow = new Color(200, 180, 120) * (alpha * 0.45f);
            for (int i = 0; i < 4; i++) {
                float angle = MathHelper.TwoPi * i / 4f + shimmerTimer * 0.2f;
                Vector2 offset = angle.ToRotationVector2() * 1.8f;
                Utils.DrawBorderString(sb, title, titlePos + offset, glow, 0.88f);
            }

            Color titleColor = new Color(255, 245, 220) * alpha;
            Utils.DrawBorderString(sb, title, titlePos, titleColor, 0.88f);

            //标题下方分割线
            Vector2 divStart = new(panelRect.X + SectionPadding, panelRect.Y + 36);
            Vector2 divEnd = new(panelRect.Right - SectionPadding, panelRect.Y + 36);
            DrawGradientLine(sb, divStart, divEnd,
                new Color(180, 160, 80) * (alpha * 0.7f),
                new Color(80, 100, 160) * (alpha * 0.15f), 1.2f);
        }

        #region 立绘区域

        private void DrawPortraitSection(SpriteBatch sb, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //立绘底框——深蓝暗色
            Color frameBg = new Color(5, 3, 12) * (alpha * 0.85f);
            sb.Draw(px, portraitRect, new Rectangle(0, 0, 1, 1), frameBg);

            //立绘框边线
            Color frameEdge = new Color(120, 140, 180) * (alpha * 0.4f);
            sb.Draw(px, new Rectangle(portraitRect.X, portraitRect.Y, portraitRect.Width, 1), new Rectangle(0, 0, 1, 1), frameEdge);
            sb.Draw(px, new Rectangle(portraitRect.X, portraitRect.Bottom - 1, portraitRect.Width, 1), new Rectangle(0, 0, 1, 1), frameEdge * 0.6f);
            sb.Draw(px, new Rectangle(portraitRect.X, portraitRect.Y, 1, portraitRect.Height), new Rectangle(0, 0, 1, 1), frameEdge * 0.8f);
            sb.Draw(px, new Rectangle(portraitRect.Right - 1, portraitRect.Y, 1, portraitRect.Height), new Rectangle(0, 0, 1, 1), frameEdge * 0.8f);

            //绘制阿波利娅立绘
            Texture2D portrait = ADVAsset.Apollia;
            if (portrait != null && !portrait.IsDisposed) {
                //将立绘缩放适配到框内，居中显示上半身
                float scaleToFit = Math.Min(
                    (float)portraitRect.Width / portrait.Width,
                    (float)portraitRect.Height / (portrait.Height * 0.5f));
                scaleToFit = Math.Min(scaleToFit, 0.55f);

                //只显示上半部分
                int srcH = (int)(portraitRect.Height / scaleToFit);
                srcH = Math.Min(srcH, portrait.Height);
                Rectangle srcRect = new(0, 0, portrait.Width, srcH);

                Vector2 drawPos = new(
                    portraitRect.Center.X - portrait.Width * scaleToFit * 0.5f,
                    portraitRect.Y);

                sb.Draw(portrait, drawPos, srcRect, Color.White * (alpha * 0.9f),
                    0f, Vector2.Zero, scaleToFit, SpriteEffects.None, 0f);
            }

            //底部渐变遮罩
            for (int i = 0; i < 20; i++) {
                float t = i / 20f;
                int y = portraitRect.Bottom - 20 + i;
                Color mask = new Color(5, 3, 12) * (alpha * t * 0.9f);
                sb.Draw(px, new Rectangle(portraitRect.X + 1, y, portraitRect.Width - 2, 1), new Rectangle(0, 0, 1, 1), mask);
            }
        }

        #endregion

        #region HP条

        private void DrawHPBar(SpriteBatch sb, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //HP标签
            string hpLabel = HPLabel.Value;
            Vector2 labelPos = new(panelRect.X + SectionPadding, hpBarRect.Y - 2);
            Utils.DrawBorderString(sb, hpLabel, labelPos, new Color(200, 180, 120) * (alpha * 0.85f), 0.72f);

            //条底
            Color barBg = new Color(10, 8, 18) * (alpha * 0.9f);
            sb.Draw(px, hpBarRect, new Rectangle(0, 0, 1, 1), barBg);

            //条边框
            Color barBorder = new Color(120, 100, 60) * (alpha * 0.5f);
            sb.Draw(px, new Rectangle(hpBarRect.X, hpBarRect.Y, hpBarRect.Width, 1), new Rectangle(0, 0, 1, 1), barBorder);
            sb.Draw(px, new Rectangle(hpBarRect.X, hpBarRect.Bottom - 1, hpBarRect.Width, 1), new Rectangle(0, 0, 1, 1), barBorder * 0.6f);
            sb.Draw(px, new Rectangle(hpBarRect.X, hpBarRect.Y, 1, hpBarRect.Height), new Rectangle(0, 0, 1, 1), barBorder * 0.8f);
            sb.Draw(px, new Rectangle(hpBarRect.Right - 1, hpBarRect.Y, 1, hpBarRect.Height), new Rectangle(0, 0, 1, 1), barBorder * 0.8f);

            //HP填充
            float ratio = HeroData.HPRatio;
            int fillW = (int)((hpBarRect.Width - 2) * ratio);
            if (fillW > 0) {
                Rectangle fillRect = new(hpBarRect.X + 1, hpBarRect.Y + 1, fillW, hpBarRect.Height - 2);

                //渐变填充：绿/黄/红 根据HP比例
                Color hpColor;
                if (ratio > 0.6f) {
                    hpColor = Color.Lerp(new Color(100, 200, 120), new Color(200, 220, 100), 1f - (ratio - 0.6f) / 0.4f);
                }
                else if (ratio > 0.3f) {
                    hpColor = Color.Lerp(new Color(200, 220, 100), new Color(240, 160, 60), 1f - (ratio - 0.3f) / 0.3f);
                }
                else {
                    hpColor = Color.Lerp(new Color(240, 160, 60), new Color(240, 70, 60), 1f - ratio / 0.3f);
                }

                hpColor *= alpha;
                sb.Draw(px, fillRect, new Rectangle(0, 0, 1, 1), hpColor);

                //高光条
                float hlPulse = MathF.Sin(shimmerTimer * 1.5f) * 0.5f + 0.5f;
                sb.Draw(px, new Rectangle(fillRect.X, fillRect.Y, fillRect.Width, 2),
                    new Rectangle(0, 0, 1, 1), Color.White * (alpha * 0.15f * hlPulse));
            }

            //HP数值文字
            string hpText = $"{(int)HeroData.HP}/{(int)HeroData.MaxHP}";
            Vector2 hpTextSize = FontAssets.MouseText.Value.MeasureString(hpText) * 0.65f;
            Vector2 hpTextPos = new(hpBarRect.Center.X - hpTextSize.X * 0.5f, hpBarRect.Y - 1);
            Utils.DrawBorderString(sb, hpText, hpTextPos, new Color(255, 250, 230) * (alpha * 0.9f), 0.65f);
        }

        #endregion

        #region 属性面板

        private void DrawStatsSection(SpriteBatch sb, float alpha) {
            float textScale = 0.75f;

            //伤害
            string dmgText = $"{DamageLabel.Value}: {(int)HeroData.BaseDamage}";
            Vector2 dmgPos = new(statsRect.X + 8, statsRect.Y + 4);
            Utils.DrawBorderString(sb, dmgText, dmgPos, new Color(255, 210, 100) * (alpha * 0.9f), textScale);

            //防御
            string defText = $"{DefenseLabel.Value}: {(int)HeroData.Defense}";
            Vector2 defPos = new(statsRect.X + 8, statsRect.Y + 28);
            Utils.DrawBorderString(sb, defText, defPos, new Color(140, 180, 220) * (alpha * 0.9f), textScale);

            //分隔线
            DrawGradientLine(sb,
                new Vector2(statsRect.X, statsRect.Bottom),
                new Vector2(statsRect.Right, statsRect.Bottom),
                new Color(120, 100, 60) * (alpha * 0.4f),
                new Color(60, 80, 120) * (alpha * 0.1f), 1f);
        }

        #endregion

        #region 指令面板

        private void DrawCommandSection(SpriteBatch sb, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //标签
            string label = CommandLabel.Value;
            Vector2 labelPos = new(commandRect.X + 8, commandRect.Y + 4);
            Utils.DrawBorderString(sb, label, labelPos, new Color(200, 180, 120) * (alpha * 0.85f), 0.72f);

            string[] cmdNames = [CmdFollow.Value, CmdHold.Value, CmdAggressive.Value, CmdDefensive.Value];

            for (int i = 0; i < 4; i++) {
                Rectangle btn = commandBtnRects[i];
                bool isActive = (int)HeroData.CurrentCommand == i;
                bool isHovering = hoveringCommandIndex == i;

                //按钮底色
                Color btnBg;
                if (isActive) {
                    float activePulse = MathF.Sin(shimmerTimer * 1.3f + i) * 0.3f + 0.7f;
                    btnBg = new Color(30, 25, 50) * (alpha * 0.9f * activePulse);
                }
                else {
                    btnBg = new Color(12, 10, 22) * (alpha * 0.75f);
                }
                sb.Draw(px, btn, new Rectangle(0, 0, 1, 1), btnBg);

                //按钮边框
                Color btnBorder = isActive
                    ? new Color(220, 190, 100) * (alpha * 0.7f)
                    : new Color(80, 70, 50) * (alpha * 0.45f);
                if (isHovering && !isActive) {
                    btnBorder = new Color(160, 140, 80) * (alpha * 0.6f);
                }
                sb.Draw(px, new Rectangle(btn.X, btn.Y, btn.Width, 1), new Rectangle(0, 0, 1, 1), btnBorder);
                sb.Draw(px, new Rectangle(btn.X, btn.Bottom - 1, btn.Width, 1), new Rectangle(0, 0, 1, 1), btnBorder * 0.65f);
                sb.Draw(px, new Rectangle(btn.X, btn.Y, 1, btn.Height), new Rectangle(0, 0, 1, 1), btnBorder * 0.8f);
                sb.Draw(px, new Rectangle(btn.Right - 1, btn.Y, 1, btn.Height), new Rectangle(0, 0, 1, 1), btnBorder * 0.8f);

                //悬停高亮
                if (isHovering) {
                    sb.Draw(px, btn, new Rectangle(0, 0, 1, 1), new Color(255, 220, 120) * (alpha * 0.08f));
                }

                //活跃指示小光点
                if (isActive) {
                    Vector2 dotPos = new(btn.X + 6, btn.Center.Y);
                    Color dotColor = new Color(255, 220, 100) * (alpha * 0.8f);
                    sb.Draw(px, dotPos, null, dotColor, 0f, new Vector2(0.5f),
                        new Vector2(3f), SpriteEffects.None, 0f);
                }

                //文字
                Color textColor = isActive
                    ? new Color(255, 240, 200) * (alpha * 0.95f)
                    : new Color(160, 145, 110) * (alpha * 0.75f);
                Vector2 textSize = FontAssets.MouseText.Value.MeasureString(cmdNames[i]) * 0.68f;
                Vector2 textPos = new(btn.Center.X - textSize.X * 0.5f, btn.Center.Y - textSize.Y * 0.5f);
                Utils.DrawBorderString(sb, cmdNames[i], textPos, textColor, 0.68f);
            }
        }


        #endregion

        #endregion

        #endregion

        #region 工具函数

        /// <summary>
        /// 英雄面板独有的六芒星标记——与对话框的四芒星区分
        /// </summary>
        private static void DrawHeroStarMark(SpriteBatch sb, Vector2 pos, float a, float size) {
            Texture2D px = VaultAsset.placeholder2.Value;
            Color c = new Color(180, 200, 255) * a;

            //六芒星：三对轴
            for (int i = 0; i < 3; i++) {
                float angle = MathHelper.TwoPi * i / 3f;
                sb.Draw(px, pos, null, c * (0.8f - i * 0.1f), angle, new Vector2(0.5f),
                    new Vector2(size * 1.2f, size * 0.18f), SpriteEffects.None, 0f);
            }

            //中心光点
            Color bright = new Color(255, 245, 220) * (a * 0.6f);
            sb.Draw(px, pos, null, bright, 0f, new Vector2(0.5f),
                new Vector2(size * 0.3f), SpriteEffects.None, 0f);
        }

        /// <summary>
        /// 渐变线条辅助
        /// </summary>
        private static void DrawGradientLine(SpriteBatch sb, Vector2 start, Vector2 end, Color startColor, Color endColor, float thickness) {
            Texture2D px = VaultAsset.placeholder2.Value;
            Vector2 edge = end - start;
            float length = edge.Length();
            if (length < 1f) return;

            edge.Normalize();
            float rotation = MathF.Atan2(edge.Y, edge.X);
            int segments = Math.Max(1, (int)(length / 11f));

            for (int i = 0; i < segments; i++) {
                float t = i / (float)segments;
                Vector2 segPos = start + edge * (length * t);
                float segLength = length / segments;
                Color color = Color.Lerp(startColor, endColor, t);
                sb.Draw(px, segPos, new Rectangle(0, 0, 1, 1), color, rotation, new Vector2(0, 0.5f),
                    new Vector2(segLength, thickness), SpriteEffects.None, 0f);
            }
        }

        #endregion
    }
}
