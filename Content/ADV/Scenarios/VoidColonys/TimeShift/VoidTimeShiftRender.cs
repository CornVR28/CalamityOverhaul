using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.GlitchWraith;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TimeShift
{
    /// <summary>
    /// 虚空聚落时空叠加屏幕后处理渲染
    /// 在虚空聚落维度激活时叠加过去滤镜与切换演出到屏幕
    /// </summary>
    internal class VoidTimeShiftRender : RenderHandle
    {
        //权重设在基础EffectLoader(1.2)之上，低于HackTime骇入效果的叠加顺序
        public override float Weight => 1.22f;

        public override void UpdateBySystem(int index) {
            if (Main.gameMenu) {
                VoidTimeShiftSystem.Reset();
                return;
            }
            VoidTimeShiftSystem.Update(VoidColony.Active);
        }

        public override void EndCaptureDraw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            if (Main.gameMenu) {
                return;
            }

            float filter = VoidTimeShiftSystem.FilterIntensity;
            float transition = VoidTimeShiftSystem.TransitionStrength;
            float glitch = GlitchWraithActor.GetLocalDistortionStrength();
            if (filter < 0.001f && transition < 0.001f && glitch < 0.001f) {
                return;
            }

            Effect shader = EffectLoader.VoidTimeShift?.Value;
            if (shader == null) {
                return;
            }
            if (screenSwap == null || screenSwap.IsDisposed) {
                return;
            }
            if (Main.screenTarget == null || Main.screenTarget.IsDisposed) {
                return;
            }

            //将当前主屏复制到交换缓冲，供着色器采样
            graphicsDevice.SetRenderTarget(screenSwap);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            shader.Parameters["filterIntensity"]?.SetValue(filter);
            shader.Parameters["glitchProximity"]?.SetValue(glitch);
            shader.Parameters["transitionStrength"]?.SetValue(transition);
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            //传入像素尺寸倒数用于邻域边缘检测
            int sw = Main.screenWidth > 0 ? Main.screenWidth : 1920;
            int sh = Main.screenHeight > 0 ? Main.screenHeight : 1080;
            shader.Parameters["pixelSize"]?.SetValue(new Vector2(1f / sw, 1f / sh));

            //应用着色器后绘回主屏
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            shader.CurrentTechnique.Passes[0].Apply();
            spriteBatch.Draw(screenSwap, Vector2.Zero, Color.White);
            spriteBatch.End();
        }
    }
}
