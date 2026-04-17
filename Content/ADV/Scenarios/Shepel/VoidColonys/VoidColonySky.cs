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
    /// 虚空聚落天空 - 深紫色虚空背景，亚空间裂隙在远处闪烁
    /// 现实与亚空间的屏障在这里极其薄弱，天空中可以看到裂缝和能量流
    /// </summary>
    internal class VoidColonySky : CustomSky, ICWRLoader
    {
        internal static string Name => "CWRMod:VoidColonySky";

        private bool active;
        private float intensity;

        //亚空间裂隙闪光
        private float riftFlashIntensity;
        private float riftFlashTimer;

        //星尘粒子
        private VoidDustParticle[] dustParticles;
        private const int MaxDustParticles = 80;

        void ICWRLoader.LoadData() {
            if (Main.dedServ) return;

            SkyManager.Instance[Name] = this;
            Filters.Scene[Name] = new Filter(new ScreenShaderData("FilterMiniTower")
                .UseColor(0.05f, 0.02f, 0.1f)
                .UseOpacity(0.4f), EffectPriority.VeryHigh);
        }

        void ICWRLoader.UnLoadData() {
            dustParticles = null;
        }

        public override void Activate(Vector2 position, params object[] args) {
            active = true;
            InitializeParticles();
        }

        public override void Deactivate(params object[] args) {
            active = false;
        }

        public override bool IsActive() => active || intensity > 0.001f;

        public override void Reset() {
            active = false;
            intensity = 0f;
        }

        private void InitializeParticles() {
            if (dustParticles != null) return;

            dustParticles = new VoidDustParticle[MaxDustParticles];
            for (int i = 0; i < MaxDustParticles; i++) {
                dustParticles[i] = new VoidDustParticle {
                    Position = new Vector2(
                        Main.rand.NextFloat() * Main.screenWidth,
                        Main.rand.NextFloat() * Main.screenHeight),
                    Velocity = new Vector2(
                        Main.rand.NextFloat(-0.3f, 0.3f),
                        Main.rand.NextFloat(-0.5f, -0.1f)),
                    Scale = Main.rand.NextFloat(0.5f, 2f),
                    Alpha = Main.rand.NextFloat(0.2f, 0.6f),
                    Color = Main.rand.NextBool()
                        ? new Color(140, 60, 200) //紫色
                        : new Color(60, 180, 220), //青色
                    Life = Main.rand.Next(120, 360),
                    MaxLife = 360
                };
            }
        }

        public override void Update(GameTime gameTime) {
            //平滑过渡
            intensity = MathHelper.Lerp(intensity, active ? 1f : 0f, 0.02f);

            if (intensity <= 0.001f) return;

            //随机触发亚空间裂隙闪光
            riftFlashTimer -= 1f;
            if (riftFlashTimer <= 0) {
                if (Main.rand.NextBool(180)) {
                    riftFlashIntensity = Main.rand.NextFloat(0.3f, 0.8f);
                    riftFlashTimer = Main.rand.Next(30, 90);
                }
            }
            riftFlashIntensity *= 0.95f;

            //更新星尘粒子
            if (dustParticles != null) {
                for (int i = 0; i < MaxDustParticles; i++) {
                    ref var p = ref dustParticles[i];
                    p.Position += p.Velocity;
                    p.Life--;

                    //淡入淡出
                    float lifeRatio = p.Life / (float)p.MaxLife;
                    float fadeIn = Math.Min((1f - lifeRatio) * 5f, 1f);
                    float fadeOut = Math.Min(lifeRatio * 5f, 1f);
                    p.CurrentAlpha = p.Alpha * fadeIn * fadeOut;

                    //重生
                    if (p.Life <= 0 || p.Position.Y < -20) {
                        p.Position = new Vector2(
                            Main.rand.NextFloat() * Main.screenWidth,
                            Main.screenHeight + 10);
                        p.Velocity = new Vector2(
                            Main.rand.NextFloat(-0.3f, 0.3f),
                            Main.rand.NextFloat(-0.5f, -0.1f));
                        p.Life = p.MaxLife;
                        p.Scale = Main.rand.NextFloat(0.5f, 2f);
                    }
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth) {
            if (intensity <= 0.001f) return;
            if (!(maxDepth >= float.MaxValue) || !(minDepth < float.MaxValue)) return;

            //深紫色虚空背景
            Color voidColor = new Color(15, 5, 25) * intensity;
            Color riftGlow = new Color(80, 20, 120) * riftFlashIntensity * intensity;

            //绘制基础虚空背景
            spriteBatch.Draw(
                Terraria.GameContent.TextureAssets.MagicPixel.Value,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                voidColor);

            //亚空间裂隙闪光叠加
            if (riftFlashIntensity > 0.01f) {
                spriteBatch.Draw(
                    Terraria.GameContent.TextureAssets.MagicPixel.Value,
                    new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                    riftGlow);
            }

            //绘制漂浮的虚空尘粒
            if (dustParticles != null) {
                for (int i = 0; i < MaxDustParticles; i++) {
                    ref var p = ref dustParticles[i];
                    if (p.CurrentAlpha <= 0.01f) continue;

                    Color c = p.Color * p.CurrentAlpha * intensity;
                    Vector2 size = new Vector2(p.Scale * 2f);
                    spriteBatch.Draw(
                        Terraria.GameContent.TextureAssets.MagicPixel.Value,
                        new Rectangle(
                            (int)(p.Position.X - size.X / 2),
                            (int)(p.Position.Y - size.Y / 2),
                            (int)size.X, (int)size.Y),
                        c);
                }
            }
        }

        private struct VoidDustParticle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Scale;
            public float Alpha;
            public float CurrentAlpha;
            public Color Color;
            public int Life;
            public int MaxLife;
        }
    }
}
