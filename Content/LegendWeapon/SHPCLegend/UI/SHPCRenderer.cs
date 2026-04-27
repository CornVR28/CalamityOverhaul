using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.UI
{
    /// <summary>
    /// SHPC扇形HUD的纯CPU程序化绘制层
    /// 通过多次径向偏移与alpha渐变模拟SDF软边，避免像素感与拉伸感
    /// 所有绘制方法都基于一个1像素白色纹理和角度参数化的弧线步进
    /// </summary>
    internal static class SHPCRenderer
    {
        #region 基础几何工具

        public static Vector2 AngleDir(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));

        public static float EaseOutCubic(float t) {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        public static float EaseInOutQuad(float t) =>
            t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) * 0.5f;

        public static float EaseOutBack(float t) {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float inv = t - 1f;
            return 1f + c3 * inv * inv * inv + c1 * inv * inv;
        }

        #endregion

        #region 线段与圆弧

        /// <summary>
        /// 基础直线段，以start为锚点旋转拉伸1像素纹理
        /// </summary>
        public static void DrawLine(SpriteBatch sb, Texture2D px, Vector2 start, Vector2 end, float thickness, Color color) {
            Vector2 diff = end - start;
            float length = diff.Length();
            if (length < 0.5f) {
                return;
            }
            sb.Draw(px, start, new Rectangle(0, 0, 1, 1), color, diff.ToRotation(),
                Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        /// <summary>
        /// 用密集径向线段绘制填充弧形，自适应分段保证无缝拼接
        /// </summary>
        public static void DrawArc(SpriteBatch sb, Texture2D px, Vector2 center,
            float rIn, float rOut, float aStart, float aEnd, Color color) {
            if (aEnd <= aStart) {
                return;
            }
            float midR = (rIn + rOut) * 0.5f;
            float arcLen = (aEnd - aStart) * midR;
            int steps = Math.Max((int)(arcLen / 2.5f), 3);
            float aStep = (aEnd - aStart) / steps;
            float lineThick = MathF.Max(aStep * midR + 0.8f, 1.5f);
            for (int i = 0; i <= steps; i++) {
                float a = aStart + i * aStep;
                Vector2 dir = AngleDir(a);
                DrawLine(sb, px, center + dir * rIn, center + dir * rOut, lineThick, color);
            }
        }

        /// <summary>
        /// 软边圆弧描边，三层不同厚度叠加模拟SDF抗锯齿
        /// </summary>
        public static void DrawArcStroke(SpriteBatch sb, Texture2D px, Vector2 center,
            float radius, float aStart, float aEnd, float thickness, Color color) {
            if (aEnd <= aStart) {
                return;
            }
            //外层柔光
            DrawArc(sb, px, center,
                radius - thickness * 0.5f - 1.2f,
                radius + thickness * 0.5f + 1.2f,
                aStart, aEnd, color * 0.18f);
            //中层主体
            DrawArc(sb, px, center,
                radius - thickness * 0.5f - 0.4f,
                radius + thickness * 0.5f + 0.4f,
                aStart, aEnd, color * 0.55f);
            //内层锐线
            DrawArc(sb, px, center,
                radius - thickness * 0.5f,
                radius + thickness * 0.5f,
                aStart, aEnd, color);
        }

        /// <summary>
        /// 程序化软边圆盘，从中心向外多层alpha渐变
        /// 给定radius为不透明核心半径，softPad为半透明过渡宽度
        /// </summary>
        public static void DrawDisc(SpriteBatch sb, Texture2D px, Vector2 center,
            float radius, float softPad, Color color) {
            if (radius <= 0f) {
                return;
            }
            //外层
            DrawArc(sb, px, center, radius, radius + softPad, 0f, MathHelper.TwoPi, color * 0.25f);
            //过渡
            DrawArc(sb, px, center, radius - 0.6f, radius + softPad * 0.5f, 0f, MathHelper.TwoPi, color * 0.55f);
            //核心填充
            DrawArc(sb, px, center, 0f, radius, 0f, MathHelper.TwoPi, color);
        }

        /// <summary>
        /// 圆环描边，三层叠加保证软边
        /// </summary>
        public static void DrawRing(SpriteBatch sb, Texture2D px, Vector2 center,
            float radius, float thickness, Color color) {
            DrawArcStroke(sb, px, center, radius, 0f, MathHelper.TwoPi, thickness, color);
        }

        #endregion

        #region 核心绘制

        /// <summary>
        /// 绘制左下能量核心
        /// 状态包括：常驻轻微呼吸、悬停高亮、展开时外环旋转刻度、点击瞬时闪烁
        /// </summary>
        public static void DrawCore(SpriteBatch sb, Texture2D px, Vector2 center,
            float expandProgress, float coreHover, float corePulse, float clickFlash, float time, float globalAlpha) {
            //外层投影，给整体一种漂浮感
            DrawDisc(sb, px, center + new Vector2(0f, 2.5f),
                SHPCTheme.CoreRingR + 4f, 6f, SHPCTheme.ShadowDark * (0.45f * globalAlpha));

            //背景圆盘，深色底
            DrawDisc(sb, px, center,
                SHPCTheme.CoreRingR - 1f, 3f, SHPCTheme.SlotBg * (0.92f * globalAlpha));

            //外环，展开时随展开进度逐渐加亮
            float ringGlow = 0.55f + expandProgress * 0.35f + coreHover * 0.25f + clickFlash * 0.5f;
            ringGlow = MathHelper.Clamp(ringGlow, 0f, 1.4f);
            Color ringCol = Color.Lerp(SHPCTheme.Cyan, SHPCTheme.CyanHi, ringGlow * 0.6f);
            DrawArcStroke(sb, px, center, SHPCTheme.CoreRingR, 0f, MathHelper.TwoPi,
                1.6f, ringCol * (ringGlow * globalAlpha));

            //旋转刻度，4个短弧表达"扇区指引"
            int markCount = 4;
            float markSpan = 0.42f;
            float markGap = MathHelper.TwoPi / markCount;
            float markRot = time * 0.35f;
            for (int i = 0; i < markCount; i++) {
                float a0 = markRot + i * markGap - markSpan * 0.5f;
                DrawArcStroke(sb, px, center, SHPCTheme.CoreRingR + 4f, a0, a0 + markSpan,
                    1.1f, SHPCTheme.Border * (0.6f * globalAlpha));
            }

            //内核呼吸光斑
            float breath = 0.78f + MathF.Sin(time * 2.4f) * 0.15f + corePulse * 0.4f;
            DrawDisc(sb, px, center,
                SHPCTheme.CoreRadius * breath * 0.5f, 4f,
                SHPCTheme.CyanHi * (0.85f * globalAlpha));

            //点击瞬闪：从核心向外扩散一道快速衰减的环
            if (clickFlash > 0.01f) {
                float flashR = SHPCTheme.CoreRingR + (1f - clickFlash) * 30f;
                DrawArcStroke(sb, px, center, flashR, 0f, MathHelper.TwoPi,
                    2.2f, SHPCTheme.CyanHi * (clickFlash * 0.85f * globalAlpha));
            }

            //中央十字微元素，纯粹增加构图细节
            DrawLine(sb, px, center - new Vector2(3f, 0f), center + new Vector2(3f, 0f),
                1.1f, SHPCTheme.SlotBg * globalAlpha);
            DrawLine(sb, px, center - new Vector2(0f, 3f), center + new Vector2(0f, 3f),
                1.1f, SHPCTheme.SlotBg * globalAlpha);
        }

        #endregion

        #region 扇形按钮

        /// <summary>
        /// 绘制单个扇形按钮，包含底板、状态填充、描边、悬停高亮、图标
        /// expandProgress用于按钮整体的入场动画与不透明度
        /// </summary>
        public static void DrawSector(SpriteBatch sb, Texture2D px, Vector2 center,
            float aStart, float aEnd, float expandProgress,
            float hoverAmt, float selectAmt, bool enabled, float statusValue,
            string glyph, float time, float globalAlpha) {
            if (expandProgress < 0.01f) {
                return;
            }

            //入场偏移：未完全展开时按钮从核心方向滑出，半径线性插值
            float ease = EaseOutBack(MathHelper.Clamp(expandProgress, 0f, 1f));
            float rIn = MathHelper.Lerp(SHPCTheme.CoreRingR + 4f, SHPCTheme.ButtonInnerR, ease);
            float rOut = MathHelper.Lerp(SHPCTheme.CoreRingR + 8f, SHPCTheme.ButtonOuterR, ease);
            float a = globalAlpha * MathHelper.Clamp(expandProgress * 1.3f, 0f, 1f);

            //投影
            DrawArc(sb, px, center + new Vector2(1.5f, 2.5f),
                rIn, rOut, aStart, aEnd, SHPCTheme.ShadowDark * (0.55f * a));

            //底板，禁用时整体偏暗
            Color bgCol = enabled ? SHPCTheme.SlotBg : SHPCTheme.SlotBg * 0.55f;
            DrawArc(sb, px, center, rIn + 1f, rOut - 1f, aStart, aEnd, bgCol * (0.92f * a));

            //内侧暗化条，模拟厚度
            DrawArc(sb, px, center, rIn + 1f, rIn + 4f, aStart, aEnd,
                SHPCTheme.ShadowDark * (0.55f * a));

            //状态弧填充，按状态值从内到外按比例覆盖
            if (enabled && statusValue > 0.01f) {
                float fillR = MathHelper.Lerp(rIn + 1f, rOut - 1f, MathHelper.Clamp(statusValue, 0f, 1f));
                Color fillCol = Color.Lerp(SHPCTheme.Cyan, SHPCTheme.CyanHi, hoverAmt * 0.6f + selectAmt * 0.5f);
                //主填充
                DrawArc(sb, px, center, rIn + 1f, fillR, aStart, aEnd, fillCol * (0.55f * a));
                //外缘高光
                DrawArc(sb, px, center, fillR - 1.5f, fillR + 1f, aStart, aEnd, SHPCTheme.CyanHi * (0.85f * a));
            }

            //悬停柔光底纹，基于扇区径向覆盖
            if (hoverAmt > 0.01f) {
                Color hoverCol = enabled ? SHPCTheme.CyanHi : SHPCTheme.Disabled;
                DrawArc(sb, px, center, rIn + 1f, rOut - 1f, aStart, aEnd,
                    hoverCol * (hoverAmt * 0.18f * a));
            }

            //选中暖色高光带
            if (selectAmt > 0.01f) {
                DrawArc(sb, px, center, rOut - 5f, rOut - 1f, aStart, aEnd,
                    SHPCTheme.Accent * (selectAmt * 0.85f * a));
            }

            //外缘描边，悬停或选中时更亮
            float borderHi = MathF.Max(hoverAmt, selectAmt);
            Color borderCol = enabled
                ? Color.Lerp(SHPCTheme.Border, SHPCTheme.BorderHi, borderHi)
                : SHPCTheme.Disabled;
            DrawArcStroke(sb, px, center, rOut - 0.5f, aStart, aEnd, 1.4f, borderCol * a);
            DrawArcStroke(sb, px, center, rIn + 0.5f, aStart, aEnd, 1.1f,
                borderCol * (0.65f * a));

            //径向封口
            float capThick = 1.4f;
            Vector2 dirS = AngleDir(aStart);
            Vector2 dirE = AngleDir(aEnd);
            DrawLine(sb, px, center + dirS * (rIn + 1f), center + dirS * (rOut - 1f),
                capThick, borderCol * (0.7f * a));
            DrawLine(sb, px, center + dirE * (rIn + 1f), center + dirE * (rOut - 1f),
                capThick, borderCol * (0.7f * a));

            //图标，绘制于按钮中心半径
            if (!string.IsNullOrEmpty(glyph)) {
                float midA = (aStart + aEnd) * 0.5f;
                Vector2 iconPos = center + AngleDir(midA) * SHPCTheme.ButtonMidR;
                DynamicSpriteFont font = FontAssets.MouseText.Value;
                Vector2 size = font.MeasureString(glyph) * 0.85f;
                Color iconCol = enabled
                    ? Color.Lerp(SHPCTheme.Text, SHPCTheme.CyanHi, borderHi)
                    : SHPCTheme.Disabled;
                Utils.DrawBorderString(sb, glyph, iconPos - size * 0.5f, iconCol * a, 0.85f);
            }

            //扫光带：在按钮内沿弧线方向缓慢扫过
            if (enabled && (hoverAmt > 0.01f || selectAmt > 0.01f)) {
                float scanT = (time * 0.6f) % 1f;
                float scanA = MathHelper.Lerp(aStart, aEnd, scanT);
                float scanWidth = (aEnd - aStart) * 0.18f;
                float sa0 = MathF.Max(aStart, scanA - scanWidth * 0.5f);
                float sa1 = MathF.Min(aEnd, scanA + scanWidth * 0.5f);
                DrawArc(sb, px, center, rIn + 2f, rOut - 2f, sa0, sa1,
                    SHPCTheme.CyanHi * (0.18f * MathF.Max(hoverAmt, selectAmt) * a));
            }
        }

        /// <summary>
        /// 核心到按钮内弧的连接装饰线，仅在展开时绘制，按扇区中线辐射
        /// </summary>
        public static void DrawConnector(SpriteBatch sb, Texture2D px, Vector2 center,
            float midAngle, float expandProgress, float hoverAmt, float globalAlpha) {
            if (expandProgress < 0.05f) {
                return;
            }
            float ease = EaseOutCubic(MathHelper.Clamp(expandProgress, 0f, 1f));
            Vector2 dir = AngleDir(midAngle);
            Vector2 p0 = center + dir * (SHPCTheme.CoreRingR + 4f);
            Vector2 p1 = center + dir * MathHelper.Lerp(SHPCTheme.CoreRingR + 8f,
                SHPCTheme.ButtonInnerR - 1f, ease);
            Color col = Color.Lerp(SHPCTheme.Border, SHPCTheme.BorderHi, hoverAmt) *
                (ease * globalAlpha);
            DrawLine(sb, px, p0, p1, 1.2f, col);
            //端点小圆点
            DrawDisc(sb, px, p1, 1.6f, 1.4f, col * 1.2f);
        }

        #endregion

        #region 二级信息面板

        /// <summary>
        /// 绘制二级信息面板，作为tooltip依附于光标位置
        /// <br/>cursor为当前鼠标位置，面板会自动避免越过屏幕边缘
        /// </summary>
        public static void DrawInfoPanel(SpriteBatch sb, Texture2D px,
            Vector2 cursor, float panelAlpha, float globalAlpha,
            string title, string subtitle, string description, string statusText) {
            if (panelAlpha < 0.02f) {
                return;
            }
            float a = panelAlpha * globalAlpha;

            const float panelW = 168f;
            const float panelH = 70f;

            //入场偏移，沿光标右下方向滑入
            float slide = (1f - panelAlpha) * 8f;
            //tooltip式定位：默认在光标右下偏移
            Vector2 panelPos = cursor + new Vector2(18f + slide, 14f);
            //屏幕边缘自适应：右越界翻转到光标左侧，下越界翻转到光标上方
            if (panelPos.X + panelW > Main.screenWidth - 8f) {
                panelPos.X = cursor.X - panelW - 18f - slide;
            }
            if (panelPos.Y + panelH > Main.screenHeight - 8f) {
                panelPos.Y = cursor.Y - panelH - 14f;
            }
            //最终再做一次硬限位防止极端情况下越界
            panelPos.X = MathHelper.Clamp(panelPos.X, 4f, Main.screenWidth - panelW - 4f);
            panelPos.Y = MathHelper.Clamp(panelPos.Y, 4f, Main.screenHeight - panelH - 4f);
            Rectangle rect = new((int)panelPos.X, (int)panelPos.Y, (int)panelW, (int)panelH);

            //投影
            DrawFilledRect(sb, px, new Rectangle(rect.X + 2, rect.Y + 3, rect.Width, rect.Height),
                SHPCTheme.ShadowDark * (0.55f * a));

            //背景
            DrawFilledRect(sb, px, rect, SHPCTheme.SlotBg * (0.92f * a));

            //顶部色带，强化"模块化"科技感
            DrawFilledRect(sb, px,
                new Rectangle(rect.X, rect.Y, rect.Width, 3), SHPCTheme.Cyan * (0.85f * a));

            //描边，四边
            DrawRectStroke(sb, px, rect, 1.2f, SHPCTheme.Border * (0.85f * a));

            //左下连接小角标，与扇区方向呼应
            DrawLine(sb, px,
                new Vector2(rect.X, rect.Y + 6), new Vector2(rect.X, rect.Y + rect.Height - 6),
                1.6f, SHPCTheme.Cyan * (0.55f * a));
            //四角L形装饰
            DrawCornerBrackets(sb, px, rect, 6f, 1.4f, SHPCTheme.BorderHi * (0.85f * a));

            //文字
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float pad = 6f;
            //标题
            if (!string.IsNullOrEmpty(title)) {
                Utils.DrawBorderString(sb, title,
                    new Vector2(rect.X + pad, rect.Y + 6f), SHPCTheme.Text * a, 0.6f);
            }
            //副标题
            if (!string.IsNullOrEmpty(subtitle)) {
                Utils.DrawBorderString(sb, subtitle,
                    new Vector2(rect.X + pad, rect.Y + 22f), SHPCTheme.TextDim * a, 0.4f);
            }
            //状态值，绘制于右上
            if (!string.IsNullOrEmpty(statusText)) {
                Vector2 size = font.MeasureString(statusText) * 0.5f;
                Utils.DrawBorderString(sb, statusText,
                    new Vector2(rect.Right - pad - size.X, rect.Y + 6f),
                    SHPCTheme.CyanHi * a, 0.5f);
            }
            //说明，靠下两行内
            if (!string.IsNullOrEmpty(description)) {
                Utils.DrawBorderString(sb, description,
                    new Vector2(rect.X + pad, rect.Y + 40f), SHPCTheme.TextDim * a, 0.42f);
            }
        }

        #endregion

        #region 矩形辅助

        public static void DrawFilledRect(SpriteBatch sb, Texture2D px, Rectangle rect, Color color) {
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), color);
        }

        public static void DrawRectStroke(SpriteBatch sb, Texture2D px, Rectangle rect, float thickness, Color color) {
            //四条边分别用线段绘制，避免重复像素叠色
            Vector2 tl = new(rect.X, rect.Y);
            Vector2 tr = new(rect.Right, rect.Y);
            Vector2 bl = new(rect.X, rect.Bottom);
            Vector2 br = new(rect.Right, rect.Bottom);
            DrawLine(sb, px, tl, tr, thickness, color);
            DrawLine(sb, px, bl, br, thickness, color);
            DrawLine(sb, px, tl, bl, thickness, color);
            DrawLine(sb, px, tr, br, thickness, color);
        }

        public static void DrawCornerBrackets(SpriteBatch sb, Texture2D px, Rectangle rect, float size, float thickness, Color color) {
            //左上
            DrawLine(sb, px, new Vector2(rect.X, rect.Y), new Vector2(rect.X + size, rect.Y), thickness, color);
            DrawLine(sb, px, new Vector2(rect.X, rect.Y), new Vector2(rect.X, rect.Y + size), thickness, color);
            //右上
            DrawLine(sb, px, new Vector2(rect.Right - size, rect.Y), new Vector2(rect.Right, rect.Y), thickness, color);
            DrawLine(sb, px, new Vector2(rect.Right, rect.Y), new Vector2(rect.Right, rect.Y + size), thickness, color);
            //左下
            DrawLine(sb, px, new Vector2(rect.X, rect.Bottom - size), new Vector2(rect.X, rect.Bottom), thickness, color);
            DrawLine(sb, px, new Vector2(rect.X, rect.Bottom), new Vector2(rect.X + size, rect.Bottom), thickness, color);
            //右下
            DrawLine(sb, px, new Vector2(rect.Right - size, rect.Bottom), new Vector2(rect.Right, rect.Bottom), thickness, color);
            DrawLine(sb, px, new Vector2(rect.Right, rect.Bottom - size), new Vector2(rect.Right, rect.Bottom), thickness, color);
        }

        #endregion
    }
}
