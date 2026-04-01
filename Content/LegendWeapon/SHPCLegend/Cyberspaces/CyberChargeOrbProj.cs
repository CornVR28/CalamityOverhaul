using CalamityOverhaul.Common;
using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces
{
    /// <summary>
    /// 赛博蓄力能量球弹幕
    /// <br/>右键持续蓄力，释放后直线高速飞行，命中后爆炸
    /// <br/>蓄力阶段：固定在玩家前方，由小变大，黄金色→白青色过渡
    /// <br/>飞行阶段：直线高速飞行，拖尾方形粒子
    /// <br/>命中阶段：生成 CyberDetonationProj 爆破特效
    /// </summary>
    internal class CyberChargeOrbProj : ModProjectile, IAdditiveDrawable
    {
        public override string Texture => CWRConstant.Placeholder;

        #region 常量

        /// <summary>满蓄所需帧数（2秒 × 60帧）</summary>
        private const int MaxChargeFrames = 120;
        /// <summary>最低蓄力帧数（低于此释放视为取消）</summary>
        private const int MinChargeFrames = 30;
        /// <summary>飞行速度</summary>
        private const float FlySpeed = 22f;
        /// <summary>蓄力完成时球体视觉直径（像素）</summary>
        private const float MaxOrbDiameter = 100f;
        /// <summary>蓄力阶段汇聚粒子生成间隔</summary>
        private const int ConvergeParticleInterval = 4;
        /// <summary>飞行阶段拖尾粒子间隔</summary>
        private const int TrailParticleInterval = 2;
        /// <summary>球体离玩家中心的前方偏移距离</summary>
        private const float ChargeOffsetDist = 70f;

        #endregion

        #region 状态枚举

        private enum OrbState
        {
            Charging = 0,
            Flying = 1,
        }

        #endregion

        #region 颜色

        // 蓄力颜色（黄金色系）
        private static readonly Color ChargeCore = new(255, 220, 80);
        private static readonly Color ChargeGlow = new(230, 170, 30);
        private static readonly Color ChargeAura = new(150, 100, 15);

        // 满蓄/飞行颜色（白青色系）
        private static readonly Color FullCore = new(220, 255, 255);
        private static readonly Color FullGlow = new(80, 230, 220);
        private static readonly Color FullAura = new(20, 140, 130);

        #endregion

        #region 实例字段

        private int chargeTime;
        private float chargeRatio; // 0~1
        private float fadeAlpha;
        private int particleTimer;

        private OrbState State {
            get => (OrbState)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }

        /// <summary>
        /// 关联的手持弹幕索引（ai[1]），用于蓄力阶段定位枪口
        /// </summary>
        private int HeldProjIndex {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        #endregion

        public override void SetDefaults() {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI() {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead) {
                Projectile.Kill();
                return;
            }

            switch (State) {
                case OrbState.Charging:
                    AI_Charging(owner);
                    break;
                case OrbState.Flying:
                    AI_Flying();
                    break;
            }
        }

        #region 蓄力阶段

        private void AI_Charging(Player owner) {
            // 尝试从手持弹幕获取枪口位置
            Vector2 targetPos;
            int heldIdx = HeldProjIndex;
            if (heldIdx >= 0 && heldIdx < Main.maxProjectiles
                && Main.projectile[heldIdx].active
                && Main.projectile[heldIdx].ModProjectile is SHPCChargeHeldProj heldProj) {
                targetPos = heldProj.TipPosition;
            }
            else {
                // 后备：直接用玩家前方偏移
                Vector2 fallbackDir = (Main.MouseWorld - owner.Center).SafeNormalize(Vector2.UnitX);
                targetPos = owner.Center + fallbackDir * ChargeOffsetDist;
            }

            Projectile.Center = targetPos;
            Vector2 aimDir = (Main.MouseWorld - owner.Center).SafeNormalize(Vector2.UnitX);
            Projectile.rotation = aimDir.ToRotation();

            // 玩家面向光球方向
            owner.ChangeDir(aimDir.X > 0f ? 1 : -1);

            // 持续蓄力
            chargeTime++;
            chargeRatio = MathHelper.Clamp((float)chargeTime / MaxChargeFrames, 0f, 1f);

            // 蓄力阶段不移动
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 600; // 重置timeLeft防止蓄力超时消失

            // 淡入
            fadeAlpha = MathHelper.Clamp(chargeTime / 15f, 0f, 1f);

            // 光照
            Color currentCore = Color.Lerp(ChargeCore, FullCore, chargeRatio);
            Lighting.AddLight(Projectile.Center, currentCore.ToVector3() * 0.5f * fadeAlpha * (0.3f + chargeRatio * 0.7f));

            // 汇聚粒子
            particleTimer++;
            if (particleTimer >= ConvergeParticleInterval && Main.netMode != NetmodeID.Server) {
                particleTimer = 0;
                SpawnConvergeParticles();
            }

            // 满蓄脉冲提示
            if (chargeRatio >= 1f && chargeTime % 20 == 0) {
                // 每20帧一个小脉冲粒子爆发
                if (Main.netMode != NetmodeID.Server) {
                    for (int i = 0; i < 4; i++) {
                        Vector2 vel = Main.rand.NextVector2CircularEdge(3f, 3f);
                        PRTLoader.AddParticle(new PRT_CyberSquare(
                            Projectile.Center, vel,
                            FullCore, FullGlow,
                            Main.rand.NextFloat(0.6f, 1.0f), Main.rand.Next(15, 25)
                        ));
                    }
                }
            }

            // 检测右键释放 → 发射
            if (!owner.PressKey(false)) {
                if (chargeTime >= MinChargeFrames) {
                    LaunchOrb(owner);
                }
                else {
                    // 蓄力不足，取消
                    Projectile.Kill();
                }
            }
        }

        private void SpawnConvergeParticles() {
            // 从周围随机方向汇聚到球体中心
            float spawnRadius = 80f + (1f - chargeRatio) * 120f; // 蓄力越满，粒子从越近处生成
            int count = 1 + (int)(chargeRatio * 2f); // 蓄力越满粒子越多

            Color mainCol = Color.Lerp(ChargeCore, FullCore, chargeRatio);
            Color edgeCol = Color.Lerp(ChargeGlow, FullGlow, chargeRatio);

            for (int i = 0; i < count; i++) {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 offset = angle.ToRotationVector2() * Main.rand.NextFloat(spawnRadius * 0.6f, spawnRadius);
                Vector2 spawnPos = Projectile.Center + offset;

                PRTLoader.AddParticle(new PRT_CyberConverge(
                    spawnPos, Projectile.Center,
                    mainCol, edgeCol,
                    Main.rand.NextFloat(0.5f, 1.0f),
                    Main.rand.Next(18, 35),
                    chargeRatio
                ));
            }
        }

        private void LaunchOrb(Player owner) {
            // 释放时销毁手持弹幕
            KillHeldProj();

            State = OrbState.Flying;
            Vector2 aimDir = (Main.MouseWorld - owner.Center).SafeNormalize(Vector2.UnitX);
            Projectile.velocity = aimDir * FlySpeed;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 300; // 飞行最多5秒

            // 发射时粒子爆发
            if (Main.netMode != NetmodeID.Server) {
                for (int i = 0; i < 12; i++) {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(5f, 5f);
                    PRTLoader.AddParticle(new PRT_CyberSquare(
                        Projectile.Center, vel + Projectile.velocity * 0.3f,
                        FullCore, FullGlow,
                        Main.rand.NextFloat(0.8f, 1.5f), Main.rand.Next(20, 35)
                    ));
                }
            }

            SoundEngine.PlaySound(SoundID.Item92, Projectile.Center);
            Projectile.netUpdate = true;
        }

        #endregion

        #region 飞行阶段

        private void AI_Flying() {
            Projectile.rotation = Projectile.velocity.ToRotation();
            fadeAlpha = 1f;

            // 飞行发光
            Color flyCore = Color.Lerp(ChargeCore, FullCore, chargeRatio);
            Lighting.AddLight(Projectile.Center, flyCore.ToVector3() * 0.7f);

            // 拖尾粒子
            particleTimer++;
            if (particleTimer >= TrailParticleInterval && Main.netMode != NetmodeID.Server) {
                particleTimer = 0;
                SpawnTrailParticles();
            }
        }

        private void SpawnTrailParticles() {
            Color mainCol = Color.Lerp(ChargeCore, FullCore, chargeRatio);
            Color edgeCol = Color.Lerp(ChargeGlow, FullGlow, chargeRatio);

            Vector2 perpDir = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 3; i++) {
                Vector2 offset = perpDir * Main.rand.NextFloat(-10f, 10f);
                Vector2 vel = -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 5f)
                    + perpDir * Main.rand.NextFloat(-2f, 2f);
                PRTLoader.AddParticle(new PRT_CyberSquare(
                    Projectile.Center + offset, vel,
                    mainCol, edgeCol,
                    Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(15, 30)
                ));
            }
        }

        #endregion

        #region 命中与爆炸

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            SpawnDetonation();
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            SpawnDetonation();
            return true; // Kill projectile
        }

        public override void OnKill(int timeLeft) {
            // 确保手持弹幕被清理
            KillHeldProj();
            // 消散粒子
            if (Main.netMode == NetmodeID.Server) return;
            Color mainCol = Color.Lerp(ChargeCore, FullCore, chargeRatio);
            Color edgeCol = Color.Lerp(ChargeGlow, FullGlow, chargeRatio);
            for (int i = 0; i < 16; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(6f, 6f);
                PRTLoader.AddParticle(new PRT_CyberSquare(
                    Projectile.Center, vel,
                    mainCol, edgeCol,
                    Main.rand.NextFloat(0.8f, 1.8f), Main.rand.Next(25, 45)
                ));
            }
        }

        private void SpawnDetonation() {
            if (Projectile.owner != Main.myPlayer) return;
            // 生成爆破弹幕，传递蓄力比例和伤害
            int damage = (int)(Projectile.damage * (0.5f + chargeRatio * 0.5f)); // 蓄力越满伤害越高
            int projIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<CyberDetonationProj>(),
                damage, Projectile.knockBack,
                Projectile.owner,
                ai0: chargeRatio // ai[0] = 蓄力比例，影响爆炸规模
            );
            if (projIndex >= 0 && projIndex < Main.maxProjectiles) {
                Main.projectile[projIndex].originalDamage = Projectile.originalDamage;
            }
        }

        #endregion

        #region 绘制

        public override bool PreDraw(ref Color lightColor) => false;

        void IAdditiveDrawable.DrawAdditiveAfterNon(SpriteBatch spriteBatch) {
            if (fadeAlpha < 0.01f) return;

            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow == null) return;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // 根据蓄力比例计算球体大小
            float sizeRatio = State == OrbState.Charging
                ? 0.2f + chargeRatio * 0.8f
                : 1f;
            float orbDiameterPx = MaxOrbDiameter * sizeRatio;

            // 当前颜色（黄金→白青过渡）
            Color currentCore = Color.Lerp(ChargeCore, FullCore, chargeRatio);
            Color currentGlow = Color.Lerp(ChargeGlow, FullGlow, chargeRatio);
            Color currentAura = Color.Lerp(ChargeAura, FullAura, chargeRatio);

            float pulse = 0.92f + 0.08f * MathF.Sin((float)Main.timeForVisualEffects * 0.12f + chargeRatio * 5f);
            float alpha = fadeAlpha * pulse;
            Vector2 glowOrigin = glow.Size() * 0.5f;

            // 外层柔和bloom
            float outerScale = (orbDiameterPx / glow.Width) * 2.5f;
            Color outerColor = currentAura * alpha * 0.2f;
            spriteBatch.Draw(glow, drawPos, null, outerColor, 0f,
                glowOrigin, outerScale, SpriteEffects.None, 0f);

            // CyberEnergyOrb 着色器绘制
            spriteBatch.End();

            Effect orbShader = EffectLoader.CyberEnergyOrb?.Value;
            Texture2D noise = CWRAsset.Extra_193?.Value;
            if (orbShader != null && noise != null) {
                float timeVal = Cyberspace.Active
                    ? Cyberspace.EffectTime
                    : (float)Main.timeForVisualEffects * 0.04f;

                orbShader.Parameters["uTime"]?.SetValue(timeVal);
                orbShader.Parameters["fadeAlpha"]?.SetValue(alpha);
                orbShader.Parameters["coreColor"]?.SetValue(currentCore.ToVector3());
                orbShader.Parameters["glowColor"]?.SetValue(currentGlow.ToVector3());
                orbShader.Parameters["auraColor"]?.SetValue(currentAura.ToVector3());
                orbShader.Parameters["orbScale"]?.SetValue(pulse);
                orbShader.Parameters["uNoiseTex"]?.SetValue(noise);

                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                orbShader.CurrentTechnique.Passes[0].Apply();

                float orbDrawScale = (orbDiameterPx / glow.Width) * 1.2f;
                spriteBatch.Draw(glow, drawPos, null, Color.White, 0f,
                    glowOrigin, orbDrawScale, SpriteEffects.None, 0f);

                spriteBatch.End();
            }

            // 恢复 Additive + Deferred
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        #endregion

        public override bool ShouldUpdatePosition() => State == OrbState.Flying;

        /// <summary>
        /// 销毁关联的手持弹幕
        /// </summary>
        private void KillHeldProj() {
            int idx = HeldProjIndex;
            if (idx >= 0 && idx < Main.maxProjectiles
                && Main.projectile[idx].active
                && Main.projectile[idx].ModProjectile is SHPCChargeHeldProj) {
                Main.projectile[idx].Kill();
            }
        }
    }
}
