using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberwares.UIs
{
    /// <summary>
    ///赛博义体界面的槽位渲染器
    ///负责义体槽位的排布、交互检测、连接线绘制
    /// </summary>
    internal class CyberSlotRenderer
    {
        #region 槽位定义

        /// <summary>
        ///义体槽位的布局定义
        /// </summary>
        internal readonly struct SlotDef(float xRatio, float yRatio, int nodeIndex)
        {
            ///槽位相对面板的水平位置比例
            public readonly float XRatio = xRatio;
            ///槽位相对面板的垂直位置比例
            public readonly float YRatio = yRatio;
            ///连接到人体节点的索引
            public readonly int NodeIndex = nodeIndex;
            ///是否位于面板左侧
            public bool IsLeft => XRatio < 0.5f;
        }

        /// <summary>
        ///所有义体槽位的布局数据，左右对称各6个
        /// </summary>
        public static readonly SlotDef[] Definitions = [
            //左侧槽位
            new(0.04f, 0.08f,  0),   //额叶皮层
            new(0.04f, 0.18f,  0),   //光学系统
            new(0.04f, 0.36f,  2),   //左臂
            new(0.04f, 0.50f,  4),   //手部
            new(0.04f, 0.68f,  7),   //左腿
            new(0.04f, 0.82f,  9),   //足部
            //右侧槽位
            new(0.76f, 0.08f,  0),   //操作系统
            new(0.76f, 0.18f,  1),   //循环系统
            new(0.76f, 0.36f,  3),   //右臂
            new(0.76f, 0.50f,  6),   //骨骼
            new(0.76f, 0.68f,  8),   //右腿
            new(0.76f, 0.82f,  10),  //神经系统
        ];

        #endregion

        #region 状态

        private int hoveredSlot = -1;
        private int selectedSlot = -1;
        private readonly float[] slotHoverAnim = new float[12];
        //预分配的节点状态缓存，避免每帧分配
        private readonly int[] nodeStatesCache = new int[CyberBodyRenderer.NodeCount];

        public int HoveredSlot => hoveredSlot;
        public int SelectedSlot { get => selectedSlot; set => selectedSlot = value; }

        #endregion

        #region 公共方法

        /// <summary>
        ///检测鼠标悬停和点击交互，返回本帧是否发生了点击
        /// </summary>
        public bool UpdateInteraction(Rectangle panelRect) {
            bool clicked = false;
            hoveredSlot = -1;
            Vector2 mouse = new(Main.mouseX, Main.mouseY);

            for (int i = 0; i < Definitions.Length; i++) {
                Rectangle slotRect = GetSlotRect(i, panelRect);
                if (slotRect.Contains((int)mouse.X, (int)mouse.Y)) {
                    hoveredSlot = i;
                    Main.LocalPlayer.mouseInterface = true;
                    break;
                }
            }

            if (hoveredSlot >= 0 && Main.mouseLeft && Main.mouseLeftRelease) {
                selectedSlot = hoveredSlot == selectedSlot ? -1 : hoveredSlot;
                clicked = true;
            }

            return clicked;
        }

        /// <summary>
        ///更新槽位悬停动画的平滑过渡
        /// </summary>
        public void UpdateAnimations() {
            for (int i = 0; i < slotHoverAnim.Length; i++) {
                float target = i == hoveredSlot ? 1f : (i == selectedSlot ? 0.6f : 0f);
                slotHoverAnim[i] += (target - slotHoverAnim[i]) * 0.15f;
            }
        }

        /// <summary>
        ///计算各节点的激活状态数组，0普通 1已连接 2高亮
        ///返回的数组为内部缓存引用，下次调用会覆盖内容
        /// </summary>
        public int[] ComputeNodeStates() {
            Array.Clear(nodeStatesCache);
            for (int i = 0; i < Definitions.Length; i++) {
                int ni = Definitions[i].NodeIndex;
                if (i == hoveredSlot || i == selectedSlot) {
                    nodeStatesCache[ni] = 2;
                }
                else if (nodeStatesCache[ni] < 1) {
                    nodeStatesCache[ni] = 1;
                }
            }
            return nodeStatesCache;
        }

        /// <summary>
        ///绘制所有义体槽位的背景、边框和文字标签
        /// </summary>
        public void DrawSlots(SpriteBatch sb, float alpha, Rectangle panelRect,
            string[] slotLabels, string selectedText, string emptyText) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            Texture2D slotGlow = CWRAsset.SoftGlow?.Value;

            for (int i = 0; i < Definitions.Length; i++) {
                Rectangle rect = GetSlotRect(i, panelRect);
                var def = Definitions[i];
                float hover = slotHoverAnim[i];
                bool isSelected = i == selectedSlot;
                bool isHovered = i == hoveredSlot;

                //槽位外层背景
                Color outerBg = Color.Lerp(CyberwareTheme.SlotEmpty, CyberwareTheme.BgDark, 0.3f) * (alpha * 0.9f);
                if (isHovered) outerBg = Color.Lerp(outerBg, CyberwareTheme.Accent, 0.06f * hover);
                if (isSelected) outerBg = Color.Lerp(outerBg, CyberwareTheme.Accent, 0.08f);
                sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), outerBg);

                //内凹区域——比外层更暗，制造深度
                Rectangle innerArea = new(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
                Color innerBg = CyberwareTheme.SlotInnerBg * (alpha * 0.85f);
                if (isSelected) innerBg = Color.Lerp(innerBg, CyberwareTheme.Accent, 0.05f);
                sb.Draw(px, innerArea, new Rectangle(0, 0, 1, 1), innerBg);

                //内侧顶部+左侧阴影条——模拟凹陷
                sb.Draw(px, new Rectangle(innerArea.X, innerArea.Y, innerArea.Width, 2),
                    new Rectangle(0, 0, 1, 1), CyberwareTheme.InnerShadow * (alpha * 0.6f));
                sb.Draw(px, new Rectangle(innerArea.X, innerArea.Y, 2, innerArea.Height),
                    new Rectangle(0, 0, 1, 1), CyberwareTheme.InnerShadow * (alpha * 0.35f));

                //外边框
                Color borderColor = isSelected ? CyberwareTheme.Accent :
                    isHovered ? Color.Lerp(CyberwareTheme.SlotBorder, CyberwareTheme.Accent, hover * 0.6f) :
                    CyberwareTheme.SlotBorder;
                borderColor *= alpha;
                sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1), new Rectangle(0, 0, 1, 1), borderColor);
                sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), new Rectangle(0, 0, 1, 1), borderColor * 0.6f);
                sb.Draw(px, new Rectangle(rect.X, rect.Y, 1, rect.Height), new Rectangle(0, 0, 1, 1), borderColor * 0.7f);
                sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), new Rectangle(0, 0, 1, 1), borderColor * 0.7f);

                //斜切角装饰
                int cutSz = 4;
                sb.Draw(px, new Rectangle(rect.X, rect.Y, cutSz, cutSz),
                    new Rectangle(0, 0, 1, 1), CyberwareTheme.BgPanel * alpha);
                CyberwareTheme.DrawLine(sb, px, new Vector2(rect.X, rect.Y + cutSz),
                    new Vector2(rect.X + cutSz, rect.Y), 1f, borderColor);
                sb.Draw(px, new Rectangle(rect.Right - cutSz, rect.Bottom - cutSz, cutSz, cutSz),
                    new Rectangle(0, 0, 1, 1), CyberwareTheme.BgPanel * alpha);
                CyberwareTheme.DrawLine(sb, px, new Vector2(rect.Right - cutSz, rect.Bottom),
                    new Vector2(rect.Right, rect.Bottom - cutSz), 1f, borderColor * 0.6f);

                //选中/悬停外发光
                if ((isSelected || hover > 0.3f) && slotGlow != null) {
                    float glowStr = isSelected ? 0.12f : hover * 0.06f;
                    Color sgColor = CyberwareTheme.Accent * (alpha * glowStr);
                    sgColor.A = 0;
                    sb.Draw(slotGlow, new Vector2(rect.Center.X, rect.Center.Y), null, sgColor, 0, slotGlow.Size() / 2,
                        new Vector2(rect.Width / 40f, rect.Height / 30f), SpriteEffects.None, 0);
                }

                //侧边强调条
                if (isSelected || hover > 0.01f) {
                    float barAlpha = isSelected ? 0.9f : hover * 0.5f;
                    Color barColor = CyberwareTheme.Accent * (alpha * barAlpha);
                    if (def.IsLeft) {
                        sb.Draw(px, new Rectangle(rect.X, rect.Y, 3, rect.Height), new Rectangle(0, 0, 1, 1), barColor);
                    }
                    else {
                        sb.Draw(px, new Rectangle(rect.Right - 3, rect.Y, 3, rect.Height), new Rectangle(0, 0, 1, 1), barColor);
                    }
                }

                //槽位标签
                Color labelColor = isSelected ? CyberwareTheme.Accent :
                    isHovered ? Color.Lerp(CyberwareTheme.TextDim, CyberwareTheme.TextBright, hover) :
                    CyberwareTheme.TextDim;
                labelColor *= alpha;
                float textX = rect.X + CyberwareTheme.SlotPadding + (def.IsLeft ? 4 : 0);
                string label = i < slotLabels.Length ? slotLabels[i] : "";
                Utils.DrawBorderString(sb, label, new Vector2(textX, rect.Y + 4), labelColor, 0.32f);

                //状态文字
                string statusStr = isSelected ? selectedText : emptyText;
                Color statusColor = isSelected ? CyberwareTheme.AccentGold : CyberwareTheme.TextDim;
                statusColor *= alpha * 0.6f;
                Utils.DrawBorderString(sb, statusStr, new Vector2(textX, rect.Y + 18), statusColor, 0.28f);
            }
        }

        /// <summary>
        ///绘制各槽位到人体节点之间的折线连接和流动光点
        /// </summary>
        public void DrawConnectors(SpriteBatch sb, float alpha, Rectangle panelRect,
            CyberBodyRenderer body, Vector2 bodyOrigin, float dataStreamPhase) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (px == null) return;

            for (int i = 0; i < Definitions.Length; i++) {
                var def = Definitions[i];
                Rectangle slotRect = GetSlotRect(i, panelRect);

                //连线起点（槽位靠内侧边缘）
                Vector2 slotEdge = def.IsLeft
                    ? new Vector2(slotRect.Right, slotRect.Center.Y)
                    : new Vector2(slotRect.Left, slotRect.Center.Y);

                //连线终点（人体节点位置）
                Vector2 nodePos = body.GetNodeWorldPosition(def.NodeIndex, bodyOrigin);

                bool isActive = i == hoveredSlot || i == selectedSlot;
                float lineAlpha = isActive ? 0.6f : 0.15f;
                Color lineColor = isActive ? CyberwareTheme.Accent : CyberwareTheme.Connector;
                lineColor *= alpha * lineAlpha;

                //折线路径：水平→垂直→水平
                float midX = def.IsLeft
                    ? slotEdge.X + (nodePos.X - slotEdge.X) * 0.4f
                    : slotEdge.X - (slotEdge.X - nodePos.X) * 0.4f;

                Vector2 p1 = slotEdge;
                Vector2 p2 = new(midX, slotEdge.Y);
                Vector2 p3 = new(midX, nodePos.Y);
                Vector2 p4 = nodePos;

                CyberwareTheme.DrawLine(sb, px, p1, p2, 1f, lineColor);
                CyberwareTheme.DrawLine(sb, px, p2, p3, 1f, lineColor);
                CyberwareTheme.DrawLine(sb, px, p3, p4, 1f, lineColor);

                //活跃连线上的流动光点
                if (isActive && glow != null) {
                    float t = (dataStreamPhase / MathHelper.TwoPi) % 1f;
                    Vector2 flowPos = CyberwareTheme.EvaluatePolyline(t, p1, p2, p3, p4);
                    Color flowColor = CyberwareTheme.Accent * (alpha * 0.5f);
                    flowColor.A = 0;
                    sb.Draw(glow, flowPos, null, flowColor, 0, glow.Size() / 2, 0.05f, SpriteEffects.None, 0);
                }

                //折线拐角的小方块装饰
                Color dotColor = lineColor * 1.5f;
                sb.Draw(px, p2, new Rectangle(0, 0, 1, 1), dotColor, 0, new Vector2(0.5f), 2f, SpriteEffects.None, 0);
                sb.Draw(px, p3, new Rectangle(0, 0, 1, 1), dotColor, 0, new Vector2(0.5f), 2f, SpriteEffects.None, 0);
            }
        }

        /// <summary>
        ///获取指定槽位的屏幕矩形区域
        /// </summary>
        public Rectangle GetSlotRect(int index, Rectangle panelRect) {
            var def = Definitions[index];
            float slotW = CyberwareTheme.PanelWidth * 0.20f;
            int x = panelRect.X + (int)(def.XRatio * panelRect.Width);
            int y = panelRect.Y + (int)(def.YRatio * panelRect.Height);
            return new Rectangle(x, y, (int)slotW, (int)CyberwareTheme.SlotSize);
        }

        #endregion
    }
}
