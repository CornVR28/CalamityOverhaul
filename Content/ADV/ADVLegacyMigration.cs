using CalamityOverhaul.Content.ADV.MainMenuOvers;
using CalamityOverhaul.Content.ADV.Scenarios;
using CalamityOverhaul.Content.ADV.Scenarios.SupCal;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.ADV
{
    /// <summary>
    /// ADV存档旧版数据恢复/迁移工具。
    /// 集中处理所有向下兼容逻辑，避免污染ADVSave和ADVSavePlayer的主流程代码。
    /// <br/>历史存档格式：
    /// <br/>v0 — ADV数据以"ADCSave"键嵌套在HalibutSave的TagCompound中，所有字段扁平存放，场景数据也在HalibutSave tag中
    /// <br/>v1 — ADV数据独立为"ADVSave"键存放在ADVSavePlayer中，但所有字段仍扁平存放（无__version标记）
    /// <br/>v2 — 当前版本，"ADVSave"中包含__version=2，每个模块使用独立的子TagCompound
    /// </summary>
    internal static class ADVLegacyMigration
    {
        /// <summary>
        /// 旧版存档中ADV数据存储在HalibutSave内部的键名（v0格式）
        /// </summary>
        private const string LegacyADCSaveKey = "ADCSave";

        /// <summary>
        /// 判断TagCompound是否为旧版扁平格式（不含版本标记，即v0或v1）
        /// </summary>
        public static bool IsLegacyFormat(TagCompound tag) {
            return !tag.ContainsKey(ADVSave.VersionKey);
        }

        /// <summary>
        /// 尝试从旧版扁平格式的TagCompound加载数据到各模块（处理v0/v1格式）。
        /// v0和v1格式中，所有字段平铺在同一层TagCompound中，
        /// 由于各模块字段名全局唯一，每个模块直接从中提取自己识别的字段即可
        /// </summary>
        /// <returns>是否检测到并处理了旧版格式</returns>
        public static bool TryLoadFromFlatFormat(TagCompound tag, IEnumerable<ADVDataModule> modules) {
            if (!IsLegacyFormat(tag)) {
                return false;
            }
            foreach (var module in modules) {
                module.LoadFields(tag);
            }
            return true;
        }

        /// <summary>
        /// 尝试从旧版HalibutSave的TagCompound中检测并迁移ADV数据（v0格式完整迁移路径）。
        /// 旧版本中ADV数据以"ADCSave"键存储在HalibutSave内部，场景数据也在同一层tag中。
        /// 此方法负责完整的v0迁移：ADV数据模块加载 + 后续处理 + 场景数据加载
        /// </summary>
        /// <param name="halibutTag">HalibutSave的完整TagCompound</param>
        /// <param name="player">当前玩家</param>
        /// <param name="advSave">目标ADVSave实例</param>
        /// <returns>是否成功检测到旧版数据并完成迁移</returns>
        public static bool TryMigrateFromHalibutSave(TagCompound halibutTag, Player player, ADVSave advSave) {
            if (!halibutTag.TryGet<TagCompound>(LegacyADCSaveKey, out var adcTag)) {
                return false;
            }

            //adcTag是v0扁平格式，LoadData内部会检测到无__version走旧版加载路径
            advSave.LoadData(adcTag);

            //迁移后执行必要的后续处理
            if (advSave.Get<SupCalADVData>().EternalBlazingNow) {
                MenuSave.UnlockEternalBlazingNowPortrait(player);
            }

            //v0格式中场景数据也存储在HalibutSave的tag中
            foreach (var scenario in ADVScenarioBase.Instances) {
                scenario.LoadData(halibutTag);
            }

            return true;
        }
    }
}
