using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.ADV.QuestManager
{
    /// <summary>
    /// 委托在屏幕左侧常驻追踪窗口中的显示样式，
    /// 各任务线可自定义此窗口的视觉风格
    /// </summary>
    internal interface IQuestTrackerWidgetStyle
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

        #endregion
    }
}
