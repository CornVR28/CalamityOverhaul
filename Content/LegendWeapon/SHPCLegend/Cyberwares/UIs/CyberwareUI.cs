using InnoVault.UIHandles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberwares.UIs
{
    /// <summary>
    /// 赛博朋克2077风格义体管理界面
    /// 深色科幻风格，中心为程序化绘制的像素人体图像，周围分布义体槽位
    /// </summary>
    internal class CyberwareUI : UIHandle
    {
        #region 常量

        /// <summary>面板总宽度</summary>
        private const float PanelWidth = 620f;
        /// <summary>面板总高度</summary>
        private const float PanelHeight = 420f;
        /// <summary>人体绘制像素缩放倍率</summary>
        private const float BodyPixelScale = 3f;
        /// <summary>义体槽位尺寸</summary>
        private const float SlotSize = 36f;
        /// <summary>槽位圆角内边距</summary>
        private const float SlotPadding = 4f;

        #endregion

        #region 颜色主题

        private static readonly Color ColorBgDark = new(8, 8, 12);
        private static readonly Color ColorBgPanel = new(14, 14, 20);
        private static readonly Color ColorBorder = new(45, 45, 55);
        private static readonly Color ColorAccent = new(255, 42, 42);         // 主强调色 - 赛博红
        private static readonly Color ColorAccentGold = new(220, 170, 40);    // 副强调色 - 赛博金
        private static readonly Color ColorAccentCyan = new(0, 220, 220);     // 信息色 - 青色
        private static readonly Color ColorTextDim = new(90, 90, 100);
        private static readonly Color ColorTextNormal = new(160, 160, 175);
        private static readonly Color ColorTextBright = new(225, 225, 235);
        private static readonly Color ColorGridLine = new(25, 25, 35);
        private static readonly Color ColorBodyOutline = new(255, 50, 50);     // 人体轮廓色
        private static readonly Color ColorBodyFill = new(30, 12, 12);         // 人体填充色
        private static readonly Color ColorBodyInner = new(60, 20, 20);        // 人体内部线条
        private static readonly Color ColorSlotEmpty = new(25, 25, 32);
        private static readonly Color ColorSlotBorder = new(55, 55, 65);
        private static readonly Color ColorConnector = new(60, 20, 20);

        #endregion

        #region 像素人体数据

        /// <summary>
        /// 像素人体轮廓数据 - 每行为一个线段 (x1, y1, x2, y2)，坐标基于32x48的像素网格
        /// 代表一个正面站立的简化人形，赛博朋克风格
        /// </summary>
        private static readonly int[,] BodyOutlineSegments = {
            // === 头部 ===
            {13, 1, 19, 1},   // 头顶
            {12, 2, 12, 6},   // 头部左侧
            {20, 2, 20, 6},   // 头部右侧
            {13, 7, 19, 7},   // 下巴

            // === 颈部 ===
            {14, 8, 14, 9},   // 颈左
            {18, 8, 18, 9},   // 颈右

            // === 肩膀 ===
            {8, 10, 13, 10},  // 左肩上
            {19, 10, 24, 10}, // 右肩上

            // === 躯干 ===
            {8, 10, 8, 13},   // 左臂连接
            {24, 10, 24, 13}, // 右臂连接
            {10, 14, 10, 28}, // 躯干左侧
            {22, 14, 22, 28}, // 躯干右侧
            {10, 14, 22, 14}, // 胸部上沿

            // === 手臂 ===
            {5, 13, 8, 13},   // 左上臂外
            {5, 13, 5, 22},   // 左臂外侧
            {8, 13, 8, 22},   // 左臂内侧
            {5, 22, 5, 26},   // 左前臂外
            {8, 22, 8, 26},   // 左前臂内
            {4, 26, 9, 26},   // 左手

            {24, 13, 27, 13}, // 右上臂外
            {27, 13, 27, 22}, // 右臂外侧
            {24, 13, 24, 22}, // 右臂内侧
            {27, 22, 27, 26}, // 右前臂外
            {24, 22, 24, 26}, // 右前臂内
            {23, 26, 28, 26}, // 右手

            // === 腰部 ===
            {10, 28, 22, 28}, // 腰线

            // === 腿部 ===
            {10, 29, 10, 40}, // 左腿外侧
            {15, 29, 15, 40}, // 左腿内侧
            {17, 29, 17, 40}, // 右腿内侧
            {22, 29, 22, 40}, // 右腿外侧

            // === 小腿 ===
            {10, 40, 10, 46}, // 左小腿外
            {15, 40, 15, 46}, // 左小腿内
            {17, 40, 17, 46}, // 右小腿外
            {22, 40, 22, 46}, // 右小腿内

            // === 脚 ===
            {9, 46, 16, 46},  // 左脚
            {16, 46, 23, 46}, // 右脚
        };

        /// <summary>
        /// 人体内部结构线（骨骼/电路风格）
        /// </summary>
        private static readonly int[,] BodyInnerLines = {
            // 脊椎
            {16, 9, 16, 28},
            // 肋骨（简化）
            {12, 16, 20, 16},
            {11, 19, 21, 19},
            {12, 22, 20, 22},
            // 骨盆
            {11, 28, 16, 31},
            {21, 28, 16, 31},
            // 腿部中线
            {12, 29, 12, 46},
            {20, 29, 20, 46},
            // 手臂中线
            {6, 14, 6, 25},
            {26, 14, 26, 25},
        };

        /// <summary>
        /// 人体上的赛博植入点标记位置 (x, y) - 关键节点
        /// </summary>
        private static readonly int[,] CyberNodes = {
            {16, 4},   // 头部芯片
            {16, 12},  // 胸腔核心
            {6, 18},   // 左臂改造
            {26, 18},  // 右臂改造
            {12, 25},  // 左手植入
            {20, 25},  // 右手植入
            {16, 28},  // 腰椎接口
            {12, 38},  // 左腿改造
            {20, 38},  // 右腿改造
            {12, 44},  // 左足增强
            {20, 44},  // 右足增强
        };

        #endregion

        #region 义体槽位定义

        /// <summary>
        /// 义体槽位 - (名称标签, 相对面板的X偏移比例, Y偏移比例, 连接到人体节点索引)
        /// 左侧槽位和右侧槽位对称分布
        /// </summary>
        private static readonly CyberwareSlotDef[] SlotDefinitions = [
            // 左侧槽位
            new("FRONTAL CORTEX",   0.04f, 0.08f,  0),   // 额叶皮层
            new("OCULAR SYSTEM",    0.04f, 0.18f,  0),   // 光学系统
            new("LEFT ARM",         0.04f, 0.36f,  2),   // 左臂
            new("HANDS",            0.04f, 0.50f,  4),   // 手部
            new("LEFT LEG",         0.04f, 0.68f,  7),   // 左腿
            new("FEET",             0.04f, 0.82f,  9),   // 足部

            // 右侧槽位
            new("OPERATING SYSTEM", 0.76f, 0.08f,  0),   // 操作系统
            new("CIRCULATORY SYS",  0.76f, 0.18f,  1),   // 循环系统
            new("RIGHT ARM",        0.76f, 0.36f,  3),   // 右臂
            new("SKELETON",         0.76f, 0.50f,  6),   // 骨骼
            new("RIGHT LEG",        0.76f, 0.68f,  8),   // 右腿
            new("NERVOUS SYSTEM",   0.76f, 0.82f,  10),  // 神经系统
        ];

        private readonly struct CyberwareSlotDef(string label, float xRatio, float yRatio, int nodeIndex)
        {
            public readonly string Label = label;
            public readonly float XRatio = xRatio;
            public readonly float YRatio = yRatio;
            public readonly int NodeIndex = nodeIndex;
        }

        #endregion

        #region 字段

        private bool isOpen;
        private float openProgress;
        private float globalTimer;
        private float scanLinePhase;
        private float glitchTimer;
        private float breathePhase;
        private float dataStreamPhase;
        private int hoveredSlot = -1;
        private int selectedSlot = -1;

        // 每个槽位的悬停动画
        private readonly float[] slotHoverAnim = new float[12];
        // 人体节点脉冲相位
        private readonly float[] nodePulsePhase = new float[11];
        // 数据粒子
        private readonly List<DataParticle> dataParticles = [];
        private int particleSpawnTimer;
        // 故障效果
        private float glitchIntensity;
        private float nextGlitchTime;
        // 面板位置缓存
        private Rectangle panelRect;
        private Vector2 panelCenter;
        private Vector2 bodyOrigin;

        #endregion

        #region 属性

        public static CyberwareUI Instance => UIHandleLoader.GetUIHandleOfType<CyberwareUI>();

        public override bool Active => isOpen || openProgress > 0.01f;

        #endregion

        #region 公共接口

        /// <summary>
        /// 打开义体界面
        /// </summary>
        public void Open() {
            if (!isOpen) {
                isOpen = true;
                glitchIntensity = 0.8f;
            }
        }

        /// <summary>
        /// 关闭义体界面
        /// </summary>
        public void Close() {
            if (isOpen) {
                isOpen = false;
                glitchIntensity = 0.4f;
            }
        }

        /// <summary>
        /// 切换义体界面开关
        /// </summary>
        public void Toggle() {
            if (isOpen) Close();
            else Open();
        }

        #endregion

        #region 更新

        public override void Update() {
            // 面板开关动画
            float targetProgress = isOpen ? 1f : 0f;
            openProgress += (targetProgress - openProgress) * 0.12f;
            if (!isOpen && openProgress < 0.01f) openProgress = 0f;
            if (openProgress < 0.01f) return;

            // 全局计时器
            globalTimer += 0.016f;
            scanLinePhase += 0.025f;
            breathePhase += 0.02f;
            dataStreamPhase += 0.03f;
            if (scanLinePhase > MathHelper.TwoPi) scanLinePhase -= MathHelper.TwoPi;
            if (breathePhase > MathHelper.TwoPi) breathePhase -= MathHelper.TwoPi;
            if (dataStreamPhase > MathHelper.TwoPi) dataStreamPhase -= MathHelper.TwoPi;

            // 故障效果衰减
            if (glitchIntensity > 0) glitchIntensity -= 0.02f;
            glitchTimer += 0.016f;
            if (glitchTimer > nextGlitchTime) {
                glitchTimer = 0;
                nextGlitchTime = 2f + Main.rand.NextFloat(4f);
                glitchIntensity = MathHelper.Clamp(0.15f + Main.rand.NextFloat(0.2f), 0, 1);
            }

            // 节点脉冲
            for (int i = 0; i < nodePulsePhase.Length; i++) {
                nodePulsePhase[i] += 0.03f + i * 0.004f;
                if (nodePulsePhase[i] > MathHelper.TwoPi) nodePulsePhase[i] -= MathHelper.TwoPi;
            }

            // 计算面板布局
            float easedProgress = CWRUtils.EaseOutCubic(Math.Clamp(openProgress, 0, 1));
            float panelW = PanelWidth * easedProgress;
            float panelH = PanelHeight * easedProgress;
            panelCenter = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
            panelRect = new Rectangle(
                (int)(panelCenter.X - panelW / 2f),
                (int)(panelCenter.Y - panelH / 2f),
                (int)panelW, (int)panelH
            );

            // 人体中心位置
            bodyOrigin = panelCenter + new Vector2(0, 5);

            // 悬停检测
            UpdateHovering();
            UpdateSlotAnimations();
            UpdateDataParticles();

            // 拦截游戏输入
            if (isOpen && panelRect.Contains(Main.mouseX, Main.mouseY)) {
                player.mouseInterface = true;
            }
        }

        private void UpdateHovering() {
            hoveredSlot = -1;
            Vector2 mouse = new(Main.mouseX, Main.mouseY);

            for (int i = 0; i < SlotDefinitions.Length; i++) {
                Rectangle slotRect = GetSlotRect(i);
                if (slotRect.Contains((int)mouse.X, (int)mouse.Y)) {
                    hoveredSlot = i;
                    player.mouseInterface = true;
                    break;
                }
            }

            // 点击选择
            if (hoveredSlot >= 0 && Main.mouseLeft && Main.mouseLeftRelease) {
                selectedSlot = hoveredSlot == selectedSlot ? -1 : hoveredSlot;
                glitchIntensity = 0.3f;
            }
        }

        private void UpdateSlotAnimations() {
            for (int i = 0; i < slotHoverAnim.Length; i++) {
                float target = i == hoveredSlot ? 1f : (i == selectedSlot ? 0.6f : 0f);
                slotHoverAnim[i] += (target - slotHoverAnim[i]) * 0.15f;
            }
        }

        private void UpdateDataParticles() {
            particleSpawnTimer++;
            if (particleSpawnTimer >= 6 && dataParticles.Count < 50 && openProgress > 0.5f) {
                particleSpawnTimer = 0;
                // 在人体周围生成数据流粒子
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist = 20f + Main.rand.NextFloat(80f);
                Vector2 pos = bodyOrigin + angle.ToRotationVector2() * dist;
                Vector2 vel = new(Main.rand.NextFloat(-0.3f, 0.3f), -0.5f - Main.rand.NextFloat(0.5f));
                Color c = Main.rand.NextBool(3) ? ColorAccent : (Main.rand.NextBool() ? ColorAccentGold : ColorAccentCyan);
                dataParticles.Add(new DataParticle(pos, vel, c * 0.6f));
            }

            for (int i = dataParticles.Count - 1; i >= 0; i--) {
                if (dataParticles[i].Update()) {
                    dataParticles.RemoveAt(i);
                }
            }
        }

        private Rectangle GetSlotRect(int index) {
            var def = SlotDefinitions[index];
            float slotW = PanelWidth * 0.20f;
            float slotH = SlotSize;
            int x = panelRect.X + (int)(def.XRatio * panelRect.Width);
            int y = panelRect.Y + (int)(def.YRatio * panelRect.Height);
            return new Rectangle(x, y, (int)slotW, (int)slotH);
        }

        #endregion

        #region 绘制

        public override void Draw(SpriteBatch spriteBatch) {
            if (openProgress < 0.01f) return;
            float alpha = Math.Clamp(openProgress, 0, 1);
            float easedAlpha = CWRUtils.EaseOutCubic(alpha);

            // 全屏暗化
            DrawFullScreenDim(spriteBatch, easedAlpha);
            // 主面板
            DrawPanel(spriteBatch, easedAlpha);
            // 网格背景
            DrawGridBackground(spriteBatch, easedAlpha);
            // 扫描线
            DrawScanLines(spriteBatch, easedAlpha);
            // 人体
            DrawPixelBody(spriteBatch, easedAlpha);
            // 人体节点
            DrawCyberNodes(spriteBatch, easedAlpha);
            // 连接线
            DrawSlotConnectors(spriteBatch, easedAlpha);
            // 义体槽位
            DrawSlots(spriteBatch, easedAlpha);
            // 标题和装饰
            DrawTitleAndDecor(spriteBatch, easedAlpha);
            // 数据粒子
            DrawDataParticles(spriteBatch, easedAlpha);
            // 故障效果
            DrawGlitchEffect(spriteBatch, easedAlpha);
        }

        /// <summary>全屏暗化遮罩</summary>
        private void DrawFullScreenDim(SpriteBatch sb, float alpha) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;
            sb.Draw(px, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                new Rectangle(0, 0, 1, 1), Color.Black * (alpha * 0.65f));
        }

        /// <summary>主面板背景和边框</summary>
        private void DrawPanel(SpriteBatch sb, float alpha) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            // 面板背景
            sb.Draw(px, panelRect, new Rectangle(0, 0, 1, 1), ColorBgPanel * (alpha * 0.95f));

            // 面板外发光
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null) {
                Color panelGlow = ColorAccent * (alpha * 0.06f);
                panelGlow.A = 0;
                sb.Draw(glow, panelCenter, null, panelGlow, 0, glow.Size() / 2,
                    new Vector2(panelRect.Width / 50f, panelRect.Height / 50f), SpriteEffects.None, 0);
            }

            // 面板边框 - 上
            float borderPulse = MathF.Sin(globalTimer * 2f) * 0.15f + 0.85f;
            Color topBorder = ColorAccent * (alpha * 0.8f * borderPulse);
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, 2),
                new Rectangle(0, 0, 1, 1), topBorder);
            // 下
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Bottom - 1, panelRect.Width, 1),
                new Rectangle(0, 0, 1, 1), ColorBorder * (alpha * 0.6f));
            // 左
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Y, 1, panelRect.Height),
                new Rectangle(0, 0, 1, 1), ColorBorder * (alpha * 0.5f));
            // 右
            sb.Draw(px, new Rectangle(panelRect.Right - 1, panelRect.Y, 1, panelRect.Height),
                new Rectangle(0, 0, 1, 1), ColorBorder * (alpha * 0.5f));

            // 上边框角落装饰 - 左上
            Color cornerColor = ColorAccent * (alpha * 0.9f);
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Y, 20, 2), new Rectangle(0, 0, 1, 1), cornerColor);
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Y, 2, 16), new Rectangle(0, 0, 1, 1), cornerColor);
            // 右上
            sb.Draw(px, new Rectangle(panelRect.Right - 20, panelRect.Y, 20, 2), new Rectangle(0, 0, 1, 1), cornerColor);
            sb.Draw(px, new Rectangle(panelRect.Right - 2, panelRect.Y, 2, 16), new Rectangle(0, 0, 1, 1), cornerColor);
            // 左下
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Bottom - 2, 20, 2), new Rectangle(0, 0, 1, 1), cornerColor * 0.6f);
            sb.Draw(px, new Rectangle(panelRect.X, panelRect.Bottom - 16, 2, 16), new Rectangle(0, 0, 1, 1), cornerColor * 0.6f);
            // 右下
            sb.Draw(px, new Rectangle(panelRect.Right - 20, panelRect.Bottom - 2, 20, 2), new Rectangle(0, 0, 1, 1), cornerColor * 0.6f);
            sb.Draw(px, new Rectangle(panelRect.Right - 2, panelRect.Bottom - 16, 2, 16), new Rectangle(0, 0, 1, 1), cornerColor * 0.6f);
        }

        /// <summary>网格背景线</summary>
        private void DrawGridBackground(SpriteBatch sb, float alpha) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            Color gridColor = ColorGridLine * (alpha * 0.4f);
            float spacing = 24f;

            // 垂直线
            for (float x = panelRect.X + spacing; x < panelRect.Right; x += spacing) {
                sb.Draw(px, new Rectangle((int)x, panelRect.Y, 1, panelRect.Height),
                    new Rectangle(0, 0, 1, 1), gridColor);
            }
            // 水平线
            for (float y = panelRect.Y + spacing; y < panelRect.Bottom; y += spacing) {
                sb.Draw(px, new Rectangle(panelRect.X, (int)y, panelRect.Width, 1),
                    new Rectangle(0, 0, 1, 1), gridColor);
            }
        }

        /// <summary>扫描线效果</summary>
        private void DrawScanLines(SpriteBatch sb, float alpha) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            // 主扫描线 - 水平移动
            float scanY = panelRect.Y + (MathF.Sin(scanLinePhase) * 0.5f + 0.5f) * panelRect.Height;
            Color scanColor = ColorAccent * (alpha * 0.08f);
            sb.Draw(px, new Rectangle(panelRect.X, (int)scanY, panelRect.Width, 2),
                new Rectangle(0, 0, 1, 1), scanColor);

            // 扫描线尾迹上方渐变
            for (int i = 1; i <= 8; i++) {
                float fade = 1f - i / 8f;
                sb.Draw(px, new Rectangle(panelRect.X, (int)scanY - i * 3, panelRect.Width, 2),
                    new Rectangle(0, 0, 1, 1), scanColor * (fade * 0.4f));
            }

            // CRT扫描线纹理 (每隔几行画半透明线)
            for (int y = panelRect.Y; y < panelRect.Bottom; y += 3) {
                sb.Draw(px, new Rectangle(panelRect.X, y, panelRect.Width, 1),
                    new Rectangle(0, 0, 1, 1), Color.Black * (alpha * 0.06f));
            }
        }

        /// <summary>程序化像素人体绘制</summary>
        private void DrawPixelBody(SpriteBatch sb, float alpha) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            float breathe = MathF.Sin(breathePhase) * 0.5f;
            float s = BodyPixelScale;

            // 人体中心偏移量（人体网格为32x48，居中到bodyOrigin）
            Vector2 bodyOffset = bodyOrigin - new Vector2(16 * s, 24 * s);

            // 绘制人体填充区域（大致的身体区域填充）
            DrawBodyFill(sb, px, bodyOffset, s, alpha, breathe);

            // 绘制内部结构线（骨骼/电路）
            Color innerColor = ColorBodyInner * (alpha * 0.35f);
            float innerPulse = MathF.Sin(globalTimer * 1.5f) * 0.15f + 0.85f;
            innerColor *= innerPulse;
            for (int i = 0; i < BodyInnerLines.GetLength(0); i++) {
                Vector2 start = bodyOffset + new Vector2(BodyInnerLines[i, 0] * s, BodyInnerLines[i, 1] * s + breathe);
                Vector2 end = bodyOffset + new Vector2(BodyInnerLines[i, 2] * s, BodyInnerLines[i, 3] * s + breathe);
                DrawLine(sb, px, start, end, 1f, innerColor);
            }

            // 绘制人体轮廓
            Color outlineColor = ColorBodyOutline * (alpha * 0.7f);
            for (int i = 0; i < BodyOutlineSegments.GetLength(0); i++) {
                Vector2 start = bodyOffset + new Vector2(BodyOutlineSegments[i, 0] * s, BodyOutlineSegments[i, 1] * s + breathe);
                Vector2 end = bodyOffset + new Vector2(BodyOutlineSegments[i, 2] * s, BodyOutlineSegments[i, 3] * s + breathe);
                DrawLine(sb, px, start, end, 2f, outlineColor);
            }

            // 人体边缘发光
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null) {
                float glowPulse = MathF.Sin(globalTimer * 1.2f) * 0.1f + 0.9f;
                Color bodyGlow = ColorAccent * (alpha * 0.08f * glowPulse);
                bodyGlow.A = 0;
                sb.Draw(glow, bodyOrigin + new Vector2(0, breathe),
                    null, bodyGlow, 0, glow.Size() / 2, 4f, SpriteEffects.None, 0);
            }
        }

        /// <summary>填充人体主要区域</summary>
        private void DrawBodyFill(SpriteBatch sb, Texture2D px, Vector2 offset, float s, float alpha, float breathe) {
            Color fillColor = ColorBodyFill * (alpha * 0.6f);

            // 头部填充
            FillRect(sb, px, offset, s, 13, 2, 7, 5, fillColor, breathe);
            // 颈部
            FillRect(sb, px, offset, s, 14, 8, 4, 2, fillColor, breathe);
            // 躯干
            FillRect(sb, px, offset, s, 10, 10, 12, 18, fillColor, breathe);
            // 左臂
            FillRect(sb, px, offset, s, 5, 13, 3, 13, fillColor, breathe);
            // 右臂
            FillRect(sb, px, offset, s, 24, 13, 3, 13, fillColor, breathe);
            // 左腿
            FillRect(sb, px, offset, s, 10, 29, 5, 17, fillColor, breathe);
            // 右腿
            FillRect(sb, px, offset, s, 17, 29, 5, 17, fillColor, breathe);
        }

        /// <summary>在像素网格上填充矩形</summary>
        private static void FillRect(SpriteBatch sb, Texture2D px, Vector2 offset, float s,
            int gx, int gy, int gw, int gh, Color color, float breathe) {
            Rectangle rect = new(
                (int)(offset.X + gx * s),
                (int)(offset.Y + gy * s + breathe),
                (int)(gw * s),
                (int)(gh * s)
            );
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), color);
        }

        /// <summary>绘制义体植入节点</summary>
        private void DrawCyberNodes(SpriteBatch sb, float alpha) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (px == null) return;

            float s = BodyPixelScale;
            Vector2 bodyOffset = bodyOrigin - new Vector2(16 * s, 24 * s);
            float breathe = MathF.Sin(breathePhase) * 0.5f;

            for (int i = 0; i < CyberNodes.GetLength(0); i++) {
                Vector2 nodePos = bodyOffset + new Vector2(CyberNodes[i, 0] * s, CyberNodes[i, 1] * s + breathe);
                float pulse = MathF.Sin(nodePulsePhase[i]) * 0.3f + 0.7f;

                // 检查是否有槽位连接到此节点
                bool isLinked = false;
                bool isSelectedNode = false;
                for (int j = 0; j < SlotDefinitions.Length; j++) {
                    if (SlotDefinitions[j].NodeIndex == i) {
                        isLinked = true;
                        if (j == selectedSlot || j == hoveredSlot) isSelectedNode = true;
                    }
                }

                Color nodeColor = isSelectedNode ? ColorAccentGold :
                                  isLinked ? ColorAccent : ColorAccentCyan;
                nodeColor *= alpha * pulse;

                // 节点方块
                float nodeSize = isSelectedNode ? 5f : 3f;
                sb.Draw(px, nodePos, new Rectangle(0, 0, 1, 1), nodeColor,
                    MathHelper.PiOver4, new Vector2(0.5f), new Vector2(nodeSize), SpriteEffects.None, 0f);

                // 节点光晕
                if (glow != null) {
                    Color nodeGlow = nodeColor * 0.4f;
                    nodeGlow.A = 0;
                    sb.Draw(glow, nodePos, null, nodeGlow, 0, glow.Size() / 2,
                        0.06f + (isSelectedNode ? 0.04f : 0f), SpriteEffects.None, 0);
                }
            }
        }

        /// <summary>绘制槽位到人体节点的连接线</summary>
        private void DrawSlotConnectors(SpriteBatch sb, float alpha) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (px == null) return;

            float s = BodyPixelScale;
            Vector2 bodyOffset = bodyOrigin - new Vector2(16 * s, 24 * s);
            float breathe = MathF.Sin(breathePhase) * 0.5f;

            for (int i = 0; i < SlotDefinitions.Length; i++) {
                var def = SlotDefinitions[i];
                Rectangle slotRect = GetSlotRect(i);
                bool isLeft = def.XRatio < 0.5f;

                // 连接起点（槽位边缘）
                Vector2 slotEdge = isLeft
                    ? new Vector2(slotRect.Right, slotRect.Center.Y)
                    : new Vector2(slotRect.Left, slotRect.Center.Y);

                // 连接终点（人体节点）
                int ni = def.NodeIndex;
                Vector2 nodePos = bodyOffset + new Vector2(CyberNodes[ni, 0] * s, CyberNodes[ni, 1] * s + breathe);

                bool isActive = i == hoveredSlot || i == selectedSlot;
                float lineAlpha = isActive ? 0.6f : 0.15f;
                Color lineColor = isActive ? ColorAccent : ColorConnector;
                lineColor *= alpha * lineAlpha;

                // 折线连接: 水平 -> 垂直 -> 水平到节点
                float midX = isLeft
                    ? slotEdge.X + (nodePos.X - slotEdge.X) * 0.4f
                    : slotEdge.X - (slotEdge.X - nodePos.X) * 0.4f;

                Vector2 p1 = slotEdge;
                Vector2 p2 = new(midX, slotEdge.Y);
                Vector2 p3 = new(midX, nodePos.Y);
                Vector2 p4 = nodePos;

                DrawLine(sb, px, p1, p2, 1f, lineColor);
                DrawLine(sb, px, p2, p3, 1f, lineColor);
                DrawLine(sb, px, p3, p4, 1f, lineColor);

                // 活跃连接线上的流动光点
                if (isActive && glow != null) {
                    float t = (dataStreamPhase / MathHelper.TwoPi) % 1f;
                    Vector2 flowPos = EvaluatePolyline(t, p1, p2, p3, p4);
                    Color flowColor = ColorAccent * (alpha * 0.5f);
                    flowColor.A = 0;
                    sb.Draw(glow, flowPos, null, flowColor, 0, glow.Size() / 2, 0.05f, SpriteEffects.None, 0);
                }

                // 连接点装饰 (折线拐角小方块)
                Color dotColor = lineColor * 1.5f;
                sb.Draw(px, p2, new Rectangle(0, 0, 1, 1), dotColor, 0, new Vector2(0.5f), 2f, SpriteEffects.None, 0);
                sb.Draw(px, p3, new Rectangle(0, 0, 1, 1), dotColor, 0, new Vector2(0.5f), 2f, SpriteEffects.None, 0);
            }
        }

        /// <summary>计算折线上某一比例位置的坐标</summary>
        private static Vector2 EvaluatePolyline(float t, Vector2 a, Vector2 b, Vector2 c, Vector2 d) {
            float dAB = Vector2.Distance(a, b);
            float dBC = Vector2.Distance(b, c);
            float dCD = Vector2.Distance(c, d);
            float total = dAB + dBC + dCD;
            float dist = t * total;

            if (dist <= dAB) return Vector2.Lerp(a, b, dist / dAB);
            dist -= dAB;
            if (dist <= dBC) return Vector2.Lerp(b, c, dist / dBC);
            dist -= dBC;
            return Vector2.Lerp(c, d, Math.Clamp(dist / dCD, 0, 1));
        }

        /// <summary>绘制义体槽位</summary>
        private void DrawSlots(SpriteBatch sb, float alpha) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            for (int i = 0; i < SlotDefinitions.Length; i++) {
                Rectangle rect = GetSlotRect(i);
                var def = SlotDefinitions[i];
                float hover = slotHoverAnim[i];
                bool isSelected = i == selectedSlot;
                bool isHovered = i == hoveredSlot;

                // 槽位背景
                Color bgColor = Color.Lerp(ColorSlotEmpty, ColorBgDark, 0.3f) * (alpha * 0.9f);
                if (isHovered) bgColor = Color.Lerp(bgColor, ColorAccent, 0.08f * hover);
                if (isSelected) bgColor = Color.Lerp(bgColor, ColorAccent, 0.12f);
                sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), bgColor);

                // 槽位边框
                Color borderColor = isSelected ? ColorAccent :
                                   isHovered ? Color.Lerp(ColorSlotBorder, ColorAccent, hover * 0.6f) :
                                   ColorSlotBorder;
                borderColor *= alpha;
                // 上
                sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1), new Rectangle(0, 0, 1, 1), borderColor);
                // 下
                sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), new Rectangle(0, 0, 1, 1), borderColor * 0.6f);
                // 左
                sb.Draw(px, new Rectangle(rect.X, rect.Y, 1, rect.Height), new Rectangle(0, 0, 1, 1), borderColor * 0.7f);
                // 右
                sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), new Rectangle(0, 0, 1, 1), borderColor * 0.7f);

                // 选中时左侧/右侧强调条
                if (isSelected || hover > 0.01f) {
                    float barAlpha = isSelected ? 0.9f : hover * 0.5f;
                    Color barColor = ColorAccent * (alpha * barAlpha);
                    bool isLeft = def.XRatio < 0.5f;
                    if (isLeft) {
                        sb.Draw(px, new Rectangle(rect.X, rect.Y, 3, rect.Height), new Rectangle(0, 0, 1, 1), barColor);
                    }
                    else {
                        sb.Draw(px, new Rectangle(rect.Right - 3, rect.Y, 3, rect.Height), new Rectangle(0, 0, 1, 1), barColor);
                    }
                }

                // 槽位标签文字
                Color labelColor = isSelected ? ColorAccent :
                                  isHovered ? Color.Lerp(ColorTextDim, ColorTextBright, hover) :
                                  ColorTextDim;
                labelColor *= alpha;
                float textScale = 0.32f;
                Vector2 textPos = new(rect.X + SlotPadding + (def.XRatio < 0.5f ? 4 : 0), rect.Y + 4);
                Utils.DrawBorderString(sb, def.Label, textPos, labelColor, textScale);

                // 空槽位提示
                string statusText = isSelected ? "-- SELECTED --" : "EMPTY";
                Color statusColor = isSelected ? ColorAccentGold : ColorTextDim;
                statusColor *= alpha * 0.6f;
                Utils.DrawBorderString(sb, statusText, new Vector2(rect.X + SlotPadding + (def.XRatio < 0.5f ? 4 : 0), rect.Y + 18), statusColor, 0.28f);
            }
        }

        /// <summary>标题和装饰元素</summary>
        private void DrawTitleAndDecor(SpriteBatch sb, float alpha) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            // 标题
            string title = "CYBERWARE";
            Vector2 titleSize = FontAssets.MouseText.Value.MeasureString(title) * 0.55f;
            Vector2 titlePos = new(panelCenter.X - titleSize.X / 2f, panelRect.Y + 8);

            // 标题发光底色
            Color titleGlowBg = ColorAccent * (alpha * 0.1f);
            sb.Draw(px, new Rectangle((int)(titlePos.X - 10), (int)titlePos.Y - 2, (int)(titleSize.X + 20), (int)(titleSize.Y + 6)),
                new Rectangle(0, 0, 1, 1), titleGlowBg);

            Color titleColor = ColorAccent * (alpha * 0.95f);
            Utils.DrawBorderString(sb, title, titlePos, titleColor, 0.55f);

            // 标题下方分割线
            float lineY = titlePos.Y + titleSize.Y + 6;
            float lineW = PanelWidth * 0.85f;
            Color divColor = ColorAccent * (alpha * 0.3f);
            sb.Draw(px, new Rectangle((int)(panelCenter.X - lineW / 2f), (int)lineY, (int)lineW, 1),
                new Rectangle(0, 0, 1, 1), divColor);

            // 版本号装饰（右上角）
            string version = "v2.077";
            Color verColor = ColorTextDim * (alpha * 0.5f);
            Utils.DrawBorderString(sb, version, new Vector2(panelRect.Right - 60, panelRect.Y + 10), verColor, 0.28f);

            // 底部状态栏
            float bottomY = panelRect.Bottom - 18;
            sb.Draw(px, new Rectangle(panelRect.X + 4, (int)bottomY - 2, panelRect.Width - 8, 1),
                new Rectangle(0, 0, 1, 1), ColorBorder * (alpha * 0.4f));

            // 状态文字
            string status = "SYSTEM STATUS: OPERATIONAL";
            float statusPulse = MathF.Sin(globalTimer * 3f) > 0 ? 1f : 0.4f;
            Color statusDot = new Color(50, 255, 80) * (alpha * statusPulse);
            sb.Draw(px, new Vector2(panelRect.X + 10, bottomY + 2), new Rectangle(0, 0, 1, 1),
                statusDot, 0, Vector2.Zero, 4f, SpriteEffects.None, 0);
            Utils.DrawBorderString(sb, status, new Vector2(panelRect.X + 20, bottomY - 2), ColorTextDim * alpha, 0.26f);

            // 右下角数据流装饰
            string dataTag = $"NET::0x{((int)(globalTimer * 100) % 0xFFFF):X4}";
            Utils.DrawBorderString(sb, dataTag, new Vector2(panelRect.Right - 100, bottomY - 2), ColorAccentCyan * (alpha * 0.35f), 0.24f);
        }

        /// <summary>数据粒子绘制</summary>
        private void DrawDataParticles(SpriteBatch sb, float alpha) {
            foreach (var p in dataParticles) {
                p.Draw(sb, alpha);
            }
        }

        /// <summary>随机故障/干扰效果</summary>
        private void DrawGlitchEffect(SpriteBatch sb, float alpha) {
            if (glitchIntensity <= 0.01f) return;
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            float intensity = glitchIntensity * alpha;

            // 随机水平位移色块
            int glitchLines = (int)(3 + intensity * 8);
            for (int i = 0; i < glitchLines; i++) {
                int y = panelRect.Y + Main.rand.Next(panelRect.Height);
                int h = 1 + Main.rand.Next(3);
                int offsetX = Main.rand.Next(-8, 9);
                Color gc = Main.rand.NextBool() ? ColorAccent : ColorAccentCyan;
                gc *= intensity * 0.3f;
                sb.Draw(px, new Rectangle(panelRect.X + offsetX, y, panelRect.Width, h),
                    new Rectangle(0, 0, 1, 1), gc);
            }
        }

        #endregion

        #region 辅助绘制

        private static void DrawLine(SpriteBatch sb, Texture2D px, Vector2 start, Vector2 end, float thickness, Color color) {
            Vector2 diff = end - start;
            float length = diff.Length();
            if (length < 1f) return;
            sb.Draw(px, start, new Rectangle(0, 0, 1, 1), color, diff.ToRotation(),
                Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        #endregion
    }

    /// <summary>
    /// 数据流粒子 - 在人体周围飘动的小光点，营造数据传输氛围
    /// </summary>
    internal class DataParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public Color BaseColor;
        private readonly float phase;

        public DataParticle(Vector2 pos, Vector2 vel, Color color) {
            Position = pos;
            Velocity = vel;
            BaseColor = color;
            MaxLife = 60 + Main.rand.Next(40);
            Life = MaxLife;
            phase = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public bool Update() {
            Life--;
            if (Life <= 0) return true;
            Position += Velocity;
            Position.X += MathF.Sin(phase + Life * 0.08f) * 0.15f;
            return false;
        }

        public void Draw(SpriteBatch sb, float alpha) {
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow == null) return;

            float progress = 1f - Life / MaxLife;
            float fadeAlpha = progress < 0.2f ? progress / 0.2f : (progress > 0.7f ? (1f - progress) / 0.3f : 1f);
            float scale = 0.04f + MathF.Sin(progress * MathHelper.Pi) * 0.03f;
            Color drawColor = BaseColor * (alpha * fadeAlpha * 0.5f);
            drawColor.A = 0;
            sb.Draw(glow, Position, null, drawColor, 0, glow.Size() / 2, scale, SpriteEffects.None, 0);
        }
    }
}
