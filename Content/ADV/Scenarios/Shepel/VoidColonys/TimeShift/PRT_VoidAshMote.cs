using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.VoidColonys.TimeShift
{
    /// <summary>
    /// 虚空聚落过去时代的环境尘粒
    /// 极低亮度的褪色琥珀/灰蓝浮尘，缓慢向侧下方漂浮，头尾渐隐
    /// 专用于烘托"百年尘封"的死寂感，与屏幕后处理滤镜保持同色域
    /// </summary>
    internal class PRT_VoidAshMote : BasePRT
    {
        public override string Texture => CWRConstant.Masking + "SoftGlow";

        /// <summary>
        /// 随机相位，用于正弦飘移
        /// </summary>
        private float drift;

        /// <summary>
        /// 基础透明度上限，不让尘粒在任何时刻过亮
        /// </summary>
        private float baseOpacity;

        public override void SetProperty() {
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            drift = Main.rand.NextFloat(MathHelper.TwoPi);
            baseOpacity = Main.rand.NextFloat(0.58f, 0.62f);
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI() {
            //正弦水平飘移加恒定缓慢下沉，营造空气中的浮游感
            drift += 0.01f;
            Velocity.X += MathF.Sin(drift) * 0.004f;
            Velocity.X *= 0.99f;
            //终端下沉速度非常低
            if (Velocity.Y < 0.35f) {
                Velocity.Y += 0.004f;
            }
            Rotation += 0.003f;

            //两端淡入淡出，用sin钟形避免突兀
            float t = LifetimeCompletion;
            Opacity = MathF.Sin(t * MathHelper.Pi) * baseOpacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D tex = TexValue;
            //很小的尺寸避免粒子挤占视野，只作为氛围暗示
            spriteBatch.Draw(tex, Position - Main.screenPosition, null,
                Color * Opacity, Rotation, tex.Size() / 2f, Scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
