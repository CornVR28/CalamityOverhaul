namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces
{
    /// <summary>
    /// 赛博空间领域系统 —— 状态管理器
    /// <br/>主题风格：赛博朋克2077黑墙AI，深红色系，方块栅格边缘，内部波形特效
    /// <br/>通过 <see cref="Activate"/> / <see cref="Deactivate"/> 控制开关，
    /// 展开与收缩带有平滑过渡动画
    /// </summary>
    internal class Cyberspace : ICWRLoader
    {
        void ICWRLoader.UnLoadData() => Reset();
        /// <summary>
        /// 赛博空间是否处于激活状态
        /// </summary>
        public static bool Active { get; private set; }

        /// <summary>
        /// 当前效果强度 (0-1)，用于着色器 intensity 参数
        /// </summary>
        public static float Intensity { get; set; }

        /// <summary>
        /// 展开进度 (0=完全收起, 1=完全展开)
        /// </summary>
        public static float ExpandProgress { get; set; }

        /// <summary>
        /// 领域半径，单位为世界像素
        /// </summary>
        public static float Radius = 600f;

        /// <summary>
        /// 方形栅格单元边长，控制边缘方块的大小
        /// </summary>
        public static float GridSize = 24f;

        /// <summary>
        /// 场景压暗强度 (0=不压暗, 1=最大压暗至约25%亮度)
        /// </summary>
        public static float DimStrength = 0.85f;

        private static float targetIntensity;
        private static float targetExpand;

        /// <summary>
        /// 激活赛博空间领域
        /// </summary>
        public static void Activate() {
            Active = true;
            targetIntensity = 1f;
            targetExpand = 1f;
        }

        /// <summary>
        /// 关闭赛博空间领域（带收缩动画）
        /// </summary>
        public static void Deactivate() {
            targetIntensity = 0f;
            targetExpand = 0f;
        }

        /// <summary>
        /// 每帧逻辑更新，驱动展开/收缩过渡
        /// </summary>
        public static void Update() {
            // 展开时稍慢，收缩时稍快
            float intensityLerp = Active ? 0.045f : 0.06f;
            Intensity = MathHelper.Lerp(Intensity, targetIntensity, intensityLerp);
            ExpandProgress = MathHelper.Lerp(ExpandProgress, targetExpand, 0.035f);

            // 收缩完毕后彻底关闭
            if (targetExpand <= 0f && ExpandProgress < 0.005f) {
                ExpandProgress = 0f;
                Intensity = 0f;
                Active = false;
            }
        }

        /// <summary>
        /// 立即重置所有状态
        /// </summary>
        public static void Reset() {
            Active = false;
            Intensity = 0f;
            ExpandProgress = 0f;
            targetIntensity = 0f;
            targetExpand = 0f;
        }
    }
}
