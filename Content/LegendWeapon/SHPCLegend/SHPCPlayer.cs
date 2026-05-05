using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend
{
    internal class SHPCPlayer : ModPlayer
    {
        public const int PresetCount = 3;

        public Item[] Modules;

        //当前激活的预设索引（0/1/2）
        public int ActivePreset = 0;

        //三套预设，每套包含 SlotCount 个槽位
        public Item[][] Presets;

        public static SHPCPlayer Get(Player player) => player.GetModPlayer<SHPCPlayer>();

        public override void Initialize() {
            Modules = CreateEmptyModules();
            Presets = CreateEmptyPresets();
            ActivePreset = 0;
        }

        private static Item[] CreateEmptyModules() {
            Item[] arr = new Item[SHPCData.SlotCount];
            for (int i = 0; i < SHPCData.SlotCount; i++) {
                arr[i] = new Item();
            }
            return arr;
        }

        private static Item[][] CreateEmptyPresets() {
            Item[][] p = new Item[PresetCount][];
            for (int i = 0; i < PresetCount; i++) {
                p[i] = CreateEmptyModules();
            }
            return p;
        }

        private static Item[] CloneModules(Item[] src) {
            Item[] dst = new Item[SHPCData.SlotCount];
            for (int i = 0; i < SHPCData.SlotCount; i++) {
                dst[i] = src[i] != null && !src[i].IsAir ? src[i].Clone() : new Item();
            }
            return dst;
        }

        //切换到指定预设，先保存当前槽位到活跃预设，再载入目标预设
        public void SwitchPreset(int newIdx) {
            if (newIdx < 0 || newIdx >= PresetCount || newIdx == ActivePreset) {
                return;
            }
            Presets ??= CreateEmptyPresets();
            Presets[ActivePreset] = CloneModules(SafeModules());
            ActivePreset = newIdx;
            Modules = CloneModules(Presets[ActivePreset]);
        }

        private Item[] SafeModules() {
            Modules ??= CreateEmptyModules();
            return Modules;
        }

        public Item GetModule(int slotIdx) {
            if (slotIdx < 0 || slotIdx >= SHPCData.SlotCount) {
                return null;
            }
            Item it = SafeModules()[slotIdx];
            return it == null || it.IsAir ? null : it;
        }

        public Item TakeModule(int slotIdx) {
            if (slotIdx < 0 || slotIdx >= SHPCData.SlotCount) {
                return null;
            }
            Item[] modules = SafeModules();
            Item old = modules[slotIdx];
            modules[slotIdx] = new Item();
            return old == null || old.IsAir ? null : old;
        }

        public Item PutModule(int slotIdx, Item module) {
            if (slotIdx < 0 || slotIdx >= SHPCData.SlotCount || module == null || module.IsAir) {
                return null;
            }
            Item[] modules = SafeModules();
            Item old = modules[slotIdx];
            Item cloned = module.Clone();
            cloned.stack = 1;
            modules[slotIdx] = cloned;
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
            try {
                //保存前先将当前槽位同步到活跃预设
                Presets ??= CreateEmptyPresets();
                Presets[ActivePreset] = CloneModules(SafeModules());

                tag["SHPC_ActivePreset"] = ActivePreset;

                for (int p = 0; p < PresetCount; p++) {
                    for (int s = 0; s < SHPCData.SlotCount; s++) {
                        Item m = Presets[p][s];
                        if (m != null && !m.IsAir) {
                            tag[$"SHPC_Preset_{p}_{s}"] = ItemIO.Save(m);
                        }
                    }
                }
            } catch (System.Exception ex) {
                CWRMod.Instance.Logger.Error($"SHPCPlayer.SaveData Error: {ex.Message}");
            }
        }

        public override void LoadData(TagCompound tag) {
            try {
                Presets ??= CreateEmptyPresets();

                //读取活跃预设索引（旧存档无此字段则默认0）
                ActivePreset = tag.TryGet("SHPC_ActivePreset", out int savedPreset)
                    ? System.Math.Clamp(savedPreset, 0, PresetCount - 1)
                    : 0;

                //以 SHPC_ActivePreset 是否存在作为新格式标记，空存档（全槽位为空）下该 key 同样存在
                bool isNewFormat = tag.ContainsKey("SHPC_ActivePreset");
                for (int p = 0; p < PresetCount; p++) {
                    for (int s = 0; s < SHPCData.SlotCount; s++) {
                        if (tag.TryGet($"SHPC_Preset_{p}_{s}", out TagCompound modTag)) {
                            try {
                                Presets[p][s] = ItemIO.Load(modTag);
                            } catch {
                                Presets[p][s] = new Item();
                            }
                        }
                        else {
                            Presets[p][s] = new Item();
                        }
                    }
                }

                //兼容旧存档：旧格式只保存 SHPC_Mod_{i}，迁移到预设0
                if (!isNewFormat) {
                    for (int i = 0; i < SHPCData.SlotCount; i++) {
                        if (tag.TryGet($"SHPC_Mod_{i}", out TagCompound modTag)) {
                            try {
                                Presets[0][i] = ItemIO.Load(modTag);
                            } catch {
                                Presets[0][i] = new Item();
                            }
                        }
                    }
                }

                //将活跃预设的内容加载到 Modules 作为当前使用状态
                Modules = CloneModules(Presets[ActivePreset]);
            } catch (System.Exception ex) {
                CWRMod.Instance.Logger.Error($"SHPCPlayer.LoadData Error: {ex.Message}");
            }
        }
    }
}
