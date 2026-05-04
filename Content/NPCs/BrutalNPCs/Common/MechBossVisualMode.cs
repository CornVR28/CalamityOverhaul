namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.Common
{
    /// <summary>
    /// 机械Boss通用视觉模式
    /// <br/>毁灭者、机械骷髅王、双子魔眼共用的滤镜状态枚举
    /// </summary>
    internal enum MechBossVisualMode
    {
        /// <summary>
        /// 常态——细微红橙描边 + 暗部红化，缓解夜晚看不清的问题
        /// </summary>
        Idle = 0,
        /// <summary>
        /// 警告（蓄力/锁定/转阶段）——红黄高对比脉冲描边
        /// </summary>
        Warning = 1,
        /// <summary>
        /// 冲刺/高速突进——白热橙边 + 横向能量条纹
        /// </summary>
        Dashing = 2,
    }
}
