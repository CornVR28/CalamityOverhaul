using CalamityOverhaul.Common;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Banish
{
    /// <summary>
    /// 赛博放逐按键处理
    /// <br/>在赛博空间激活时按键放逐光标下的目标
    /// </summary>
    internal class CyberBanishInput : ModPlayer
    {
        public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet) {
            if (Player.whoAmI != Main.myPlayer) return;
            if (CWRKeySystem.CyberBanish_Key != null && CWRKeySystem.CyberBanish_Key.JustPressed) {
                CyberBanish.BanishAtCursor();
            }
        }
    }
}
