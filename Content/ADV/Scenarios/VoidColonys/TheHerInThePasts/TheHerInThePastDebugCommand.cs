#if DEBUG
using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TheHerInThePasts
{
    /// <summary>
    /// 快速调试：在鼠标位置生成女巫留影雕像
    /// 聊天里输入 /thehertest 即可
    /// </summary>
    internal class TheHerInThePastDebugCommand : ModCommand
    {
        public override string Command => "thehertest";
        public override string Description => "Debug: 在鼠标处生成女巫雕像";
        public override CommandType Type => CommandType.Chat;

        public override void Action(CommandCaller caller, string input, string[] args) {
            //优先用鼠标位置，没有则退化到玩家中心
            Vector2 statuePos = Main.MouseWorld != Vector2.Zero ? Main.MouseWorld : caller.Player.Center;

            //清理旧的雕像，避免单例冲突
            foreach (var existing in ActorLoader.GetActiveActors<WitchStatueActor>()) {
                if (existing != null) {
                    ActorLoader.KillActor(existing.WhoAmI);
                }
            }

            //生成新雕像
            int statueIndex = ActorLoader.NewActor<WitchStatueActor>(statuePos, Vector2.Zero);
            caller.Reply($"WitchStatueActor spawned at {statuePos} (index {statueIndex}).");
        }
    }
}
#endif
