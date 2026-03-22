using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace CalamityOverhaul.Content.ADV.QuestManager
{
    /// <summary>
    /// 任务条目状态
    /// </summary>
    internal enum QuestEntryStatus
    {
        /// <summary>已激活——正在进行中的任务</summary>
        Active,
        /// <summary>已关注——玩家手动置顶的任务</summary>
        Tracked,
        /// <summary>已挂起——玩家暂时搁置的任务</summary>
        Suspended,
        /// <summary>已完成——任务目标已达成</summary>
        Completed,
        /// <summary>已失败——任务失败</summary>
        Failed,
    }

    /// <summary>
    /// 任务管理器中单条委托的数据模型，
    /// 各任务线应继承此类并提供自定义的
    /// <see cref="EntryStyle"/>、<see cref="TrackerStyle"/>，
    /// 重写 <see cref="GetTrackerDetails"/> 和 <see cref="OnUpdate"/>
    /// </summary>
    internal class QuestEntryData
    {
        #region 核心数据

        /// <summary>唯一标识符</summary>
        public string Key;
        /// <summary>显示名称</summary>
        public string Title;
        /// <summary>任务简要描述</summary>
        public string Summary;
        /// <summary>所属任务线分类标签</summary>
        public string Category;
        /// <summary>当前状态</summary>
        public QuestEntryStatus Status;
        /// <summary>进度 0~1</summary>
        public float Progress;
        /// <summary>进度文本</summary>
        public string ProgressText;
        /// <summary>是否为新任务</summary>
        public bool IsNew;
        /// <summary>排序优先级，越大越靠前</summary>
        public int Priority;

        #endregion

        #region 样式系统

        /// <summary>
        /// 列表中的自定义显示样式，
        /// 为null则使用 <see cref="IQuestManagerStyle.DrawQuestEntry"/> 默认样式
        /// </summary>
        public IQuestEntryStyle EntryStyle { get; set; }

        /// <summary>
        /// 追踪窗口的自定义显示样式，为null则使用默认样式
        /// </summary>
        public IQuestTrackerWidgetStyle TrackerStyle { get; set; }

        #endregion

        #region 追踪面板内容

        /// <summary>
        /// 获取追踪面板中显示的详细内容行，
        /// 默认返回Summary单行，子类可重写以提供多行内容
        /// </summary>
        public virtual List<string> GetTrackerDetails() {
            return [Summary ?? ""];
        }

        /// <summary>
        /// 在追踪面板中绘制自定义内容区域，
        /// 返回true表示完全接管内容绘制，
        /// 返回false则使用默认文字加进度条布局
        /// </summary>
        public virtual bool DrawTrackerContent(SpriteBatch sb, Rectangle contentRect, float alpha) {
            return false;
        }

        #endregion

        #region 生命周期

        /// <summary>每帧更新数据，子类按需重写</summary>
        public virtual void OnUpdate() { }

        /// <summary>状态变化时调用，子类可重写以触发音效或动画</summary>
        public virtual void OnStatusChanged(QuestEntryStatus oldStatus, QuestEntryStatus newStatus) { }

        #endregion

        public QuestEntryData(string key, string title, string summary, string category) {
            Key = key;
            Title = title;
            Summary = summary;
            Category = category;
            Status = QuestEntryStatus.Active;
        }
    }
}
