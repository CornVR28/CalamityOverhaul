using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TimeShift;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures
{
    /// <summary>
    /// 虚空聚落建筑的时空扭曲/崩解绘制助手
    /// 绑定ArchitectureWarp.fx着色器，把建筑/连接段贴图作为单次Draw整体送GPU处理
    /// 崩解蒙版、行撕裂、块错位、RGB分离、扫描线等所有视觉均在GPU完成
    /// </summary>
    internal static class ArchitectureWarpDraw
    {
        /// <summary>
        /// 每帧推进可见度向目标值逼近，在过去时目标为1，在现在时目标为0
        /// </summary>
        public static float TickVisibility(ref float visibility) {
            float target = VoidTimeShiftSystem.InPast ? 1f : 0f;
            //略慢于切换演出的淡入淡出，让建筑比滤镜稍晚完成凝结/崩解
            const float ease = 0.025f;
            if (visibility < target) visibility = Math.Min(target, visibility + ease);
            else if (visibility > target) visibility = Math.Max(target, visibility - ease);
            return visibility;
        }

        /// <summary>完全隐形且无演出时直接跳过绘制</summary>
        public static bool ShouldDraw(float visibility) {
            if (visibility > 0.001f) return true;
            return VoidTimeShiftSystem.TransitionStrength > 0.001f;
        }

        /// <summary>切换演出强度，驱动瞬时扰动；平稳时为0</summary>
        public static float ComputeWarp() => VoidTimeShiftSystem.TransitionStrength;

        /// <summary>
        /// 使用ArchitectureWarp.fx绘制一张贴图
        /// 内部会End当前SpriteBatch并以Immediate模式重开，绘完后恢复默认批次配置
        /// </summary>
        /// <param name="spriteBatch">当前Actor绘制用的spriteBatch</param>
        /// <param name="tex">建筑或连接段贴图</param>
        /// <param name="drawPos">贴图左上角的屏幕像素坐标（调用方自行减去screenPosition）</param>
        /// <param name="visibility">可见度0到1</param>
        /// <param name="warpStrength">切换演出额外扰动，0到1</param>
        public static void DrawWithShader(SpriteBatch spriteBatch, Texture2D tex,
            Vector2 drawPos, float visibility, float warpStrength) {
            if (tex == null) return;
            Effect shader = EffectLoader.ArchitectureWarp?.Value;
            //完全显示且无切换演出时直接普通绘制，避免稳定期持续闪烁
            bool stableVisible = visibility >= 0.999f && warpStrength <= 0.001f;
            if (shader == null || stableVisible) {
                spriteBatch.Draw(tex, drawPos, null, Color.White * visibility, 0f,
                    Vector2.Zero, 1f, SpriteEffects.None, 0f);
                return;
            }

            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["visibility"]?.SetValue(MathHelper.Clamp(visibility, 0f, 1f));
            shader.Parameters["warpStrength"]?.SetValue(MathHelper.Clamp(warpStrength, 0f, 1f));
            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);

            //整张贴图一次性送给着色器，所有时空效果在像素着色器中完成
            spriteBatch.Draw(tex, drawPos, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
