namespace CalamityOverhaul.Content.RAMSystems
{
    /// <summary>
    /// 提供运行时挂入 RAM 系统的修饰器接口
    /// <br/>实现者可在每帧贡献 <see cref="MaxRamBonus"/> 与 <see cref="RecoveryRateBonus"/>
    /// <br/>当 <see cref="IsActive"/> 为 false 时该贡献会被忽略，便于义体/Buff 等动态来源即开即用
    /// </summary>
    public interface IRamModifierProvider
    {
        /// <summary>
        /// 该来源向 RAM 上限提供的额外格数（可正可负）
        /// </summary>
        int MaxRamBonus { get; }
        /// <summary>
        /// 该来源向恢复速度提供的额外量（每秒，正数表示加速）
        /// </summary>
        float RecoveryRateBonus { get; }
        /// <summary>
        /// 当前是否生效，false 时跳过聚合
        /// </summary>
        bool IsActive { get; }
    }
}
