using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.VoidColonys.VoidPortals
{
    /// <summary>
    /// 虚空传送完整演出编排器
    /// </summary>
    internal class VoidTransportPlayer : ModPlayer
    {
        //演出阶段
        internal enum Stage
        {
            //空闲
            Idle,
            //等待传送门完全展开
            WaitExpand,
            //传送门展示阶段
            Hold,
            //吸入演出
            Suction,
            //黑闪峰值
            BlackFlash,
            //演出结束
            Finish
        }

        #region 配置

        //展示等待帧数（传送门完全展开后的hold时间）
        const int HoldDuration = 60;
        //吸入阶段总帧数
        const int SuctionDuration = 110;
        //黑闪总持续帧数（上升+保持+传送等待）
        const int BlackFlashDuration = 50;
        //黑闪上升到峰值的帧数
        const int BlackFlashRise = 10;
        //黑闪完全黑屏保持帧数（峰值后持续纯黑）
        const int BlackFlashHold = 30;
        //镜头缩放lerp速度
        const float ZoomLerpSpeed = 0.025f;
        //镜头位置lerp速度
        const float PosLerpSpeed = 0.035f;
        //吸入阶段的目标缩放倍率
        const float SuctionTargetZoom = 2.2f;
        //玩家被牵引的最大速度
        const float MaxPullSpeed = 6f;

        #endregion

        #region 运行时

        internal Stage CurrentStage { get; private set; }
        int stageTimer;

        //吸入演出进度0~1（供着色器读取）
        internal float SuctionProgress { get; private set; }
        //黑闪覆盖强度0~1
        internal float BlackFlashAlpha { get; private set; }

        //传送门弹幕索引
        int portalIndex = -1;
        //运镜状态
        float currentZoom = 1f;
        float savedZoom;
        Vector2 smoothedScreenPos;
        bool cameraInit;
        //传送完成回调
        Action onTransportDone;

        #endregion

        #region 公开API

        /// <summary>
        /// 启动虚空传送演出
        /// </summary>
        /// <param name="portalWorldPos">传送门世界坐标</param>
        /// <param name="onDone">传送完成后的回调（切换场景等）</param>
        public void StartTransport(Vector2 portalWorldPos, Action onDone = null) {
            if (CurrentStage != Stage.Idle) return;

            onTransportDone = onDone;
            portalIndex = VoidPortal.Spawn(
                Player.GetSource_Misc("VoidTransport"),
                portalWorldPos, sustainDuration: 600);

            CurrentStage = Stage.WaitExpand;
            stageTimer = 0;
            SuctionProgress = 0f;
            BlackFlashAlpha = 0f;
            savedZoom = Main.GameZoomTarget;
            currentZoom = savedZoom;
            cameraInit = false;
        }

        /// <summary>
        /// 强制中断演出并恢复状态
        /// </summary>
        public void Abort() {
            if (CurrentStage == Stage.Idle) return;
            KillPortal();
            RestorePlayer();
            CurrentStage = Stage.Idle;
        }

        #endregion

        public override void PostUpdate() {
            if (CurrentStage == Stage.Idle) return;
            if (Main.dedServ) return;

            stageTimer++;
            VoidPortal portal = GetPortal();

            switch (CurrentStage) {
                case Stage.WaitExpand:
                    UpdateWaitExpand(portal);
                    break;
                case Stage.Hold:
                    UpdateHold();
                    break;
                case Stage.Suction:
                    UpdateSuction(portal);
                    break;
                case Stage.BlackFlash:
                    UpdateBlackFlash(portal);
                    break;
                case Stage.Finish:
                    UpdateFinish();
                    break;
            }
        }

        public override void ModifyScreenPosition() {
            if (CurrentStage == Stage.Idle) return;

            bool isSuctionActive = CurrentStage == Stage.Suction
                || CurrentStage == Stage.BlackFlash;
            bool isPreHeat = CurrentStage == Stage.Hold && stageTimer > HoldDuration / 2;

            //缩放
            float targetZoom;
            if (isSuctionActive) {
                float t = SuctionProgress;
                targetZoom = MathHelper.Lerp(savedZoom, SuctionTargetZoom, t * t);
            }
            else {
                targetZoom = savedZoom;
            }

            currentZoom = MathHelper.Lerp(currentZoom, targetZoom, ZoomLerpSpeed);
            Main.GameZoomTarget = currentZoom;

            //吸入和预热阶段锁定镜头到传送门中心
            if (isSuctionActive || isPreHeat) {
                VoidPortal portal = GetPortal();
                if (portal != null) {
                    if (!cameraInit) {
                        smoothedScreenPos = Main.screenPosition;
                        cameraInit = true;
                    }

                    Vector2 screenSize = new Vector2(Main.screenWidth, Main.screenHeight);
                    Vector2 desired = portal.PortalCenter - screenSize * 0.5f;
                    float lerpSpeed = isPreHeat ? PosLerpSpeed * 0.3f : PosLerpSpeed;
                    smoothedScreenPos = Vector2.Lerp(smoothedScreenPos, desired, lerpSpeed);
                    Main.screenPosition = smoothedScreenPos;
                }
            }
        }

        public override void PreUpdate() {
            //吸入和黑闪阶段锁定玩家操作
            if (CurrentStage == Stage.Suction || CurrentStage == Stage.BlackFlash) {
                LockControls();
            }
        }

        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo) {
            if (CurrentStage != Stage.Suction && CurrentStage != Stage.BlackFlash) return;

            VoidPortal portal = GetPortal();
            if (portal == null) return;

            //计算玩家朝传送门方向的旋转（身体被"拽"向裂隙）
            Vector2 toPortal = portal.PortalCenter - Player.Center;
            float angle = toPortal.ToRotation();
            //修正：角色面朝右时0度是正常，面朝左时需要翻转
            if (Player.direction == -1)
                angle = MathHelper.Pi - angle;

            float rotationStr = SuctionProgress * SuctionProgress;
            float maxRotation = MathHelper.PiOver4 * 0.8f;
            Player.fullRotation = MathHelper.Lerp(0f, maxRotation * MathF.Sign(angle), rotationStr)
                * MathHelper.Clamp(MathF.Abs(angle) / MathHelper.PiOver2, 0f, 1f);
            Player.fullRotationOrigin = new Vector2(Player.width / 2f, Player.height / 2f);

            //暗红色调：随吸入进度加深
            float darkening = 1f - SuctionProgress * 0.6f;
            float redTint = darkening + SuctionProgress * 0.1f;
            Color tint = new Color(
                MathHelper.Clamp(redTint, 0f, 1f),
                MathHelper.Clamp(darkening * 0.7f, 0f, 1f),
                MathHelper.Clamp(darkening * 0.5f, 0f, 1f),
                1f);

            drawInfo.colorArmorBody = drawInfo.colorArmorBody.MultiplyRGBA(tint);
            drawInfo.colorArmorHead = drawInfo.colorArmorHead.MultiplyRGBA(tint);
            drawInfo.colorArmorLegs = drawInfo.colorArmorLegs.MultiplyRGBA(tint);
            drawInfo.colorBodySkin = drawInfo.colorBodySkin.MultiplyRGBA(tint);
            drawInfo.colorHead = drawInfo.colorHead.MultiplyRGBA(tint);
            drawInfo.colorLegs = drawInfo.colorLegs.MultiplyRGBA(tint);
            drawInfo.colorEyes = drawInfo.colorEyes.MultiplyRGBA(tint);
            drawInfo.colorEyeWhites = drawInfo.colorEyeWhites.MultiplyRGBA(tint);

            //黑闪阶段玩家完全变黑（即将被虚空吞噬）
            if (BlackFlashAlpha > 0.5f) {
                float blackout = MathHelper.Clamp((BlackFlashAlpha - 0.5f) * 2f, 0f, 1f);
                float remain = 1f - blackout;
                Color blackTint = new Color(remain, remain, remain, 1f);
                drawInfo.colorArmorBody = drawInfo.colorArmorBody.MultiplyRGBA(blackTint);
                drawInfo.colorArmorHead = drawInfo.colorArmorHead.MultiplyRGBA(blackTint);
                drawInfo.colorArmorLegs = drawInfo.colorArmorLegs.MultiplyRGBA(blackTint);
                drawInfo.colorBodySkin = drawInfo.colorBodySkin.MultiplyRGBA(blackTint);
                drawInfo.colorHead = drawInfo.colorHead.MultiplyRGBA(blackTint);
                drawInfo.colorLegs = drawInfo.colorLegs.MultiplyRGBA(blackTint);
                drawInfo.colorEyes = drawInfo.colorEyes.MultiplyRGBA(blackTint);
                drawInfo.colorEyeWhites = drawInfo.colorEyeWhites.MultiplyRGBA(blackTint);
            }
        }

        #region 阶段逻辑

        void UpdateWaitExpand(VoidPortal portal) {
            //等待传送门展开到位
            if (portal == null) {
                CurrentStage = Stage.Idle;
                return;
            }

            if (portal.CurrentPhase == VoidPortal.Phase.Sustaining) {
                TransitionTo(Stage.Hold);
            }
        }

        void UpdateHold() {
            //展示后半段开始微量滤镜预热（让观众感知到"有事要发生"）
            if (stageTimer > HoldDuration / 2) {
                float preHeat = (float)(stageTimer - HoldDuration / 2) / (HoldDuration / 2);
                SuctionProgress = preHeat * 0.08f;
            }

            if (stageTimer >= HoldDuration) {
                TransitionTo(Stage.Suction);
            }
        }

        void UpdateSuction(VoidPortal portal) {
            //吸入进度曲线：从预热值平滑过渡到1
            float t = (float)stageTimer / SuctionDuration;
            t = MathHelper.Clamp(t, 0f, 1f);
            float target = t * t * (3f - 2f * t);
            //确保不低于预热值
            SuctionProgress = MathHelper.Max(0.08f, target);

            //牵引玩家向门中心
            if (portal != null) {
                Vector2 toPortal = portal.PortalCenter - Player.Center;
                float dist = toPortal.Length();
                if (dist > 4f) {
                    toPortal /= dist;
                    float pullSpeed = MathHelper.Lerp(0.5f, MaxPullSpeed, SuctionProgress);
                    Player.velocity = toPortal * pullSpeed;
                }
                else {
                    Player.velocity = Vector2.Zero;
                }
            }

            //屏幕震动递增
            float shakeStr = SuctionProgress * 2.5f;
            if (shakeStr > 0.3f) {
                Main.screenPosition += Main.rand.NextVector2Circular(shakeStr, shakeStr);
            }

            if (stageTimer >= SuctionDuration) {
                TransitionTo(Stage.BlackFlash);
            }
        }

        void UpdateBlackFlash(VoidPortal portal) {
            //黑闪曲线：上升→纯黑保持→结束
            if (stageTimer <= BlackFlashRise) {
                float rise = (float)stageTimer / BlackFlashRise;
                BlackFlashAlpha = rise * rise;
            }
            else {
                BlackFlashAlpha = 1f;
            }

            //黑闪期间吸入参数保持峰值
            SuctionProgress = 1f;

            if (stageTimer >= BlackFlashDuration) {
                TransitionTo(Stage.Finish);
            }
        }

        void UpdateFinish() {
            //关闭传送门
            KillPortal();
            RestorePlayer();

            onTransportDone?.Invoke();
            onTransportDone = null;
            CurrentStage = Stage.Idle;
        }

        void TransitionTo(Stage next) {
            CurrentStage = next;
            stageTimer = 0;
        }

        #endregion

        #region 辅助

        VoidPortal GetPortal() {
            if (portalIndex >= 0 && portalIndex < Main.maxProjectiles) {
                Projectile proj = Main.projectile[portalIndex];
                if (proj.active && proj.ModProjectile is VoidPortal vp) {
                    return vp;
                }
            }
            return VoidPortal.ActiveInstance;
        }

        void KillPortal() {
            VoidPortal portal = GetPortal();
            if (portal != null && portal.Projectile.active) {
                portal.Close();
            }
            portalIndex = -1;
        }

        void RestorePlayer() {
            BlackFlashAlpha = 0f;
            SuctionProgress = 0f;
            currentZoom = savedZoom;
            Main.GameZoomTarget = savedZoom;
            cameraInit = false;
            Player.fullRotation = 0f;
        }

        void LockControls() {
            Player.controlLeft = false;
            Player.controlRight = false;
            Player.controlUp = false;
            Player.controlDown = false;
            Player.controlJump = false;
            Player.controlUseItem = false;
            Player.controlUseTile = false;
            Player.controlHook = false;
            Player.controlMount = false;
        }

        #endregion
    }
}
