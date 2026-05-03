using CalamityOverhaul.Common;
using CalamityOverhaul.Content.HackTimes;
using CalamityOverhaul.Content.RAMSystems;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Banish
{
    /// <summary>
    /// 赛博放逐系统 —— 状态管理器
    /// <br/>将域内目标放逐到深层赛博空间：触发故障着色器 → 缩小高光消失 → 无声移除
    /// <br/>独立于HackTime，只要赛博空间Active即可使用
    /// </summary>
    internal class CyberBanish : ICWRLoader
    {
        void ICWRLoader.UnLoadData() => Reset();

        /// <summary>
        /// 放逐动画总时长（帧，约1.8秒 = 108帧）
        /// </summary>
        public const int BanishDuration = 108;

        /// <summary>
        /// 单次放逐消耗的RAM量
        /// </summary>
        public const int RamCostPerCast = 5;

        /// <summary>
        /// 当前正在被放逐的NPC列表
        /// </summary>
        public static readonly List<BanishEntry> ActiveBanishments = [];

        /// <summary>
        /// 判断某NPC是否正在被放逐
        /// </summary>
        public static bool IsBanishing(int npcIndex) {
            for (int i = 0; i < ActiveBanishments.Count; i++) {
                if (ActiveBanishments[i].NpcIndex == npcIndex)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取某NPC的放逐进度 (0=刚开始, 1=完成)，不在放逐中返回-1
        /// </summary>
        public static float GetProgress(int npcIndex) {
            for (int i = 0; i < ActiveBanishments.Count; i++) {
                if (ActiveBanishments[i].NpcIndex == npcIndex)
                    return ActiveBanishments[i].Progress;
            }
            return -1f;
        }

        /// <summary>
        /// 对光标下的NPC发起放逐
        /// </summary>
        public static void BanishAtCursor() {
            if (!Cyberspace.Active || Cyberspace.Intensity < 0.5f || Cyberspace.CurrentLayer < 2) return;

            //先尝试找命中目标，再依据目标是否为Boss级决定走哪条RAM消耗与处理路径
            int hitIndex = FindCursorTarget();
            if (hitIndex < 0) return;

            NPC hitNpc = Main.npc[hitIndex];
            bool boss = CyberBossExecution.IsBossTier(hitNpc);
            int ramCost = boss ? CyberBossExecution.RamCostPerCast : RamCostPerCast;

            //RAM检查：不足时触发HUD故障闪烁提示
            if (!HackTime.InfiniteHack && (RamSystem.IsLocked || !RamSystem.CanAfford(ramCost))) {
                if (!VaultUtils.isServer) {
                    SoundEngine.PlaySound(CWRSound.FailureCurrent with {
                        Volume = 0.4f,
                        Pitch = -0.3f,
                    }, Main.LocalPlayer.Center);
                    RamSystem.NotifyInsufficient();
                    Terraria.CombatText.NewText(Main.LocalPlayer.Hitbox, new Microsoft.Xna.Framework.Color(255, 90, 80), "// LOW RAM", true);
                }
                return;
            }

            //命中目标后消耗RAM
            if (!HackTime.InfiniteHack) {
                RamSystem.TryConsume(ramCost);
            }

            if (boss) {
                //Boss级目标：走完整放逐滤镜演出，收尾不抹除而是起动天雷打击
                StartBanish(hitIndex, isBoss: true);
                NPC bossRoot = Main.npc[hitIndex];
                NpcGroupHelper.CollectGroupIndices(bossRoot, banishGroupBuffer);
                for (int i = 0; i < banishGroupBuffer.Count; i++) {
                    int memberIdx = banishGroupBuffer[i];
                    if (memberIdx == hitIndex) continue;
                    if (IsBanishing(memberIdx)) continue;
                    //群组成员同样进入Boss版演出，由主体负责触发雷击
                    StartBanish(memberIdx, isBoss: true);
                }
                banishGroupBuffer.Clear();
                return;
            }

            //普通目标：维持原有放逐演出，并将群组成员一并拉入放逐
            StartBanish(hitIndex);
            NPC root = Main.npc[hitIndex];
            NpcGroupHelper.CollectGroupIndices(root, banishGroupBuffer);
            for (int i = 0; i < banishGroupBuffer.Count; i++) {
                int memberIdx = banishGroupBuffer[i];
                if (memberIdx == hitIndex) continue;
                if (IsBanishing(memberIdx)) continue;
                StartBanish(memberIdx);
            }
            banishGroupBuffer.Clear();
        }

        /// <summary>
        /// 在领域内、且光标命中的最近未处理NPC，找不到时返回-1
        /// </summary>
        private static int FindCursorTarget() {
            Vector2 mouse = Main.MouseWorld;
            Vector2 domainCenter = Cyberspace.DomainCenter;
            float effectiveRadius = Cyberspace.Radius * Cyberspace.ExpandProgress;

            int bestIndex = -1;
            float bestDistSq = float.MaxValue;

            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.townNPC) continue;

                float dx = npc.Center.X - domainCenter.X;
                float dy = npc.Center.Y - domainCenter.Y;
                if (dx * dx + dy * dy > effectiveRadius * effectiveRadius) continue;

                if (IsBanishing(i)) continue;
                if (CyberBossExecution.IsExecuting(i)) continue;

                Rectangle hitbox = npc.Hitbox;
                hitbox.Inflate(8, 8);
                if (!hitbox.Contains(mouse.ToPoint())) continue;

                float distSq = Vector2.DistanceSquared(npc.Center, mouse);
                if (distSq < bestDistSq) {
                    bestDistSq = distSq;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        //群组放逐用的复用缓冲，避免每次扩散都重新分配
        private static readonly List<int> banishGroupBuffer = [];

        /// <summary>
        /// 启动对指定NPC的放逐
        /// </summary>
        public static void StartBanish(int npcIndex, bool isBoss = false) {
            if (IsBanishing(npcIndex)) return;

            NPC npc = Main.npc[npcIndex];
            if (!npc.active) return;

            ActiveBanishments.Add(new BanishEntry {
                NpcIndex = npcIndex,
                Timer = 0,
                OriginalScale = npc.scale,
                FreezePosition = npc.Center,
                Seed = Main.rand.NextFloat(),
                IsBoss = isBoss,
                OwnerWho = Main.myPlayer,
                ExecutionTriggered = false,
            });

            // 冻结NPC运动
            npc.velocity = Vector2.Zero;

            if (!VaultUtils.isServer) {
                SoundEngine.PlaySound(CWRSound.Fault, Main.LocalPlayer.Center);
            }
        }

        /// <summary>
        /// 每帧更新所有活跃放逐
        /// </summary>
        public static void Update() {
            for (int i = ActiveBanishments.Count - 1; i >= 0; i--) {
                BanishEntry entry = ActiveBanishments[i];
                entry.Timer++;

                NPC npc = Main.npc[entry.NpcIndex];
                if (!npc.active) {
                    ActiveBanishments.RemoveAt(i);
                    continue;
                }

                float progress = entry.Progress;

                // 冻结NPC位置
                npc.Center = entry.FreezePosition;
                npc.velocity = Vector2.Zero;

                if (entry.IsBoss) {
                    //Boss版：不缩小、不抹除。末段触发雷击后仍保留滤镜到结束
                    if (!Main.dedServ) {
                        CyberBanishParticles.SpawnBanishParticles(npc, progress, entry.Seed);
                    }

                    if (!entry.ExecutionTriggered && progress >= 0.7f) {
                        entry.ExecutionTriggered = true;
                        Player owner = entry.OwnerWho >= 0 && entry.OwnerWho < Main.maxPlayers ? Main.player[entry.OwnerWho] : Main.LocalPlayer;
                        CyberBossExecution.StartExecution(entry.NpcIndex, owner);
                    }

                    if (entry.Timer >= BanishDuration) {
                        ActiveBanishments.RemoveAt(i);
                    }
                    continue;
                }

                // 阶段一 (0~0.5): 强烈故障闪烁，NPC保持原大小
                // 阶段二 (0.5~0.85): 开始缩小 + 更激烈故障
                // 阶段三 (0.85~1.0): 急速缩小 → 高光闪白 → 消失
                if (progress > 0.5f) {
                    float shrinkPhase = (progress - 0.5f) / 0.5f;
                    float shrink = 1f - MathF.Pow(shrinkPhase, 2.2f);
                    npc.scale = entry.OriginalScale * Math.Max(shrink, 0.02f);
                }

                // 每帧生成故障粒子（仅客户端）
                if (!Main.dedServ) {
                    CyberBanishParticles.SpawnBanishParticles(npc, progress, entry.Seed);
                }

                // 动画完毕 → 无声移除
                if (entry.Timer >= BanishDuration) {
                    npc.active = false;
                    npc.life = 0;
                    // 最终爆发粒子（仅客户端）
                    if (!Main.dedServ) {
                        CyberBanishParticles.SpawnFinalBurst(npc.Center, entry.OriginalScale);
                    }
                    ActiveBanishments.RemoveAt(i);
                }
            }
        }

        public static void Reset() {
            ActiveBanishments.Clear();
        }
    }

    /// <summary>
    /// 单个NPC的放逐数据
    /// </summary>
    internal class BanishEntry
    {
        public int NpcIndex;
        public int Timer;
        public float OriginalScale;
        public Vector2 FreezePosition;
        public float Seed;
        /// <summary>
        /// 是否为Boss级演出：不缩小不抹除，末段触发<see cref="CyberBossExecution"/>
        /// </summary>
        public bool IsBoss;
        /// <summary>
        /// 发起者whoAmI，Boss版雷击伤害会从该玩家读取SHPC面板
        /// </summary>
        public int OwnerWho;
        /// <summary>
        /// Boss版是否已触发雷击，避免重复调用StartExecution
        /// </summary>
        public bool ExecutionTriggered;

        /// <summary>
        /// 放逐进度 0→1
        /// </summary>
        public float Progress => (float)Timer / CyberBanish.BanishDuration;
    }
}
