using CalamityOverhaul.Common;
using CalamityOverhaul.Content.HackTimes;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Restart
{
    /// <summary>
    /// 赛博重启按键处理
    /// <br/>赛博空间激活时按下 <see cref="CWRKeySystem.Legend_Restart"/> 立即触发重启演出
    /// <br/>仅本地玩家响应，所有合法性校验在 <see cref="CyberRestart.TryRestart"/> 内完成
    /// </summary>
    internal class CyberRestartInput : ModPlayer
    {
        public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet) {
            if (Player.whoAmI != Main.myPlayer) return;
            if (CWRKeySystem.Legend_Restart == null) return;
            if (!CWRKeySystem.Legend_Restart.JustPressed) return;

            //骇客时间激活期间禁止使用领域技能
            if (HackTime.Active) return;

            //领域未激活时不抢按键，留给 Halibut 等其它系统响应
            if (!Cyberspace.Active) return;

            CyberRestart.TryRestart(Player);
        }
    }
}
