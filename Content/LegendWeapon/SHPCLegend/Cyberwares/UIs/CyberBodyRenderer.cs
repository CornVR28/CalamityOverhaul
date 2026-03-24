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
        public const float PixelScale = 4.5f;

        /// <summary>
        ///人体节点总数
        /// </summary>
        public const int NodeCount = 11;

        #region 像素人体数据

        //人体轮廓线段数据，每行为(x1,y1,x2,y2)，坐标基于32x60像素网格（七头身比例）
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
            {10, 14, 10, 24},   //左胸侧
            {10, 24, 11, 28},   //左腰收窄斜线
            {11, 28, 11, 31},   //左腰窄段
            {11, 31, 10, 34},   //左髋外扩
            //右侧躯干（镜像）
            {22, 14, 22, 24},   //右胸侧
            {22, 24, 21, 28},   //右腰收窄斜线
            {21, 28, 21, 31},   //右腰窄段
            {21, 31, 22, 34},   //右髋外扩
            //左臂（肩→上臂→肘关节→前臂→腕→手）
            {5, 13, 8, 13},     //肩顶
            {5, 13, 5, 28},     //外侧连续线
            {8, 13, 8, 21},     //上臂内侧
            {8, 21, 7, 22},     //肘关节内斜
            {7, 22, 7, 28},     //前臂内侧
            {5, 28, 4, 29},     //腕外展
            {7, 28, 8, 29},     //腕内展
            {4, 29, 4, 32},     //手外侧
            {8, 29, 8, 32},     //手内侧
            {4, 32, 9, 32},     //手掌底
            //右臂（镜像）
            {24, 13, 27, 13},   //肩顶
            {27, 13, 27, 28},   //外侧连续线
            {24, 13, 24, 21},   //上臂内侧
            {24, 21, 25, 22},   //肘关节内斜
            {25, 22, 25, 28},   //前臂内侧
            {27, 28, 28, 29},   //腕外展
            {25, 28, 24, 29},   //腕内展
            {28, 29, 28, 32},   //手外侧
            {24, 29, 24, 32},   //手内侧
            {23, 32, 28, 32},   //手掌底
            //髋部（分叉+裆部+衔接腿部）
            {10, 34, 15, 34},   //左髋横线
            {17, 34, 22, 34},   //右髋横线
            {10, 34, 10, 35},   //左外侧髋→腿衔接
            {22, 34, 22, 35},   //右外侧髋→腿衔接
            {15, 34, 15, 35},   //左裆内侧
            {17, 34, 17, 35},   //右裆内侧
            //左腿（大腿→膝关节→小腿→踝→足）
            {10, 35, 10, 45},   //大腿外侧
            {15, 35, 15, 45},   //大腿内侧
            {10, 45, 11, 47},   //膝外斜
            {15, 45, 14, 47},   //膝内斜
            {11, 47, 11, 55},   //小腿外侧
            {14, 47, 14, 55},   //小腿内侧
            {11, 55, 9, 58},    //踝→足跟
            {14, 55, 15, 58},   //踝→脚趾
            {9, 58, 16, 58},    //左足底
            //右腿（镜像）
            {22, 35, 22, 45},   //大腿外侧
            {17, 35, 17, 45},   //大腿内侧
            {22, 45, 21, 47},   //膝外斜
            {17, 45, 18, 47},   //膝内斜
            {21, 47, 21, 55},   //小腿外侧
            {18, 47, 18, 55},   //小腿内侧
            {21, 55, 23, 58},   //踝→足跟
            {18, 55, 17, 58},   //踝→脚趾
            {16, 58, 23, 58},   //右足底
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
            {16, 6, 16, 34},
            //锁骨连接器（脊椎→肩关节）
            {10, 14, 16, 13},   //左锁骨
            {22, 14, 16, 13},   //右锁骨
            //肋骨（从宽到窄匹配躯干锥形）
            {11, 18, 21, 18},   //上肋
            {11, 21, 21, 21},   //中肋
            {12, 24, 20, 24},   //下肋
            //胸腔交叉电路（围绕核心）
            {13, 16, 19, 19},   //左上→右下
            {19, 16, 13, 19},   //右上→左下
            //腹部电路
            {12, 28, 20, 28},   //腹横线
            {13, 31, 19, 31},   //下腹横线
            //骨盆
            {11, 34, 16, 38},
            {21, 34, 16, 38},
            //髋关节连接器（脊椎→腿骨）
            {16, 34, 12, 37},   //左髋
            {16, 34, 20, 37},   //右髋
            //腿部中线（分段：大腿骨+小腿骨）
            {12, 35, 12, 45},   //左大腿骨
            {12, 47, 12, 55},   //左小腿骨
            {20, 35, 20, 45},   //右大腿骨
            {20, 47, 20, 55},   //右小腿骨
            //膝关节横线
            {11, 46, 14, 46},   //左膝
            {18, 46, 21, 46},   //右膝
            //足部电路
            {10, 57, 14, 57},   //左足横线
            {18, 57, 22, 57},   //右足横线
            //手臂中线（分段：上臂骨+前臂骨+手指分隔）
            {6, 14, 6, 21},     //左上臂骨
            {6, 22, 6, 28},     //左前臂骨
            {5, 31, 8, 31},     //左手指分隔
            {26, 14, 26, 21},   //右上臂骨
            {26, 22, 26, 28},   //右前臂骨
            {24, 31, 27, 31},   //右手指分隔
            //肘关节横线
            {5, 22, 7, 22},     //左肘
            {25, 22, 27, 22},   //右肘
        };

        //赛博植入节点坐标(x,y)
        private static readonly int[,] NodePositions = {
            {16, 4},   //头部芯片
            {16, 16},  //胸腔核心
            {6, 21},   //左臂改造
            {26, 21},  //右臂改造
            {12, 30},  //左手植入
            {20, 30},  //右手植入
            {16, 34},  //腰椎接口
            {12, 42},  //左腿改造
            {20, 42},  //右腿改造
            {12, 53},  //左足增强
            {20, 53},  //右足增强
        };

        #endregion

        #region 动画状态

        private float breathePhase;
        private float scanLineY;
        private float energyFlowPhase;
        private readonly float[] nodePulsePhase = new float[NodeCount];

        #endregion

        #region 公共方法

        /// <summary>
        ///获取指定节点在屏幕上的世界坐标
        /// </summary>
        public Vector2 GetNodeWorldPosition(int nodeIndex, Vector2 bodyOrigin) {
            float s = PixelScale;
            Vector2 offset = bodyOrigin - new Vector2(16 * s, 29 * s);
            float breathe = MathF.Sin(breathePhase) * 0.8f;
            return offset + new Vector2(NodePositions[nodeIndex, 0] * s, NodePositions[nodeIndex, 1] * s + breathe);
        }

        /// <summary>
        ///推进呼吸和节点脉冲等动画计时器
        /// </summary>
        public void Update() {
            breathePhase += 0.02f;
            if (breathePhase > MathHelper.TwoPi) breathePhase -= MathHelper.TwoPi;

            //扫描线从头到脚循环（0→58网格单位）
            scanLineY += 0.35f;
            if (scanLineY > 62f) scanLineY = -4f;

            //能量流动相位
            energyFlowPhase += 0.04f;
            if (energyFlowPhase > MathHelper.TwoPi) energyFlowPhase -= MathHelper.TwoPi;

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

            float breathe = MathF.Sin(breathePhase) * 0.8f;
            float s = PixelScale;
            Vector2 bodyOffset = bodyOrigin - new Vector2(16 * s, 29 * s);

            //填充身体主要区域
            DrawBodyFill(sb, px, bodyOffset, s, alpha, breathe);

            //内部骨骼电路（带能量流动脉冲）
            DrawInnerCircuits(sb, px, bodyOffset, s, alpha, breathe, globalTimer);

            //轮廓线
            Color outlineColor = CyberwareTheme.BodyOutline * (alpha * 0.7f);
            for (int i = 0; i < OutlineSegments.GetLength(0); i++) {
                Vector2 start = bodyOffset + new Vector2(OutlineSegments[i, 0] * s, OutlineSegments[i, 1] * s + breathe);
                Vector2 end = bodyOffset + new Vector2(OutlineSegments[i, 2] * s, OutlineSegments[i, 3] * s + breathe);
                CyberwareTheme.DrawLine(sb, px, start, end, 2f, outlineColor);
            }

            //能量粒子沿脊椎流动
            DrawSpineEnergyFlow(sb, px, bodyOffset, s, alpha, breathe, globalTimer);

            //外发光
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null) {
                float glowPulse = MathF.Sin(globalTimer * 1.2f) * 0.1f + 0.9f;
                Color bodyGlow = CyberwareTheme.Accent * (alpha * 0.08f * glowPulse);
                bodyGlow.A = 0;
                sb.Draw(glow, bodyOrigin + new Vector2(0, breathe),
                    null, bodyGlow, 0, glow.Size() / 2, 5.5f, SpriteEffects.None, 0);
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
            Vector2 bodyOffset = bodyOrigin - new Vector2(16 * s, 29 * s);
            float breathe = MathF.Sin(breathePhase) * 0.8f;

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
                float nodeSize = isHighlighted ? 7f : 4f;
                sb.Draw(px, nodePos, new Rectangle(0, 0, 1, 1), nodeColor,
                    MathHelper.PiOver4, new Vector2(0.5f), new Vector2(nodeSize), SpriteEffects.None, 0f);

                //节点光晕
                if (glow != null) {
                    Color nodeGlow = nodeColor * 0.4f;
                    nodeGlow.A = 0;
                    sb.Draw(glow, nodePos, null, nodeGlow, 0, glow.Size() / 2,
                        0.08f + (isHighlighted ? 0.05f : 0f), SpriteEffects.None, 0);
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        ///绘制内部电路线，每条线根据其Y位置产生独立的能量脉冲波纹
        /// </summary>
        private void DrawInnerCircuits(SpriteBatch sb, Texture2D px, Vector2 bodyOffset, float s, float alpha, float breathe, float globalTimer) {
            Color baseInner = CyberwareTheme.BodyInner * (alpha * 0.35f);
            Color accentInner = CyberwareTheme.AccentCyan * (alpha * 0.2f);

            for (int i = 0; i < InnerLines.GetLength(0); i++) {
                float y1 = InnerLines[i, 1];
                float y2 = InnerLines[i, 3];
                float midY = (y1 + y2) * 0.5f;

                //基于Y位置的波纹脉冲——模拟能量从头部向下流动
                float wave = MathF.Sin(energyFlowPhase - midY * 0.15f);
                float brightness = wave * 0.3f + 0.7f;

                //混合基础暗色和强调色
                Color lineColor = Color.Lerp(baseInner, accentInner, Math.Clamp(wave * 0.5f + 0.3f, 0f, 1f));
                lineColor *= brightness;

                Vector2 start = bodyOffset + new Vector2(InnerLines[i, 0] * s, y1 * s + breathe);
                Vector2 end = bodyOffset + new Vector2(InnerLines[i, 2] * s, y2 * s + breathe);
                CyberwareTheme.DrawLine(sb, px, start, end, 1f, lineColor);
            }
        }

        /// <summary>
        ///绘制沿脊椎线从上到下流动的能量粒子
        /// </summary>
        private void DrawSpineEnergyFlow(SpriteBatch sb, Texture2D px, Vector2 bodyOffset, float s, float alpha, float breathe, float globalTimer) {
            Texture2D glow = CWRAsset.SoftGlow?.Value;

            //脊椎范围 y=6→y=34
            float spineX = 16 * s;
            const int particleCount = 5;
            float spineTop = 6f;
            float spineBot = 34f;
            float spineLen = spineBot - spineTop;

            for (int i = 0; i < particleCount; i++) {
                //每个粒子在脊椎上均匀分布，以不同速度循环
                float t = (energyFlowPhase / MathHelper.TwoPi + i / (float)particleCount) % 1f;
                float gridY = spineTop + t * spineLen;
                float screenX = bodyOffset.X + spineX;
                float screenY = bodyOffset.Y + gridY * s + breathe;

                //粒子亮度随位置脉动
                float particleBright = MathF.Sin(t * MathHelper.Pi) * 0.6f + 0.4f;
                Color particleColor = CyberwareTheme.Accent * (alpha * 0.5f * particleBright);
                particleColor.A = 0;

                //小方形粒子
                sb.Draw(px, new Vector2(screenX - 1.5f, screenY - 1.5f), new Rectangle(0, 0, 1, 1),
                    particleColor, 0f, Vector2.Zero, new Vector2(3f, 3f), SpriteEffects.None, 0f);

                //粒子光晕
                if (glow != null) {
                    Color glowColor = CyberwareTheme.Accent * (alpha * 0.2f * particleBright);
                    glowColor.A = 0;
                    sb.Draw(glow, new Vector2(screenX, screenY), null, glowColor,
                        0, glow.Size() / 2, 0.04f, SpriteEffects.None, 0f);
                }
            }

            //额外：左右腿骨各2个流动粒子
            DrawLimbEnergyParticles(sb, px, glow, bodyOffset, s, alpha, breathe, 12f, 35f, 55f); //左腿
            DrawLimbEnergyParticles(sb, px, glow, bodyOffset, s, alpha, breathe, 20f, 35f, 55f); //右腿
        }

        /// <summary>
        ///沿肢体中线绘制流动粒子
        /// </summary>
        private void DrawLimbEnergyParticles(SpriteBatch sb, Texture2D px, Texture2D glow,
            Vector2 bodyOffset, float s, float alpha, float breathe,
            float gridX, float topY, float botY) {

            float screenX = bodyOffset.X + gridX * s;
            float limbLen = botY - topY;
            const int count = 2;

            for (int i = 0; i < count; i++) {
                float t = (energyFlowPhase / MathHelper.TwoPi * 0.7f + i / (float)count) % 1f;
                float gridY = topY + t * limbLen;
                float screenY = bodyOffset.Y + gridY * s + breathe;

                float bright = MathF.Sin(t * MathHelper.Pi) * 0.5f + 0.3f;
                Color c = CyberwareTheme.AccentCyan * (alpha * 0.35f * bright);
                c.A = 0;

                sb.Draw(px, new Vector2(screenX - 1f, screenY - 1f), new Rectangle(0, 0, 1, 1),
                    c, 0f, Vector2.Zero, new Vector2(2f, 2f), SpriteEffects.None, 0f);

                if (glow != null) {
                    Color gc = CyberwareTheme.AccentCyan * (alpha * 0.15f * bright);
                    gc.A = 0;
                    sb.Draw(glow, new Vector2(screenX, screenY), null, gc,
                        0, glow.Size() / 2, 0.03f, SpriteEffects.None, 0f);
                }
            }
        }

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
            CyberwareTheme.FillGridRect(sb, px, offset, s, 10, 14, 12, 10, fillColor, breathe); //胸部 y14-24
            CyberwareTheme.FillGridRect(sb, px, offset, s, 11, 24, 10, 7, fillColor, breathe);  //腰部 y24-31（窄）
            CyberwareTheme.FillGridRect(sb, px, offset, s, 10, 31, 12, 4, fillColor, breathe);  //髋部 y31-35
            //左臂（分段填充：上臂+前臂+手）
            CyberwareTheme.FillGridRect(sb, px, offset, s, 5, 13, 3, 8, fillColor, breathe);    //上臂 y13-21
            CyberwareTheme.FillGridRect(sb, px, offset, s, 5, 21, 2, 8, fillColor, breathe);    //前臂 y21-29
            CyberwareTheme.FillGridRect(sb, px, offset, s, 4, 29, 5, 3, fillColor, breathe);    //手掌 y29-32
            //右臂
            CyberwareTheme.FillGridRect(sb, px, offset, s, 24, 13, 3, 8, fillColor, breathe);   //上臂
            CyberwareTheme.FillGridRect(sb, px, offset, s, 25, 21, 2, 8, fillColor, breathe);   //前臂
            CyberwareTheme.FillGridRect(sb, px, offset, s, 24, 29, 5, 3, fillColor, breathe);   //手掌
            //左腿（分段：大腿+小腿+足）
            CyberwareTheme.FillGridRect(sb, px, offset, s, 10, 35, 5, 12, fillColor, breathe);  //大腿+膝 y35-47
            CyberwareTheme.FillGridRect(sb, px, offset, s, 11, 47, 3, 8, fillColor, breathe);   //小腿 y47-55
            CyberwareTheme.FillGridRect(sb, px, offset, s, 9, 55, 7, 3, fillColor, breathe);    //足部 y55-58
            //右腿
            CyberwareTheme.FillGridRect(sb, px, offset, s, 17, 35, 5, 12, fillColor, breathe);  //大腿+膝
            CyberwareTheme.FillGridRect(sb, px, offset, s, 18, 47, 3, 8, fillColor, breathe);   //小腿
            CyberwareTheme.FillGridRect(sb, px, offset, s, 16, 55, 7, 3, fillColor, breathe);   //足部
        }

        #endregion
    }
}