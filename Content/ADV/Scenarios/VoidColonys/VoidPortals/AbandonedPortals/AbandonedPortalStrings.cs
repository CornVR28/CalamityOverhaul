using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.VoidPortals.AbandonedPortals
{
    internal class AbandonedPortalStrings : ModSystem, ILocalizedModType
    {
        public string LocalizationCategory => "UI";

        public static LocalizedText Title { get; private set; }
        public static LocalizedText BrokenSubtitle { get; private set; }
        public static LocalizedText RepairingSubtitle { get; private set; }
        public static LocalizedText RepairedSubtitle { get; private set; }
        public static LocalizedText BrokenBody { get; private set; }
        public static LocalizedText RepairingBody { get; private set; }
        public static LocalizedText RepairedBody { get; private set; }
        public static LocalizedText StartRepair { get; private set; }
        public static LocalizedText Teleport { get; private set; }
        public static LocalizedText Close { get; private set; }
        public static LocalizedText ProgressFormat { get; private set; }
        public static LocalizedText StatusBroken { get; private set; }
        public static LocalizedText StatusRepairing { get; private set; }
        public static LocalizedText StatusRepaired { get; private set; }
        public static LocalizedText DiagnosticHeader { get; private set; }
        public static LocalizedText DiagnosticBroken { get; private set; }
        public static LocalizedText DiagnosticRepairing { get; private set; }
        public static LocalizedText DiagnosticRepaired { get; private set; }

        public override void SetStaticDefaults() {
            Title = this.GetLocalization(nameof(Title), () => "废墟传送门控制台");
            BrokenSubtitle = this.GetLocalization(nameof(BrokenSubtitle), () => "结构断裂 · 核心离线");
            RepairingSubtitle = this.GetLocalization(nameof(RepairingSubtitle), () => "自修复程序运行中");
            RepairedSubtitle = this.GetLocalization(nameof(RepairedSubtitle), () => "通道稳定 · 虚无坐标可用");
            BrokenBody = this.GetLocalization(nameof(BrokenBody), () => "残破门框中仍残留着微弱的亚空间回声。启动自修复后，门体会在数分钟内重建定位环。");
            RepairingBody = this.GetLocalization(nameof(RepairingBody), () => "纳米焊缝正在重构裂隙约束器。请保持附近区域稳定，等待校准完成。");
            RepairedBody = this.GetLocalization(nameof(RepairedBody), () => "门体已经完成自修复。传送序列会先展开裂隙演出，再将你送入虚无世界。");
            StartRepair = this.GetLocalization(nameof(StartRepair), () => "启 动 自 修 复");
            Teleport = this.GetLocalization(nameof(Teleport), () => "进 入 通 道");
            Close = this.GetLocalization(nameof(Close), () => "关 闭");
            ProgressFormat = this.GetLocalization(nameof(ProgressFormat), () => "校准进度  {0}%");
            StatusBroken = this.GetLocalization(nameof(StatusBroken), () => "STATUS  ▌ OFFLINE");
            StatusRepairing = this.GetLocalization(nameof(StatusRepairing), () => "STATUS  ▌ CALIBRATING");
            StatusRepaired = this.GetLocalization(nameof(StatusRepaired), () => "STATUS  ▌ ONLINE");
            DiagnosticHeader = this.GetLocalization(nameof(DiagnosticHeader), () => "[DIAG]");
            DiagnosticBroken = this.GetLocalization(nameof(DiagnosticBroken), () => "ERR-0xC4: 定位环裂隙 / 主能源回路断开");
            DiagnosticRepairing = this.GetLocalization(nameof(DiagnosticRepairing), () => "INFO: 纳米焊缝阵列同步中…请勿移动门基座");
            DiagnosticRepaired = this.GetLocalization(nameof(DiagnosticRepaired), () => "OK: 全部子系统在线 / 坐标解析就绪");
        }
    }
}
