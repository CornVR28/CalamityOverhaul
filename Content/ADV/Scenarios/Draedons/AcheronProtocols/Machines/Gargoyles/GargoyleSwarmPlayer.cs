using InnoVault.Actors;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.Gargoyles
{
    /// <summary>
    /// 石像鬼虫群过场演出控制器——管理整个虫群飞越天空的运镜和生命周期。
    /// <para>
    /// 演出流程：<br/>
    /// 1. 摄像机缓缓上摇，拉向高空<br/>
    /// 2. 大量石像鬼从画面一侧涌入，以鸟群算法驱动飞越天空<br/>
    /// 3. 虫群穿过后，摄像机平滑回落至玩家位置<br/>
    /// </para>
    /// 通过 <see cref="StartCutscene"/> 从外部触发（对话、事件节点等）
    /// </summary>
    internal class GargoyleSwarmPlayer : ModPlayer
    {
        #region 时间轴常量

        /// <summary>摄像机开始上摇的帧</summary>
        private const int PanUpStart = 0;
        /// <summary>摄像机上摇完成的帧</summary>
        private const int PanUpEnd = 80;
        /// <summary>石像鬼生成帧（在上摇过程中提前生成，飞入镜头时恰好可见）</summary>
        private const int SpawnFrame = 30;
        /// <summary>虫群主体飞越开始帧</summary>
        private const int SwarmActiveStart = 80;
        /// <summary>虫群主体飞越结束帧</summary>
        private const int SwarmActiveEnd = 380;
        /// <summary>摄像机开始下摇的帧</summary>
        private const int PanDownStart = 360;
        /// <summary>摄像机下摇完成的帧</summary>
        private const int PanDownEnd = 460;
        /// <summary>整个过场结束帧</summary>
        private const int CutsceneEndFrame = 500;

        #endregion

        #region 演出参数

        /// <summary>摄像机上摇距离（像素）</summary>
        private const float PanDistance = 500f;
        /// <summary>虫群生成数量</summary>
        private const int SwarmCount = 300;
        /// <summary>虫群整体飞越方向基础速度（负=向左）</summary>
        private const float SwarmBaseSpeed = -4.5f;
        /// <summary>虫群飞行弧线最大高度偏移</summary>
        private const float ArcHeight = 80f;
        /// <summary>缩放：飞越期间略微拉远以展示更多天空</summary>
        private const float CrossingZoom = 0.85f;
        /// <summary>摄像机水平跟踪比例（0=不跟, 1=完全跟随集群重心）</summary>
        private const float HorizontalTrackRatio = 0.1f;

        #endregion

        #region 运行时状态

        private static bool cutsceneActive;
        private static int timer;
        private static Vector2 cutsceneOrigin;
        private static Vector2 cameraOffset;
        private static float currentZoom;

        /// <summary>当前鸟群全局航路点——由 <see cref="GargoyleActor"/> 的鸟群算法读取</summary>
        internal static Vector2 CurrentWaypoint { get; private set; }

        /// <summary>过场演出是否正在进行</summary>
        internal static bool IsActive => cutsceneActive;

        #endregion

        #region 公开 API

        /// <summary>
        /// 启动石像鬼虫群飞越过场演出
        /// </summary>
        internal static void StartCutscene() {
            if (cutsceneActive) return;

            cutsceneActive = true;
            timer = 0;
            cutsceneOrigin = Main.LocalPlayer.Center;
            cameraOffset = Vector2.Zero;
            currentZoom = 1f;
            CurrentWaypoint = cutsceneOrigin - new Vector2(0, PanDistance);
        }

        /// <summary>
        /// 立即停止演出并清理所有石像鬼
        /// </summary>
        internal static void StopCutscene() {
            cutsceneActive = false;
            timer = 0;
            cameraOffset = Vector2.Zero;
            currentZoom = 1f;
            KillAllGargoyles();
        }

        #endregion

        #region 每帧逻辑

        public override void PostUpdate() {
            //安全退出：离开子世界时自动终止
            if (!MachineWorld.Active && cutsceneActive) {
                StopCutscene();
                return;
            }

            if (!cutsceneActive) return;

            timer++;

            UpdateCamera();
            UpdateWaypoint();

            //生成虫群
            if (timer == SpawnFrame) {
                SpawnSwarm();
            }

            //清理飞出画面远处的个体
            if (timer > SwarmActiveEnd) {
                PruneOffscreenGargoyles();
            }

            //演出结束
            if (timer >= CutsceneEndFrame) {
                StopCutscene();
                return;
            }

            //锁定玩家操作
            LockPlayerControls();
        }

        public override void ModifyScreenPosition() {
            if (!cutsceneActive) return;

            //叠加运镜偏移
            Main.screenPosition += cameraOffset;

            //缩放
            Main.GameZoomTarget = currentZoom;
        }

        #endregion

        #region 摄像机控制

        private void UpdateCamera() {
            //── 垂直：EaseInOutCubic 上摇/下摇 ──
            if (timer >= PanUpStart && timer < PanUpEnd) {
                float t = (float)(timer - PanUpStart) / (PanUpEnd - PanUpStart);
                cameraOffset.Y = -PanDistance * EaseInOutCubic(t);
            }

            if (timer >= PanDownStart && timer < PanDownEnd) {
                float t = (float)(timer - PanDownStart) / (PanDownEnd - PanDownStart);
                cameraOffset.Y = -PanDistance * (1f - EaseInOutCubic(t));
            }

            if (timer >= PanDownEnd) {
                cameraOffset.Y = MathHelper.Lerp(cameraOffset.Y, 0f, 0.05f);
            }

            //── 水平：虫群飞越期间轻微跟踪集群重心 ──
            if (timer >= SwarmActiveStart && timer < SwarmActiveEnd) {
                List<GargoyleActor> flock = ActorLoader.GetActiveActors<GargoyleActor>();
                if (flock.Count > 0) {
                    float avgX = 0f;
                    foreach (GargoyleActor g in flock) {
                        avgX += g.Center.X;
                    }
                    avgX /= flock.Count;

                    float trackOffset = (avgX - cutsceneOrigin.X) * HorizontalTrackRatio;
                    cameraOffset.X = MathHelper.Lerp(cameraOffset.X, trackOffset, 0.02f);
                }
            }

            //下摇阶段水平也归零
            if (timer >= PanDownStart) {
                cameraOffset.X = MathHelper.Lerp(cameraOffset.X, 0f, 0.03f);
            }

            //── 缩放 ──
            float targetZoom = (timer >= SwarmActiveStart && timer < SwarmActiveEnd) ? CrossingZoom : 1f;
            currentZoom = MathHelper.Lerp(currentZoom, targetZoom, 0.025f);
        }

        #endregion

        #region 航路点

        private void UpdateWaypoint() {
            if (timer < SpawnFrame) return;

            float screenWidth = Main.screenWidth;
            float skyY = cutsceneOrigin.Y - PanDistance;

            //航路点从画面右侧向左侧平移
            float progress = MathHelper.Clamp(
                (float)(timer - SpawnFrame) / (SwarmActiveEnd - SpawnFrame), 0f, 1f);

            float waypointX = cutsceneOrigin.X + screenWidth * 0.8f * (1f - 2f * progress);

            //弧线——中段略高，两端略低，制造波浪感
            float arc = MathF.Sin(progress * MathF.PI) * ArcHeight;

            //大尺度缓慢起伏
            float undulation = MathF.Sin(timer * 0.015f) * 35f;

            CurrentWaypoint = new Vector2(waypointX, skyY - arc + undulation);
        }

        #endregion

        #region 虫群生成

        private void SpawnSwarm() {
            float screenWidth = Main.screenWidth;
            float skyY = cutsceneOrigin.Y - PanDistance;
            float spawnCenterX = cutsceneOrigin.X + screenWidth * 0.5f + 300f;

            for (int i = 0; i < SwarmCount; i++) {
                //在生成区域内散布——椭圆分布，前端密后端疏
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radiusX = Main.rand.NextFloat(50f, 350f);
                float radiusY = Main.rand.NextFloat(30f, 180f);

                Vector2 spawnPos = new(
                    spawnCenterX + MathF.Cos(angle) * radiusX,
                    skyY + MathF.Sin(angle) * radiusY);

                //初始速度：向左飞行，带随机偏差
                Vector2 spawnVel = new(
                    SwarmBaseSpeed + Main.rand.NextFloat(-1f, 0.5f),
                    Main.rand.NextFloat(-0.8f, 0.8f));

                ActorLoader.NewActor<GargoyleActor>(spawnPos, spawnVel);
            }
        }

        #endregion

        #region 清理

        private static void KillAllGargoyles() {
            List<GargoyleActor> gargoyles = ActorLoader.GetActiveActors<GargoyleActor>();
            foreach (GargoyleActor g in gargoyles) {
                ActorLoader.KillActor(g.WhoAmI, false);
            }
        }

        private static void PruneOffscreenGargoyles() {
            float margin = Main.screenWidth + 800f;
            float cameraLeft = Main.screenPosition.X - margin;
            float cameraRight = Main.screenPosition.X + Main.screenWidth + margin;

            List<GargoyleActor> gargoyles = ActorLoader.GetActiveActors<GargoyleActor>();
            foreach (GargoyleActor g in gargoyles) {
                if (g.Position.X < cameraLeft || g.Position.X > cameraRight) {
                    ActorLoader.KillActor(g.WhoAmI, false);
                }
            }
        }

        #endregion

        #region 辅助

        private void LockPlayerControls() {
            Player.controlLeft = false;
            Player.controlRight = false;
            Player.controlUp = false;
            Player.controlDown = false;
            Player.controlJump = false;
            Player.controlUseItem = false;
            Player.controlUseTile = false;
            Player.velocity = Vector2.Zero;
        }

        private static float EaseInOutCubic(float t) {
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;
        }

        #endregion
    }
}
