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
    /// 赛博空间领域的玩家级状态承载，所有原本散落在 <see cref="Cyberspace"/> 静态字段上的运行时数据
    /// 都搬迁到这里，以玩家实例为粒度持有，便于未来网络同步与多人独立体验
    /// </summary>
    public class CyberspacePlayer : ModPlayer
    {
        //是否激活
        public bool Active { get; internal set; }

        //内部强度原值，对外通过 Intensity 暴露
        internal float intensityRaw;

        /// <summary>
        /// 当前效果强度，已叠加 RestartCollapse 视觉抑制
        /// </summary>
        public float Intensity {
            get => intensityRaw * (1f - MathHelper.Clamp(RestartCollapse, 0f, 1f));
            set => intensityRaw = value;
        }

        /// <summary>
        /// 重启演出专用视觉抑制系数
        /// </summary>
        public float RestartCollapse { get; set; }

        /// <summary>
        /// 领域中心
        /// </summary>
        public Vector2 DomainCenter { get; private set; }

        //领域中心缓动剩余帧
        private int domainEaseTimer;

        //RAM 崩溃锁定计时
        private int crashLockoutTimer;

        public bool IsCrashLockedOut => crashLockoutTimer > 0;

        //每层独立展开进度
        internal readonly float[] layerExpand = new float[Cyberspace.MaxLayerCount];
        internal readonly int[] layerBurstTimer = new int[Cyberspace.MaxLayerCount];

        /// <summary>
        /// 当前领域层数
        /// </summary>
        public int CurrentLayer { get; internal set; }

        /// <summary>
        /// 收缩前的层数，用于在收缩动画期间继续渲染
        /// </summary>
        public int RenderLayerCount {
            get {
                int count = 0;
                for (int i = 0; i < Cyberspace.MaxLayerCount; i++) {
                    if (layerExpand[i] > 0.01f) count = i + 1;
                }
                return count;
            }
        }

        /// <summary>
        /// 当前最外层目标半径
        /// </summary>
        public float Radius {
            get {
                int rLayer = Math.Max(CurrentLayer, RenderLayerCount);
                return rLayer > 0 ? Cyberspace.GetLayerRadius(rLayer - 1) : Cyberspace.BaseRadius;
            }
        }

        /// <summary>
        /// 全局展开进度 = 有效外半径 / 目标外半径
        /// </summary>
        public float ExpandProgress {
            get {
                float r = Radius;
                if (r <= 0f) return 0f;
                return MathHelper.Clamp(EffectiveOuterRadius / r, 0f, 1f);
            }
        }

        /// <summary>
        /// 实际有效的最外层半径
        /// </summary>
        public float EffectiveOuterRadius {
            get {
                float maxR = 0f;
                for (int i = 0; i < Cyberspace.MaxLayerCount; i++) {
                    float r = Cyberspace.GetLayerRadius(i) * layerExpand[i];
                    if (r > maxR) maxR = r;
                }
                return maxR * (1f - MathHelper.Clamp(RestartCollapse, 0f, 1f));
            }
        }

        /// <summary>
        /// 着色器累计时间
        /// </summary>
        public float EffectTime { get; private set; }

        /// <summary>
        /// 玩家移动淡化系数
        /// </summary>
        public float MotionFade { get; private set; }

        private float targetIntensity;

        //环境故障闪电计时器
        private int ambientBoltTimer;

        //上次关闭前的层数
        private int lastLayer = 1;

        //同帧内防重入开关
        private long lastManualToggleFrame = -1;

        public float GetLayerExpand(int layerIndex) {
            layerIndex = Math.Clamp(layerIndex, 0, Cyberspace.MaxLayerCount - 1);
            return layerExpand[layerIndex];
        }

        /// <summary>
        /// 是否能在余量条件下维持指定层的最低秒数
        /// </summary>
        public bool CanAffordLayer(int layer) {
            if (HackTime.InfiniteHack) {
                return true;
            }
            if (layer < 1 || layer > Cyberspace.MaxLayerCount) {
                return false;
            }
            float required = Cyberspace.LayerRamDrainPerSecond[layer - 1] * Cyberspace.MinSustainSeconds;
            return RamSystem.CurrentRam >= required;
        }

        /// <summary>
        /// 同帧防重入的手动切换
        /// </summary>
        public bool Toggle() {
            long frame = (long)Main.GameUpdateCount;
            if (lastManualToggleFrame == frame) {
                return false;
            }
            lastManualToggleFrame = frame;

            if (Active) {
                Deactivate();
            }
            else {
                Activate();
            }
            return true;
        }

        /// <summary>
        /// 激活领域第一层（或恢复到上次层数）
        /// </summary>
        public void Activate() {
            //崩溃锁定期内拒绝
            if (crashLockoutTimer > 0) {
                if (!VaultUtils.isServer) {
                    SoundEngine.PlaySound(CWRSound.FailureCurrent with { Volume = 0.45f, Pitch = -0.4f }, Player.Center);
                }
                return;
            }

            int resumeLayer = Math.Clamp(lastLayer, 1, Cyberspace.MaxLayerCount);

            if (!HackTime.InfiniteHack) {
                while (resumeLayer >= 1 && !CanAffordLayer(resumeLayer)) {
                    resumeLayer--;
                }
                if (resumeLayer < 1) {
                    if (!VaultUtils.isServer) {
                        SoundEngine.PlaySound(CWRSound.FailureCurrent with { Volume = 0.45f, Pitch = -0.4f }, Player.Center);
                        RamSystem.NotifyInsufficient();
                        Color denyColor = new(255, 90, 80);
                        CombatText.NewText(Player.Hitbox, denyColor, "// LOW RAM", true);
                    }
                    return;
                }
            }

            for (int i = resumeLayer; i < Cyberspace.MaxLayerCount; i++) {
                layerBurstTimer[i] = 0;
            }
            Active = true;
            CurrentLayer = resumeLayer;
            targetIntensity = 1f;
            for (int i = 0; i < resumeLayer; i++) {
                layerBurstTimer[i] = Cyberspace.BurstDurations[i];
            }
            SpawnActivationVFX();

            if (!VaultUtils.isServer) {
                SoundEngine.PlaySound(CWRSound.FailureCurrent, Player.Center);
                SoundEngine.PlaySound(CWRSound.Faultrelease, Player.Center);
            }
        }

        /// <summary>
        /// 升降层
        /// </summary>
        public void SetLayer(int layer) {
            layer = Math.Clamp(layer, 1, Cyberspace.MaxLayerCount);
            if (!Active) return;
            if (layer == CurrentLayer) return;

            if (layer > CurrentLayer && !CanAffordLayer(layer)) {
                if (!VaultUtils.isServer) {
                    SoundEngine.PlaySound(CWRSound.FailureCurrent with { Volume = 0.4f, Pitch = -0.3f }, Player.Center);
                    RamSystem.NotifyInsufficient();
                    Color denyColor = new(255, 90, 80);
                    CombatText.NewText(Player.Hitbox, denyColor, $"// L{layer} - LOW RAM", true);
                }
                return;
            }

            int oldLayer = CurrentLayer;
            CurrentLayer = layer;

            if (layer > oldLayer) {
                for (int i = oldLayer; i < layer; i++) {
                    layerBurstTimer[i] = Cyberspace.BurstDurations[i];
                }
                SpawnLayerVFX(oldLayer, layer);
            }
        }

        /// <summary>
        /// 关闭领域，所有层带收缩动画
        /// </summary>
        public void Deactivate() {
            Active = false;
            targetIntensity = 0f;
            if (CurrentLayer > 0) {
                lastLayer = CurrentLayer;
            }
            CurrentLayer = 0;
            for (int i = 0; i < Cyberspace.MaxLayerCount; i++) {
                layerBurstTimer[i] = 0;
            }

            if (!VaultUtils.isServer && Player.whoAmI == Main.myPlayer) {
                SoundEngine.PlaySound(CWRSound.Faultrelease, Player.Center);
            }
        }

        /// <summary>
        /// RAM 耗尽触发的"系统崩溃"
        /// </summary>
        public void TriggerSystemCrash() {
            if (!Active && CurrentLayer == 0 && crashLockoutTimer > 0) {
                return;
            }

            Deactivate();
            crashLockoutTimer = Cyberspace.CrashLockoutFrames;

            if (!VaultUtils.isServer && Player.whoAmI == Main.myPlayer) {
                Color crashColor = new(255, 70, 70);
                CombatText.NewText(Player.Hitbox, crashColor, "// SYSTEM CRASH", true);
                SoundEngine.PlaySound(CWRSound.FailureCurrent with { Volume = 0.85f, Pitch = -0.3f }, Player.Center);
                SoundEngine.PlaySound(CWRSound.Faultrelease with { Volume = 0.7f, Pitch = -0.5f }, Player.Center);
            }
        }

        /// <summary>
        /// 主更新逻辑
        /// </summary>
        public void Update() {
            //仅手持 SHPC 时才允许维持领域；切换其它武器立即关闭，避免领域脱离 SHPC 上下文存在
            if (Active && Player.HeldItem.type != CWRID.Item_SHPC) {
                Deactivate();
            }

            if (crashLockoutTimer > 0) {
                crashLockoutTimer--;
            }

            UpdateDomainCenter();

            if (Active && CurrentLayer >= 1 && !HackTime.InfiniteHack) {
                float drain = Cyberspace.LayerRamDrainPerSecond[CurrentLayer - 1] * TimeGear.TimeScale;
                RamSystem.ConsumeOverTime(drain);
                if (RamSystem.CurrentRam <= 0f) {
                    TriggerSystemCrash();
                }
            }

            float dt = 1f / 60f;
            float timeSpeed = TimeGear.IsTimeSlowed ? MathHelper.Max(TimeGear.TimeScale, 0.1f) : 1f;
            EffectTime += dt * timeSpeed;

            UpdateMotionFade();

            float intensityLerp;
            if (Active && CurrentLayer > 0) {
                intensityLerp = 0.045f;
                if (layerBurstTimer[0] > 0) {
                    float burstFactor = (float)layerBurstTimer[0] / Cyberspace.BurstDurations[0];
                    intensityLerp = MathHelper.Lerp(0.08f, 0.25f, burstFactor);
                }
            }
            else {
                intensityLerp = 0.015f;
            }
            intensityRaw = MathHelper.Lerp(intensityRaw, targetIntensity, intensityLerp);

            for (int i = 0; i < Cyberspace.MaxLayerCount; i++) {
                float target = (i < CurrentLayer) ? 1f : 0f;
                int burstDur = Cyberspace.BurstDurations[i];

                if (layerBurstTimer[i] > 0) {
                    layerBurstTimer[i]--;
                    float burstFactor = (float)layerBurstTimer[i] / burstDur;
                    float burstLerpMin = MathHelper.Lerp(0.06f, 0.025f, (float)i / (Cyberspace.MaxLayerCount - 1));
                    float burstLerpMax = MathHelper.Lerp(0.22f, 0.10f, (float)i / (Cyberspace.MaxLayerCount - 1));
                    float expandLerp = MathHelper.Lerp(burstLerpMin, burstLerpMax, burstFactor);
                    layerExpand[i] = MathHelper.Lerp(layerExpand[i], target, expandLerp);
                }
                else {
                    float expandLerp = target > 0f ? Cyberspace.ExpandLerps[i] : Cyberspace.ContractLerps[i];
                    layerExpand[i] = MathHelper.Lerp(layerExpand[i], target, expandLerp);
                }

                if (target <= 0f && layerExpand[i] < 0.005f)
                    layerExpand[i] = 0f;
            }

            if (CurrentLayer >= 2 && intensityRaw > 0.5f && RestartCollapse < 0.2f) {
                ambientBoltTimer--;
                if (ambientBoltTimer <= 0) {
                    SpawnAmbientBolts();
                    ambientBoltTimer = 40 + Main.rand.Next(-8, 12);
                }
            }

            if (!Active && CurrentLayer == 0) {
                bool allCollapsed = true;
                for (int i = 0; i < Cyberspace.MaxLayerCount; i++) {
                    if (layerExpand[i] >= 0.005f) {
                        allCollapsed = false;
                        break;
                    }
                }
                if (allCollapsed && intensityRaw < 0.005f) {
                    intensityRaw = 0f;
                    EffectTime = 0f;
                    ambientBoltTimer = 0;
                }
            }
        }

        /// <summary>
        /// 立即重置全部状态
        /// </summary>
        public void Reset() {
            Active = false;
            intensityRaw = 0f;
            RestartCollapse = 0f;
            EffectTime = 0f;
            MotionFade = 0f;
            CurrentLayer = 0;
            lastLayer = 1;
            lastManualToggleFrame = -1;
            targetIntensity = 0f;
            ambientBoltTimer = 0;
            crashLockoutTimer = 0;
            DomainCenter = Vector2.Zero;
            domainEaseTimer = 0;
            for (int i = 0; i < Cyberspace.MaxLayerCount; i++) {
                layerExpand[i] = 0f;
                layerBurstTimer[i] = 0;
            }
        }

        /// <summary>
        /// 通知领域中心暂留在出发点
        /// </summary>
        public void NotifyTeleport(Vector2 anchorCenter) {
            DomainCenter = anchorCenter;
            domainEaseTimer = Cyberspace.DomainEaseTotal;
        }

        public bool IsInsideDomain(Vector2 worldPos) {
            if (Intensity < 0.01f) return false;
            float dx = worldPos.X - DomainCenter.X;
            float dy = worldPos.Y - DomainCenter.Y;
            return dx * dx + dy * dy <= EffectiveOuterRadius * EffectiveOuterRadius;
        }

        private void UpdateDomainCenter() {
            if (Player == null || !Player.active) {
                domainEaseTimer = 0;
                return;
            }

            Vector2 target = Player.Center;
            if (!Active && Intensity < 0.001f && domainEaseTimer == 0) {
                DomainCenter = target;
                return;
            }

            if (domainEaseTimer > 0) {
                float remain = (float)domainEaseTimer / Cyberspace.DomainEaseTotal;
                float prog = 1f - remain;
                float lerpRate = MathHelper.Lerp(0.06f, 0.25f, MathF.Pow(prog, 0.65f));
                DomainCenter = Vector2.Lerp(DomainCenter, target, lerpRate);
                domainEaseTimer--;
                if (domainEaseTimer == 0) {
                    DomainCenter = target;
                }
            }
            else {
                DomainCenter = target;
            }
        }

        private void UpdateMotionFade() {
            float target = 0f;
            if (Intensity > 0.001f) {
                if (Player != null && Player.active && !Player.dead) {
                    float speed = Player.velocity.Length();
                    target = MathHelper.Clamp(speed / Cyberspace.MotionFadeFullSpeed, 0f, 1f);
                }
            }

            float lerpRate = target > MotionFade ? 0.18f : 0.06f;
            MotionFade = MathHelper.Lerp(MotionFade, target, lerpRate);
            if (MotionFade < 0.001f) {
                MotionFade = 0f;
            }
        }

        private void SpawnActivationVFX() {
            if (Main.myPlayer != Player.whoAmI) return;

            IEntitySource source = Player.GetSource_FromThis();
            Vector2 center = Player.Center;

            Projectile.NewProjectile(source, center, Vector2.Zero,
                ModContent.ProjectileType<CyberShockwaveProj>(), 0, 0, Player.whoAmI);

            int boltCount = Main.rand.Next(6, 9);
            float baseAngle = Main.rand.NextFloat() * MathHelper.TwoPi;
            for (int i = 0; i < boltCount; i++) {
                float angle = baseAngle + MathHelper.TwoPi * i / boltCount
                    + Main.rand.NextFloat(-0.28f, 0.28f);
                int delay = Main.rand.Next(0, 5);
                Projectile.NewProjectile(source, center, Vector2.Zero,
                    ModContent.ProjectileType<CyberGlitchBoltProj>(), 0, 0, Player.whoAmI,
                    ai0: angle, ai1: delay);
            }
        }

        private void SpawnLayerVFX(int oldLayer, int newLayer) {
            if (Main.myPlayer != Player.whoAmI) return;

            IEntitySource source = Player.GetSource_FromThis();
            Vector2 center = Player.Center;

            Projectile.NewProjectile(source, center, Vector2.Zero,
                ModContent.ProjectileType<CyberShockwaveProj>(), 0, 0, Player.whoAmI);

            int boltCount = Main.rand.Next(4 + newLayer * 2, 7 + newLayer * 2);
            float baseAngle = Main.rand.NextFloat() * MathHelper.TwoPi;
            for (int i = 0; i < boltCount; i++) {
                float angle = baseAngle + MathHelper.TwoPi * i / boltCount
                    + Main.rand.NextFloat(-0.28f, 0.28f);
                int delay = Main.rand.Next(0, 4);
                Projectile.NewProjectile(source, center, Vector2.Zero,
                    ModContent.ProjectileType<CyberGlitchBoltProj>(), 0, 0, Player.whoAmI,
                    ai0: angle, ai1: delay);
            }
        }

        private void SpawnAmbientBolts() {
            if (Main.myPlayer != Player.whoAmI) return;
            if (Player == null || !Player.active) return;

            IEntitySource source = Player.GetSource_FromThis();
            float outerR = EffectiveOuterRadius;
            Vector2 center = DomainCenter;

            int count = Main.rand.Next(1, 1 + CurrentLayer);
            for (int i = 0; i < count; i++) {
                float angle = Main.rand.NextFloat() * MathHelper.TwoPi;
                float spawnDist = outerR * Main.rand.NextFloat(0.4f, 0.85f);
                Vector2 spawnPos = center + angle.ToRotationVector2() * spawnDist;
                int delay = Main.rand.Next(0, 6);
                Projectile.NewProjectile(source, spawnPos, Vector2.Zero,
                    ModContent.ProjectileType<CyberGlitchBoltProj>(), 0, 0, Player.whoAmI,
                    ai0: angle, ai1: delay);
            }
        }
    }
}
