using Terraria;

namespace CalamityOverhaul.Content.UIs.StorageUIs
{
    /// <summary>
    /// 箱子存储访问接口，统一不同存储模式（本地副本 / 直接引用）的物品存取
    /// </summary>
    internal interface IChestStorage
    {
        /// <summary>每行槽位数</summary>
        int SlotsPerRow { get; }
        /// <summary>行数</summary>
        int SlotRows { get; }
        /// <summary>总槽位数</summary>
        int TotalSlots => SlotsPerRow * SlotRows;
        /// <summary>已占用的槽位数</summary>
        int UsedSlotCount { get; }
        /// <summary>获取指定槽位物品</summary>
        Item GetItem(int slot);
        /// <summary>设置指定槽位物品</summary>
        void SetItem(int slot, Item item);
    }
}
