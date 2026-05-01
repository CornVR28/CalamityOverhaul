using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces
{
    /// <summary>
    /// 赛博空间领域展开冲击波弹幕
    /// <br/>在领域激活瞬间生成，以领域中心为原点向外扩散的深红色数字冲击环
    /// </summary>
    internal class CyberShockwaveProj : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        private const int Lifetime = 38;
        private float maxDrawRadius;

        public override void SetDefaults() {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI() {
            //跟随领域中心(支持瞬移期间领域缓动)，避免冲击波在玩家瞬间位移时跳到新位置
            Projectile.Center = Cyberspace.DomainCenter;
            if (Projectile.localAI[0] == 0f) {
                maxDrawRadius = Cyberspace.Radius * 1.1f;
                Projectile.localAI[0] = 1f;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Effect shader = EffectLoader.CyberShockwave?.Value;
            if (shader == null) return false;
            if (CWRAsset.Placeholder_White == null) return false;
            if (CWRAsset.Extra_193?.Value == null) return false;

            Texture2D canvas = CWRAsset.Placeholder_White.Value;
            Texture2D noise = CWRAsset.Extra_193.Value;

            float t = 1f - (float)Projectile.timeLeft / Lifetime;
            //快速起步的缓出曲线
            float ringProgress = 1f - MathF.Pow(1f - t, 2.8f);
            float fadeAlpha;
            if (t < 0.55f)
                fadeAlpha = MathHelper.SmoothStep(0f, 1f, t / 0.2f);
            else
                fadeAlpha = MathHelper.SmoothStep(1f, 0f, (t - 0.55f) / 0.45f);
            fadeAlpha = MathHelper.Clamp(fadeAlpha, 0f, 1f);

            //设置着色器参数
            shader.Parameters["uTime"]?.SetValue(Cyberspace.EffectTime);
            shader.Parameters["ringProgress"]?.SetValue(ringProgress);
            shader.Parameters["ringThickness"]?.SetValue(0.065f + (1f - t) * 0.04f);
            shader.Parameters["fadeAlpha"]?.SetValue(fadeAlpha);

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float drawDiameter = maxDrawRadius * 2f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            Main.graphics.GraphicsDevice.Textures[1] = noise;
            Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            shader.CurrentTechnique.Passes[0].Apply();

            Color ringTint = new Color(1f, 0.85f, 0.75f);
            Main.spriteBatch.Draw(canvas, drawPos, null, ringTint,
                0f, canvas.Size() * 0.5f, new Vector2(drawDiameter, drawDiameter),
                SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
