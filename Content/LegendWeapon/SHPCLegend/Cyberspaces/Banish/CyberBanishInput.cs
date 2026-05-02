using CalamityOverhaul.Common;
using CalamityOverhaul.Content.HackTimes;
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
            //骇客时间激活期间禁止使用领域技能
            if (HackTime.Active) return;
            if (CWRKeySystem.CyberBanish_Key != null && CWRKeySystem.CyberBanish_Key.JustPressed) {
                CyberBanish.BanishAtCursor();
            }
        }
    }
}
