using CalamityOverhaul.Common;
using InnoVault;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.VoidColonys
{
    /// <summary>
    /// 虚空聚落世界雾气渲染器
    /// 在所有世界实体绘制完毕后，叠加程序化红色雾气覆盖层
    /// 雾气具有多层视差、域弯曲噪声和高度梯度，营造世界中弥漫浓雾的效果
    /// </summary>
    internal class VoidFogRender : RenderHandle
    {
        private float intensity;
        public override float Weight => 1.35f;

        public override void UpdateBySystem(int index) {
            float target = VoidColony.Active ? 1f : 0f;
            intensity = MathHelper.Lerp(intensity, target, 0.02f);
        }

        public override void DrawAfterEntities(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            if (intensity < 0.001f) return;

            var fogShader = EffectLoader.VoidFog?.Value;
            if (fogShader == null) return;

            int vpW = graphicsDevice.Viewport.Width;
            int vpH = graphicsDevice.Viewport.Height;
            float time = (float)Main.timeForVisualEffects * 0.008f;
            float aspectRatio = (float)vpW / vpH;

            //归一化相机位置，让雾气在世界空间中产生视差
            Vector2 screenPos = Main.screenPosition / new Vector2(4200 * 16f, 1800 * 16f);

            fogShader.Parameters["uTime"]?.SetValue(time);
            fogShader.Parameters["uIntensity"]?.SetValue(intensity);
            fogShader.Parameters["uAspectRatio"]?.SetValue(aspectRatio);
            fogShader.Parameters["uScreenPos"]?.SetValue(screenPos);
            fogShader.Parameters["uFogDensity"]?.SetValue(1.0f);

            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone);

            fogShader.CurrentTechnique.Passes[0].Apply();

            spriteBatch.Draw(
                VaultAsset.placeholder2.Value,
                new Rectangle(0, 0, vpW, vpH),
                Color.White);

            spriteBatch.End();
        }
    }
}
