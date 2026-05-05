using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Core;
using Terraria;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.States
{
    /// <summary>
    /// 离场：所有玩家死亡或脱离时，向上加速渐隐，最后置为非活跃。
    /// </summary>
    internal class KingSlimeDespawnState : KingSlimeStateBase
    {
        public override string StateName => "Despawn";
        public override KingSlimeStateIndex StateIndex => KingSlimeStateIndex.Despawn;

        public override void OnEnter(KingSlimeStateContext context) {
            base.OnEnter(context);
            context.Npc.noTileCollide = true;
        }

        public override IKingSlimeState OnUpdate(KingSlimeStateContext context) {
            NPC npc = context.Npc;
            npc.velocity.X *= 0.94f;
            npc.velocity.Y -= 0.4f;
            if (npc.velocity.Y < -16f) npc.velocity.Y = -16f;
            npc.alpha = (int)MathHelper.Min(npc.alpha + 4, 255);
            npc.timeLeft = (int)MathHelper.Min(npc.timeLeft, 90);

            Timer++;
            if (Timer > 180 || npc.alpha >= 255) {
                npc.active = false;
                npc.life = 0;
            }
            return null;
        }
    }
}
