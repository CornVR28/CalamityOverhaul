namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    /// <summary>
    /// SHPC 射击行为聚合上下文，所有改件通过 <see cref="SHPCModuleItem.Apply"/> 修改这些字段
    /// 默认值都是中性值（1f 倍率、0 加成、false 标志），多改件累积时按乘法/加法叠加
    /// </summary>
    internal struct ShootContext
    {
        //左键攻速倍率，作用于 useTime/useAnimation（>1 更快，<1 更慢）
        public float AttackSpeedMul;
        //通用伤害倍率
        public float DamageMul;
        //左键散布角度倍率，0 表示散布归零
        public float SpreadMul;
        //左键基础发射数量加成（最终发数 = max(1, BeamCount + BeamCountAdd)，MergeBeams 启用则强制为 1）
        public int BeamCountAdd;
        //左键弹丸初速度倍率
        public float BeamSpeedMul;
        //左键弹丸追踪强度倍率（>1 更强）
        public float HomingMul;
        //是否将多发弹幕合并为一发（聚束枪管）
        public bool MergeBeams;
        //合并模式下的伤害加成倍率（额外）
        public float MergedDamageBonus;
        //法力消耗倍率
        public float ManaCostMul;
        //右键蓄力时间倍率（<1 更快蓄满）
        public float ChargeTimeMul;
        //右键能量球飞行速度倍率
        public float OrbSpeedMul;
        //暴击率加成（百分点直接相加）
        public int CritAdd;

        /// <summary>
        /// 中性默认值
        /// </summary>
        public static ShootContext Default => new() {
            AttackSpeedMul = 1f,
            DamageMul = 1f,
            SpreadMul = 1f,
            BeamCountAdd = 0,
            BeamSpeedMul = 1f,
            HomingMul = 1f,
            MergeBeams = false,
            MergedDamageBonus = 1f,
            ManaCostMul = 1f,
            ChargeTimeMul = 1f,
            OrbSpeedMul = 1f,
            CritAdd = 0,
        };
    }
}
