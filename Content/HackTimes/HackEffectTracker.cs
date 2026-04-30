using CalamityOverhaul.Common;
using CalamityOverhaul.Content.RAMSystems;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;

namespace CalamityOverhaul.Content.HackTimes
{
    /// <summary>
    /// 单个骇入效果的运行时实例
    /// <br/>统一承载 NPC / 物块两类持续性骇入效果，目标通过<see cref="IHackTarget"/>抽象
    /// </summary>
    internal class ActiveHackEffect
    {
        /// <summary>对应的协议定义</summary>
        public QuickHackDef Hack;
        /// <summary>受影响的目标</summary>
        public IHackTarget Target;
        /// <summary>发起骇入的玩家 whoAmI</summary>
        public int CasterIndex;
        /// <summary>已持续帧数</summary>
        public int Elapsed;
        /// <summary>效果是否仍然活跃</summary>
        public bool Active = true;
        /// <summary>是否已对目标调用过 OnApply</summary>
        public bool Applied;
        /// <summary>Boss 时效果倍率（Boss 为 0.5f，普通为 1f）</summary>
        public float EffectMult = 1f;
        /// <summary>传播代数（用于蔓延协议的一跳限制。0=初始施加，1=已传播一次）</summary>
        public int Generation;

        //----- 兼容旧 API：暴露 NPC / Tile 维度的便捷查询 -----

        /// <summary>当 Target 为 NpcScannable 时返回对应 NPC 索引，否则返回 -1</summary>
        public int TargetIndex => Target is NpcScannable n ? n.NpcIndex : -1;
        /// <summary>当 Target 为 TileScannable 时返回对应物块 X，否则返回 -1</summary>
        public int TileX => Target is TileScannable t ? t.TileCoordX : -1;
        /// <summary>当 Target 为 TileScannable 时返回对应物块 Y，否则返回 -1</summary>
        public int TileY => Target is TileScannable t ? t.TileCoordY : -1;
    }

    /// <summary>
    /// 骇入效果全局追踪器
    /// <br/>管理所有目标身上正在生效的骇入协议，驱动效果生命周期（Apply→Tick→Remove）
    /// <br/>无叠加限制，同一目标可承受任意数量的不同协议
    /// </summary>
    internal class HackEffectTracker : ICWRLoader
    {
        //所有 NPC 维度的活跃效果
        private static readonly List<ActiveHackEffect> activeEffects = [];
        //帧内移除缓冲
        private static readonly List<ActiveHackEffect> removeBuffer = [];

        //所有 Tile 维度的活跃效果
        private static readonly List<ActiveHackEffect> activeTileEffects = [];
        private static readonly List<ActiveHackEffect> tileRemoveBuffer = [];

        //击杀回收 RAM 的比例（返还协议 RamCost 的百分比）
        private const float KillRefundRatio = 0.5f;
        //本帧已处理击杀回收的 NPC 索引集（避免同一 NPC 多效果重复回收）
        private static readonly HashSet<int> killRefundedThisFrame = [];

        void ICWRLoader.UnLoadData() => Reset();

        #region NPC 效果

        /// <summary>
        /// 对指定 NPC 施加一个骇入协议效果
        /// </summary>
        public static ActiveHackEffect ApplyNpcEffect(QuickHackDef hack, int targetIndex, int casterIndex) {
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs) return null;
            NPC npc = Main.npc[targetIndex];
            if (npc == null || !npc.active) return null;

            var target = new NpcScannable(targetIndex);
            if (!hack.CanApplyTo(target)) return null;

            var effect = new ActiveHackEffect {
                Hack = hack,
                Target = target,
                CasterIndex = casterIndex,
                Elapsed = 0,
                Active = true,
                Applied = false,
                //Boss 效果减半
                EffectMult = npc.boss ? 0.5f : 1f,
            };

            activeEffects.Add(effect);
            return effect;
        }

        /// <summary>兼容旧 API：等价于 <see cref="ApplyNpcEffect"/></summary>
        public static ActiveHackEffect Apply(QuickHackDef hack, int targetIndex, int casterIndex)
            => ApplyNpcEffect(hack, targetIndex, casterIndex);

