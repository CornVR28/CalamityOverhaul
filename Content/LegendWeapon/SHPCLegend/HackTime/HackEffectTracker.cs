using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

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

    /// <summary>
    /// 骇入协议效果的NPC全局钩子
    /// <br/>处理效果对NPC行为的干预（AI修改、绘制覆盖等）
    /// </summary>
    internal class HackEffectNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        //缓存列表避免每帧GC
        private readonly List<ActiveHackEffect> effectsCache = [];

        public override bool PreAI(NPC npc) {
            HackEffectTracker.GetEffects(npc.whoAmI, effectsCache);
            if (effectsCache.Count == 0) return true;

            bool allowAI = true;
            for (int i = 0; i < effectsCache.Count; i++) {
                var eff = effectsCache[i];
                switch (eff.Hack) {
                    //系统重置：完全阻止AI
                    case Protocols.SystemReset:
                        npc.velocity = Vector2.Zero;
                        allowAI = false;
                        break;
                    //赛博精神病：重定向AI攻击最近NPC
                    case Protocols.Cyberpsychosis:
                        RedirectAI(npc, eff);
                        allowAI = false;
                        break;
                    //视觉过载：NPC失去跟踪能力，随机游荡
                    case Protocols.OpticOverload:
                        BlindWander(npc);
                        allowAI = false;
                        break;
                    //记忆清除：清除仇恨，停止追击
                    case Protocols.MemoryWipe:
                        WipeAggro(npc);
                        allowAI = false;
                        break;
                }
            }

            return allowAI;
        }

        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers) {
            //赛博精神病状态下NPC不对玩家造成伤害
            if (HackEffectTracker.HasEffect<Protocols.Cyberpsychosis>(npc.whoAmI)) {
                modifiers.FinalDamage *= 0f;
            }
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone) {
            //记忆清除被打断
            if (HackEffectTracker.HasEffect<Protocols.MemoryWipe>(npc.whoAmI)) {
                //被打击可能恢复记忆（30%概率提前结束）
                if (Main.rand.NextFloat() < 0.3f) {
                    var eff = HackEffectTracker.GetEffect<Protocols.MemoryWipe>(npc.whoAmI);
                    if (eff != null) {
                        eff.Hack.OnRemove(npc);
                        eff.Active = false;
                    }
                }
            }
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone) {
            if (HackEffectTracker.HasEffect<Protocols.MemoryWipe>(npc.whoAmI)) {
                if (Main.rand.NextFloat() < 0.3f) {
                    var eff = HackEffectTracker.GetEffect<Protocols.MemoryWipe>(npc.whoAmI);
                    if (eff != null) {
                        eff.Hack.OnRemove(npc);
                        eff.Active = false;
                    }
                }
            }
        }

        //赛博精神病：重定向NPC攻击最近的其他NPC
        private static void RedirectAI(NPC npc, ActiveHackEffect eff) {
            //寻找最近的其他活跃NPC
            float closestDist = float.MaxValue;
            NPC closestNPC = null;
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC other = Main.npc[i];
                if (!other.active || other.whoAmI == npc.whoAmI || other.friendly || other.dontTakeDamage)
                    continue;
                //不攻击已经被赛博精神病影响的NPC
                if (HackEffectTracker.HasEffect<Protocols.Cyberpsychosis>(other.whoAmI))
                    continue;
                float dist = Vector2.DistanceSquared(npc.Center, other.Center);
                if (dist < closestDist) {
                    closestDist = dist;
                    closestNPC = other;
                }
            }

            if (closestNPC != null) {
                //朝最近NPC移动
                Vector2 dir = (closestNPC.Center - npc.Center);
                float dist = dir.Length();
                if (dist > 0) dir /= dist;
                float speed = Math.Max(npc.velocity.Length(), 3f);
                npc.velocity = Vector2.Lerp(npc.velocity, dir * speed, 0.08f);
                npc.direction = closestNPC.Center.X > npc.Center.X ? 1 : -1;
                npc.spriteDirection = npc.direction;

                //接触伤害
                if (dist < (npc.width + closestNPC.width) * 0.6f) {
                    int dmg = Math.Max(npc.damage / 2, 10);
                    NPC.HitInfo hitInfo = new() {
                        Damage = dmg,
                        Knockback = 2f,
                        HitDirection = npc.direction,
                    };
                    closestNPC.StrikeNPC(hitInfo);
                }
            }
            else {
                //没有目标时减速游荡
                npc.velocity *= 0.96f;
            }
        }

        //视觉过载：随机游荡
        private static void BlindWander(NPC npc) {
            //缓慢随机改变方向
            if (Main.GameUpdateCount % 30 == 0) {
                npc.velocity = Main.rand.NextVector2CircularEdge(1.5f, 1.5f);
                npc.direction = npc.velocity.X > 0 ? 1 : -1;
                npc.spriteDirection = npc.direction;
            }
            npc.velocity *= 0.98f;
        }

        //记忆清除：清除仇恨停止追击
        private static void WipeAggro(NPC npc) {
            npc.target = -1;
            npc.velocity *= 0.95f;
            //缓慢随机漂移
            if (Main.GameUpdateCount % 60 == 0) {
                npc.velocity += Main.rand.NextVector2Circular(0.5f, 0.5f);
            }
        }
    }

    /// <summary>
    /// 骇入协议效果的NPC绘制钩子
    /// <br/>在受到协议影响的NPC身上叠加对应的着色器特效
    /// <br/>与HackTimeNPCDraw分开处理——HackTimeNPCDraw处理选中/悬停高亮
    /// <br/>此类处理已施加的效果视觉
    /// </summary>
    internal class HackEffectNPCDraw : GlobalNPC
    {
        //当前帧激活的着色器标记
        private static bool _shaderActive;
        //按优先级选择的效果类型（用于选择shader）
        private static QuickHackDef _activeShaderHack;

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            var effects = HackEffectTracker.AllActiveEffects;
            QuickHackDef bestHack = null;
            float bestProgress = 0f;

            //找到此NPC上优先级最高的可视效果
            for (int i = 0; i < effects.Count; i++) {
                var eff = effects[i];
                if (!eff.Active || eff.TargetIndex != npc.whoAmI) continue;
                //即时效果（ShortCircuit）不持续绘制
                if (eff.Hack.GetDuration() == 0) continue;

                float progress = 0f;
                int dur = (int)(eff.Hack.GetDuration() * eff.EffectMult);
                if (dur > 0) progress = (float)eff.Elapsed / dur;

                //取最新施加的效果做着色器渲染
                if (bestHack == null || eff.Elapsed < (bestProgress * dur)) {
                    bestHack = eff.Hack;
                    bestProgress = progress;
                }
            }

            if (bestHack == null) return true;

            Effect shader = bestHack switch {
                Protocols.SynapseBurn => HackEffectAssets.HackSynapseBurn,
                Protocols.Cyberpsychosis => HackEffectAssets.HackCyberpsychosis,
                Protocols.SystemReset => HackEffectAssets.HackSystemReset,
                Protocols.OpticOverload => HackEffectAssets.HackOpticOverload,
                Protocols.MemoryWipe => HackEffectAssets.HackMemoryWipe,
                Protocols.Contagion => HackEffectAssets.HackContagion,
                _ => null
            };
            if (shader == null) return true;

            Texture2D tex = Terraria.GameContent.TextureAssets.Npc[npc.type].Value;
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["progress"]?.SetValue(bestProgress);
            shader.Parameters["intensity"]?.SetValue(1f);
            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();

            _shaderActive = true;
            _activeShaderHack = bestHack;
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (!_shaderActive) return;
            _shaderActive = false;
            _activeShaderHack = null;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);
        }
    }

    /// <summary>
    /// 骇入效果着色器资源
    /// <br/>通过VaultLoaden自动加载Assets/Effects/下对应的.fxc文件
    /// </summary>
    internal class HackEffectAssets
    {
        [VaultLoaden(CWRConstant.Effects)]
        public static Effect HackSynapseBurn { get; private set; }

        [VaultLoaden(CWRConstant.Effects)]
        public static Effect HackShortCircuit { get; private set; }

        [VaultLoaden(CWRConstant.Effects)]
        public static Effect HackCyberpsychosis { get; private set; }

        [VaultLoaden(CWRConstant.Effects)]
        public static Effect HackSystemReset { get; private set; }

        [VaultLoaden(CWRConstant.Effects)]
        public static Effect HackOpticOverload { get; private set; }

        [VaultLoaden(CWRConstant.Effects)]
        public static Effect HackMemoryWipe { get; private set; }

        [VaultLoaden(CWRConstant.Effects)]
        public static Effect HackContagion { get; private set; }
    }
}
