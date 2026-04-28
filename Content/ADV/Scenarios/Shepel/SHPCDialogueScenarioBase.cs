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
        /// 触发此场景所需的最低故事阶段（StoryPhase）
        /// 当前阶段低于该值时不参与路由，无需在子类重复判断
        /// </summary>
        public virtual int RequiredPhase => 0;

        /// <summary>
        /// 子类实现的额外触发条件，通用检查（阶段门控）已由基类处理
        /// 默认始终返回true
        /// </summary>
        protected virtual bool CheckConditions(Player player, ADVSave save) => true;

        /// <summary>
        /// 路由器调用的最终判断入口，封装了故事阶段门控
        /// 非虚，防止子类绕过通用检查
        /// </summary>
        public bool CanBeRoutedTo(Player player, ADVSave save) {
            ShepelADVData data = save.Get<ShepelADVData>();

            //故事阶段门控
            if (data.StoryPhase < RequiredPhase) {
                return false;
            }

            return CheckConditions(player, save);
        }
    }
}
