using Terraria;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalDestroyer.Rendering
{
    /// <summary>
    /// 毁灭者视觉模式
    /// </summary>
    internal enum DestroyerVisualMode
    {
        /// <summary>
        /// 常态——细微红橙描边+暗部红化，解决夜晚看不清问题
        /// </summary>
        Idle = 0,
        /// <summary>
        /// 警告（蓄力冲刺/激光等）——红黄高对比脉冲描边
        /// </summary>
        Warning = 1,
        /// <summary>
        /// 冲刺中——白热橙边+横向能量条纹
        /// </summary>
        Dashing = 2,
    }

    /// <summary>
    /// 毁灭者视觉状态共享容器
    /// <br/>头部 AI 每帧根据状态机推送 Mode/Intensity/Progress，
    /// <br/>身体与尾巴在 Draw 时读取以保持整条蠕虫的滤镜一致
    /// </summary>
    internal static class DestroyerVisualState
    {
        public static DestroyerVisualMode Mode { get; private set; } = DestroyerVisualMode.Idle;
        /// <summary>
        /// 着色器总强度 0~1
        /// </summary>
        public static float Intensity { get; private set; }
        /// <summary>
        /// 警告/冲刺过程进度 0~1，仅在 Warning/Dashing 模式下有效
        /// </summary>
        public static float Progress { get; private set; }
        /// <summary>
        /// 上一次推送视觉状态的游戏帧——若 Boss 暂时不存在则自动衰减回常态
        /// </summary>
        public static long LastPushFrame { get; private set; }

        /// <summary>
        /// 距离上次推送是否过期（>5 帧未推送视为头部已离场，自动退化为常态）
        /// </summary>
        public static bool Stale => Main.GameUpdateCount - LastPushFrame > 5;

        /// <summary>
        /// 头部 AI 每帧推送的入口
        /// </summary>
        public static void Push(DestroyerVisualMode mode, float intensity, float progress = 0f) {
            Mode = mode;
            Intensity = MathHelper.Clamp(intensity, 0f, 1f);
            Progress = MathHelper.Clamp(progress, 0f, 1f);
            LastPushFrame = Main.GameUpdateCount;
        }

        /// <summary>
        /// 让身体/尾巴在 Draw 时读取当前可用的视觉状态。
        /// 若头部一段时间未推送，自动返回最低限度的常态（避免身体保持警告色）。
        /// </summary>
        public static (DestroyerVisualMode mode, float intensity, float progress) Read() {
            if (Stale) {
                return (DestroyerVisualMode.Idle, 0.55f, 0f);
            }
            return (Mode, Intensity, Progress);
        }
    }
}
