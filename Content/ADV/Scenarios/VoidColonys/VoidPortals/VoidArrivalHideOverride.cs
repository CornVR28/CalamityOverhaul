using InnoVault.GameSystem;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Graphics;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.VoidPortals
{
    /// <summary>
    /// 用于在抵达演出期间从玩家绘制列表中移除本玩家
    /// <see cref="ModPlayer"/> 没有 PreDrawPlayers 钩子，因此通过 PlayerOverride 转发
    /// </summary>
    internal class VoidArrivalHideOverride : PlayerOverride
    {
        public override bool PreDrawPlayers(ref Camera camera, ref IEnumerable<Player> players) {
            if (!Player.TryGetModPlayer(out VoidArrivalCutscene cs)) return true;
            if (!cs.ShouldHidePlayer) return true;
            players = players.Where(p => p.whoAmI != Player.whoAmI);
            return true;
        }
    }
}
