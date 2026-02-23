using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors
{
    /// <summary>
    /// 可复用的演出运镜系统
    /// 通过 <see cref="ApolliaPlayer.ModifyScreenPosition"/> 驱动屏幕位置和缩放，
    /// 不在AI帧中直接修改 Main.screenPosition，避免与引擎的摄像机流程冲突
    /// </summary>
    internal class CutsceneCamera
    {
        /// <summary>运镜是否处于激活状态</summary>
        public bool Active { get; private set; }

        /// <summary>期望摄像机聚焦的世界坐标</summary>
        public Vector2 FocusTarget;

        /// <summary>摄像机位置插值速度 (0~1)</summary>
        public float PositionLerpSpeed = 0.03f;

        /// <summary>目标缩放倍率</summary>
        public float TargetZoom = 1f;

        /// <summary>缩放插值速度 (0~1)</summary>
        public float ZoomLerpSpeed = 0.02f;

        /// <summary>是否在运镜期间锁定玩家操作</summary>
        public bool LockPlayerControls = true;

        //内部状态
        private float currentZoom = 1f;
        private Vector2 smoothedScreenPos;
        private bool initialized;

        //震动状态
        private Vector2 shakeDirection;
        private float shakeIntensity;
        private float shakeDecay;
        private int shakeDuration;
        private int shakeTimer;

        /// <summary>
        /// 启动运镜
        /// </summary>
        public void Start(Vector2 initialFocus, float posLerp = 0.03f, float zoom = 1f, float zoomLerp = 0.02f) {
            Active = true;
            FocusTarget = initialFocus;
            PositionLerpSpeed = posLerp;
            TargetZoom = zoom;
            ZoomLerpSpeed = zoomLerp;
            currentZoom = Main.GameZoomTarget;
            initialized = false;
        }

        /// <summary>
        /// 停止运镜并开始平滑恢复
        /// </summary>
        public void Stop() {
            Active = false;
        }

        /// <summary>
        /// 强制立即重置到默认状态
        /// </summary>
        public void Reset() {
            Active = false;
            currentZoom = 1f;
            TargetZoom = 1f;
            initialized = false;
            shakeTimer = 0;
        }

        /// <summary>
        /// 触发屏幕震动——在运镜锁定期间替代原版 PunchCameraModifier
        /// </summary>
        /// <param name="direction">震动方向（会自动归一化），传入 Zero 则随机方向</param>
        /// <param name="intensity">初始偏移像素强度</param>
        /// <param name="decay">每帧衰减系数 (0~1)，越小衰减越快</param>
        /// <param name="duration">持续帧数</param>
        public void Shake(Vector2 direction, float intensity, float decay = 0.9f, int duration = 20) {
            if (direction == Vector2.Zero) {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                direction = angle.ToRotationVector2();
            }
            else {
                direction.Normalize();
            }
            shakeDirection = direction;
            shakeIntensity = intensity;
            shakeDecay = MathHelper.Clamp(decay, 0f, 0.99f);
            shakeDuration = duration;
            shakeTimer = 0;
        }

        /// <summary>
        /// 在 <see cref="ApolliaPlayer.ModifyScreenPosition"/> 中调用，
        /// 平滑地将屏幕位置和缩放过渡到目标值
        /// </summary>
        public void Apply() {
            //缩放始终平滑过渡（无论激活与否都要恢复到目标值）
            float zoomTarget = Active ? TargetZoom : 1f;
            float zoomSpeed = Active ? ZoomLerpSpeed : 0.02f;
            currentZoom = MathHelper.Lerp(currentZoom, zoomTarget, zoomSpeed);
            Main.GameZoomTarget = currentZoom;

            if (!Active) {
                //非激活时不干预屏幕位置，让引擎自然控制
                initialized = false;
                return;
            }

            //初始化平滑位置（首帧捕获当前屏幕位置避免跳切）
            if (!initialized) {
                smoothedScreenPos = Main.screenPosition;
                initialized = true;
            }

            //计算期望屏幕位置：让FocusTarget位于屏幕中心
            //ModifyScreenPosition在引擎缩放之前运行，所以使用原始屏幕尺寸
            Vector2 screenSize = new Vector2(Main.screenWidth, Main.screenHeight);
            Vector2 desiredScreenPos = FocusTarget - screenSize * 0.5f;

            //平滑插值
            smoothedScreenPos = Vector2.Lerp(smoothedScreenPos, desiredScreenPos, PositionLerpSpeed);
            Main.screenPosition = smoothedScreenPos;

            //叠加震动偏移
            if (shakeTimer < shakeDuration && shakeIntensity > 0.5f) {
                float progress = shakeTimer / (float)shakeDuration;
                float currentIntensity = shakeIntensity * MathF.Pow(shakeDecay, shakeTimer) * (1f - progress);
                float sign = (shakeTimer % 2 == 0) ? 1f : -1f;
                float rotJitter = Main.rand.NextFloat(-0.3f, 0.3f);
                Vector2 offset = shakeDirection.RotatedBy(rotJitter) * currentIntensity * sign;
                Main.screenPosition += offset;
                shakeTimer++;
            }

            //锁定玩家操作
            if (LockPlayerControls) {
                Player player = Main.LocalPlayer;
                if (player != null && player.active) {
                    player.controlLeft = false;
                    player.controlRight = false;
                    player.controlUp = false;
                    player.controlDown = false;
                    player.controlJump = false;
                    player.controlUseItem = false;
                }
            }
        }
    }
}
