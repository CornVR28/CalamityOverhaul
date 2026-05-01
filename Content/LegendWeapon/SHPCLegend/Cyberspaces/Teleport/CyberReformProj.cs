using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Teleport
{
    /// <summary>
    /// 赛博瞬移黑墙数据块重现弹幕（终点演出）
    /// <br/>在目标点以橙红+黑墙基调绘制圆形 quad，由外向内聚拢 16 块楔形数据
    /// <br/>聚拢中段触发 SNAP 闪光与冲击环——玩家在闪光中重现
    /// <br/>使用 CyberReform.fx 着色器，单矩形 ImmediateMode 绘制
    /// </summary>
    internal class CyberReformProj : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        //生命：覆盖玩家恢复可见之后再保留几帧消散尾焰
        private const int Lifetime = 32;
        //聚拢完成时刻（归一化），之后开始消散
        private const float ReformCompleteT = 0.55f;
        //SNAP 闪光峰值时刻（重现闪光）
        private const float SnapPeakT = 0.55f;

        //渲染半径（像素）—— 略大于玩家碰撞箱
        private const float DrawRadius = 96f;

        private float seed;

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
            if (Projectile.localAI[0] == 0f) {
                seed = Main.rand.NextFloat();
                Projectile.localAI[0] = 1f;
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            Effect shader = EffectLoader.CyberReform?.Value;
            if (shader == null) return false;
            if (CWRAsset.Placeholder_White?.Value == null) return false;
            if (CWRAsset.Extra_193?.Value == null) return false;

            Texture2D canvas = CWRAsset.Placeholder_White.Value;
            Texture2D noise = CWRAsset.Extra_193.Value;

            float t = 1f - (float)Projectile.timeLeft / Lifetime;

            //聚拢进度：0 → 1，到 ReformCompleteT 时已经收紧到中心
            float reformProgress;
            if (t < ReformCompleteT) {
                //缓出曲线：起步快，中段持续，临近完成稍微减速给"咬合"感
                float k = t / ReformCompleteT;
                reformProgress = 1f - MathF.Pow(1f - k, 2.6f);
            }
            else {
                reformProgress = 1f;
            }

            //SNAP 脉冲：以 SnapPeakT 为中心的快速三角脉冲，约 8 帧宽
            float snapPulse;
            float snapWidth = 0.18f;
            float snapDelta = MathF.Abs(t - SnapPeakT);
            if (snapDelta < snapWidth) {
                float u = 1f - snapDelta / snapWidth;
                //尖锐山形：中央快速达峰
                snapPulse = MathF.Pow(u, 1.6f);
            }
            else {
                snapPulse = 0f;
            }

            //后段消散：从 ReformCompleteT+0.1 起开始向外塌缩
            float dissipate;
            if (t < ReformCompleteT + 0.1f) {
                dissipate = 0f;
            }
            else {
                float u = (t - ReformCompleteT - 0.1f) / (1f - ReformCompleteT - 0.1f);
                dissipate = MathHelper.Clamp(u, 0f, 1f);
                dissipate = MathF.Pow(dissipate, 1.4f);
            }

            //整体淡入淡出
            float fadeAlpha;
            if (t < 0.10f) {
                fadeAlpha = MathHelper.SmoothStep(0f, 1f, t / 0.10f);
            }
            else if (t < 0.78f) {
                fadeAlpha = 1f;
            }
            else {
                fadeAlpha = MathHelper.SmoothStep(1f, 0f, (t - 0.78f) / 0.22f);
            }
            fadeAlpha = MathHelper.Clamp(fadeAlpha, 0f, 1f);

            shader.Parameters["uTime"]?.SetValue(Cyberspace.EffectTime);
            shader.Parameters["reformProgress"]?.SetValue(reformProgress);
            shader.Parameters["snapPulse"]?.SetValue(snapPulse);
            shader.Parameters["dissipate"]?.SetValue(dissipate);
            shader.Parameters["fadeAlpha"]?.SetValue(fadeAlpha);
            shader.Parameters["seed"]?.SetValue(seed);

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float drawDiameter = DrawRadius * 2f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            Main.graphics.GraphicsDevice.Textures[1] = noise;
            Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            shader.CurrentTechnique.Passes[0].Apply();

            //柔和橙红主调
            Color tint = new(1f, 0.78f, 0.65f);
            Main.spriteBatch.Draw(canvas, drawPos, null, tint,
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
