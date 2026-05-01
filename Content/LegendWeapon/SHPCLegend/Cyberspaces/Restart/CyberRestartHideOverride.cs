using InnoVault.GameSystem;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Graphics;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Restart
{
    /// <summary>
    /// 赛博重启隐藏路由
    /// <br/>奇点段从玩家绘制列表中移除本地玩家，让"压缩为维度核心裂缝"的视觉成立
    /// </summary>
    internal class CyberRestartHideOverride : PlayerOverride
    {
        public override bool PreDrawPlayers(ref Camera camera, ref IEnumerable<Player> players) {
            if (Player.whoAmI != Main.myPlayer) return true;
            if (!CyberRestart.IsLocalPlayerHidden) return true;
            players = players.Where(p => p.whoAmI != Player.whoAmI);
            return true;
        }
    }
}
