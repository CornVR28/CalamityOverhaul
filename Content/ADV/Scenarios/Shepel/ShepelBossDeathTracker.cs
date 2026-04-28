using CalamityOverhaul.Content.ADV.Common;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel
{
    /// <summary>
    /// 监听客户端的Boss死亡事件，向本地玩家写入BossDefeated响应式标记
    /// 仅当npc.boss为true时生效，确保体节等非主体不触发
    /// </summary>
    internal class ShepelBossDeathTracker : DeathTrackingNPC
    {
        public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.boss;

        public override void OnNPCDeath(NPC npc) {
            if (Main.dedServ) return;
            ShepelReactiveEvents.EnqueueBossDefeated(Main.LocalPlayer, npc.type);
        }
    }
}
