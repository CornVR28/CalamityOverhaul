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
            //头部（圆角矩形，所有角用斜线连接）
            {12, 0, 20, 0},     //颅顶
            {11, 1, 12, 0},     //左上圆角
            {20, 0, 21, 1},     //右上圆角
            {11, 1, 11, 5},     //左太阳穴
            {21, 1, 21, 5},     //右太阳穴
            {11, 5, 12, 6},     //左颧骨
            {21, 5, 20, 6},     //右颧骨
            //下颌线过渡到颈部（斜线确保连接）
            {12, 6, 14, 8},     //左下颌
            {20, 6, 18, 8},     //右下颌
            {14, 8, 18, 8},     //下巴
            //颈部（共享下颌端点）
            {14, 8, 14, 10},    //左颈
            {18, 8, 18, 10},    //右颈
            //肩膀（自然斜坡，共享颈部端点）
            {14, 10, 8, 12},    //左肩斜坡
            {18, 10, 24, 12},   //右肩斜坡
            //躯干上段（连接肩膀到手臂分叉处）
            {8, 12, 8, 13},     //左上胸侧
            {24, 12, 24, 13},   //右上胸侧
            {10, 14, 22, 14},   //胸顶横线
            //左侧躯干（胸→腰收窄→髋外扩）
            {10, 14, 10, 20},   //左胸侧
            {10, 20, 11, 23},   //左腰收窄斜线
            {11, 23, 11, 26},   //左腰窄段
            {11, 26, 10, 28},   //左髋外扩
            //右侧躯干（镜像）
            {22, 14, 22, 20},   //右胸侧
            {22, 20, 21, 23},   //右腰收窄斜线
            {21, 23, 21, 26},   //右腰窄段
            {21, 26, 22, 28},   //右髋外扩
            //左臂（肩→上臂→肘关节→前臂→腕→手）
            {5, 13, 8, 13},     //肩顶
            {5, 13, 5, 24},     //外侧连续线
            {8, 13, 8, 18},     //上臂内侧
            {8, 18, 7, 19},     //肘关节内斜
            {7, 19, 7, 24},     //前臂内侧
            {5, 24, 4, 25},     //腕外展
            {7, 24, 8, 25},     //腕内展
            {4, 25, 4, 27},     //手外侧
            {8, 25, 8, 27},     //手内侧
            {4, 27, 9, 27},     //手掌底
            //右臂（镜像）
            {24, 13, 27, 13},   //肩顶
            {27, 13, 27, 24},   //外侧连续线
            {24, 13, 24, 18},   //上臂内侧
            {24, 18, 25, 19},   //肘关节内斜
            {25, 19, 25, 24},   //前臂内侧
            {27, 24, 28, 25},   //腕外展
            {25, 24, 24, 25},   //腕内展
            {28, 25, 28, 27},   //手外侧
            {24, 25, 24, 27},   //手内侧
            {23, 27, 28, 27},   //手掌底
            //髋部（分叉+裆部+衔接腿部）
            {10, 28, 15, 28},   //左髋横线
            {17, 28, 22, 28},   //右髋横线
            {10, 28, 10, 29},   //左外侧髋→腿衔接
            {22, 28, 22, 29},   //右外侧髋→腿衔接
            {15, 28, 15, 29},   //左裆内侧
            {17, 28, 17, 29},   //右裆内侧
            //左腿（大腿→膝关节→小腿→踝→足）
            {10, 29, 10, 36},   //大腿外侧
            {15, 29, 15, 36},   //大腿内侧
            {10, 36, 11, 38},   //膝外斜
            {15, 36, 14, 38},   //膝内斜
            {11, 38, 11, 44},   //小腿外侧
            {14, 38, 14, 44},   //小腿内侧
            {11, 44, 9, 46},    //踝→足跟
            {14, 44, 15, 46},   //踝→脚趾
            {9, 46, 16, 46},    //左足底
            //右腿（镜像）
            {22, 29, 22, 36},   //大腿外侧
            {17, 29, 17, 36},   //大腿内侧
            {22, 36, 21, 38},   //膝外斜
            {17, 36, 18, 38},   //膝内斜
            {21, 38, 21, 44},   //小腿外侧
            {18, 38, 18, 44},   //小腿内侧
            {21, 44, 23, 46},   //踝→足跟
            {18, 44, 17, 46},   //踝→脚趾
            {16, 46, 23, 46},   //右足底
        };

        //内部结构线，骨骼和电路风格
        private static readonly int[,] InnerLines = {
            //头部神经电路
            {13, 2, 19, 2},     //上层脑回路
            {14, 4, 18, 4},     //下层脑回路
            {16, 1, 16, 6},     //脑中线（连接到脊椎）
            {14, 3, 15, 3},     //左眼电路
            {17, 3, 18, 3},     //右眼电路
            //脊椎（从头部延伸到骨盆）
            {16, 6, 16, 28},
            //锁骨连接器（脊椎→肩关节）
            {10, 14, 16, 13},   //左锁骨
            {22, 14, 16, 13},   //右锁骨
            //肋骨（从宽到窄匹配躯干锥形）
            {11, 16, 21, 16},   //上肋
            {11, 18, 21, 18},   //中肋
            {12, 20, 20, 20},   //下肋（进入腰部收窄区）
            //胸腔交叉电路（围绕核心）
            {13, 15, 19, 17},   //左上→右下
            {19, 15, 13, 17},   //右上→左下
            //腹部电路
            {12, 24, 20, 24},   //腹横线
            {13, 26, 19, 26},   //下腹横线
            //骨盆
            {11, 28, 16, 31},
            {21, 28, 16, 31},
            //髋关节连接器（脊椎→腿骨）
            {16, 28, 12, 30},   //左髋
            {16, 28, 20, 30},   //右髋
            //腿部中线（分段：大腿骨+小腿骨）
            {12, 29, 12, 36},   //左大腿骨
            {12, 38, 12, 44},   //左小腿骨
            {20, 29, 20, 36},   //右大腿骨
            {20, 38, 20, 44},   //右小腿骨
            //膝关节横线
            {11, 37, 14, 37},   //左膝
            {18, 37, 21, 37},   //右膝
            //足部电路
            {10, 45, 14, 45},   //左足横线
            {18, 45, 22, 45},   //右足横线
            //手臂中线（分段：上臂骨+前臂骨+手指分隔）
            {6, 14, 6, 18},     //左上臂骨
            {6, 19, 6, 24},     //左前臂骨
            {5, 26, 8, 26},     //左手指分隔
            {26, 14, 26, 18},   //右上臂骨
            {26, 19, 26, 24},   //右前臂骨
            {24, 26, 27, 26},   //右手指分隔
            //肘关节横线
            {5, 19, 7, 19},     //左肘
            {25, 19, 27, 19},   //右肘
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
            //头部（分层填充以匹配圆角轮廓）
            CyberwareTheme.FillGridRect(sb, px, offset, s, 13, 0, 6, 1, fillColor, breathe);  //颅顶窄
            CyberwareTheme.FillGridRect(sb, px, offset, s, 12, 1, 8, 5, fillColor, breathe);  //面部主体
            CyberwareTheme.FillGridRect(sb, px, offset, s, 13, 6, 6, 1, fillColor, breathe);  //上颌
            CyberwareTheme.FillGridRect(sb, px, offset, s, 14, 7, 4, 1, fillColor, breathe);  //下颌
            //颈部
            CyberwareTheme.FillGridRect(sb, px, offset, s, 14, 8, 4, 2, fillColor, breathe);
            //肩膀过渡（梯形近似）
            CyberwareTheme.FillGridRect(sb, px, offset, s, 13, 10, 6, 1, fillColor, breathe);  //锁骨
            CyberwareTheme.FillGridRect(sb, px, offset, s, 10, 11, 12, 1, fillColor, breathe); //肩中段
            CyberwareTheme.FillGridRect(sb, px, offset, s, 8, 12, 16, 2, fillColor, breathe);  //上胸
            //躯干（分段填充匹配腰部收窄）
            CyberwareTheme.FillGridRect(sb, px, offset, s, 10, 14, 12, 6, fillColor, breathe);  //上胸 y14-20
            CyberwareTheme.FillGridRect(sb, px, offset, s, 11, 20, 10, 6, fillColor, breathe);  //腰部 y20-26（窄）
            CyberwareTheme.FillGridRect(sb, px, offset, s, 10, 26, 12, 3, fillColor, breathe);  //髋部 y26-29
            //左臂（分段填充：上臂+前臂+手）
            CyberwareTheme.FillGridRect(sb, px, offset, s, 5, 13, 3, 6, fillColor, breathe);   //上臂 y13-19
            CyberwareTheme.FillGridRect(sb, px, offset, s, 5, 19, 2, 6, fillColor, breathe);   //前臂 y19-25
            CyberwareTheme.FillGridRect(sb, px, offset, s, 4, 25, 5, 2, fillColor, breathe);   //手掌 y25-27
            //右臂
            CyberwareTheme.FillGridRect(sb, px, offset, s, 24, 13, 3, 6, fillColor, breathe);  //上臂
            CyberwareTheme.FillGridRect(sb, px, offset, s, 25, 19, 2, 6, fillColor, breathe);  //前臂
            CyberwareTheme.FillGridRect(sb, px, offset, s, 24, 25, 5, 2, fillColor, breathe);  //手掌
            //左腿（分段：大腿+小腿+足）
            CyberwareTheme.FillGridRect(sb, px, offset, s, 10, 29, 5, 9, fillColor, breathe);   //大腿+膝 y29-38
            CyberwareTheme.FillGridRect(sb, px, offset, s, 11, 38, 3, 6, fillColor, breathe);   //小腿 y38-44
            CyberwareTheme.FillGridRect(sb, px, offset, s, 9, 44, 7, 2, fillColor, breathe);    //足部 y44-46
            //右腿
            CyberwareTheme.FillGridRect(sb, px, offset, s, 17, 29, 5, 9, fillColor, breathe);   //大腿+膝
            CyberwareTheme.FillGridRect(sb, px, offset, s, 18, 38, 3, 6, fillColor, breathe);   //小腿
            CyberwareTheme.FillGridRect(sb, px, offset, s, 16, 44, 7, 2, fillColor, breathe);   //足部
        }

        #endregion
    }
}