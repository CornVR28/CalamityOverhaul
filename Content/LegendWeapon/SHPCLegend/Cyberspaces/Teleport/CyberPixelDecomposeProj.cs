using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Teleport
{
    /// <summary>
    /// 赛博瞬移起点解构演出弹幕
    /// <br/>玩家在起点被解构成像素数据块 → 沿 <see cref="CyberRiftSlashProj"/> 走廊离去
    /// <br/>共用 CyberReform.fx，但 direction = -1：格子从轮廓位置向外飞散
    /// <br/>与 <see cref="CyberReformProj"/> 形成"解构 → 传输 → 重组"三段式视觉闭环
    /// </summary>
    internal class CyberPixelDecomposeProj : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        //寿命：略短于走廊延伸时间，让"解构"先于走廊到达终点完成
        private const int Lifetime = 22;
        //演出整体可视半径
        private const float DisplayRadius = 220f;
        //离心方向 -1 = decompose
        private const float Direction = -1f;

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
            if (CWRAsset.Placeholder_White == null) return false;
            if (CWRAsset.Extra_193?.Value == null) return false;

            Texture2D canvas = CWRAsset.Placeholder_White.Value;
            Texture2D noise = CWRAsset.Extra_193.Value;

            float t = 1f - (float)Projectile.timeLeft / Lifetime;

            //decompose: progress 0 = 完整轮廓刚开始撕裂；progress 1 = 完全飞散
            float progress = MathHelper.Clamp(t, 0f, 1f);
            //snap 在 0~0.15 内闪一记"撕开"白光
            float snap = MathF.Max(0f, 1f - t / 0.15f);
            snap = MathF.Pow(snap, 1.4f) * 0.85f;
            //decompose 走 direction=-1，shader 内自带"早期最亮 → 飞远变暗"
            float dissipate = 0f;

            //淡入：极快 / 淡出：尾段抖一下
            float fadeAlpha;
            if (t < 0.05f) fadeAlpha = t / 0.05f;
            else if (t > 0.75f) fadeAlpha = MathHelper.SmoothStep(1f, 0f, (t - 0.75f) / 0.25f);
            else fadeAlpha = 1f;

            shader.Parameters["uTime"]?.SetValue(
                Cyberspace.Active ? Cyberspace.EffectTime : (float)Main.timeForVisualEffects * 0.04f);
            shader.Parameters["fadeAlpha"]?.SetValue(MathHelper.Clamp(fadeAlpha, 0f, 1f));
            shader.Parameters["reformProgress"]?.SetValue(progress);
            shader.Parameters["snapPulse"]?.SetValue(MathHelper.Clamp(snap, 0f, 1f));
            shader.Parameters["dissipate"]?.SetValue(dissipate);
            shader.Parameters["seed"]?.SetValue(seed);
            shader.Parameters["direction"]?.SetValue(Direction);

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float drawDiameter = DisplayRadius * 2f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            Main.graphics.GraphicsDevice.Textures[1] = noise;
            Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            shader.CurrentTechnique.Passes[0].Apply();

            Main.spriteBatch.Draw(canvas, drawPos, null, Color.White,
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
