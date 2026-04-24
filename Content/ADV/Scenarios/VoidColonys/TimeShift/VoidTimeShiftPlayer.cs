using CalamityOverhaul.Common;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TimeShift
{
    /// <summary>
    /// 虚空聚落时空叠加玩家输入处理
    /// 仅在本地玩家处于虚空聚落维度时响应切换按键
    /// </summary>
    internal class VoidTimeShiftPlayer : ModPlayer
    {
        public override void ProcessTriggers(TriggersSet triggersSet) {
            if (Player.whoAmI != Main.myPlayer) {
                return;
            }
            if (!VoidColony.Active) {
                return;
            }
            if (CWRKeySystem.VoidTimeShift_Key?.JustPressed == true) {
                VoidTimeShiftSystem.RequestToggle();
            }
        }
    }
}
