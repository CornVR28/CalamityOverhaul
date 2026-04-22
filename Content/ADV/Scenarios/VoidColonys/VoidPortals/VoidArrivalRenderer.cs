using CalamityOverhaul.Common;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.VoidPortals
{
    /// <summary>
    /// 虚空抵达门专用屏幕空间渲染器
    /// 结合：1) VoidArrival.fx 全屏后处理  2) 顶点绘制的径向能量光刃  3) Additive 辉光环
    /// </summary>
    internal class VoidArrivalRenderer : RenderHandle
    {
        [VaultLoaden(CWRConstant.Masking + "Noise2")]
        private static Asset<Texture2D> noise2;
        [VaultLoaden(CWRConstant.Masking + "SoftGlow")]
        private static Asset<Texture2D> softGlow;
        [VaultLoaden(CWRConstant.Masking + "StarTexture")]
        private static Asset<Texture2D> starTex;

        public override float Weight => 1.35f;

        public override void DrawNPCsOverTiles(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice,
            RenderTarget2D screenSwap) {
            if (Main.gameMenu) return;
            var portal = VoidArrivalPortal.ValidateActiveInstance();
            if (portal == null) return;
            if (portal.Intensity < 0.001f || portal.ExpandProgress < 0.001f) return;

            ApplyFullScreenShader(spriteBatch, graphicsDevice, screenSwap, portal);

            //Additive 辉光层
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            DrawEdgeGlowRing(spriteBatch, portal);
            DrawStarBurst(spriteBatch, portal);
            spriteBatch.End();

            //顶点绘制的径向能量光刃
            DrawRadialBlades(graphicsDevice, portal);
        }

        private static void ApplyFullScreenShader(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice,
            RenderTarget2D screenSwap, VoidArrivalPortal portal) {
            Effect shader = EffectLoader.VoidArrival?.Value;
            Texture2D noiseTex = noise2?.Value;
            if (shader == null || noiseTex == null) return;
            if (screenSwap == null || screenSwap.IsDisposed) return;
            if (Main.screenTarget == null || Main.screenTarget.IsDisposed) return;

            graphicsDevice.SetRenderTarget(screenSwap);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Vector2 screenPixels = Main.ScreenSize.ToVector2();
            Vector2 worldViewSize = screenPixels / zoom;
            Vector2 worldViewOrigin = Main.screenPosition
                + screenPixels * (Vector2.One - Vector2.One / zoom) * 0.5f;

            shader.Parameters["uTime"]?.SetValue(portal.EffectTime);
            shader.Parameters["intensity"]?.SetValue(portal.Intensity);
            shader.Parameters["expandProgress"]?.SetValue(portal.ExpandProgress);
            shader.Parameters["ejectBurst"]?.SetValue(portal.EjectBurst);
            shader.Parameters["portalRadius"]?.SetValue(VoidArrivalPortal.BaseRadius);
            shader.Parameters["seed"]?.SetValue(portal.Seed);
            shader.Parameters["shockTime0"]?.SetValue(portal.ShockTime0);
            shader.Parameters["shockTime1"]?.SetValue(portal.ShockTime1);
            shader.Parameters["shockTime2"]?.SetValue(portal.ShockTime2);
            shader.Parameters["portalCenter"]?.SetValue(portal.Center);
            shader.Parameters["screenPosition"]?.SetValue(worldViewOrigin);
            shader.Parameters["worldViewSize"]?.SetValue(worldViewSize);

            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            graphicsDevice.Textures[1] = noiseTex;
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            shader.CurrentTechnique.Passes[0].Apply();
            spriteBatch.Draw(screenSwap, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        /// <summary>沿门边缘分布的脉动辉光环</summary>
        private static void DrawEdgeGlowRing(SpriteBatch spriteBatch, VoidArrivalPortal portal) {
            Texture2D glow = softGlow?.Value;
            if (glow == null) return;

            Vector2 center = portal.Center;
            float radius = VoidArrivalPortal.BaseRadius * portal.ExpandProgress;
            if (radius < 20f) return;

            float time = portal.EffectTime;
            float intensity = portal.Intensity;
            Vector2 origin = new Vector2(glow.Width * 0.5f, glow.Height * 0.5f);

            int count = Math.Clamp((int)(radius * 0.4f), 32, 120);
            for (int i = 0; i < count; i++) {
                float t = (float)i / count;
                float a = t * MathHelper.TwoPi;
                Vector2 pos = center + a.ToRotationVector2() * radius;

                //屏幕剔除
                Vector2 screenP = pos - Main.screenPosition;
                if (screenP.X < -120 || screenP.X > Main.screenWidth + 120 ||
                    screenP.Y < -120 || screenP.Y > Main.screenHeight + 120) continue;

                float pulse = 0.35f + 0.65f * MathF.Sin(time * 2.6f + i * 0.37f);
                float alpha = pulse * intensity * 0.45f;
                float scale = (26f + 10f * pulse) / glow.Width;
                Color col = new Color(0.95f * alpha, 0.22f * alpha, 0.08f * alpha, 0f);
                spriteBatch.Draw(glow, pos - Main.screenPosition, null, col, 0f, origin, scale, SpriteEffects.None, 0f);
            }

            //中心核心辉光
            float coreScale = (radius * 0.9f) / glow.Width;
            float corePulse = 0.7f + 0.3f * MathF.Sin(time * 1.4f);
            Color coreCol = new Color(0.55f, 0.08f, 0.03f, 0f) * (intensity * corePulse);
            spriteBatch.Draw(glow, center - Main.screenPosition, null, coreCol, 0f, origin, coreScale, SpriteEffects.None, 0f);
        }

        /// <summary>中心星形爆裂，抛出瞬间更强</summary>
        private static void DrawStarBurst(SpriteBatch spriteBatch, VoidArrivalPortal portal) {
            Texture2D star = starTex?.Value;
            if (star == null) return;
            Vector2 center = portal.Center - Main.screenPosition;

            float baseScale = (VoidArrivalPortal.BaseRadius * portal.ExpandProgress * 2.4f) / star.Width;
            float basePulse = 0.6f + 0.4f * MathF.Sin(portal.EffectTime * 1.8f);
            Color baseCol = new Color(1.0f, 0.55f, 0.22f, 0f) * (portal.Intensity * basePulse * 0.55f);
            spriteBatch.Draw(star, center, null, baseCol, portal.EffectTime * 0.3f, star.Size() * 0.5f, baseScale, SpriteEffects.None, 0f);

            //抛出闪光叠加
            if (portal.EjectBurst > 0.01f) {
                float ebScale = (VoidArrivalPortal.BaseRadius * 4.2f) / star.Width * portal.EjectBurst;
                Color ebCol = new Color(1.0f, 0.9f, 0.7f, 0f) * (portal.EjectBurst * portal.EjectBurst * 1.2f);
                spriteBatch.Draw(star, center, null, ebCol, -portal.EffectTime * 0.6f, star.Size() * 0.5f, ebScale, SpriteEffects.None, 0f);
                spriteBatch.Draw(star, center, null, ebCol * 0.7f, portal.EffectTime * 0.45f + MathHelper.PiOver4, star.Size() * 0.5f, ebScale * 0.75f, SpriteEffects.None, 0f);
            }
        }

        /// <summary>使用顶点 TriangleList 绘制从门向外喷射的能量光刃</summary>
        private static void DrawRadialBlades(GraphicsDevice device, VoidArrivalPortal portal) {
            if (portal.Intensity < 0.08f || portal.ExpandProgress < 0.2f) return;

            Effect effect = EffectLoader.GradientTrail?.Value;
            Texture2D whiteTex = softGlow?.Value;
            if (effect == null || whiteTex == null) return;

            float radius = VoidArrivalPortal.BaseRadius * portal.ExpandProgress;
            float bladeInner = radius * 0.88f;
            float bladeOuterBase = radius * 2.2f;
            Vector2 center = portal.Center;

            //光刃数量与长度脉动
            const int bladeCount = 14;
            float time = portal.EffectTime;

            var verts = new List<VertexPositionColorTexture>(bladeCount * 6);
            for (int i = 0; i < bladeCount; i++) {
                //每刃有独立相位
                float phase = i * 0.617f + portal.Seed * 0.1f;
                float angle = i * MathHelper.TwoPi / bladeCount + time * 0.12f
                    + MathF.Sin(time * 0.7f + phase) * 0.08f;
                //长度呼吸：部分刃更长
                float lenPulse = 0.55f + 0.45f * MathF.Sin(time * 1.6f + phase * 2.1f);
                float bladeOuter = MathHelper.Lerp(bladeInner + 60f, bladeOuterBase, lenPulse);
                if (portal.EjectBurst > 0.2f) {
                    bladeOuter += 380f * portal.EjectBurst;
                }

                //宽度（内宽，外尖）
                float innerHalfW = MathHelper.Lerp(8f, 28f, lenPulse);
                float outerHalfW = 2f;

                Vector2 dir = angle.ToRotationVector2();
                Vector2 perp = new Vector2(-dir.Y, dir.X);

                Vector2 innerP = center + dir * bladeInner;
                Vector2 outerP = center + dir * bladeOuter;

                Vector2 v0 = innerP + perp * innerHalfW;
                Vector2 v1 = innerP - perp * innerHalfW;
                Vector2 v2 = outerP + perp * outerHalfW;
                Vector2 v3 = outerP - perp * outerHalfW;

                //根部热白，尖部深红透明
                float bladeAlpha = MathHelper.Clamp(portal.Intensity * (0.55f + 0.45f * lenPulse), 0f, 1f);
                if (portal.EjectBurst > 0.01f) bladeAlpha = MathHelper.Min(1f, bladeAlpha + portal.EjectBurst);
                Color innerCol = new Color(1.0f, 0.75f, 0.40f, 0f) * bladeAlpha;
                Color outerCol = new Color(0.85f, 0.08f, 0.04f, 0f) * (bladeAlpha * 0.15f);

                //两个三角形：v0-v1-v2 与 v1-v3-v2
                verts.Add(new VertexPositionColorTexture(new Vector3(v0, 0f), innerCol, new Vector2(0f, 0f)));
                verts.Add(new VertexPositionColorTexture(new Vector3(v1, 0f), innerCol, new Vector2(0f, 1f)));
                verts.Add(new VertexPositionColorTexture(new Vector3(v2, 0f), outerCol, new Vector2(1f, 0f)));
                verts.Add(new VertexPositionColorTexture(new Vector3(v1, 0f), innerCol, new Vector2(0f, 1f)));
                verts.Add(new VertexPositionColorTexture(new Vector3(v3, 0f), outerCol, new Vector2(1f, 1f)));
                verts.Add(new VertexPositionColorTexture(new Vector3(v2, 0f), outerCol, new Vector2(1f, 0f)));
            }

            if (verts.Count < 3) return;

            var originalRaster = device.RasterizerState;
            var originalBlend = device.BlendState;
            var originalSampler = device.SamplerStates[0];

            device.BlendState = BlendState.Additive;
            device.SamplerStates[0] = SamplerState.LinearClamp;
            device.RasterizerState = RasterizerState.CullNone;

            effect.Parameters["transformMatrix"]?.SetValue(VaultUtils.GetTransfromMatrix());
            effect.Parameters["uTime"]?.SetValue(portal.EffectTime * 0.2f);
            effect.Parameters["uTimeG"]?.SetValue(portal.EffectTime * 0.4f);
            effect.Parameters["udissolveS"]?.SetValue(1f);
            effect.Parameters["uBaseImage"]?.SetValue(whiteTex);
            effect.Parameters["uFlow"]?.SetValue(whiteTex);
            effect.Parameters["uGradient"]?.SetValue(whiteTex);
            effect.Parameters["uDissolve"]?.SetValue(whiteTex);

            foreach (var pass in effect.CurrentTechnique.Passes) {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.TriangleList, verts.ToArray(), 0, verts.Count / 3);
            }

            device.RasterizerState = originalRaster;
            device.BlendState = originalBlend;
            device.SamplerStates[0] = originalSampler;
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }
    }
}
