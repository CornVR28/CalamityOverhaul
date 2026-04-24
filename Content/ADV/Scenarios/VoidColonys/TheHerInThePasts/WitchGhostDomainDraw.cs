using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TheHerInThePasts
{
    /// <summary>
    /// 使用专属WitchBrimstoneDomain着色器绘制女巫留影的硫磺火鬼域
    /// </summary>
    internal static class WitchGhostDomainDraw
    {
        //鬼域基准半径，domainT为1时达到该半径
        private const float BaseRadius = 620f;

        public static void Draw(SpriteBatch sb, Vector2 center, float domainT, float timeAccum, float visibility, float dissolveT = 0f) {
            Effect shader = EffectLoader.WitchBrimstoneDomain?.Value;
            if (shader == null) return;

            Texture2D canvas = CWRAsset.Placeholder_White?.Value;
            Texture2D noise = CWRAsset.Extra_193?.Value;
            if (canvas == null || noise == null) return;

            //鬼域画布尺寸始终按最大尺寸铺满，扩张进度交给shader内部裁切
            float drawDiameter = BaseRadius * 2f * 1.12f;
            float fade = visibility * MathHelper.Clamp(domainT * 1.2f, 0f, 1f);

            shader.Parameters["uTime"]?.SetValue(timeAccum);
            shader.Parameters["fadeAlpha"]?.SetValue(fade);
            shader.Parameters["expandProgress"]?.SetValue(MathHelper.Clamp(domainT, 0f, 1f));
            shader.Parameters["dissolveProgress"]?.SetValue(MathHelper.Clamp(dissolveT, 0f, 1f));
            shader.Parameters["pulseIntensity"]?.SetValue(0.55f + MathHelper.Clamp(domainT, 0f, 1f) * 0.35f);

            //女巫留影的硫火配色，核心冷硫橙，中层血锈红，边缘焦紫，虚空底
            shader.Parameters["coreColor"]?.SetValue(new Vector3(1f, 0.42f, 0.18f));
            shader.Parameters["midColor"]?.SetValue(new Vector3(0.55f, 0.1f, 0.08f));
            shader.Parameters["edgeColor"]?.SetValue(new Vector3(0.22f, 0.05f, 0.09f));
            shader.Parameters["voidColor"]?.SetValue(new Vector3(0.04f, 0.01f, 0.02f));
            shader.Parameters["uNoiseTex"]?.SetValue(noise);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            shader.CurrentTechnique.Passes[0].Apply();

            sb.Draw(canvas, center - Main.screenPosition, null, Color.White,
                0f, canvas.Size() * 0.5f, new Vector2(drawDiameter, drawDiameter),
                SpriteEffects.None, 0f);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
