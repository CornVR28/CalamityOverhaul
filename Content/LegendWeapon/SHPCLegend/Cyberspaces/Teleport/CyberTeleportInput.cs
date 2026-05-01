using CalamityOverhaul.Common;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Teleport
{
    /// <summary>
    /// 赛博瞬移按键处理
    /// <br/>赛博空间激活时，按下 <see cref="CWRKeySystem.Legend_Teleport"/> 立刻瞬移到光标位置
    /// <br/>仅本地玩家响应，所有合法性校验在 <see cref="CyberTeleport.TryTeleport"/> 内完成
    /// </summary>
    internal class CyberTeleportInput : ModPlayer
    {
        public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet) {
            if (Player.whoAmI != Main.myPlayer) return;
            if (CWRKeySystem.Legend_Teleport == null) return;
            if (!CWRKeySystem.Legend_Teleport.JustPressed) return;

            //领域未激活时不抢按键，留给 Halibut 等其它系统响应
            if (!Cyberspace.Active) return;

            CyberTeleport.TryTeleport(Player);
        }
    }
}
