using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Common
{
    /// <summary>
    /// 中子星扭曲效果辅助器 — 使用NeutronWarp着色器替代CPU端暴力叠绘
    /// </summary>
    internal static class NeutronWarpHelper
    {
        /// <summary>
        /// 使用NeutronWarp着色器绘制一次扭曲位移贡献，替代原先33-133层循环叠绘
        /// </summary>
        /// <param name="worldCenter">效果的世界坐标中心</param>
        /// <param name="screenWidth">屏幕空间效果宽度（像素）</param>
        /// <param name="screenHeight">屏幕空间效果高度（像素）</param>
        /// <param name="intensity">位移强度 0-1</param>
        /// <param name="progress">生命进度 0-1（控制扩张/收缩）</param>
        /// <param name="rotation">基础旋转角度</param>
        /// <param name="technique">着色器技术名: GravitationalVortex / ShockwaveRing / RelativisticJet / GravitationalLens</param>
        /// <param name="radius">归一化效果半径（UV空间，默认0.45）</param>
        public static void DrawWarp(
            Vector2 worldCenter,
            float screenWidth,
            float screenHeight,
            float intensity,
            float progress,
            float rotation,
            string technique,
            float radius = 0.45f)
        {
            if (EffectLoader.NeutronWarp == null) {
                return;
            }

            Effect effect = EffectLoader.NeutronWarp.Value;
            if (effect == null) {
                return;
            }

            effect.Parameters["uTime"]?.SetValue((float)Main.GameUpdateCount * 0.05f);
            effect.Parameters["uIntensity"]?.SetValue(intensity);
            effect.Parameters["uProgress"]?.SetValue(MathHelper.Clamp(progress, 0f, 1f));
            effect.Parameters["uRadius"]?.SetValue(radius);
            effect.Parameters["uRotation"]?.SetValue(rotation);
            effect.CurrentTechnique = effect.Techniques[technique];

            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone,
                effect, Main.GameViewMatrix.TransformationMatrix);

            effect.CurrentTechnique.Passes[0].Apply();

            Vector2 screenPos = worldCenter - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle destRect = new Rectangle(
                (int)(screenPos.X - screenWidth * 0.5f),
                (int)(screenPos.Y - screenHeight * 0.5f),
                (int)screenWidth,
                (int)screenHeight
            );

            Main.spriteBatch.Draw(pixel, destRect, new Rectangle(0, 0, 1, 1), Color.White);

            Main.spriteBatch.End();

            Main.spriteBatch.Begin(0, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null,
                Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
