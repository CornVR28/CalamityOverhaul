using System.Linq;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel
{
    /// <summary>
    /// SHPC对话按钮路由器
    /// 玩家点击HUD对话按钮时调用TryStart，按优先级找到首个可触发的场景并启动
    /// 所有SHPCDialogueScenarioBase子类均自动参与路由，无需额外注册
    /// </summary>
    internal static class SHPCDialogueRouter
    {
        /// <summary>
        /// 尝试启动最高优先级的可用对话
        /// 当前已有场景运行中则直接返回false
        /// </summary>
        public static bool TryStart(Player player)
        {
            if (ScenarioManager.IsActive())
            {
                return false;
            }
            ADVSave save = player.GetModPlayer<ADVSavePlayer>().ADVSave;
            foreach (SHPCDialogueScenarioBase scenario in ADVScenarioBase.Instances
                .OfType<SHPCDialogueScenarioBase>()
                .OrderByDescending(s => s.DialoguePriority))
            {
                if (scenario.CanBeRoutedTo(player, save))
                {
                    return scenario.StartScenario();
                }
            }
            return false;
        }
    }
}
