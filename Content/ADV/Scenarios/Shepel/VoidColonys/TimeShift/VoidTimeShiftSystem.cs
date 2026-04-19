using Microsoft.Xna.Framework;
using System;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.VoidColonys.TimeShift
{
    /// <summary>
    /// 虚空聚落时空叠加机制核心状态
    /// 管理当前所处时代、切换演出进度和过去滤镜强度
    /// 仅在客户端运行，所有输入和渲染都从此读写
    /// </summary>
    internal static class VoidTimeShiftSystem
    {
        /// <summary>
        /// 切换演出总帧数，瞬发模式下过渡完整时长
        /// </summary>
        public const int TransitionDuration = 30;

        /// <summary>
        /// 滤镜强度每帧线性变化量，从当前值平滑趋近目标
        /// </summary>
        private const float FilterEaseRate = 0.12f;

        /// <summary>
        /// 过渡演出衰减帧数外的冗余线性淡出速率
        /// </summary>
        private const float TransitionDecayRate = 0.1f;

        /// <summary>
        /// 当前所处时代
        /// </summary>
        public static VoidEra CurrentEra { get; private set; } = VoidEra.Present;

        /// <summary>
        /// 过去滤镜强度0到1，Past时逼近1，Present时逼近0
        /// </summary>
        public static float FilterIntensity { get; private set; }

        /// <summary>
        /// 切换演出强度0到1的钟形曲线，只在切换瞬间的短暂过渡期间非零
        /// </summary>
        public static float TransitionStrength { get; private set; }

        /// <summary>
        /// 切换演出剩余帧数
        /// </summary>
        private static int transitionTimer;

        /// <summary>
        /// 是否有待处理的切换请求
        /// </summary>
        private static bool toggleRequested;

        /// <summary>
        /// 是否处于过去时代
        /// </summary>
        public static bool InPast => CurrentEra == VoidEra.Past;

        /// <summary>
        /// 是否正在切换过渡中
        /// </summary>
        public static bool InTransition => transitionTimer > 0;

        /// <summary>
        /// 由玩家按键调用，请求切换时代
        /// 过渡期间忽略后续请求，避免连按造成闪屏
        /// </summary>
        public static void RequestToggle() {
            if (transitionTimer > 0) {
                return;
            }
            toggleRequested = true;
        }

        /// <summary>
        /// 强制重置到现在，用于离开虚空聚落或进入游戏菜单
        /// </summary>
        public static void Reset() {
            CurrentEra = VoidEra.Present;
            FilterIntensity = 0f;
            TransitionStrength = 0f;
            transitionTimer = 0;
            toggleRequested = false;
        }

        /// <summary>
        /// 每帧推进过渡计时与滤镜插值
        /// </summary>
        /// <param name="active">当前是否仍在虚空聚落维度</param>
        public static void Update(bool active) {
            if (!active) {
                //离开虚空聚落强制回到现在并平滑淡出
                CurrentEra = VoidEra.Present;
                toggleRequested = false;
                transitionTimer = 0;
                FilterIntensity = Math.Max(0f, FilterIntensity - FilterEaseRate);
                TransitionStrength = Math.Max(0f, TransitionStrength - TransitionDecayRate);
                return;
            }

            //切换请求在同一帧立即翻转Era，让演出同时盖住"离开现在"与"到达过去"两端
            if (toggleRequested && transitionTimer == 0) {
                toggleRequested = false;
                transitionTimer = TransitionDuration;
                CurrentEra = CurrentEra == VoidEra.Present ? VoidEra.Past : VoidEra.Present;
            }

            if (transitionTimer > 0) {
                //sin钟形曲线：0在两端，1在中点，切换中段强度最高
                float t = 1f - (float)transitionTimer / TransitionDuration;
                TransitionStrength = MathF.Sin(MathHelper.Pi * t);
                transitionTimer--;
            }
            else {
                TransitionStrength = Math.Max(0f, TransitionStrength - TransitionDecayRate);
            }

            //滤镜强度线性逼近目标
            float target = CurrentEra == VoidEra.Past ? 1f : 0f;
            if (FilterIntensity < target) {
                FilterIntensity = Math.Min(target, FilterIntensity + FilterEaseRate);
            }
            else if (FilterIntensity > target) {
                FilterIntensity = Math.Max(target, FilterIntensity - FilterEaseRate);
            }
        }
    }

    /// <summary>
    /// 虚空聚落时空叠加维度下可能处于的时代
    /// 预留枚举以便后续扩展（例如更远的远古时代）
    /// </summary>
    internal enum VoidEra
    {
        /// <summary>
        /// 当前时代，虚空聚落实验室群的原貌
        /// </summary>
        Present,
        /// <summary>
        /// 百年前的过去时代，仅存废墟与数据残影
        /// </summary>
        Past,
    }
}
