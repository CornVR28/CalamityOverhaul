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
    /// </summary>
    internal class ActiveHackEffect
    {
        /// <summary>
        /// 对应的协议定义
        /// </summary>
        public QuickHackDef Hack;
        /// <summary>
        /// 发起骇入的玩家whoAmI
        /// </summary>
        public int CasterIndex;
        /// <summary>
        /// 目标NPC的whoAmI
        /// </summary>
        public int TargetIndex;
        /// <summary>
        /// 已持续帧数
        /// </summary>
        public int Elapsed;
        /// <summary>
        /// 效果是否仍然活跃
        /// </summary>
        public bool Active = true;
        /// <summary>
        /// 是否已对目标调用过OnApply
        /// </summary>
        public bool Applied;
        /// <summary>
        /// Boss时效果倍率（Boss为0.5f，普通为1f）
        /// </summary>
        public float EffectMult = 1f;
        /// <summary>
        /// 传播代数（用于蔓延协议的一跳限制。0=初始施加，1=已传播一次）
        /// </summary>
        public int Generation;
    }

    /// <summary>
    /// 物块骇入效果的运行时实例
    /// </summary>
    internal class ActiveTileHackEffect
    {
        public QuickHackDef Hack;
        public int CasterIndex;
        public int TileX;
        public int TileY;
        public int Elapsed;
        public bool Active = true;
        public bool Applied;
    }

    /// <summary>
    /// 骇入效果全局追踪器
    /// <br/>管理所有NPC身上正在生效的骇入协议，驱动效果生命周期（Apply→Tick→Remove）
    /// <br/>无叠加限制，同一NPC可承受任意数量的不同协议
    /// </summary>
    internal class HackEffectTracker : ICWRLoader
    {
        //所有活跃效果
        private static readonly List<ActiveHackEffect> activeEffects = [];
        //帧内移除缓冲
        private static readonly List<ActiveHackEffect> removeBuffer = [];
        //击杀回收RAM的比例（返还协议RamCost的百分比）
        private const float KillRefundRatio = 0.5f;
        //本帧已处理击杀回收的NPC索引集（避免同一NPC多效果重复回收）
        private static readonly HashSet<int> killRefundedThisFrame = [];

        //物块效果列表
        private static readonly List<ActiveTileHackEffect> activeTileEffects = [];
        private static readonly List<ActiveTileHackEffect> tileRemoveBuffer = [];

        void ICWRLoader.UnLoadData() => Reset();

        /// <summary>
        /// 对指定NPC施加一个骇入协议效果
        /// </summary>
        public static ActiveHackEffect Apply(QuickHackDef hack, int targetIndex, int casterIndex) {
            NPC target = Main.npc[targetIndex];
            if (target == null || !target.active) return null;

            //先检查CanApplyTo
            if (!hack.CanApplyTo(target)) return null;

            var effect = new ActiveHackEffect {
                Hack = hack,
                CasterIndex = casterIndex,
                TargetIndex = targetIndex,
                Elapsed = 0,
                Active = true,
                Applied = false,
                //Boss效果减半
                EffectMult = target.boss ? 0.5f : 1f,
            };

            activeEffects.Add(effect);
            return effect;
        }

        /// <summary>
        /// 每帧更新所有活跃效果
        /// </summary>
        public static void Update() {
            removeBuffer.Clear();
            killRefundedThisFrame.Clear();

            for (int i = 0; i < activeEffects.Count; i++) {
                var eff = activeEffects[i];
                if (!eff.Active) {
                    removeBuffer.Add(eff);
                    continue;
                }

                NPC target = Main.npc[eff.TargetIndex];
                //目标死亡或失效则移除
                if (target == null || !target.active || target.life <= 0) {
                    //击杀回收：目标死亡时返还该协议部分RAM
                    if (!HackTime.InfiniteHack && target != null
                        && !killRefundedThisFrame.Contains(eff.TargetIndex)) {
                        OnHackedTargetKilled(target, eff.TargetIndex);
                    }
                    eff.Active = false;
                    removeBuffer.Add(eff);
                    continue;
                }

                //首帧触发OnApply
                if (!eff.Applied) {
                    eff.Applied = true;
                    Player caster = eff.CasterIndex >= 0 && eff.CasterIndex < Main.maxPlayers
                        ? Main.player[eff.CasterIndex] : Main.LocalPlayer;
                    eff.Hack.OnApply(target, caster);
                }

                //驱动TickEffect（内含持续时间检查）
                //Boss效果减半：实际duration = GetDuration * EffectMult
                int effectiveDuration = (int)(eff.Hack.GetDuration() * eff.EffectMult);
                if (effectiveDuration > 0 && eff.Elapsed >= effectiveDuration) {
                    //到期
                    eff.Hack.OnRemove(target);
                    eff.Active = false;
                    removeBuffer.Add(eff);
                    continue;
                }

                //即时效果（duration=0）在Apply后直接结束
                if (eff.Hack.GetDuration() == 0 && eff.Applied) {
                    eff.Active = false;
                    removeBuffer.Add(eff);
                    continue;
                }

                //持续效果每帧tick
                bool alive = eff.Hack.OnTick(target, eff.Elapsed);
                if (!alive) {
                    eff.Hack.OnRemove(target);
                    eff.Active = false;
                    removeBuffer.Add(eff);
                    continue;
                }

                eff.Elapsed++;
            }

            //清理已结束的效果
            for (int i = 0; i < removeBuffer.Count; i++) {
                activeEffects.Remove(removeBuffer[i]);
            }
        }

        /// <summary>
        /// 查询指定NPC身上是否有某类型的活跃效果
        /// </summary>
        public static bool HasEffect<T>(int npcIndex) where T : QuickHackDef {
            for (int i = 0; i < activeEffects.Count; i++) {
                if (activeEffects[i].Active && activeEffects[i].TargetIndex == npcIndex
                    && activeEffects[i].Hack is T)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取指定NPC身上某类型效果的进度(0~1)，不存在返回-1
        /// </summary>
        public static float GetEffectProgress<T>(int npcIndex) where T : QuickHackDef {
            for (int i = 0; i < activeEffects.Count; i++) {
                var eff = activeEffects[i];
                if (!eff.Active || eff.TargetIndex != npcIndex || eff.Hack is not T) continue;
                int dur = (int)(eff.Hack.GetDuration() * eff.EffectMult);
                if (dur <= 0) return 1f;
                return Math.Clamp((float)eff.Elapsed / dur, 0f, 1f);
            }
            return -1f;
        }

        /// <summary>
        /// 获取指定NPC身上所有活跃效果
        /// </summary>
        public static void GetEffects(int npcIndex, List<ActiveHackEffect> result) {
            result.Clear();
            for (int i = 0; i < activeEffects.Count; i++) {
                if (activeEffects[i].Active && activeEffects[i].TargetIndex == npcIndex)
                    result.Add(activeEffects[i]);
            }
        }

        /// <summary>
        /// 获取指定NPC身上某类型效果的实例，不存在返回null
        /// </summary>
        public static ActiveHackEffect GetEffect<T>(int npcIndex) where T : QuickHackDef {
            for (int i = 0; i < activeEffects.Count; i++) {
                var eff = activeEffects[i];
                if (eff.Active && eff.TargetIndex == npcIndex && eff.Hack is T)
                    return eff;
            }
            return null;
        }

        /// <summary>
        /// 所有活跃效果列表（只读访问）
        /// </summary>
        public static IReadOnlyList<ActiveHackEffect> AllActiveEffects => activeEffects;

        //击杀带有骇入效果的NPC时，汇总所有效果的RAM消耗并按比例返还
        private static void OnHackedTargetKilled(NPC target, int npcIndex) {
            killRefundedThisFrame.Add(npcIndex);

            //汇总该NPC身上所有活跃效果的RAM消耗
            int totalCost = 0;
            for (int i = 0; i < activeEffects.Count; i++) {
                var e = activeEffects[i];
                if (e.TargetIndex == npcIndex && e.Active)
                    totalCost += e.Hack.RamCost;
            }
            if (totalCost <= 0) return;

            float refund = totalCost * KillRefundRatio;
            //至少返还1点
            if (refund < 1f) refund = 1f;
            float before = RamSystem.CurrentRam;
            RamSystem.Restore(refund);
            float actual = RamSystem.CurrentRam - before;

            //视觉反馈：在NPC位置弹出青色浮动文字
            if (actual > 0.01f && !VaultUtils.isServer) {
                string text = HackTime.RamRefund.Format(actual.ToString("F0"));
                CombatText.NewText(target.Hitbox, HackTheme.Accent, text, true);
                SoundEngine.PlaySound(CWRSound.Hacker with { Volume = 0.35f, Pitch = 0.4f },
                    target.Center);
            }
        }

        public static void Reset() {
            activeEffects.Clear();
            removeBuffer.Clear();
            killRefundedThisFrame.Clear();
            activeTileEffects.Clear();
            tileRemoveBuffer.Clear();
        }

        #region 物块效果管理

        /// <summary>
        /// 对指定物块施加一个骇入协议效果
        /// </summary>
        public static ActiveTileHackEffect ApplyToTile(QuickHackDef hack, int tileX, int tileY, int casterIndex) {
            if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
                return null;
            if (!Main.tile[tileX, tileY].HasTile) return null;
            if (!hack.CanApplyToTile(tileX, tileY)) return null;

            var effect = new ActiveTileHackEffect {
                Hack = hack,
                CasterIndex = casterIndex,
                TileX = tileX,
                TileY = tileY,
                Elapsed = 0,
                Active = true,
                Applied = false,
            };

            activeTileEffects.Add(effect);
            return effect;
        }

        /// <summary>
        /// 每帧更新所有物块效果
        /// </summary>
        public static void UpdateTileEffects() {
            tileRemoveBuffer.Clear();

            for (int i = 0; i < activeTileEffects.Count; i++) {
                var eff = activeTileEffects[i];
                if (!eff.Active) {
                    tileRemoveBuffer.Add(eff);
                    continue;
                }

                //物块被挖掉则移除效果
                if (eff.TileX < 0 || eff.TileX >= Main.maxTilesX
                    || eff.TileY < 0 || eff.TileY >= Main.maxTilesY
                    || !Main.tile[eff.TileX, eff.TileY].HasTile) {
                    eff.Active = false;
                    tileRemoveBuffer.Add(eff);
                    continue;
                }

                //首帧触发OnApplyToTile
                if (!eff.Applied) {
                    eff.Applied = true;
                    Player caster = eff.CasterIndex >= 0 && eff.CasterIndex < Main.maxPlayers
                        ? Main.player[eff.CasterIndex] : Main.LocalPlayer;
                    eff.Hack.OnApplyToTile(eff.TileX, eff.TileY, caster);
                }

                int duration = eff.Hack.GetDuration();
                //到期
                if (duration > 0 && eff.Elapsed >= duration) {
                    eff.Hack.OnRemoveTile(eff.TileX, eff.TileY);
                    eff.Active = false;
                    tileRemoveBuffer.Add(eff);
                    continue;
                }

                //即时效果在Apply后直接结束
                if (duration == 0 && eff.Applied) {
                    eff.Active = false;
                    tileRemoveBuffer.Add(eff);
                    continue;
                }

                //持续逻辑
                bool alive = eff.Hack.OnTickTile(eff.TileX, eff.TileY, eff.Elapsed);
                if (!alive) {
                    eff.Hack.OnRemoveTile(eff.TileX, eff.TileY);
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

        /// <summary>
        /// 查询指定物块位置是否有某类型的活跃效果
        /// </summary>
        public static bool HasTileEffect<T>(int tileX, int tileY) where T : QuickHackDef {
            for (int i = 0; i < activeTileEffects.Count; i++) {
                if (activeTileEffects[i].Active
                    && activeTileEffects[i].TileX == tileX
                    && activeTileEffects[i].TileY == tileY
                    && activeTileEffects[i].Hack is T)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取指定物块坐标上所有活跃效果
        /// </summary>
        public static void GetTileEffects(int tileX, int tileY, List<ActiveTileHackEffect> result) {
            result.Clear();
            for (int i = 0; i < activeTileEffects.Count; i++) {
                if (activeTileEffects[i].Active
                    && activeTileEffects[i].TileX == tileX
                    && activeTileEffects[i].TileY == tileY)
                    result.Add(activeTileEffects[i]);
            }
        }

        /// <summary>
        /// 所有活跃的物块效果（只读）
        /// </summary>
        public static IReadOnlyList<ActiveTileHackEffect> AllActiveTileEffects => activeTileEffects;

        #endregion
    }
}
