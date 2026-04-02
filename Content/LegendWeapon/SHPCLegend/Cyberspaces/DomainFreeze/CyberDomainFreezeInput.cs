using CalamityOverhaul.Common;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.DomainFreeze
{
    /// <summary>
    /// 赛博领域冻结按键处理
    /// <br/>在赛博空间激活时按键冻结领域内所有敌对实体
    /// </summary>
    internal class CyberDomainFreezeInput : ModPlayer
    {
        public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet) {
            if (Player.whoAmI != Main.myPlayer) return;
            if (CWRKeySystem.CyberFreeze_Key != null && CWRKeySystem.CyberFreeze_Key.JustPressed) {
                CyberDomainFreeze.TriggerFreeze(Player);
            }
        }
    }
}
