using CalamityOverhaul.Common;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.Industrials.Generator.Thermal
{
    /// <summary>
    /// 热能发电机热浪扭曲后处理——在EndCapture阶段对屏幕施加基于发电机位置的局部热浪UV偏移
    /// 支持最多8个热源同时渲染
    /// </summary>
    internal class ThermalHeatHazeRender : RenderHandle
    {
        private struct HeatSource
        {
            public Vector2 WorldPos;
            public float TemperatureRatio;
        }

        private const int MaxSources = 8;
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

            // 缩放修正
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Vector2 screenCenter = new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);

            // 收集所有可见热源（按强度排序，取前MaxSources个）
            Vector2[] centers = new Vector2[MaxSources];
            float[] intensities = new float[MaxSources];
            int count = 0;

            // 先按温度比降序排列，优先保留强的热源
            pendingSources.Sort((a, b) => b.TemperatureRatio.CompareTo(a.TemperatureRatio));

            foreach (var src in pendingSources) {
                if (count >= MaxSources)
                    break;

                // 世界坐标 → 应用缩放的屏幕坐标
                Vector2 rawScreen = src.WorldPos - Main.screenPosition;
                Vector2 screenPos = (rawScreen - screenCenter) * zoom + screenCenter;

                // 跳过不在屏幕范围内的热源（留出余量，因为热浪向上扩散）
                if (screenPos.X < -200 || screenPos.X > Main.screenWidth + 200 ||
                    screenPos.Y < -400 || screenPos.Y > Main.screenHeight + 100)
                    continue;

                if (src.TemperatureRatio < 0.01f)
                    continue;

                // 强度映射：温度比 0.1~1.0 → 视觉强度 0.08~0.55
                float visualIntensity = MathHelper.Lerp(0.08f, 0.55f, MathHelper.Clamp(src.TemperatureRatio, 0f, 1f));

                centers[count] = new Vector2(screenPos.X / Main.screenWidth, screenPos.Y / Main.screenHeight);
                intensities[count] = visualIntensity;
                count++;
            }

            if (count == 0)
                goto cleanup;

            Effect shader = EffectLoader.ThermalHeatHaze.Value;

            // ① 复制当前屏幕到 screenSwap
            gd.SetRenderTarget(screenSwap);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            sb.End();

            // ② 设置着色器参数
            shader.Parameters["screenSize"]?.SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
            shader.Parameters["hazeCenters"]?.SetValue(centers);
            shader.Parameters["hazeIntensities"]?.SetValue(intensities);
            shader.Parameters["sourceCount"]?.SetValue(count);
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
