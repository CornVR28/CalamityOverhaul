using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

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
            if (!Cyberspace.Active || Cyberspace.Intensity < 0.5f) return;

            Vector2 mouse = Main.MouseWorld;
            Vector2 domainCenter = Main.LocalPlayer.Center;
            float effectiveRadius = Cyberspace.Radius * Cyberspace.ExpandProgress;

            int bestIndex = -1;
            float bestDistSq = float.MaxValue;

            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.townNPC) continue;

                // 必须在领域内
                float dx = npc.Center.X - domainCenter.X;
                float dy = npc.Center.Y - domainCenter.Y;
                if (dx * dx + dy * dy > effectiveRadius * effectiveRadius) continue;

                // 已在放逐中则跳过
                if (IsBanishing(i)) continue;

                // 光标命中判定（使用NPC碰撞箱 + 宽容边距）
                Rectangle hitbox = npc.Hitbox;
                hitbox.Inflate(8, 8);
                if (!hitbox.Contains(mouse.ToPoint())) continue;

                float distSq = Vector2.DistanceSquared(npc.Center, mouse);
                if (distSq < bestDistSq) {
                    bestDistSq = distSq;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0) {
                StartBanish(bestIndex);
            }
        }

        /// <summary>
        /// 启动对指定NPC的放逐
        /// </summary>
        public static void StartBanish(int npcIndex) {
            if (IsBanishing(npcIndex)) return;

            NPC npc = Main.npc[npcIndex];
            if (!npc.active) return;

            ActiveBanishments.Add(new BanishEntry {
                NpcIndex = npcIndex,
                Timer = 0,
                OriginalScale = npc.scale,
                FreezePosition = npc.Center,
                Seed = Main.rand.NextFloat()
            });

            // 冻结NPC运动
            npc.velocity = Vector2.Zero;
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

                // 冻结NPC位置
                npc.Center = entry.FreezePosition;
                npc.velocity = Vector2.Zero;

                float progress = entry.Progress;

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
        /// 放逐进度 0→1
        /// </summary>
        public float Progress => (float)Timer / CyberBanish.BanishDuration;
    }
}
