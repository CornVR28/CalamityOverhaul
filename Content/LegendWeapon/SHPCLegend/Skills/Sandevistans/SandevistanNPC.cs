using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Skills.Sandevistans
{
    /// <summary>
    /// 独立的GlobalNPC，专门处理斯安威斯坦时缓对NPC的影响。
    /// 不侵入CWRNpc，由tModLoader自动加载和调度。
    /// </summary>
    internal class SandevistanNPC : GlobalNPC
    {
        public override bool PreAI(NPC npc) {
            if (!SandevistanTimeSlow.IsActive) {
                return true;
            }
            if (!SandevistanTimeSlow.ShouldAffectNPC(npc)) {
                return true;
            }

            int id = npc.whoAmI;
            //时缓期间新出现的NPC，首次遇到时记录它的速度
            if (!SandevistanTimeSlow.NPCHasCache[id]) {
                SandevistanTimeSlow.NPCCachedVelocities[id] = npc.velocity;
                SandevistanTimeSlow.NPCHasCache[id] = true;
            }

            Vector2 slowVel = SandevistanTimeSlow.NPCCachedVelocities[id] * SandevistanTimeSlow.SlowFactor;

            //维持NPC存活，冻结动画
            npc.timeLeft++;
            npc.aiAction = 0;
            npc.frameCounter = 0;
            //撤销本帧物理引擎造成的移动，按缓存速度的缩放值重新位移
            npc.position = npc.oldPosition + slowVel;
            npc.velocity = slowVel;
            npc.direction = npc.oldDirection;

            return false;
        }
    }
}
