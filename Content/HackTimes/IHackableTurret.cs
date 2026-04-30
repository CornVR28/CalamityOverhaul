using InnoVault.Actors;
using Terraria;

namespace CalamityOverhaul.Content.HackTimes
{
    /// <summary>
    /// 可被骇客时间骇入的 Actor 机械目标
    /// 典型实现：虚空聚落炮台（加特林/激光炮台）
    /// 通过 <see cref="IHackTarget"/> 暴露统一的目标抽象，
    /// 通过本接口暴露电路层面的骇入反应
    /// </summary>
    internal interface IHackableTurret : IHackTarget
    {
        /// <summary>返回 Actor 本体，用于统一的光标悬停判定与生命周期校验</summary>
        Actor AsActor { get; }

        /// <summary>当前是否正处于电路过载导致的失效状态，用于面板状态显示</summary>
        bool IsCircuitDisabled { get; }

        /// <summary>剩余失效帧数，用于扫描面板展示和 UI 倒计时</summary>
        int CircuitDisabledFrames { get; }

        /// <summary>
        /// 对炮台执行电路短路：瞬时放电，当场使其沉寂指定帧数
        /// 区别于过载——短路是一次性释放内部电能，恢复后立刻可用
        /// </summary>
        void ApplyShortCircuit(int frames, Player caster);

        /// <summary>
        /// 对炮台执行电路过载：长时间使其失效并伴随持续视觉故障
        /// 适合演出中让塔防临时瘫痪、腾出战术窗口
        /// </summary>
        void ApplyCircuitOverload(int frames, Player caster);
    }
}
