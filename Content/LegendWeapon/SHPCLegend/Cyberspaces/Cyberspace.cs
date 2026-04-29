using CalamityOverhaul.Common;
using CalamityOverhaul.Content.HackTimes;
using CalamityOverhaul.Content.RAMSystems;
using System;
using Terraria;
using Terraria.Audio;
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
        /// 各层维持领域时每秒消耗的 RAM 量
        /// </summary>
        public static readonly float[] LayerRamDrainPerSecond = { 0.2f, 1.6f, 6f };

        /// <summary>
        /// 取当前层的 RAM 每秒消耗量；未激活或层数为 0 时返回 0
        /// <br/>UI 与外部系统可调用此方法读取实时消耗速度
        /// </summary>
        public static float GetCurrentDrainRate() {
            if (!Active || CurrentLayer < 1 || CurrentLayer > MaxLayerCount) {
                return 0f;
            }
            return LayerRamDrainPerSecond[CurrentLayer - 1];
        }

        /// <summary>
        /// 取指定层(1..MaxLayerCount)的 RAM 每秒消耗量
        /// </summary>
        public static float GetLayerDrainRate(int layer) {
            if (layer < 1 || layer > MaxLayerCount) {
                return 0f;
            }
            return LayerRamDrainPerSecond[layer - 1];
        }

        //RAM 崩溃后强制锁定的剩余帧数，期间禁止重启领域
        private const int CrashLockoutFrames = 90;
        private static int crashLockoutTimer;

        /// <summary>
        /// 当前是否处于"系统崩溃"锁定中(刚被 RAM 耗尽强制关闭)
        /// </summary>
        public static bool IsCrashLockedOut => crashLockoutTimer > 0;

        /// <summary>
        /// 激活/升层最低应能维持的秒数：低于这个阈值就直接拒绝，避免"开一帧立刻崩溃"的劣质反馈
        /// </summary>
        public const float MinSustainSeconds = 1f;

        /// <summary>
        /// 检查目标层(1..MaxLayerCount)是否有足够 RAM 维持最低秒数
        /// <br/><see cref="HackTime.InfiniteHack"/> 模式下永远视为足够
        /// </summary>
        public static bool CanAffordLayer(int layer) {
            if (HackTime.InfiniteHack) {
                return true;
            }
            if (layer < 1 || layer > MaxLayerCount) {
                return false;
            }
            float required = LayerRamDrainPerSecond[layer - 1] * MinSustainSeconds;
            return RamSystem.CurrentRam >= required;
        }

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
        /// 收缩前的层数，用于收缩动画期间仍继续渲染各层边界环
        /// </summary>
        public static int RenderLayerCount {
            get {
                //返回展开进度大于阈值的最高层数
                int count = 0;
                for (int i = 0; i < MaxLayerCount; i++) {
                    if (layerExpand[i] > 0.01f) count = i + 1;
                }
                return count;
            }
        }

        /// <summary>
        /// 基础半径（第一层领域半径），单位为世界像素
        /// </summary>
        public static float BaseRadius = 600f;

        /// <summary>
        /// 当前最外层的目标半径（着色器使用，收缩期仍返回正在收缩的最高层半径）
        /// </summary>
        public static float Radius {
            get {
                int rLayer = Math.Max(CurrentLayer, RenderLayerCount);
                return rLayer > 0 ? GetLayerRadius(rLayer - 1) : BaseRadius;
            }
        }

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
        public static float GetLayerRadius(int layerIndex) {
            layerIndex = Math.Clamp(layerIndex, 0, MaxLayerCount - 1);
            return BaseRadius * LayerRadiusScale[layerIndex];
        }

        /// <summary>
        /// 获取指定层(0-indexed)的展开进度
        /// </summary>
        public static float GetLayerExpand(int layerIndex) {
            layerIndex = Math.Clamp(layerIndex, 0, MaxLayerCount - 1);
            return layerExpand[layerIndex];
        }

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

        //上次关闭前的层数，用于下次激活时恢复
        private static int lastLayer = 1;

        /// <summary>
        /// 激活赛博空间领域第一层（带爆发式展开+视觉特效）
        /// <br/>支持在前一次收缩动画尚未完成时立即重新展开，避免吞操作
        /// <br/>"系统崩溃"锁定期间(<see cref="IsCrashLockedOut"/>)会拒绝激活并播放"拒绝"音效
        /// <br/>RAM 不足以维持目标层 <see cref="MinSustainSeconds"/> 秒时也会拒绝，避免"开一帧立刻崩溃"
        /// </summary>
        public static void Activate(Player owner) {
            //RAM 耗尽后的强制锁定期间不允许立刻重启领域
            if (crashLockoutTimer > 0) {
                if (!VaultUtils.isServer && owner != null) {
                    SoundEngine.PlaySound(CWRSound.FailureCurrent with { Volume = 0.45f, Pitch = -0.4f }, owner.Center);
                }
                return;
            }
            //平滑接管：保留当前 layerExpand/Intensity，让动画从当前进度连续过渡
            //仅对超出层（>=1）清掉残留 burst，确保第一层独享爆发动画
            int resumeLayer = Math.Clamp(lastLayer, 1, MaxLayerCount);

            //RAM 余量门槛：拒绝在不可维持的余量下激活（且不静默——给玩家一个失败反馈）
            //从 resumeLayer 向下递减找到一个能维持的层；若连 L1 都不够，直接拒绝
            if (!HackTime.InfiniteHack) {
                while (resumeLayer >= 1 && !CanAffordLayer(resumeLayer)) {
                    resumeLayer--;
                }
                if (resumeLayer < 1) {
                    if (!VaultUtils.isServer && owner != null) {
                        SoundEngine.PlaySound(CWRSound.FailureCurrent with { Volume = 0.45f, Pitch = -0.4f }, owner.Center);
                        Color denyColor = new(255, 90, 80);
                        CombatText.NewText(owner.Hitbox, denyColor, "// LOW RAM", true);
                    }
                    return;
                }
            }
            for (int i = resumeLayer; i < MaxLayerCount; i++) {
                layerBurstTimer[i] = 0;
            }
            Active = true;
            CurrentLayer = resumeLayer;
            targetIntensity = 1f;
            //所有需要展开的层都触发爆发动画
            for (int i = 0; i < resumeLayer; i++) {
                layerBurstTimer[i] = BurstDurations[i];
            }
            SpawnActivationVFX(owner);

            if (!VaultUtils.isServer) {
                SoundEngine.PlaySound(CWRSound.FailureCurrent, owner.Center);
                SoundEngine.PlaySound(CWRSound.Faultrelease, owner.Center);
            }
        }

        /// <summary>
        /// 自由设置领域层数（可升可降，带展开/收缩过渡）
        /// <br/>升层时会先做 RAM 余量检查：维持不到 <see cref="MinSustainSeconds"/> 秒就拒绝并播放失败反馈
        /// </summary>
        public static void SetLayer(int layer, Player owner = null) {
            layer = Math.Clamp(layer, 1, MaxLayerCount);
            if (!Active) return;
            if (layer == CurrentLayer) return;

            //升层时校验 RAM 是否能维持新层最低秒数；拒绝时给一个失败反馈，避免"升一帧就崩"
            if (layer > CurrentLayer && !CanAffordLayer(layer)) {
                if (!VaultUtils.isServer && owner != null) {
                    SoundEngine.PlaySound(CWRSound.FailureCurrent with { Volume = 0.4f, Pitch = -0.3f }, owner.Center);
                    Color denyColor = new(255, 90, 80);
                    CombatText.NewText(owner.Hitbox, denyColor, $"// L{layer} - LOW RAM", true);
                }
                return;
            }

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
        /// <br/>立即把 Active 置为 false 表示用户意图，残余收缩动画不会阻挡再次激活
        /// </summary>
        public static void Deactivate() {
            //立即翻转激活意图，避免在收缩动画期间 UI/外部判定仍认为领域处于开启状态
            Active = false;
            targetIntensity = 0f;
            //记住当前层以便下次恢复
            if (CurrentLayer > 0) {
                lastLayer = CurrentLayer;
            }
            CurrentLayer = 0;
            //清掉所有未消费的爆发计时，避免下次激活与残余 burst 叠加产生抖动
            for (int i = 0; i < MaxLayerCount; i++) {
                layerBurstTimer[i] = 0;
            }

            if (!VaultUtils.isServer) {
                SoundEngine.PlaySound(CWRSound.Faultrelease, Main.LocalPlayer.Center);
            }
        }

        /// <summary>
        /// RAM 耗尽触发的"系统崩溃"：强制关闭领域、播放警示音效、设置短暂锁定期
        /// <br/>锁定期内 <see cref="Activate"/> 会被拒绝，避免出现"瞬间重启又瞬间崩溃"的抖动
        /// </summary>
        public static void TriggerSystemCrash() {
            if (!Active && CurrentLayer == 0 && crashLockoutTimer > 0) {
                return;
            }

            //先关闭领域，再设置锁定计时（Deactivate 会重置不少状态，所以锁定要在它之后）
            Deactivate();
            crashLockoutTimer = CrashLockoutFrames;

            if (!VaultUtils.isServer) {
                Player p = Main.LocalPlayer;
                if (p != null && p.active) {
                    //系统崩溃文字反馈，使用 RAM HUD 的危险色系
                    Color crashColor = new(255, 70, 70);
                    CombatText.NewText(p.Hitbox, crashColor, "// SYSTEM CRASH", true);
                    SoundEngine.PlaySound(CWRSound.FailureCurrent with { Volume = 0.85f, Pitch = -0.3f }, p.Center);
                    SoundEngine.PlaySound(CWRSound.Faultrelease with { Volume = 0.7f, Pitch = -0.5f }, p.Center);
                }
            }
        }

        /// <summary>
        /// 每帧逻辑更新，驱动多层展开/收缩过渡
        /// </summary>
        public static void Update() {
            //崩溃锁定计时
            if (crashLockoutTimer > 0) {
                crashLockoutTimer--;
            }

            //领域处于激活态时按当前层消耗 RAM；耗尽即触发系统崩溃强制关闭
            if (Active && CurrentLayer >= 1 && !HackTime.InfiniteHack) {
                float drain = LayerRamDrainPerSecond[CurrentLayer - 1];
                RamSystem.ConsumeOverTime(drain);
                if (RamSystem.CurrentRam <= 0f) {
                    TriggerSystemCrash();
                }
            }

            //累计效果时间：根据变速齿轮缩放，最低保持0.1倍速让着色器动画不完全停止
            float dt = 1f / 60f;
            float timeSpeed = TimeGear.IsTimeSlowed ? MathHelper.Max(TimeGear.TimeScale, 0.1f) : 1f;
            EffectTime += dt * timeSpeed;

            //强度过渡：关闭时Intensity收缩要比layerExpand更慢，确保收缩动画可见
            float intensityLerp;
            if (Active && CurrentLayer > 0) {
                intensityLerp = 0.045f;
                if (layerBurstTimer[0] > 0) {
                    float burstFactor = (float)layerBurstTimer[0] / BurstDurations[0];
                    intensityLerp = MathHelper.Lerp(0.08f, 0.25f, burstFactor);
                }
            }
            else {
                //关闭阶段：Intensity最后消失，速率极慢
                intensityLerp = 0.015f;
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

            //所有层收缩完毕且Intensity足够低后清掉残余状态（不影响 Active，已在 Deactivate 时立即翻转）
            if (!Active && CurrentLayer == 0) {
                bool allCollapsed = true;
                for (int i = 0; i < MaxLayerCount; i++) {
                    if (layerExpand[i] >= 0.005f) {
                        allCollapsed = false;
                        break;
                    }
                }
                if (allCollapsed && Intensity < 0.005f) {
                    Intensity = 0f;
                    EffectTime = 0f;
                    ambientBoltTimer = 0;
                }
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
            lastLayer = 1;
            targetIntensity = 0f;
            ambientBoltTimer = 0;
            crashLockoutTimer = 0;
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
        /// 判断指定世界坐标是否处于当前赛博空间领域内
        /// </summary>
        public static bool IsInsideDomain(Vector2 worldPos) {
            //仅按视觉强度判定，避免Active翻转后与正在收缩的画面不一致
            if (Intensity < 0.01f) return false;
            float dx = worldPos.X - Main.LocalPlayer.Center.X;
            float dy = worldPos.Y - Main.LocalPlayer.Center.Y;
            return dx * dx + dy * dy <= EffectiveOuterRadius * EffectiveOuterRadius;
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
