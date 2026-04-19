using CalamityOverhaul.Common;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.VoidColonys
{
    /// <summary>
    /// 虚空聚落世界雾气渲染
    /// 在 DrawNPCsOverTiles 时机以世界对齐的全屏雾覆盖物块和实体之上
    /// 雾的位置由世界坐标决定，因此玩家移动时雾团稳定漂浮于世界中
    /// </summary>
    internal class VoidColonyFog : RenderHandle
    {
        //淡入淡出系数，离开维度时平滑收回
        private static float intensity;
        //着色器自身的累积时间，避免暂停时仍在翻腾
        private static float effectTime;

        //雾的最浓厚度 1.0 接近完全遮挡远景
        private const float FogDensityCap = 0.95f;
        //玩家周围完全清雾的半径 像素
        private const float ClearRadius = 220f;
        //雾从清空到正常浓度的过渡半径 像素
        private const float FalloffRadius = 720f;

        //权重略大于 EffectLoader 主权重，让雾在其它后处理稳定后再叠加
        public override float Weight => 1.5f;

        public override void UpdateBySystem(int index) {
            if (Main.gameMenu) {
                intensity = 0f;
                return;
            }

            bool active = VoidColony.Active;
            float target = active ? 1f : 0f;
            intensity = MathHelper.Lerp(intensity, target, 0.025f);

            if (!Main.gamePaused) {
                //稍快的时间推进 配合 shader 内部速度形成显著翻滚
                effectTime += 1f / 45f;
            }
        }

        public override void DrawNPCsOverTiles(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            if (Main.gameMenu || intensity < 0.005f) {
                return;
            }

            Effect shader = EffectLoader.VoidFog?.Value;
            Texture2D noiseTex = CWRAsset.PerlinNoise?.Value;
            Texture2D canvas = CWRAsset.Placeholder_White?.Value;
            if (shader == null || noiseTex == null || canvas == null) {
                return;
            }

            //计算世界视口参数，与 CyberspaceRender 一致
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Vector2 screenPixels = Main.ScreenSize.ToVector2();
            Vector2 viewScale = Vector2.One / zoom;
            Vector2 worldViewOrigin = Main.screenPosition
                + screenPixels * (Vector2.One - viewScale) * 0.5f;

            shader.Parameters["uTime"]?.SetValue(effectTime);
            shader.Parameters["uIntensity"]?.SetValue(intensity);
            shader.Parameters["uDensity"]?.SetValue(FogDensityCap);
            shader.Parameters["uScreenSize"]?.SetValue(screenPixels);
            shader.Parameters["uWorldOffset"]?.SetValue(worldViewOrigin);
            shader.Parameters["uViewScale"]?.SetValue(viewScale);
            //玩家屏幕坐标 用于近距清雾
            Vector2 playerScreen = Main.LocalPlayer.Center - Main.screenPosition;
            shader.Parameters["uPlayerScreen"]?.SetValue(playerScreen);
            shader.Parameters["uClearRadius"]?.SetValue(ClearRadius);
            shader.Parameters["uFalloffRadius"]?.SetValue(FalloffRadius);
            //血红主题 稍微提亮 Low 与 Mid 让远景仍然可读 Hi 保持深沉
            shader.Parameters["uFogColorLow"]?.SetValue(new Vector4(0.50f, 0.14f, 0.10f, 1f));
            shader.Parameters["uFogColorMid"]?.SetValue(new Vector4(0.62f, 0.10f, 0.06f, 1f));
            shader.Parameters["uFogColorHi"]?.SetValue(new Vector4(0.20f, 0.03f, 0.02f, 1f));
            shader.Parameters["uHighlightColor"]?.SetValue(new Vector4(1.0f, 0.66f, 0.22f, 1f));

            graphicsDevice.Textures[1] = noiseTex;
            graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

            //使用 NonPremultiplied 配合 shader 输出的直通 alpha
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            shader.CurrentTechnique.Passes[0].Apply();
            spriteBatch.Draw(canvas,
                new Rectangle(0, 0, (int)screenPixels.X, (int)screenPixels.Y),
                Color.White);
            spriteBatch.End();
        }
    }
}
