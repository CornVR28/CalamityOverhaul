using InnoVault.GameSystem;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Graphics;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Teleport
{
    /// <summary>
    /// 赛博瞬移隐藏路由
    /// <br/>演出期间从玩家绘制列表中移除本地玩家，让"化作裂缝"的视觉成立
    /// <br/><see cref="ModPlayer"/> 没有 PreDrawPlayers 钩子，因此通过 PlayerOverride 转发
    /// </summary>
    internal class CyberTeleportHideOverride : PlayerOverride
    {
        public override bool PreDrawPlayers(ref Camera camera, ref IEnumerable<Player> players) {
            //仅本地玩家在演出期内被隐藏；其它玩家由各自客户端各自决定
            if (Player.whoAmI != Main.myPlayer) return true;
            if (!CyberTeleport.IsLocalPlayerHidden) return true;
            players = players.Where(p => p.whoAmI != Player.whoAmI);
            return true;
        }
    }
}
