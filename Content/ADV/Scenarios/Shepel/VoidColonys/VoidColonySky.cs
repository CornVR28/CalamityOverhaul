using CalamityOverhaul.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.VoidColonys
{
    /// <summary>
    /// 虚空聚落天空场景效果注册
    /// </summary>
    internal class VoidColonySkyData : ModSceneEffect
    {
        public override int Music => -1;
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
        public override bool IsSceneEffectActive(Player player) => VoidColony.Active;
        public override void SpecialVisuals(Player player, bool isActive) =>
            player.ManageSpecialBiomeVisuals(VoidColonySky.Name, isActive);
    }

    /// <summary>
    /// 虚空聚落天空 — 红黑亚空间末日背景
    /// 使用全屏着色器渲染：深邃漩涡 + 能量裂流 + 虚空之眼 + 闪电裂痕 + 星空星云
    /// </summary>
    internal class VoidColonySky : CustomSky, ICWRLoader
    {
        internal static string Name => "CWRMod:VoidColonySky";

        private bool active;
        private float intensity;
        private float shaderTime;

        void ICWRLoader.LoadData() {
            if (Main.dedServ) return;

            SkyManager.Instance[Name] = this;
            Filters.Scene[Name] = new Filter(new ScreenShaderData("FilterMiniTower")
                .UseColor(0.03f, 0.01f, 0.05f)
                .UseOpacity(0.3f), EffectPriority.VeryHigh);
        }

        void ICWRLoader.UnLoadData() { }

        public override void Activate(Vector2 position, params object[] args) {
            active = true;
        }

        public override void Deactivate(params object[] args) {
            active = false;
        }

        public override bool IsActive() => active || intensity > 0.001f;

        public override void Reset() {
            active = false;
            intensity = 0f;
            shaderTime = 0f;
        }

        public override void Update(GameTime gameTime) {
            intensity = MathHelper.Lerp(intensity, active ? 1f : 0f, 0.02f);
            if (intensity > 0.001f) {
                shaderTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth) {
            if (intensity <= 0.001f) return;
            if (maxDepth < 0f || minDepth >= 0f) {
                return;//只绘制一次，并且保证图层最上
            }

            Effect shader = EffectLoader.VoidColonySky?.Value;
            if (shader == null) {
                //回退：绘制纯色背景
                DrawFallbackBackground(spriteBatch);
                return;
            }

            var gd = Main.instance.GraphicsDevice;
            //使用实际视口尺寸，确保覆盖完整屏幕
            int vpW = gd.Viewport.Width;
            int vpH = gd.Viewport.Height;

            //设置着色器参数
            shader.Parameters["uTime"]?.SetValue(shaderTime);
            shader.Parameters["intensity"]?.SetValue(intensity);
            shader.Parameters["aspectRatio"]?.SetValue((float)vpW / (float)vpH);
            shader.Parameters["uNoiseTex"]?.SetValue(CWRAsset.Extra_193.Value);

            //颜色参数 — 红黑亚空间主题
            shader.Parameters["voidCore"]?.SetValue(new Vector3(0.02f, 0.005f, 0.01f));
            shader.Parameters["fireColor1"]?.SetValue(new Vector3(1.0f, 0.55f, 0.12f));  //亮橙火焰
            shader.Parameters["fireColor2"]?.SetValue(new Vector3(0.6f, 0.08f, 0.03f));  //深红
            shader.Parameters["nebulaColor"]?.SetValue(new Vector3(0.12f, 0.04f, 0.2f)); //暗紫星云

            //切换到 Immediate 模式绘制着色器
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearWrap, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            shader.CurrentTechnique.Passes[0].Apply();

            //全屏绘制
            spriteBatch.Draw(VaultAsset.placeholder2.Value,
                new Rectangle(0, 0, vpW, vpH),
                Color.White);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// 着色器不可用时的回退方案
        /// </summary>
        private void DrawFallbackBackground(SpriteBatch spriteBatch) {
            Color voidColor = new Color(15, 5, 20) * intensity;
            spriteBatch.Draw(
                Terraria.GameContent.TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                voidColor);
        }
    }
}
