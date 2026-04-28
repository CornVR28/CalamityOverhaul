using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel
{
    /// <summary>
    /// 所有可由SHPC对话按钮主动触发的场景基类
    /// 子类自动被SHPCDialogueRouter发现，无需手动注册
    /// </summary>
    internal abstract class SHPCDialogueScenarioBase : ADVScenarioBase
    {
        /// <summary>
        /// 路由优先级，数值越大越优先检查，默认0
        /// </summary>
        public virtual int DialoguePriority => 0;

        /// <summary>
        /// 当前是否允许被路由器选中并触发
        /// 默认始终可触发，子类按需重写
        /// </summary>
        public virtual bool CanBeRoutedTo(Player player, ADVSave save) => true;
    }
}
