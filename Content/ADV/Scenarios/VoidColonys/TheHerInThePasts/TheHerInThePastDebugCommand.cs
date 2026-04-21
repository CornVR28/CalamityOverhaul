#if DEBUG
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.GlitchWraith;
using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TheHerInThePasts
{
    /// <summary>
    /// 快速调试：在鼠标位置生成女巫留影雕像，并在旁边刷一只鬼乱码
    /// 聊天里输入 /thehertest 即可，无参数默认偏移400px
    /// </summary>
    internal class TheHerInThePastDebugCommand : ModCommand
    {
        public override string Command => "thehertest";
        public override string Description => "Debug: 在鼠标处生成女巫雕像并刷一只鬼乱码";
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

            //在雕像右侧600px处生成一只鬼乱码，如果已经有就不重复生成
            if (ActorLoader.GetActiveActors<GlitchWraithActor>().Count == 0) {
                Vector2 wraithPos = statuePos + new Vector2(600f, -80f);
                int wraithIndex = ActorLoader.NewActor<GlitchWraithActor>(wraithPos, Vector2.Zero);
                caller.Reply($"GlitchWraithActor spawned at {wraithPos} (index {wraithIndex}).");
            }
            else {
                caller.Reply("GlitchWraithActor already exists, skipping spawn.");
            }
        }
    }
}
#endif
