using CalamityOverhaul.Common;
using InnoVault.PRT;
using System;
using Terraria;
using Terraria.Audio;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TimeShift
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
        /// 外部演出是否正在接管本系统的滤镜/过渡状态
        /// 接管期间 <see cref="Update"/> 内部逻辑完全让位，尘粒与插值都暂停
        /// </summary>
        public static bool ExternallyDriven { get; private set; }

        /// <summary>
        /// 由演出代码调用，进入接管模式；在此模式下外部以 <see cref="DriveState"/> 直接写入数据
        /// </summary>
        public static void BeginExternalDrive() {
            ExternallyDriven = true;
            toggleRequested = false;
            transitionTimer = 0;
        }

        /// <summary>
        /// 结束接管，回归正常规则，并立即清零滤镜与过渡
        /// </summary>
        public static void EndExternalDrive() {
            ExternallyDriven = false;
            toggleRequested = false;
            transitionTimer = 0;
            FilterIntensity = 0f;
            TransitionStrength = 0f;
            CurrentEra = VoidEra.Present;
        }

        /// <summary>
        /// 接管模式下，由外部每帧写入滤镜与过渡强度
        /// </summary>
        public static void DriveState(float filterIntensity, float transitionStrength) {
            if (!ExternallyDriven) {
                return;
            }
            FilterIntensity = MathHelper.Clamp(filterIntensity, 0f, 1f);
            TransitionStrength = MathHelper.Clamp(transitionStrength, 0f, 1f);
            CurrentEra = FilterIntensity > 0.5f ? VoidEra.Past : VoidEra.Present;
        }

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
            //演出接管期间完全让位
            if (ExternallyDriven) {
                return;
            }

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
                if (!VaultUtils.isServer) {
                    SoundEngine.PlaySound(CurrentEra == VoidEra.Present ? CWRSound.InvasionPast : CWRSound.InvasionPastPosten);
                }
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

            //滤镜显著时在屏幕外缘生成环境尘粒
            SpawnAmbientMotes();
        }

        /// <summary>
        /// 过去滤镜生效期间，在屏幕边缘随机生成缓慢漂入的尘粒
        /// 生成概率与滤镜强度正相关，避免切入切出瞬间突兀
        /// </summary>
        private static void SpawnAmbientMotes() {
            if (FilterIntensity < 0.35f) {
                return;
            }
            if (Main.netMode == Terraria.ID.NetmodeID.Server) {
                return;
            }

            //单帧最多尝试一次，基础概率约每秒2粒
            if (Main.rand.NextFloat() > 0.04f * FilterIntensity) {
                return;
            }

            int side = Main.rand.Next(3);
            Vector2 pos;
            Vector2 vel;
            //从左右或上方边缘进入，下沉速度统一较低
            if (side == 0) {
                pos = new Vector2(Main.screenPosition.X - 24f,
                    Main.screenPosition.Y + Main.rand.NextFloat(0f, Main.screenHeight));
                vel = new Vector2(Main.rand.NextFloat(0.25f, 0.55f), Main.rand.NextFloat(-0.05f, 0.15f));
            }
            else if (side == 1) {
                pos = new Vector2(Main.screenPosition.X + Main.screenWidth + 24f,
                    Main.screenPosition.Y + Main.rand.NextFloat(0f, Main.screenHeight));
                vel = new Vector2(Main.rand.NextFloat(-0.55f, -0.25f), Main.rand.NextFloat(-0.05f, 0.15f));
            }
            else {
                pos = new Vector2(Main.screenPosition.X + Main.rand.NextFloat(0f, Main.screenWidth),
                    Main.screenPosition.Y - 24f);
                vel = new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextFloat(0.1f, 0.25f));
            }

            //配色与shader滤镜调色板保持同族：琥珀灰或冷蓝灰随机二选一
            Color color = Main.rand.NextBool(2)
                ? new Color(188, 172, 138)
                : new Color(142, 158, 184);

            var mote = new PRT_VoidAshMote {
                Position = pos,
                Velocity = vel,
                Color = color,
                Scale = Main.rand.NextFloat(0.18f, 0.28f) * 10,
                Lifetime = Main.rand.Next(260, 420),
            };
            PRTLoader.AddParticle(mote);
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
