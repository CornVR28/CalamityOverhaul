using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.Cyberwares.Implementation.Sandevistans
{
    /// <summary>
    /// 斯安威斯坦冷却值HUD，通用设计，支持不同型号的斯安威斯坦
    /// 当玩家装备了任意斯安威斯坦义体时显示冷却条
    /// </summary>
    internal class SandevistanHUD : UIHandle
    {
        public override bool Active => Sandevistan.GetEquipped(Main.LocalPlayer) != null;

        //显示用的平滑冷却比例
        private float displayRatio = 1f;
        //激活时的脉冲动画计时器
        private float glowPulse;
        //状态文本闪烁计时器
        private float statusFlicker;
        //装饰扫描线偏移
        private float scanlineOffset;

        //HUD布局
        private const int BarWidth = 180;
        private const int BarHeight = 6;
        private const int BarPadding = 4;
        private const int TotalWidth = BarWidth + BarPadding * 2 + 2;
        private const int TextHeight = 16;
        private const int StatusHeight = 14;
        private const int TotalHeight = TextHeight + BarHeight + BarPadding * 2 + StatusHeight + 4;

        //赛博颜色
        private static readonly Color CyanAccent = new(0, 220, 220);
        private static readonly Color RedAccent = new(255, 42, 42);
        private static readonly Color DimText = new(70, 70, 80);
        private static readonly Color BgDark = new(8, 8, 14, 220);
        private static readonly Color BorderDim = new(35, 35, 45, 200);
        private static readonly Color BarBg = new(16, 16, 24);
        private static readonly Color ActiveGlow = new(0, 255, 255, 30);

        public override void Update() {
            float target = Sandevistan.CooldownRatio;
            displayRatio += (target - displayRatio) * 0.12f;

            if (Sandevistan.IsActive) {
                glowPulse += 0.1f;
                statusFlicker += 0.15f;
            }
            else {
                glowPulse *= 0.92f;
                statusFlicker *= 0.9f;
            }

            scanlineOffset += 0.5f;
            if (scanlineOffset > TotalHeight) {
                scanlineOffset -= TotalHeight;
            }
        }

        public override void Draw(SpriteBatch sb) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float ratio = Math.Clamp(displayRatio, 0f, 1f);
            bool active = Sandevistan.IsActive;

            //定位：屏幕底部中央偏上
            Vector2 basePos = new(
                Main.screenWidth / 2f - TotalWidth / 2f,
                Main.screenHeight - 105
            );

            //外框背景
            Rectangle outerRect = new((int)basePos.X - 1, (int)basePos.Y - 1, TotalWidth + 2, TotalHeight + 2);
            sb.Draw(px, outerRect, BgDark);

            //外框边线
            Color borderColor = active ? Color.Lerp(RedAccent, CyanAccent, 0.5f + (float)Math.Sin(glowPulse) * 0.3f) * 0.8f : BorderDim;
            DrawHollowRect(sb, px, outerRect, borderColor);

            //激活时外发光
            if (active) {
                Rectangle glowRect = new(outerRect.X - 1, outerRect.Y - 1, outerRect.Width + 2, outerRect.Height + 2);
                DrawHollowRect(sb, px, glowRect, ActiveGlow);
            }

            //角标装饰（四角小角标）
            int cornerLen = 5;
            DrawCornerMarks(sb, px, outerRect, cornerLen, active ? CyanAccent * 0.6f : DimText * 0.5f);

            //标题 "SANDEVISTAN"
            float textScale = 0.55f;
            Vector2 titlePos = basePos + new Vector2(BarPadding + 2, 2);
            Color titleColor = active ? CyanAccent : new Color(120, 120, 135);
            Utils.DrawBorderString(sb, "SANDEVISTAN", titlePos, titleColor, textScale);

            //百分比
            int pct = (int)(ratio * 100);
            string pctText = $"{pct}%";
            Vector2 pctSize = font.MeasureString(pctText) * textScale;
            Vector2 pctPos = basePos + new Vector2(TotalWidth - BarPadding - 2 - pctSize.X, 2);
            Color pctColor = active ? Color.Lerp(RedAccent, CyanAccent, ratio) : Color.Lerp(new Color(180, 50, 50), new Color(100, 180, 180), ratio);
            Utils.DrawBorderString(sb, pctText, pctPos, pctColor, textScale);

            //冷却条背景
            Vector2 barPos = basePos + new Vector2(BarPadding + 1, TextHeight + 1);
            Rectangle barBgRect = new((int)barPos.X, (int)barPos.Y, BarWidth, BarHeight);
            sb.Draw(px, barBgRect, BarBg);

            //冷却条填充
            int fillWidth = (int)(BarWidth * ratio);
            if (fillWidth > 0) {
                //颜色根据比例从红过渡到青
                Color barColor = Color.Lerp(RedAccent, CyanAccent, ratio);

                if (active) {
                    //激活时的脉冲闪烁
                    float pulse = 0.75f + (float)Math.Sin(glowPulse * 2f) * 0.25f;
                    barColor *= pulse;
                }

                Rectangle fillRect = new((int)barPos.X, (int)barPos.Y, fillWidth, BarHeight);
                sb.Draw(px, fillRect, barColor);

                //条末端高光
                if (fillWidth > 2) {
                    Rectangle edgeRect = new((int)barPos.X + fillWidth - 2, (int)barPos.Y, 2, BarHeight);
                    sb.Draw(px, edgeRect, Color.White * 0.3f);
                }
            }

            //条框线
            DrawHollowRect(sb, px, barBgRect, borderColor * 0.5f);

            //状态文本
            float statusScale = 0.45f;
            Vector2 statusPos = basePos + new Vector2(BarPadding + 2, TextHeight + BarHeight + BarPadding + 2);
            string statusText;
            Color statusColor;

            if (active) {
                statusText = ">> ACTIVE";
                float flicker = (float)Math.Sin(statusFlicker * 3f) > 0 ? 1f : 0.5f;
                statusColor = CyanAccent * flicker;
            }
            else if (ratio >= 0.99f) {
                statusText = "READY";
                statusColor = new Color(0, 200, 0);
            }
            else {
                statusText = "RECHARGING...";
                statusColor = new Color(200, 160, 40);
            }

            Utils.DrawBorderString(sb, statusText, statusPos, statusColor, statusScale);

            //扫描线装饰
            if (active) {
                int scanY = (int)(basePos.Y + scanlineOffset) % (TotalHeight + 2);
                Rectangle scanRect = new(outerRect.X, (int)basePos.Y + scanY, outerRect.Width, 1);
                sb.Draw(px, scanRect, CyanAccent * 0.1f);
            }
        }

        private static void DrawHollowRect(SpriteBatch sb, Texture2D px, Rectangle rect, Color color) {
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
        }

        private static void DrawCornerMarks(SpriteBatch sb, Texture2D px, Rectangle rect, int len, Color color) {
            //左上
            sb.Draw(px, new Rectangle(rect.X, rect.Y, len, 1), color);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 1, len), color);
            //右上
            sb.Draw(px, new Rectangle(rect.Right - len, rect.Y, len, 1), color);
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, len), color);
            //左下
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, len, 1), color);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - len, 1, len), color);
            //右下
            sb.Draw(px, new Rectangle(rect.Right - len, rect.Bottom - 1, len, 1), color);
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Bottom - len, 1, len), color);
        }
    }
}
