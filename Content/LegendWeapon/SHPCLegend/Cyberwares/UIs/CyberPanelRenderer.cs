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

            //四角装饰
            Color cornerColor = CyberwareTheme.Accent * (alpha * 0.9f);
            Color cornerDim = cornerColor * 0.6f;
            //左上
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Y, 20, 2), new Rectangle(0, 0, 1, 1), cornerColor);
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Y, 2, 16), new Rectangle(0, 0, 1, 1), cornerColor);
            //右上
            sb.Draw(px, new Rectangle(panelRect.Right - 20, panelRect.Y, 20, 2), new Rectangle(0, 0, 1, 1), cornerColor);
            sb.Draw(px, new Rectangle(panelRect.Right - 2, panelRect.Y, 2, 16), new Rectangle(0, 0, 1, 1), cornerColor);
            //左下
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Bottom - 2, 20, 2), new Rectangle(0, 0, 1, 1), cornerDim);
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Bottom - 16, 2, 16), new Rectangle(0, 0, 1, 1), cornerDim);
            //右下
            sb.Draw(px, new Rectangle(panelRect.Right - 20, panelRect.Bottom - 2, 20, 2), new Rectangle(0, 0, 1, 1), cornerDim);
            sb.Draw(px, new Rectangle(panelRect.Right - 2, panelRect.Bottom - 16, 2, 16), new Rectangle(0, 0, 1, 1), cornerDim);
        }

        /// <summary>
        ///绘制面板内的网格背景线
        /// </summary>
        public void DrawGrid(SpriteBatch sb, float alpha, Rectangle panelRect) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            Color gridColor = CyberwareTheme.GridLine * (alpha * 0.4f);
            float spacing = 24f;

            //垂直线
            for (float x = panelRect.X + spacing; x < panelRect.Right; x += spacing) {
                sb.Draw(px, new Rectangle((int)x, panelRect.Y, 1, panelRect.Height),
                    new Rectangle(0, 0, 1, 1), gridColor);
            }
            //水平线
            for (float y = panelRect.Y + spacing; y < panelRect.Bottom; y += spacing) {
                sb.Draw(px, new Rectangle(panelRect.X, (int)y, panelRect.Width, 1),
                    new Rectangle(0, 0, 1, 1), gridColor);
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

            //标题
            Vector2 titleSize = FontAssets.MouseText.Value.MeasureString(title) * 0.55f;
            Vector2 titlePos = new(panelCenter.X - titleSize.X / 2f, panelRect.Y + 8);

            //标题发光底色条
            Color titleGlowBg = CyberwareTheme.Accent * (alpha * 0.1f);
            sb.Draw(px, new Rectangle((int)(titlePos.X - 10), (int)titlePos.Y - 2,
                (int)(titleSize.X + 20), (int)(titleSize.Y + 6)),
                new Rectangle(0, 0, 1, 1), titleGlowBg);

            Color titleColor = CyberwareTheme.Accent * (alpha * 0.95f);
            Utils.DrawBorderString(sb, title, titlePos, titleColor, 0.55f);

            //标题下分割线
            float lineY = titlePos.Y + titleSize.Y + 6;
            float lineW = CyberwareTheme.PanelWidth * 0.85f;
            Color divColor = CyberwareTheme.Accent * (alpha * 0.3f);
            sb.Draw(px, new Rectangle((int)(panelCenter.X - lineW / 2f), (int)lineY, (int)lineW, 1),
                new Rectangle(0, 0, 1, 1), divColor);

            //版本号装饰
            Color verColor = CyberwareTheme.TextDim * (alpha * 0.5f);
            Utils.DrawBorderString(sb, "v2.077", new Vector2(panelRect.Right - 60, panelRect.Y + 10), verColor, 0.28f);

            //底部状态栏分割线
            float bottomY = panelRect.Bottom - 18;
            sb.Draw(px, new Rectangle(panelRect.X + 4, (int)bottomY - 2, panelRect.Width - 8, 1),
                new Rectangle(0, 0, 1, 1), CyberwareTheme.Border * (alpha * 0.4f));

            //运行状态指示灯和文字
            float statusPulse = MathF.Sin(globalTimer * 3f) > 0 ? 1f : 0.4f;
            Color statusDot = new Color(50, 255, 80) * (alpha * statusPulse);
            sb.Draw(px, new Vector2(panelRect.X + 10, bottomY + 2), new Rectangle(0, 0, 1, 1),
                statusDot, 0, Vector2.Zero, 4f, SpriteEffects.None, 0);
            Utils.DrawBorderString(sb, statusText, new Vector2(panelRect.X + 20, bottomY - 2),
                CyberwareTheme.TextDim * alpha, 0.26f);

            //右下角滚动数据标签
            string dataTag = $"NET::0x{((int)(globalTimer * 100) % 0xFFFF):X4}";
            Utils.DrawBorderString(sb, dataTag, new Vector2(panelRect.Right - 100, bottomY - 2),
                CyberwareTheme.AccentCyan * (alpha * 0.35f), 0.24f);
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
