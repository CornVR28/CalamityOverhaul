using Terraria;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Core
{
    /// <summary>
    /// 史莱姆王状态索引，用于网络同步
    /// </summary>
    internal enum KingSlimeStateIndex : int
    {
        Intro = 0,
        Hop = 1,
        RoyalSlamPrepare = 2,
        RoyalSlamFalling = 3,
        CrownBarrage = 4,
        SlimeRain = 5,
        TeleDash = 6,
        Despawn = 7,
    }

    internal interface IKingSlimeState
    {
        string StateName { get; }
        KingSlimeStateIndex StateIndex { get; }
        void OnEnter(KingSlimeStateContext context);
        IKingSlimeState OnUpdate(KingSlimeStateContext context);
        void OnExit(KingSlimeStateContext context);
    }

    internal abstract class KingSlimeStateBase : IKingSlimeState
    {
        public abstract string StateName { get; }
        public abstract KingSlimeStateIndex StateIndex { get; }
        protected int Timer { get; set; }
        protected int Counter { get; set; }

        public virtual void OnEnter(KingSlimeStateContext context) {
            Timer = 0;
            Counter = 0;
        }

        public abstract IKingSlimeState OnUpdate(KingSlimeStateContext context);

        public virtual void OnExit(KingSlimeStateContext context) {
            context.ResetChargeState();
        }

        #region 工具方法

        /// <summary>
        /// 获取 NPC 朝向玩家的方向向量
        /// </summary>
        protected static Vector2 DirectionToTarget(KingSlimeStateContext context) {
            return (context.Target.Center - context.Npc.Center).SafeNormalize(Vector2.UnitY);
        }

        /// <summary>
        /// 让 NPC 转向玩家所在水平方向
        /// </summary>
        protected static void FaceTargetX(NPC npc, Player player) {
            npc.direction = npc.Center.X > player.Center.X ? -1 : 1;
            npc.spriteDirection = npc.direction;
        }

        #endregion
    }
}
