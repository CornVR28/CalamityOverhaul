using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel
{
    /// <summary>
    /// 可触发响应式对话的游戏事件枚举，每个值对应ReactiveEventFlags中的一个bit位
    /// 新增事件时只需在此枚举添加新成员（保持2的幂次），无需修改其他代码
    /// </summary>
    [Flags]
    internal enum ShepelReactiveEvent
    {
        None = 0,
        BossDefeated = 1 << 0,
        CyberLevelUp = 1 << 1,
        RAMOverload = 1 << 2,
        //后续新增事件在此追加，保持2的幂次
    }

    /// <summary>
    /// 响应式事件队列工具，外部代码（如Boss死亡钩子）通过Enqueue写入事件
    /// SHPCDialogueRouter在对话时通过TryDequeue消费
    /// </summary>
    internal static class ShepelReactiveEvents
    {
        /// <summary>
        /// 向指定玩家的响应式事件队列写入事件bit
        /// </summary>
        public static void Enqueue(Player player, ShepelReactiveEvent evt) {
            var data = player.GetModPlayer<ADVSavePlayer>().ADVSave.Get<ShepelADVData>();
            data.ReactiveEventFlags |= (int)evt;
        }

        /// <summary>
        /// 专用于Boss击败事件，同时记录具体Boss的NPC类型
        /// </summary>
        public static void EnqueueBossDefeated(Player player, int npcType) {
            var data = player.GetModPlayer<ADVSavePlayer>().ADVSave.Get<ShepelADVData>();
            data.LastDefeatedBossNpcType = npcType;
            data.ReactiveEventFlags |= (int)ShepelReactiveEvent.BossDefeated;
        }

        /// <summary>
        /// 检查指定事件是否在待播队列中
        /// </summary>
        public static bool HasFlag(ShepelADVData data, ShepelReactiveEvent evt)
            => (data.ReactiveEventFlags & (int)evt) != 0;

        /// <summary>
        /// 消费（清除）指定事件，由对话场景Build时调用
        /// </summary>
        public static void ClearFlag(ShepelADVData data, ShepelReactiveEvent evt)
            => data.ReactiveEventFlags &= ~(int)evt;

        /// <summary>
        /// 当前队列是否有任意待播事件，可用于UI状态提示
        /// </summary>
        public static bool HasPending(ShepelADVData data) => data.ReactiveEventFlags != 0;
    }
}
