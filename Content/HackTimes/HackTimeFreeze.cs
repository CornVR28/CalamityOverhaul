using CalamityOverhaul.Common;
using InnoVault.GameSystem;
using System;
using System.Reflection;
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
        //Liquid.UpdateLiquid拦截委托
        private delegate void Hook_UpdateLiquid(Action orig);
        //Player.UpdateEquips拦截委托
        private delegate void Hook_UpdateEquips(Action<Player, int> orig, Player self, int i);

        void ICWRLoader.UnLoadData() {
            IsActive = false;
            NPCFrozenPositions = null;
            NPCFrozenVelocities = null;
            NPCSnapshotCaptured = null;
            NPCSnapshotTypes = null;
            ProjFrozenPositions = null;
            ProjFrozenVelocities = null;
            ProjSnapshotCaptured = null;
            ProjSpawnedDuringFreeze = null;
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
        //标记该弹幕是否在冻结期间新生成，解冻时需清理避免造成爆发伤害
        internal static bool[] ProjSpawnedDuringFreeze;
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
            ProjSpawnedDuringFreeze = new bool[Main.maxProjectiles];
            ProjSnapshotTypes = new int[Main.maxProjectiles];
            ProjSnapshotOwners = new int[Main.maxProjectiles];
            ProjSnapshotIdentities = new int[Main.maxProjectiles];
        }

        void ICWRLoader.SetupData() {
            //拦截液体更新，使水流在冻结期间不再传播
            MethodInfo liquidMethod = typeof(Liquid).GetMethod("UpdateLiquid"
                , BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (liquidMethod != null) {
                VaultHook.Add(liquidMethod, (Hook_UpdateLiquid)OnUpdateLiquidHook);
            }

            //拦截玩家装备更新，阻止饰品在冻结期间继续运行（包括其生成弹幕、扣除冷却等行为）
            MethodInfo equipMethod = typeof(Player).GetMethod("UpdateEquips"
                , BindingFlags.Public | BindingFlags.Instance, null, [typeof(int)], null);
            if (equipMethod != null) {
                VaultHook.Add(equipMethod, (Hook_UpdateEquips)OnUpdateEquipsHook);
            }
        }

        private static void OnUpdateLiquidHook(Action orig) {
            if (IsActive) return;
            orig();
        }

        private static void OnUpdateEquipsHook(Action<Player, int> orig, Player self, int i) {
            if (IsActive) return;
            orig(self, i);
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
            KillProjectilesSpawnedDuringFreeze();
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
                    //冻结开始前已存在的弹幕不算"冻结期间新生成"
                    CaptureProjectile(proj, spawnedDuringFreeze: false);
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
                //首次出现且当前处于冻结状态，说明是冻结期间被生成
                CaptureProjectile(proj, spawnedDuringFreeze: IsActive);
            }
        }

        private static void CaptureNPC(NPC npc) {
            int id = npc.whoAmI;
            NPCFrozenPositions[id] = npc.position;
            NPCFrozenVelocities[id] = npc.velocity;
            NPCSnapshotCaptured[id] = true;
            NPCSnapshotTypes[id] = npc.type;
        }

        private static void CaptureProjectile(Projectile proj, bool spawnedDuringFreeze) {
            int id = proj.whoAmI;
            ProjFrozenPositions[id] = proj.position;
            ProjFrozenVelocities[id] = proj.velocity;
            ProjSnapshotCaptured[id] = true;
            ProjSpawnedDuringFreeze[id] = spawnedDuringFreeze;
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

        private static void KillProjectilesSpawnedDuringFreeze() {
            for (int i = 0; i < Main.maxProjectiles; i++) {
                if (!ProjSpawnedDuringFreeze[i]) continue;
                Projectile proj = Main.projectile[i];
                if (!proj.active) continue;
                //校验槽位未被复用
                if (ProjSnapshotTypes[i] != proj.type
                    || ProjSnapshotOwners[i] != proj.owner
                    || ProjSnapshotIdentities[i] != proj.identity) {
                    continue;
                }
                proj.Kill();
            }
        }

        private static void ClearSnapshots() {
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPCSnapshotCaptured[i] = false;
            }
            for (int i = 0; i < Main.maxProjectiles; i++) {
                ProjSnapshotCaptured[i] = false;
                ProjSpawnedDuringFreeze[i] = false;
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
        //各类计时器快照，避免冷却条/无敌帧/呼吸条等持续推进
        private int frozenPotionDelay;
        private int frozenRestorationDelayTime;
        private int frozenItemAnimation;
        private int frozenItemAnimationMax;
        private int frozenItemTime;
        private int frozenImmuneTime;
        private bool frozenImmune;
        private int frozenBreath;
        private int frozenBreathCD;
        //Calamity怒气与肾上腺素快照
        private float frozenRage;
        private float frozenAdrenaline;
        private int frozenRageGainCooldown;
        private int frozenRageCombatFrames;
        private int frozenAdrenalinePauseTimer;
        //HP、魔力及回复计时器快照
        private int frozenStatLife;
        private float frozenLifeRegenTime;
        private int frozenStatMana;
        private float frozenManaRegenDelay;
        //buff持续时间快照，阻止药水病/闪避冷却等buff在冻结期间流逝
        private int[] frozenBuffTime;
        private int[] frozenBuffType;
        //飞行/翅膀时间快照，防止开启或关闭骇客时间时飞行时间被重置
        internal float frozenWingTime;
        internal int frozenRocketTime;
        //背包开启状态
        private bool snapshotInventoryOpen;

        public override void PreUpdate() {
            if (!HackTimeFreeze.IsActive) {
                if (positionCaptured) {
                    //还原背包
                    Main.playerInventory = snapshotInventoryOpen;
                    //还原飞行时间
                    Player.wingTime = frozenWingTime;
                    Player.rocketTime = frozenRocketTime;
                }
                positionCaptured = false;
                return;
            }

            //首次冻结时快照位置、朝向、动画帧及各类计时器
            if (!positionCaptured) {
                frozenPosition = Player.position;
                frozenDirection = Player.direction;
                frozenBodyFrame = Player.bodyFrame;
                frozenLegFrame = Player.legFrame;
                frozenHeadFrame = Player.headFrame;
                frozenPotionDelay = Player.potionDelay;
                frozenRestorationDelayTime = Player.restorationDelayTime;
                frozenItemAnimation = Player.itemAnimation;
                frozenItemAnimationMax = Player.itemAnimationMax;
                frozenItemTime = Player.itemTime;
                frozenImmuneTime = Player.immuneTime;
                frozenImmune = Player.immune;
                frozenBreath = Player.breath;
                frozenBreathCD = Player.breathCD;
                CWRRef.SnapshotRippers(Player, ref frozenRage, ref frozenAdrenaline
                    , ref frozenRageGainCooldown, ref frozenRageCombatFrames, ref frozenAdrenalinePauseTimer);
                frozenStatLife = Player.statLife;
                frozenLifeRegenTime = Player.lifeRegenTime;
                frozenStatMana = Player.statMana;
                frozenManaRegenDelay = Player.manaRegenDelay;
                frozenBuffTime ??= new int[Player.MaxBuffs];
                frozenBuffType ??= new int[Player.MaxBuffs];
                Array.Copy(Player.buffTime, frozenBuffTime, Player.MaxBuffs);
                Array.Copy(Player.buffType, frozenBuffType, Player.MaxBuffs);
                //快照飞行时间
                frozenWingTime = Player.wingTime;
                frozenRocketTime = Player.rocketTime;
                positionCaptured = true;
                //背包开启状态
                snapshotInventoryOpen = Main.playerInventory;
            }

            //锁定位置和速度
            Player.position = frozenPosition;
            Player.velocity = Vector2.Zero;
            //锁定朝向
            Player.direction = frozenDirection;
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
            //还原各类冷却计时器，使其在冻结期间不流逝
            Player.potionDelay = frozenPotionDelay;
            Player.restorationDelayTime = frozenRestorationDelayTime;
            Player.itemAnimation = frozenItemAnimation;
            Player.itemAnimationMax = frozenItemAnimationMax;
            Player.itemTime = frozenItemTime;
            Player.immuneTime = frozenImmuneTime;
            Player.immune = frozenImmune;
            Player.breath = frozenBreath;
            Player.breathCD = frozenBreathCD;
            //还原Calamity怒气与肾上腺素，阻止冻结期间充能或衰减
            CWRRef.RestoreRippers(Player, frozenRage, frozenAdrenaline
                , frozenRageGainCooldown, frozenRageCombatFrames, frozenAdrenalinePauseTimer);
            //阻止HP和魔力在冻结期间自然恢复
            Player.statLife = frozenStatLife;
            Player.lifeRegenTime = frozenLifeRegenTime;
            Player.statMana = frozenStatMana;
            Player.manaRegenDelay = frozenManaRegenDelay;
            //还原buff计时，阻止药水病/闪避冷却等在冻结期间流逝
            for (int i = 0; i < Player.MaxBuffs; i++) {
                if (Player.buffType[i] != 0 && Player.buffType[i] == frozenBuffType[i]) {
                    Player.buffTime[i] = frozenBuffTime[i];
                }
            }
            //还原飞行时间，阻止翅膀耐久在冻结期间消耗或被系统归零
            Player.wingTime = frozenWingTime;
            Player.rocketTime = frozenRocketTime;

            //关闭背包
            Main.playerInventory = false;
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

        public override void UpdateDead() {
            if (Player.whoAmI != Main.myPlayer) return;
            if (HackTime.Active || HackTimeFreeze.IsActive) {
                HackTime.Deactivate();
            }
        }
    }
}
