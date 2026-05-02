using CalamityOverhaul.Content.HackTimes;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content
{
    /// <summary>
    /// 统一时间冻结NPC拦截器，同时处理TimeFrozenTick和HackTimeFreeze两种冻结模式
    /// </summary>
    internal class TimeFreezeNPC : GlobalNPC
    {
        public override bool PreAI(NPC npc) {
            if (HackTimeFreeze.IsActive) {
                if (!HackTimeFreeze.ShouldFreezeNPC(npc)) {
                    return true;
                }
                int id = npc.whoAmI;
                HackTimeFreeze.EnsureNPCSnapshot(npc);
                npc.position = HackTimeFreeze.NPCFrozenPositions[id];
                npc.velocity = Vector2.Zero;
                npc.aiAction = 0;
                npc.frameCounter = 0;
                npc.timeLeft++;
                return false;
            }
            if (CWRWorld.TimeFrozenTick > 0) {
                CWRNpc.DoTimeFrozen(npc);
                return false;
            }
            return true;
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot) {
            if (HackTimeFreeze.IsActive) {
                return false;
            }
            return true;
        }

        public override bool CanHitNPC(NPC npc, NPC target) {
            if (HackTimeFreeze.IsActive) {
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 统一时间冻结弹幕拦截器，同时处理TimeFrozenTick和HackTimeFreeze两种冻结模式
    /// </summary>
    internal class TimeFreezeProjectile : GlobalProjectile
    {
        public override bool PreAI(Projectile proj) {
            if (HackTimeFreeze.IsActive) {
                if (!HackTimeFreeze.ShouldFreezeProjectile(proj)) {
                    return true;
                }
                int id = proj.whoAmI;
                HackTimeFreeze.EnsureProjectileSnapshot(proj);
                proj.position = HackTimeFreeze.ProjFrozenPositions[id];
                proj.velocity = Vector2.Zero;
                proj.timeLeft++;
                return false;
            }
            if (CWRWorld.TimeFrozenTick > 0 && !proj.hide && !proj.friendly
                && !Main.projPet[proj.type] && !proj.minion && !Main.projHook[proj.type]
                && !CWRLoad.ProjValue.ImmuneFrozen[proj.type]) {
                proj.position = proj.oldPosition;
                proj.timeLeft++;
                return false;
            }
            return true;
        }

        public override bool? CanHitNPC(Projectile projectile, NPC target) {
            if (HackTimeFreeze.IsActive) {
                return false;
            }
            return null;
        }

        public override bool CanHitPlayer(Projectile projectile, Player target) {
            if (HackTimeFreeze.IsActive) {
                return false;
            }
            return true;
        }

        public override bool CanHitPvp(Projectile projectile, Player target) {
            if (HackTimeFreeze.IsActive) {
                return false;
            }
            return true;
        }
    }
}
