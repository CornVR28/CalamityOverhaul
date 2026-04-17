using CalamityOverhaul.Common;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.Industrials.Generator.Thermal
{
    /// <summary>
    /// 热能发电机热浪扭曲后处理——在EndCapture阶段对屏幕施加基于发电机位置的局部热浪UV偏移
    /// 支持多个热源，选取最强的一个进行渲染
    /// </summary>
    internal class ThermalHeatHazeRender : RenderHandle
    {
        private struct HeatSource
        {
            public Vector2 WorldPos;
            public float TemperatureRatio;
        }

        private static readonly List<HeatSource> pendingSources = new();

        /// <summary>
        /// 由 ThermalGeneratorTP 每帧调用，注册活跃热源
        /// </summary>
        internal static void RegisterHeatSource(Vector2 worldPos, float temperatureRatio) {
            pendingSources.Add(new HeatSource {
                WorldPos = worldPos,
                TemperatureRatio = temperatureRatio
            });
        }

        public override float Weight => 1.04f;

        public override void EndCaptureDraw(SpriteBatch sb, GraphicsDevice gd, RenderTarget2D screenSwap) {
            if (pendingSources.Count == 0)
                return;
            if (EffectLoader.ThermalHeatHaze == null || !EffectLoader.ThermalHeatHaze.IsLoaded)
                goto cleanup;
            if (screenSwap == null || Main.screenTarget == null)
                goto cleanup;

            // 选取视野内最强的热源
            float bestScore = 0f;
            Vector2 bestScreenNorm = Vector2.Zero;
            float bestIntensity = 0f;

            foreach (var src in pendingSources) {
                Vector2 screenPos = src.WorldPos - Main.screenPosition;
                // 跳过不在屏幕范围内的热源（留出余量，因为热浪向上扩散）
                if (screenPos.X < -200 || screenPos.X > Main.screenWidth + 200 ||
                    screenPos.Y < -400 || screenPos.Y > Main.screenHeight + 100)
                    continue;

                float distToCenter = Vector2.Distance(screenPos,
                    new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f));
                // 越近、温度越高 → 优先级越高
                float score = src.TemperatureRatio / (1f + distToCenter * 0.001f);
                if (score > bestScore) {
                    bestScore = score;
                    bestScreenNorm = new Vector2(screenPos.X / Main.screenWidth, screenPos.Y / Main.screenHeight);
                    bestIntensity = src.TemperatureRatio;
                }
            }

            if (bestIntensity < 0.05f)
                goto cleanup;

            // 强度映射：温度比 0.1~1.0 → 视觉强度 0.08~0.55
            float visualIntensity = MathHelper.Lerp(0.08f, 0.55f, MathHelper.Clamp(bestIntensity, 0f, 1f));

            Effect shader = EffectLoader.ThermalHeatHaze.Value;

            // ① 复制当前屏幕到 screenSwap
            gd.SetRenderTarget(screenSwap);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            sb.End();

            // ② 设置着色器参数
            shader.Parameters["screenSize"]?.SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
            shader.Parameters["hazeCenter"]?.SetValue(bestScreenNorm);
            shader.Parameters["hazeIntensity"]?.SetValue(visualIntensity);
            shader.Parameters["globalTime"]?.SetValue((float)Main.timeForVisualEffects * 0.018f);
            shader.Parameters["uNoise"]?.SetValue(CWRAsset.Extra_193.Value);

            // ③ 用着色器把 screenSwap 画回 Main.screenTarget
            gd.SetRenderTarget(Main.screenTarget);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            shader.CurrentTechnique.Passes[0].Apply();
            sb.Draw(screenSwap, Vector2.Zero, Color.White);
            sb.End();

        cleanup:
            pendingSources.Clear();
        }
    }
}
