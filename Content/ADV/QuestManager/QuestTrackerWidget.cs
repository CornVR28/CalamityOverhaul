using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.QuestManager
{
    /// <summary>
    /// 被关注委托的常驻追踪窗口，位于屏幕左侧，
    /// 多个被关注任务纵向排列，管理器打开时自动隐藏
    /// </summary>
    internal class QuestTrackerWidget : UIHandle
    {
        public static QuestTrackerWidget Instance => UIHandleLoader.GetUIHandleOfType<QuestTrackerWidget>();

        #region 配置

        private const int WidgetWidth = 220;
        private const int WidgetMinHeight = 80;
        private const int WidgetMaxHeight = 160;
        private const int WidgetMarginTop = 80;
        private const int WidgetMarginLeft = 8;
        private const int WidgetSpacing = 6;
        private const float WidgetPadding = 8f;

        #endregion

        #region 状态

        /// <summary>滑入/滑出进度 0~1</summary>
        private float slideProgress;

        /// <summary>折叠进度</summary>
        private float collapseProgress;

        /// <summary>是否折叠（全部收起只显示标题条）</summary>
        private bool isCollapsed;

        /// <summary>全局动画计时器</summary>
        private float animTimer;

        /// <summary>NPC重叠时的透明度衰减</summary>
        private float overlappingAlpha = 1f;

        /// <summary>缓存的被关注条目引用</summary>
        private readonly List<QuestEntryData> trackedEntries = [];

        #endregion

        #region UIHandle 生命周期

        /// <summary>当存在被关注的委托且管理器未打开时显示</summary>
        public override bool Active {
            get {
                if (Main.gameMenu) return false;
                //动画尚未结束时保持激活
                if (slideProgress > 0.005f) return true;
                //直接查询管理器源数据，避免缓存列表导致的首帧不活跃问题
                var manager = QuestManagerUI.Instance;
                return manager != null && manager.HasTrackedEntries();
            }
        }

        public override void OnEnterWorld() {
            slideProgress = 0f;
            collapseProgress = 0f;
            isCollapsed = false;
            animTimer = 0f;
            overlappingAlpha = 1f;
            trackedEntries.Clear();
        }

        public override void Update() {
            //刷新被关注条目列表
            RefreshTrackedEntries();

            //目标状态：有被关注的条目且管理器未打开
            var manager = QuestManagerUI.Instance;
            bool shouldShow = trackedEntries.Count > 0
                && (manager == null || !manager.IsOpen);

            float targetSlide = shouldShow ? 1f : 0f;
            slideProgress = MathHelper.Lerp(slideProgress, targetSlide, 0.15f);
            if (slideProgress < 0.005f && !shouldShow) slideProgress = 0f;
            if (slideProgress > 0.995f && shouldShow) slideProgress = 1f;

            //折叠动画
            float targetCollapse = isCollapsed ? 1f : 0f;
            collapseProgress = MathHelper.Lerp(collapseProgress, targetCollapse, 0.12f);

            //动画计时器
            animTimer += 0.016f;
            if (animTimer > MathHelper.TwoPi) animTimer -= MathHelper.TwoPi;

            //更新每个条目的追踪窗口样式
            for (int i = 0; i < trackedEntries.Count; i++) {
                var widgetRect = GetWidgetRect(i);
                trackedEntries[i].TrackerStyle?.Update(widgetRect, slideProgress);
            }

            //NPC重叠检测，追踪窗口与近处NPC重叠时半透明
            UpdateOverlapAlpha();

            //鼠标交互
            hoverInMainPage = false;
            if (slideProgress > 0.3f) {
                for (int i = 0; i < trackedEntries.Count; i++) {
                    var rect = GetWidgetRect(i);
                    if (rect.Contains(Main.mouseX, Main.mouseY)) {
                        hoverInMainPage = true;
                        break;
                    }
                }
            }
        }

        #endregion

        #region 数据刷新

        private void RefreshTrackedEntries() {
            trackedEntries.Clear();
            var manager = QuestManagerUI.Instance;
            if (manager == null) return;

            manager.GetTrackedEntries(trackedEntries);
        }

        #endregion

        #region 矩形计算

        private Rectangle GetWidgetRect(int index) {
            float eased = CWRUtils.EaseOutCubic(MathHelper.Clamp(slideProgress, 0f, 1f));
            int w = WidgetWidth;
            int x = (int)MathHelper.Lerp(-w - 10f, WidgetMarginLeft, eased);

            //纵向排列
            int y = WidgetMarginTop;
            for (int i = 0; i < index; i++) {
                y += GetWidgetHeight(i) + WidgetSpacing;
            }

            int h = GetWidgetHeight(index);

            //折叠时高度缩到标题行
            float collapse = CWRUtils.EaseInOutCubic(MathHelper.Clamp(collapseProgress, 0f, 1f));
            int collapsedH = 24;
            h = (int)MathHelper.Lerp(h, collapsedH, collapse);

            return new Rectangle(x, y, w, h);
        }

        private int GetWidgetHeight(int index) {
            if (index >= trackedEntries.Count) return WidgetMinHeight;
            var entry = trackedEntries[index];
            int? custom = entry.TrackerStyle?.GetMinHeight();
            int baseH = custom ?? WidgetMinHeight;

            //根据内容行数动态调整高度
            var details = entry.GetTrackerDetails();
            int contentH = 30 + details.Count * 16; //标题 + 每行16px
            if (entry.Progress > 0f && entry.Status != QuestEntryStatus.Completed) {
                contentH += 20; //进度条
            }
            contentH += 16; //底部边距

            return Math.Clamp(contentH, baseH, WidgetMaxHeight);
        }

        #endregion

        #region 绘制

        public override void Draw(SpriteBatch spriteBatch) {
            if (slideProgress <= 0.005f) return;

            float alpha = slideProgress * overlappingAlpha;
            var font = FontAssets.MouseText.Value;

            for (int i = 0; i < trackedEntries.Count; i++) {
                var entry = trackedEntries[i];
                Rectangle widgetRect = GetWidgetRect(i);

                //可见性裁剪
                if (widgetRect.Bottom < 0 || widgetRect.Y > Main.screenHeight) continue;

                var style = entry.TrackerStyle;

                // 1. 背景 
                if (style != null) {
                    style.DrawWidgetBackground(spriteBatch, widgetRect, alpha);
                }
                else {
                    DrawDefaultBackground(spriteBatch, widgetRect, alpha);
                }

                // 2. 边框 
                if (style != null) {
                    style.DrawWidgetFrame(spriteBatch, widgetRect, alpha);
                }
                else {
                    DrawDefaultFrame(spriteBatch, widgetRect, alpha);
                }

                // 3. 标题 
                Rectangle headerRect = new(widgetRect.X, widgetRect.Y, widgetRect.Width, 24);
                if (style != null) {
                    style.DrawWidgetHeader(spriteBatch, headerRect, entry.Title ?? "", alpha);
                }
                else {
                    DrawDefaultHeader(spriteBatch, headerRect, entry.Title ?? "", alpha);
                }

                //折叠时只画标题
                if (collapseProgress > 0.95f) continue;

                float contentAlpha = alpha * (1f - collapseProgress);

                // 4. 内容区域 
                Rectangle contentRect = new(
                    widgetRect.X + (int)WidgetPadding,
                    widgetRect.Y + 26,
                    widgetRect.Width - (int)(WidgetPadding * 2),
                    widgetRect.Height - 30);

                //让条目自定义绘制
                if (!entry.DrawTrackerContent(spriteBatch, contentRect, contentAlpha)) {
                    //默认绘制：文字行 + 进度条
                    DrawDefaultContent(spriteBatch, contentRect, entry, style, contentAlpha);
                }

                // 5. 前景特效 
                style?.DrawWidgetOverlay(spriteBatch, widgetRect, alpha);
            }
        }

        #endregion

        #region 默认绘制

        private void DrawDefaultBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            //深色半透明背景
            BaseManagerStyle.FillRect(sb, rect, new Color(4, 8, 18) * (alpha * 0.85f));
        }

        private void DrawDefaultFrame(SpriteBatch sb, Rectangle rect, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            Color frameC = new Color(60, 150, 220) * (alpha * 0.4f);
            //顶部线
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 2), frameC);
            //左侧线
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 2, rect.Height), frameC * 0.6f);
            //底部线
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), frameC * 0.3f);
            //右侧线
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), frameC * 0.2f);
        }

        private void DrawDefaultHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha) {
            //标题栏背景
            BaseManagerStyle.FillRect(sb, headerRect, new Color(8, 16, 32) * (alpha * 0.5f));
            //标题文字
            Color titleC = new Color(140, 210, 255) * alpha;
            Utils.DrawBorderString(sb, title,
                new Vector2(headerRect.X + 8f, headerRect.Y + (headerRect.Height - 16f) / 2f),
                titleC, 0.72f);
            //底部分隔
            var px = VaultAsset.placeholder2.Value;
            sb.Draw(px, new Rectangle(headerRect.X + 4, headerRect.Bottom - 1, headerRect.Width - 8, 1),
                new Color(60, 150, 220) * (alpha * 0.3f));
        }

        private void DrawDefaultContent(SpriteBatch sb, Rectangle contentRect, QuestEntryData entry,
            IQuestTrackerWidgetStyle style, float alpha) {
            var font = FontAssets.MouseText.Value;
            Color textC = style?.GetWidgetTextColor(alpha) ?? new Color(160, 190, 210) * (alpha * 0.8f);
            Color accentC = style?.GetWidgetAccentColor(alpha) ?? new Color(80, 255, 220) * alpha;

            float y = contentRect.Y;

            //详细信息行
            var details = entry.GetTrackerDetails();
            foreach (string line in details) {
                if (y + 16 > contentRect.Bottom) break;
                Utils.DrawBorderString(sb, line, new Vector2(contentRect.X, y), textC, 0.6f);
                y += 16f;
            }

            //分隔线 + 进度条
            if (entry.Progress > 0f && entry.Status != QuestEntryStatus.Completed) {
                y += 3f;

                //分隔线——委托给样式
                if (style != null) {
                    style.DrawWidgetDivider(sb,
                        new Vector2(contentRect.X, y),
                        new Vector2(contentRect.Right - 4, y), alpha);
                }
                y += 4f;

                //进度条——委托给样式
                int barW = contentRect.Width - 4;
                Rectangle barRect = new(contentRect.X, (int)y, barW, 5);
                if (style != null) {
                    style.DrawWidgetProgress(sb, barRect, entry.Progress,
                        entry.ProgressText, alpha);
                }
                else {
                    BaseManagerStyle.FillRect(sb, barRect, new Color(8, 16, 32) * alpha);
                    int fillW = (int)(barW * MathHelper.Clamp(entry.Progress, 0f, 1f));
                    if (fillW > 0) {
                        BaseManagerStyle.FillRect(sb, new Rectangle(barRect.X, barRect.Y, fillW, 5), accentC * 0.8f);
                    }

                    if (entry.ProgressText != null) {
                        Utils.DrawBorderString(sb, entry.ProgressText,
                            new Vector2(barRect.Right - font.MeasureString(entry.ProgressText).X * 0.5f - 2f,
                                barRect.Bottom + 2f),
                            accentC * 0.7f, 0.5f);
                    }
                }
            }
        }

        #endregion

        #region NPC重叠透明化

        private void UpdateOverlapAlpha() {
            bool overlapping = false;
            for (int i = 0; i < trackedEntries.Count && !overlapping; i++) {
                Rectangle wRect = GetWidgetRect(i);
                for (int n = 0; n < Main.maxNPCs; n++) {
                    NPC npc = Main.npc[n];
                    if (!npc.active || npc.friendly) continue;
                    Vector2 screen = npc.Center - Main.screenPosition;
                    if (wRect.Contains((int)screen.X, (int)screen.Y)) {
                        overlapping = true;
                        break;
                    }
                }
            }

            float target = overlapping ? 0.3f : 1f;
            overlappingAlpha = MathHelper.Lerp(overlappingAlpha, target, 0.08f);
        }

        #endregion
    }
}
