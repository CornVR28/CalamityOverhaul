using CalamityOverhaul.Common;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.VoidPortals
{
    /// <summary>
    /// 虚空传送门屏幕空间渲染器
    /// </summary>
    internal class VoidPortalRenderer : RenderHandle
    {
        [VaultLoaden(CWRConstant.Masking + "Noise2")]
        private static Asset<Texture2D> noise2;
        [VaultLoaden(CWRConstant.Masking + "SoftGlow")]
        private static Asset<Texture2D> softGlow;

        public override float Weight => 1.3f;

        public override void DrawNPCsOverTiles(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice,
            RenderTarget2D screenSwap) {
            if (Main.gameMenu) return;

            VoidPortal portal = VoidPortal.ValidateActiveInstance();
            if (portal == null) return;
            if (portal.Intensity < 0.001f || portal.ExpandProgress < 0.001f) return;

            ApplyFullScreenShader(spriteBatch, graphicsDevice, screenSwap, portal);

            //裂隙边缘辉光环（Additive混合）
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            DrawEdgeGlow(spriteBatch, portal);
            spriteBatch.End();
        }

        /// <summary>
        /// 吸入滤镜放在EndCaptureDraw，确保覆盖所有实体、PRT粒子和玩家
        /// </summary>
        public override void EndCaptureDraw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice,
            RenderTarget2D screenSwap) {
            if (Main.gameMenu) return;

            VoidPortal portal = VoidPortal.ValidateActiveInstance();
            if (portal == null) return;

            ApplySuctionShader(spriteBatch, graphicsDevice, screenSwap, portal);
        }

        private static void ApplyFullScreenShader(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice,
            RenderTarget2D screenSwap, VoidPortal portal) {
            Effect shader = EffectLoader.VoidPortal?.Value;
            Texture2D noiseTex = noise2?.Value;
            if (shader == null || noiseTex == null) return;
            if (screenSwap == null || screenSwap.IsDisposed) return;
            if (Main.screenTarget == null || Main.screenTarget.IsDisposed) return;

            //复制当前屏幕到交换缓冲
            graphicsDevice.SetRenderTarget(screenSwap);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            //计算缩放感知的世界坐标参数（参考CyberspaceRender）
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Vector2 screenPixels = Main.ScreenSize.ToVector2();
            Vector2 worldViewSize = screenPixels / zoom;
            Vector2 worldViewOrigin = Main.screenPosition
                + screenPixels * (Vector2.One - Vector2.One / zoom) * 0.5f;

            //设置着色器参数（全部世界坐标）
            shader.Parameters["uTime"]?.SetValue(portal.EffectTime);
            shader.Parameters["intensity"]?.SetValue(portal.Intensity);
            shader.Parameters["expandProgress"]?.SetValue(portal.ExpandProgress);
            shader.Parameters["riftHalfHeight"]?.SetValue(VoidPortal.BaseRiftHalfHeight);
            shader.Parameters["riftMaxWidth"]?.SetValue(VoidPortal.BaseRiftMaxWidth);
            shader.Parameters["dimStrength"]?.SetValue(VoidPortal.BaseDimStrength);
            shader.Parameters["energyPower"]?.SetValue(VoidPortal.BaseEnergyPower);
            shader.Parameters["crackSeed"]?.SetValue(portal.CrackSeed);
            shader.Parameters["shockwaveTime"]?.SetValue(portal.ShockwaveTime);
            shader.Parameters["riftCenter"]?.SetValue(portal.PortalCenter);
            shader.Parameters["screenPosition"]?.SetValue(worldViewOrigin);
            shader.Parameters["worldViewSize"]?.SetValue(worldViewSize);

            //应用着色器绘制回主屏幕
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            graphicsDevice.Textures[1] = noiseTex;
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            shader.CurrentTechnique.Passes[0].Apply();
            spriteBatch.Draw(screenSwap, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        private static void ApplySuctionShader(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice,
            RenderTarget2D screenSwap, VoidPortal portal) {
            Player localPlayer = Main.LocalPlayer;
            if (localPlayer == null || !localPlayer.active) return;
            if (!localPlayer.TryGetModPlayer(out VoidTransportPlayer tp)) return;
            if (tp.SuctionProgress < 0.001f && tp.BlackFlashAlpha < 0.001f) return;

            Effect shader = EffectLoader.VoidSuction?.Value;
            Texture2D noiseTex = noise2?.Value;
            if (shader == null || noiseTex == null) return;
            if (screenSwap == null || screenSwap.IsDisposed) return;
            if (Main.screenTarget == null || Main.screenTarget.IsDisposed) return;

            //复制当前屏幕到交换缓冲
            graphicsDevice.SetRenderTarget(screenSwap);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            //世界坐标参数
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Vector2 screenPixels = Main.ScreenSize.ToVector2();
            Vector2 worldViewSize = screenPixels / zoom;
            Vector2 worldViewOrigin = Main.screenPosition
                + screenPixels * (Vector2.One - Vector2.One / zoom) * 0.5f;

            shader.Parameters["uTime"]?.SetValue(portal.EffectTime);
            shader.Parameters["suctionProgress"]?.SetValue(tp.SuctionProgress);
            shader.Parameters["blackFlash"]?.SetValue(tp.BlackFlashAlpha);
            shader.Parameters["focusCenter"]?.SetValue(portal.PortalCenter);
            shader.Parameters["screenPosition"]?.SetValue(worldViewOrigin);
            shader.Parameters["worldViewSize"]?.SetValue(worldViewSize);

            //应用
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            graphicsDevice.Textures[1] = noiseTex;
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            shader.CurrentTechnique.Passes[0].Apply();
            spriteBatch.Draw(screenSwap, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        /// <summary>
        /// 沿裂隙边缘绘制辉光光点
        /// </summary>
        private static void DrawEdgeGlow(SpriteBatch spriteBatch, VoidPortal portal) {
            Texture2D glowTex = softGlow?.Value;
            if (glowTex == null || portal.Intensity < 0.01f) return;

            Vector2 center = portal.PortalCenter;
            float expand = portal.ExpandProgress;
            float halfH = VoidPortal.BaseRiftHalfHeight * expand;
            float maxW = VoidPortal.BaseRiftMaxWidth * expand;
            float time = portal.EffectTime;
            float effectIntensity = portal.Intensity;

            if (halfH < 20f) return;

            Vector2 glowOrigin = new Vector2(glowTex.Width * 0.5f, glowTex.Height * 0.5f);
            float screenW = Main.screenWidth;
            float screenH = Main.screenHeight;
            float margin = 80f;

            // 沿裂隙上下边缘分布辉光点
            int numSteps = Math.Clamp((int)(halfH * 2f / 12f), 20, 120);
            for (int i = 0; i < numSteps; i++) {
                float t = (float)i / (numSteps - 1);
                float yWorld = -halfH + t * halfH * 2f;
                float yNorm = MathHelper.Clamp(yWorld / MathF.Max(halfH, 1f), -1f, 1f);

                // 裂隙宽度包络
                float envelope = 1f - MathF.Pow(MathF.Abs(yNorm), 1.5f);
                float localWidth = maxW * envelope;

                // 两侧各放一个辉光点
                for (int side = -1; side <= 1; side += 2) {
                    float xWorld = localWidth * side;
                    float worldX = center.X + xWorld;
                    float worldY = center.Y + yWorld;

                    float screenX = worldX - Main.screenPosition.X;
                    float screenY = worldY - Main.screenPosition.Y;
                    if (screenX < -margin || screenX > screenW + margin ||
                        screenY < -margin || screenY > screenH + margin) continue;

                    // 脉动
                    float cellHash = MathF.Abs(MathF.Sin(yWorld * 0.037f + side * 1.7f));
                    float pulse = 0.3f + 0.7f * MathF.Max(0f,
                        MathF.Sin(time * 1.6f + cellHash * MathHelper.TwoPi));
                    float alpha = pulse * effectIntensity * 0.35f;

                    // 颜色：白热核心→暗红外层
                    float edgeHeat = envelope;
                    Color glowColor = new Color(
                        0.9f * alpha,
                        0.12f * alpha * edgeHeat,
                        0.05f * alpha * edgeHeat,
                        0f);

                    float glowScale = (28f + 12f * envelope) / glowTex.Width;

                    spriteBatch.Draw(glowTex, new Vector2(screenX, screenY), null,
                        glowColor, 0f, glowOrigin, glowScale, SpriteEffects.None, 0f);
                }
            }

            // 裂隙尖端额外辉光（上下两端）
            for (int tip = -1; tip <= 1; tip += 2) {
                float tipY = center.Y + halfH * tip;
                float tipScreenX = center.X - Main.screenPosition.X;
                float tipScreenY = tipY - Main.screenPosition.Y;

                if (tipScreenX < -margin || tipScreenX > screenW + margin ||
                    tipScreenY < -margin || tipScreenY > screenH + margin) continue;

                float tipPulse = 0.5f + 0.5f * MathF.Sin(time * 2.2f + tip * 1.5f);
                float tipAlpha = tipPulse * effectIntensity * 0.5f;
                Color tipColor = new Color(1f * tipAlpha, 0.25f * tipAlpha, 0.1f * tipAlpha, 0f);
                float tipScale = 45f / glowTex.Width;

                spriteBatch.Draw(glowTex, new Vector2(tipScreenX, tipScreenY), null,
                    tipColor, 0f, glowOrigin, tipScale, SpriteEffects.None, 0f);
            }
        }
    }
}