        /// <summary>每帧更新所有 NPC 维度的活跃效果</summary>
        public static void Update() {
            removeBuffer.Clear();
            killRefundedThisFrame.Clear();

            for (int i = 0; i < activeEffects.Count; i++) {
                var eff = activeEffects[i];
                if (!eff.Active) {
                    removeBuffer.Add(eff);
                    continue;
                }

                NPC npc = eff.Target is NpcScannable n ? Main.npc[n.NpcIndex] : null;
                //目标死亡或失效则移除
                if (npc == null || !npc.active || npc.life <= 0) {
                    if (!HackTime.InfiniteHack && npc != null && eff.Target is NpcScannable np
                        && !killRefundedThisFrame.Contains(np.NpcIndex)) {
                        OnHackedTargetKilled(npc, np.NpcIndex);
                    }
                    eff.Active = false;
                    removeBuffer.Add(eff);
                    continue;
                }

                Player caster = eff.CasterIndex >= 0 && eff.CasterIndex < Main.maxPlayers
                    ? Main.player[eff.CasterIndex] : Main.LocalPlayer;

                //首帧触发 OnApply
                if (!eff.Applied) {
                    eff.Applied = true;
                    eff.Hack.OnApply(eff.Target, caster);
                }

                //驱动 TickEffect（内含持续时间检查）；Boss 减半：实际 duration = GetDuration * EffectMult
                int effectiveDuration = (int)(eff.Hack.GetDuration() * eff.EffectMult);
                if (effectiveDuration > 0 && eff.Elapsed >= effectiveDuration) {
                    eff.Hack.OnRemove(eff.Target);
                    eff.Active = false;
                    removeBuffer.Add(eff);
                    continue;
                }

                //即时效果（duration=0）在 Apply 后直接结束
                if (eff.Hack.GetDuration() == 0 && eff.Applied) {
                    eff.Active = false;
                    removeBuffer.Add(eff);
                    continue;
                }

                //持续效果每帧 tick
                bool alive = eff.Hack.OnTick(eff.Target, eff.Elapsed);
                if (!alive) {
                    eff.Hack.OnRemove(eff.Target);
                    eff.Active = false;
                    removeBuffer.Add(eff);
                    continue;
                }

                eff.Elapsed++;
            }

            for (int i = 0; i < removeBuffer.Count; i++) {
                activeEffects.Remove(removeBuffer[i]);
            }
        }

        /// <summary>查询指定 NPC 身上是否有某类型的活跃效果</summary>
        public static bool HasEffect<T>(int npcIndex) where T : QuickHackDef {
            for (int i = 0; i < activeEffects.Count; i++) {
                var e = activeEffects[i];
                if (e.Active && e.Hack is T && e.Target is NpcScannable n && n.NpcIndex == npcIndex)
                    return true;
            }
            return false;
        }

        /// <summary>获取指定 NPC 身上某类型效果的进度(0~1)，不存在返回 -1</summary>
        public static float GetEffectProgress<T>(int npcIndex) where T : QuickHackDef {
            for (int i = 0; i < activeEffects.Count; i++) {
                var eff = activeEffects[i];
                if (!eff.Active || eff.Hack is not T) continue;
                if (eff.Target is not NpcScannable n || n.NpcIndex != npcIndex) continue;
                int dur = (int)(eff.Hack.GetDuration() * eff.EffectMult);
                if (dur <= 0) return 1f;
                return Math.Clamp((float)eff.Elapsed / dur, 0f, 1f);
            }
            return -1f;
        }

        /// <summary>获取指定 NPC 身上所有活跃效果</summary>
        public static void GetEffects(int npcIndex, List<ActiveHackEffect> result) {
            result.Clear();
            for (int i = 0; i < activeEffects.Count; i++) {
                var e = activeEffects[i];
                if (e.Active && e.Target is NpcScannable n && n.NpcIndex == npcIndex)
                    result.Add(e);
            }
        }

        /// <summary>获取指定 NPC 身上某类型效果的实例，不存在返回 null</summary>
        public static ActiveHackEffect GetEffect<T>(int npcIndex) where T : QuickHackDef {
            for (int i = 0; i < activeEffects.Count; i++) {
                var eff = activeEffects[i];
                if (eff.Active && eff.Hack is T && eff.Target is NpcScannable n && n.NpcIndex == npcIndex)
                    return eff;
            }
            return null;
        }

        /// <summary>所有 NPC 维度的活跃效果列表（只读访问）</summary>
        public static IReadOnlyList<ActiveHackEffect> AllActiveEffects => activeEffects;

        //击杀带有骇入效果的 NPC 时，汇总所有效果的 RAM 消耗并按比例返还
        private static void OnHackedTargetKilled(NPC target, int npcIndex) {
            killRefundedThisFrame.Add(npcIndex);

            int totalCost = 0;
            for (int i = 0; i < activeEffects.Count; i++) {
                var e = activeEffects[i];
                if (e.Active && e.Target is NpcScannable n && n.NpcIndex == npcIndex)
                    totalCost += e.Hack.RamCost;
            }
            if (totalCost <= 0) return;

            float refund = totalCost * KillRefundRatio;
            if (refund < 1f) refund = 1f;
            float before = RamSystem.CurrentRam;
            RamSystem.Restore(refund);
            float actual = RamSystem.CurrentRam - before;

            if (actual > 0.01f && !VaultUtils.isServer) {
                string text = HackTime.RamRefund.Format(actual.ToString("F0"));
                CombatText.NewText(target.Hitbox, HackTheme.Accent, text, true);
                SoundEngine.PlaySound(CWRSound.Hacker with { Volume = 0.35f, Pitch = 0.4f },
                    target.Center);
            }
        }

