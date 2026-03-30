using CalamityOverhaul.Common;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces
{
    /// <summary>
    /// 赛博空间双层渲染器。
    /// <br/>EndCaptureDraw：全屏后处理（压暗+去饱和+红染+加法赛博特效）
    /// <br/>PostEndCaptureDraw：在后处理之上叠加世界层边缘光晕，提供领域存在感
    /// </summary>
    internal class CyberspaceRender : RenderHandle
    {
        [VaultLoaden(CWRConstant.Masking)]
        public static Texture2D Noise2 = null!;

        [VaultLoaden(CWRConstant.Masking)]
        public static Texture2D SoftGlow = null!;

        public override float Weight => 1.12f;

        public override void UpdateBySystem(int index) {
            Cyberspace.Update();
        }

        public override void EndCaptureDraw(SpriteBatch sb, GraphicsDevice gd, RenderTarget2D screenSwap) {
            if (Main.gameMenu) {
                Cyberspace.Reset();
                return;
            }

            if (!Cyberspace.Active || Cyberspace.Intensity < 0.001f) {
                return;
            }

            Effect shader = EffectLoader.CyberspaceField?.Value;
            if (shader == null) {
                return;
            }

            // 将当前屏幕内容复制到交换缓冲
            gd.SetRenderTarget(screenSwap);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            sb.End();

            // 设置着色器参数
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["radius"]?.SetValue(Cyberspace.Radius);
            shader.Parameters["intensity"]?.SetValue(Cyberspace.Intensity);
            shader.Parameters["expandProgress"]?.SetValue(Cyberspace.ExpandProgress);
            shader.Parameters["dimStrength"]?.SetValue(Cyberspace.DimStrength);
            shader.Parameters["setPoint"]?.SetValue(Main.LocalPlayer.Center);
            shader.Parameters["screenPosition"]?.SetValue(Main.screenPosition);
            shader.Parameters["screenSize"]?.SetValue(Main.ScreenSize.ToVector2());
            shader.Parameters["gridSize"]?.SetValue(Cyberspace.GridSize);

            // 应用着色器并绘制回主屏幕
            gd.SetRenderTarget(Main.screenTarget);
            gd.Clear(Color.Transparent);
            gd.Textures[1] = Noise2;
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            shader.CurrentTechnique.Passes[0].Apply();
            sb.Draw(screenSwap, Vector2.Zero, Color.White);
            sb.End();
        }

        /// <summary>
        /// 在后处理着色器之上，用加法混合绘制世界层边缘光晕环。
        /// 这些光晕不受压暗影响，以红色脉冲发光标记领域边界的世界位置，
        /// 提供视差运动感和"领域存在于世界中"的视觉暗示。
        /// </summary>
        public override void PostEndCaptureDraw(SpriteBatch sb, GraphicsDevice gd, RenderTarget2D screenSwap) {
            if (Main.gameMenu || !Cyberspace.Active || Cyberspace.Intensity < 0.01f) {
                return;
            }

            if (SoftGlow == null) {
                return;
            }

            Vector2 center = Main.LocalPlayer.Center;
            float r = Cyberspace.Radius * Cyberspace.ExpandProgress;
            float gs = Cyberspace.GridSize;
            float time = Main.GlobalTimeWrappedHourly;
            float effectIntensity = Cyberspace.Intensity;

            if (r < gs * 2) {
                return;
            }

            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
                DepthStencilState.None, RasterizerState.CullNone, null,
                Main.GameViewMatrix.TransformationMatrix);

            DrawEdgeGlowRing(sb, center, r, gs, time, effectIntensity);

            sb.End();
        }

        private void DrawEdgeGlowRing(SpriteBatch sb, Vector2 center, float r, float gs,
            float time, float effectIntensity) {
            // 沿圆周扫描，在每个边界栅格单元位置绘制光晕
            int numSteps = Math.Clamp((int)(MathHelper.TwoPi * r / (gs * 0.6f)), 48, 280);
            float prevSnapX = float.NaN;
            float prevSnapY = float.NaN;
            float screenW = Main.screenWidth;
            float screenH = Main.screenHeight;
            float margin = gs * 4;
            Vector2 glowOrigin = new Vector2(SoftGlow.Width * 0.5f, SoftGlow.Height * 0.5f);
            float glowScale = gs * 3.0f / SoftGlow.Width;

            for (int i = 0; i < numSteps; i++) {
                float angle = i * MathHelper.TwoPi / numSteps;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);

                // 沿半径外侧偏移1.5格，使光晕落在边界外围
                float wx = center.X + cos * (r + gs * 1.2f);
                float wy = center.Y + sin * (r + gs * 1.2f);

                // 对齐到栅格单元中心
                float relX = wx - center.X;
                float relY = wy - center.Y;
                float snapX = MathF.Floor(relX / gs) * gs + gs * 0.5f;
                float snapY = MathF.Floor(relY / gs) * gs + gs * 0.5f;

                // 连续去重
                if (snapX == prevSnapX && snapY == prevSnapY) {
                    continue;
                }
                prevSnapX = snapX;
                prevSnapY = snapY;

                float cellWorldX = center.X + snapX;
                float cellWorldY = center.Y + snapY;

                // 屏幕空间裁剪
                float screenX = cellWorldX - Main.screenPosition.X;
                float screenY = cellWorldY - Main.screenPosition.Y;
                if (screenX < -margin || screenX > screenW + margin ||
                    screenY < -margin || screenY > screenH + margin) {
                    continue;
                }

                // 每个单元独立的脉冲动画
                float cellHash = MathF.Abs(MathF.Sin(snapX * 0.137f + snapY * 0.251f));
                float pulse = 0.3f + 0.7f * MathF.Sin(time * 1.8f + cellHash * MathF.PI * 2f);
                pulse = MathF.Max(pulse, 0f);
                float alpha = pulse * effectIntensity * 0.4f;

                Color glowColor = new Color(0.80f * alpha, 0.05f * alpha, 0.04f * alpha, 0f);

                sb.Draw(SoftGlow, new Vector2(screenX, screenY), null, glowColor,
                    0f, glowOrigin, glowScale, SpriteEffects.None, 0f);
            }
        }
    }
}
