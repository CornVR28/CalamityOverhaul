using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TimeShift;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys
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
    /// 虚空聚落天空 — 高级着色器驱动的亚空间末日背景
    /// 红黑旋涡漩涡 + 能量裂隙 + 星云 + 星辰 + 中心暗域
    /// </summary>
    internal class VoidColonySky : CustomSky, ICWRLoader
    {
        internal static string Name => "CWRMod:VoidColonySky";

        private bool active;
        private float intensity;

        void ICWRLoader.LoadData() {
            if (Main.dedServ) return;

            SkyManager.Instance[Name] = this;
            Filters.Scene[Name] = new Filter(new ScreenShaderData("FilterMiniTower")
                .UseColor(0.06f, 0.01f, 0.015f)
                .UseOpacity(0.35f), EffectPriority.VeryHigh);
        }

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
        }

        public override void Update(GameTime gameTime) {
            intensity = MathHelper.Lerp(intensity, active ? 1f : 0f, 0.02f);
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth) {
            if (maxDepth < 0f || minDepth >= 0f) {
                return;//只绘制一次，并且保证图层最上
            }

            var shader = EffectLoader.VoidColonySky?.Value;
            var gd = Main.instance.GraphicsDevice;
            //使用实际视口尺寸，确保覆盖完整屏幕
            int vpW = gd.Viewport.Width;
            int vpH = gd.Viewport.Height;

            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone);

            //传递uniforms
            float time = (float)Main.timeForVisualEffects * 0.008f;
            shader.Parameters["uTime"]?.SetValue(time);
            shader.Parameters["uIntensity"]?.SetValue(intensity);
            shader.Parameters["uAspectRatio"]?.SetValue((float)vpW / vpH);
            //时空叠加状态：过去时代混合与切换演出爆闪
            //两者都由VoidTimeShiftSystem统一驱动，保证与屏幕后处理完全同步
            shader.Parameters["uPastBlend"]?.SetValue(VoidTimeShiftSystem.FilterIntensity);
            shader.Parameters["uTransitionBurst"]?.SetValue(VoidTimeShiftSystem.TransitionStrength);

            shader.CurrentTechnique.Passes[0].Apply();

            //使用专用白色纹理绘制全屏四边形
            //着色器在UV空间(0,0)-(1,1)中程序化生成全部视觉效果
            spriteBatch.Draw(
                VaultAsset.placeholder2.Value,
                new Rectangle(0, 0, vpW, vpH),
                Color.White);

            //恢复原始SpriteBatch状态
            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.BackgroundViewMatrix.TransformationMatrix);
        }
    }
}
