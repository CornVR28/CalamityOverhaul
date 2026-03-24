using InnoVault.UIHandles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberwares.UIs
{
    /// <summary>
    ///赛博朋克2077风格义体管理界面
    ///深色科幻风格，中心为程序化像素人体，周围分布义体槽位
    /// </summary>
    internal class CyberwareUI : UIHandle, ILocalizedModType
    {
        public string LocalizationCategory => "UI";

        #region 本地化文本

        public static LocalizedText TitleText { get; private set; }
        public static LocalizedText StatusText { get; private set; }
        public static LocalizedText SlotSelectedText { get; private set; }
        public static LocalizedText SlotEmptyText { get; private set; }

        //按顺序对应CyberSlotRenderer.Definitions的12个槽位
        private static LocalizedText[] slotLabelTexts;

        //缓存的槽位标签字符串数组，避免每帧重复取值
        private readonly string[] slotLabelCache = new string[12];

        public override void SetStaticDefaults() {
            TitleText = this.GetLocalization(nameof(TitleText), () => "CYBERWARE");
            StatusText = this.GetLocalization(nameof(StatusText), () => "SYSTEM STATUS: OPERATIONAL");
            SlotSelectedText = this.GetLocalization(nameof(SlotSelectedText), () => "[ SELECTED ]");
            SlotEmptyText = this.GetLocalization(nameof(SlotEmptyText), () => "EMPTY");

            slotLabelTexts = [
                this.GetLocalization("Slot_FrontalCortex", () => "FRONTAL CORTEX"),
                this.GetLocalization("Slot_OcularSystem", () => "OCULAR SYSTEM"),
                this.GetLocalization("Slot_LeftArm", () => "LEFT ARM"),
                this.GetLocalization("Slot_Hands", () => "HANDS"),
                this.GetLocalization("Slot_LeftLeg", () => "LEFT LEG"),
                this.GetLocalization("Slot_Feet", () => "FEET"),
                this.GetLocalization("Slot_OperatingSystem", () => "OPERATING SYSTEM"),
                this.GetLocalization("Slot_CirculatorySystem", () => "CIRCULATORY SYS"),
                this.GetLocalization("Slot_RightArm", () => "RIGHT ARM"),
                this.GetLocalization("Slot_Skeleton", () => "SKELETON"),
                this.GetLocalization("Slot_RightLeg", () => "RIGHT LEG"),
                this.GetLocalization("Slot_NervousSystem", () => "NERVOUS SYSTEM"),
            ];
        }

        #endregion

        #region 子渲染器

        private readonly CyberBodyRenderer bodyRenderer = new();
        private readonly CyberPanelRenderer panelRenderer = new();
        private readonly CyberSlotRenderer slotRenderer = new();
        private readonly CyberDataParticleSystem particleSystem = new();

        #endregion

        #region 状态

        private bool isOpen;
        private float openProgress;
        private float globalTimer;
        private float dataStreamPhase;
        private Rectangle panelRect;
        private Vector2 panelCenter;
        private Vector2 bodyOrigin;

        #endregion

        #region 属性

        public static CyberwareUI Instance => UIHandleLoader.GetUIHandleOfType<CyberwareUI>();

        public override bool Active => isOpen || openProgress > 0.01f;

        #endregion

        #region 公共接口

        /// <summary>
        ///打开义体界面
        /// </summary>
        public void Open() {
            if (!isOpen) {
                isOpen = true;
                panelRenderer.TriggerGlitch(0.8f);
            }
        }

        /// <summary>
        ///关闭义体界面
        /// </summary>
        public void Close() {
            if (isOpen) {
                isOpen = false;
                panelRenderer.TriggerGlitch(0.4f);
            }
        }

        /// <summary>
        ///切换义体界面的开关状态
        /// </summary>
        public void Toggle() {
            if (isOpen) Close();
            else Open();
        }

        #endregion

        #region 更新

        public override void Update() {
            //开关过渡动画
            float targetProgress = isOpen ? 1f : 0f;
            openProgress += (targetProgress - openProgress) * 0.12f;
            if (!isOpen && openProgress < 0.01f) openProgress = 0f;
            if (openProgress < 0.01f) return;

            //推进全局计时器
            globalTimer += 0.016f;
            dataStreamPhase += 0.03f;
            if (dataStreamPhase > MathHelper.TwoPi) dataStreamPhase -= MathHelper.TwoPi;

            //计算面板布局
            float easedProgress = CWRUtils.EaseOutCubic(Math.Clamp(openProgress, 0, 1));
            float panelW = CyberwareTheme.PanelWidth * easedProgress;
            float panelH = CyberwareTheme.PanelHeight * easedProgress;
            panelCenter = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
            panelRect = new Rectangle(
                (int)(panelCenter.X - panelW / 2f),
                (int)(panelCenter.Y - panelH / 2f),
                (int)panelW, (int)panelH
            );
            bodyOrigin = panelCenter + new Vector2(0, 5);

            //更新各子渲染器
            bodyRenderer.Update();
            panelRenderer.Update();

            if (slotRenderer.UpdateInteraction(panelRect)) {
                panelRenderer.TriggerGlitch(0.3f);
            }
            slotRenderer.UpdateAnimations();

            particleSystem.Update(bodyOrigin, openProgress);

            //拦截面板区域内的游戏输入
            if (isOpen && panelRect.Contains(Main.mouseX, Main.mouseY)) {
                player.mouseInterface = true;
            }
        }

        #endregion

        #region 绘制

        public override void Draw(SpriteBatch spriteBatch) {
            if (openProgress < 0.01f) return;
            float alpha = CWRUtils.EaseOutCubic(Math.Clamp(openProgress, 0, 1));

            //刷新本地化缓存
            RefreshSlotLabelCache();

            //逐层绘制
            panelRenderer.DrawFullScreenDim(spriteBatch, alpha);
            panelRenderer.DrawBackground(spriteBatch, alpha, panelRect, panelCenter, globalTimer);
            panelRenderer.DrawGrid(spriteBatch, alpha, panelRect);
            panelRenderer.DrawScanLines(spriteBatch, alpha, panelRect);

            bodyRenderer.DrawBody(spriteBatch, alpha, bodyOrigin, globalTimer);
            bodyRenderer.DrawNodes(spriteBatch, alpha, bodyOrigin, slotRenderer.ComputeNodeStates());

            slotRenderer.DrawConnectors(spriteBatch, alpha, panelRect, bodyRenderer, bodyOrigin, dataStreamPhase);
            slotRenderer.DrawSlots(spriteBatch, alpha, panelRect, slotLabelCache,
                SlotSelectedText.Value, SlotEmptyText.Value);

            panelRenderer.DrawTitleAndDecor(spriteBatch, alpha, panelRect, panelCenter,
                globalTimer, TitleText.Value, StatusText.Value);

            particleSystem.Draw(spriteBatch, alpha);
            panelRenderer.DrawGlitchEffect(spriteBatch, alpha, panelRect);
        }

        private void RefreshSlotLabelCache() {
            for (int i = 0; i < slotLabelTexts.Length && i < slotLabelCache.Length; i++) {
                slotLabelCache[i] = slotLabelTexts[i].Value;
            }
        }

        #endregion
    }
}
