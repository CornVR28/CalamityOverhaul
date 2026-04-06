using CalamityOverhaul.Common;
using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Banish;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.DomainFreeze
{
    /// <summary>
    /// 赛博领域冻结系统 —— 状态管理器
    /// <br/>按下按键后冻结领域范围内所有敌对NPC和弹幕，每个实体有独立的冻结计时
    /// <br/>冻结时生成黑墙风格能量波扩散演出
    /// <br/>被冻结的实体带有故障滤镜 + 六角能量网格覆盖效果
    /// </summary>
    internal class CyberDomainFreeze : ICWRLoader
    {
        void ICWRLoader.UnLoadData() => Reset();

        /// <summary>
        /// 默认冻结时长（帧，10秒 = 600帧）
        /// </summary>
        public const int DefaultFreezeDuration = 600;

        /// <summary>
        /// 当前正在被冻结的NPC列表
        /// </summary>
        public static readonly List<FreezeEntry> FrozenNPCs = [];

        /// <summary>
        /// 当前正在被冻结的弹幕列表
        /// </summary>
        public static readonly List<FreezeProjEntry> FrozenProjectiles = [];

        /// <summary>
        /// 判断某NPC是否正在被冻结
        /// </summary>
        public static bool IsNPCFrozen(int npcIndex) {
            for (int i = 0; i < FrozenNPCs.Count; i++) {
                if (FrozenNPCs[i].EntityIndex == npcIndex)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 判断某弹幕是否正在被冻结
        /// </summary>
        public static bool IsProjectileFrozen(int projIndex) {
            for (int i = 0; i < FrozenProjectiles.Count; i++) {
                if (FrozenProjectiles[i].EntityIndex == projIndex)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取某NPC的冻结进度 (0=刚冻结, 1=即将解冻)，不在冻结中返回-1
        /// </summary>
        public static float GetNPCFreezeProgress(int npcIndex) {
            for (int i = 0; i < FrozenNPCs.Count; i++) {
                if (FrozenNPCs[i].EntityIndex == npcIndex)
                    return FrozenNPCs[i].Progress;
            }
            return -1f;
        }

        /// <summary>
        /// 获取某弹幕的冻结进度
        /// </summary>
        public static float GetProjectileFreezeProgress(int projIndex) {
            for (int i = 0; i < FrozenProjectiles.Count; i++) {
                if (FrozenProjectiles[i].EntityIndex == projIndex)
                    return FrozenProjectiles[i].Progress;
            }
            return -1f;
        }

        /// <summary>
        /// 获取某NPC的冻结种子
        /// </summary>
        public static float GetNPCSeed(int npcIndex) {
            for (int i = 0; i < FrozenNPCs.Count; i++) {
                if (FrozenNPCs[i].EntityIndex == npcIndex)
                    return FrozenNPCs[i].Seed;
            }
            return 0f;
        }

        /// <summary>
        /// 触发领域冻结：冻结领域内所有敌对实体 + 生成能量波
        /// </summary>
        public static void TriggerFreeze(Player owner) {
            if (!Cyberspace.Active || Cyberspace.Intensity < 0.5f) return;

            Vector2 domainCenter = owner.Center;
            float effectiveRadius = Cyberspace.Radius * Cyberspace.ExpandProgress;
            float radiusSq = effectiveRadius * effectiveRadius;

            // 冻结域内所有敌对NPC
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.townNPC) continue;
                if (IsNPCFrozen(i)) continue;
                // 正在被放逐的NPC不冻结
                if (CyberBanish.IsBanishing(i)) continue;

                float dx = npc.Center.X - domainCenter.X;
                float dy = npc.Center.Y - domainCenter.Y;
                if (dx * dx + dy * dy > radiusSq) continue;

                FrozenNPCs.Add(new FreezeEntry {
                    EntityIndex = i,
                    Timer = 0,
                    Duration = DefaultFreezeDuration,
                    FreezePosition = npc.Center,
                    Seed = Main.rand.NextFloat(),
                    FreezeVelocity = npc.velocity
                });
                npc.velocity = Vector2.Zero;
            }

            // 冻结域内所有敌对弹幕
            for (int i = 0; i < Main.maxProjectiles; i++) {
                Projectile proj = Main.projectile[i];
                if (!proj.active) continue;
                if (proj.friendly) continue;
                if (Main.projPet[proj.type] || proj.minion || Main.projHook[proj.type]) continue;
                if (IsProjectileFrozen(i)) continue;

                float dx = proj.Center.X - domainCenter.X;
                float dy = proj.Center.Y - domainCenter.Y;
                if (dx * dx + dy * dy > radiusSq) continue;

                FrozenProjectiles.Add(new FreezeProjEntry {
                    EntityIndex = i,
                    Timer = 0,
                    Duration = DefaultFreezeDuration,
                    FreezePosition = proj.Center,
                    Seed = Main.rand.NextFloat(),
                    FreezeVelocity = proj.velocity
                });
                proj.velocity = Vector2.Zero;
            }

            // 生成黑墙能量波弹幕
            if (Main.myPlayer == owner.whoAmI) {
                IEntitySource source = owner.GetSource_FromThis();
                Projectile.NewProjectile(source, owner.Center, Vector2.Zero,
                    ModContent.ProjectileType<CyberFreezeWaveProj>(), 0, 0, owner.whoAmI);
            }
        }

        /// <summary>
        /// 每帧更新所有冻结实体
        /// </summary>
        public static void Update() {
            UpdateFrozenNPCs();
            UpdateFrozenProjectiles();
        }

        private static void UpdateFrozenNPCs() {
            for (int i = FrozenNPCs.Count - 1; i >= 0; i--) {
                FreezeEntry entry = FrozenNPCs[i];
                entry.Timer++;

                NPC npc = Main.npc[entry.EntityIndex];
                if (!npc.active) {
                    FrozenNPCs.RemoveAt(i);
                    continue;
                }

                // 生成冻结粒子（仅客户端）
                if (!Main.dedServ) {
                    CyberDomainFreezeParticles.SpawnFreezeParticles(npc, entry.Progress, entry.Seed);
                }

                // 解冻动画（最后15%时间）
                float progress = entry.Progress;
                if (progress > 0.85f) {
                    float thawPhase = (progress - 0.85f) / 0.15f;
                    // 逐渐恢复一点速度抖动表示即将解冻
                    float jitter = thawPhase * 2f;
                    npc.position += new Vector2(
                        Main.rand.NextFloat(-jitter, jitter),
                        Main.rand.NextFloat(-jitter, jitter));
                }

                if (entry.Timer == MathHelper.Max(0, entry.Duration - 90)) {
                    if (!VaultUtils.isServer) {
                        SoundEngine.PlaySound(CWRSound.FaultTransition, npc.Center);
                    }
                }

                // 冻结时间结束 → 解冻
                if (entry.Timer >= entry.Duration) {
                    // 恢复原始速度
                    npc.velocity = entry.FreezeVelocity * 0.5f;
                    if (!Main.dedServ) {
                        CyberDomainFreezeParticles.SpawnThawBurst(npc.Center);
                    }
                    FrozenNPCs.RemoveAt(i);
                }
            }
        }

        private static void UpdateFrozenProjectiles() {
            for (int i = FrozenProjectiles.Count - 1; i >= 0; i--) {
                FreezeProjEntry entry = FrozenProjectiles[i];
                entry.Timer++;

                Projectile proj = Main.projectile[entry.EntityIndex];
                if (!proj.active) {
                    FrozenProjectiles.RemoveAt(i);
                    continue;
                }

                // 冻结时间结束 → 解冻
                if (entry.Timer >= entry.Duration) {
                    proj.velocity = entry.FreezeVelocity;
                    FrozenProjectiles.RemoveAt(i);
                }
            }
        }

        public static void Reset() {
            FrozenNPCs.Clear();
            FrozenProjectiles.Clear();
        }
    }

    /// <summary>
    /// NPC冻结数据条目
    /// </summary>
    internal class FreezeEntry
    {
        public int EntityIndex;
        public int Timer;
        public int Duration;
        public Vector2 FreezePosition;
        public Vector2 FreezeVelocity;
        public float Seed;

        public float Progress => (float)Timer / Duration;
    }

    /// <summary>
    /// 弹幕冻结数据条目
    /// </summary>
    internal class FreezeProjEntry
    {
        public int EntityIndex;
        public int Timer;
        public int Duration;
        public Vector2 FreezePosition;
        public Vector2 FreezeVelocity;
        public float Seed;

        public float Progress => (float)Timer / Duration;
    }
}
