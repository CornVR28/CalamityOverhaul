using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend
{
    internal class SHPCPlayer : ModPlayer
    {
        public Item[] Modules;

        public static SHPCPlayer Get(Player player) => player.GetModPlayer<SHPCPlayer>();

        public override void Initialize() {
            Modules = CreateEmptyModules();
        }

        private static Item[] CreateEmptyModules() {
            Item[] arr = new Item[SHPCData.SlotCount];
            for (int i = 0; i < SHPCData.SlotCount; i++) {
                arr[i] = new Item();
            }
            return arr;
        }

        public Item GetModule(int slotIdx) {
            if (slotIdx < 0 || slotIdx >= SHPCData.SlotCount) {
                return null;
            }
            Item it = Modules[slotIdx];
            return it == null || it.IsAir ? null : it;
        }

        public Item TakeModule(int slotIdx) {
            if (slotIdx < 0 || slotIdx >= SHPCData.SlotCount) {
                return null;
            }
            Item old = Modules[slotIdx];
            Modules[slotIdx] = new Item();
            return old == null || old.IsAir ? null : old;
        }

        public Item PutModule(int slotIdx, Item module) {
            if (slotIdx < 0 || slotIdx >= SHPCData.SlotCount || module == null || module.IsAir) {
                return null;
            }
            Item old = Modules[slotIdx];
            Item cloned = module.Clone();
            cloned.stack = 1;
            Modules[slotIdx] = cloned;
            return old == null || old.IsAir ? null : old;
        }

        public int EquippedCount() {
            int n = 0;
            for (int i = 0; i < SHPCData.SlotCount; i++) {
                if (GetModule(i) != null) {
                    n++;
                }
            }
            return n;
        }

        public override void SaveData(TagCompound tag) {
            for (int i = 0; i < SHPCData.SlotCount; i++) {
                Item m = Modules[i];
                if (m != null && !m.IsAir) {
                    tag[$"SHPC_Mod_{i}"] = ItemIO.Save(m);
                }
            }
        }

        public override void LoadData(TagCompound tag) {
            Modules ??= CreateEmptyModules();
            for (int i = 0; i < SHPCData.SlotCount; i++) {
                if (tag.TryGet($"SHPC_Mod_{i}", out TagCompound modTag)) {
                    try {
                        Modules[i] = ItemIO.Load(modTag);
                    }
                    catch {
                        Modules[i] = new Item();
                    }
                }
                else {
                    Modules[i] = new Item();
                }
            }
        }
    }
}
