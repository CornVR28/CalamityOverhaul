using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.VoidPortals;
using InnoVault.PRT;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.VoidPortals
{
    internal class VoidPortal : ModProjectile
    {
        /// <summary>
        /// 传送门阶段
        /// </summary>
        public enum Phase
        {
            /// <summary>展开中</summary>
            Opening,
            /// <summary>维持中</summary>
            Sustaining,
            /// <summary>关闭中</summary>
            Closing,
            /// <summary>已完成</summary>
            Done
        }

        #region 配置

        /// <summary>裂隙半高（世界像素）</summary>
        public const float BaseRiftHalfHeight = 420f;
        /// <summary>裂隙最大半宽（世界像素）</summary>
        public const float BaseRiftMaxWidth = 320f;
        /// <summary>背景压暗强度</summary>
        public const float BaseDimStrength = 0.85f;
        /// <summary>能量辉光强度</summary>
        public const float BaseEnergyPower = 1.5f;

        // 展开burst持续帧数
        private const int OpenBurstDuration = 22;
        // 展开lerp速率（稳定阶段）
        private const float OpenExpandLerp = 0.04f;
        // 展开burst阶段lerp范围（更猛的初始爆发）
        private const float OpenBurstLerpMin = 0.08f;
        private const float OpenBurstLerpMax = 0.42f;
        // 收缩lerp速率
        private const float CloseContractLerp = 0.042f;
        // Intensity过渡速率
        private const float IntensityOpenLerp = 0.06f;
        private const float IntensityCloseLerp = 0.018f;
        // 展开overshoot峰值（>1产生过冲回弹）
        private const float OpenOvershoot = 1.18f;
        // overshoot持续帧（之后回到1.0维持）
        private const int OvershootDuration = 32;
        // 冲击波生命周期（秒，与shader中阈值2.4保持一致）
        private const float ShockwaveLifetime = 2.4f;

        #endregion

        #region 运行时状态

        /// <summary>当前活跃的传送门实例（供渲染器读取）</summary>
        internal static VoidPortal ActiveInstance { get; private set; }

        /// <summary>
        /// 验证并返回有效的活跃实例，清理过期引用（世界卸载后弹幕引用会残留）
        /// </summary>
        internal static VoidPortal ValidateActiveInstance() {
            var inst = ActiveInstance;
            if (inst == null) return null;

            var proj = inst.Projectile;
            if (proj == null || !proj.active) {
                ActiveInstance = null;
                return null;
            }

            // 验证弹幕仍在主数组中（防止世界卸载后引用残留）
            int idx = proj.whoAmI;
            if (idx < 0 || idx >= Main.maxProjectiles || Main.projectile[idx].ModProjectile != inst) {
                ActiveInstance = null;
                return null;
            }

            return inst;
        }

        /// <summary>当前阶段</summary>
        public Phase CurrentPhase { get; private set; }
        /// <summary>当前阶段内的计时器（帧）</summary>
        private int phaseTimer;
        /// <summary>维持阶段时长（帧）</summary>
        private int sustainDuration = 300;

        /// <summary>全局效果强度 0-1（最后淡出，确保收缩动画可见）</summary>
        public float Intensity { get; private set; }
        private float targetIntensity;

        /// <summary>裂隙展开进度 0-1（lerp驱动，带burst）</summary>
        public float ExpandProgress { get; private set; }
        private float targetExpand;
        private int burstTimer;

        /// <summary>着色器专用累计时间（秒）</summary>
        public float EffectTime { get; private set; }

        /// <summary>裂缝随机种子</summary>
        public float CrackSeed { get; private set; }

        /// <summary>当前冲击波累计时间（秒），<0表示未激活</summary>
        public float ShockwaveTime { get; private set; } = -1f;
        /// <summary>overshoot剩余帧数</summary>
        private int overshootTimer;

        /// <summary>传送门世界坐标中心</summary>
        public Vector2 PortalCenter => Projectile.Center;

        #endregion

        #region 公开API

        /// <summary>
        /// 在世界中生成一个虚空传送门
        /// </summary>
        public static int Spawn(IEntitySource source, Vector2 worldPosition, int sustainDuration = 300) {
            return Projectile.NewProjectile(
                source, worldPosition, Vector2.Zero,
                ModContent.ProjectileType<VoidPortal>(), 0, 0,
                Main.myPlayer, sustainDuration);
        }

        /// <summary>
        /// 手动触发关闭
        /// </summary>
        public void Close() {
            if (CurrentPhase == Phase.Opening || CurrentPhase == Phase.Sustaining) {
                BeginClosing();
            }
        }

        #endregion

        public override string Texture => CWRConstant.Placeholder;

        public override void Unload() {
            ActiveInstance = null;
        }

        public override void SetDefaults() {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = int.MaxValue;
            Projectile.penetrate = -1;
            Projectile.hide = true;
        }

        public override void OnSpawn(IEntitySource source) {
            sustainDuration = (int)Projectile.ai[0];
            if (sustainDuration <= 0) sustainDuration = 300;
            CrackSeed = Main.rand.NextFloat(0f, 100f);

            //清理旧实例避免竞争
            if (ActiveInstance != null && ActiveInstance != this && ActiveInstance.Projectile.active) {
                ActiveInstance.Projectile.Kill();
            }
            ActiveInstance = this;

            //初始化：展开burst（带overshoot）
            CurrentPhase = Phase.Opening;
            phaseTimer = 0;
            targetIntensity = 1f;
            targetExpand = OpenOvershoot;
            Intensity = 0f;
            ExpandProgress = 0f;
            burstTimer = OpenBurstDuration;
            overshootTimer = OvershootDuration;
            //触发开启冲击波
            ShockwaveTime = 0f;

            //开启瞬间一次性环形爆发粒子
            if (!Main.dedServ) {
                SpawnOpeningBurstParticles();
            }
        }

        /// <summary>
        /// 传送门撕开瞬间的环形粒子爆发，用于强化打开冲击感
        /// </summary>
        private void SpawnOpeningBurstParticles() {
            //放射状火花环
            int sparkCount = 48;
            for (int i = 0; i < sparkCount; i++) {
                float ang = MathHelper.TwoPi * i / sparkCount + Main.rand.NextFloat(-0.05f, 0.05f);
                float speed = Main.rand.NextFloat(6f, 14f);
                Vector2 vel = ang.ToRotationVector2() * speed;
                //垂直方向更快（裂隙力线方向）
                vel.Y *= 1.4f;

                Color c = Color.Lerp(new Color(255, 200, 110), new Color(220, 40, 18), Main.rand.NextFloat());
                float scale = Main.rand.NextFloat(0.5f, 1.1f);
                PRTLoader.AddParticle(new PRT_VoidSpark(PortalCenter, vel, c, scale));
            }

            //电弧
            for (int i = 0; i < 14; i++) {
                float ang = MathHelper.TwoPi * i / 14f + Main.rand.NextFloat(-0.15f, 0.15f);
                Vector2 vel = ang.ToRotationVector2() * Main.rand.NextFloat(3f, 7f);
                PRTLoader.AddParticle(new PRT_VoidArc(PortalCenter, vel, Main.rand.NextFloat(1.2f, 2.2f)));
            }
        }

        public override void AI() {
            if (ActiveInstance == null || !ActiveInstance.Projectile.active) {
                ActiveInstance = this;
            }

            EffectTime += 1f / 60f;
            phaseTimer++;

            //冲击波时间推进
            if (ShockwaveTime >= 0f) {
                ShockwaveTime += 1f / 60f;
                if (ShockwaveTime > ShockwaveLifetime) {
                    ShockwaveTime = -1f;
                }
            }

            //overshoot回收：到时间后将目标从1.18拉回1.0
            if (overshootTimer > 0) {
                overshootTimer--;
                if (overshootTimer == 0 && CurrentPhase != Phase.Closing) {
                    targetExpand = 1f;
                }
            }

            // ================================================================
            // Intensity 过渡（参考 Cyberspace.Update 的模式）
            // ================================================================
            float intensityLerp;
            if (CurrentPhase == Phase.Opening || CurrentPhase == Phase.Sustaining) {
                intensityLerp = IntensityOpenLerp;
                if (burstTimer > 0) {
                    float burstFactor = (float)burstTimer / OpenBurstDuration;
                    intensityLerp = MathHelper.Lerp(0.08f, 0.3f, burstFactor);
                }
            }
            else {
                // 关闭阶段：Intensity最后消失，速率极慢
                intensityLerp = IntensityCloseLerp;
            }
            Intensity = MathHelper.Lerp(Intensity, targetIntensity, intensityLerp);

            // ================================================================
            // ExpandProgress 过渡（lerp + burst 机制）
            // ================================================================
            if (burstTimer > 0) {
                burstTimer--;
                float burstFactor = (float)burstTimer / OpenBurstDuration;
                float expandLerp = MathHelper.Lerp(OpenBurstLerpMin, OpenBurstLerpMax, burstFactor);
                ExpandProgress = MathHelper.Lerp(ExpandProgress, targetExpand, expandLerp);
            }
            else {
                float expandLerp = targetExpand > 0f ? OpenExpandLerp : CloseContractLerp;
                ExpandProgress = MathHelper.Lerp(ExpandProgress, targetExpand, expandLerp);
            }

            // 阈值钳位
            if (targetExpand <= 0f && ExpandProgress < 0.005f) {
                ExpandProgress = 0f;
            }

            // ================================================================
            // 阶段转换逻辑
            // ================================================================
            switch (CurrentPhase) {
                case Phase.Opening:
                    // 展开完毕（overshoot结束并回落到接近1时切换为维持）
                    if (overshootTimer <= 0 && ExpandProgress > 0.95f && ExpandProgress < 1.05f && burstTimer <= 0) {
                        CurrentPhase = Phase.Sustaining;
                        phaseTimer = 0;
                    }
                    break;

                case Phase.Sustaining:
                    // 维持阶段微脉动
                    targetExpand = 1f + MathF.Sin(EffectTime * 1.2f) * 0.015f;
                    if (phaseTimer >= sustainDuration) {
                        BeginClosing();
                    }
                    break;

                case Phase.Closing:
                    // 所有收缩完毕且Intensity足够低后结束
                    if (ExpandProgress < 0.005f && Intensity < 0.008f) {
                        CurrentPhase = Phase.Done;
                        Projectile.Kill();
                        return;
                    }
                    break;
            }

            // ================================================================
            // 客户端视觉
            // ================================================================
            if (!Main.dedServ) {
                ApplyScreenShake();
                SpawnParticles();
            }
        }

        private void BeginClosing() {
            CurrentPhase = Phase.Closing;
            phaseTimer = 0;
            targetIntensity = 0f;
            targetExpand = 0f;
            // 关闭时短暂burst（挣扎感）
            burstTimer = 6;
            overshootTimer = 0;
            //关闭瞬间触发反向冲击波
            ShockwaveTime = 0f;
        }

        public override void OnKill(int timeLeft) {
            if (ActiveInstance == this) {
                ActiveInstance = null;
            }
        }

        #region 视觉效果

        private void ApplyScreenShake() {
            float shakeIntensity = 0f;

            if (CurrentPhase == Phase.Opening && burstTimer > 0) {
                //初始爆发：更强
                float burstRatio = (float)burstTimer / OpenBurstDuration;
                shakeIntensity = burstRatio * 6f;
            }
            else if (CurrentPhase == Phase.Opening && overshootTimer > 0) {
                //overshoot回弹震动
                float t = (float)overshootTimer / OvershootDuration;
                shakeIntensity = t * 2.2f;
            }
            else if (CurrentPhase == Phase.Closing && burstTimer > 0) {
                shakeIntensity = 2.4f * (burstTimer / 6f);
            }

            //冲击波余震：前0.2s极猛震动（指数衰减），与shader爆发同步
            if (ShockwaveTime >= 0f && ShockwaveTime < 0.25f) {
                shakeIntensity += 8f * MathF.Exp(-ShockwaveTime * 18f);
            }

            if (shakeIntensity > 0.1f) {
                Main.screenPosition += Main.rand.NextVector2Circular(shakeIntensity, shakeIntensity);
            }
        }

        private void SpawnParticles() {
            if (ExpandProgress < 0.08f || Intensity < 0.05f) return;

            float spawnRate;
            if (CurrentPhase == Phase.Opening) {
                // burst阶段粒子爆发
                spawnRate = burstTimer > 0 ? 5f : MathHelper.Lerp(1f, 3f, ExpandProgress);
            }
            else if (CurrentPhase == Phase.Sustaining) {
                spawnRate = 1.5f;
            }
            else {
                spawnRate = MathHelper.Lerp(3f, 0.3f, 1f - ExpandProgress);
            }

            float worldHalfH = BaseRiftHalfHeight * ExpandProgress;
            float worldHalfW = BaseRiftMaxWidth * ExpandProgress;

            //=== 火花粒子 ===
            int sparkCount = (int)(spawnRate * 2.5f);
            for (int i = 0; i < sparkCount; i++) {
                float yOff = Main.rand.NextFloat(-worldHalfH, worldHalfH) * 0.85f;
                float xSign = Main.rand.NextBool() ? 1f : -1f;
                float xOff = worldHalfW * xSign * Main.rand.NextFloat(0.3f, 0.6f);

                Vector2 spawnPos = PortalCenter + new Vector2(xOff, yOff);
                Vector2 vel = new Vector2(
                    xSign * Main.rand.NextFloat(2.5f, 7f),
                    Main.rand.NextFloat(-3.5f, -0.5f));

                Color sparkColor = Color.Lerp(
                    new Color(255, 160, 60),
                    new Color(210, 35, 12),
                    Main.rand.NextFloat());
                float sparkScale = Main.rand.NextFloat(0.25f, 0.65f);

                PRTLoader.AddParticle(new PRT_VoidSpark(spawnPos, vel, sparkColor, sparkScale));
            }

            //=== 电弧粒子 ===
            if (Main.rand.NextFloat() < spawnRate * 0.3f) {
                float yOff = Main.rand.NextFloat(-worldHalfH, worldHalfH) * 0.7f;
                float xSign = Main.rand.NextBool() ? 1f : -1f;
                float xOff = worldHalfW * xSign * Main.rand.NextFloat(0.2f, 0.5f);

                Vector2 spawnPos = PortalCenter + new Vector2(xOff, yOff);
                Vector2 vel = new Vector2(
                    xSign * Main.rand.NextFloat(1f, 4f),
                    Main.rand.NextFloat(-2f, 2f));

                float arcScale = Main.rand.NextFloat(0.6f, 1.4f);
                PRTLoader.AddParticle(new PRT_VoidArc(spawnPos, vel, arcScale));
            }
        }

        #endregion
    }
}

