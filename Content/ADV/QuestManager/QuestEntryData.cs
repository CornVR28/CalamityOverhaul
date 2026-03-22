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
    /// 任务管理器中单条任务的数据模型<br/>
    /// 这是一个概念化的数据接口——后续各任务线（Helen/Draedons/OldDukes/SupCal）
    /// 的具体任务将实现此接口并注册到 <see cref="QuestManagerUI"/>
    /// </summary>
    internal class QuestEntryData
    {
        /// <summary>唯一标识符</summary>
        public string Key;
        /// <summary>显示名称</summary>
        public string Title;
        /// <summary>任务简要描述（显示在列表中）</summary>
        public string Summary;
        /// <summary>所属任务线/分类标签</summary>
        public string Category;
        /// <summary>当前状态</summary>
        public QuestEntryStatus Status;
        /// <summary>进度 0~1</summary>
        public float Progress;
        /// <summary>进度文本（例如 "3/10"）</summary>
        public string ProgressText;
        /// <summary>是否为新任务（刚加入时的高亮标记）</summary>
        public bool IsNew;
        /// <summary>任务优先级（影响排序，越大越靠前）</summary>
        public int Priority;

        public QuestEntryData(string key, string title, string summary, string category) {
            Key = key;
            Title = title;
            Summary = summary;
            Category = category;
            Status = QuestEntryStatus.Active;
        }
    }
}
