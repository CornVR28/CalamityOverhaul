using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberwares.UIs
{
    /// <summary>
    ///赛博义体界面的面板渲染器
    ///负责面板背景、网格、扫描线、故障特效和标题装饰等视觉层
    /// </summary>
    internal class CyberPanelRenderer
    {
        #region 动画状态

        private float scanLinePhase;
        private float glitchTimer;
        private float glitchIntensity;
        private float nextGlitchTime;

        #endregion

        #region 公共方法

        /// <summary>
        ///触发一次故障干扰效果
        /// </summary>
        public void TriggerGlitch(float intensity) {
            glitchIntensity = MathHelper.Clamp(intensity, 0, 1);
        }

        /// <summary>
        ///推进扫描线和故障效果的动画计时器
        /// </summary>
        public void Update() {
            scanLinePhase += 0.025f;
            if (scanLinePhase > MathHelper.TwoPi) scanLinePhase -= MathHelper.TwoPi;

            if (glitchIntensity > 0) glitchIntensity -= 0.02f;
            glitchTimer += 0.016f;
            if (glitchTimer > nextGlitchTime) {
                glitchTimer = 0;
                nextGlitchTime = 2f + Main.rand.NextFloat(4f);
                glitchIntensity = MathHelper.Clamp(0.15f + Main.rand.NextFloat(0.2f), 0, 1);
            }
        }

        /// <summary>
        ///绘制全屏暗色遮罩
        /// </summary>
        public void DrawFullScreenDim(SpriteBatch sb, float alpha) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;
            sb.Draw(px, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                new Rectangle(0, 0, 1, 1), Color.Black * (alpha * 0.65f));
        }

        /// <summary>
        ///绘制面板背景、边框和四角装饰
        /// </summary>
        public void DrawBackground(SpriteBatch sb, float alpha, Rectangle panelRect, Vector2 panelCenter, float globalTimer) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            //面板主背景
            sb.Draw(px, panelRect, new Rectangle(0, 0, 1, 1), CyberwareTheme.BgPanel * (alpha * 0.95f));

            //内侧暗角渐变——营造凹陷深度感
            Color vignetteColor = CyberwareTheme.InnerShadow * (alpha * 0.8f);
            int vigSize = 8;
            for (int i = 0; i < vigSize; i++) {
                float fade = 1f - (float)i / vigSize;
                sb.Draw(px, new Rectangle(panelRect.X, panelRect.Y + i, panelRect.Width, 1),
                    new Rectangle(0, 0, 1, 1), vignetteColor * fade);
                sb.Draw(px, new Rectangle(panelRect.X, panelRect.Bottom - 1 - i, panelRect.Width, 1),
                    new Rectangle(0, 0, 1, 1), vignetteColor * fade);
                sb.Draw(px, new Rectangle(panelRect.X + i, panelRect.Y, 1, panelRect.Height),
                    new Rectangle(0, 0, 1, 1), vignetteColor * (fade * 0.6f));
                sb.Draw(px, new Rectangle(panelRect.Right - 1 - i, panelRect.Y, 1, panelRect.Height),
                    new Rectangle(0, 0, 1, 1), vignetteColor * (fade * 0.6f));
            }

            //面板外发光
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null) {
                Color panelGlow = CyberwareTheme.Accent * (alpha * 0.06f);
                panelGlow.A = 0;
                sb.Draw(glow, panelCenter, null, panelGlow, 0, glow.Size() / 2,
                    new Vector2(panelRect.Width / 50f, panelRect.Height / 50f), SpriteEffects.None, 0);
            }

            //顶部边框带脉冲
            float borderPulse = MathF.Sin(globalTimer * 2f) * 0.15f + 0.85f;
            Color topBorder = CyberwareTheme.Accent * (alpha * 0.8f * borderPulse);
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, 2),
                new Rectangle(0, 0, 1, 1), topBorder);
            //底边
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Bottom - 1, panelRect.Width, 1),
                new Rectangle(0, 0, 1, 1), CyberwareTheme.Border * (alpha * 0.6f));
            //左边
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Y, 1, panelRect.Height),
                new Rectangle(0, 0, 1, 1), CyberwareTheme.Border * (alpha * 0.5f));
            //右边
            sb.Draw(px, new Rectangle(panelRect.Right - 1, panelRect.Y, 1, panelRect.Height),
                new Rectangle(0, 0, 1, 1), CyberwareTheme.Border * (alpha * 0.5f));

            //四角装饰 —— 双层赛博朋克括号+斜切线
            Color cornerColor = CyberwareTheme.Accent * (alpha * 0.9f);
            Color cornerDim = cornerColor * 0.5f;
            Color cornerInner = CyberwareTheme.Accent * (alpha * 0.25f);
            int cL = 28, cL2 = 14, cInset = 6;
            //左上 外+内层
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Y, cL, 2), new Rectangle(0, 0, 1, 1), cornerColor);
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Y, 2, cL), new Rectangle(0, 0, 1, 1), cornerColor);
            CyberwareTheme.DrawLine(sb, px, new Vector2(panelRect.X + cL, panelRect.Y + 1),
                new Vector2(panelRect.X + cL + 5, panelRect.Y + 6), 1f, cornerColor * 0.4f);
            sb.Draw(px, new Rectangle(panelRect.X + cInset, panelRect.Y + cInset, cL2, 1), new Rectangle(0, 0, 1, 1), cornerInner);
            sb.Draw(px, new Rectangle(panelRect.X + cInset, panelRect.Y + cInset, 1, cL2), new Rectangle(0, 0, 1, 1), cornerInner);
            //右上
            sb.Draw(px, new Rectangle(panelRect.Right - cL, panelRect.Y, cL, 2), new Rectangle(0, 0, 1, 1), cornerColor);
            sb.Draw(px, new Rectangle(panelRect.Right - 2, panelRect.Y, 2, cL), new Rectangle(0, 0, 1, 1), cornerColor);
            CyberwareTheme.DrawLine(sb, px, new Vector2(panelRect.Right - cL, panelRect.Y + 1),
                new Vector2(panelRect.Right - cL - 5, panelRect.Y + 6), 1f, cornerColor * 0.4f);
            sb.Draw(px, new Rectangle(panelRect.Right - cInset - cL2, panelRect.Y + cInset, cL2, 1), new Rectangle(0, 0, 1, 1), cornerInner);
            sb.Draw(px, new Rectangle(panelRect.Right - cInset - 1, panelRect.Y + cInset, 1, cL2), new Rectangle(0, 0, 1, 1), cornerInner);
            //左下
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Bottom - 2, cL, 2), new Rectangle(0, 0, 1, 1), cornerDim);
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Bottom - cL, 2, cL), new Rectangle(0, 0, 1, 1), cornerDim);
            CyberwareTheme.DrawLine(sb, px, new Vector2(panelRect.X + cL, panelRect.Bottom - 1),
                new Vector2(panelRect.X + cL + 5, panelRect.Bottom - 6), 1f, cornerDim * 0.4f);
            sb.Draw(px, new Rectangle(panelRect.X + cInset, panelRect.Bottom - cInset - 1, cL2, 1), new Rectangle(0, 0, 1, 1), cornerInner * 0.7f);
            sb.Draw(px, new Rectangle(panelRect.X + cInset, panelRect.Bottom - cInset - cL2, 1, cL2), new Rectangle(0, 0, 1, 1), cornerInner * 0.7f);
            //右下
            sb.Draw(px, new Rectangle(panelRect.Right - cL, panelRect.Bottom - 2, cL, 2), new Rectangle(0, 0, 1, 1), cornerDim);
            sb.Draw(px, new Rectangle(panelRect.Right - 2, panelRect.Bottom - cL, 2, cL), new Rectangle(0, 0, 1, 1), cornerDim);
            CyberwareTheme.DrawLine(sb, px, new Vector2(panelRect.Right - cL, panelRect.Bottom - 1),
                new Vector2(panelRect.Right - cL - 5, panelRect.Bottom - 6), 1f, cornerDim * 0.4f);
            sb.Draw(px, new Rectangle(panelRect.Right - cInset - cL2, panelRect.Bottom - cInset - 1, cL2, 1), new Rectangle(0, 0, 1, 1), cornerInner * 0.7f);
            sb.Draw(px, new Rectangle(panelRect.Right - cInset - 1, panelRect.Bottom - cInset - cL2, 1, cL2), new Rectangle(0, 0, 1, 1), cornerInner * 0.7f);

            //边缘脉冲光——沿顶部边框移动的亮点
            float pulsePos = (globalTimer * 0.35f) % 1f;
            int pulseX = panelRect.X + (int)(pulsePos * panelRect.Width);
            sb.Draw(px, new Rectangle(pulseX - 20, panelRect.Y, 40, 2),
                new Rectangle(0, 0, 1, 1), CyberwareTheme.EdgeGlow * (alpha * 0.5f));
            if (glow != null) {
                Color pulseGlow = CyberwareTheme.EdgeGlow * (alpha * 0.25f);
                pulseGlow.A = 0;
                sb.Draw(glow, new Vector2(pulseX, panelRect.Y), null, pulseGlow, 0,
                    glow.Size() / 2, new Vector2(0.5f, 0.08f), SpriteEffects.None, 0);
            }
        }

        /// <summary>
        ///绘制面板内的网格背景线
        /// </summary>
        public void DrawGrid(SpriteBatch sb, float alpha, Rectangle panelRect) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            float spacing = 24f;

            //极暗全线——隐约的结构感
            Color faintLine = CyberwareTheme.GridLine * (alpha * 0.15f);
            for (float x = panelRect.X + spacing; x < panelRect.Right; x += spacing) {
                sb.Draw(px, new Rectangle((int)x, panelRect.Y, 1, panelRect.Height),
                    new Rectangle(0, 0, 1, 1), faintLine);
            }
            for (float y = panelRect.Y + spacing; y < panelRect.Bottom; y += spacing) {
                sb.Draw(px, new Rectangle(panelRect.X, (int)y, panelRect.Width, 1),
                    new Rectangle(0, 0, 1, 1), faintLine);
            }

            //交叉点高亮——电路板十字标记
            Color crossColor = CyberwareTheme.GridLine * (alpha * 0.6f);
            for (float x = panelRect.X + spacing; x < panelRect.Right; x += spacing) {
                for (float y = panelRect.Y + spacing; y < panelRect.Bottom; y += spacing) {
                    int ix = (int)x, iy = (int)y;
                    sb.Draw(px, new Rectangle(ix - 2, iy, 5, 1), new Rectangle(0, 0, 1, 1), crossColor);
                    sb.Draw(px, new Rectangle(ix, iy - 2, 1, 5), new Rectangle(0, 0, 1, 1), crossColor);
                }
            }
        }

        /// <summary>
        ///绘制扫描线和CRT纹理效果
        /// </summary>
        public void DrawScanLines(SpriteBatch sb, float alpha, Rectangle panelRect) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            //主扫描线
            float scanY = panelRect.Y + (MathF.Sin(scanLinePhase) * 0.5f + 0.5f) * panelRect.Height;
            Color scanColor = CyberwareTheme.Accent * (alpha * 0.08f);
            sb.Draw(px, new Rectangle(panelRect.X, (int)scanY, panelRect.Width, 2),
                new Rectangle(0, 0, 1, 1), scanColor);

            //扫描线上方渐变尾迹
            for (int i = 1; i <= 8; i++) {
                float fade = 1f - i / 8f;
                sb.Draw(px, new Rectangle(panelRect.X, (int)scanY - i * 3, panelRect.Width, 2),
                    new Rectangle(0, 0, 1, 1), scanColor * (fade * 0.4f));
            }

            //CRT扫描线纹理
            for (int y = panelRect.Y; y < panelRect.Bottom; y += 3) {
                sb.Draw(px, new Rectangle(panelRect.X, y, panelRect.Width, 1),
                    new Rectangle(0, 0, 1, 1), Color.Black * (alpha * 0.06f));
            }
        }

        /// <summary>
        ///绘制标题栏、版本号、底部状态栏和数据流装饰
        /// </summary>
        public void DrawTitleAndDecor(SpriteBatch sb, float alpha, Rectangle panelRect, Vector2 panelCenter,
            float globalTimer, string title, string statusText) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            //标题栏独立背景区——深色区域创造层次分离
            int headerH = 26;
            sb.Draw(px, new Rectangle(panelRect.X + 2, panelRect.Y + 2, panelRect.Width - 4, headerH),
                new Rectangle(0, 0, 1, 1), CyberwareTheme.SectionBg * (alpha * 0.9f));

            //标题栏底部分割线
            int divY = panelRect.Y + headerH + 3;
            Color divBright = CyberwareTheme.Accent * (alpha * 0.45f);
            sb.Draw(px, new Rectangle(panelRect.X + 10, divY, panelRect.Width - 20, 1),
                new Rectangle(0, 0, 1, 1), divBright);

            //分割线中央菱形缺口装饰
            int notchW = 8;
            sb.Draw(px, new Rectangle((int)panelCenter.X - notchW, divY - 2, notchW * 2, 5),
                new Rectangle(0, 0, 1, 1), CyberwareTheme.BgPanel * alpha);
            CyberwareTheme.DrawLine(sb, px, new Vector2(panelCenter.X - notchW, divY),
                new Vector2(panelCenter.X, divY - 2), 1f, divBright);
            CyberwareTheme.DrawLine(sb, px, new Vector2(panelCenter.X, divY - 2),
                new Vector2(panelCenter.X + notchW, divY), 1f, divBright);
            CyberwareTheme.DrawLine(sb, px, new Vector2(panelCenter.X - notchW, divY),
                new Vector2(panelCenter.X, divY + 2), 1f, divBright * 0.5f);
            CyberwareTheme.DrawLine(sb, px, new Vector2(panelCenter.X, divY + 2),
                new Vector2(panelCenter.X + notchW, divY), 1f, divBright * 0.5f);

            //标题文字
            Vector2 titleSize = FontAssets.MouseText.Value.MeasureString(title) * 0.65f;
            Vector2 titlePos = new(panelCenter.X - titleSize.X / 2f, panelRect.Y + 7);
            Color titleColor = CyberwareTheme.Accent * (alpha * 0.95f);
            Utils.DrawBorderString(sb, title, titlePos, titleColor, 0.65f);

            //标题两侧对称装饰线+尖括号
            float sideY = titlePos.Y + titleSize.Y * 0.45f;
            Color sideColor = CyberwareTheme.Accent * (alpha * 0.35f);
            float gapFromTitle = 10f;
            float sideLineLen = 35f;
            //左侧
            float lsx = titlePos.X - gapFromTitle - sideLineLen;
            sb.Draw(px, new Rectangle((int)lsx, (int)sideY, (int)sideLineLen, 1),
                new Rectangle(0, 0, 1, 1), sideColor);
            CyberwareTheme.DrawLine(sb, px, new Vector2(lsx - 6, sideY - 4),
                new Vector2(lsx, sideY), 1f, sideColor * 0.8f);
            CyberwareTheme.DrawLine(sb, px, new Vector2(lsx - 6, sideY + 4),
                new Vector2(lsx, sideY), 1f, sideColor * 0.8f);
            //右侧
            float rsx = titlePos.X + titleSize.X + gapFromTitle;
            sb.Draw(px, new Rectangle((int)rsx, (int)sideY, (int)sideLineLen, 1),
                new Rectangle(0, 0, 1, 1), sideColor);
            CyberwareTheme.DrawLine(sb, px, new Vector2(rsx + sideLineLen + 6, sideY - 4),
                new Vector2(rsx + sideLineLen, sideY), 1f, sideColor * 0.8f);
            CyberwareTheme.DrawLine(sb, px, new Vector2(rsx + sideLineLen + 6, sideY + 4),
                new Vector2(rsx + sideLineLen, sideY), 1f, sideColor * 0.8f);

            //版本号
            Color verColor = CyberwareTheme.TextDim * (alpha * 0.5f);
            Utils.DrawBorderString(sb, "v2.077", new Vector2(panelRect.Right - 70, panelRect.Y + 10), verColor, 0.36f);

            //底部状态栏独立背景区
            int footerH = 22;
            int footerTop = panelRect.Bottom - footerH;
            sb.Draw(px, new Rectangle(panelRect.X + 2, footerTop, panelRect.Width - 4, footerH - 2),
                new Rectangle(0, 0, 1, 1), CyberwareTheme.SectionBg * (alpha * 0.75f));

            //底部双线分割
            sb.Draw(px, new Rectangle(panelRect.X + 10, footerTop - 2, panelRect.Width - 20, 1),
                new Rectangle(0, 0, 1, 1), CyberwareTheme.Accent * (alpha * 0.25f));
            sb.Draw(px, new Rectangle(panelRect.X + 10, footerTop, panelRect.Width - 20, 1),
                new Rectangle(0, 0, 1, 1), CyberwareTheme.Border * (alpha * 0.15f));

            //运行状态指示灯和文字
            float bottomTextY = footerTop + 4;
            float statusPulse = MathF.Sin(globalTimer * 3f) > 0 ? 1f : 0.4f;
            Color statusDot = new Color(50, 255, 80) * (alpha * statusPulse);
            sb.Draw(px, new Vector2(panelRect.X + 10, bottomTextY + 2), new Rectangle(0, 0, 1, 1),
                statusDot, 0, Vector2.Zero, 4f, SpriteEffects.None, 0);
            Utils.DrawBorderString(sb, statusText, new Vector2(panelRect.X + 22, bottomTextY - 2),
                CyberwareTheme.TextDim * alpha, 0.34f);

            //右下角滚动数据标签
            string dataTag = $"NET::0x{((int)(globalTimer * 100) % 0xFFFF):X4}";
            Utils.DrawBorderString(sb, dataTag, new Vector2(panelRect.Right - 115, bottomTextY - 2),
                CyberwareTheme.AccentCyan * (alpha * 0.35f), 0.32f);
        }

        /// <summary>
        ///绘制随机故障干扰色块
        /// </summary>
        public void DrawGlitchEffect(SpriteBatch sb, float alpha, Rectangle panelRect) {
            if (glitchIntensity <= 0.01f) return;
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            float intensity = glitchIntensity * alpha;
            int glitchLines = (int)(3 + intensity * 8);
            for (int i = 0; i < glitchLines; i++) {
                int y = panelRect.Y + Main.rand.Next(panelRect.Height);
                int h = 1 + Main.rand.Next(3);
                int offsetX = Main.rand.Next(-8, 9);
                Color gc = Main.rand.NextBool() ? CyberwareTheme.Accent : CyberwareTheme.AccentCyan;
                gc *= intensity * 0.3f;
                sb.Draw(px, new Rectangle(panelRect.X + offsetX, y, panelRect.Width, h),
                    new Rectangle(0, 0, 1, 1), gc);
            }
        }

        #endregion
    }
}
