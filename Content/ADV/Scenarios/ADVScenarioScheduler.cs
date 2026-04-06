using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios
{
    /// <summary>
    /// ADV场景中央调度器。每帧由ADVPlayer调用一次<see cref="Tick"/>，
    /// 对所有场景无条件调用Update()，并统一评估声明了<see cref="ScenarioPolicy"/>的场景的触发条件
    /// </summary>
    internal static class ADVScenarioScheduler
    {
        //注册式阻塞器提供者列表，每帧Tick时调用并按位合并
        private static readonly List<Func<ScenarioBlockers>> blockerProviders = [];

        /// <summary>
        /// 注册一个阻塞器提供者。调度器每帧调用所有已注册的提供者，
        /// 按位合并得到当前帧的全局阻塞状态。
        /// 应在Mod加载阶段调用（如ModSystem.PostSetupContent）
        /// </summary>
        public static void RegisterBlocker(Func<ScenarioBlockers> provider) {
            blockerProviders.Add(provider);
        }

        /// <summary>
        /// 每帧由ADVPlayer.PostUpdate()调用，驱动所有场景的评估和触发
        /// </summary>
        public static void Tick(ADVSave save, Player player) {
            //1.预计算当前帧的全局阻塞状态
            ScenarioBlockers currentBlockers = ScenarioBlockers.None;
            foreach (var provider in blockerProviders) {
                currentBlockers |= provider();
            }


            //2.遍历所有场景实例
            ADVScenarioBase bestCandidate = null;
            int bestPriority = int.MinValue;

            foreach (var scenario in ADVScenarioBase.Instances) {
                //Update是通用帧更新钩子，无条件调用
                scenario.Update(save, player);

                var policy = scenario.Policy;
                if (policy == null) {
                    continue;
                }

                //已完成的场景跳过触发评估
                if (policy.IsCompleted(save)) {
                    continue;
                }

                //阻塞器检查: 当前帧存在的阻塞标志与场景声明的BlockedBy有交集则跳过
                if ((currentBlockers & policy.BlockedBy) != 0) {
                    continue;
                }

                //自定义触发条件
                if (policy.CanTrigger != null && !policy.CanTrigger(save, player)) {
                    continue;
                }

                //到这里说明条件完全满足，参与优先级竞选
                if (policy.Priority > bestPriority) {
                    bestPriority = policy.Priority;
                    bestCandidate = scenario;
                }
            }

            //3.触发选中的候选场景
            if (bestCandidate == null) {
                return;
            }

            if (!bestCandidate.StartScenario()) {
                return;
            }

            //标记完成
            bestCandidate.Policy.MarkCompleted?.Invoke(save);

            //触发后回调
            bestCandidate.Policy.OnStarted?.Invoke(save, player);
        }

        /// <summary>
        /// Mod卸载时清理所有状态
        /// </summary>
        internal static void Unload() {
            blockerProviders.Clear();
        }
    }
}
