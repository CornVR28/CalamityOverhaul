using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols
{
    /// <summary>
    /// 银河危机剧情演出渲染器（主控）
    /// 负责生命周期管理、阶段控制和绘制调度
    /// 具体的银河系/虫群/灭绝令/面板绘制逻辑分布在各partial文件中
    /// </summary>
    internal partial class GalacticCrisisRender : UIHandle
    {
        public override bool Active => active || fadeProgress > 0.01f;
        public override float RenderPriority => 0.88f;

        #region 动画阶段定义

        internal enum AnimPhase
        {
            None = 0,
            GalaxyReveal,
            SwarmApproach,
            ExtinctionProtocol,
            Idle,
            KortoZoom,
            KortoPlanetView,
            FadeOut,
        }

        #endregion

        #region 全局状态

        private static bool active;
        private static float fadeProgress;
        private static AnimPhase currentPhase = AnimPhase.None;
        private static float phaseTimer;
        private static float phaseProgress;

        //全息投影通用效果
        private static float hologramFlicker;
        private static float scanLineProgress;
        private static float globalTimer;

        //面板参数
        private const float BasePanelWidth = 620f;
        private const float BasePanelHeight = 580f;
        private const float ExpandedPanelWidth = 860f;
        private const float ExpandedPanelHeight = 740f;
        private const int BorderThickness = 3;
        private static float panelExpandProgress;

        //信号干扰
        private static float glitchIntensity;
        private static float glitchTimer;
        private static int glitchFrameSkip;

        #endregion

        #region 生命周期

        internal static void Activate() {
            active = true;
            fadeProgress = 0f;
            currentPhase = AnimPhase.None;
            phaseTimer = 0f;
            phaseProgress = 0f;
            hologramFlicker = 0f;
            scanLineProgress = 0f;
            globalTimer = 0f;
            glitchIntensity = 0f;
            glitchTimer = 0f;
            glitchFrameSkip = 0;
            panelExpandProgress = 0f;

            InitGalaxy();
            InitSwarm();
            InitExtinction();
            InitKorto();
        }

        internal static void SetPhase(AnimPhase phase) {
            if (currentPhase == phase) return;
            currentPhase = phase;
            phaseTimer = 0f;
            phaseProgress = 0f;

            switch (phase) {
                case AnimPhase.GalaxyReveal:
                    SoundEngine.PlaySound(SoundID.DD2_EtherianPortalSpawnEnemy with {
                        Volume = 0.5f, Pitch = 0.4f, MaxInstances = 1
                    });
                    break;
                case AnimPhase.SwarmApproach:
                    SoundEngine.PlaySound(SoundID.Zombie105 with {
                        Volume = 0.3f, Pitch = -0.6f, MaxInstances = 1
                    });
                    break;
                case AnimPhase.ExtinctionProtocol:
                    SoundEngine.PlaySound(SoundID.Item117 with {
                        Volume = 0.6f, Pitch = -0.3f, MaxInstances = 1
                    });
                    break;
                case AnimPhase.KortoZoom:
                    SoundEngine.PlaySound(SoundID.DD2_EtherianPortalDryadTouch with {
                        Volume = 0.4f, Pitch = 0.3f, MaxInstances = 1
                    });
                    break;
                case AnimPhase.KortoPlanetView:
                    SoundEngine.PlaySound(SoundID.DD2_EtherianPortalDryadTouch with {
                        Volume = 0.3f, Pitch = 0.5f, MaxInstances = 1
                    });
                    break;
            }
        }

        internal static void Deactivate() {
            active = false;
            currentPhase = AnimPhase.FadeOut;
            phaseTimer = 0f;
        }

        internal static void ForceCleanup() {
            active = false;
            fadeProgress = 0f;
            currentPhase = AnimPhase.None;
            CleanupGalaxy();
            CleanupSwarm();
            CleanupKorto();
        }

        #endregion

        #region 逻辑更新

        public override void LogicUpdate() {
            if (!active && fadeProgress <= 0.01f) return;

            globalTimer += 0.016f;
            hologramFlicker += 0.06f;
            scanLineProgress += 0.025f;
            if (hologramFlicker > MathHelper.TwoPi) hologramFlicker -= MathHelper.TwoPi;
            if (scanLineProgress > 1f) scanLineProgress -= 1f;

            phaseTimer += 1f;
            UpdatePhase();
            UpdateGlitch();

            //面板在虫群出现后或科尔托阶段平滑扩大
            float expandTarget = (currentPhase >= AnimPhase.SwarmApproach && currentPhase != AnimPhase.FadeOut) ? 1f : 0f;
            panelExpandProgress += (expandTarget - panelExpandProgress) * 0.04f;
            panelExpandProgress = MathHelper.Clamp(panelExpandProgress, 0f, 1f);

            UpdateGalaxyLogic();
            UpdateSwarmLogic();
            UpdateExtinctionLogic();

            //整体淡入淡出
            if (active && currentPhase != AnimPhase.FadeOut) {
                fadeProgress = MathF.Min(fadeProgress + 0.03f, 1f);
            }
            else if (currentPhase == AnimPhase.FadeOut) {
                fadeProgress = MathF.Max(fadeProgress - 0.04f, 0f);
                if (fadeProgress <= 0.01f) {
                    ForceCleanup();
                }
            }

            if (!DraedonEffect.IsActive && active) {
                Deactivate();
            }
        }

        private static void UpdatePhase() {
            switch (currentPhase) {
                case AnimPhase.GalaxyReveal:
                    UpdateGalaxyRevealPhase();
                    break;
                case AnimPhase.SwarmApproach:
                    UpdateSwarmApproachPhase();
                    break;
                case AnimPhase.ExtinctionProtocol:
                    UpdateExtinctionPhase();
                    break;
                case AnimPhase.Idle:
                    UpdateIdlePhase();
                    break;
                case AnimPhase.KortoZoom:
                    UpdateKortoZoomPhase();
                    break;
                case AnimPhase.KortoPlanetView:
                    UpdateKortoPlanetViewPhase();
                    break;
            }
        }

        private static void UpdateGlitch() {
            glitchTimer += 0.1f;
            if (Main.rand.NextFloat() < glitchIntensity * 0.3f) {
                glitchFrameSkip = Main.rand.Next(1, 4);
            }
            if (glitchFrameSkip > 0) {
                glitchFrameSkip--;
            }
        }

        #endregion

        #region 绘制调度

        private static Rectangle GetPanelRect() {
            float ease = CWRUtils.EaseOutCubic(panelExpandProgress);
            float w = MathHelper.Lerp(BasePanelWidth, ExpandedPanelWidth, ease);
            float h = MathHelper.Lerp(BasePanelHeight, ExpandedPanelHeight, ease);
            int x = (int)(Main.screenWidth * 0.5f - w * 0.5f);
            int y = (int)(Main.screenHeight * 0.32f - h * 0.5f);
            return new Rectangle(x, y, (int)w, (int)h);
        }

        private static Vector2 GetMapCenter() {
            Rectangle panel = GetPanelRect();
            return new Vector2(panel.X + panel.Width * 0.5f, panel.Y + panel.Height * 0.5f);
        }

        public override void Draw(SpriteBatch sb) {
            if (fadeProgress <= 0.01f) return;
            if (glitchFrameSkip > 0 && Main.rand.NextBool(3)) return;

            float alpha = fadeProgress;
            float flicker = MathF.Sin(hologramFlicker * 1.5f) * 0.08f + 0.92f;
            alpha *= flicker;

            Vector2 center = GetMapCenter();
            Rectangle panelRect = GetPanelRect();

            DrawPanelBackground(sb, panelRect, alpha);
            DrawPanelBorder(sb, panelRect, alpha);

            //科尔托行星视图阶段：完全替换银河系绘制
            if (currentPhase == AnimPhase.KortoPlanetView && kortoPlanetTransition > 0.99f) {
                DrawKortoPlanetView(sb, center, alpha);
            }
            //科尔托缩放阶段：使用缩放版银河系绘制
            else if (currentPhase == AnimPhase.KortoZoom || (currentPhase == AnimPhase.KortoPlanetView && kortoPlanetTransition <= 0.99f)) {
                //银河系淡出（行星视图过渡中）
                float galaxyFade = currentPhase == AnimPhase.KortoPlanetView ? 1f - kortoPlanetTransition : 1f;
                if (galaxyFade > 0.01f) {
                    DrawKortoZoomGalaxy(sb, center, alpha * galaxyFade);
                }
                //行星视图淡入
                if (currentPhase == AnimPhase.KortoPlanetView && kortoPlanetTransition > 0.01f) {
                    DrawKortoPlanetView(sb, center, alpha);
                }
            }
            //正常银河系绘制
            else {
                if (galaxyRevealProgress > 0.01f) {
                    DrawGalaxy(sb, center, alpha);
                }

                if (swarmApproachProgress > 0.01f || currentPhase == AnimPhase.Idle) {
                    DrawSwarm(sb, center, alpha);
                }

                if (extinctionProgress > 0.01f) {
                    DrawExtinctionOverlay(sb, center, alpha);
                }

                if (galaxyRevealProgress > 0.5f) {
                    DrawTerraMarker(sb, center, alpha);
                }
            }

            DrawScanLineEffect(sb, panelRect, alpha);

            if (glitchIntensity > 0.02f) {
                DrawGlitchNoise(sb, panelRect, alpha);
            }

            DrawPanelHeader(sb, panelRect, alpha);
        }

        #endregion

        #region 面板UI绘制

        private static void DrawPanelBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            sb.Draw(pixel, rect, new Rectangle(0, 0, 1, 1), new Color(6, 8, 16) * (alpha * 0.92f));

            Rectangle innerRect = new(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8);
            sb.Draw(pixel, innerRect, new Rectangle(0, 0, 1, 1), new Color(10, 14, 25) * (alpha * 0.6f));

            Rectangle bottomGlow = new(rect.X + 6, rect.Bottom - 40, rect.Width - 12, 36);
            sb.Draw(pixel, bottomGlow, new Rectangle(0, 0, 1, 1), new Color(20, 35, 55) * (alpha * 0.25f));
        }

        private static void DrawPanelBorder(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            Color techColor = new Color(60, 160, 220);
            float pulse = MathF.Sin(hologramFlicker * 2f) * 0.15f + 0.85f;
            Color borderColor = techColor * (alpha * 0.85f * pulse);
            Color borderDim = techColor * (alpha * 0.5f);

            sb.Draw(pixel, new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 2, BorderThickness), borderColor);
            sb.Draw(pixel, new Rectangle(rect.X - 1, rect.Bottom - BorderThickness + 1, rect.Width + 2, BorderThickness), borderColor * 0.8f);
            sb.Draw(pixel, new Rectangle(rect.X - 1, rect.Y - 1, BorderThickness, rect.Height + 2), borderColor * 0.9f);
            sb.Draw(pixel, new Rectangle(rect.Right - BorderThickness + 1, rect.Y - 1, BorderThickness, rect.Height + 2), borderColor * 0.9f);

            sb.Draw(pixel, new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, 1), borderDim);
            sb.Draw(pixel, new Rectangle(rect.X + 4, rect.Bottom - 5, rect.Width - 8, 1), borderDim * 0.7f);
            sb.Draw(pixel, new Rectangle(rect.X + 4, rect.Y + 4, 1, rect.Height - 8), borderDim * 0.8f);
            sb.Draw(pixel, new Rectangle(rect.Right - 5, rect.Y + 4, 1, rect.Height - 8), borderDim * 0.8f);

            float cornerSize = 20f;
            Color cornerColor = new Color(80, 200, 255) * (alpha * 0.7f);
            DrawCornerDecor(sb, new Vector2(rect.X + 8, rect.Y + 8), cornerColor, cornerSize, -MathHelper.PiOver2);
            DrawCornerDecor(sb, new Vector2(rect.Right - 8, rect.Y + 8), cornerColor, cornerSize, 0f);
            DrawCornerDecor(sb, new Vector2(rect.X + 8, rect.Bottom - 8), cornerColor, cornerSize, MathHelper.Pi);
            DrawCornerDecor(sb, new Vector2(rect.Right - 8, rect.Bottom - 8), cornerColor, cornerSize, MathHelper.PiOver2);

            Color auxColor = techColor * (alpha * 0.3f);
            sb.Draw(pixel, new Rectangle(rect.X + 8, rect.Y + 8, 30, 1), auxColor);
            sb.Draw(pixel, new Rectangle(rect.X + 8, rect.Y + 8, 1, 30), auxColor);
            sb.Draw(pixel, new Rectangle(rect.Right - 38, rect.Y + 8, 30, 1), auxColor);
            sb.Draw(pixel, new Rectangle(rect.Right - 9, rect.Y + 8, 1, 30), auxColor);
            sb.Draw(pixel, new Rectangle(rect.X + 8, rect.Bottom - 9, 30, 1), auxColor);
            sb.Draw(pixel, new Rectangle(rect.X + 8, rect.Bottom - 38, 1, 30), auxColor);
            sb.Draw(pixel, new Rectangle(rect.Right - 38, rect.Bottom - 9, 30, 1), auxColor);
            sb.Draw(pixel, new Rectangle(rect.Right - 9, rect.Bottom - 38, 1, 30), auxColor);
        }

        private static void DrawPanelHeader(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            Color techColor = new Color(60, 160, 220);

            Rectangle headerRect = new(rect.X + 5, rect.Y + 5, rect.Width - 10, 28);
            sb.Draw(pixel, headerRect, new Rectangle(0, 0, 1, 1), new Color(12, 22, 38) * (alpha * 0.8f));
            sb.Draw(pixel, new Rectangle(headerRect.X, headerRect.Bottom, headerRect.Width, 2), techColor * (alpha * 0.5f));
            sb.Draw(pixel, new Rectangle(headerRect.X, headerRect.Bottom + 3, headerRect.Width, 1), techColor * (alpha * 0.2f));

            string title = currentPhase switch {
                AnimPhase.KortoZoom => "◢ STELLAR NAVIGATION ◣",
                AnimPhase.KortoPlanetView => "◢ KORTO SYSTEM OVERVIEW ◣",
                _ => "◢ GALACTIC STRATEGIC MAP ◣"
            };
            var font = FontAssets.MouseText.Value;
            Vector2 titleSize = font.MeasureString(title) * 0.45f;
            Vector2 titlePos = new(
                headerRect.X + (headerRect.Width - titleSize.X) * 0.5f,
                headerRect.Y + (headerRect.Height - titleSize.Y) * 0.5f
            );

            for (int i = 0; i < 4; i++) {
                float angle = MathHelper.TwoPi * i / 4f;
                Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 1f;
                Utils.DrawBorderString(sb, title, titlePos + offset, techColor * (alpha * 0.4f), 0.45f);
            }
            Utils.DrawBorderString(sb, title, titlePos, Color.White * alpha, 0.45f);
        }

        private static void DrawCornerDecor(SpriteBatch sb, Vector2 pos, Color color, float size, float rotation) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), color, rotation,
                new Vector2(0.5f), new Vector2(size, size * 0.2f), SpriteEffects.None, 0f);
            sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), color * 0.7f, rotation + MathHelper.PiOver2,
                new Vector2(0.5f), new Vector2(size, size * 0.2f), SpriteEffects.None, 0f);
            sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), color * 0.4f, rotation,
                new Vector2(0.5f), new Vector2(size * 0.5f, size * 0.12f), SpriteEffects.None, 0f);
        }

        private static void DrawScanLineEffect(SpriteBatch sb, Rectangle panelRect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            float scanY = panelRect.Y + 38f + scanLineProgress * (panelRect.Height - 42f);
            Color scanColor = new Color(60, 180, 255) * (alpha * 0.2f);
            sb.Draw(pixel, new Vector2(panelRect.X + 6, scanY), new Rectangle(0, 0, 1, 1),
                scanColor, 0f, Vector2.Zero, new Vector2(panelRect.Width - 12, 2f), SpriteEffects.None, 0f);

            float scan2 = (scanLineProgress + 0.5f) % 1f;
            float scan2Y = panelRect.Y + 38f + scan2 * (panelRect.Height - 42f);
            sb.Draw(pixel, new Vector2(panelRect.X + 6, scan2Y), new Rectangle(0, 0, 1, 1),
                scanColor * 0.4f, 0f, Vector2.Zero, new Vector2(panelRect.Width - 12, 1f), SpriteEffects.None, 0f);
        }

        private static void DrawGlitchNoise(SpriteBatch sb, Rectangle panelRect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            int noiseCount = (int)(glitchIntensity * 50f);
            for (int i = 0; i < noiseCount; i++) {
                float x = panelRect.X + Main.rand.NextFloat(panelRect.Width);
                float y = panelRect.Y + Main.rand.NextFloat(panelRect.Height);
                float w = Main.rand.NextFloat(5f, 50f);
                float h = Main.rand.NextFloat(1f, 3f);

                Color noiseColor = Main.rand.NextBool()
                    ? new Color(80, 200, 255) * (alpha * 0.15f)
                    : new Color(200, 50, 50) * (alpha * 0.1f);

                sb.Draw(pixel, new Vector2(x, y), new Rectangle(0, 0, 1, 1),
                    noiseColor, 0f, Vector2.Zero, new Vector2(w, h), SpriteEffects.None, 0f);
            }
        }

        #endregion
    }
}
