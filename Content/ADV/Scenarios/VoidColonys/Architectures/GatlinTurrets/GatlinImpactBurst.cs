using CalamityOverhaul.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.GatlinTurrets
{
    /// <summary>
    /// 加特林子弹命中爆点纯视觉弹幕
    /// 用 GatlinImpactBurst 着色器在一个方形 Quad 上程序化绘制：
    /// 白炽核心闪光 + 扩张冲击环 + 高频放射火花丝 + 沿命中反方向的定向热浪
    /// </summary>
    internal class GatlinImpactBurst : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder2;

        //爆点贴花的像素直径，越大感染范围越广
        private const float BurstDiameter = 50f;
        //生命周期帧数，配合 timeLeft 控制 uProgress 0..1
        private const int Lifetime = 24;

        public override void SetDefaults() {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 255;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI() {
            //ai[0]/ai[1] 承载命中方向，由生成者写入，不再变更
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;

            //爆点早期在地面投下暖光，随进度快速衰减
            float lightFalloff = 1f - progress;
            Lighting.AddLight(Projectile.Center, 1.4f * lightFalloff, 0.75f * lightFalloff, 0.25f * lightFalloff);
        }

        public override bool PreDraw(ref Color lightColor) {
            Effect shader = EffectLoader.GatlinImpactBurst?.Value;
            Texture2D noise = CWRAsset.Extra_193?.Value;
            Texture2D quadTex = CWRAsset.Placeholder_White.Value;
            if (shader == null || noise == null || quadTex == null) return false;

            float progress = MathHelper.Clamp(1f - Projectile.timeLeft / (float)Lifetime, 0f, 1f);
            //早期略微膨胀、后期再轻微收缩，模拟爆发冲击回弹
            float scaleEnv = 0.4f + 1.3f * (1f - (1f - progress) * (1f - progress));
            float pixelScale = BurstDiameter * scaleEnv / quadTex.Width;

            Vector2 dir = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            if (dir.LengthSquared() < 0.001f) dir = Vector2.UnitX;
            else dir.Normalize();

            shader.Parameters["uProgress"]?.SetValue(progress);
            shader.Parameters["uIntensity"]?.SetValue(1.6f);
            shader.Parameters["uDirection"]?.SetValue(dir);
            shader.Parameters["uNoiseTex"]?.SetValue(noise);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = quadTex.Size() * 0.5f;
            //用白色像素贴图承载 UV，随机相位让连续命中爆点角度不一致
            float quadRot = Projectile.identity * 0.37f;
            Main.spriteBatch.Draw(quadTex, drawPos, null, Color.White, quadRot, origin, pixelScale,
                SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
