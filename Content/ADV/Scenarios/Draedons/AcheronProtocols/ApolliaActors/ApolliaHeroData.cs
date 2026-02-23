namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors
{
    /// <summary>
    /// 阿波利娅英雄单位状态数据
    /// </summary>
    internal class ApolliaHeroData
    {
        /// <summary>当前生命值</summary>
        public float HP { get; set; } = 8200000f;

        /// <summary>最大生命值</summary>
        public float MaxHP { get; set; } = 8200000f;

        /// <summary>基础伤害</summary>
        public float BaseDamage { get; set; } = 2200f;

        /// <summary>防御力</summary>
        public float Defense { get; set; } = 180f;

        /// <summary>当前指令模式</summary>
        public HeroCommand CurrentCommand { get; set; } = HeroCommand.Follow;

        /// <summary>HP比例 (0~1)</summary>
        public float HPRatio => MaxHP > 0 ? HP / MaxHP : 0f;
    }

    /// <summary>
    /// 英雄单位指令枚举
    /// </summary>
    internal enum HeroCommand
    {
        /// <summary>跟随玩家</summary>
        Follow,
        /// <summary>驻守当前位置</summary>
        Hold,
        /// <summary>主动进攻</summary>
        Aggressive,
        /// <summary>防御优先</summary>
        Defensive
    }
}
