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
        private const string VersionKey = "__version";
        private const int CurrentVersion = 2;

        private readonly Dictionary<Type, ADVDataModule> _modules = [];
        private readonly Dictionary<string, ADVDataModule> _modulesByKey = [];

        public ADVSave() {
            foreach (var type in typeof(ADVDataModule).Assembly.GetTypes()) {
                if (!type.IsAbstract && type.IsSubclassOf(typeof(ADVDataModule))) {
                    var module = (ADVDataModule)Activator.CreateInstance(type);
                    if (_modulesByKey.ContainsKey(module.SaveKey)) {
                        throw new Exception($"ADVDataModule SaveKey冲突: '{module.SaveKey}' " +
                            $"(类型 {type.Name} 与 {_modulesByKey[module.SaveKey].GetType().Name})");
                    }
                    _modules[type] = module;
                    _modulesByKey[module.SaveKey] = module;
                }
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
            if (tag.ContainsKey(VersionKey)) {
                //新版分层格式：按模块SaveKey读取各自的子TagCompound
                foreach (var module in _modules.Values) {
                    if (tag.TryGet<TagCompound>(module.SaveKey, out var moduleTag)) {
                        module.LoadFields(moduleTag);
                    }
                }
            }
            else {
                //旧版扁平格式：所有字段在同一层，直接让每个模块从扁平tag中读取自己的字段
                foreach (var module in _modules.Values) {
                    module.LoadFields(tag);
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
