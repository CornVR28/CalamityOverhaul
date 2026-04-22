#if DEBUG
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Strongholds
{
    /// <summary>
    /// 调试指令：聊天里输入 /zerosite 把玩家瞬移到零号站点锚点
    /// 默认把玩家放到上层桥面正上方的空位，避免直接卡进桥面碰撞
    /// </summary>
    internal class ZeroSiteDebugCommand : ModCommand
    {
        public override string Command => "zerosite";
        public override string Description => "Debug: 传送到零号站点锚点";
        public override CommandType Type => CommandType.Chat;

        public override void Action(CommandCaller caller, string input, string[] args) {
            var stronghold = new ZeroSiteStronghold();
            if (!stronghold.TryPickAnchor(out int tileX, out int tileY)) {
                caller.Reply("ZeroSite锚点不可用（世界尺寸可能过小）。");
                return;
            }

            //tile坐标→像素，然后抬高一点避免被桥面碰撞卡住
            Vector2 worldPos = new(tileX * 16f, tileY * 16f - 160f);
            caller.Player.Teleport(worldPos, 1, 0);
            caller.Player.velocity = Vector2.Zero;
            caller.Reply($"已传送到零号站点：tile({tileX},{tileY})。");
        }
    }
}
#endif
