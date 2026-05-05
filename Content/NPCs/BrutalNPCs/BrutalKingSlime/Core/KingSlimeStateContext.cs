using Terraria;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Core
{
    /// <summary>
    /// 史莱姆王状态上下文，存储状态机运行所需的共享数据
    /// </summary>
    internal class KingSlimeStateContext
    {
        #region 核心引用
        public NPC Npc { get; set; }
        public Player Target { get; set; }
        #endregion

        #region 战斗状态
        /// <summary>
        /// 是否进入二阶段（HP &lt; 50%）
        /// </summary>
        public bool IsEnraged { get; set; }
        /// <summary>
        /// 是否处于死亡模式 / Boss Rush
        /// </summary>
        public bool IsDeathMode { get; set; }
        #endregion

        #region 蓄力 / 视觉
        public bool IsCharging { get; set; }
        /// <summary>
        /// 0=无 1=皇家砸地蓄力 2=皇冠光束 3=史莱姆雨蓄力 4=瞬移冲撞蓄力
        /// </summary>
        public int ChargeType { get; set; }
        /// <summary>
        /// 蓄力 / 演出进度，0~1
        /// </summary>
        public float ChargeProgress { get; set; }
        /// <summary>
        /// 当 IsCharging 为 4 时为冲撞方向
        /// </summary>
        public Vector2 DashDirection { get; set; }
        /// <summary>
        /// 描述"皇室凝胶"果冻挤压效果，正值=纵向压扁，负值=纵向拉伸；外部由状态写入，PostDraw 读取
        /// </summary>
        public float SquishY { get; set; }
        #endregion

        #region 跨状态记忆
        /// <summary>
        /// 上一次主动攻击是什么类型，避免连续重复
        /// </summary>
        public KingSlimeStateIndex LastAttackKind { get; set; } = KingSlimeStateIndex.Hop;
        /// <summary>
        /// 已经累计的小跳次数，足够则进入主动技能
        /// </summary>
        public int HopChainCount { get; set; }
        #endregion

        public void ResetChargeState() {
            IsCharging = false;
            ChargeProgress = 0f;
            ChargeType = 0;
        }

        public void SetChargeState(int type, float progress) {
            IsCharging = true;
            ChargeType = type;
            ChargeProgress = MathHelper.Clamp(progress, 0f, 1f);
        }
    }
}
