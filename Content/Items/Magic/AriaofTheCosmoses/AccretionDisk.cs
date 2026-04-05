using CalamityOverhaul.Common;
using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.AriaofTheCosmoses
{
    /// <summary>
    /// 吸积盘
    /// </summary>
    internal class AccretionDisk : ModProjectile, IPrimitiveDrawable
    {
        public override string Texture => CWRConstant.Placeholder;

        //吸积盘参数
        public ref float RotationSpeed => ref Projectile.ai[0];
        public ref float InnerRadius => ref Projectile.ai[1];
        public ref float OuterRadius => ref Projectile.ai[2];

        private float time;
        private float brightness = 1f;
        private float distortionStrength = 0.15f;

        //颜色配置
        private Color innerColor = new Color(255, 200, 100); //内圈
        private Color midColor = new Color(255, 120, 50);    //中圈
        private Color outerColor = new Color(100, 50, 150);  //外圈

        private bool isAttacking = false;

        public override void SetStaticDefaults() {
            Main.projFrames[Type] = 1;
        }

        public override void SetDefaults() {
            Projectile.width = 400;
            Projectile.height = 400;
            Projectile.friendly = false; //初始不造成伤害
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 2;
        }

        public override void AI() {
            //检测是否进入攻击模式
            if (Projectile.velocity.Length() > 0.1f && !isAttacking) {
                isAttacking = true;
                Projectile.friendly = true;
            }

            //淡入效果
            if (Projectile.alpha > 0 && !isAttacking) {
                Projectile.alpha -= 5;
            }
            else if (isAttacking && Projectile.alpha > 50) {
                Projectile.alpha = 50; //攻击时保持可见
            }

            time += 0.016f;

            //默认参数设置
            if (RotationSpeed == 0) {
                RotationSpeed = 1f;
            }
            if (InnerRadius == 0) {
                InnerRadius = 0.15f;
            }
            if (OuterRadius == 0) {
                OuterRadius = 0.85f;
            }

            //脉动效果
            float pulse = (float)Math.Sin(time * 2f) * 0.1f + 0.9f;
            brightness = pulse;

            //旋转
            Projectile.rotation += 0.005f + (isAttacking ? RotationSpeed * 0.02f : 0);

            //攻击模式下的行为
            if (isAttacking) {
                //追踪附近敌人
                HomeInOnNearestEnemy();

                //减速效果
                Projectile.velocity *= 0.98f;

                //生成轨迹粒子
                if (Main.rand.NextBool(2)) {
                    SpawnTrailParticles();
                }
            }
            else {
                //蓄力模式生成环绕粒子
                if (Projectile.timeLeft % 3 == 0 && !VaultUtils.isServer) {
                    SpawnDiskParticles();
                }
            }

            //淡出效果
            if (Projectile.timeLeft < 60) {
                Projectile.alpha += 4;
                brightness *= Projectile.timeLeft / 60f;
            }

            //发光
            Lighting.AddLight(Projectile.Center,
                innerColor.ToVector3() * brightness * 0.8f * (1f - Projectile.alpha / 255f));
        }

        private void HomeInOnNearestEnemy() {
            float maxDetectDistance = 600f;
            float homingStrength = 0.15f;

            NPC closestNPC = null;
            float minDistance = maxDetectDistance;

            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.CanBeChasedBy(Projectile)) {
                    continue;
                }

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance < minDistance) {
                    minDistance = distance;
                    closestNPC = npc;
                }
            }

            if (closestNPC != null) {
                Vector2 desiredVelocity = Projectile.DirectionTo(closestNPC.Center) * Projectile.velocity.Length();
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, homingStrength);
            }
        }

        private void SpawnDiskParticles() {
            //在吸积盘边缘生成高级粒子
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            float distance = Main.rand.NextFloat(InnerRadius, OuterRadius) * Projectile.width * 0.5f * Projectile.scale;

            Vector2 offset = new Vector2(
                (float)Math.Cos(angle) * distance,
                (float)Math.Sin(angle) * distance
            );

            Vector2 particlePos = Projectile.Center + offset;
            Vector2 particleVel = Vector2.Normalize(offset.RotatedBy(MathHelper.PiOver2)) * Main.rand.NextFloat(1f, 3f);

            //计算距离中心的比例来决定颜色
            float distanceRatio = (distance - InnerRadius * Projectile.width * 0.5f * Projectile.scale) /
                                 ((OuterRadius - InnerRadius) * Projectile.width * 0.5f * Projectile.scale);
            Color particleColor = Color.Lerp(innerColor, outerColor, distanceRatio);

            //创建高级粒子
            BasePRT particle = new PRT_AccretionDiskImpact(
                particlePos,
                particleVel,
                particleColor,
                Main.rand.NextFloat(0.3f, 0.6f),
                Main.rand.Next(20, 35),
                Main.rand.NextFloat(-0.1f, 0.1f),
                false,
                Main.rand.NextFloat(0.12f, 0.18f)
            );
            PRTLoader.AddParticle(particle);

            //放射状冲击粒子
            for (int i = 0; i < 24; i++) {
                float angle2 = MathHelper.TwoPi * i / 24f;
                Vector2 velocity = angle2.ToRotationVector2() * Main.rand.NextFloat(6f, 13f);

                PRT_GammaImpact burst = new PRT_GammaImpact(
                    Projectile.Center,
                    velocity,
                    Color.Lerp(Color.Cyan, Color.White, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.5f, 0.75f),
                    Main.rand.Next(30, 45),
                    Main.rand.NextFloat(-0.4f, 0.4f),
                    false,
                    0.3f
                );
                PRTLoader.AddParticle(burst);
            }
        }

        private void SpawnTrailParticles() {
            int particleCount = 3;
            for (int i = 0; i < particleCount; i++) {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float distance = Main.rand.NextFloat(0, Projectile.width * 0.5f * Projectile.scale);
                Vector2 offset = angle.ToRotationVector2() * distance;

                Vector2 particlePos = Projectile.Center + offset;
                Vector2 particleVel = -Projectile.velocity * Main.rand.NextFloat(0.3f, 0.6f);

                Color particleColor = Color.Lerp(innerColor, outerColor, Main.rand.NextFloat()) * 0.9f;

                //创建高级拖尾粒子
                BasePRT particle = new PRT_AccretionDiskImpact(
                    particlePos,
                    particleVel,
                    particleColor,
                    Main.rand.NextFloat(0.4f, 0.8f),
                    Main.rand.Next(15, 25),
                    Main.rand.NextFloat(-0.2f, 0.2f),
                    false,
                    Main.rand.NextFloat(0.15f, 0.25f)
                );
                PRTLoader.AddParticle(particle);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (!isAttacking) {
                return false;
            }

            //圆形碰撞检测
            float collisionRadius = Projectile.width * 0.5f * Projectile.scale * OuterRadius;
            return VaultUtils.CircleIntersectsRectangle(Projectile.Center, collisionRadius, targetHitbox);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            //击中音效
            SoundEngine.PlaySound(SoundID.Item14 with {
                Volume = 0.5f,
                Pitch = 0.3f
            }, Projectile.Center);

            //击中特效
            if (!VaultUtils.isServer && Projectile.velocity.Length() < 2) {
                for (int i = 0; i < 15 * Projectile.scale; i++) {
                    Vector2 velocity = Main.rand.NextVector2Circular(8f, 8f);
                    Color particleColor = Color.Lerp(innerColor, outerColor, Main.rand.NextFloat());

                    BasePRT particle = new PRT_AccretionDiskImpact(
                        target.Center,
                        velocity,
                        particleColor,
                        Main.rand.NextFloat(0.5f, 1.0f),
                        Main.rand.Next(20, 35),
                        Main.rand.NextFloat(-0.3f, 0.3f),
                        false,
                        Main.rand.NextFloat(0.2f, 0.3f)
                    );
                    PRTLoader.AddParticle(particle);
                }

                //放射状冲击粒子
                for (int i = 0; i < 24 * Projectile.scale; i++) {
                    float angle = MathHelper.TwoPi * i / 24f;
                    Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(6f, 13f);

                    PRT_GammaImpact burst = new PRT_GammaImpact(
                        Projectile.Center,
                        velocity,
                        Color.Lerp(Color.Cyan, Color.White, Main.rand.NextFloat()),
                        Main.rand.NextFloat(0.5f, 0.75f) * Projectile.scale,
                        Main.rand.Next(30, 45),
                        Main.rand.NextFloat(-0.4f, 0.4f),
                        false,
                        0.3f
                    );
                    PRTLoader.AddParticle(burst);
                }

                //内爆收缩粒子
                for (int i = 0; i < 15 * Projectile.scale; i++) {
                    Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(90f, 90f);
                    Vector2 velocity = (Projectile.Center - spawnPos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(10f, 18f);

                    BasePRT implosion = new PRT_Spark(
                        spawnPos,
                        velocity,
                        false,
                        Main.rand.Next(20, 30),
                        Main.rand.NextFloat(1f, 1.8f),
                        Color.White
                    );
                    PRTLoader.AddParticle(implosion);
                }
            }
        }

        public override void OnKill(int timeLeft) {
            //死亡爆炸效果
            if (!VaultUtils.isServer) {
                SoundEngine.PlaySound(SoundID.Item62 with {
                    Volume = 0.7f,
                    Pitch = -0.2f
                }, Projectile.Center);

                int particleCount = (int)(30 * Projectile.scale);
                for (int i = 0; i < particleCount; i++) {
                    float angle = MathHelper.TwoPi * i / particleCount;
                    Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(3f, 12f);
                    Color particleColor = Color.Lerp(innerColor, outerColor, Main.rand.NextFloat());

                    BasePRT particle = new PRT_AccretionDiskImpact(
                        Projectile.Center,
                        velocity,
                        particleColor,
                        Main.rand.NextFloat(0.6f, 1.2f),
                        Main.rand.Next(25, 45),
                        Main.rand.NextFloat(-0.4f, 0.4f),
                        false,
                        Main.rand.NextFloat(0.15f, 0.25f)
                    );
                    PRTLoader.AddParticle(particle);
                }
            }
        }

        public void DrawPrimitives() {
            if (VaultUtils.isServer) {
                return;
            }

            DrawAccretionDisk();
        }

        [VaultLoaden(CWRConstant.Masking)]
        private static Texture2D TransverseTwill = null!;

        private void DrawAccretionDisk() {
            SpriteBatch spriteBatch = Main.spriteBatch;
            float alpha = 1f - Projectile.alpha / 255f;
            if (alpha <= 0f) return;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float actualSize = Projectile.width * Projectile.scale;
            Vector2 texHalf = TransverseTwill.Size() * 0.5f;
            float bhScale = actualSize / TransverseTwill.Width;
            Vector2 drawScale = new Vector2(bhScale, bhScale); //1:1圆形

            Matrix finalMatrix = Matrix.Identity
                * Main.GameViewMatrix.TransformationMatrix
                * Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);

            Effect bhShader = EffectLoader.BlackHole.Value;

            //设置着色器参数
            bhShader.Parameters["transformMatrix"]?.SetValue(finalMatrix);
            bhShader.Parameters["uTime"]?.SetValue(time);
            bhShader.Parameters["rotationSpeed"]?.SetValue(RotationSpeed);
            bhShader.Parameters["eventHorizonRadius"]?.SetValue(0.1f);
            bhShader.Parameters["diskInnerRadius"]?.SetValue(0.14f);
            bhShader.Parameters["diskOuterRadius"]?.SetValue(0.42f);
            bhShader.Parameters["brightness"]?.SetValue(brightness * 1.0f);
            bhShader.Parameters["dopplerStrength"]?.SetValue(0.45f);
            bhShader.Parameters["distortionStrength"]?.SetValue(0.6f);
            bhShader.Parameters["noiseTexture"]?.SetValue(TransverseTwill);
            bhShader.Parameters["centerPos"]?.SetValue(drawPos);
            bhShader.Parameters["innerColor"]?.SetValue(innerColor.ToVector4());
            bhShader.Parameters["midColor"]?.SetValue(midColor.ToVector4());
            bhShader.Parameters["outerColor"]?.SetValue(outerColor.ToVector4());

            Main.graphics.GraphicsDevice.Textures[1] = TransverseTwill;
            Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

            //Phase1: 事件视界（AlphaBlend模式吞噬背景光）
            {
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                bhShader.CurrentTechnique = bhShader.Techniques["EventHorizon"];
                bhShader.CurrentTechnique.Passes[0].Apply();

                spriteBatch.Draw(TransverseTwill, drawPos, null,
                    Color.White * alpha,
                    0f,
                    texHalf, drawScale * 1.3f, SpriteEffects.None, 0);

                spriteBatch.End();
            }

            //Phase2: 吸积盘+光子环（Additive模式叠加光效）
            {
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                bhShader.CurrentTechnique = bhShader.Techniques["Accretion"];
                bhShader.CurrentTechnique.Passes[0].Apply();

                //外围柔光
                spriteBatch.Draw(TransverseTwill, drawPos, null,
                    Color.White * alpha * 0.25f,
                    Projectile.rotation * 0.08f,
                    texHalf, drawScale * 1.6f, SpriteEffects.None, 0);

                //主体吸积盘
                spriteBatch.Draw(TransverseTwill, drawPos, null,
                    Color.White * alpha * 0.7f,
                    Projectile.rotation * 0.05f,
                    texHalf, drawScale * 1.15f, SpriteEffects.None, 0);

                //第二层（轻微偏移增加质感）
                spriteBatch.Draw(TransverseTwill, drawPos, null,
                    Color.White * alpha * 0.5f,
                    Projectile.rotation * 0.03f + 0.2f,
                    texHalf, drawScale * 1.05f, SpriteEffects.None, 0);

                spriteBatch.End();
            }
        }
    }
}
