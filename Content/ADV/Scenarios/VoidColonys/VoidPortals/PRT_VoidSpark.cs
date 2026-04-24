using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.VoidPortals
{
    /// <summary>
    /// 虚空火花粒子 — 从裂隙边缘逸散的红色/橙色能量碎片
    /// 快速向外飞散，带有辉光拖尾，颜色从炽热橙红渐变到暗红
    /// </summary>
    internal class PRT_VoidSpark : BasePRT
    {
        public override string Texture => CWRConstant.Masking + "Photosphere";

        private Color initialColor;
        private float initialScale;

        public PRT_VoidSpark() { }

        public PRT_VoidSpark(Vector2 position, Vector2 velocity, Color color, float scale) {
            Position = position;
            Velocity = velocity;
            initialColor = color;
            Color = color;
            initialScale = scale;
            Scale = scale;
            Lifetime = 25 + Main.rand.Next(20);
        }

        public override void SetProperty() {
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
        }

        public override void AI() {
            // 快速减速（被虚空拖曳感）
            Velocity *= 0.91f;

            // 受轻微重力影响
            Velocity.Y += 0.04f;

            // 旋转
            Rotation += Velocity.X * 0.06f;

            float life = LifetimeCompletion;

            // 尖锐的出现/消散曲线
            float fadeIn = Math.Min(life * 10f, 1f);
            float fadeOut = 1f - MathF.Pow(Math.Max(life - 0.4f, 0f) / 0.6f, 2f);
            Opacity = fadeIn * fadeOut;

            // 缩放：先膨胀后收缩
            float breathe = MathF.Sin(life * MathHelper.Pi);
            Scale = initialScale * (0.5f + breathe * 0.5f);

            // 颜色渐变：炽热 → 暗红
            Color darkEnd = new Color(80, 10, 5);
            float colorShift = MathF.Pow(life, 1.2f);
            Color = Color.Lerp(initialColor, darkEnd, colorShift);

            if (Scale < 0.05f || Opacity < 0.01f) Kill();
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D texture = PRTLoader.PRT_IDToTexture[ID];
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPos = Position - Main.screenPosition;

            // 外层柔光晕（大而淡）
            spriteBatch.Draw(texture, drawPos, null,
                Color * Opacity * 0.25f,
                Rotation, origin,
                Scale * 2.2f,
                SpriteEffects.None, 0f);

            // 中层辉光
            spriteBatch.Draw(texture, drawPos, null,
                Color * Opacity * 0.6f,
                Rotation, origin,
                Scale * 1.1f,
                SpriteEffects.None, 0f);

            // 核心亮点（白热）
            Color coreColor = Color.Lerp(Color.White, Color, 0.3f);
            spriteBatch.Draw(texture, drawPos, null,
                coreColor * Opacity * 0.9f,
                Rotation, origin,
                Scale * 0.4f,
                SpriteEffects.None, 0f);

            return false;
        }
    }
}
