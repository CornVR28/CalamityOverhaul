using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.Common
{
    /// <summary>
    /// 机械Boss通用热感滤镜渲染器
    /// <br/>实现"机械Boss三态滤镜"：常态描边 / 警告脉冲 / 冲刺白热
    /// <br/>抽取自毁灭者最初的实现，所有机械Boss（毁灭者、机械骷髅王、双子魔眼）共用。
    /// <br/>使用方法：先调用 <see cref="DrawOutlineHalo"/> 绘制外圈描边光环，
    /// 再调用 <see cref="BeginThermalShader"/> 切到 Immediate 模式套上着色器，绘制本体后 <see cref="EndThermalShader"/> 还原。
    /// </summary>
    internal static class MechBossThermalRenderer
    {
        #region 8方向描边光环

        /// <summary>
        /// 各状态下描边的主色 + 高光色
        /// </summary>
        private static Color GetHaloColor(MechBossVisualMode mode, float progress, float pulse) {
            return mode switch {
                MechBossVisualMode.Dashing =>
                    Color.Lerp(new Color(255, 110, 30), new Color(255, 240, 170), 0.45f + 0.55f * pulse),
                MechBossVisualMode.Warning =>
                    Color.Lerp(new Color(220, 40, 10), new Color(255, 210, 60), 0.35f + 0.55f * pulse * progress),
                _ =>
                    Color.Lerp(new Color(150, 30, 10), new Color(220, 80, 25), 0.40f + 0.30f * pulse),
            };
        }

        private static float GetHaloStrength(MechBossVisualMode mode, float progress, float intensity) {
            float baseStrength = mode switch {
                MechBossVisualMode.Dashing => 1.0f,
                MechBossVisualMode.Warning => 0.55f + 0.45f * progress,
                _ => 0.35f,
            };
            return baseStrength * intensity;
        }

        private static float GetHaloRadius(MechBossVisualMode mode, float progress) {
            return mode switch {
                MechBossVisualMode.Dashing => 5.5f,
                MechBossVisualMode.Warning => 3.5f + 2.5f * progress,
                _ => 2.0f,
            };
        }

        /// <summary>
        /// 8方向偏移描边光环——把贴图沿圆周偏移多次叠加，形成一圈彩色描边
        /// <br/>这一层独立于着色器存在，因为它是真正"在贴图外"绘制的，
        /// 不受单帧透明像素空间或多帧 UV 边界限制，远距离也能看清整个机械Boss轮廓。
        /// </summary>
        public static void DrawOutlineHalo(SpriteBatch spriteBatch, Texture2D texture, Vector2 drawPos,
            Rectangle? sourceRect, float rotation, Vector2 origin, float scale, SpriteEffects effects,
            MechBossVisualMode mode, float intensity, float progress) {
            if (intensity <= 0.01f) return;

            float pulseSpeed = mode == MechBossVisualMode.Idle ? 2.2f : 6.5f;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * pulseSpeed);
            float radius = GetHaloRadius(mode, progress);
            float strength = GetHaloStrength(mode, progress, intensity);
            Color haloColor = GetHaloColor(mode, progress, pulse) * strength;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            int steps = mode == MechBossVisualMode.Idle ? 6 : 8;
            for (int i = 0; i < steps; i++) {
                float angle = MathHelper.TwoPi / steps * i;
                Vector2 offset = angle.ToRotationVector2() * radius;
                spriteBatch.Draw(texture, drawPos + offset, sourceRect, haloColor,
                    rotation, origin, scale, effects, 0f);
            }

            //冲刺时再叠一圈更宽更浅的外晕，强化"高速过热"视觉感
            if (mode == MechBossVisualMode.Dashing) {
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

        #endregion

        #region 机械热感像素着色器

        /// <summary>
        /// 切到 Immediate 模式并启用机械热感着色器，调用方在绘制完后必须调用 <see cref="EndThermalShader"/> 还原
        /// </summary>
        /// <param name="sourceRect">当前帧 sourceRectangle，用于将 UV 边界写入 shader，避免越帧采样</param>
        public static bool BeginThermalShader(SpriteBatch spriteBatch, Texture2D texture, Rectangle sourceRect,
            MechBossVisualMode mode, float intensity, float progress, float seed = 0f) {
            if (intensity <= 0.01f) return false;

            Effect shader = EffectLoader.DestroyerThermalOutline?.Value;
            if (shader == null) return false;

            float invW = 1f / texture.Width;
            float invH = 1f / texture.Height;
            //内缩半像素：贴帧边的采样仍属本帧，但越界采样不会触及相邻帧的首列/首行
            Vector4 frameUV = new Vector4(
                (sourceRect.X + 0.5f) * invW,
                (sourceRect.Y + 0.5f) * invH,
                (sourceRect.X + sourceRect.Width - 0.5f) * invW,
                (sourceRect.Y + sourceRect.Height - 0.5f) * invH);

            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["intensity"]?.SetValue(intensity);
            shader.Parameters["mode"]?.SetValue((float)mode);
            shader.Parameters["progress"]?.SetValue(progress);
            shader.Parameters["texelSize"]?.SetValue(new Vector2(invW, invH));
            shader.Parameters["frameUV"]?.SetValue(frameUV);
            shader.Parameters["seed"]?.SetValue(seed);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();
            return true;
        }

        /// <summary>
        /// 还原默认的 Deferred AlphaBlend SpriteBatch（与 <see cref="BeginThermalShader"/> 配对使用）
        /// </summary>
        public static void EndThermalShader(SpriteBatch spriteBatch) {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        #endregion

        #region NPC ID 状态查询便捷重载

        /// <summary>
        /// 通过 <see cref="MechBossVisualState"/> 自动查询某控制器（Boss头部）的当前视觉状态后绘制描边光环
        /// </summary>
        public static void DrawOutlineHaloByController(SpriteBatch spriteBatch, Texture2D texture, Vector2 drawPos,
            Rectangle? sourceRect, float rotation, Vector2 origin, float scale, SpriteEffects effects,
            int controllerNpcId) {
            var (mode, intensity, progress) = MechBossVisualState.Read(controllerNpcId);
            DrawOutlineHalo(spriteBatch, texture, drawPos, sourceRect, rotation, origin, scale, effects,
                mode, intensity, progress);
        }

        /// <summary>
        /// 通过 <see cref="MechBossVisualState"/> 自动查询某控制器（Boss头部）的当前视觉状态后启用着色器
        /// </summary>
        public static bool BeginThermalShaderByController(SpriteBatch spriteBatch, Texture2D texture, Rectangle sourceRect,
            int controllerNpcId, float seed = 0f) {
            var (mode, intensity, progress) = MechBossVisualState.Read(controllerNpcId);
            return BeginThermalShader(spriteBatch, texture, sourceRect, mode, intensity, progress, seed);
        }

        #endregion
    }
}
