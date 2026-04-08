using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime
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

            for (int i = 0; i < activeEffects.Count; i++) {
                var eff = activeEffects[i];
                if (!eff.Active) {
                    removeBuffer.Add(eff);
                    continue;
                }

                NPC target = Main.npc[eff.TargetIndex];
                //目标死亡或失效则移除
                if (target == null || !target.active || target.life <= 0) {
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

        public static void Reset() {
            activeEffects.Clear();
            removeBuffer.Clear();
        }
    }
}
