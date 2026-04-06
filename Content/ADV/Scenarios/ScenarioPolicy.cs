using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios
{
    /// <summary>
    /// 场景触发阻塞器标志位，由调度器统一预计算。
    /// 场景通过声明<see cref="ScenarioPolicy.BlockedBy"/>来指定哪些全局状态会阻止自己触发，
    /// 无需在Update中手写每种阻塞条件的判断代码
    /// </summary>
    [Flags]
    public enum ScenarioBlockers
    {
        None = 0,
        ///<summary>CWRWorld.HasBoss — 当前世界存在Boss</summary>
        Boss = 1,
        ///<summary>CWRWorld.BossRush — Boss Rush模式</summary>
        BossRush = 2,
        ///<summary>ScenarioManager.IsActive() — 已有场景正在播放</summary>
        ActiveScenario = 4,
        ///<summary>EbnEffect.IsActive — 永恒燃烧等全局过场动画</summary>
        Cutscene = 8,
    }

    /// <summary>
    /// 声明式场景触发策略。场景通过重写<see cref="ADVScenarioBase.ConfigurePolicy"/>
    /// 返回此对象来声明触发条件，由<see cref="ADVScenarioScheduler"/>统一评估和调度。
    /// <br/>Update()始终被调用，Policy仅控制触发逻辑。返回null表示该场景不参与调度器的触发评估
    /// </summary>
    public class ScenarioPolicy
    {
        /// <summary>
        /// 检查场景是否已完成（读取ADVSave中的标记）。
        /// 返回true时调度器跳过该场景的触发评估。必须设置，不可为null
        /// </summary>
        public Func<ADVSave, bool> IsCompleted;

        /// <summary>
        /// 标记场景已完成（写入ADVSave）。
        /// 调度器在场景成功启动后调用。可为null表示不需要标记（可重复触发的场景）
        /// </summary>
        public Action<ADVSave> MarkCompleted;

        /// <summary>
        /// 核心触发条件，每帧评估。
        /// 所有标准阻塞器检查通过后才会调用此委托，
        /// 因此不需要在这里重复判断Boss/过场等通用条件
        /// </summary>
        public Func<ADVSave, Player, bool> CanTrigger;

        /// <summary>
        /// 场景成功启动后的额外回调（在MarkCompleted之后执行）。
        /// 用于打开UI、播放特效等启动时的附加操作。可为null
        /// </summary>
        public Action<ADVSave, Player> OnStarted;

        /// <summary>
        /// 优先级，数值越大越优先。
        /// 当同一帧有多个场景都满足触发条件时，调度器选择优先级最高的。
        /// 默认值0
        /// </summary>
        public int Priority;

        /// <summary>
        /// 标准阻塞器（位标志）。调度器每帧预计算一次全局阻塞状态，
        /// 与此标志做位与判断。默认值阻塞Boss战和正在播放的场景
        /// </summary>
        public ScenarioBlockers BlockedBy = ScenarioBlockers.Boss
                                          | ScenarioBlockers.ActiveScenario;

    }
}
