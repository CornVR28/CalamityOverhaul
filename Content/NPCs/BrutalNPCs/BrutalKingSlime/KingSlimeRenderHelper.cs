using CalamityOverhaul.Common;
using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Core;
using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Projectiles;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime
{
    /// <summary>
    /// 史莱姆王绘制 / 视觉辅助
    /// <br/>负责蓄力光环、皇家描边、冲击波弹幕生成等公共操作
    /// </summary>
    internal static class KingSlimeRenderHelper
    {
        /// <summary>
        /// 皇室主色板
        /// </summary>
        public static Vector3 RoyalCore => Color.Red.ToVector3();
        public static Vector3 RoyalEdge => Color.GreenYellow.ToVector3();

        private static void BeginAdditive(SpriteBatch spriteBatch) {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static void EndAdditive(SpriteBatch spriteBatch) {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// 在 NPC 落地时生成一发皇室凝胶冲击波弹幕，并附带粒子点缀
        /// </summary>
        public static void DoLandingShockwave(NPC npc, KingSlimeStateContext ctx, float scale) {
            if (!VaultUtils.isClient) {
                int slamProj = ModContent.ProjectileType<KingSlimeShockwaveProj>();
                int dmg = CWRRef.GetProjectileDamage(npc, ProjectileID.None);
                if (dmg < 18) dmg = 18;
                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Bottom + new Vector2(0, -8),
                    Vector2.Zero, slamProj, dmg, 2f, Main.myPlayer, ai0: scale);
            }

            if (!VaultUtils.isServer) {
                for (int i = 0; i < (int)(8 * scale); i++) {
                    Vector2 vel = new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, -1f));
                    Dust dust = Dust.NewDustDirect(npc.Bottom - new Vector2(8, 12),
                        16, 12, DustID.PinkCrystalShard, vel.X, vel.Y, 100, default, 1.4f * scale);
                    dust.noGravity = true;
                }
            }
        }

        /// <summary>
        /// 绘制蓄力 / 演出特效
        /// </summary>
        public static void DrawChargeEffect(SpriteBatch spriteBatch, KingSlimeStateContext context) {
            if (!context.IsCharging || context.ChargeProgress <= 0) return;
            Vector2 drawPos = context.Npc.Center - Main.screenPosition;

            switch (context.ChargeType) {
                case 1: DrawSlamChargeEffect(spriteBatch, drawPos, context); break;
                case 2: DrawCrownChargeEffect(spriteBatch, drawPos, context); break;
                case 3: DrawSlimeRainEffect(spriteBatch, drawPos, context); break;
                case 4: DrawTeleDashEffect(spriteBatch, drawPos, context); break;
            }
        }

        private static void DrawSlamChargeEffect(SpriteBatch sb, Vector2 drawPos, KingSlimeStateContext context) {
            Texture2D glowTex = CWRAsset.SoftGlow.Value;
            Texture2D circleTex = CWRAsset.DiffusionCircle.Value;
            Texture2D lineTex = CWRAsset.LightShot.Value;
            float progress = context.ChargeProgress;
            Color royalColor = Color.Lerp(new Color(110, 80, 230), new Color(255, 220, 110), progress);

            BeginAdditive(sb);

            //外圈大光晕
            float outerScale = 2.5f + progress * 3.5f;
            Main.EntitySpriteDraw(glowTex, drawPos, null, royalColor * (progress * 0.5f),
                0f, glowTex.Size() / 2f, outerScale, SpriteEffects.None, 0);

            //白色内核
            float innerScale = 1.4f + progress * 2f;
            Main.EntitySpriteDraw(glowTex, drawPos, null, Color.White * (progress * 0.45f),
                0f, glowTex.Size() / 2f, innerScale, SpriteEffects.None, 0);

            //收缩圆环
            for (int i = 0; i < 3; i++) {
                float phase = (progress + i * 0.12f) % 1f;
                float ringScale = (3.5f - phase * 3f) * (1f + i * 0.15f);
                float ringAlpha = phase * (1f - phase) * 1.1f;
                Main.EntitySpriteDraw(circleTex, drawPos, null, royalColor * ringAlpha,
                    Main.GlobalTimeWrappedHourly * (3f + i), circleTex.Size() / 2f,
                    ringScale, SpriteEffects.None, 0);
            }

            //6 道向下/侧向放射线，体现"皇家落雷"预警
            if (progress > 0.3f) {
                float rayProg = (progress - 0.3f) / 0.7f;
                int rayCount = 6;
                for (int i = 0; i < rayCount; i++) {
                    float angle = MathHelper.TwoPi / rayCount * i + Main.GlobalTimeWrappedHourly * 1.5f;
                    Vector2 dir = angle.ToRotationVector2();
                    int segs = 8;
                    for (int j = 0; j < segs; j++) {
                        float t = j / (float)segs;
                        Vector2 pos = drawPos + dir * (50f + t * 220f * rayProg);
                        float a = (1f - t) * rayProg * 0.6f;
                        Main.EntitySpriteDraw(lineTex, pos, null, royalColor * a,
                            angle, new Vector2(0, lineTex.Height / 2f),
                            new Vector2(0.5f, 0.20f * (1f - t * 0.4f)), SpriteEffects.None, 0);
                    }
                }
            }

            EndAdditive(sb);
        }

        private static void DrawCrownChargeEffect(SpriteBatch sb, Vector2 drawPos, KingSlimeStateContext context) {
            Texture2D glowTex = CWRAsset.SoftGlow.Value;
            Texture2D circleTex = CWRAsset.DiffusionCircle.Value;
            float progress = context.ChargeProgress;
            //皇冠位置略上方
            Vector2 crownPos = drawPos + new Vector2(0, -context.Npc.height * 0.45f);
            Color crownColor = Color.Lerp(new Color(255, 235, 130), new Color(255, 180, 60), progress);

            BeginAdditive(sb);

            //大范围核心光晕
            float coreScale = 1.6f + progress * 2.5f;
            Main.EntitySpriteDraw(glowTex, crownPos, null, crownColor * (progress * 0.6f),
                0f, glowTex.Size() / 2f, coreScale, SpriteEffects.None, 0);

            //白色内核
            float innerScale = 0.6f + progress * 1.2f;
            Main.EntitySpriteDraw(glowTex, crownPos, null, Color.White * (progress * 0.45f),
                0f, glowTex.Size() / 2f, innerScale, SpriteEffects.None, 0);

            //脉冲波纹
            for (int i = 0; i < 3; i++) {
                float pulsePhase = (Main.GlobalTimeWrappedHourly * 2.5f + i * 0.33f) % 1f;
                float pulseScale = 0.7f + pulsePhase * 2.8f;
                float pulseAlpha = (1f - pulsePhase) * progress * 0.6f;
                Main.EntitySpriteDraw(circleTex, crownPos, null, crownColor * pulseAlpha,
                    0f, circleTex.Size() / 2f, pulseScale, SpriteEffects.None, 0);
            }

            EndAdditive(sb);
        }

        private static void DrawSlimeRainEffect(SpriteBatch sb, Vector2 drawPos, KingSlimeStateContext context) {
            Texture2D glowTex = CWRAsset.SoftGlow.Value;
            Texture2D circleTex = CWRAsset.DiffusionCircle.Value;
            float progress = context.ChargeProgress;
            Color royalColor = Color.Lerp(new Color(120, 200, 255), new Color(150, 110, 255), progress);

            BeginAdditive(sb);

            //大范围光晕
            float coreScale = 2.0f + progress * 3.0f;
            Main.EntitySpriteDraw(glowTex, drawPos, null, royalColor * (progress * 0.5f),
                0f, glowTex.Size() / 2f, coreScale, SpriteEffects.None, 0);

            //双层旋转环
            for (int i = 0; i < 2; i++) {
                float ringScale = (1.6f + progress * 2.2f) * (1f + i * 0.3f);
                float ringAlpha = progress * 0.45f * (1f - i * 0.3f);
                float ringRot = Main.GlobalTimeWrappedHourly * (3f + i * 2f) * (i == 0 ? 1 : -1);
                Main.EntitySpriteDraw(circleTex, drawPos, null, royalColor * ringAlpha,
                    ringRot, circleTex.Size() / 2f, ringScale, SpriteEffects.None, 0);
            }

            EndAdditive(sb);
        }

        private static void DrawTeleDashEffect(SpriteBatch sb, Vector2 drawPos, KingSlimeStateContext context) {
            Texture2D glowTex = CWRAsset.SoftGlow.Value;
            Texture2D lineTex = CWRAsset.LightShot.Value;
            float progress = context.ChargeProgress;
            Color royalColor = Color.Lerp(new Color(140, 100, 255), new Color(255, 220, 110), progress);

            BeginAdditive(sb);

            //核心光晕
            float coreScale = 1.5f + progress * 2.5f;
            Main.EntitySpriteDraw(glowTex, drawPos, null, royalColor * (progress * 0.6f),
                0f, glowTex.Size() / 2f, coreScale, SpriteEffects.None, 0);

            //冲撞瞄准线
            if (progress > 0.4f && context.DashDirection != Vector2.Zero) {
                float aimProg = (progress - 0.4f) / 0.6f;
                float lineLength = 480f * aimProg;
                int segments = (int)(lineLength / 12f);
                Vector2 dir = context.DashDirection;
                for (int i = 0; i < segments; i++) {
                    float t = i / (float)Math.Max(segments, 1);
                    Vector2 segPos = drawPos + dir * (40f + t * lineLength);
                    float segAlpha = aimProg * (1f - t * 0.6f) * 0.75f;
                    float pulse = 0.7f + 0.3f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f + t * 8f);
                    Main.EntitySpriteDraw(lineTex, segPos, null, royalColor * segAlpha * pulse,
                        dir.ToRotation(), new Vector2(0, lineTex.Height / 2f),
                        new Vector2(0.5f, 0.30f * (1f - t * 0.3f)), SpriteEffects.None, 0);
                }
            }

            EndAdditive(sb);
        }

        #region 皇家描边光环 + 着色器

        /// <summary>
        /// 在贴图四周做 8 方向偏移叠加，得到一圈彩色描边光晕，确保夜晚远距离也能看清史莱姆王轮廓。
        /// </summary>
        public static void DrawRoyalHalo(SpriteBatch spriteBatch, Texture2D texture, Vector2 drawPos,
            Rectangle? sourceRect, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects,
            KingSlimeAuraMode mode, float intensity, float progress) {
            if (intensity <= 0.01f) return;

            float pulseSpeed = mode switch {
                KingSlimeAuraMode.Slamming => 9f,
                KingSlimeAuraMode.Charging => 5f + progress * 4f,
                KingSlimeAuraMode.Enraged => 3.5f,
                _ => 1.4f,
            };
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * pulseSpeed);

            float radius = mode switch {
                KingSlimeAuraMode.Slamming => 6f,
                KingSlimeAuraMode.Charging => 3.5f + 2.5f * progress,
                KingSlimeAuraMode.Enraged => 4.5f + 0.8f * pulse,
                _ => 2.2f,
            };
            float baseStrength = mode switch {
                KingSlimeAuraMode.Slamming => 1f,
                KingSlimeAuraMode.Charging => 0.55f + 0.45f * progress,
                KingSlimeAuraMode.Enraged => 0.7f + 0.20f * pulse,
                _ => 0.40f,
            };
            Color haloColor = mode switch {
                KingSlimeAuraMode.Slamming => Color.Lerp(new Color(255, 100, 0), new Color(255, 215, 0), 0.4f + 0.55f * pulse),
                KingSlimeAuraMode.Charging => Color.Lerp(new Color(138, 3, 3), new Color(220, 20, 60), 0.35f + 0.55f * pulse * progress),
                KingSlimeAuraMode.Enraged => Color.Lerp(new Color(60, 0, 0), new Color(180, 0, 0), 0.4f + 0.5f * pulse),
                _ => Color.Lerp(new Color(100, 10, 10), new Color(196, 156, 72), 0.40f + 0.30f * pulse),
            };
            haloColor *= baseStrength * intensity;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            int steps = mode == KingSlimeAuraMode.Idle ? 6 : 8;
            for (int i = 0; i < steps; i++) {
                float angle = MathHelper.TwoPi / steps * i;
                Vector2 offset = angle.ToRotationVector2() * radius;
                spriteBatch.Draw(texture, drawPos + offset, sourceRect, haloColor,
                    rotation, origin, scale, effects, 0f);
            }

            //砸地时再叠一层更宽更柔的外晕
            if (mode == KingSlimeAuraMode.Slamming) {
                Color softColor = haloColor * 0.5f;
                for (int i = 0; i < 6; i++) {
                    float angle = MathHelper.TwoPi / 6 * i + MathHelper.PiOver4;
                    Vector2 offset = angle.ToRotationVector2() * (radius + 5f);
                    spriteBatch.Draw(texture, drawPos + offset, sourceRect, softColor,
                        rotation, origin, scale, effects, 0f);
                }
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// 切到 Immediate 模式并启用皇室光环着色器，调用方在绘制完后必须调用 <see cref="EndRoyalAuraShader"/> 还原。
        /// </summary>
        public static bool BeginRoyalAuraShader(SpriteBatch spriteBatch, Texture2D texture, Rectangle sourceRect,
            KingSlimeAuraMode mode, float intensity, float progress, float seed = 0f) {
            if (intensity <= 0.01f) return false;
            Effect shader = EffectLoader.KingSlimeRoyalAura?.Value;
            if (shader == null) return false;

            float invW = 1f / texture.Width;
            float invH = 1f / texture.Height;

            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["intensity"]?.SetValue(intensity);
            shader.Parameters["mode"]?.SetValue((float)mode);
            shader.Parameters["progress"]?.SetValue(progress);
            shader.Parameters["texelSize"]?.SetValue(new Vector2(invW, invH));
            shader.Parameters["seed"]?.SetValue(seed);
            shader.Parameters["royalCore"]?.SetValue(RoyalCore);
            shader.Parameters["royalEdge"]?.SetValue(RoyalEdge);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();
            return true;
        }

        public static void EndRoyalAuraShader(SpriteBatch spriteBatch) {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        #endregion
    }

    /// <summary>
    /// 皇室光环可视模式，与着色器中的 mode 参数对应：
    /// 0=常态 1=蓄力 2=暴怒 3=砸地
    /// </summary>
    internal enum KingSlimeAuraMode
    {
        Idle = 0,
        Charging = 1,
        Enraged = 2,
        Slamming = 3,
    }
}
