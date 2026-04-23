using System;

namespace CalamityOverhaul.Content.HackTimes
{
    /// <summary>
    /// 骇入协议支持的目标类型
    /// <br/>使用Flags允许一个协议同时支持多种目标
    /// </summary>
    [Flags]
    internal enum HackTargetKind
    {
        //无目标
        None = 0,
        //NPC目标
        Npc = 1,
        //物块目标
        Tile = 2,
        //灵异目标（如乱码鬼等Actor类实体）
        Wraith = 4,
        //虚空聚落炮台等可被电路骇入的Actor机械
        Turret = 8,
        //虚空聚落信号塔等承担广播/扫描职能的核心Actor
        SignalTower = 16,
    }
}
