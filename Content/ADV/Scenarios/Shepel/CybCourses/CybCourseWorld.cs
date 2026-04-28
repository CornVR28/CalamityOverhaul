using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.WorldBuilding;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.CybCourses
{
    internal class CybCourseWorld : Subworld
    {
        //世界宽高保持极小，够用就行，不浪费内存
        public override int Width => 400;
        public override int Height => 250;

        public static bool Active => SubworldSystem.IsActive<CybCourseWorld>();

        public override List<GenPass> Tasks => [new CybCourseGen()];

        public static void Enter() => SubworldSystem.Enter<CybCourseWorld>();
        public static void Exit() => SubworldSystem.Exit();

        //加载动画累计时间，每次进入子世界时重置
        private static float _loadTime = 0f;
        //预估加载时长（秒），进度条基于此动画，超出后钉在95%
        private const float _estDuration = 5.5f;

        public override void OnEnter() {
            _loadTime = 0f;
        }

        public override void OnExit() { }

        public override void OnLoad() {
            //固定为永夜，强化赛博朋克氛围
            Main.dayTime = false;
            Main.time = 0;
            //把地表线和岩层线推到世界底部，令游戏认为整个世界处于地面以上
            //这样环境光照正常工作，不会出现地下黑暗
            Main.worldSurface = Height - 5;
            Main.rockLayer = Height - 4;
        }

        public override void Update() { }

        //完全接管加载界面的绘制逻辑
        public override void DrawSetup(GameTime gameTime) {
            //限制单帧增量避免首帧跳变
            _loadTime += 0.02f;

            PlayerInput.SetZoom_UI();
            Main.instance.GraphicsDevice.Clear(Color.Black);

            //先绘制着色器背景（单独的SpriteBatch，使用Immediate模式应用shader）
            DrawLoadingBackground(_loadTime);

            //文字层沿用base结构（Deferred + UIScaleMatrix）
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer,
                null, Main.UIScaleMatrix);
            DrawMenu(gameTime);
            Main.DrawCursor(Main.DrawThickCursor());
            Main.spriteBatch.End();
        }

        //绘制全屏着色器背景（在DrawSetup内、文字层之前调用）
        private static void DrawLoadingBackground(float time) {
            var shader = EffectLoader.CybCourseLoading?.Value;
            if (shader == null || VaultAsset.placeholder2 == null || VaultAsset.placeholder2.IsDisposed) {
                return;
            }
            int w = Main.screenWidth;
            int h = Main.screenHeight;
            float progress = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_loadTime / _estDuration, 0f, 0.95f));

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer,
                null, Main.UIScaleMatrix);

            shader.Parameters["uTime"]?.SetValue(time);
            shader.Parameters["uProgress"]?.SetValue(progress);
            shader.Parameters["uAspectRatio"]?.SetValue((float)w / h);
            shader.CurrentTechnique.Passes[0].Apply();

            Main.spriteBatch.Draw(VaultAsset.placeholder2.Value, new Rectangle(0, 0, w, h), Color.White);
            Main.spriteBatch.End();
        }

        //绘制加载界面文字层（由DrawSetup在已开启的SpriteBatch内调用）
        public override void DrawMenu(GameTime gameTime) {
            int sw = Main.screenWidth;
            int sh = Main.screenHeight;
            float progress = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(_loadTime / _estDuration, 0f, 0.95f));

            Color yellow = new Color(245, 197, 24);
            Color dimWhite = new Color(190, 205, 215);

            var titleFont = FontAssets.DeathText.Value;
            var bodyFont = FontAssets.MouseText.Value;

            //顶部左侧节点标识（对应着色器顶部水平线下方）
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, bodyFont, "CYBERSPACE NODE // 4082-E",
                new Vector2(sw * 0.032f, sh * 0.095f), dimWhite * 0.52f);

            //中央大标题（两端各加空格形成间距感）
            string title = "ENGRAM  LINK";
            Vector2 titleSz = titleFont.MeasureString(title);
            Vector2 titlePos = new Vector2(sw * 0.5f - titleSz.X * 0.5f, sh * 0.296f);
            //阴影
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, titleFont, title, titlePos + new Vector2(2f, 2f), Color.Black * 0.65f);
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, titleFont, title, titlePos, yellow);

            //加载状态文本（显示WorldGen的当前步骤信息）
            string status = (Main.statusText ?? "").ToUpperInvariant();
            Vector2 statusSz = bodyFont.MeasureString(status);
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, bodyFont, status,
                new Vector2(sw * 0.5f - statusSz.X * 0.5f, sh * 0.578f), dimWhite * 0.70f);

            //进度百分比（对齐进度条右侧）
            string progStr = $"{(int)(progress * 100):D2}%";
            Vector2 progSz = bodyFont.MeasureString(progStr);
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, bodyFont, progStr,
                new Vector2(sw * 0.970f - progSz.X, sh * 0.908f - progSz.Y * 0.5f), yellow);

            //进度条左侧标签
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, bodyFont, "INITIALIZING NEURAL BRIDGE",
                new Vector2(sw * 0.030f, sh * 0.908f - bodyFont.MeasureString("A").Y * 0.5f),
                dimWhite * 0.36f);
        }
    }
}

