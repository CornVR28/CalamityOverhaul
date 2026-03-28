using CalamityOverhaul.Content.ADV.Scenarios.SupCal;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.ADV
{
    /// <summary>
    /// ADV数据聚合器。自动发现并管理所有<see cref="ADVDataModule"/>子类，
    /// 提供统一的存档读写和旧版兼容能力
    /// 新增剧情线只需创建新的ADVDataModule子类，无需修改此处
    /// </summary>
    public class ADVSave
    {
        internal const string VersionKey = "__version";
        private const int CurrentVersion = 2;

        private readonly Dictionary<Type, ADVDataModule> _modules = [];
        private readonly Dictionary<string, ADVDataModule> _modulesByKey = [];

        public ADVSave() {
            List<ADVDataModule> dataModules = VaultUtils.GetDerivedInstances<ADVDataModule>();
            foreach(var module in dataModules) {
                if (_modulesByKey.TryGetValue(module.SaveKey, out ADVDataModule value)) {
                    throw new Exception($"ADVDataModule SaveKey conflict: '{module.SaveKey}' " + $"(Type {module.GetType().Name} vs {value.GetType().Name})");
                }
                _modules[module.GetType()] = module;
                _modulesByKey[module.SaveKey] = module;
            }
        }

        /// <summary>
        /// 获取指定类型的数据模块
        /// </summary>
        public T Get<T>() where T : ADVDataModule {
            return (T)_modules[typeof(T)];
        }

        public virtual TagCompound SaveData() {
            TagCompound tag = [];
            tag[VersionKey] = CurrentVersion;
            foreach (var module in _modules.Values) {
                tag[module.SaveKey] = module.SaveFields();
            }
            return tag;
        }

        public virtual void LoadData(TagCompound tag) {
            //旧版扁平格式（v0/v1）由迁移工具类统一处理
            if (ADVLegacyMigration.TryLoadFromFlatFormat(tag, _modules.Values)) {
                return;
            }
            //当前v2分层格式：按模块SaveKey读取各自的子TagCompound
            foreach (var module in _modules.Values) {
                if (tag.TryGet<TagCompound>(module.SaveKey, out var moduleTag)) {
                    module.LoadFields(moduleTag);
                }
            }
        }

        public void SendEbnData(Player player) {
            if (VaultUtils.isSinglePlayer) {
                return;
            }
            ModPacket modPacket = CWRMod.Instance.GetPacket();
            modPacket.Write((byte)CWRMessageType.EbnTag);
            modPacket.Write(player.whoAmI);
            modPacket.Write(Get<SupCalADVData>().EternalBlazingNow);
            modPacket.Send();
        }

        internal static void NetHandle(CWRMessageType type, BinaryReader reader, int whoAmI) {
            if (type == CWRMessageType.EbnTag) {
                int playerIndex = reader.ReadInt32();
                bool eternalBlazingNow = reader.ReadBoolean();
                if (!playerIndex.TryGetPlayer(out var player)) {
                    return;
                }
                if (!player.TryGetADVSave(out var save)) {
                    return;
                }
                save.Get<SupCalADVData>().EternalBlazingNow = eternalBlazingNow;
                if (!VaultUtils.isServer) {
                    return;
                }
                ModPacket modPacket = CWRMod.Instance.GetPacket();
                modPacket.Write((byte)CWRMessageType.EbnTag);
                modPacket.Write(player.whoAmI);
                modPacket.Write(eternalBlazingNow);
                modPacket.Send(-1, whoAmI);
            }
        }
    }
}
