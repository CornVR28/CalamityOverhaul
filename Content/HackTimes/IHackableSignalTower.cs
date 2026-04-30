using InnoVault.Actors;
using Terraria;

namespace CalamityOverhaul.Content.HackTimes
{
    /// <summary>
    /// 可被骇客时间骇入的信号塔 Actor 目标
    /// 与<see cref="IHackableTurret"/>并列但独立，因为信号塔的可用协议与炮台不同
    /// （病毒广播、扫描增幅等广域行为，而非单体电路破坏）
    /// </summary>
    internal interface IHackableSignalTower : IHackTarget
    {
        /// <summary>返回 Actor 本体，用于光标悬停判定与生命周期校验</summary>
        Actor AsActor { get; }

        /// <summary>
        /// 对信号塔触发病毒广播：向周围发射赛博电磁脉冲
        /// 广播覆盖范围内的所有可骇入炮台会被长时间短路
        /// </summary>
        /// <param name="radiusPixels">广播半径像素</param>
        /// <param name="disableFrames">命中炮台的短路帧长</param>
        /// <param name="caster">发起的玩家，用于多人同步</param>
        void BeginVirusBroadcast(float radiusPixels, int disableFrames, Player caster);
    }
}
