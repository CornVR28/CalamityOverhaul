using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces
{
    /// <summary>
    /// 赛博空间领域系统 —— 多层状态管理器
    /// <br/>主题风格：赛博朋克2077黑墙AI，深红色系，方块栅格边缘，内部波形特效
    /// <br/>支持最多 <see cref="MaxLayerCount"/> 层嵌套领域，层层递进，越发震撼
    /// <br/>通过 <see cref="Activate"/> 开启第一层，<see cref="SetLayer"/> 升层，
    /// <see cref="Deactivate"/> 收缩关闭
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

        // ====== 多层领域配置 ======

        /// <summary>
        /// 最大支持层数
        /// </summary>
        public const int MaxLayerCount = 3;

        /// <summary>
        /// 每层半径相对于基础半径的倍率
        /// </summary>
        private static readonly float[] LayerRadiusScale = { 1.0f, 1.7f, 2.6f };

        /// <summary>
        /// 每层独立展开进度 (0~1)
        /// </summary>
        private static readonly float[] layerExpand = new float[MaxLayerCount];
        private static readonly int[] layerBurstTimer = new int[MaxLayerCount];

        /// <summary>
        /// 当前领域层数 (1~MaxLayerCount)，0表示正在收缩/未激活
        /// </summary>
        public static int CurrentLayer { get; private set; }

        /// <summary>
        /// 基础半径（第一层领域半径），单位为世界像素
        /// </summary>
        public static float BaseRadius = 600f;

        /// <summary>
        /// 当前最外层的目标半径（着色器使用）
        /// </summary>
        public static float Radius => CurrentLayer > 0 ? GetLayerRadius(CurrentLayer - 1) : BaseRadius;

        /// <summary>
        /// 全局展开进度 = 有效外半径 / 目标外半径（着色器使用）
        /// </summary>
        public static float ExpandProgress {
            get {
                float r = Radius;
                if (r <= 0f) return 0f;
                return MathHelper.Clamp(EffectiveOuterRadius / r, 0f, 1f);
            }
        }

        /// <summary>
        /// 当前实际有效的最外层半径（所有层中最大的有效半径）
        /// </summary>
        public static float EffectiveOuterRadius {
            get {
                float maxR = 0f;
                for (int i = 0; i < MaxLayerCount; i++) {
                    float r = GetLayerRadius(i) * layerExpand[i];
                    if (r > maxR) maxR = r;
                }
                return maxR;
            }
        }

        /// <summary>
        /// 获取指定层(0-indexed)的完整半径
        /// </summary>
        public static float GetLayerRadius(int layerIndex) => BaseRadius * LayerRadiusScale[layerIndex];

        /// <summary>
        /// 获取指定层(0-indexed)的展开进度
        /// </summary>
        public static float GetLayerExpand(int layerIndex) => layerExpand[layerIndex];

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

        //爆发阶段：每层持续帧数递增（高层领域大，过渡更久）
        private static readonly int[] BurstDurations = { 14, 24, 36 };
        //常规展开lerp速率：高层更缓
        private static readonly float[] ExpandLerps = { 0.035f, 0.020f, 0.013f };
        //收缩lerp速率：高层更缓
        private static readonly float[] ContractLerps = { 0.050f, 0.030f, 0.020f };

        //环境故障闪电计时器（二层以上生效）
        private static int ambientBoltTimer;

        /// <summary>
        /// 激活赛博空间领域第一层（带爆发式展开+视觉特效）
        /// </summary>
        public static void Activate(Player owner) {
            //重置所有层展开进度，确保每次激活都从零开始展开
            for (int i = 0; i < MaxLayerCount; i++) {
                layerExpand[i] = 0f;
                layerBurstTimer[i] = 0;
            }
            Active = true;
            CurrentLayer = 1;
            targetIntensity = 1f;
            Intensity = 0f;
            layerBurstTimer[0] = BurstDurations[0];
            SpawnActivationVFX(owner);
        }

        /// <summary>
        /// 自由设置领域层数（可升可降，带展开/收缩过渡）
        /// </summary>
        public static void SetLayer(int layer, Player owner = null) {
            layer = Math.Clamp(layer, 1, MaxLayerCount);
            if (!Active || layer == CurrentLayer) return;

            int oldLayer = CurrentLayer;
            CurrentLayer = layer;

            if (layer > oldLayer) {
                //升层：新层爆发展开（高层持续更久）
                for (int i = oldLayer; i < layer; i++) {
                    layerBurstTimer[i] = BurstDurations[i];
                }
                //升层视觉特效
                if (owner != null) {
                    SpawnLayerVFX(owner, oldLayer, layer);
                }
            }
            //降层：超出的层会在Update中自然收缩（target=0），无需额外处理
        }

        /// <summary>
        /// 关闭赛博空间领域（所有层带收缩动画）
        /// </summary>
        public static void Deactivate() {
            targetIntensity = 0f;
            CurrentLayer = 0;
        }

        /// <summary>
        /// 每帧逻辑更新，驱动多层展开/收缩过渡
        /// </summary>
        public static void Update() {
            //累计效果时间：骇客时间期间放缓10倍
            float dt = 1f / 60f;
            float timeSpeed = HackTime.HackTime.Active ? 0.1f : 1f;
            EffectTime += dt * timeSpeed;

            //强度过渡
            float intensityLerp = Active ? 0.045f : 0.06f;
            if (layerBurstTimer[0] > 0) {
                float burstFactor = (float)layerBurstTimer[0] / BurstDurations[0];
                intensityLerp = MathHelper.Lerp(0.08f, 0.25f, burstFactor);
            }
            Intensity = MathHelper.Lerp(Intensity, targetIntensity, intensityLerp);

            //逐层展开/收缩（高层用更缓的lerp，过渡更平滑可见）
            for (int i = 0; i < MaxLayerCount; i++) {
                float target = (i < CurrentLayer) ? 1f : 0f;
                int burstDur = BurstDurations[i];

                if (layerBurstTimer[i] > 0) {
                    layerBurstTimer[i]--;
                    float burstFactor = (float)layerBurstTimer[i] / burstDur;
                    //爆发lerp：高层起始更小、结束更小，整体更缓
                    float burstLerpMin = MathHelper.Lerp(0.06f, 0.025f, (float)i / (MaxLayerCount - 1));
                    float burstLerpMax = MathHelper.Lerp(0.22f, 0.10f, (float)i / (MaxLayerCount - 1));
                    float expandLerp = MathHelper.Lerp(burstLerpMin, burstLerpMax, burstFactor);
                    layerExpand[i] = MathHelper.Lerp(layerExpand[i], target, expandLerp);
                }
                else {
                    float expandLerp = target > 0f ? ExpandLerps[i] : ContractLerps[i];
                    layerExpand[i] = MathHelper.Lerp(layerExpand[i], target, expandLerp);
                }

                if (target <= 0f && layerExpand[i] < 0.005f)
                    layerExpand[i] = 0f;
            }

            //二层以上：周期性从外环释放环境故障闪电
            if (CurrentLayer >= 2 && Intensity > 0.5f) {
                ambientBoltTimer--;
                if (ambientBoltTimer <= 0) {
                    SpawnAmbientBolts();
                    ambientBoltTimer = 40 + Main.rand.Next(-8, 12);
                }
            }

            //所有层收缩完毕后彻底关闭
            if (CurrentLayer == 0 && layerExpand[0] < 0.005f) {
                Reset();
            }
        }

        /// <summary>
        /// 立即重置所有状态
        /// </summary>
        public static void Reset() {
            Active = false;
            Intensity = 0f;
            EffectTime = 0f;
            CurrentLayer = 0;
            targetIntensity = 0f;
            ambientBoltTimer = 0;
            for (int i = 0; i < MaxLayerCount; i++) {
                layerExpand[i] = 0f;
                layerBurstTimer[i] = 0;
            }
        }

        /// <summary>
        /// 生成第一层领域激活时的视觉特效弹幕（冲击波+故障闪电）
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

        /// <summary>
        /// 升层时生成冲击波+故障闪电（数量随层数递增）
        /// </summary>
        private static void SpawnLayerVFX(Player owner, int oldLayer, int newLayer) {
            if (Main.myPlayer != owner.whoAmI) return;

            IEntitySource source = owner.GetSource_FromThis();
            Vector2 center = owner.Center;

            //新层冲击波
            Projectile.NewProjectile(source, center, Vector2.Zero,
                ModContent.ProjectileType<CyberShockwaveProj>(), 0, 0, owner.whoAmI);

            //故障闪电数量随层数增多
            int boltCount = Main.rand.Next(4 + newLayer * 2, 7 + newLayer * 2);
            float baseAngle = Main.rand.NextFloat() * MathHelper.TwoPi;
            for (int i = 0; i < boltCount; i++) {
                float angle = baseAngle + MathHelper.TwoPi * i / boltCount
                    + Main.rand.NextFloat(-0.28f, 0.28f);
                int delay = Main.rand.Next(0, 4);
                Projectile.NewProjectile(source, center, Vector2.Zero,
                    ModContent.ProjectileType<CyberGlitchBoltProj>(), 0, 0, owner.whoAmI,
                    ai0: angle, ai1: delay);
            }
        }

        /// <summary>
        /// 从最外层边缘周期性释放环境故障闪电（仅二层以上触发）
        /// </summary>
        private static void SpawnAmbientBolts() {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return;
            if (Main.myPlayer != player.whoAmI) return;

            IEntitySource source = player.GetSource_FromThis();
            float outerR = EffectiveOuterRadius;
            Vector2 center = player.Center;

            //1~(1+层数)条闪电从领域内部随机位置向外射出
            int count = Main.rand.Next(1, 1 + CurrentLayer);
            for (int i = 0; i < count; i++) {
                float angle = Main.rand.NextFloat() * MathHelper.TwoPi;
                //生成在领域内部(40%~85%半径处)，而不是边缘
                float spawnDist = outerR * Main.rand.NextFloat(0.4f, 0.85f);
                Vector2 spawnPos = center + angle.ToRotationVector2() * spawnDist;
                int delay = Main.rand.Next(0, 6);
                Projectile.NewProjectile(source, spawnPos, Vector2.Zero,
                    ModContent.ProjectileType<CyberGlitchBoltProj>(), 0, 0, player.whoAmI,
                    ai0: angle, ai1: delay);
            }
        }
    }
}
