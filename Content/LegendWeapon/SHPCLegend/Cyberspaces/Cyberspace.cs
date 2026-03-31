using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

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

        /// <summary>
        /// 着色器专用累计时间，骇客时间期间以1/10速率推进
        /// </summary>
        public static float EffectTime { get; private set; }

        private static float targetIntensity;
        private static float targetExpand;

        //爆发阶段：激活后的前N帧用更高的lerp速率实现快速展开
        private const int BurstDuration = 14;
        private static int burstTimer;

        /// <summary>
        /// 激活赛博空间领域（带爆发式展开+视觉特效）
        /// </summary>
        public static void Activate(Player owner) {
            Active = true;
            targetIntensity = 1f;
            targetExpand = 1f;
            burstTimer = BurstDuration;
            SpawnActivationVFX(owner);
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
            //累计效果时间：骇客时间期间放缓10倍
            float dt = 1f / 60f;
            float timeSpeed = HackTime.HackTime.Active ? 0.1f : 1f;
            EffectTime += dt * timeSpeed;

            if (burstTimer > 0) {
                //爆发阶段：极速展开+强度拉满
                burstTimer--;
                float burstFactor = (float)burstTimer / BurstDuration; //1→0，越往后越接近目标
                float expandLerp = MathHelper.Lerp(0.06f, 0.22f, burstFactor);
                float intensityLerp = MathHelper.Lerp(0.08f, 0.25f, burstFactor);
                ExpandProgress = MathHelper.Lerp(ExpandProgress, targetExpand, expandLerp);
                Intensity = MathHelper.Lerp(Intensity, targetIntensity, intensityLerp);
            }
            else {
                //常规阶段：平滑过渡
                float intensityLerp = Active ? 0.045f : 0.06f;
                Intensity = MathHelper.Lerp(Intensity, targetIntensity, intensityLerp);
                ExpandProgress = MathHelper.Lerp(ExpandProgress, targetExpand, 0.035f);
            }

            //收缩完毕后彻底关闭
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
            EffectTime = 0f;
            targetIntensity = 0f;
            targetExpand = 0f;
            burstTimer = 0;
        }

        /// <summary>
        /// 生成领域激活时的视觉特效弹幕（冲击波+故障闪电）
        /// </summary>
        private static void SpawnActivationVFX(Player owner) {
            if (Main.myPlayer != owner.whoAmI) return;

            IEntitySource source = owner.GetSource_FromThis();
            Vector2 center = owner.Center;

            //环形冲击波
            Projectile.NewProjectile(source, center, Vector2.Zero,
                ModContent.ProjectileType<CyberShockwaveProj>(), 0, 0, owner.whoAmI);

            //故障闪电（6~8条，均匀分布+随机偏移+延迟交错）
            int boltCount = Main.rand.Next(6, 9);
            float baseAngle = Main.rand.NextFloat() * MathHelper.TwoPi;
            for (int i = 0; i < boltCount; i++) {
                float angle = baseAngle + MathHelper.TwoPi * i / boltCount
                    + Main.rand.NextFloat(-0.28f, 0.28f);
                int delay = Main.rand.Next(0, 5);
                Projectile.NewProjectile(source, center, Vector2.Zero,
                    ModContent.ProjectileType<CyberGlitchBoltProj>(), 0, 0, owner.whoAmI,
                    ai0: angle, ai1: delay);
            }
        }
    }
}