        #endregion

        #region 物块效果

        /// <summary>对指定物块施加一个骇入协议效果</summary>
        public static ActiveHackEffect ApplyTileEffect(QuickHackDef hack, int tileX, int tileY, int casterIndex) {
            if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
                return null;
            if (!Main.tile[tileX, tileY].HasTile) return null;

            var target = new TileScannable(tileX, tileY);
            if (!hack.CanApplyTo(target)) return null;

            var effect = new ActiveHackEffect {
                Hack = hack,
                Target = target,
                CasterIndex = casterIndex,
                Elapsed = 0,
                Active = true,
                Applied = false,
            };

            activeTileEffects.Add(effect);
            return effect;
        }

        /// <summary>兼容旧 API：等价于 <see cref="ApplyTileEffect"/></summary>
        public static ActiveHackEffect ApplyToTile(QuickHackDef hack, int tileX, int tileY, int casterIndex)
            => ApplyTileEffect(hack, tileX, tileY, casterIndex);

        /// <summary>每帧更新所有物块效果</summary>
        public static void UpdateTileEffects() {
            tileRemoveBuffer.Clear();

            for (int i = 0; i < activeTileEffects.Count; i++) {
                var eff = activeTileEffects[i];
                if (!eff.Active) {
                    tileRemoveBuffer.Add(eff);
                    continue;
                }

                if (eff.Target is not TileScannable ts || !ts.IsValid) {
                    eff.Active = false;
                    tileRemoveBuffer.Add(eff);
                    continue;
                }

                Player caster = eff.CasterIndex >= 0 && eff.CasterIndex < Main.maxPlayers
                    ? Main.player[eff.CasterIndex] : Main.LocalPlayer;

                //首帧触发 OnApply
                if (!eff.Applied) {
                    eff.Applied = true;
                    eff.Hack.OnApply(eff.Target, caster);
                }

                int duration = eff.Hack.GetDuration();
                if (duration > 0 && eff.Elapsed >= duration) {
                    eff.Hack.OnRemove(eff.Target);
                    eff.Active = false;
                    tileRemoveBuffer.Add(eff);
                    continue;
                }

                if (duration == 0 && eff.Applied) {
                    eff.Active = false;
                    tileRemoveBuffer.Add(eff);
                    continue;
                }

                bool alive = eff.Hack.OnTick(eff.Target, eff.Elapsed);
                if (!alive) {
                    eff.Hack.OnRemove(eff.Target);
                    eff.Active = false;
                    tileRemoveBuffer.Add(eff);
                    continue;
                }

                eff.Elapsed++;
            }

            for (int i = 0; i < tileRemoveBuffer.Count; i++) {
                activeTileEffects.Remove(tileRemoveBuffer[i]);
            }
        }

        /// <summary>查询指定物块位置是否有某类型的活跃效果</summary>
        public static bool HasTileEffect<T>(int tileX, int tileY) where T : QuickHackDef {
            for (int i = 0; i < activeTileEffects.Count; i++) {
                var e = activeTileEffects[i];
                if (e.Active && e.Hack is T && e.Target is TileScannable t
                    && t.TileCoordX == tileX && t.TileCoordY == tileY)
                    return true;
            }
            return false;
        }

        /// <summary>获取指定物块坐标上所有活跃效果</summary>
        public static void GetTileEffects(int tileX, int tileY, List<ActiveHackEffect> result) {
            result.Clear();
            for (int i = 0; i < activeTileEffects.Count; i++) {
                var e = activeTileEffects[i];
                if (e.Active && e.Target is TileScannable t
                    && t.TileCoordX == tileX && t.TileCoordY == tileY)
                    result.Add(e);
            }
        }

        /// <summary>所有活跃的物块效果（只读）</summary>
        public static IReadOnlyList<ActiveHackEffect> AllActiveTileEffects => activeTileEffects;

        #endregion

        public static void Reset() {
            activeEffects.Clear();
            removeBuffer.Clear();
            killRefundedThisFrame.Clear();
            activeTileEffects.Clear();
            tileRemoveBuffer.Clear();
        }
    }
}
