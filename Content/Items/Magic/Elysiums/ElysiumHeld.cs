using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    /// <summary>
    /// 天国极乐手持弹幕，实现左键攻击(化蛇术)
    /// </summary>
    internal class ElysiumHeld : ModProjectile
    {
        public override string Texture => "CalamityOverhaul/Content/Items/Magic/Elysiums/Elysium";

        private Player Owner => Main.player[Projectile.owner];

        //蓄力时间
        private ref float ChargeTime => ref Projectile.ai[0];

        //攻击冷却
        private ref float AttackCooldown => ref Projectile.ai[1];

        //视觉效果相关
        private float staffRotation = 0f;
        private float glowPulse = 0f;
        private List<HolyRingData> holyRings = [];

        //释放爆发状态
        private bool releasing = false;
        private float releaseTimer = 0f;
        private const float ReleaseMaxTime = 25f;
        private float releaseChargeRatio = 0f;

        //圣环数据
        private class HolyRingData
        {
            public float Radius;
            public float MaxRadius;
            public float Life;
            public float MaxLife;
            public float Rotation;
            public Color RingColor;
        }

        [VaultLoaden(CWRConstant.Masking + "SoftGlow")]
        private static Asset<Texture2D> GlowAsset = null;

        public override void SetDefaults() {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI() {
            if (!Owner.active || Owner.dead) {
                Projectile.Kill();
                return;
            }

            //保持武器
            Owner.heldProj = Projectile.whoAmI;
            Projectile.timeLeft = 2;

            //释放爆发阶段
            if (releasing) {
                releaseTimer++;
                glowPulse += 0.1f;
                UpdateHolyRings();

                //爆发期间缓慢衰减照明
                float burstProg = releaseTimer / ReleaseMaxTime;
                float burstLight = (1f - burstProg) * 0.8f * releaseChargeRatio;
                Lighting.AddLight(Owner.Center, burstLight, burstLight * 0.95f, burstLight * 0.85f);

                if (releaseTimer >= ReleaseMaxTime) {
                    Projectile.Kill();
                }
                return;
            }

            //检查是否继续引导
            if (!Owner.channel) {
                //释放攻击
                if (ChargeTime > 30) {
                    ReleaseSnakeConversion();
                }
                else {
                    Projectile.Kill();
                }
                return;
            }

            //更新位置和朝向
            UpdatePositionAndRotation();

            //蓄力
            ChargeTime++;
            glowPulse += 0.1f;

            //蓄力特效
            SpawnChargeEffects();

            //更新圣环
            UpdateHolyRings();

            //消耗法力
            if (ChargeTime % 30 == 0) {
                if (!Owner.CheckMana(Owner.inventory[Owner.selectedItem], -5, true)) {
                    Projectile.Kill();
                    return;
                }
            }

            //动态照明(黑白色调)
            float intensity = 0.5f + (float)Math.Sin(glowPulse) * 0.2f;
            Lighting.AddLight(Projectile.Center, intensity, intensity, intensity);
        }

        private bool ist;
        /// <summary>
        /// 更新位置和旋转
        /// </summary>
        private void UpdatePositionAndRotation() {
            if (!ist) {
                ist = true;
                if (Owner.direction == 1) {
                    staffRotation = MathHelper.ToRadians(-160);
                }
            }

            Vector2 toMouse = Main.MouseWorld - Owner.Center;
            float targetRot = toMouse.ToRotation();

            //平滑旋转
            staffRotation = MathHelper.Lerp(staffRotation, targetRot, 0.15f);
            Projectile.rotation = staffRotation + MathHelper.PiOver4;

            //权杖位置
            Projectile.Center = Owner.Center + staffRotation.ToRotationVector2() * 30f;

            //玩家朝向
            Owner.direction = Math.Sign(toMouse.X);
            if (Owner.direction == 0) Owner.direction = 1;

            //固定玩家动作
            Owner.itemTime = Owner.itemAnimation = 2;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, staffRotation - MathHelper.PiOver2);
        }

        /// <summary>
        /// 生成蓄力特效
        /// </summary>
        private void SpawnChargeEffects() {
            //蓄力阶段性特效
            if (ChargeTime == 30) {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = 0.5f }, Projectile.Center);
                SpawnHolyRing(100f, 30, Color.White);
            }
            if (ChargeTime == 60) {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 1f, Pitch = 0.3f }, Projectile.Center);
                SpawnHolyRing(150f, 40, Color.Gold);
            }
            if (ChargeTime == 90) {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 1.2f, Pitch = 0f }, Projectile.Center);
                SpawnHolyRing(200f, 50, new Color(255, 255, 200));
            }

            //持续粒子效果
            if (ChargeTime > 20 && ChargeTime % 3 == 0) {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist = 50f + ChargeTime * 0.5f;
                Vector2 spawnPos = Owner.Center + angle.ToRotationVector2() * dist;
                Vector2 vel = (Owner.Center - spawnPos).SafeNormalize(Vector2.Zero) * 3f;

                //黑白双色粒子
                int dustType = Main.rand.NextBool() ? DustID.SilverFlame : DustID.Shadowflame;
                Dust d = Dust.NewDustPerfect(spawnPos, dustType, vel, 100, default, 1.5f);
                d.noGravity = true;
            }

            //十字架光芒
            if (ChargeTime > 60 && ChargeTime % 10 == 0) {
                SpawnCrossLight(Owner.Center);
            }
        }

        /// <summary>
        /// 生成圣环
        /// </summary>
        private void SpawnHolyRing(float maxRadius, int lifetime, Color color) {
            holyRings.Add(new HolyRingData {
                Radius = 0,
                MaxRadius = maxRadius,
                Life = 0,
                MaxLife = lifetime,
                Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                RingColor = color
            });
        }

        /// <summary>
        /// 更新圣环
        /// </summary>
        private void UpdateHolyRings() {
            for (int i = holyRings.Count - 1; i >= 0; i--) {
                var ring = holyRings[i];
                ring.Life++;
                ring.Radius = MathHelper.Lerp(0, ring.MaxRadius, ring.Life / ring.MaxLife);
                ring.Rotation += 0.05f;

                if (ring.Life >= ring.MaxLife) {
                    holyRings.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 生成十字架光芒
        /// </summary>
        private void SpawnCrossLight(Vector2 center) {
            //垂直光线
            for (int i = -3; i <= 3; i++) {
                Vector2 pos = center + new Vector2(0, i * 15);
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, Vector2.Zero, 100, Color.White, 1.2f);
                d.noGravity = true;
            }
            //水平光线
            for (int i = -2; i <= 2; i++) {
                Vector2 pos = center + new Vector2(i * 15, -15);
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, Vector2.Zero, 100, Color.White, 1.2f);
                d.noGravity = true;
            }
        }

        /// <summary>
        /// 释放化蛇术攻击，启动着色器驱动的爆发演出
        /// </summary>
        private void ReleaseSnakeConversion() {
            SoundEngine.PlaySound(SoundID.Item117 with { Volume = 1.5f, Pitch = -0.3f }, Owner.Center);

            //计算攻击范围和威力
            float chargeRatio = Math.Min(ChargeTime / 120f, 1f);
            float radius = 200f + chargeRatio * 200f;
            int damage = Projectile.damage + (int)(chargeRatio * Projectile.damage);

            //生成化蛇波动弹幕
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.Center,
                Vector2.Zero,
                ModContent.ProjectileType<SnakeConversionWave>(),
                damage,
                Projectile.knockBack,
                Owner.whoAmI,
                radius,
                chargeRatio
            );

            //进入释放爆发阶段(着色器渲染)
            releasing = true;
            releaseTimer = 0f;
            releaseChargeRatio = chargeRatio;

            //生成多层爆发圣环
            SpawnHolyRing(180f + chargeRatio * 120f, 20, Color.White);
            SpawnHolyRing(250f + chargeRatio * 150f, 25, new Color(255, 220, 160));
            if (chargeRatio > 0.4f) {
                SpawnHolyRing(320f + chargeRatio * 100f, 22, Color.Gold);
            }
            if (chargeRatio > 0.7f) {
                SpawnHolyRing(400f, 28, new Color(255, 255, 220));
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            SpriteBatch sb = Main.spriteBatch;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Texture2D staffTex = ModContent.Request<Texture2D>(Texture).Value;

            //结束当前SpriteBatch，进入着色器管线
            sb.End();

            //释放爆发阶段：渲染DivineBurst + 圣环
            if (releasing) {
                DrawShaderDivineBurst(sb);
                DrawShaderHolyRings(sb);

                //恢复SpriteBatch，不再绘制权杖
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    Main.DefaultSamplerState, DepthStencilState.None,
                    Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                return false;
            }

            //着色器绘制：神圣扩散环
            DrawShaderHolyRings(sb);

            //着色器绘制：蓄力神圣光辉
            if (ChargeTime > 10) {
                DrawShaderDivineAura(sb);
            }

            //恢复常规SpriteBatch绘制权杖
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            //蓄力时权杖附带金色光泽
            Color staffColor = lightColor;
            if (ChargeTime > 30) {
                float glowFactor = Math.Min((ChargeTime - 30) / 90f, 1f) * 0.4f;
                staffColor = Color.Lerp(staffColor, new Color(255, 230, 180), glowFactor);
            }

            //绘制权杖
            Vector2 origin = new Vector2(Owner.direction > 0 ? 10 : 20, staffTex.Height - 65f);
            float drawRot = staffRotation + MathHelper.ToRadians(76);
            SpriteEffects effect = SpriteEffects.None;
            sb.Draw(staffTex, drawPos, null, staffColor, drawRot, origin, 1f, effect, 0);

            return false;
        }

        /// <summary>
        /// 着色器绘制蓄力神圣光辉
        /// </summary>
        private void DrawShaderDivineAura(SpriteBatch sb) {
            Effect effect = EffectLoader.ElysiumStaff?.Value;
            Texture2D canvas = CWRAsset.Placeholder_White?.Value;
            Texture2D noise = CWRAsset.Extra_193?.Value;
            if (effect == null || canvas == null) {
                //着色器不可用时的简易回退
                DrawDivineAuraFallback(sb);
                return;
            }

            Vector2 tipPos = (Owner.Center + staffRotation.ToRotationVector2() * 140f) - Main.screenPosition;
            float chargeRat = Math.Min(ChargeTime / 120f, 1f);
            float auraSize = (40f + chargeRat * 60f) * 2f;

            effect.CurrentTechnique = effect.Techniques["DivineAura"];
            effect.Parameters["uTime"]?.SetValue(glowPulse);
            effect.Parameters["fadeAlpha"]?.SetValue(1f);
            effect.Parameters["chargeRatio"]?.SetValue(chargeRat);
            effect.Parameters["auraRotation"]?.SetValue(glowPulse * 0.3f);
            effect.Parameters["warmGold"]?.SetValue(new Vector3(1f, 0.863f, 0.588f));
            effect.Parameters["brightGold"]?.SetValue(new Vector3(1f, 0.784f, 0.392f));
            effect.Parameters["holyWhite"]?.SetValue(new Vector3(1f, 0.98f, 0.94f));

            if (noise != null) {
                Main.graphics.GraphicsDevice.Textures[1] = noise;
                Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            }

            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            effect.CurrentTechnique.Passes[0].Apply();

            sb.Draw(canvas, tipPos, null, Color.White, 0f,
                canvas.Size() * 0.5f, auraSize, SpriteEffects.None, 0f);

            sb.End();
        }

        /// <summary>
        /// 着色器绘制释放爆发特效
        /// </summary>
        private void DrawShaderDivineBurst(SpriteBatch sb) {
            Effect effect = EffectLoader.ElysiumStaff?.Value;
            Texture2D canvas = CWRAsset.Placeholder_White?.Value;
            Texture2D noise = CWRAsset.Extra_193?.Value;
            if (effect == null || canvas == null) return;

            Vector2 center = Owner.Center - Main.screenPosition;
            float burstProg = releaseTimer / ReleaseMaxTime;
            //爆发画布尺寸适中，让十字架和纹饰清晰可辨
            float burstSize = (120f + releaseChargeRatio * 100f) * (1f + burstProg * 0.4f);

            effect.CurrentTechnique = effect.Techniques["DivineBurst"];
            effect.Parameters["uTime"]?.SetValue(glowPulse);
            effect.Parameters["fadeAlpha"]?.SetValue(1f);
            effect.Parameters["burstProgress"]?.SetValue(burstProg);
            effect.Parameters["burstIntensity"]?.SetValue(0.6f + releaseChargeRatio * 0.4f);
            effect.Parameters["auraRotation"]?.SetValue(glowPulse * 0.3f);
            effect.Parameters["warmGold"]?.SetValue(new Vector3(1f, 0.863f, 0.588f));
            effect.Parameters["brightGold"]?.SetValue(new Vector3(1f, 0.784f, 0.392f));
            effect.Parameters["holyWhite"]?.SetValue(new Vector3(1f, 0.98f, 0.94f));

            if (noise != null) {
                Main.graphics.GraphicsDevice.Textures[1] = noise;
                Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            }

            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            effect.CurrentTechnique.Passes[0].Apply();

            sb.Draw(canvas, center, null, Color.White, 0f,
                canvas.Size() * 0.5f, burstSize, SpriteEffects.None, 0f);

            sb.End();
        }

        /// <summary>
        /// 着色器绘制神圣扩散环
        /// </summary>
        private void DrawShaderHolyRings(SpriteBatch sb) {
            if (holyRings.Count == 0) return;

            Effect effect = EffectLoader.ElysiumStaff?.Value;
            Texture2D canvas = CWRAsset.Placeholder_White?.Value;
            if (effect == null || canvas == null) return;

            Vector2 center = Owner.Center - Main.screenPosition;

            effect.CurrentTechnique = effect.Techniques["SacredRing"];
            effect.Parameters["warmGold"]?.SetValue(new Vector3(1f, 0.863f, 0.588f));
            effect.Parameters["brightGold"]?.SetValue(new Vector3(1f, 0.784f, 0.392f));
            effect.Parameters["holyWhite"]?.SetValue(new Vector3(1f, 0.98f, 0.94f));

            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (var ring in holyRings) {
                float progress = ring.Life / ring.MaxLife;
                float quadSize = (ring.MaxRadius + 40f) * 2f;

                effect.Parameters["uTime"]?.SetValue(glowPulse);
                effect.Parameters["fadeAlpha"]?.SetValue(1f);
                effect.Parameters["ringProgress"]?.SetValue(progress);
                effect.Parameters["ringColor"]?.SetValue(ring.RingColor.ToVector3());
                effect.Parameters["ringRotation"]?.SetValue(ring.Rotation);

                effect.CurrentTechnique.Passes[0].Apply();

                sb.Draw(canvas, center, null, Color.White, 0f,
                    canvas.Size() * 0.5f, quadSize, SpriteEffects.None, 0f);
            }

            sb.End();
        }

        /// <summary>
        /// 着色器不可用时的回退渲染
        /// </summary>
        private void DrawDivineAuraFallback(SpriteBatch sb) {
            if (GlowAsset?.IsLoaded != true) return;

            Vector2 tipPos = (Owner.Center + staffRotation.ToRotationVector2() * 140f) - Main.screenPosition;
            float glowScale = 0.5f + (ChargeTime / 120f) * 1f;
            float pulse = (float)Math.Sin(glowPulse) * 0.2f + 0.8f;

            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Color outerGlow = Color.White with { A = 0 } * 0.3f * pulse;
            sb.Draw(GlowAsset.Value, tipPos, null, outerGlow, 0, GlowAsset.Value.Size() / 2, glowScale * 2f, SpriteEffects.None, 0);

            Color innerGlow = new Color(255, 215, 100) with { A = 0 } * 0.5f * pulse;
            sb.Draw(GlowAsset.Value, tipPos, null, innerGlow, 0, GlowAsset.Value.Size() / 2, glowScale, SpriteEffects.None, 0);

            sb.End();
        }
    }
}
