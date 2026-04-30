using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria.Localization;

namespace CalamityOverhaul.Content.ADV.EntrustManager
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
    /// 所有文本字段通过 <see cref="LocalizedText"/> 实现本地化，
    /// 各任务线应继承此类并提供自定义的
    /// <see cref="EntryStyle"/>、<see cref="TrackerStyle"/>，
    /// 重写 <see cref="GetTrackerDetails"/> 和 <see cref="OnUpdate"/>
    /// </summary>
    internal class EntrustEntryData
    {
        #region 核心数据

        /// <summary>唯一标识符</summary>
        public string Key;

        /// <summary>显示名称（本地化）</summary>
        public LocalizedText TitleText;
        /// <summary>任务简要描述（本地化）</summary>
        public LocalizedText SummaryText;
        /// <summary>所属任务线分类标签（本地化）</summary>
        public LocalizedText CategoryText;
        /// <summary>进度文本（本地化），为null时不显示进度文本</summary>
        public LocalizedText ProgressLabel;

        /// <summary>显示名称，运行时从 <see cref="TitleText"/> 解析</summary>
        public string Title => TitleText?.Value ?? "";
        /// <summary>任务简要描述，运行时从 <see cref="SummaryText"/> 解析</summary>
        public string Summary => SummaryText?.Value ?? "";
        /// <summary>分类标签，运行时从 <see cref="CategoryText"/> 解析</summary>
        public string Category => CategoryText?.Value ?? "";
        /// <summary>进度文本，运行时从 <see cref="ProgressLabel"/> 解析，null表示无进度文本</summary>
        public string ProgressText => ProgressLabel?.Value;

        /// <summary>当前状态</summary>
        public QuestEntryStatus Status;
        /// <summary>从关注状态挂起时记录，用于恢复时回到关注而不是普通激活</summary>
        public bool RestoreTrackedOnUnsuspend;
        /// <summary>进度 0~1</summary>
        public float Progress;
        /// <summary>是否为新任务</summary>
        public bool IsNew = false;
        /// <summary>排序优先级，越大越靠前</summary>
        public int Priority;

        #endregion

        #region 展开状态（由QuestManagerUI管理）

        /// <summary>条目在管理器列表中是否展开显示完整描述</summary>
        public bool IsExpanded;
        /// <summary>展开/折叠动画进度 0~1，由管理器每帧插值</summary>
        public float ExpandProgress;

        #endregion

        #region 样式系统

        /// <summary>
        /// 列表中的自定义显示样式，
        /// 为null则使用 <see cref="IEntrustManagerStyle.DrawQuestEntry"/> 默认样式
        /// </summary>
        public IEntrustEntryStyle EntryStyle { get; set; }

        /// <summary>
        /// 追踪窗口的自定义显示样式，为null则使用默认样式
        /// </summary>
        public IEntrustTrackerWidgetStyle TrackerStyle { get; set; }

        #endregion

        #region 追踪面板内容

        /// <summary>
        /// 获取追踪面板中显示的详细内容行，
        /// 默认返回Summary单行，子类可重写以提供多行内容
        /// </summary>
        public virtual List<string> GetTrackerDetails() {
            return [Summary];
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

        /// <summary>
        /// 从挂起状态恢复为激活时调用的回调，
        /// 注册者可设置此委托以同步存档标记（如清除拒绝标记、设置接受标记）
        /// </summary>
        public Action OnUnsuspended { get; set; }

        #endregion

        public EntrustEntryData(string key, LocalizedText title, LocalizedText summary, LocalizedText category) {
            Key = key;
            TitleText = title;
            SummaryText = summary;
            CategoryText = category;
            Status = QuestEntryStatus.Active;
        }
    }
}
