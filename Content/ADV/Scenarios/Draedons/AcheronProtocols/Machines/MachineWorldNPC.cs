using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines
{
    internal class MachineWorldNPC : GlobalNPC
    {
        /// <summary>
        /// 允许在机械世界中生成的NPC类型白名单，目前为空
        /// </summary>
        public static readonly HashSet<int> SpawnWhitelist = [];

        public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => MachineWorld.Active;

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) {
            if (!MachineWorld.Active)
                return;

            //完全禁止自然生成
            spawnRate = 0;
            maxSpawns = 0;
        }

        public override bool PreAI(NPC npc) {
            if (!MachineWorld.Active)
                return true;

            //白名单内的NPC正常运行
            if (SpawnWhitelist.Contains(npc.type))
                return true;

            //非白名单NPC直接清除
            npc.active = false;
            npc.netUpdate = true;
            return false;
        }
    }
}
