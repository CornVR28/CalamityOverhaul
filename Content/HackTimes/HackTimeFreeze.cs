using CalamityOverhaul.Common;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.HackTimes
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
            NPCFrozenVelocities = null;
            NPCSnapshotCaptured = null;
            NPCSnapshotTypes = null;
            ProjFrozenPositions = null;
            ProjFrozenVelocities = null;
            ProjSnapshotCaptured = null;
            ProjSnapshotTypes = null;
            ProjSnapshotOwners = null;
            ProjSnapshotIdentities = null;
        }

        /// <summary>
        /// 时间冻结是否激活
        /// </summary>
        public static bool IsActive { get; private set; }

        //NPC冻结位置快照
        internal static Vector2[] NPCFrozenPositions;
        //NPC冻结速度快照
        internal static Vector2[] NPCFrozenVelocities;
        //NPC快照是否有效
        internal static bool[] NPCSnapshotCaptured;
        //NPC快照对应的类型，用于避免复用槽位时套用旧快照
        internal static int[] NPCSnapshotTypes;
        //弹幕冻结位置快照
        internal static Vector2[] ProjFrozenPositions;
        //弹幕冻结速度快照
        internal static Vector2[] ProjFrozenVelocities;
        //弹幕快照是否有效
        internal static bool[] ProjSnapshotCaptured;
        //弹幕快照对应的类型/归属/身份，用于避免复用槽位时套用旧快照
        internal static int[] ProjSnapshotTypes;
        internal static int[] ProjSnapshotOwners;
        internal static int[] ProjSnapshotIdentities;

        void ICWRLoader.LoadData() {
            NPCFrozenPositions = new Vector2[Main.maxNPCs];
            NPCFrozenVelocities = new Vector2[Main.maxNPCs];
            NPCSnapshotCaptured = new bool[Main.maxNPCs];
            NPCSnapshotTypes = new int[Main.maxNPCs];
            ProjFrozenPositions = new Vector2[Main.maxProjectiles];
            ProjFrozenVelocities = new Vector2[Main.maxProjectiles];
            ProjSnapshotCaptured = new bool[Main.maxProjectiles];
            ProjSnapshotTypes = new int[Main.maxProjectiles];
            ProjSnapshotOwners = new int[Main.maxProjectiles];
            ProjSnapshotIdentities = new int[Main.maxProjectiles];
        }

        /// <summary>
        /// 激活时间冻结，快照当前所有实体位置
        /// </summary>
        public static void Activate() {
            if (IsActive) return;
            IsActive = true;
            TimeGear.Register("HackTimeFreeze", 0f);
            SnapshotPositions();
        }

        /// <summary>
        /// 解除时间冻结
        /// </summary>
        public static void Deactivate() {
            if (!IsActive) return;
            RestoreSnapshots();
            ClearSnapshots();
            IsActive = false;
            TimeGear.Unregister("HackTimeFreeze");
        }

        private static void SnapshotPositions() {
            ClearSnapshots();
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC npc = Main.npc[i];
                if (npc.active) {
                    CaptureNPC(npc);
                }
            }
            for (int i = 0; i < Main.maxProjectiles; i++) {
                Projectile proj = Main.projectile[i];
                if (proj.active) {
                    CaptureProjectile(proj);
                }
            }
        }

        internal static void EnsureNPCSnapshot(NPC npc) {
            int id = npc.whoAmI;
            if (!NPCSnapshotCaptured[id] || NPCSnapshotTypes[id] != npc.type) {
                CaptureNPC(npc);
            }
        }

        internal static void EnsureProjectileSnapshot(Projectile proj) {
            int id = proj.whoAmI;
            if (!ProjSnapshotCaptured[id]
                || ProjSnapshotTypes[id] != proj.type
                || ProjSnapshotOwners[id] != proj.owner
                || ProjSnapshotIdentities[id] != proj.identity) {
                CaptureProjectile(proj);
            }
        }

        private static void CaptureNPC(NPC npc) {
            int id = npc.whoAmI;
            NPCFrozenPositions[id] = npc.position;
            NPCFrozenVelocities[id] = npc.velocity;
            NPCSnapshotCaptured[id] = true;
            NPCSnapshotTypes[id] = npc.type;
        }

        private static void CaptureProjectile(Projectile proj) {
            int id = proj.whoAmI;
            ProjFrozenPositions[id] = proj.position;
            ProjFrozenVelocities[id] = proj.velocity;
            ProjSnapshotCaptured[id] = true;
            ProjSnapshotTypes[id] = proj.type;
            ProjSnapshotOwners[id] = proj.owner;
            ProjSnapshotIdentities[id] = proj.identity;
        }

        private static void RestoreSnapshots() {
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC npc = Main.npc[i];
                if (!npc.active || !NPCSnapshotCaptured[i] || NPCSnapshotTypes[i] != npc.type) continue;
                npc.velocity = NPCFrozenVelocities[i];
            }

            for (int i = 0; i < Main.maxProjectiles; i++) {
                Projectile proj = Main.projectile[i];
                if (!proj.active || !ProjSnapshotCaptured[i]) continue;
                if (ProjSnapshotTypes[i] != proj.type
                    || ProjSnapshotOwners[i] != proj.owner
                    || ProjSnapshotIdentities[i] != proj.identity) {
                    continue;
                }
                proj.velocity = ProjFrozenVelocities[i];
            }
        }

        private static void ClearSnapshots() {
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPCSnapshotCaptured[i] = false;
            }
            for (int i = 0; i < Main.maxProjectiles; i++) {
                ProjSnapshotCaptured[i] = false;
            }
        }

        /// <summary>
        /// 判断该NPC是否应被冻结
        /// </summary>
        internal static bool ShouldFreezeNPC(NPC npc) {
            if (!npc.active) return false;
            return true;
        }

        /// <summary>
        /// 判断该弹幕是否应被冻结
        /// </summary>
        internal static bool ShouldFreezeProjectile(Projectile proj) {
            if (!proj.active) return false;
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
            HackTimeFreeze.EnsureNPCSnapshot(npc);

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
            HackTimeFreeze.EnsureProjectileSnapshot(proj);

            //将弹幕钉在快照位置上
            proj.position = HackTimeFreeze.ProjFrozenPositions[id];
            proj.velocity = Vector2.Zero;
            proj.timeLeft++;

            return false;
        }
    }

    /// <summary>
    /// 骇客时间玩家冻结拦截器
    /// <br/>骇入期间玩家不可移动、不可使用物品，仅保留UI交互和切换键
    /// </summary>
    internal class HackTimeFreezePlayer : ModPlayer
    {
        //冻结时的玩家位置快照
        private Vector2 frozenPosition;
        //是否已记录冻结位置
        private bool positionCaptured;
        //冻结时的朝向快照
        private int frozenDirection;
        //冻结时的动画帧快照
        private Rectangle frozenBodyFrame;
        private Rectangle frozenLegFrame;
        private Rectangle frozenHeadFrame;

        public override void PreUpdate() {
            if (!HackTimeFreeze.IsActive) {
                positionCaptured = false;
                return;
            }

            //首次冻结时快照位置、朝向、动画帧
            if (!positionCaptured) {
                frozenPosition = Player.position;
                frozenDirection = Player.direction;
                frozenBodyFrame = Player.bodyFrame;
                frozenLegFrame = Player.legFrame;
                frozenHeadFrame = Player.headFrame;
                positionCaptured = true;
            }

            //锁定位置和速度
            Player.position = frozenPosition;
            Player.velocity = Vector2.Zero;
            //锁定朝向
            Player.direction = frozenDirection;
            //Player.ChangeDir = 0;
            //防止解冻后摔落伤害
            Player.fallStart = (int)(Player.position.Y / 16f);

            //禁用所有移动和交互控制，保留鼠标用于UI操作
            Player.controlLeft = false;
            Player.controlRight = false;
            Player.controlUp = false;
            Player.controlDown = false;
            Player.controlJump = false;
            Player.controlHook = false;
            Player.controlMount = false;
            Player.controlUseItem = false;
            Player.controlUseTile = false;
            Player.controlThrow = false;
            Player.controlSmart = false;
            Player.controlTorch = false;
        }

        public override void PostUpdate() {
            if (!HackTimeFreeze.IsActive || !positionCaptured) return;
            //PostUpdate后再次锁定，防止其他系统在更新中修改朝向和位置
            Player.position = frozenPosition;
            Player.velocity = Vector2.Zero;
            Player.direction = frozenDirection;
        }

        public override void FrameEffects() {
            if (!HackTimeFreeze.IsActive || !positionCaptured) return;
            //锁定动画帧，阻止任何帧变化
            Player.bodyFrame = frozenBodyFrame;
            Player.legFrame = frozenLegFrame;
            Player.headFrame = frozenHeadFrame;
        }

        public override bool PreItemCheck() {
            if (HackTimeFreeze.IsActive) return false;
            return true;
        }
    }
}
