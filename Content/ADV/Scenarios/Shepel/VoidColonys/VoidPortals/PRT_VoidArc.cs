using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.VoidColonys.VoidPortals
{
    /// <summary>
    /// 虚空电弧粒子 — 裂隙边缘的不稳定闪烁/闪电效果
    /// 极短生命周期产生闪烁感，细长形态模拟电弧/裂纹
    /// </summary>
    internal class PRT_VoidArc : BasePRT
    {
        public override string Texture => CWRConstant.Masking + "LightBeam";

        private float initialScale;

        public PRT_VoidArc() { }

        public PRT_VoidArc(Vector2 position, Vector2 velocity, float scale) {
            Position = position;
            Velocity = velocity;
            initialScale = scale;
            Scale = scale;
            Lifetime = 6 + Main.rand.Next(10);
            Rotation = velocity.ToRotation() + Main.rand.NextFloat(-0.4f, 0.4f);
            Color = Color.Lerp(new Color(255, 200, 180), new Color(255, 80, 30), Main.rand.NextFloat());
        }

        public override void SetProperty() {
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
        }

        public override void AI() {
            Velocity *= 0.85f;

            float life = LifetimeCompletion;

            // 闪电般的快速出现和消失
            float fadeIn = Math.Min(life * 12f, 1f);
            float fadeOut = 1f - MathF.Pow(life, 1.5f);
            Opacity = fadeIn * fadeOut;

            // 闪烁效果（高频闪动）
            if (Main.rand.NextBool(4)) {
                Opacity *= Main.rand.NextFloat(0.3f, 1f);
            }

            // 缩放在生命中期最长
            float stretch = MathF.Sin(life * MathHelper.Pi);
            Scale = initialScale * (0.7f + stretch * 0.3f);

            // 颜色从白热到暗红
            Color darkEnd = new Color(120, 15, 5);
            Color = Color.Lerp(Color, darkEnd, MathF.Pow(life, 2f) * 0.5f);

            if (Opacity < 0.02f) Kill();
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D texture = PRTLoader.PRT_IDToTexture[ID];
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPos = Position - Main.screenPosition;

            // 非对称缩放：极窄 + 长条 → 电弧/裂纹形态
            Vector2 scaleVec = new Vector2(0.012f, 0.2f) * Scale;

            // 外层辉光（宽且淡，柔化边缘）
            spriteBatch.Draw(texture, drawPos, null,
                Color * Opacity * 0.3f,
                Rotation, origin,
                scaleVec * new Vector2(3.0f, 1.15f),
                SpriteEffects.None, 0f);

            // 核心电弧（亮且锐利）
            Color coreColor = Color.Lerp(Color.White, Color, 0.4f);
            spriteBatch.Draw(texture, drawPos, null,
                coreColor * Opacity,
                Rotation, origin,
                scaleVec,
                SpriteEffects.None, 0f);

            return false;
        }
    }
}
