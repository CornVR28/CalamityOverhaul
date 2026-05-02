using System.IO;
using Terraria;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend
{
    internal class SHPCData : LegendData
    {
        //六个改件槽位，索引顺序与 SHPCModPanel.SlotLabels/SlotOffsets 保持一致
        //0=BARREL 1=OPTIC 2=POWER 3=STOCK 4=GRIP 5=FRAME
        public const int SlotCount = 6;

        public Item[] Modules = CreateEmptyModules();

        public override int TargetLevel => InWorldBossPhase.SHPC_Level();

        /// <summary>
        /// 从 Item 上的 LegendData 取出 SHPCData，找不到时返回 null
        /// </summary>
        public static SHPCData TryGet(Item item) {
            if (item == null || item.IsAir) {
                return null;
            }
            return item.CWR()?.LegendData as SHPCData;
        }

        private static Item[] CreateEmptyModules() {
            Item[] arr = new Item[SlotCount];
            for (int i = 0; i < SlotCount; i++) {
                arr[i] = new Item();
            }
            return arr;
        }

        /// <summary>
        /// 获取指定槽位的改件物品，索引越界或为空返回 null
        /// </summary>
        public Item GetModule(int slotIdx) {
            if (slotIdx < 0 || slotIdx >= SlotCount) {
                return null;
            }
            Item it = Modules[slotIdx];
            return it == null || it.IsAir ? null : it;
        }

        /// <summary>
        /// 取出指定槽位的改件，返回原物品并清空该槽
        /// </summary>
        public Item TakeModule(int slotIdx) {
            if (slotIdx < 0 || slotIdx >= SlotCount) {
                return null;
            }
            Item old = Modules[slotIdx];
            Modules[slotIdx] = new Item();
            if (old == null || old.IsAir) {
                return null;
            }
            return old;
        }

        /// <summary>
        /// 装入一个改件物品（未做类别校验，校验由上层处理）
        /// 已有物品时返回旧物品供上层退还背包
        /// </summary>
        public Item PutModule(int slotIdx, Item module) {
            if (slotIdx < 0 || slotIdx >= SlotCount || module == null || module.IsAir) {
                return null;
            }
            Item old = Modules[slotIdx];
            Item cloned = module.Clone();
            cloned.stack = 1;
            Modules[slotIdx] = cloned;
            if (old == null || old.IsAir) {
                return null;
            }
            return old;
        }

        public override void SaveData(Item item, TagCompound tag) {
            base.SaveData(item, tag);
            for (int i = 0; i < SlotCount; i++) {
                Item m = Modules[i];
                if (m != null && !m.IsAir) {
                    tag[$"SHPC_Mod_{i}"] = ItemIO.Save(m);
                }
            }
        }

        public override void LoadData(Item item, TagCompound tag) {
            base.LoadData(item, tag);
            Modules ??= CreateEmptyModules();
            for (int i = 0; i < SlotCount; i++) {
                if (tag.TryGet($"SHPC_Mod_{i}", out TagCompound modTag)) {
                    try {
                        Modules[i] = ItemIO.Load(modTag);
                    } catch {
                        Modules[i] = new Item();
                    }
                }
                else {
                    Modules[i] = new Item();
                }
            }
        }

        public override void SendLegend(Item item, BinaryWriter writer) {
            base.SendLegend(item, writer);
            Modules ??= CreateEmptyModules();
            for (int i = 0; i < SlotCount; i++) {
                Item m = Modules[i];
                bool has = m != null && !m.IsAir;
                writer.Write(has);
                if (has) {
                    ItemIO.Send(m, writer, true);
                }
            }
        }

        public override void ReceiveLegend(Item item, BinaryReader reader) {
            base.ReceiveLegend(item, reader);
            Modules ??= CreateEmptyModules();
            for (int i = 0; i < SlotCount; i++) {
                bool has = reader.ReadBoolean();
                Modules[i] = has ? ItemIO.Receive(reader, true) : new Item();
            }
        }

        /// <summary>
        /// 计算当前已装备的改件数（用于状态显示）
        /// </summary>
        public int EquippedCount() {
            int n = 0;
            for (int i = 0; i < SlotCount; i++) {
                if (GetModule(i) != null) {
                    n++;
                }
            }
            return n;
        }
    }
}
