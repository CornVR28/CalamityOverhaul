using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols
{
    /// <summary>
    /// 科尔托星系：银河系放大聚焦 → 标记科尔托 → 切换为行星链正视图 → 标记第三行星
    /// </summary>
    internal partial class GalacticCrisisRender
    {
        #region 科尔托参数与数据

        //科尔托星系在银河系中的位置（旋臂3，径向0.7）
        private const float KortoRadialDistance = 0.7f;
        private const int KortoArmIndex = 3;
        private const float KortoAngleOffset = -0.1f;

        //缩放聚焦阶段
        private static float kortoZoomProgress;
        private static float kortoZoomScale;
        private static Vector2 kortoZoomOffset;
        private static float kortoMarkerAlpha;

        //行星视图阶段
        private static float kortoPlanetViewProgress;
        private static float kortoPlanetTransition;
        private const int KortoPlanetCount = 6;
        private static float kortoPlanetOrbitTimer;

        //科尔托星系恒星（用于行星视图中心）
        private static readonly Color KortoStarColor = new(255, 200, 140);
        //第三行星标记
        private static float kortoTargetBlinkTimer;

        #endregion

        #region 初始化与清理

        private static void InitKorto() {
            kortoZoomProgress = 0f;
            kortoZoomScale = 1f;
            kortoZoomOffset = Vector2.Zero;
            kortoMarkerAlpha = 0f;
            kortoPlanetViewProgress = 0f;
            kortoPlanetTransition = 0f;
            kortoPlanetOrbitTimer = 0f;
            kortoTargetBlinkTimer = 0f;
        }

        private static void CleanupKorto() {
            kortoZoomProgress = 0f;
            kortoZoomScale = 1f;
            kortoZoomOffset = Vector2.Zero;
        }

        #endregion

        #region 逻辑更新

        /// <summary>
        /// 科尔托聚焦阶段：银河系不断放大，镜头平移到科尔托位置
        /// </summary>
        private static void UpdateKortoZoomPhase() {
            kortoZoomProgress = MathF.Min(kortoZoomProgress + 0.006f, 1f);
            phaseProgress = kortoZoomProgress;

            float ease = CWRUtils.EaseInOutCubic(kortoZoomProgress);

            //缩放：从1x到5x
            kortoZoomScale = MathHelper.Lerp(1f, 5f, ease);

            //计算科尔托在银河中的原始位置偏移
            float r = KortoRadialDistance * GalaxyRadius;
            float spiralTightness = 2.8f;
            float armAngle = MathHelper.TwoPi * KortoArmIndex / GalaxyArmCount;
            float angle = armAngle + KortoAngleOffset + spiralTightness * MathF.Log(MathF.Max(KortoRadialDistance, 0.05f)) + galaxyRotation;
            Vector2 kortoLocalPos = new(MathF.Cos(angle) * r, MathF.Sin(angle) * r);

            //镜头偏移：将科尔托位置移到画面中心
            kortoZoomOffset = -kortoLocalPos * ease;

            //科尔托标记在缩放超过60%后开始出现
            float markerFade = MathHelper.Clamp((kortoZoomProgress - 0.6f) / 0.3f, 0f, 1f);
            kortoMarkerAlpha = CWRUtils.EaseOutCubic(markerFade);

            //缩放过程中逐渐降低glitch
            glitchIntensity = MathHelper.Lerp(glitchIntensity, 0.01f, 0.05f);
        }

        /// <summary>
        /// 科尔托行星视图阶段：从银河俯视图过渡到行星链正视图
        /// </summary>
        private static void UpdateKortoPlanetViewPhase() {
            kortoPlanetViewProgress = MathF.Min(kortoPlanetViewProgress + 0.005f, 1f);
            phaseProgress = kortoPlanetViewProgress;

            //过渡：银河淡出，行星视图淡入
            kortoPlanetTransition = MathF.Min(kortoPlanetTransition + 0.02f, 1f);

            //行星轨道运动
            kortoPlanetOrbitTimer += 0.008f;

            //目标标记闪烁
            kortoTargetBlinkTimer += 0.06f;

            glitchIntensity = MathHelper.Lerp(glitchIntensity, 0.005f, 0.05f);
        }

        #endregion

        #region 绘制

        /// <summary>
        /// 在KortoZoom阶段绘制放大中的银河系 + 科尔托标记
        /// </summary>
        private static void DrawKortoZoomGalaxy(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            //应用缩放和偏移
            Vector2 zoomCenter = center + kortoZoomOffset * kortoZoomScale;

            float reveal = CWRUtils.EaseOutCubic(galaxyRevealProgress);
            float galaxyAlpha = alpha * reveal;

            //缩放后的银河核心
            DrawGalaxyCoreZoomed(sb, zoomCenter, galaxyAlpha);

            Texture2D softGlow = CWRAsset.SoftGlow?.Value;

            //绘制缩放后的恒星
            foreach (var star in galaxyStars) {
                if (star.RadialDistance > reveal) continue;
                if (star.ExtinctionMarked && star.ExtinctionStage >= 2 && star.ExtinctionStageTimer >= ExtinctionFadeDuration) {
                    continue;
                }

                Vector2 pos = star.GetPosition(galaxyRotation, GalaxyRadius);
                Vector2 screenPos = zoomCenter + pos * kortoZoomScale;

                //只绘制在合理屏幕范围内的恒星
                Rectangle panelRect = GetPanelRect();
                if (screenPos.X < panelRect.X - 20 || screenPos.X > panelRect.Right + 20 ||
                    screenPos.Y < panelRect.Y - 20 || screenPos.Y > panelRect.Bottom + 20) continue;

                float flicker = MathF.Sin(globalTimer * 2f + star.BrightnessPhase) * 0.2f + 0.8f;
                float finalBrightness = star.Brightness * flicker * galaxyAlpha;
                float drawSize = star.Size * MathF.Min(kortoZoomScale * 0.6f, 2.5f);

                Color starColor = star.BaseColor;
                //灭绝后恒星保持暗色
                if (star.ExtinctionMarked && star.ExtinctionStage >= 2) {
                    float fadeT = MathF.Min(star.ExtinctionStageTimer / ExtinctionFadeDuration, 1f);
                    float shrink = 1f - CWRUtils.EaseInCubic(fadeT);
                    drawSize *= shrink;
                    finalBrightness *= shrink;
                    if (drawSize < 0.05f) continue;
                }
                starColor *= finalBrightness;

                sb.Draw(pixel, screenPos, new Rectangle(0, 0, 1, 1),
                    starColor, 0f, new Vector2(0.5f), new Vector2(drawSize), SpriteEffects.None, 0f);

                if (softGlow != null && star.Brightness > 0.7f) {
                    Color glowColor = starColor * 0.25f;
                    glowColor.A = 0;
                    sb.Draw(softGlow, screenPos, null, glowColor, 0f,
                        new Vector2(softGlow.Width * 0.5f, softGlow.Height * 0.5f),
                        drawSize * 0.12f, SpriteEffects.None, 0f);
                }
            }

            //绘制科尔托标记
            if (kortoMarkerAlpha > 0.01f) {
                DrawKortoMarker(sb, zoomCenter, alpha * kortoMarkerAlpha);
            }
        }

        private static void DrawGalaxyCoreZoomed(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D softGlow = CWRAsset.SoftGlow?.Value;
            if (softGlow == null) return;

            Color coreColor = new Color(255, 240, 200);
            Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);

            for (int i = 3; i >= 0; i--) {
                float layerScale = GalaxyCoreRadius * (0.02f + i * 0.015f) * MathF.Min(kortoZoomScale, 3f);
                float layerAlpha = alpha * (0.3f - i * 0.05f);
                float pulse = MathF.Sin(globalTimer * 1.5f + i * 0.5f) * 0.12f + 0.88f;
                layerAlpha *= pulse;

                Color layerColor = Color.Lerp(coreColor, new Color(180, 210, 255), i * 0.2f);
                layerColor.A = 0;

                sb.Draw(softGlow, center, null, layerColor * layerAlpha, 0f,
                    glowOrigin, layerScale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>
        /// 绘制科尔托星系标记：瞄准框 + 文字 + 脉冲光环
        /// </summary>
        private static void DrawKortoMarker(SpriteBatch sb, Vector2 zoomCenter, float alpha) {
            float r = KortoRadialDistance * GalaxyRadius;
            float spiralTightness = 2.8f;
            float armAngle = MathHelper.TwoPi * KortoArmIndex / GalaxyArmCount;
            float angle = armAngle + KortoAngleOffset + spiralTightness * MathF.Log(MathF.Max(KortoRadialDistance, 0.05f)) + galaxyRotation;
            Vector2 kortoPos = zoomCenter + new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r) * kortoZoomScale;

            Texture2D pixel = VaultAsset.placeholder2.Value;
            Texture2D softGlow = CWRAsset.SoftGlow?.Value;
            if (pixel == null) return;

            Color markerColor = new Color(255, 180, 60);
            float pulse = MathF.Sin(globalTimer * 3f) * 0.3f + 0.7f;

            //瞄准框四角
            float frameSize = 18f + MathF.Sin(globalTimer * 2f) * 3f;
            float cornerLen = 8f;
            Color frameColor = markerColor * (alpha * 0.9f * pulse);

            //左上
            sb.Draw(pixel, kortoPos + new Vector2(-frameSize, -frameSize), new Rectangle(0, 0, 1, 1),
                frameColor, 0f, Vector2.Zero, new Vector2(cornerLen, 1.5f), SpriteEffects.None, 0f);
            sb.Draw(pixel, kortoPos + new Vector2(-frameSize, -frameSize), new Rectangle(0, 0, 1, 1),
                frameColor, 0f, Vector2.Zero, new Vector2(1.5f, cornerLen), SpriteEffects.None, 0f);
            //右上
            sb.Draw(pixel, kortoPos + new Vector2(frameSize - cornerLen, -frameSize), new Rectangle(0, 0, 1, 1),
                frameColor, 0f, Vector2.Zero, new Vector2(cornerLen, 1.5f), SpriteEffects.None, 0f);
            sb.Draw(pixel, kortoPos + new Vector2(frameSize - 1.5f, -frameSize), new Rectangle(0, 0, 1, 1),
                frameColor, 0f, Vector2.Zero, new Vector2(1.5f, cornerLen), SpriteEffects.None, 0f);
            //左下
            sb.Draw(pixel, kortoPos + new Vector2(-frameSize, frameSize - 1.5f), new Rectangle(0, 0, 1, 1),
                frameColor, 0f, Vector2.Zero, new Vector2(cornerLen, 1.5f), SpriteEffects.None, 0f);
            sb.Draw(pixel, kortoPos + new Vector2(-frameSize, frameSize - cornerLen), new Rectangle(0, 0, 1, 1),
                frameColor, 0f, Vector2.Zero, new Vector2(1.5f, cornerLen), SpriteEffects.None, 0f);
            //右下
            sb.Draw(pixel, kortoPos + new Vector2(frameSize - cornerLen, frameSize - 1.5f), new Rectangle(0, 0, 1, 1),
                frameColor, 0f, Vector2.Zero, new Vector2(cornerLen, 1.5f), SpriteEffects.None, 0f);
            sb.Draw(pixel, kortoPos + new Vector2(frameSize - 1.5f, frameSize - cornerLen), new Rectangle(0, 0, 1, 1),
                frameColor, 0f, Vector2.Zero, new Vector2(1.5f, cornerLen), SpriteEffects.None, 0f);

            //中心光点
            if (softGlow != null) {
                Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);
                Color centerGlow = markerColor;
                centerGlow.A = 0;
                sb.Draw(softGlow, kortoPos, null, centerGlow * (alpha * 0.6f * pulse), 0f,
                    glowOrigin, 0.12f, SpriteEffects.None, 0f);

                //外圈脉冲
                float ringPulse = (MathF.Sin(globalTimer * 1.5f) + 1f) * 0.5f;
                sb.Draw(softGlow, kortoPos, null,
                    centerGlow * (alpha * 0.2f * (1f - ringPulse)),
                    0f, glowOrigin, 0.25f + ringPulse * 0.1f, SpriteEffects.None, 0f);
            }

            //标注文字
            Color textColor = markerColor * alpha;
            Utils.DrawBorderString(sb, "KORTO SYSTEM", kortoPos + new Vector2(frameSize + 6, -8),
                textColor, 0.42f);
            Utils.DrawBorderString(sb, "◢ TARGET LOCKED ◣", kortoPos + new Vector2(frameSize + 6, 6),
                new Color(255, 100, 60) * (alpha * pulse), 0.32f);
        }

        /// <summary>
        /// 绘制行星链正视图：恒星 + 6颗行星的轨道 + 标记第三行星
        /// </summary>
        private static void DrawKortoPlanetView(SpriteBatch sb, Vector2 center, float alpha) {
            float viewAlpha = alpha * CWRUtils.EaseOutCubic(kortoPlanetTransition);
            if (viewAlpha < 0.01f) return;

            Texture2D pixel = VaultAsset.placeholder2.Value;
            Texture2D softGlow = CWRAsset.SoftGlow?.Value;
            if (pixel == null) return;

            Rectangle panelRect = GetPanelRect();

            //行星视图中心偏左（恒星位置）
            Vector2 starPos = new(panelRect.X + panelRect.Width * 0.15f, center.Y);

            //绘制恒星
            DrawKortoStar(sb, starPos, viewAlpha);

            //绘制行星链（水平分布，从左到右距离递增）
            float planetSpacing = (panelRect.Width * 0.75f) / (KortoPlanetCount + 1);

            for (int i = 0; i < KortoPlanetCount; i++) {
                float planetX = starPos.X + planetSpacing * (i + 1);
                //每颗行星有轻微垂直波动
                float yOffset = MathF.Sin(kortoPlanetOrbitTimer * (1.5f - i * 0.15f) + i * 1.7f) * (8f + i * 2f);
                Vector2 planetPos = new(planetX, center.Y + yOffset);

                bool isTarget = (i == 2); //第三行星（index 2）
                DrawKortoPlanet(sb, planetPos, i, isTarget, viewAlpha);

                //轨道线（虚线）
                DrawOrbitLine(sb, starPos, planetPos, viewAlpha * 0.25f);
            }

            //顶部标题
            DrawKortoPlanetViewHeader(sb, panelRect, viewAlpha);
        }

        private static void DrawKortoStar(SpriteBatch sb, Vector2 pos, float alpha) {
            Texture2D softGlow = CWRAsset.SoftGlow?.Value;
            Texture2D pixel = VaultAsset.placeholder2.Value;

            if (softGlow != null) {
                Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);
                float pulse = MathF.Sin(globalTimer * 1.2f) * 0.1f + 0.9f;

                //外层光晕
                Color outerGlow = new Color(255, 200, 120);
                outerGlow.A = 0;
                sb.Draw(softGlow, pos, null, outerGlow * (alpha * 0.35f * pulse), 0f,
                    glowOrigin, 0.6f, SpriteEffects.None, 0f);

                //核心
                Color coreGlow = new Color(255, 240, 200);
                coreGlow.A = 0;
                sb.Draw(softGlow, pos, null, coreGlow * (alpha * 0.8f), 0f,
                    glowOrigin, 0.15f, SpriteEffects.None, 0f);

                //亮点
                sb.Draw(softGlow, pos, null, Color.White * (alpha * 0.5f), 0f,
                    glowOrigin, 0.05f, SpriteEffects.None, 0f);
            } else if (pixel != null) {
                sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1),
                    KortoStarColor * alpha, 0f, new Vector2(0.5f),
                    new Vector2(12f), SpriteEffects.None, 0f);
            }
        }

        private static void DrawKortoPlanet(SpriteBatch sb, Vector2 pos, int index, bool isTarget, float alpha) {
            Texture2D softGlow = CWRAsset.SoftGlow?.Value;
            Texture2D pixel = VaultAsset.placeholder2.Value;

            //每颗行星的尺寸和颜色
            float[] planetSizes = [3f, 4.5f, 6f, 8f, 5f, 3.5f];
            Color[] planetColors = [
                new Color(180, 160, 140),  //I: 岩石行星
                new Color(200, 180, 150),  //II: 沙漠行星
                new Color(100, 160, 200),  //III: 目标行星（蓝绿色，类地）
                new Color(220, 180, 120),  //IV: 气态巨行星
                new Color(160, 140, 180),  //V: 冰巨行星
                new Color(140, 130, 150),  //VI: 矮行星
            ];

            float size = planetSizes[index];
            Color baseColor = planetColors[index];

            if (isTarget) {
                //第三行星特殊处理：危险标记
                float targetPulse = MathF.Sin(kortoTargetBlinkTimer) * 0.3f + 0.7f;

                //红色警告光环
                if (softGlow != null) {
                    Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);

                    //外圈危险光环
                    Color dangerGlow = new Color(255, 60, 30);
                    dangerGlow.A = 0;
                    float ringScale = 0.2f + MathF.Sin(kortoTargetBlinkTimer * 0.8f) * 0.05f;
                    sb.Draw(softGlow, pos, null, dangerGlow * (alpha * 0.35f * targetPulse), 0f,
                        glowOrigin, ringScale, SpriteEffects.None, 0f);

                    //行星本体光晕
                    Color planetGlow = baseColor;
                    planetGlow.A = 0;
                    sb.Draw(softGlow, pos, null, planetGlow * (alpha * 0.5f), 0f,
                        glowOrigin, size * 0.02f, SpriteEffects.None, 0f);
                }

                //行星像素点
                if (pixel != null) {
                    sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1),
                        baseColor * (alpha * 0.9f), 0f, new Vector2(0.5f),
                        new Vector2(size), SpriteEffects.None, 0f);
                }

                //瞄准框
                DrawTargetReticle(sb, pos, alpha * targetPulse);

                //文字标注
                Color targetTextColor = new Color(255, 80, 40) * (alpha * targetPulse);
                Utils.DrawBorderString(sb, "KORTO-III", pos + new Vector2(-20, -size - 22),
                    targetTextColor, 0.45f);

                //副标题
                float subtitleAlpha = alpha * MathF.Max(0f, (kortoPlanetViewProgress - 0.4f) * 2.5f);
                if (subtitleAlpha > 0.01f) {
                    Color subColor = new Color(255, 200, 60) * subtitleAlpha;
                    Utils.DrawBorderString(sb, "◢ PRIMARY OBJECTIVE ◣", pos + new Vector2(-45, size + 10),
                        subColor, 0.35f);
                }
            } else {
                //普通行星
                if (softGlow != null) {
                    Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);
                    Color planetGlow = baseColor;
                    planetGlow.A = 0;
                    sb.Draw(softGlow, pos, null, planetGlow * (alpha * 0.3f), 0f,
                        glowOrigin, size * 0.015f, SpriteEffects.None, 0f);
                }

                if (pixel != null) {
                    sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1),
                        baseColor * (alpha * 0.7f), 0f, new Vector2(0.5f),
                        new Vector2(size), SpriteEffects.None, 0f);
                }

                //编号标注
                string label = (index + 1).ToString();
                Color labelColor = new Color(150, 180, 220) * (alpha * 0.5f);
                Utils.DrawBorderString(sb, label, pos + new Vector2(-3, -size - 14),
                    labelColor, 0.35f);
            }
        }

        /// <summary>
        /// 绘制目标行星的瞄准十字线
        /// </summary>
        private static void DrawTargetReticle(SpriteBatch sb, Vector2 pos, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            Color reticleColor = new Color(255, 70, 40) * (alpha * 0.8f);
            float reticleSize = 16f + MathF.Sin(kortoTargetBlinkTimer * 1.5f) * 2f;
            float gap = 5f;
            float lineLen = reticleSize - gap;

            //上
            sb.Draw(pixel, pos + new Vector2(-0.5f, -reticleSize), new Rectangle(0, 0, 1, 1),
                reticleColor, 0f, Vector2.Zero, new Vector2(1f, lineLen), SpriteEffects.None, 0f);
            //下
            sb.Draw(pixel, pos + new Vector2(-0.5f, gap), new Rectangle(0, 0, 1, 1),
                reticleColor, 0f, Vector2.Zero, new Vector2(1f, lineLen), SpriteEffects.None, 0f);
            //左
            sb.Draw(pixel, pos + new Vector2(-reticleSize, -0.5f), new Rectangle(0, 0, 1, 1),
                reticleColor, 0f, Vector2.Zero, new Vector2(lineLen, 1f), SpriteEffects.None, 0f);
            //右
            sb.Draw(pixel, pos + new Vector2(gap, -0.5f), new Rectangle(0, 0, 1, 1),
                reticleColor, 0f, Vector2.Zero, new Vector2(lineLen, 1f), SpriteEffects.None, 0f);
        }

        /// <summary>
        /// 绘制从恒星到行星的轨道虚线
        /// </summary>
        private static void DrawOrbitLine(SpriteBatch sb, Vector2 from, Vector2 to, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            Color lineColor = new Color(60, 120, 180) * alpha;
            Vector2 dir = to - from;
            float dist = dir.Length();
            if (dist < 1f) return;
            dir /= dist;
            float segAngle = MathF.Atan2(dir.Y, dir.X);

            float dashLen = 4f;
            float gapLen = 6f;
            float pos = 20f; //从恒星附近开始
            while (pos < dist - 5f) {
                Vector2 dashStart = from + dir * pos;
                float len = MathF.Min(dashLen, dist - 5f - pos);
                sb.Draw(pixel, dashStart, new Rectangle(0, 0, 1, 1),
                    lineColor, segAngle, Vector2.Zero,
                    new Vector2(len, 0.5f), SpriteEffects.None, 0f);
                pos += dashLen + gapLen;
            }
        }

        /// <summary>
        /// 行星视图的顶部信息标题
        /// </summary>
        private static void DrawKortoPlanetViewHeader(SpriteBatch sb, Rectangle panelRect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            Color techColor = new Color(60, 160, 220);

            //副标题区域
            float infoY = panelRect.Y + 42f;
            Color infoColor = techColor * (alpha * 0.7f);
            Utils.DrawBorderString(sb, "KORTO SYSTEM — 6 PLANETS", new Vector2(panelRect.X + 12, infoY),
                infoColor, 0.38f);

            //分隔线
            sb.Draw(pixel, new Vector2(panelRect.X + 6, infoY + 18), new Rectangle(0, 0, 1, 1),
                techColor * (alpha * 0.3f), 0f, Vector2.Zero,
                new Vector2(panelRect.Width - 12, 1f), SpriteEffects.None, 0f);

            //右侧状态信息
            string statusText = "STATUS: COMPROMISED";
            Color statusColor = new Color(255, 80, 40) * (alpha * 0.8f);
            var font = FontAssets.MouseText.Value;
            Vector2 statusSize = font.MeasureString(statusText) * 0.35f;
            Utils.DrawBorderString(sb, statusText,
                new Vector2(panelRect.Right - statusSize.X - 12, infoY),
                statusColor, 0.35f);
        }

        #endregion
    }
}
