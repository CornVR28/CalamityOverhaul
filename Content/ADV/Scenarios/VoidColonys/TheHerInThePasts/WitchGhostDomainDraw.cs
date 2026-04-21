using CalamityOverhaul.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TheHerInThePasts
{
    /// <summary>
    /// 复用硫磺鬼域着色器绘制女巫的鬼域
    /// 颜色偏向更深的硫火红黑基调，凸显留影的阴森气质
    /// </summary>
    internal static class WitchGhostDomainDraw
    {
        //鬼域基准半径，domainT为1时达到该半径
        private const float BaseRadius = 620f;

        public static void Draw(SpriteBatch sb, Vector2 center, float domainT, float timeAccum, float visibility) {
            Effect shader = EffectLoader.BrimstoneDomain?.Value;
            if (shader == null) return;

            Texture2D canvas = CWRAsset.Placeholder_White?.Value;
            Texture2D noise = CWRAsset.Extra_193?.Value;
            if (canvas == null || noise == null) return;

            float drawDiameter = BaseRadius * 2f * domainT * 1.1f;
            float fade = visibility * MathHelper.Clamp(domainT * 1.2f, 0f, 1f);

            shader.Parameters["uTime"]?.SetValue(timeAccum);
            shader.Parameters["fadeAlpha"]?.SetValue(fade);
            //偏高的层级配色，核心白热、外缘更深
            shader.Parameters["tierLevel"]?.SetValue(2f);
            shader.Parameters["expandProgress"]?.SetValue(MathHelper.Clamp(domainT, 0f, 1f));
            shader.Parameters["pulseIntensity"]?.SetValue(0.45f + MathHelper.Clamp(domainT, 0f, 1f) * 0.4f);

            //女巫鬼域色：硫磺红带暗紫，比Pandemonium更阴郁
            shader.Parameters["coreColor"]?.SetValue(new Vector3(1f, 0.35f, 0.2f));
            shader.Parameters["midColor"]?.SetValue(new Vector3(0.6f, 0.12f, 0.1f));
            shader.Parameters["edgeColor"]?.SetValue(new Vector3(0.28f, 0.06f, 0.1f));
            shader.Parameters["voidColor"]?.SetValue(new Vector3(0.05f, 0.01f, 0.02f));
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
