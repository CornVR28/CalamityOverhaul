using CalamityOverhaul.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    /// <summary>
    /// 启示录Q技能：从天而降的巨型神圣天体
    /// </summary>
    internal class RevelationStar : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        private Player Owner => Main.player[Projectile.owner];

        //ai[0]=状态（0下落，1着地爆发）
        private ref float Phase => ref Projectile.ai[0];
        //ai[1]=通用计时器
        private ref float Timer => ref Projectile.ai[1];

        //天体视觉尺寸（像素）
        private float visualSize = 180f;
        //下落速度
        private float fallVelocity = 0f;
        //着地目标Y
        private float targetY;
        //全局时间累加
        private float shaderTime = 0f;
        //冲击波进度
        private float impactProgress = 0f;
        //着地时的中心位置
        private Vector2 impactCenter;
        //屏幕震动强度
        private float screenShake = 0f;

        public override void SetDefaults() {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.timeLeft = 300;
        }

        public override void AI() {
            shaderTime += 0.016f;

            if (Phase == 0) {
                FallingPhase();
            }
            else {
                ImpactPhase();
            }

            //动态照明
            float lightScale = Phase == 0 ? 0.8f : (1.2f * (1f - impactProgress));
            Lighting.AddLight(Projectile.Center, 1f * lightScale, 0.85f * lightScale, 0.5f * lightScale);

            //屏幕震动衰减
            if (screenShake > 0) {
                screenShake *= 0.9f;
                if (screenShake < 0.5f) screenShake = 0;
            }
        }

        /// <summary>
        /// 下落阶段
        /// </summary>
        private void FallingPhase() {
            Timer++;

            if (Timer == 1) {
                //初始化目标Y为生成位置下方（鼠标位置）
                targetY = Projectile.Center.Y + 800f;
                fallVelocity = 2f;
                //初始音效：天体降临预兆
                SoundEngine.PlaySound(SoundID.Item105 with { Volume = 1.2f, Pitch = -0.5f }, Projectile.Center);
            }

            //加速下落
            fallVelocity += 0.35f;
            if (fallVelocity > 22f) fallVelocity = 22f;
            Projectile.velocity = new Vector2(0, fallVelocity);

            //检测是否到达目标Y或碰到物块
            bool hitTile = false;
            //在天体前方检测物块
            Vector2 checkPos = Projectile.Center + new Vector2(0, Projectile.height / 2f + 16f);
            Point tileCoord = checkPos.ToTileCoordinates();
            if (tileCoord.X >= 0 && tileCoord.X < Main.maxTilesX &&
                tileCoord.Y >= 0 && tileCoord.Y < Main.maxTilesY) {
                Tile tile = Framing.GetTileSafely(tileCoord.X, tileCoord.Y);
                if (tile.HasTile && Main.tileSolid[tile.TileType]) {
                    hitTile = true;
                }
            }

            if (Projectile.Center.Y >= targetY || hitTile) {
                BeginImpact();
            }

            //下落过程中产生粒子尾迹
            if (Timer > 5 && Timer % 2 == 0) {
                for (int i = 0; i < 3; i++) {
                    Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(40, 20);
                    dustPos.Y -= 30f;
                    Vector2 dustVel = new Vector2(Main.rand.NextFloat(-1f, 1f), -Main.rand.NextFloat(2f, 5f));
                    Dust d = Dust.NewDustPerfect(dustPos, DustID.GoldFlame, dustVel, 100, default, 1.8f);
                    d.noGravity = true;
                }
            }
        }

        /// <summary>
        /// 开始着地冲击
        /// </summary>
        private void BeginImpact() {
            Phase = 1;
            Timer = 0;
            impactProgress = 0f;
            impactCenter = Projectile.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = impactCenter;
            screenShake = 15f;

            //冲击音效
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = 1.5f, Pitch = -0.4f }, impactCenter);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1.2f, Pitch = -0.6f }, impactCenter);

            //冲击粒子爆发
            for (int i = 0; i < 40; i++) {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float speed = Main.rand.NextFloat(3f, 12f);
                Vector2 vel = angle.ToRotationVector2() * speed;
                int dustType = Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.SilverFlame;
                Dust d = Dust.NewDustPerfect(impactCenter, dustType, vel, 80, default, Main.rand.NextFloat(1.5f, 2.5f));
                d.noGravity = true;
            }

            //地面碎片粒子
            for (int i = 0; i < 20; i++) {
                float angle = Main.rand.NextFloat(-MathHelper.Pi, 0);
                float speed = Main.rand.NextFloat(2f, 8f);
                Vector2 vel = angle.ToRotationVector2() * speed;
                Dust d = Dust.NewDustPerfect(impactCenter + new Vector2(Main.rand.NextFloat(-60, 60), 0), DustID.Smoke, vel, 150, default, 2f);
                d.noGravity = false;
            }
        }

        /// <summary>
        /// 冲击爆发阶段
        /// </summary>
        private void ImpactPhase() {
            Timer++;
            impactProgress = Math.Min(Timer / 45f, 1f);

            //冲击范围伤害（前几帧）
            if (Timer <= 10 && Timer % 3 == 0) {
                float damageRadius = 200f + impactProgress * 200f;
                foreach (NPC npc in Main.npc) {
                    if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                    if (Vector2.Distance(npc.Center, impactCenter) < damageRadius) {
                        Owner.ApplyDamageToNPC(npc, Projectile.damage, Projectile.knockBack, 0, false);
                    }
                }
            }

            //持续照明
            float lightFade = 1f - impactProgress;
            Lighting.AddLight(impactCenter, 1.5f * lightFade, 1.3f * lightFade, 0.8f * lightFade);

            //余波粒子
            if (Timer % 4 == 0 && impactProgress < 0.7f) {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist = 50f + impactProgress * 150f;
                Vector2 pos = impactCenter + angle.ToRotationVector2() * dist;
                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, Vector2.Zero, 100, default, 1.2f);
                d.noGravity = true;
            }

            if (Timer >= 50) {
                Projectile.Kill();
            }
        }

        public override bool? CanDamage() {
            //只在下落阶段直接碰撞造成伤害
            if (Phase == 0) return true;
            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            //天体砸到的直接伤害加成
            modifiers.SourceDamage *= 1.5f;
        }

        public override bool PreDraw(ref Color lightColor) {
            SpriteBatch sb = Main.spriteBatch;
            Effect effect = EffectLoader.CelestialStar?.Value;
            Texture2D canvas = CWRAsset.Placeholder_White?.Value;
            Texture2D noise = CWRAsset.Extra_193?.Value;

            if (effect == null || canvas == null) {
                //着色器不可用的简易回退
                DrawFallback(sb);
                return false;
            }

            sb.End();

            if (noise != null) {
                Main.graphics.GraphicsDevice.Textures[1] = noise;
                Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            }

            if (Phase == 0) {
                DrawCelestialBody(sb, effect, canvas);
            }
            else {
                //着地后两个效果叠加
                if (impactProgress < 0.8f) {
                    //天体本身逐渐消散
                    DrawCelestialBody(sb, effect, canvas, 1f - impactProgress * 1.2f);
                }
                DrawImpactFlare(sb, effect, canvas);
            }

            //恢复SpriteBatch
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        /// <summary>
        /// 绘制天体球体
        /// </summary>
        private void DrawCelestialBody(SpriteBatch sb, Effect effect, Texture2D canvas, float alphaOverride = 1f) {
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            //屏幕震动偏移
            if (screenShake > 0.5f) {
                drawPos += Main.rand.NextVector2Circular(screenShake, screenShake);
            }

            float drawSize = visualSize * 2.5f;

            effect.CurrentTechnique = effect.Techniques["CelestialBody"];
            effect.Parameters["uTime"]?.SetValue(shaderTime);
            effect.Parameters["fadeAlpha"]?.SetValue(Math.Max(alphaOverride, 0f));
            effect.Parameters["fallSpeed"]?.SetValue(fallVelocity);
            effect.Parameters["sphereRadius"]?.SetValue(0.18f);
            effect.Parameters["coronaWidth"]?.SetValue(0.12f);
            effect.Parameters["intensity"]?.SetValue(1.4f);

            //神圣色调：暖白核心、金色表面、琥珀日冕
            effect.Parameters["coreColor"]?.SetValue(new Vector3(1f, 0.96f, 0.88f));
            effect.Parameters["surfaceColor"]?.SetValue(new Vector3(1f, 0.82f, 0.45f));
            effect.Parameters["coronaColor"]?.SetValue(new Vector3(0.95f, 0.55f, 0.2f));
            effect.Parameters["trailColor"]?.SetValue(new Vector3(0.9f, 0.65f, 0.3f));

            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            effect.CurrentTechnique.Passes[0].Apply();

            sb.Draw(canvas, drawPos, null, Color.White, 0f,
                canvas.Size() * 0.5f, drawSize, SpriteEffects.None, 0f);

            sb.End();
        }

        /// <summary>
        /// 绘制着地冲击波
        /// </summary>
        private void DrawImpactFlare(SpriteBatch sb, Effect effect, Texture2D canvas) {
            Vector2 drawPos = impactCenter - Main.screenPosition;

            if (screenShake > 0.5f) {
                drawPos += Main.rand.NextVector2Circular(screenShake, screenShake);
            }

            float flareSize = 400f + impactProgress * 300f;

            effect.CurrentTechnique = effect.Techniques["ImpactFlare"];
            effect.Parameters["uTime"]?.SetValue(shaderTime);
            effect.Parameters["fadeAlpha"]?.SetValue(1f - impactProgress * 0.8f);
            effect.Parameters["impactProgress"]?.SetValue(impactProgress);
            effect.Parameters["impactRadius"]?.SetValue(0.4f);
            effect.Parameters["intensity"]?.SetValue(1.6f * (1f - impactProgress * 0.5f));

            effect.Parameters["coreColor"]?.SetValue(new Vector3(1f, 0.96f, 0.88f));
            effect.Parameters["surfaceColor"]?.SetValue(new Vector3(1f, 0.82f, 0.45f));
            effect.Parameters["coronaColor"]?.SetValue(new Vector3(0.95f, 0.55f, 0.2f));

            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            effect.CurrentTechnique.Passes[0].Apply();

            sb.Draw(canvas, drawPos, null, Color.White, 0f,
                canvas.Size() * 0.5f, flareSize, SpriteEffects.None, 0f);

            sb.End();
        }

        /// <summary>
        /// 着色器不可用时的回退绘制
        /// </summary>
        private void DrawFallback(SpriteBatch sb) {
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float pulse = (float)Math.Sin(shaderTime * 3f) * 0.15f + 0.85f;
            float scale = visualSize / 32f * pulse;

            //简单的圆形光辉
            Texture2D glow = CWRAsset.Placeholder_White?.Value;
            if (glow == null) return;

            Color c = new Color(255, 210, 100) with { A = 0 } * 0.6f;
            sb.Draw(glow, drawPos, null, c, 0, glow.Size() / 2, scale, SpriteEffects.None, 0);
        }
    }
}
