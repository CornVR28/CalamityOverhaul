using CalamityOverhaul.Content.ADV.Extras.Styles;
using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Extras
{
    public enum ExtraTab
    {
        Gallery,
        Scene,
        Music,
        Staff
    }

    public class ExtraMain : UIHandle, ILocalizedModType
    {
        #region Data
        public const int ExtraMenuMode = 888;
        public static ExtraMain Instance => UIHandleLoader.GetUIHandleOfType<ExtraMain>();
        internal bool _active;
        public override bool Active => _active || openAlpha > 0.01f;
        public override LayersModeEnum LayersMode => LayersModeEnum.Mod_MenuLoad;

        public IExtraStyle CurrentStyle { get; private set; }
        public float PanelAlpha => openAlpha;

        private float openAlpha;
        private ExtraTab activeTab = ExtraTab.Gallery;
        private Rectangle panelRect;

        //CG网格相关
        private float scrollOffset;
        private float maxScroll;
        private List<CGEntry> sortedEntries = [];
        private int hoveredCGIndex = -1;
        private const int Columns = 7;
        private const int ThumbGap = 8;

        //全屏查看器
        private bool isFullscreenView;
        private int fullscreenCGIndex = -1;
        private float fullscreenAlpha;

        //滚动条拖拽
        private bool isDraggingScrollbar;
        private float dragScrollStartY;
        private float dragScrollStartOffset;
        private int oldScrollWheelValue;

        public string LocalizationCategory => "UI";
        public static LocalizedText ExtraText;
        public static LocalizedText GalleryText;
        public static LocalizedText SceneText;
        public static LocalizedText MusicText;
        public static LocalizedText StaffText;
        public static LocalizedText BackText;
        public static LocalizedText LockedText;
        #endregion

        public override void SetStaticDefaults() {
            ExtraText = this.GetLocalization(nameof(ExtraText), () => "EXTRA");
            GalleryText = this.GetLocalization(nameof(GalleryText), () => "GALLERY");
            SceneText = this.GetLocalization(nameof(SceneText), () => "SCENE");
            MusicText = this.GetLocalization(nameof(MusicText), () => "MUSIC");
            StaffText = this.GetLocalization(nameof(StaffText), () => "STAFF");
            BackText = this.GetLocalization(nameof(BackText), () => "BACK");
            LockedText = this.GetLocalization(nameof(LockedText), () => "???");
        }

        public override void Load() {
            CurrentStyle = new HotwindExtraStyle();
            openAlpha = 0;
        }

        public static void OnOpen() {
            if (Main.menuMode == ExtraMenuMode) {
                SoundEngine.PlaySound(SoundID.Unlock);
                return;
            }
            Main.menuMode = ExtraMenuMode;
            var instance = Instance;
            if (instance != null) {
                instance._active = true;
            }
        }

        public void OnClose() {
            SoundEngine.PlaySound(SoundID.MenuClose);
            Main.menuMode = 0;
            _active = false;
        }

        public static bool OnActive() {
            var instance = Instance;
            return instance != null && (instance._active || instance.openAlpha > 0.01f);
        }

        public override void UnLoad() {
            CurrentStyle = null;
            sortedEntries = null;
        }

        //刷新排序后的CG条目列表
        private void RefreshSortedEntries() {
            sortedEntries = CGEntry.AllEntries?.OrderBy(e => e.SortOrder).ToList() ?? [];
        }

        public override void MenuLogicUpdate() {
            CurrentStyle?.UpdateStyle();
        }

        public override void Update() {
            if (_active) {
                openAlpha = MathHelper.Lerp(openAlpha, 1f, 0.24f);
            }
            else {
                openAlpha = MathHelper.Lerp(openAlpha, 0f, 0.24f);
                if (openAlpha < 0.01f) return;
            }

            //全屏覆盖
            panelRect = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);

            UIHitBox = panelRect;
            hoverInMainPage = UIHitBox.Intersects(MouseHitBox);

            //全屏查看器动画
            if (isFullscreenView) {
                if (fullscreenAlpha < 1f) fullscreenAlpha = MathHelper.Lerp(fullscreenAlpha, 1f, 0.15f);
            }
            else {
                if (fullscreenAlpha > 0f) fullscreenAlpha = MathHelper.Lerp(fullscreenAlpha, 0f, 0.15f);
            }

            //ESC分层关闭
            if (_active && Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape)) {
                if (isFullscreenView) {
                    isFullscreenView = false;
                    SoundEngine.PlaySound(SoundID.MenuClose);
                }
                else {
                    OnClose();
                }
                return;
            }

            //全屏模式下的交互
            if (isFullscreenView && fullscreenAlpha > 0.5f) {
                UpdateFullscreenViewer();
                return;
            }

            //刷新条目
            if (sortedEntries == null || sortedEntries.Count != (CGEntry.AllEntries?.Count ?? 0)) {
                RefreshSortedEntries();
            }

            //返回按钮
            Rectangle backRect = CurrentStyle.GetBackButtonRect(panelRect);
            bool hoverBack = backRect.Contains(MousePosition.ToPoint());
            if (hoverBack && keyLeftPressState == KeyPressState.Pressed) {
                OnClose();
                return;
            }

            //标签栏点击
            UpdateTabBarInput();

            //Gallery标签内容
            if (activeTab == ExtraTab.Gallery) {
                UpdateGalleryInput();
            }
        }

        private void UpdateTabBarInput() {
            Rectangle tabBarRect = CurrentStyle.GetTabBarRect(panelRect);
            string[] tabNames = [GalleryText.Value, SceneText.Value, MusicText.Value, StaffText.Value];
            ExtraTab[] tabs = [ExtraTab.Gallery, ExtraTab.Scene, ExtraTab.Music, ExtraTab.Staff];
            var font = FontAssets.MouseText.Value;

            float totalWidth = 0;
            float[] widths = new float[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++) {
                widths[i] = font.MeasureString(tabNames[i]).X + 30;
                totalWidth += widths[i];
            }

            float startX = tabBarRect.Right - totalWidth;
            for (int i = 0; i < tabNames.Length; i++) {
                Rectangle tabRect = new Rectangle((int)startX, tabBarRect.Y, (int)widths[i], tabBarRect.Height);
                if (tabRect.Contains(MousePosition.ToPoint()) && keyLeftPressState == KeyPressState.Pressed) {
                    //只有Gallery可用，其他标签不可点击
                    if (tabs[i] == ExtraTab.Gallery) {
                        activeTab = tabs[i];
                        SoundEngine.PlaySound(SoundID.MenuTick);
                    }
                }
                startX += widths[i];
            }
        }

        private void UpdateGalleryInput() {
            Vector4 padding = CurrentStyle.GetPadding();
            int contentX = panelRect.X + (int)padding.X;
            int contentY = panelRect.Y + (int)padding.Y + 50; //标题+标签栏高度
            int contentW = panelRect.Width - (int)padding.X - (int)padding.Z - 20; //留出滚动条宽度
            int contentH = panelRect.Height - (int)padding.Y - (int)padding.W - 50;

            int thumbW = (contentW - ThumbGap * (Columns - 1)) / Columns;
            int thumbH = thumbW * 9 / 16;
            int totalRows = sortedEntries.Count > 0 ? (int)Math.Ceiling(sortedEntries.Count / (float)Columns) : 1;
            float contentTotalH = totalRows * (thumbH + ThumbGap) - ThumbGap;
            maxScroll = Math.Max(0, contentTotalH - contentH);

            //滚轮滚动
            if (hoverInMainPage) {
                int scroll = Mouse.GetState().ScrollWheelValue;
                if (scroll != oldScrollWheelValue) {
                    scrollOffset -= (scroll - oldScrollWheelValue) * 0.3f;
                    scrollOffset = MathHelper.Clamp(scrollOffset, 0, maxScroll);
                }
                oldScrollWheelValue = scroll;
            }

            //滚动条拖拽
            if (maxScroll > 0) {
                Rectangle scrollTrack = new Rectangle(
                    panelRect.Right - (int)padding.Z - 12,
                    contentY, 8, contentH);

                float viewRatio = contentH / (contentTotalH > 0 ? contentTotalH : 1);
                float thumbHeight = Math.Max(20, scrollTrack.Height * viewRatio);
                float scrollRatio = maxScroll > 0 ? scrollOffset / maxScroll : 0;
                float thumbY = scrollTrack.Y + (scrollTrack.Height - thumbHeight) * scrollRatio;
                Rectangle scrollThumb = new Rectangle(scrollTrack.X, (int)thumbY, scrollTrack.Width, (int)thumbHeight);

                if (keyLeftPressState == KeyPressState.Pressed && scrollThumb.Contains(MousePosition.ToPoint())) {
                    isDraggingScrollbar = true;
                    dragScrollStartY = MousePosition.Y;
                    dragScrollStartOffset = scrollOffset;
                }
                if (isDraggingScrollbar && keyLeftPressState == KeyPressState.Held) {
                    float deltaY = MousePosition.Y - dragScrollStartY;
                    float trackRange = scrollTrack.Height - thumbHeight;
                    if (trackRange > 0) {
                        scrollOffset = dragScrollStartOffset + deltaY / trackRange * maxScroll;
                        scrollOffset = MathHelper.Clamp(scrollOffset, 0, maxScroll);
                    }
                }
                if (keyLeftPressState == KeyPressState.Released) {
                    isDraggingScrollbar = false;
                }
            }

            //CG缩略图悬浮/点击
            hoveredCGIndex = -1;
            for (int i = 0; i < sortedEntries.Count; i++) {
                int row = i / Columns;
                int col = i % Columns;
                float itemY = contentY + row * (thumbH + ThumbGap) - scrollOffset;
                if (itemY + thumbH < contentY || itemY > contentY + contentH) continue;

                Rectangle thumbRect = new Rectangle(
                    contentX + col * (thumbW + ThumbGap),
                    (int)itemY, thumbW, thumbH);

                if (thumbRect.Contains(MousePosition.ToPoint())) {
                    hoveredCGIndex = i;
                    if (keyLeftPressState == KeyPressState.Pressed && sortedEntries[i].IsUnlocked) {
                        fullscreenCGIndex = i;
                        isFullscreenView = true;
                        SoundEngine.PlaySound(SoundID.MenuTick);
                    }
                    break;
                }
            }
        }

        private void UpdateFullscreenViewer() {
            //点击遮罩区域退出
            if (keyLeftPressState == KeyPressState.Pressed) {
                Rectangle leftArrow = CurrentStyle.GetLeftArrowRect();
                Rectangle rightArrow = CurrentStyle.GetRightArrowRect();
                if (!leftArrow.Contains(MousePosition.ToPoint()) && !rightArrow.Contains(MousePosition.ToPoint())) {
                    //点中的不是箭头，检查是否在CG图片外
                    isFullscreenView = false;
                    SoundEngine.PlaySound(SoundID.MenuClose);
                    return;
                }
            }

            //左右箭头/键盘切换
            bool goLeft = false, goRight = false;

            Rectangle leftRect = CurrentStyle.GetLeftArrowRect();
            Rectangle rightRect = CurrentStyle.GetRightArrowRect();
            if (leftRect.Contains(MousePosition.ToPoint()) && keyLeftPressState == KeyPressState.Pressed) goLeft = true;
            if (rightRect.Contains(MousePosition.ToPoint()) && keyLeftPressState == KeyPressState.Pressed) goRight = true;

            if (Main.keyState.IsKeyDown(Keys.A) && Main.oldKeyState.IsKeyUp(Keys.A)) goLeft = true;
            if (Main.keyState.IsKeyDown(Keys.D) && Main.oldKeyState.IsKeyUp(Keys.D)) goRight = true;
            if (Main.keyState.IsKeyDown(Keys.Left) && Main.oldKeyState.IsKeyUp(Keys.Left)) goLeft = true;
            if (Main.keyState.IsKeyDown(Keys.Right) && Main.oldKeyState.IsKeyUp(Keys.Right)) goRight = true;

            if (goLeft) NavigateFullscreen(-1);
            if (goRight) NavigateFullscreen(1);
        }

        private void NavigateFullscreen(int direction) {
            if (sortedEntries.Count == 0) return;
            int start = fullscreenCGIndex;
            int current = start;
            for (int attempt = 0; attempt < sortedEntries.Count; attempt++) {
                current += direction;
                if (current < 0) current = sortedEntries.Count - 1;
                if (current >= sortedEntries.Count) current = 0;
                if (current == start) return;
                if (sortedEntries[current].IsUnlocked) {
                    fullscreenCGIndex = current;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    return;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (openAlpha < 0.01f) return;

            //面板背景
            CurrentStyle.DrawBackground(spriteBatch, this, panelRect);

            //标题
            CurrentStyle.DrawTitle(spriteBatch, this, panelRect, openAlpha);

            //标签栏
            CurrentStyle.DrawTabBar(spriteBatch, this, panelRect, activeTab, openAlpha);

            //Gallery内容
            if (activeTab == ExtraTab.Gallery) {
                DrawGalleryContent(spriteBatch);
            }

            //返回按钮
            Rectangle backRect = CurrentStyle.GetBackButtonRect(panelRect);
            bool hoverBack = backRect.Contains(MousePosition.ToPoint());
            CurrentStyle.DrawBackButton(spriteBatch, panelRect, hoverBack, openAlpha);

            //全屏查看器
            if (fullscreenAlpha > 0.01f && fullscreenCGIndex >= 0 && fullscreenCGIndex < sortedEntries.Count) {
                CurrentStyle.DrawFullscreenViewer(spriteBatch, sortedEntries[fullscreenCGIndex], fullscreenAlpha);

                //左右箭头
                Rectangle leftRect = CurrentStyle.GetLeftArrowRect();
                Rectangle rightRect = CurrentStyle.GetRightArrowRect();
                bool hoverLeft = leftRect.Contains(MousePosition.ToPoint());
                bool hoverRight = rightRect.Contains(MousePosition.ToPoint());
                CurrentStyle.DrawViewerArrow(spriteBatch, leftRect, true, hoverLeft, fullscreenAlpha);
                CurrentStyle.DrawViewerArrow(spriteBatch, rightRect, false, hoverRight, fullscreenAlpha);
            }
        }

        private void DrawGalleryContent(SpriteBatch spriteBatch) {
            Vector4 padding = CurrentStyle.GetPadding();
            int contentX = panelRect.X + (int)padding.X;
            int contentY = panelRect.Y + (int)padding.Y + 50;
            int contentW = panelRect.Width - (int)padding.X - (int)padding.Z - 20;
            int contentH = panelRect.Height - (int)padding.Y - (int)padding.W - 50;

            int thumbW = (contentW - ThumbGap * (Columns - 1)) / Columns;
            int thumbH = thumbW * 9 / 16;

            //ScissorRect裁剪，需要通过UIScaleMatrix变换到屏幕像素坐标
            Rectangle clipRect = new Rectangle(contentX, contentY, contentW + 20, contentH);
            Rectangle prevScissor = spriteBatch.GraphicsDevice.ScissorRectangle;
            RasterizerState prevRasterizer = spriteBatch.GraphicsDevice.RasterizerState;
            spriteBatch.End();
            Rectangle safeClip = Rectangle.Intersect(clipRect, spriteBatch.GraphicsDevice.Viewport.Bounds);
            spriteBatch.GraphicsDevice.ScissorRectangle = VaultUtils.GetClippingRectangle(spriteBatch, safeClip);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.None,
                new RasterizerState { ScissorTestEnable = true }, null, Main.UIScaleMatrix);

            //绘制CG缩略图网格
            for (int i = 0; i < sortedEntries.Count; i++) {
                int row = i / Columns;
                int col = i % Columns;
                float itemY = contentY + row * (thumbH + ThumbGap) - scrollOffset;
                if (itemY + thumbH < contentY || itemY > contentY + contentH) continue;

                Rectangle thumbRect = new Rectangle(
                    contentX + col * (thumbW + ThumbGap),
                    (int)itemY, thumbW, thumbH);

                bool isHovered = (hoveredCGIndex == i);
                if (sortedEntries[i].IsUnlocked) {
                    CurrentStyle.DrawCGThumbnail(spriteBatch, sortedEntries[i], thumbRect, isHovered, openAlpha);
                }
                else {
                    CurrentStyle.DrawLockedCG(spriteBatch, thumbRect, openAlpha);
                }
            }

            //如果没有任何CG条目，绘制一些占位网格来展示布局
            if (sortedEntries.Count == 0) {
                int placeholderCount = Columns * 4;
                for (int i = 0; i < placeholderCount; i++) {
                    int row = i / Columns;
                    int col = i % Columns;
                    float itemY = contentY + row * (thumbH + ThumbGap) - scrollOffset;
                    if (itemY + thumbH < contentY || itemY > contentY + contentH) continue;

                    Rectangle thumbRect = new Rectangle(
                        contentX + col * (thumbW + ThumbGap),
                        (int)itemY, thumbW, thumbH);
                    CurrentStyle.DrawLockedCG(spriteBatch, thumbRect, openAlpha);
                }
            }

            //恢复ScissorRect
            spriteBatch.End();
            spriteBatch.GraphicsDevice.ScissorRectangle = prevScissor;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.None,
                prevRasterizer, null, Main.UIScaleMatrix);

            //滚动条
            if (maxScroll > 0) {
                int totalRows = (int)Math.Ceiling((sortedEntries.Count > 0 ? sortedEntries.Count : Columns * 4) / (float)Columns);
                float contentTotalH = totalRows * (thumbH + ThumbGap) - ThumbGap;
                float viewRatio = contentH / (contentTotalH > 0 ? contentTotalH : 1);
                float scrollRatio = maxScroll > 0 ? scrollOffset / maxScroll : 0;

                Rectangle scrollTrack = new Rectangle(
                    panelRect.Right - (int)padding.Z - 12,
                    contentY, 8, contentH);
                CurrentStyle.DrawScrollbar(spriteBatch, scrollTrack, scrollRatio, viewRatio, openAlpha);
            }
        }

        //获取Gallery解锁百分比
        public float GetGalleryProgress() {
            if (sortedEntries == null || sortedEntries.Count == 0) return 0;
            int unlocked = sortedEntries.Count(e => e.IsUnlocked);
            return (float)unlocked / sortedEntries.Count;
        }
    }
}
