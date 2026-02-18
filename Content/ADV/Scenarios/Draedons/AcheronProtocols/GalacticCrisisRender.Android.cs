using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols
{
    /// <summary>
    /// 战术人形档案展示：阿蒂丝与阿波拉的立绘、信息面板
    /// 从科尔托行星视图平滑过渡为双人档案展示
    /// </summary>
    internal partial class GalacticCrisisRender
    {
        #region 战术人形参数

        private static float androidRevealProgress;
        private static float androidGlitchTimer;
        private static float androidDataScrollTimer;

        //两人立绘的独立动画状态
        private static float artisRevealProgress;
        private static float apolaRevealProgress;
        private static float artisPortraitFloat;
        private static float apolaPortraitFloat;

        //信号丢失闪烁
        private static float signalLostBlinkTimer;

        #endregion

        #region 初始化与清理

        private static void InitAndroid() {
            androidRevealProgress = 0f;
            androidGlitchTimer = 0f;
            androidDataScrollTimer = 0f;
            artisRevealProgress = 0f;
            apolaRevealProgress = 0f;
            artisPortraitFloat = 0f;
            apolaPortraitFloat = 0f;
            signalLostBlinkTimer = 0f;
        }

        private static void CleanupAndroid() {
            androidRevealProgress = 0f;
            artisRevealProgress = 0f;
            apolaRevealProgress = 0f;
        }

        #endregion

        #region 逻辑更新

        private static void UpdateAndroidLogic() {
            if (currentPhase != AnimPhase.AndroidProfile) return;

            androidDataScrollTimer += 0.016f;
            signalLostBlinkTimer += 0.05f;

            //全局毛刺效果
            androidGlitchTimer += 0.016f;
        }

        private static void UpdateAndroidProfilePhase() {
            androidRevealProgress = MathF.Min(androidRevealProgress + 0.015f, 1f);
            phaseProgress = androidRevealProgress;

            //阿蒂丝先出现，阿波拉稍后出现
            if (androidRevealProgress > 0.1f) {
                artisRevealProgress = MathF.Min(artisRevealProgress + 0.025f, 1f);
            }
            if (androidRevealProgress > 0.3f) {
                apolaRevealProgress = MathF.Min(apolaRevealProgress + 0.025f, 1f);
            }

            //立绘浮动
            artisPortraitFloat += 0.03f;
            apolaPortraitFloat += 0.025f;

            //轻微信号干扰
            glitchIntensity = MathHelper.Lerp(0.01f, 0.05f, androidRevealProgress);
        }

        #endregion

        #region 绘制

        private static void DrawAndroidProfile(SpriteBatch sb, Vector2 center, Rectangle panelRect, float alpha) {
            float revealAlpha = alpha * CWRUtils.EaseOutCubic(androidRevealProgress);

            //面板内部区域（留出标题栏和边框）
            int margin = 40;
            Rectangle contentRect = new(
                panelRect.X + margin,
                panelRect.Y + margin,
                panelRect.Width - margin * 2,
                panelRect.Height - margin * 2
            );

            //中间分割线位置
            float dividerX = panelRect.X + panelRect.Width * 0.5f;

            //绘制中央分割线
            DrawProfileDivider(sb, dividerX, contentRect, revealAlpha);

            //左侧：阿蒂丝
            Rectangle leftPanel = new(contentRect.X, contentRect.Y, contentRect.Width / 2 - 10, contentRect.Height);
            DrawAndroidCard(sb, leftPanel, ADVAsset.Artis, AndroidArtisName.Value,
                artisRevealProgress, artisPortraitFloat, revealAlpha, true);

            //右侧：阿波拉
            Rectangle rightPanel = new((int)dividerX + 10, contentRect.Y, contentRect.Width / 2 - 10, contentRect.Height);
            DrawAndroidCard(sb, rightPanel, ADVAsset.Apola, AndroidApolaName.Value,
                apolaRevealProgress, apolaPortraitFloat, revealAlpha, false);
        }

        /// <summary>
        /// 绘制中央分割线
        /// </summary>
        private static void DrawProfileDivider(SpriteBatch sb, float x, Rectangle contentRect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            Color techColor = new Color(60, 160, 220);
            float pulse = MathF.Sin(hologramFlicker * 2f) * 0.15f + 0.85f;

            //主分割线
            float lineHeight = contentRect.Height * CWRUtils.EaseOutCubic(androidRevealProgress);
            float lineY = contentRect.Y + (contentRect.Height - lineHeight) * 0.5f;

            sb.Draw(pixel, new Vector2(x, lineY), new Rectangle(0, 0, 1, 1),
                techColor * (alpha * 0.6f * pulse), 0f, Vector2.Zero,
                new Vector2(2f, lineHeight), SpriteEffects.None, 0f);

            //分割线上的装饰节点
            int nodeCount = 5;
            for (int i = 0; i < nodeCount; i++) {
                float t = i / (float)(nodeCount - 1);
                float nodeY = lineY + lineHeight * t;
                float nodeSize = 4f + MathF.Sin(hologramFlicker + t * 3f) * 1f;

                sb.Draw(pixel, new Vector2(x - nodeSize / 2f, nodeY - nodeSize / 2f),
                    new Rectangle(0, 0, 1, 1),
                    techColor * (alpha * 0.8f * pulse), 0f, Vector2.Zero,
                    new Vector2(nodeSize), SpriteEffects.None, 0f);
            }
        }

        /// <summary>
        /// 绘制单个战术人形卡片（立绘+信息）
        /// </summary>
        private static void DrawAndroidCard(SpriteBatch sb, Rectangle area, Texture2D portrait,
            string name, float revealProgress, float floatTimer, float alpha, bool isLeft) {
            if (revealProgress <= 0.01f) return;

            float cardAlpha = alpha * CWRUtils.EaseOutCubic(revealProgress);
            Texture2D pixel = VaultAsset.placeholder2.Value;
            Color techColor = new Color(60, 160, 220);

            //立绘区域（上方大部分空间）
            int portraitHeight = (int)(area.Height * 0.65f);
            Rectangle portraitRect = new(area.X, area.Y, area.Width, portraitHeight);

            //绘制立绘背景框
            DrawPortraitFrame(sb, portraitRect, cardAlpha, revealProgress);

            //绘制立绘
            if (portrait != null) {
                DrawAndroidPortrait(sb, portrait, portraitRect, cardAlpha, revealProgress, floatTimer);
            }

            //绘制信息区域（立绘下方）
            Rectangle infoRect = new(area.X, area.Y + portraitHeight + 8, area.Width, area.Height - portraitHeight - 8);
            DrawAndroidInfo(sb, infoRect, name, cardAlpha, revealProgress, isLeft);
        }

        /// <summary>
        /// 绘制立绘背景框（科技风格边框）
        /// </summary>
        private static void DrawPortraitFrame(SpriteBatch sb, Rectangle rect, float alpha, float reveal) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            Color frameColor = new Color(40, 120, 180);
            float pulse = MathF.Sin(hologramFlicker * 2f) * 0.1f + 0.9f;
            Color borderColor = frameColor * (alpha * 0.5f * pulse);

            //半透明背景
            Color bgColor = new Color(8, 14, 28) * (alpha * 0.5f);
            sb.Draw(pixel, rect, new Rectangle(0, 0, 1, 1), bgColor);

            //边框线
            float lineWidth = rect.Width * CWRUtils.EaseOutCubic(reveal);
            float lineX = rect.X + (rect.Width - lineWidth) * 0.5f;
            //上边
            sb.Draw(pixel, new Vector2(lineX, rect.Y), new Rectangle(0, 0, 1, 1),
                borderColor, 0f, Vector2.Zero, new Vector2(lineWidth, 1f), SpriteEffects.None, 0f);
            //下边
            sb.Draw(pixel, new Vector2(lineX, rect.Bottom - 1), new Rectangle(0, 0, 1, 1),
                borderColor, 0f, Vector2.Zero, new Vector2(lineWidth, 1f), SpriteEffects.None, 0f);

            float lineHeight = rect.Height * CWRUtils.EaseOutCubic(reveal);
            float lineY = rect.Y + (rect.Height - lineHeight) * 0.5f;
            //左边
            sb.Draw(pixel, new Vector2(rect.X, lineY), new Rectangle(0, 0, 1, 1),
                borderColor, 0f, Vector2.Zero, new Vector2(1f, lineHeight), SpriteEffects.None, 0f);
            //右边
            sb.Draw(pixel, new Vector2(rect.Right - 1, lineY), new Rectangle(0, 0, 1, 1),
                borderColor, 0f, Vector2.Zero, new Vector2(1f, lineHeight), SpriteEffects.None, 0f);

            //角落装饰
            float cornerLen = 12f;
            Color cornerColor = new Color(80, 200, 255) * (alpha * 0.6f * pulse);
            //左上
            sb.Draw(pixel, new Vector2(rect.X, rect.Y), new Rectangle(0, 0, 1, 1),
                cornerColor, 0f, Vector2.Zero, new Vector2(cornerLen, 2f), SpriteEffects.None, 0f);
            sb.Draw(pixel, new Vector2(rect.X, rect.Y), new Rectangle(0, 0, 1, 1),
                cornerColor, 0f, Vector2.Zero, new Vector2(2f, cornerLen), SpriteEffects.None, 0f);
            //右上
            sb.Draw(pixel, new Vector2(rect.Right - cornerLen, rect.Y), new Rectangle(0, 0, 1, 1),
                cornerColor, 0f, Vector2.Zero, new Vector2(cornerLen, 2f), SpriteEffects.None, 0f);
            sb.Draw(pixel, new Vector2(rect.Right - 2, rect.Y), new Rectangle(0, 0, 1, 1),
                cornerColor, 0f, Vector2.Zero, new Vector2(2f, cornerLen), SpriteEffects.None, 0f);
            //左下
            sb.Draw(pixel, new Vector2(rect.X, rect.Bottom - 2), new Rectangle(0, 0, 1, 1),
                cornerColor, 0f, Vector2.Zero, new Vector2(cornerLen, 2f), SpriteEffects.None, 0f);
            sb.Draw(pixel, new Vector2(rect.X, rect.Bottom - cornerLen), new Rectangle(0, 0, 1, 1),
                cornerColor, 0f, Vector2.Zero, new Vector2(2f, cornerLen), SpriteEffects.None, 0f);
            //右下
            sb.Draw(pixel, new Vector2(rect.Right - cornerLen, rect.Bottom - 2), new Rectangle(0, 0, 1, 1),
                cornerColor, 0f, Vector2.Zero, new Vector2(cornerLen, 2f), SpriteEffects.None, 0f);
            sb.Draw(pixel, new Vector2(rect.Right - 2, rect.Bottom - cornerLen), new Rectangle(0, 0, 1, 1),
                cornerColor, 0f, Vector2.Zero, new Vector2(2f, cornerLen), SpriteEffects.None, 0f);
        }

        /// <summary>
        /// 绘制战术人形立绘（带全息投影效果）
        /// </summary>
        private static void DrawAndroidPortrait(SpriteBatch sb, Texture2D portrait, Rectangle rect,
            float alpha, float reveal, float floatTimer) {
            if (portrait == null) return;

            //计算绘制尺寸，保持纵横比并填充框架
            float texAspect = portrait.Width / (float)portrait.Height;
            float rectAspect = rect.Width / (float)rect.Height;

            int drawWidth, drawHeight;
            if (texAspect > rectAspect) {
                //纹理更宽，以高度为基准
                drawHeight = (int)(rect.Height * 0.9f);
                drawWidth = (int)(drawHeight * texAspect);
            }
            else {
                //纹理更高，以宽度为基准
                drawWidth = (int)(rect.Width * 0.85f);
                drawHeight = (int)(drawWidth / texAspect);
            }

            //居中绘制 + 浮动偏移
            float floatY = MathF.Sin(floatTimer) * 3f;
            Vector2 drawPos = new(
                rect.X + (rect.Width - drawWidth) * 0.5f,
                rect.Y + (rect.Height - drawHeight) * 0.5f + floatY
            );

            //出场动画：从下方滑入
            float slideOffset = (1f - CWRUtils.EaseOutCubic(reveal)) * 40f;
            drawPos.Y += slideOffset;

            float portraitAlpha = alpha * MathHelper.Clamp(reveal * 2f, 0f, 1f);

            Rectangle destRect = new((int)drawPos.X, (int)drawPos.Y, drawWidth, drawHeight);

            //全息投影底色光晕
            Texture2D softGlow = CWRAsset.SoftGlow?.Value;
            if (softGlow != null) {
                Vector2 glowCenter = new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f + floatY);
                Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);
                Color glowColor = new Color(40, 120, 200, 0) * (portraitAlpha * 0.15f);
                float glowScale = MathF.Max(drawWidth, drawHeight) / (float)softGlow.Width * 2.5f;
                sb.Draw(softGlow, glowCenter, null, glowColor, 0f, glowOrigin, glowScale, SpriteEffects.None, 0f);
            }

            //主立绘
            Color mainColor = Color.White * portraitAlpha;
            //轻微全息投影色调
            Color holoTint = new Color(200, 230, 255) * portraitAlpha;
            sb.Draw(portrait, destRect, null, holoTint, 0f, Vector2.Zero, SpriteEffects.None, 0f);

            //全息扫描线覆盖效果
            DrawHoloScanOverlay(sb, rect, portraitAlpha, floatY);
        }

        /// <summary>
        /// 绘制全息扫描线覆盖在立绘上
        /// </summary>
        private static void DrawHoloScanOverlay(SpriteBatch sb, Rectangle rect, float alpha, float floatY) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            //水平扫描线
            float scanProgress = (androidDataScrollTimer * 0.5f) % 1f;
            float scanY = rect.Y + scanProgress * rect.Height;
            Color scanColor = new Color(80, 200, 255, 0) * (alpha * 0.12f);
            sb.Draw(pixel, new Vector2(rect.X, scanY), new Rectangle(0, 0, 1, 1),
                scanColor, 0f, Vector2.Zero, new Vector2(rect.Width, 2f), SpriteEffects.None, 0f);

            //第二条扫描线（稍慢）
            float scan2Progress = (androidDataScrollTimer * 0.35f + 0.5f) % 1f;
            float scan2Y = rect.Y + scan2Progress * rect.Height;
            sb.Draw(pixel, new Vector2(rect.X, scan2Y), new Rectangle(0, 0, 1, 1),
                scanColor * 0.5f, 0f, Vector2.Zero, new Vector2(rect.Width, 1f), SpriteEffects.None, 0f);
        }

        /// <summary>
        /// 绘制战术人形信息面板
        /// </summary>
        private static void DrawAndroidInfo(SpriteBatch sb, Rectangle rect, string name,
            float alpha, float reveal, bool isLeft) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            var font = FontAssets.MouseText.Value;
            Color techColor = new Color(60, 160, 220);
            Color dimTech = new Color(40, 100, 160);
            float pulse = MathF.Sin(hologramFlicker * 2f) * 0.1f + 0.9f;

            //信息区域背景
            Color infoBg = new Color(6, 12, 22) * (alpha * 0.6f);
            sb.Draw(pixel, rect, new Rectangle(0, 0, 1, 1), infoBg);

            //上方分隔线
            sb.Draw(pixel, new Vector2(rect.X, rect.Y), new Rectangle(0, 0, 1, 1),
                techColor * (alpha * 0.4f), 0f, Vector2.Zero,
                new Vector2(rect.Width, 1f), SpriteEffects.None, 0f);

            float textAlpha = alpha * MathHelper.Clamp((reveal - 0.3f) * 3f, 0f, 1f);
            if (textAlpha <= 0.01f) return;

            float lineY = rect.Y + 8f;
            float lineSpacing = 22f;

            //代号标签
            DrawInfoLabel(sb, font, AndroidCodename.Value, name,
                new Vector2(rect.X + 10f, lineY), techColor, textAlpha, 0.5f);
            lineY += lineSpacing;

            //类型标签
            DrawInfoLine(sb, font, AndroidClassLabel.Value,
                new Vector2(rect.X + 10f, lineY), dimTech, textAlpha * 0.8f, 0.42f);
            lineY += lineSpacing;

            //状态标签（带闪烁警告效果）
            float statusBlink = MathF.Sin(signalLostBlinkTimer) * 0.5f + 0.5f;
            Color statusColor = Color.Lerp(new Color(200, 60, 40), new Color(255, 100, 60), statusBlink);

            DrawInfoLabel(sb, font, AndroidStatusLabel.Value, AndroidStatusLost.Value,
                new Vector2(rect.X + 10f, lineY), statusColor, textAlpha, 0.5f);
            lineY += lineSpacing;

            //信号丢失指示条（动态）
            DrawSignalLostBar(sb, new Rectangle(rect.X + 10, (int)lineY, rect.Width - 20, 6), textAlpha);
        }

        /// <summary>
        /// 绘制信息标签行（标签: 值）
        /// </summary>
        private static void DrawInfoLabel(SpriteBatch sb, dynamic font, string label, string value,
            Vector2 pos, Color color, float alpha, float scale) {
            //标签（暗色）
            Color labelColor = new Color(80, 140, 180) * (alpha * 0.7f);
            Utils.DrawBorderString(sb, label + ": ", pos, labelColor, scale);

            //值（亮色）
            float labelWidth = font.MeasureString(label + ": ").X * scale;
            Utils.DrawBorderString(sb, value, pos + new Vector2(labelWidth, 0), color * alpha, scale);
        }

        /// <summary>
        /// 绘制单行信息文本
        /// </summary>
        private static void DrawInfoLine(SpriteBatch sb, dynamic font, string text,
            Vector2 pos, Color color, float alpha, float scale) {
            Utils.DrawBorderString(sb, text, pos, color * alpha, scale);
        }

        /// <summary>
        /// 绘制信号丢失状态条（模拟信号强度动画）
        /// </summary>
        private static void DrawSignalLostBar(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (pixel == null) return;

            //背景
            Color bgColor = new Color(15, 5, 5) * (alpha * 0.5f);
            sb.Draw(pixel, rect, new Rectangle(0, 0, 1, 1), bgColor);

            //模拟噪波信号块
            int blockCount = rect.Width / 4;
            for (int i = 0; i < blockCount; i++) {
                float noise = MathF.Sin(androidDataScrollTimer * 8f + i * 1.7f) * 0.5f + 0.5f;
                noise *= MathF.Cos(androidDataScrollTimer * 3f + i * 0.9f) * 0.5f + 0.5f;

                if (noise < 0.3f) continue; //大部分时间为空，模拟信号丢失

                float blockHeight = rect.Height * noise;
                float blockY = rect.Y + (rect.Height - blockHeight);

                Color blockColor = Color.Lerp(new Color(200, 40, 30), new Color(255, 80, 50), noise);
                blockColor *= alpha * 0.6f * noise;

                sb.Draw(pixel, new Vector2(rect.X + i * 4f, blockY), new Rectangle(0, 0, 1, 1),
                    blockColor, 0f, Vector2.Zero, new Vector2(3f, blockHeight), SpriteEffects.None, 0f);
            }

            //边框
            Color borderColor = new Color(200, 50, 40) * (alpha * 0.3f);
            sb.Draw(pixel, new Vector2(rect.X, rect.Y), new Rectangle(0, 0, 1, 1),
                borderColor, 0f, Vector2.Zero, new Vector2(rect.Width, 1f), SpriteEffects.None, 0f);
            sb.Draw(pixel, new Vector2(rect.X, rect.Bottom - 1), new Rectangle(0, 0, 1, 1),
                borderColor, 0f, Vector2.Zero, new Vector2(rect.Width, 1f), SpriteEffects.None, 0f);
        }

        #endregion
    }
}
