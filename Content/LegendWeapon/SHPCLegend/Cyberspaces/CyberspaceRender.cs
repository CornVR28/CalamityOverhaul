using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces
{
    /// <summary>
    /// 赛博空间渲染器。
    /// <br/>在PostDrawTiles时机执行全屏后处理（压暗+去饱和+红染+加法赛博特效），
    /// <br/>位于物块图层之上、玩家和其他实体图层之下，
    /// <br/>随后叠加世界层边缘光晕环。
    /// </summary>
    internal class CyberspaceRender : ModSystem
    {
        [VaultLoaden(CWRConstant.Masking + "Noise2")]
        private static Asset<Texture2D> noise2;
        [VaultLoaden(CWRConstant.Masking + "SoftGlow")]
        private static Asset<Texture2D> softGlow;
        private static RenderTarget2D swapTarget;

        public override void Unload() {
            if (swapTarget != null && !swapTarget.IsDisposed) {
                swapTarget.Dispose();
            }
            swapTarget = null;
        }

        public override void PostUpdateEverything() {
            Cyberspace.Update();
        }

        public override void PostDrawTiles() {
            if (Main.dedServ) return;

            if (Main.gameMenu) {
                Cyberspace.Reset();
                return;
            }

            if (!Cyberspace.Active || Cyberspace.Intensity < 0.001f) {
                return;
            }

            ApplyFullScreenShader();
            DrawEdgeGlowRing();
        }

        private static void EnsureSwapTarget(GraphicsDevice gd) {
            int w = Main.screenWidth;
            int h = Main.screenHeight;
            if (swapTarget == null || swapTarget.IsDisposed || swapTarget.Width != w || swapTarget.Height != h) {
                swapTarget?.Dispose();
                swapTarget = new RenderTarget2D(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None);
            }
        }

        private static void ApplyFullScreenShader() {
            Effect shader = EffectLoader.CyberspaceField?.Value;
            Texture2D noiseTex = noise2?.Value;
            if (shader == null || noiseTex == null) return;
            if (Main.screenTarget == null || Main.screenTarget.IsDisposed) return;

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;

            EnsureSwapTarget(gd);

            // 将当前屏幕内容复制到交换缓冲
            gd.SetRenderTarget(swapTarget);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            sb.End();

            // 设置着色器参数
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Vector2 screenPixels = Main.ScreenSize.ToVector2();
            Vector2 worldViewSize = screenPixels / zoom;
            Vector2 worldViewOrigin = Main.screenPosition
                + screenPixels * (Vector2.One - Vector2.One / zoom) * 0.5f;

            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["radius"]?.SetValue(Cyberspace.Radius);
            shader.Parameters["intensity"]?.SetValue(Cyberspace.Intensity);
            shader.Parameters["expandProgress"]?.SetValue(Cyberspace.ExpandProgress);
            shader.Parameters["dimStrength"]?.SetValue(Cyberspace.DimStrength);
            shader.Parameters["setPoint"]?.SetValue(Main.LocalPlayer.Center);
            shader.Parameters["screenPosition"]?.SetValue(worldViewOrigin);
            shader.Parameters["worldViewSize"]?.SetValue(worldViewSize);
            shader.Parameters["gridSize"]?.SetValue(Cyberspace.GridSize);

            // 应用着色器并绘制回主屏幕
            gd.SetRenderTarget(Main.screenTarget);
            gd.Clear(Color.Transparent);
            gd.Textures[1] = noiseTex;
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            shader.CurrentTechnique.Passes[0].Apply();
            sb.Draw(swapTarget, Vector2.Zero, Color.White);
            sb.End();
        }

        private static void DrawEdgeGlowRing() {
            Texture2D glowTex = softGlow?.Value;
            if (glowTex == null || Cyberspace.Intensity < 0.01f) return;

            Vector2 center = Main.LocalPlayer.Center;
            float r = Cyberspace.Radius * Cyberspace.ExpandProgress;
            float gs = Cyberspace.GridSize;
            float time = Main.GlobalTimeWrappedHourly;
            float effectIntensity = Cyberspace.Intensity;

            if (r < gs * 2) return;

            SpriteBatch sb = Main.spriteBatch;
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
                DepthStencilState.None, RasterizerState.CullNone, null,
                Main.GameViewMatrix.TransformationMatrix);

            int numSteps = Math.Clamp((int)(MathHelper.TwoPi * r / (gs * 0.6f)), 48, 280);
            float prevSnapX = float.NaN;
            float prevSnapY = float.NaN;
            float screenW = Main.screenWidth;
            float screenH = Main.screenHeight;
            float margin = gs * 4;
            Vector2 glowOrigin = new Vector2(glowTex.Width * 0.5f, glowTex.Height * 0.5f);
            float glowScale = gs * 3.0f / glowTex.Width;

            for (int i = 0; i < numSteps; i++) {
                float angle = i * MathHelper.TwoPi / numSteps;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);

                float wx = center.X + cos * (r + gs * 1.2f);
                float wy = center.Y + sin * (r + gs * 1.2f);

                float relX = wx - center.X;
                float relY = wy - center.Y;
                float snapX = MathF.Floor(relX / gs) * gs + gs * 0.5f;
                float snapY = MathF.Floor(relY / gs) * gs + gs * 0.5f;

                if (snapX == prevSnapX && snapY == prevSnapY) continue;
                prevSnapX = snapX;
                prevSnapY = snapY;

                float cellWorldX = center.X + snapX;
                float cellWorldY = center.Y + snapY;

                float screenX = cellWorldX - Main.screenPosition.X;
                float screenY = cellWorldY - Main.screenPosition.Y;
                if (screenX < -margin || screenX > screenW + margin ||
                    screenY < -margin || screenY > screenH + margin) continue;

                float cellHash = MathF.Abs(MathF.Sin(snapX * 0.137f + snapY * 0.251f));
                float pulse = 0.3f + 0.7f * MathF.Sin(time * 1.8f + cellHash * MathF.PI * 2f);
                pulse = MathF.Max(pulse, 0f);
                float alpha = pulse * effectIntensity * 0.4f;

                Color glowColor = new Color(0.80f * alpha, 0.05f * alpha, 0.04f * alpha, 0f);

                sb.Draw(glowTex, new Vector2(screenX, screenY), null, glowColor,
                    0f, glowOrigin, glowScale, SpriteEffects.None, 0f);
            }

            sb.End();
        }
    }
}
