using CalamityOverhaul.Common;
using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalDestroyer.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalDestroyer.Rendering
{
    /// <summary>
    /// 毁灭者绘制辅助类
    /// </summary>
    internal static class DestroyerRenderHelper
    {
        #region 机械热感着色器/外描边光环
        /// <summary>
        /// 8方向偏移描边色（头/体/尾共用调色盘）
        /// </summary>
        private static Color GetHaloColor(DestroyerVisualMode mode, float progress, float pulse) {
            return mode switch {
                DestroyerVisualMode.Dashing => Color.Lerp(new Color(255, 110, 30), new Color(255, 240, 170), 0.45f + 0.55f * pulse),
                DestroyerVisualMode.Warning => Color.Lerp(new Color(220, 40, 10), new Color(255, 210, 60), 0.35f + 0.55f * pulse * progress),
                _ => Color.Lerp(new Color(150, 30, 10), new Color(220, 80, 25), 0.40f + 0.30f * pulse),
            };
        }

        /// <summary>
        /// 各状态在外圈描边的力度系数（用于halo混合强度）
        /// </summary>
        private static float GetHaloStrength(DestroyerVisualMode mode, float progress, float intensity) {
            float baseStrength = mode switch {
                DestroyerVisualMode.Dashing => 1.0f,
                DestroyerVisualMode.Warning => 0.55f + 0.45f * progress,
                _ => 0.35f,
            };
            return baseStrength * intensity;
        }

        /// <summary>
        /// 各状态在外圈描边的扩散半径（像素）
        /// </summary>
        private static float GetHaloRadius(DestroyerVisualMode mode, float progress) {
            return mode switch {
                DestroyerVisualMode.Dashing => 5.5f,
                DestroyerVisualMode.Warning => 3.5f + 2.5f * progress,
                _ => 2.0f,
            };
        }

        /// <summary>
        /// 8方向描边光环——将贴图沿圆周偏移多次叠加，形成颜色描边
        /// 这一层主要解决远距离/夜晚的"看不清"问题（即使着色器对边缘范围有限，
        /// 这层halo是真正"在贴图外"绘制的，不受单帧透明像素空间限制）
        /// </summary>
        public static void DrawOutlineHalo(SpriteBatch spriteBatch, Texture2D texture, Vector2 drawPos,
            Rectangle? sourceRect, float rotation, Vector2 origin, float scale, SpriteEffects effects) {
            var (mode, intensity, progress) = DestroyerVisualState.Read();
            if (intensity <= 0.01f) return;

            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * (mode == DestroyerVisualMode.Idle ? 2.2f : 6.5f));
            float radius = GetHaloRadius(mode, progress);
            float strength = GetHaloStrength(mode, progress, intensity);
            Color haloColor = GetHaloColor(mode, progress, pulse) * strength;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //外圈8方向偏移
            int steps = mode == DestroyerVisualMode.Idle ? 6 : 8;
            for (int i = 0; i < steps; i++) {
                float angle = MathHelper.TwoPi / steps * i;
                Vector2 offset = angle.ToRotationVector2() * radius;
                spriteBatch.Draw(texture, drawPos + offset, sourceRect, haloColor,
                    rotation, origin, scale, effects, 0f);
            }

            //冲刺时再叠一圈更宽更浅的外晕，强化"高速过热"感
            if (mode == DestroyerVisualMode.Dashing) {
                Color softColor = haloColor * 0.45f;
                for (int i = 0; i < 6; i++) {
                    float angle = MathHelper.TwoPi / 6 * i + MathHelper.PiOver4;
                    Vector2 offset = angle.ToRotationVector2() * (radius + 4f);
                    spriteBatch.Draw(texture, drawPos + offset, sourceRect, softColor,
                        rotation, origin, scale, effects, 0f);
                }
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// 切到 Immediate 模式并启用机械热感着色器，调用方需在绘制完后调用 EndThermalShader 还原
        /// </summary>
        public static bool BeginThermalShader(SpriteBatch spriteBatch, Texture2D texture, float seed = 0f) {
            var (mode, intensity, progress) = DestroyerVisualState.Read();
            if (intensity <= 0.01f) return false;

            Effect shader = EffectLoader.DestroyerThermalOutline?.Value;
            if (shader == null) return false;

            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["intensity"]?.SetValue(intensity);
            shader.Parameters["mode"]?.SetValue((float)mode);
            shader.Parameters["progress"]?.SetValue(progress);
            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / texture.Width, 1f / texture.Height));
            shader.Parameters["seed"]?.SetValue(seed);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();
            return true;
        }

        /// <summary>
        /// 还原默认的 Deferred AlphaBlend SpriteBatch（与 Begin 配对使用）
        /// </summary>
        public static void EndThermalShader(SpriteBatch spriteBatch) {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        #endregion

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
        /// 绘制蓄力特效
        /// </summary>
        public static void DrawChargeEffect(SpriteBatch spriteBatch, DestroyerStateContext context) {
            if (!context.IsCharging || context.ChargeProgress <= 0) return;

            Vector2 drawPos = context.Npc.Center - Main.screenPosition;

            switch (context.ChargeType) {
                case 1:
                    DrawDashChargeEffect(spriteBatch, drawPos, context);
                    break;
                case 2:
                    DrawLaserChargeEffect(spriteBatch, drawPos, context);
                    break;
                case 3:
                    DrawEncircleEffect(spriteBatch, context);
                    break;
                case 4:
                    DrawProbeMatrixEffect(spriteBatch, drawPos, context);
                    break;
            }
        }

        /// <summary>
        /// 冲刺蓄力：大范围光晕+明显收缩圆环+粗瞄准线
        /// </summary>
        private static void DrawDashChargeEffect(SpriteBatch spriteBatch, Vector2 drawPos, DestroyerStateContext context) {
            Texture2D glowTex = CWRAsset.SoftGlow.Value;
            Texture2D circleTex = CWRAsset.DiffusionCircle.Value;
            Texture2D lineTex = CWRAsset.LightShot.Value;
            float progress = context.ChargeProgress;
            Color chargeColor = Color.Lerp(Color.OrangeRed, Color.Red, progress);

            BeginAdditive(spriteBatch);

            //大范围外圈光晕
            float outerScale = 3f + progress * 3f;
            Main.EntitySpriteDraw(glowTex, drawPos, null, chargeColor * (progress * 0.4f),
                0f, glowTex.Size() / 2f, outerScale, SpriteEffects.None, 0);

            //内圈强光
            float innerScale = 1.5f + progress * 2f;
            Main.EntitySpriteDraw(glowTex, drawPos, null, Color.White * (progress * 0.5f),
                0f, glowTex.Size() / 2f, innerScale, SpriteEffects.None, 0);

            //收缩圆环（多层，大尺寸）
            for (int i = 0; i < 3; i++) {
                float phase = (progress + i * 0.12f) % 1f;
                float ringScale = (3.5f - phase * 3f) * (1f + i * 0.15f);
                float ringAlpha = phase * (1f - phase) * 1.2f;
                Main.EntitySpriteDraw(circleTex, drawPos, null, chargeColor * ringAlpha,
                    Main.GlobalTimeWrappedHourly * (3f + i), circleTex.Size() / 2f, ringScale, SpriteEffects.None, 0);
            }

            //瞄准线（用LightShot纹理绘制连续线段）
            if (progress > 0.3f && context.DashDirection != Vector2.Zero) {
                DrawAimLine(drawPos, context.DashDirection, chargeColor, progress, lineTex, glowTex);
            }

            EndAdditive(spriteBatch);
        }

        /// <summary>
        /// 粗瞄准线：用LightShot绘制连续可见线段+末端光点
        /// </summary>
        private static void DrawAimLine(Vector2 drawPos, Vector2 direction, Color baseColor, float progress,
            Texture2D lineTex, Texture2D glowTex) {
            float aimProgress = (progress - 0.3f) / 0.7f;
            float lineLength = 600f * aimProgress;
            int segments = (int)(lineLength / 10f);
            float lineRotation = direction.ToRotation();

            for (int i = 0; i < segments; i++) {
                float t = i / (float)Math.Max(segments, 1);
                Vector2 segPos = drawPos + direction * (30f + t * lineLength);
                float segAlpha = aimProgress * (1f - t * 0.6f) * 0.8f;
                float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f + t * 8f);

                Main.EntitySpriteDraw(lineTex, segPos, null, baseColor * segAlpha * pulse,
                    lineRotation, new Vector2(0, lineTex.Height / 2f),
                    new Vector2(0.5f, 0.3f * (1f - t * 0.3f)), SpriteEffects.None, 0);
            }

            //末端光点
            Vector2 tipPos = drawPos + direction * (30f + lineLength);
            Main.EntitySpriteDraw(glowTex, tipPos, null, baseColor * aimProgress * 0.7f,
                0f, glowTex.Size() / 2f, 0.8f, SpriteEffects.None, 0);
        }

        /// <summary>
        /// 激光弹幕充能：大范围中心发光+多层脉冲波纹
        /// </summary>
        private static void DrawLaserChargeEffect(SpriteBatch spriteBatch, Vector2 drawPos, DestroyerStateContext context) {
            Texture2D glowTex = CWRAsset.SoftGlow.Value;
            Texture2D circleTex = CWRAsset.DiffusionCircle.Value;
            float progress = context.ChargeProgress;
            Color chargeColor = Color.IndianRed;

            BeginAdditive(spriteBatch);

            //大范围光晕
            float coreScale = 2f + progress * 3f;
            Main.EntitySpriteDraw(glowTex, drawPos, null, chargeColor * (progress * 0.6f),
                0f, glowTex.Size() / 2f, coreScale, SpriteEffects.None, 0);

            //白色内核
            float innerScale = 0.8f + progress * 1.5f;
            Main.EntitySpriteDraw(glowTex, drawPos, null, Color.White * (progress * 0.4f),
                0f, glowTex.Size() / 2f, innerScale, SpriteEffects.None, 0);

            //多层脉冲波纹（增大尺寸和透明度）
            for (int i = 0; i < 3; i++) {
                float pulsePhase = (Main.GlobalTimeWrappedHourly * 2.5f + i * 0.33f) % 1f;
                float pulseScale = 1f + pulsePhase * 3.5f;
                float pulseAlpha = (1f - pulsePhase) * progress * 0.6f;
                Main.EntitySpriteDraw(circleTex, drawPos, null, chargeColor * pulseAlpha,
                    0f, circleTex.Size() / 2f, pulseScale, SpriteEffects.None, 0);
            }

            //放射线（像探照灯一样从中心射出）
            if (progress > 0.4f) {
                float rayAlphaBase = (progress - 0.4f) / 0.6f;
                int rayCount = 8;
                float rayRotation = Main.GlobalTimeWrappedHourly * 1.5f;
                Texture2D lineTex = CWRAsset.LightShot.Value;

                for (int i = 0; i < rayCount; i++) {
                    float angle = MathHelper.TwoPi / rayCount * i + rayRotation;
                    Vector2 rayDir = angle.ToRotationVector2();

                    int segs = 8;
                    for (int j = 0; j < segs; j++) {
                        float t = j / (float)segs;
                        Vector2 rayPos = drawPos + rayDir * (40f + t * 160f * rayAlphaBase);
                        float rayAlpha = (1f - t) * rayAlphaBase * 0.6f;
                        Main.EntitySpriteDraw(lineTex, rayPos, null, chargeColor * rayAlpha,
                            angle, new Vector2(0, lineTex.Height / 2f),
                            new Vector2(0.4f, 0.15f * (1f - t * 0.5f)), SpriteEffects.None, 0);
                    }
                }
            }

            EndAdditive(spriteBatch);
        }

        /// <summary>
        /// 包围特效：以玩家为中心的收缩警告环（玩家视角可见）
        /// </summary>
        private static void DrawEncircleEffect(SpriteBatch spriteBatch, DestroyerStateContext context) {
            Texture2D circleTex = CWRAsset.DiffusionCircle.Value;
            Texture2D glowTex = CWRAsset.SoftGlow.Value;
            float progress = context.ChargeProgress;
            Color chargeColor = Color.Lerp(Color.DarkRed, Color.OrangeRed, progress);

            //以玩家为中心绘制，这样玩家能看到警告
            Vector2 centerPos = context.Target != null
                ? context.Target.Center - Main.screenPosition
                : context.Npc.Center - Main.screenPosition;
            Vector2 headPos = context.Npc.Center - Main.screenPosition;

            BeginAdditive(spriteBatch);

            //玩家周围的收缩警告环
            for (int i = 0; i < 3; i++) {
                float layerProgress = (progress + i * 0.15f) % 1f;
                float layerScale = (5f - layerProgress * 4f) * (1f + i * 0.1f);
                float layerAlpha = layerProgress * (1f - layerProgress) * 1.0f;
                Main.EntitySpriteDraw(circleTex, centerPos, null, chargeColor * layerAlpha,
                    Main.GlobalTimeWrappedHourly * (2f + i), circleTex.Size() / 2f, layerScale, SpriteEffects.None, 0);
            }

            //头部位置光晕
            float headGlowScale = 2f + progress * 2.5f;
            Main.EntitySpriteDraw(glowTex, headPos, null, chargeColor * (progress * 0.5f),
                0f, glowTex.Size() / 2f, headGlowScale, SpriteEffects.None, 0);

            //旋转的警告标记点（围绕玩家）
            if (progress > 0.2f) {
                float markerProgress = (progress - 0.2f) / 0.8f;
                int markerCount = 8;
                float markerRadius = 200f * (1f - markerProgress * 0.4f);
                float rot = Main.GlobalTimeWrappedHourly * 3f;

                for (int i = 0; i < markerCount; i++) {
                    float angle = MathHelper.TwoPi / markerCount * i + rot;
                    Vector2 markerPos = centerPos + angle.ToRotationVector2() * markerRadius;
                    float pulse = 0.7f + 0.3f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f + i);
                    Main.EntitySpriteDraw(glowTex, markerPos, null, chargeColor * (markerProgress * 0.7f * pulse),
                        0f, glowTex.Size() / 2f, 0.6f + markerProgress * 0.4f, SpriteEffects.None, 0);
                }
            }

            EndAdditive(spriteBatch);
        }

        /// <summary>
        /// 探针阵列特效：大范围能量核+可见放射线+旋转外环
        /// </summary>
        private static void DrawProbeMatrixEffect(SpriteBatch spriteBatch, Vector2 drawPos, DestroyerStateContext context) {
            Texture2D glowTex = CWRAsset.SoftGlow.Value;
            Texture2D circleTex = CWRAsset.DiffusionCircle.Value;
            Texture2D lineTex = CWRAsset.LightShot.Value;
            float progress = context.ChargeProgress;
            Color chargeColor = Color.Lerp(Color.Red, Color.MediumVioletRed, progress);

            BeginAdditive(spriteBatch);

            //大范围能量核光晕
            float coreScale = 2.5f + progress * 3f;
            Main.EntitySpriteDraw(glowTex, drawPos, null, chargeColor * (progress * 0.6f),
                0f, glowTex.Size() / 2f, coreScale, SpriteEffects.None, 0);

            //白色内核
            float innerScale = 1f + progress * 1.5f;
            Main.EntitySpriteDraw(glowTex, drawPos, null, Color.White * (progress * 0.35f),
                0f, glowTex.Size() / 2f, innerScale, SpriteEffects.None, 0);

            //粗放射线
            if (progress > 0.2f) {
                float rayProgress = (progress - 0.2f) / 0.8f;
                int rayCount = 6;
                float rayLength = 200f * rayProgress;
                float rayRotation = Main.GlobalTimeWrappedHourly * 2f;

                for (int i = 0; i < rayCount; i++) {
                    float angle = MathHelper.TwoPi / rayCount * i + rayRotation;
                    Vector2 rayDir = angle.ToRotationVector2();

                    int segs = 10;
                    for (int j = 0; j < segs; j++) {
                        float t = j / (float)segs;
                        Vector2 rayPos = drawPos + rayDir * (30f + t * rayLength);
                        float rayAlpha = (1f - t) * rayProgress * 0.7f;

                        Main.EntitySpriteDraw(lineTex, rayPos, null, chargeColor * rayAlpha,
                            angle, new Vector2(0, lineTex.Height / 2f),
                            new Vector2(0.5f, 0.25f * (1f - t * 0.4f)), SpriteEffects.None, 0);
                    }

                    //射线末端光点
                    Vector2 tipPos = drawPos + rayDir * (30f + rayLength);
                    float tipPulse = 0.6f + 0.4f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f + i);
                    Main.EntitySpriteDraw(glowTex, tipPos, null, chargeColor * (rayProgress * 0.6f * tipPulse),
                        0f, glowTex.Size() / 2f, 0.7f, SpriteEffects.None, 0);
                }
            }

            //双层旋转外环
            if (progress > 0.4f) {
                float ringProgress = (progress - 0.4f) / 0.6f;
                for (int i = 0; i < 2; i++) {
                    float ringScale = (1.5f + ringProgress * 2f) * (1f + i * 0.3f);
                    float ringAlpha = ringProgress * 0.5f * (1f - i * 0.3f);
                    float ringRot = Main.GlobalTimeWrappedHourly * (4f + i * 2f) * (i == 0 ? 1 : -1);
                    Main.EntitySpriteDraw(circleTex, drawPos, null, chargeColor * ringAlpha,
                        ringRot, circleTex.Size() / 2f, ringScale, SpriteEffects.None, 0);
                }
            }

            EndAdditive(spriteBatch);
        }

        /// <summary>
        /// 绘制冲刺残影
        /// </summary>
        public static void DrawDashTrail(SpriteBatch spriteBatch, NPC npc, Texture2D texture,
            Rectangle frameRec, Vector2 origin, Vector2 screenPos) {
            for (int i = 0; i < npc.oldPos.Length; i++) {
                if (npc.oldPos[i] == Vector2.Zero) continue;
                float trailFade = 1f - i / (float)npc.oldPos.Length;
                Vector2 drawPos = npc.oldPos[i] - screenPos + npc.Size / 2;
                Color trailColor = Color.Lerp(Color.OrangeRed, Color.DarkRed, i / (float)npc.oldPos.Length) * (0.5f * trailFade);
                float trailScale = npc.scale * (0.9f + 0.1f * trailFade);
                spriteBatch.Draw(texture, drawPos, frameRec, trailColor,
                    npc.rotation + MathHelper.Pi, origin, trailScale, SpriteEffects.None, 0f);
            }
        }
    }
}
