using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Skills.Sandevistans
{
    /// <summary>
    /// 斯安威斯坦屏幕级渲染器。
    /// 在 EndCaptureDraw 中对整个屏幕画面应用赛博朋克2077风格的后处理效果：
    /// 色差分离、青色调去饱和、暗角、扫描线、数字噪点、边缘辉光。
    /// </summary>
    internal class SandevistanRender : RenderHandle
    {
        /// <summary>
        /// 确保残影RT绘制完成后再执行屏幕后处理
        /// </summary>
        public override float Weight => 1.1f;

        public override void EndCaptureDraw(SpriteBatch sb, GraphicsDevice gd, RenderTarget2D screenSwap) {
            float effectIntensity = Sandevistan.ScreenEffectIntensity;
            if (effectIntensity <= 0.001f) {
                return;
            }

            Effect shader = SandevistanAssets.SandevistanScreen;
            if (shader == null) {
                return;
            }

            //将当前屏幕内容复制到 swap
            gd.SetRenderTarget(screenSwap);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            sb.End();

            //计算玩家在屏幕上的归一化位置
            Vector2 playerScreen = Main.LocalPlayer.Center - Main.screenPosition;
            Vector2 playerUV = new Vector2(
                playerScreen.X / Main.screenWidth,
                playerScreen.Y / Main.screenHeight
            );
            playerUV = Vector2.Clamp(playerUV, Vector2.Zero, Vector2.One);

            //设置着色器参数
            shader.Parameters["intensity"]?.SetValue(effectIntensity);
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["chromaticOffset"]?.SetValue(0.004f * effectIntensity);
            shader.Parameters["vignetteStrength"]?.SetValue(0.45f);
            shader.Parameters["playerCenter"]?.SetValue(playerUV);

            //应用着色器并绘制回主屏幕
            gd.SetRenderTarget(Main.screenTarget);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            shader.CurrentTechnique.Passes[0].Apply();
            sb.Draw(screenSwap, Vector2.Zero, Color.White);
            sb.End();
        }
    }
}
