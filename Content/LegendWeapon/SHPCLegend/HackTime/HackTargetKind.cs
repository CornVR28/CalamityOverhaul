using System;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime
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
    }
}
