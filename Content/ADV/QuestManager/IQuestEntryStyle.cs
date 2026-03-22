using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.ADV.QuestManager
{
    /// <summary>
    /// 委托条目在管理器列表中的自定义显示样式，
    /// 各任务线可提供独立实现来定义自己的视觉风格，
    /// 若委托未提供则由 <see cref="IQuestManagerStyle"/> 的默认绘制接管
    /// </summary>
    internal interface IQuestEntryStyle
    {
        /// <summary>每帧更新动画计时器</summary>
        void Update();

        /// <summary>
        /// 绘制条目自定义背景，返回true表示完全接管背景绘制
        /// </summary>
        bool DrawEntryBackground(SpriteBatch sb, Rectangle entryRect, QuestEntryData entry,
            bool isSelected, bool isHovered, float alpha);

        /// <summary>
        /// 绘制标题左侧图标装饰，返回标题文字应右移的像素数
        /// </summary>
        float DrawEntryIcon(SpriteBatch sb, Vector2 titlePos, QuestEntryData entry, float alpha);

        /// <summary>绘制条目前景特效覆盖层</summary>
        void DrawEntryOverlay(SpriteBatch sb, Rectangle entryRect, QuestEntryData entry, float alpha);

        /// <summary>获取条目左侧状态色带颜色</summary>
        Color GetAccentColor(QuestEntryStatus status, float alpha);

        /// <summary>获取条目标题颜色</summary>
        Color GetTitleColor(QuestEntryStatus status, float alpha);

        /// <summary>自定义条目高度，返回null则使用容器默认高度</summary>
        int? GetCustomEntryHeight();

        /// <summary>重置样式状态</summary>
        void Reset();
    }
}
