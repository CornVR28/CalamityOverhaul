using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace CalamityOverhaul.Content.ADV.QuestManager
{
    /// <summary>
    /// 任务管理器界面样式接口<br/>
    /// 定义了任务管理器面板所有可视化元素的绘制契约，
    /// 允许不同的视觉风格实现（嘉登科幻、硫磺海、硫火等）
    /// </summary>
    internal interface IQuestManagerStyle
    {
        #region 生命周期

        /// <summary>
        /// 每逻辑帧更新样式内部动画计时器和粒子
        /// </summary>
        void Update(Rectangle panelRect, float openProgress);

        /// <summary>
        /// 重置所有样式状态（切换样式时调用）
        /// </summary>
        void Reset();

        #endregion

        #region 面板级绘制

        /// <summary>
        /// 绘制面板背景（渐变、纹理、粒子底层）
        /// </summary>
        void DrawPanelBackground(SpriteBatch sb, Rectangle panelRect, float alpha);

        /// <summary>
        /// 绘制面板边框（装饰线、角标等）
        /// </summary>
        void DrawPanelFrame(SpriteBatch sb, Rectangle panelRect, float alpha);

        /// <summary>
        /// 绘制面板顶部标题栏区域（标题、装饰、关闭按钮背景等）
        /// </summary>
        void DrawHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha);

        /// <summary>
        /// 绘制分类选项卡栏（全部/进行中/已完成 等过滤标签）
        /// </summary>
        void DrawCategoryTabs(SpriteBatch sb, Rectangle tabRect, string[] categories,
            int selectedIndex, float alpha);

        /// <summary>
        /// 绘制滚动条轨道和滑块
        /// </summary>
        void DrawScrollbar(SpriteBatch sb, Rectangle trackRect, float scrollRatio,
            float viewRatio, float alpha);

        /// <summary>
        /// 绘制面板底部状态栏（任务统计、页码等）
        /// </summary>
        void DrawFooter(SpriteBatch sb, Rectangle footerRect, int totalQuests,
            int activeQuests, float alpha);

        #endregion

        #region 任务条目绘制

        /// <summary>
        /// 绘制单个任务条目
        /// </summary>
        /// <param name="sb">SpriteBatch</param>
        /// <param name="entryRect">条目区域</param>
        /// <param name="entry">任务数据</param>
        /// <param name="isSelected">是否为当前选中</param>
        /// <param name="isHovered">是否鼠标悬停</param>
        /// <param name="alpha">透明度</param>
        /// <param name="entryIndex">条目在列表中的索引</param>
        void DrawQuestEntry(SpriteBatch sb, Rectangle entryRect, QuestEntryData entry,
            bool isSelected, bool isHovered, float alpha, int entryIndex);

        /// <summary>
        /// 绘制条目之间的分隔线
        /// </summary>
        void DrawEntrySeparator(SpriteBatch sb, Vector2 start, Vector2 end, float alpha);

        #endregion

        #region 颜色与度量

        /// <summary>获取面板背景阴影颜色</summary>
        Color GetShadowColor(float alpha);

        /// <summary>获取标题文字颜色</summary>
        Color GetHeaderTextColor(float alpha);

        /// <summary>根据任务状态获取对应的状态颜色</summary>
        Color GetStatusColor(QuestEntryStatus status, float alpha);

        /// <summary>获取单个任务条目的高度</summary>
        int GetEntryHeight();

        /// <summary>获取条目内边距</summary>
        int GetEntryPadding();

        #endregion

        #region 特效

        /// <summary>
        /// 绘制面板级粒子/特效层（背景粒子、扫描线等）
        /// </summary>
        void DrawParticles(SpriteBatch sb, Rectangle panelRect, float alpha);

        /// <summary>
        /// 绘制面板级前景特效层（全息覆盖、故障条等）
        /// </summary>
        void DrawOverlayEffects(SpriteBatch sb, Rectangle panelRect, float alpha);

        #endregion
    }
}
