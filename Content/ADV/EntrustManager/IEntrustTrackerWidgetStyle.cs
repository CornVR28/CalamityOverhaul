using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.ADV.EntrustManager
{
    /// <summary>
    /// 委托在屏幕左侧常驻追踪窗口中的显示样式，
    /// 各任务线可自定义此窗口的视觉风格
    /// </summary>
    internal interface IEntrustTrackerWidgetStyle
    {
        #region 生命周期

        /// <summary>每帧更新样式内部动画/粒子</summary>
        void Update(Rectangle widgetRect, float slideProgress);

        /// <summary>重置样式状态</summary>
        void Reset();

        #endregion

        #region 面板绘制

        /// <summary>绘制追踪窗口的背景</summary>
        void DrawWidgetBackground(SpriteBatch sb, Rectangle rect, float alpha);

        /// <summary>绘制追踪窗口的边框/装饰</summary>
        void DrawWidgetFrame(SpriteBatch sb, Rectangle rect, float alpha);

        /// <summary>绘制追踪窗口的标题区域</summary>
        void DrawWidgetHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha);

        /// <summary>绘制追踪窗口的进度条</summary>
        void DrawWidgetProgress(SpriteBatch sb, Rectangle barRect, float progress,
            string progressText, float alpha);

        /// <summary>绘制分隔线</summary>
        void DrawWidgetDivider(SpriteBatch sb, Vector2 start, Vector2 end, float alpha);

        /// <summary>绘制追踪窗口的前景特效覆盖层</summary>
        void DrawWidgetOverlay(SpriteBatch sb, Rectangle rect, float alpha);

        #endregion

        #region 颜色

        /// <summary>获取标题颜色</summary>
        Color GetWidgetTitleColor(float alpha);

        /// <summary>获取正文颜色</summary>
        Color GetWidgetTextColor(float alpha);

        /// <summary>获取数值/进度颜色</summary>
        Color GetWidgetAccentColor(float alpha);

        #endregion

        #region 度量

        /// <summary>获取追踪窗口的首选宽度（null 则使用默认 220px）</summary>
        int? GetPreferredWidth();

        /// <summary>获取追踪窗口的最小高度（null 则使用默认 90px）</summary>
        int? GetMinHeight();

        /// <summary>
        /// 根据条目当前状态返回紧凑高度（null 则不使用紧凑模式）。<br/>
        /// 由样式自行判定何时启用紧凑模式（如待机/已完成/冷却中等），
        /// 框架不预设空闲条件，完全交由样式决定
        /// </summary>
        int? GetIdleCompactHeight(QuestEntryData entry) => null;

        /// <summary>
        /// 返回该追踪窗口的紧凑可见度，0=完全收起（滑出屏幕），1=完全展开。<br/>
        /// 由样式自行判定收起条件并在Update中维护动画进度，
        /// 框架仅使用该值来控制滑入/滑出偏移。默认始终展开
        /// </summary>
        float GetCompactVisibility(QuestEntryData entry) => 1f;

        #endregion
    }
}
