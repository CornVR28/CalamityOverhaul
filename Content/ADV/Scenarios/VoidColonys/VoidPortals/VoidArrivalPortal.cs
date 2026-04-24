using InnoVault.PRT;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.VoidPortals
{
    /// <summary>
    /// 虚空抵达门，玩家进入虚空聚落时从此门被吐出
    /// 结构与 VoidPortal 完全独立，采用径向爆发式展开
    /// </summary>
    internal class VoidArrivalPortal : ModProjectile
    {
        //阶段
        public enum Phase
        {
            //能量汇聚与爆开
            Opening,
            //维持敞开，准备吐出
            Sustaining,
            //抛出瞬间（强闪光）
            Ejecting,
            //关闭收缩
            Closing,
            //完成
            Done,
        }

        #region 配置
        //门基础半径
        public const float BaseRadius = 260f;
        //打开过程帧数
        private const int OpeningDuration = 28;
        //抛出动画帧数
        private const int EjectingDuration = 22;
        //关闭过程帧数
        private const int ClosingDuration = 36;
        //开启过冲倍率
        private const float OpenOvershoot = 1.22f;
        //过冲回落帧数
        private const int OvershootDuration = 20;
        #endregion

        #region 运行时
        internal static VoidArrivalPortal ActiveInstance { get; private set; }

        /// <summary>当前活跃实例，带世界卸载引用清理</summary>
        internal static VoidArrivalPortal ValidateActiveInstance() {
            var inst = ActiveInstance;
            if (inst == null) return null;
            var proj = inst.Projectile;
            if (proj == null || !proj.active) {
                ActiveInstance = null;
                return null;
            }
            int idx = proj.whoAmI;
            if (idx < 0 || idx >= Main.maxProjectiles || Main.projectile[idx].ModProjectile != inst) {
                ActiveInstance = null;
                return null;
            }
            return inst;
        }

        public Phase CurrentPhase { get; private set; }
        private int phaseTimer;
        //维持时长由外部指定
        private int sustainDuration = 60;

        /// <summary>整体强度 0~1</summary>
        public float Intensity { get; private set; }
        private float targetIntensity;

        /// <summary>展开进度 0~1（可 overshoot 到 1.22）</summary>
        public float ExpandProgress { get; private set; }
        private float targetExpand;
        private int overshootTimer;

        /// <summary>抛出瞬间闪光 0~1</summary>
        public float EjectBurst { get; private set; }

        /// <summary>着色器时间累计（秒）</summary>
        public float EffectTime { get; private set; }

        /// <summary>三条冲击波时间线，<0 未激活</summary>
        public float ShockTime0 { get; private set; } = -1f;
        public float ShockTime1 { get; private set; } = -1f;
        public float ShockTime2 { get; private set; } = -1f;

        /// <summary>着色器种子</summary>
        public float Seed { get; private set; }

        public Vector2 Center => Projectile.Center;
        #endregion

        #region 公开API
        /// <summary>在指定世界坐标生成抵达门</summary>
        public static int Spawn(IEntitySource source, Vector2 worldPos, int sustainFrames = 60) {
            return Projectile.NewProjectile(source, worldPos, Vector2.Zero,
                ModContent.ProjectileType<VoidArrivalPortal>(), 0, 0, Main.myPlayer, sustainFrames);
        }

        /// <summary>开始抛出阶段</summary>
        public void BeginEject() {
            if (CurrentPhase == Phase.Opening || CurrentPhase == Phase.Sustaining) {
                CurrentPhase = Phase.Ejecting;
                phaseTimer = 0;
                ShockTime2 = 0f;
                EjectBurst = 1f;
                if (!Main.dedServ) SpawnEjectParticles();
            }
        }

        /// <summary>开始关闭阶段</summary>
        public void BeginClose() {
            if (CurrentPhase == Phase.Closing || CurrentPhase == Phase.Done) return;
            CurrentPhase = Phase.Closing;
            phaseTimer = 0;
            targetIntensity = 0f;
            targetExpand = 0f;
            overshootTimer = 0;
        }
        #endregion

        public override string Texture => CWRConstant.Placeholder;

        public override void Unload() => ActiveInstance = null;

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
            if (sustainDuration <= 0) sustainDuration = 60;
            Seed = Main.rand.NextFloat(0f, 100f);

            if (ActiveInstance != null && ActiveInstance != this && ActiveInstance.Projectile.active) {
                ActiveInstance.Projectile.Kill();
            }
            ActiveInstance = this;

            CurrentPhase = Phase.Opening;
            phaseTimer = 0;
            Intensity = 0f;
            ExpandProgress = 0f;
            EjectBurst = 0f;
            targetIntensity = 1f;
            targetExpand = OpenOvershoot;
            overshootTimer = OvershootDuration;
            //开启与能量汇聚冲击波
            ShockTime0 = 0f;
            ShockTime1 = 0f;

            if (!Main.dedServ) SpawnOpeningParticles();
        }

        public override void AI() {
            if (ActiveInstance == null || !ActiveInstance.Projectile.active) {
                ActiveInstance = this;
            }

            EffectTime += 1f / 60f;
            phaseTimer++;

            //冲击波推进
            if (ShockTime0 >= 0f) { ShockTime0 += 1f / 60f; if (ShockTime0 > 2.5f) ShockTime0 = -1f; }
            if (ShockTime1 >= 0f) { ShockTime1 += 1f / 60f; if (ShockTime1 > 2.8f) ShockTime1 = -1f; }
            if (ShockTime2 >= 0f) { ShockTime2 += 1f / 60f; if (ShockTime2 > 2.0f) ShockTime2 = -1f; }

            //过冲回落
            if (overshootTimer > 0) {
                overshootTimer--;
                if (overshootTimer == 0 && CurrentPhase != Phase.Closing) {
                    targetExpand = 1f;
                }
            }

            //Intensity lerp
            float intensityLerp = CurrentPhase == Phase.Closing ? 0.025f : 0.12f;
            Intensity = MathHelper.Lerp(Intensity, targetIntensity, intensityLerp);

            //ExpandProgress lerp
            float expandLerp;
            if (CurrentPhase == Phase.Opening) {
                float op = 1f - (float)phaseTimer / OpeningDuration;
                expandLerp = MathHelper.Lerp(0.06f, 0.32f, MathHelper.Clamp(op, 0f, 1f));
            }
            else if (CurrentPhase == Phase.Closing) {
                expandLerp = 0.055f;
            }
            else {
                expandLerp = 0.05f;
            }
            ExpandProgress = MathHelper.Lerp(ExpandProgress, targetExpand, expandLerp);
            if (targetExpand <= 0f && ExpandProgress < 0.006f) ExpandProgress = 0f;

            //抛出闪光衰减
            if (EjectBurst > 0f) {
                float dec = CurrentPhase == Phase.Ejecting ? 0.025f : 0.06f;
                EjectBurst = MathHelper.Max(0f, EjectBurst - dec);
            }

            //阶段切换
            switch (CurrentPhase) {
                case Phase.Opening:
                    if (phaseTimer >= OpeningDuration && overshootTimer <= 0 && ExpandProgress > 0.85f) {
                        CurrentPhase = Phase.Sustaining;
                        phaseTimer = 0;
                    }
                    break;
                case Phase.Sustaining:
                    //微脉动
                    targetExpand = 1f + MathF.Sin(EffectTime * 1.4f) * 0.02f;
                    if (phaseTimer >= sustainDuration) {
                        //默认自然进入抛出（若外部未主动调用）
                        BeginEject();
                    }
                    break;
                case Phase.Ejecting:
                    if (phaseTimer >= EjectingDuration) {
                        BeginClose();
                    }
                    break;
                case Phase.Closing:
                    if (phaseTimer >= ClosingDuration && ExpandProgress < 0.008f && Intensity < 0.01f) {
                        CurrentPhase = Phase.Done;
                        Projectile.Kill();
                        return;
                    }
                    break;
            }

            if (!Main.dedServ) {
                ApplyScreenShake();
                SpawnRuntimeParticles();
            }
        }

        public override void OnKill(int timeLeft) {
            if (ActiveInstance == this) ActiveInstance = null;
        }

        #region 视觉效果
        private void ApplyScreenShake() {
            float s = 0f;
            if (CurrentPhase == Phase.Opening) {
                float t = 1f - (float)phaseTimer / OpeningDuration;
                s += t * 7f;
            }
            else if (CurrentPhase == Phase.Ejecting) {
                float t = 1f - (float)phaseTimer / EjectingDuration;
                s += t * 10f;
            }
            //冲击波余震
            if (ShockTime0 >= 0f && ShockTime0 < 0.25f)
                s += 6f * MathF.Exp(-ShockTime0 * 18f);
            if (ShockTime2 >= 0f && ShockTime2 < 0.25f)
                s += 10f * MathF.Exp(-ShockTime2 * 18f);

            if (s > 0.1f)
                Main.screenPosition += Main.rand.NextVector2Circular(s, s);
        }

        private void SpawnOpeningParticles() {
            //放射状火花环，数量较多
            const int sparks = 72;
            for (int i = 0; i < sparks; i++) {
                float a = MathHelper.TwoPi * i / sparks + Main.rand.NextFloat(-0.04f, 0.04f);
                Vector2 v = a.ToRotationVector2() * Main.rand.NextFloat(5f, 13f);
                Color c = Color.Lerp(new Color(255, 210, 120), new Color(230, 50, 20), Main.rand.NextFloat());
                PRTLoader.AddParticle(new PRT_VoidSpark(Center, v, c, Main.rand.NextFloat(0.6f, 1.2f)));
            }
            //电弧
            for (int i = 0; i < 18; i++) {
                float a = MathHelper.TwoPi * i / 18f + Main.rand.NextFloat(-0.18f, 0.18f);
                Vector2 v = a.ToRotationVector2() * Main.rand.NextFloat(3f, 7f);
                PRTLoader.AddParticle(new PRT_VoidArc(Center, v, Main.rand.NextFloat(1.4f, 2.4f)));
            }
        }

        private void SpawnEjectParticles() {
            //极猛的单次爆裂
            const int sparks = 140;
            for (int i = 0; i < sparks; i++) {
                float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Vector2 v = a.ToRotationVector2() * Main.rand.NextFloat(8f, 22f);
                Color c = Color.Lerp(new Color(255, 230, 150), new Color(240, 60, 25), Main.rand.NextFloat());
                PRTLoader.AddParticle(new PRT_VoidSpark(Center, v, c, Main.rand.NextFloat(0.7f, 1.6f)));
            }
            for (int i = 0; i < 28; i++) {
                float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Vector2 v = a.ToRotationVector2() * Main.rand.NextFloat(4f, 11f);
                PRTLoader.AddParticle(new PRT_VoidArc(Center, v, Main.rand.NextFloat(1.8f, 3.2f)));
            }
        }

        private void SpawnRuntimeParticles() {
            if (ExpandProgress < 0.1f || Intensity < 0.1f) return;

            float r = BaseRadius * ExpandProgress;
            //每帧少量环形火花
            int count = CurrentPhase == Phase.Sustaining ? 3 : 6;
            for (int i = 0; i < count; i++) {
                float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Vector2 dir = a.ToRotationVector2();
                Vector2 pos = Center + dir * r * Main.rand.NextFloat(0.85f, 1.05f);
                Vector2 v = dir * Main.rand.NextFloat(2f, 5f);
                Color c = Color.Lerp(new Color(255, 170, 80), new Color(220, 35, 15), Main.rand.NextFloat());
                PRTLoader.AddParticle(new PRT_VoidSpark(pos, v, c, Main.rand.NextFloat(0.3f, 0.75f)));
            }

            //偶尔一条电弧
            if (Main.rand.NextBool(3)) {
                float a = Main.rand.NextFloat(0f, MathHelper.TwoPi);
                Vector2 dir = a.ToRotationVector2();
                Vector2 pos = Center + dir * r * Main.rand.NextFloat(0.7f, 1.0f);
                Vector2 v = dir * Main.rand.NextFloat(1f, 3f);
                PRTLoader.AddParticle(new PRT_VoidArc(pos, v, Main.rand.NextFloat(0.9f, 1.6f)));
            }
        }
        #endregion
    }
}
