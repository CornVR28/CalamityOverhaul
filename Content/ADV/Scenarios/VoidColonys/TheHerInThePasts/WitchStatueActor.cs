using InnoVault.Actors;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TheHerInThePasts
{
    /// <summary>
    /// 过去的她场景中女巫的留影雕像
    /// 召唤出现后即展开硫磺火鬼域，鬼域稳定后由对话场景接管推进对白
    /// 场景结束则像素剥落消散
    /// </summary>
    internal class WitchStatueActor : Actor
    {
        /// <summary>
        /// 演出阶段
        /// </summary>
        public enum PhaseKind
        {
            //鬼域正在从中心向外扩张
            Expanding,
            //鬼域完全展开，等待或处于对话阶段
            Active,
            //像素剥落消散
            Dissolve,
        }

        //每场地图仅会有一尊雕像，保留静态引用便于场景查询
        public static WitchStatueActor Current { get; private set; }

        //可见度淡入步进
        private const float FadeInStep = 0.04f;

        //鬼域扩张时长
        private const int ExpandDuration = 90;
        //像素剥落时长
        private const int DissolveDuration = 180;

        //鬼域展开最大半径，像素
        private const float DomainMaxRadius = 780f;

        /// <summary>可见度0到1</summary>
        private float visibility;

        /// <summary>当前演出阶段</summary>
        public PhaseKind Phase { get; private set; } = PhaseKind.Expanding;

        /// <summary>鬼域是否已完全展开，可供场景触发对话</summary>
        public bool IsDomainReady => Phase == PhaseKind.Active;

        /// <summary>鬼域扩张进度0到1</summary>
        private float domainT;

        /// <summary>像素剥落进度0到1</summary>
        private float dissolveT;

        /// <summary>演出阶段内部计时</summary>
        private int phaseTimer;

        /// <summary>鬼域总时间累计，用于shader的uTime</summary>
        private float domainTimeAccum;

        /// <summary>鬼域展开后启动对话前的等待计时</summary>
        private int triggerDelay;

        /// <summary>是否已经启动过对话，避免重复触发</summary>
        private bool scenarioStarted;

        public new Vector2 Center {
            get => Position + new Vector2(Width * 0.5f, Height * 0.5f);
            set => Position = value - new Vector2(Width * 0.5f, Height * 0.5f);
        }

        public override void OnSpawn(params object[] args) {
            Width = 120;
            Height = 260;
            DrawLayer = ActorDrawLayer.AfterTiles;
            DrawExtendMode = (int)(DomainMaxRadius * 1.4f);
            visibility = 0f;
            Current = this;
        }

        public override void AI() {
            //维持单例引用，切地图或重建actor后新的实例接管
            if (Current != this) {
                Current = this;
            }

            //可见度直接淡入，出场即可见
            if (visibility < 1f) visibility = MathF.Min(1f, visibility + FadeInStep);

            Velocity = Vector2.Zero;
            phaseTimer++;
            domainTimeAccum += 0.016f;

            switch (Phase) {
                case PhaseKind.Expanding:
                    UpdateExpanding();
                    break;
                case PhaseKind.Active:
                    //保持鬼域稳定，短暂延迟后自行启动对话场景
                    domainT = 1f;
                    TryStartScenario();
                    break;
                case PhaseKind.Dissolve:
                    UpdateDissolve();
                    break;
            }
        }

        /// <summary>
        /// 由场景调用，进入像素剥落消散阶段
        /// </summary>
        public void BeginDissolve() {
            if (Phase == PhaseKind.Dissolve) return;
            Phase = PhaseKind.Dissolve;
            phaseTimer = 0;
            dissolveT = 0f;
        }

        /// <summary>
        /// 鬼域稳定后由Actor自行驱动对话启动
        /// </summary>
        private void TryStartScenario() {
            if (scenarioStarted) return;
            //已完成过的存档不再触发
            //if (Main.LocalPlayer.TryGetADVSave(out var save)
            //    && save.Get<VoidColonyADVData>().TheHerInThePast) {
            //    scenarioStarted = true;
            //    return;
            //}

            if (++triggerDelay < 30) return;

            ScenarioManager.Reset<TheHerInThePast>();
            if (ScenarioManager.Start<TheHerInThePast>()) {
                scenarioStarted = true;
            }
        }

        private void UpdateExpanding() {
            domainT = MathHelper.Clamp(phaseTimer / (float)ExpandDuration, 0f, 1f);
            if (phaseTimer >= ExpandDuration) {
                Phase = PhaseKind.Active;
                phaseTimer = 0;
                domainT = 1f;
            }
        }

        private void UpdateDissolve() {
            dissolveT = MathHelper.Clamp(phaseTimer / (float)DissolveDuration, 0f, 1f);
            //消散过程中鬼域逐步收缩
            domainT = 1f - dissolveT;

            if (phaseTimer >= DissolveDuration) {
                ActorLoader.KillActor(WhoAmI);
                if (Current == this) Current = null;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            if (visibility <= 0.01f) return false;

            //先绘制鬼域光晕，叠加在雕像下面
            if (domainT > 0.01f) {
                WitchGhostDomainDraw.Draw(spriteBatch, Center, domainT, domainTimeAccum, visibility, dissolveT);
            }

            //绘制雕像本体
            //DrawStatue(spriteBatch);

            return false;
        }
    }
}
