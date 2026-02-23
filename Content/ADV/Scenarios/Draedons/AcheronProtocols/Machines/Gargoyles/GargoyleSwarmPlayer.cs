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
        /// <summary>虫群开始分批生成的帧</summary>
        private const int SpawnStart = 30;
        /// <summary>虫群停止生成的帧——长窗口让虫群绵延不断</summary>
        private const int SpawnEnd = 480;
        /// <summary>虫群主体飞越开始帧</summary>
        private const int SwarmActiveStart = 80;
        /// <summary>虫群主体飞越结束帧</summary>
        private const int SwarmActiveEnd = 950;
        /// <summary>摄像机开始下摇的帧</summary>
        private const int PanDownStart = 720;
        /// <summary>摄像机下摇完成的帧</summary>
        private const int PanDownEnd = 820;
        /// <summary>安全上限——防止无限等待</summary>
        private const int CutsceneHardLimit = 1200;

        #endregion

        #region 演出参数

        /// <summary>摄像机上摇距离（像素）</summary>
        private const float PanDistance = 500f;
        /// <summary>虫群总生成数量</summary>
        private const int SwarmCount = 3000;
        /// <summary>虫群整体飞越方向基础速度（负=向左）</summary>
        private const float SwarmBaseSpeed = -13f;
        /// <summary>流道垂直分布总高度</summary>
        private const float StreamSpread = 400f;
        /// <summary>缩放：飞越期间略微拉远以展示更多天空</summary>
        private const float CrossingZoom = 0.85f;
        /// <summary>摄像机水平跟踪比例（0=不跟, 1=完全跟随集群重心）</summary>
        private const float HorizontalTrackRatio = 0.05f;

        #endregion

        #region 运行时状态

        private static bool cutsceneActive;
        private static int timer;
        private static Vector2 cutsceneOrigin;
        private static Vector2 cameraOffset;
        private static float currentZoom;
        private static int totalSpawned;

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
            totalSpawned = 0;
            cutsceneOrigin = Main.LocalPlayer.Center;
            cameraOffset = Vector2.Zero;
            currentZoom = 1f;

            float skyY = cutsceneOrigin.Y - PanDistance;
            GargoyleBoids.InitializeStreams(skyY, StreamSpread);
        }

        /// <summary>
        /// 立即停止演出并清理所有石像鬼
        /// </summary>
        internal static void StopCutscene() {
            cutsceneActive = false;
            timer = 0;
            totalSpawned = 0;
            cameraOffset = Vector2.Zero;
            currentZoom = 1f;
            GargoyleBoids.Reset();
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

            //分批生成虫群——持续从右侧涌入，形成流动的河流效果
            if (timer >= SpawnStart && timer <= SpawnEnd) {
                int spawnFrames = SpawnEnd - SpawnStart;
                int elapsed = timer - SpawnStart;
                int targetTotal = (int)((float)(elapsed + 1) / spawnFrames * SwarmCount);
                int batch = Math.Min(targetTotal - totalSpawned, SwarmCount - totalSpawned);
                if (batch > 0) SpawnBatch(batch);
            }

            //持续清理飞出屏幕左侧的个体
            if (timer > SwarmActiveStart) {
                PruneOffscreenGargoyles();
            }

            //演出结束：下摇完成后，等待所有石像鬼飞出屏幕再收场
            if (timer >= PanDownEnd) {
                List<GargoyleActor> remaining = ActorLoader.GetActiveActors<GargoyleActor>();
                if (remaining.Count < 10 || timer >= CutsceneHardLimit) {
                    StopCutscene();
                    return;
                }
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

        #region 虫群分批生成

        private void SpawnBatch(int count) {
            float screenW = Main.screenWidth;
            float skyY = cutsceneOrigin.Y - PanDistance;
            //生成点在屏幕右边缘外侧
            float spawnX = cutsceneOrigin.X + screenW * 0.5f + 300f;

            for (int i = 0; i < count; i++) {
                float xScatter = Main.rand.NextFloat(-120f, 120f);
                float yScatter = Main.rand.NextFloat(-80f, 80f);

                Vector2 spawnPos = new(spawnX + xScatter, skyY + yScatter);

                //初始速度：高速向左飞行，带随机偏差
                Vector2 spawnVel = new(
                    SwarmBaseSpeed + Main.rand.NextFloat(-2f, 1f),
                    Main.rand.NextFloat(-0.6f, 0.6f));

                ActorLoader.NewActor<GargoyleActor>(spawnPos, spawnVel);
                totalSpawned++;
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
            //只清除已飞过屏幕左侧远处的个体——确保玩家绝对看不到消失
            float leftKill = Main.screenPosition.X - 1000f;

            List<GargoyleActor> gargoyles = ActorLoader.GetActiveActors<GargoyleActor>();
            foreach (GargoyleActor g in gargoyles) {
                if (g.Position.X < leftKill) {
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
