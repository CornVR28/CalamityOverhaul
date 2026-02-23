using CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors.States;
using CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines;
using CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.LandingScens;
using InnoVault.Actors;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors
{
    /// <summary>
    /// 管理阿波利娅演出的生命周期：
    /// 1. 检测着陆完成 → 延迟生成 ApolliaActor
    /// 2. 在 ModifyScreenPosition 中驱动 <see cref="CutsceneCamera"/>
    /// </summary>
    internal class ApolliaPlayer : ModPlayer
    {
        /// <summary>阿波利娅出场延迟计时器</summary>
        private int spawnDelay;

        /// <summary>是否已经生成过阿波利娅</summary>
        private bool spawned;

        /// <summary>着陆完成时记录的玩家位置</summary>
        private Vector2 landingPodCenter;

        /// <summary>是否已检测到玩家弹出空降仓完成</summary>
        private bool ejectDetected;

        /// <summary>当前场景中的阿波利娅Actor引用（弱引用方式通过索引）</summary>
        private int apolliaActorIndex = -1;

        /// <summary>是否已触发过阿波利娅到达后的对话场景</summary>
        private bool dialogueTriggered;

        /// <summary>英雄面板是否已激活</summary>
        internal bool HeroPanelActivated;

        public override void PostUpdate() {
            if (!Player.Alives()) {
                return;
            }

            if (!MachineWorld.Active) {
                ResetState();
                return;
            }

            if (!MachineWorld.landingCompleted) {
                return;
            }

            //阶段1：检测玩家完全弹出空降仓
            if (!ejectDetected) {
                //必须获取到PlayerOverride且着陆和弹出都已完成才算弹出成功
                //TryGetOverride返回false或仍在着陆/弹出中时都不应继续
                if (!Player.TryGetOverride<MachineWorldLandingPlayer>(out var lp)
                    || lp.LandingActive || lp.EjectAnimating) {
                    return;
                }
                ejectDetected = true;
                landingPodCenter = Player.Center;
                return;
            }

            //阶段2：延迟生成阿波利娅
            if (!spawned) {
                spawnDelay++;
                if (spawnDelay >= 120) {
                    SpawnApollia();
                    spawned = true;
                }
                return;
            }

            //阶段3：阿波利娅到达玩家面前后触发对话场景
            if (!dialogueTriggered) {
                ApolliaActor actor = GetApolliaActor();
                if (actor?.CurrentState is ApolliaArrivedState) {
                    dialogueTriggered = true;
                    ScenarioManager.Reset<ApolliaDialogueScenario>();
                    ScenarioManager.Start<ApolliaDialogueScenario>();
                }
            }

            //阶段4：英雄面板同步
            if (HeroPanelActivated && ApolliaHeroPanelUI.Instance != null) {
                ApolliaHeroPanelUI.Instance.Unlocked = true;
            }
        }

        public override void ModifyScreenPosition() {
            ApolliaActor actor = GetApolliaActor();
            if (actor == null) return;

            //运镜参数由Camera自身根据Actor状态推导
            actor.Camera.UpdateFocus(actor, Player);
            actor.Camera.Apply();
        }

        private void SpawnApollia() {
            int index = ActorLoader.NewActor<ApolliaActor>(Vector2.Zero, Vector2.Zero);
            if (index >= 0 && index < ActorLoader.MaxActorCount
                && ActorLoader.Actors[index] is ApolliaActor apollia) {
                apollia.StartLandingCutscene(landingPodCenter);
                apolliaActorIndex = index;
            }
        }

        /// <summary>
        /// 获取当前场景中的阿波利娅Actor实例，无效时返回null
        /// </summary>
        internal ApolliaActor GetApolliaActor() {
            if (apolliaActorIndex >= 0 && apolliaActorIndex < ActorLoader.MaxActorCount
                && ActorLoader.Actors[apolliaActorIndex] is ApolliaActor actor
                && actor.Active) {
                return actor;
            }
            apolliaActorIndex = -1;
            return null;
        }

        /// <summary>
        /// 激活英雄面板——在对话场景完成时调用
        /// </summary>
        internal void ActivateHeroPanel() {
            HeroPanelActivated = true;

            //平滑关闭运镜
            ApolliaActor actor = GetApolliaActor();
            actor?.Camera.Stop();

            if (ApolliaHeroPanelUI.Instance != null) {
                ApolliaHeroPanelUI.Instance.Unlocked = true;
                ApolliaHeroPanelUI.Instance.StartFlyIn();
            }
        }

        private void ResetState() {
            spawnDelay = 0;
            spawned = false;
            ejectDetected = false;
            dialogueTriggered = false;
            HeroPanelActivated = false;
            landingPodCenter = Vector2.Zero;
            apolliaActorIndex = -1;

            if (ApolliaHeroPanelUI.Instance != null) {
                ApolliaHeroPanelUI.Instance.Unlocked = false;
            }
        }
    }
}
