using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberwares.UIs
{
    /// <summary>
    ///赛博义体界面的像素人体渲染器
    ///负责程序化绘制像素风格的人形轮廓、体内电路结构和赛博植入节点
    /// </summary>
    internal class CyberBodyRenderer
    {
        /// <summary>
        ///像素缩放倍率，控制人体绘制的整体大小
        /// </summary>
        public const float PixelScale = 3f;

        /// <summary>
        ///人体节点总数
        /// </summary>
        public const int NodeCount = 11;

        #region 像素人体数据

        //人体轮廓线段数据，每行为(x1,y1,x2,y2)，坐标基于32x48像素网格
        private static readonly int[,] OutlineSegments = {
            //头部
            {13, 1, 19, 1},
            {12, 2, 12, 6},
            {20, 2, 20, 6},
            {13, 7, 19, 7},
            //颈部
            {14, 8, 14, 9},
            {18, 8, 18, 9},
            //肩膀
            {8, 10, 13, 10},
            {19, 10, 24, 10},
            //躯干
            {8, 10, 8, 13},
            {24, 10, 24, 13},
            {10, 14, 10, 28},
            {22, 14, 22, 28},
            {10, 14, 22, 14},
            //左臂
            {5, 13, 8, 13},
            {5, 13, 5, 22},
            {8, 13, 8, 22},
            {5, 22, 5, 26},
            {8, 22, 8, 26},
            {4, 26, 9, 26},
            //右臂
            {24, 13, 27, 13},
            {27, 13, 27, 22},
            {24, 13, 24, 22},
            {27, 22, 27, 26},
            {24, 22, 24, 26},
            {23, 26, 28, 26},
            //腰部
            {10, 28, 22, 28},
            //大腿
            {10, 29, 10, 40},
            {15, 29, 15, 40},
            {17, 29, 17, 40},
            {22, 29, 22, 40},
            //小腿
            {10, 40, 10, 46},
            {15, 40, 15, 46},
            {17, 40, 17, 46},
            {22, 40, 22, 46},
            //脚
            {9, 46, 16, 46},
            {16, 46, 23, 46},
        };

        //内部结构线，骨骼和电路风格
        private static readonly int[,] InnerLines = {
            //脊椎
            {16, 9, 16, 28},
            //肋骨
            {12, 16, 20, 16},
            {11, 19, 21, 19},
            {12, 22, 20, 22},
            //骨盆
            {11, 28, 16, 31},
            {21, 28, 16, 31},
            //腿部中线
            {12, 29, 12, 46},
            {20, 29, 20, 46},
            //手臂中线
            {6, 14, 6, 25},
            {26, 14, 26, 25},
        };

        //赛博植入节点坐标(x,y)
        private static readonly int[,] NodePositions = {
            {16, 4},   //头部芯片
            {16, 12},  //胸腔核心
            {6, 18},   //左臂改造
            {26, 18},  //右臂改造
            {12, 25},  //左手植入
            {20, 25},  //右手植入
            {16, 28},  //腰椎接口
            {12, 38},  //左腿改造
            {20, 38},  //右腿改造
            {12, 44},  //左足增强
            {20, 44},  //右足增强
        };

        #endregion

        #region 动画状态

        private float breathePhase;
        private readonly float[] nodePulsePhase = new float[NodeCount];

        #endregion

        #region 公共方法

        /// <summary>
        ///获取指定节点在屏幕上的世界坐标
        /// </summary>
        public Vector2 GetNodeWorldPosition(int nodeIndex, Vector2 bodyOrigin) {
            float s = PixelScale;
            Vector2 offset = bodyOrigin - new Vector2(16 * s, 24 * s);
            float breathe = MathF.Sin(breathePhase) * 0.5f;
            return offset + new Vector2(NodePositions[nodeIndex, 0] * s, NodePositions[nodeIndex, 1] * s + breathe);
        }

        /// <summary>
        ///推进呼吸和节点脉冲等动画计时器
        /// </summary>
        public void Update() {
            breathePhase += 0.02f;
            if (breathePhase > MathHelper.TwoPi) breathePhase -= MathHelper.TwoPi;

            for (int i = 0; i < nodePulsePhase.Length; i++) {
                nodePulsePhase[i] += 0.03f + i * 0.004f;
                if (nodePulsePhase[i] > MathHelper.TwoPi) nodePulsePhase[i] -= MathHelper.TwoPi;
            }
        }

        /// <summary>
        ///绘制完整的像素人体，包括填充、内部结构、轮廓和外发光
        /// </summary>
        public void DrawBody(SpriteBatch sb, float alpha, Vector2 bodyOrigin, float globalTimer) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            float breathe = MathF.Sin(breathePhase) * 0.5f;
            float s = PixelScale;
            Vector2 bodyOffset = bodyOrigin - new Vector2(16 * s, 24 * s);

            //填充身体主要区域
            DrawBodyFill(sb, px, bodyOffset, s, alpha, breathe);

            //内部骨骼电路
            Color innerColor = CyberwareTheme.BodyInner * (alpha * 0.35f);
            float innerPulse = MathF.Sin(globalTimer * 1.5f) * 0.15f + 0.85f;
            innerColor *= innerPulse;
            for (int i = 0; i < InnerLines.GetLength(0); i++) {
                Vector2 start = bodyOffset + new Vector2(InnerLines[i, 0] * s, InnerLines[i, 1] * s + breathe);
                Vector2 end = bodyOffset + new Vector2(InnerLines[i, 2] * s, InnerLines[i, 3] * s + breathe);
                CyberwareTheme.DrawLine(sb, px, start, end, 1f, innerColor);
            }

            //轮廓线
            Color outlineColor = CyberwareTheme.BodyOutline * (alpha * 0.7f);
            for (int i = 0; i < OutlineSegments.GetLength(0); i++) {
                Vector2 start = bodyOffset + new Vector2(OutlineSegments[i, 0] * s, OutlineSegments[i, 1] * s + breathe);
                Vector2 end = bodyOffset + new Vector2(OutlineSegments[i, 2] * s, OutlineSegments[i, 3] * s + breathe);
                CyberwareTheme.DrawLine(sb, px, start, end, 2f, outlineColor);
            }

            //外发光
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null) {
                float glowPulse = MathF.Sin(globalTimer * 1.2f) * 0.1f + 0.9f;
                Color bodyGlow = CyberwareTheme.Accent * (alpha * 0.08f * glowPulse);
                bodyGlow.A = 0;
                sb.Draw(glow, bodyOrigin + new Vector2(0, breathe),
                    null, bodyGlow, 0, glow.Size() / 2, 4f, SpriteEffects.None, 0);
            }
        }

        /// <summary>
        ///绘制赛博植入节点标记
        ///nodeStates数组标记各节点状态：0普通 1已连接 2高亮激活
        /// </summary>
        public void DrawNodes(SpriteBatch sb, float alpha, Vector2 bodyOrigin, int[] nodeStates) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (px == null) return;

            float s = PixelScale;
            Vector2 bodyOffset = bodyOrigin - new Vector2(16 * s, 24 * s);
            float breathe = MathF.Sin(breathePhase) * 0.5f;

            for (int i = 0; i < NodeCount; i++) {
                Vector2 nodePos = bodyOffset + new Vector2(NodePositions[i, 0] * s, NodePositions[i, 1] * s + breathe);
                float pulse = MathF.Sin(nodePulsePhase[i]) * 0.3f + 0.7f;

                int state = i < nodeStates.Length ? nodeStates[i] : 0;
                bool isHighlighted = state == 2;

                Color nodeColor = state switch {
                    2 => CyberwareTheme.AccentGold,
                    1 => CyberwareTheme.Accent,
                    _ => CyberwareTheme.AccentCyan
                };
                nodeColor *= alpha * pulse;

                //节点菱形方块
                float nodeSize = isHighlighted ? 5f : 3f;
                sb.Draw(px, nodePos, new Rectangle(0, 0, 1, 1), nodeColor,
                    MathHelper.PiOver4, new Vector2(0.5f), new Vector2(nodeSize), SpriteEffects.None, 0f);

                //节点光晕
                if (glow != null) {
                    Color nodeGlow = nodeColor * 0.4f;
                    nodeGlow.A = 0;
                    sb.Draw(glow, nodePos, null, nodeGlow, 0, glow.Size() / 2,
                        0.06f + (isHighlighted ? 0.04f : 0f), SpriteEffects.None, 0);
                }
            }
        }

        #endregion

        #region 私有方法

        private static void DrawBodyFill(SpriteBatch sb, Texture2D px, Vector2 offset, float s, float alpha, float breathe) {
            Color fillColor = CyberwareTheme.BodyFill * (alpha * 0.6f);
            //头部
            CyberwareTheme.FillGridRect(sb, px, offset, s, 13, 2, 7, 5, fillColor, breathe);
            //颈部
            CyberwareTheme.FillGridRect(sb, px, offset, s, 14, 8, 4, 2, fillColor, breathe);
            //躯干
            CyberwareTheme.FillGridRect(sb, px, offset, s, 10, 10, 12, 18, fillColor, breathe);
            //左臂
            CyberwareTheme.FillGridRect(sb, px, offset, s, 5, 13, 3, 13, fillColor, breathe);
            //右臂
            CyberwareTheme.FillGridRect(sb, px, offset, s, 24, 13, 3, 13, fillColor, breathe);
            //左腿
            CyberwareTheme.FillGridRect(sb, px, offset, s, 10, 29, 5, 17, fillColor, breathe);
            //右腿
            CyberwareTheme.FillGridRect(sb, px, offset, s, 17, 29, 5, 17, fillColor, breathe);
        }

        #endregion
    }
}
