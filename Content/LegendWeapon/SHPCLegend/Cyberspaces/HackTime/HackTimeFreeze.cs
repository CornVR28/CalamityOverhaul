using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.HackTime
{
    /// <summary>
    /// 骇客时间的时间冻结系统
    /// <br/>独立于CWRWorld.TimeFrozenTick，专门管理骇客模式下的世界冻结
    /// <br/>通过GlobalNPC拦截AI，通过GlobalProjectile拦截弹幕运动
    /// </summary>
    internal class HackTimeFreeze : ICWRLoader
    {
        void ICWRLoader.UnLoadData() {
            IsActive = false;
            NPCFrozenPositions = null;
            ProjFrozenPositions = null;
        }

        /// <summary>
        /// 时间冻结是否激活
        /// </summary>
        public static bool IsActive { get; private set; }

        //NPC冻结位置快照
        internal static Vector2[] NPCFrozenPositions;
        //弹幕冻结位置快照
        internal static Vector2[] ProjFrozenPositions;

        void ICWRLoader.LoadData() {
            NPCFrozenPositions = new Vector2[Main.maxNPCs];
            ProjFrozenPositions = new Vector2[Main.maxProjectiles];
        }

        /// <summary>
        /// 激活时间冻结，快照当前所有实体位置
        /// </summary>
        public static void Activate() {
            if (IsActive) return;
            IsActive = true;
            SnapshotPositions();
        }

        /// <summary>
        /// 解除时间冻结
        /// </summary>
        public static void Deactivate() {
            IsActive = false;
        }

        private static void SnapshotPositions() {
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC npc = Main.npc[i];
                if (npc.active) {
                    NPCFrozenPositions[i] = npc.position;
                }
            }
            for (int i = 0; i < Main.maxProjectiles; i++) {
                Projectile proj = Main.projectile[i];
                if (proj.active) {
                    ProjFrozenPositions[i] = proj.position;
                }
            }
        }

        /// <summary>
        /// 判断该NPC是否应被冻结
        /// </summary>
        internal static bool ShouldFreezeNPC(NPC npc) {
            if (!npc.active) return false;
            if (npc.friendly || npc.townNPC) return false;
            return true;
        }

        /// <summary>
        /// 判断该弹幕是否应被冻结
        /// </summary>
        internal static bool ShouldFreezeProjectile(Projectile proj) {
            if (!proj.active) return false;
            //玩家自己的弹幕不冻结
            if (proj.friendly) return false;
            if (Main.projPet[proj.type] || proj.minion || Main.projHook[proj.type]) return false;
            return true;
        }
    }

    /// <summary>
    /// 骇客时间NPC冻结拦截器
    /// </summary>
    internal class HackTimeFreezeNPC : GlobalNPC
    {
        public override bool PreAI(NPC npc) {
            if (!HackTimeFreeze.IsActive) return true;
            if (!HackTimeFreeze.ShouldFreezeNPC(npc)) return true;

            int id = npc.whoAmI;

            //将NPC钉在快照位置上，完全冻结
            npc.position = HackTimeFreeze.NPCFrozenPositions[id];
            npc.velocity = Vector2.Zero;
            npc.aiAction = 0;
            npc.frameCounter = 0;
            npc.timeLeft++;

            return false;
        }
    }

    /// <summary>
    /// 骇客时间弹幕冻结拦截器
    /// </summary>
    internal class HackTimeFreezeProjectile : GlobalProjectile
    {
        public override bool PreAI(Projectile proj) {
            if (!HackTimeFreeze.IsActive) return true;
            if (!HackTimeFreeze.ShouldFreezeProjectile(proj)) return true;

            int id = proj.whoAmI;

            //将弹幕钉在快照位置上
            proj.position = HackTimeFreeze.ProjFrozenPositions[id];
            proj.velocity = Vector2.Zero;
            proj.timeLeft++;

            return false;
        }
    }
}
