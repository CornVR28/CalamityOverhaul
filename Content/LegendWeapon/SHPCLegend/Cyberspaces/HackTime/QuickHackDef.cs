namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.HackTime
{
    /// <summary>
    /// 快速骇入协议定义
    /// <br/>每种协议对应一种可对目标施加的骇入效果
    /// </summary>
    internal class QuickHackDef
    {
        /// <summary>
        /// 协议内部标识名
        /// </summary>
        public string Id;
        /// <summary>
        /// 显示名称
        /// </summary>
        public string Name;
        /// <summary>
        /// 简短效果描述
        /// </summary>
        public string Desc;
        /// <summary>
        /// 上传所需时间（帧数，60帧为1秒）
        /// </summary>
        public int UploadTime;
        /// <summary>
        /// 协议类别标签
        /// </summary>
        public QuickHackCategory Category;

        public QuickHackDef(string id, string name, string desc, int uploadTime, QuickHackCategory category) {
            Id = id;
            Name = name;
            Desc = desc;
            UploadTime = uploadTime;
            Category = category;
        }
    }

    /// <summary>
    /// 骇入协议类别
    /// </summary>
    internal enum QuickHackCategory
    {
        /// <summary>
        /// 致命型：直接造成伤害
        /// </summary>
        Lethal,
        /// <summary>
        /// 控制型：限制目标行动
        /// </summary>
        Control,
        /// <summary>
        /// 隐匿型：干扰目标感知
        /// </summary>
        Covert,
        /// <summary>
        /// 传播型：扩散至附近目标
        /// </summary>
        Contagion
    }

    /// <summary>
    /// 内置骇入协议列表
    /// </summary>
    internal static class QuickHackRegistry
    {
        public static readonly QuickHackDef[] All = {
            new("synapse_burn", "突触焚毁", "对目标神经系统造成持续热伤害", 90, QuickHackCategory.Lethal),
            new("short_circuit", "短路", "释放电磁脉冲造成即时伤害并短暂麻痹", 60, QuickHackCategory.Lethal),
            new("cyberpsychosis", "赛博精神病", "使目标陷入狂暴攻击周围一切单位", 150, QuickHackCategory.Control),
            new("system_reset", "系统重启", "强制目标系统重启导致长时间晕眩", 120, QuickHackCategory.Control),
            new("optic_overload", "视觉过载", "过载目标光学设备使其致盲", 75, QuickHackCategory.Covert),
            new("memory_wipe", "记忆清除", "抹除目标短期记忆使其失去仇恨", 80, QuickHackCategory.Covert),
            new("contagion", "蔓延协议", "植入自复制病毒扩散至附近目标", 100, QuickHackCategory.Contagion),
        };
    }
}
