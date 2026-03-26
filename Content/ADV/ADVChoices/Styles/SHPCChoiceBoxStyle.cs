using CalamityOverhaul.Content.ADV.UIEffect;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.ADV.ADVChoices.Styles
{
    /// <summary>
    /// SHPC赛博女仆风格选项框：不对称蓝紫重边框+碳纤维对角线纹理+二进制矩阵底纹<br/>
    /// +四角状态文字+右侧刻度尺+左侧数据流线+四角星/褶边角饰+领结前缀<br/>
    /// 与SHPCDialogueBox保持统一的「赛博女仆」视觉语言
    /// </summary>
    internal class SHPCChoiceBoxStyle : IChoiceBoxStyle
    {
        //动画计时器
        private float neonPulseTimer;
        private float holoFlicker;
        private float dataFlowTimer;
        private float sweepTimer;
        private float starSpinTimer;

        //左侧数据流线相位
        private readonly float[] dataLinePhases = new float[3];

        //四角状态文字
        private readonly string[] cornerStatus = ["LINK.OK", "SYS:RDY", "v2.07b", "SYNC.."];
        private int statusUpdateClock;
        private static readonly string[] StatusPool = [
            "MAID.OK", "SYS:RDY", "LINK.UP", "ACT:ON", "v2.07b",
            "NRG:98%", "SYNC..", "CORE:A+", "NET.OK", "STB:Hi",
            "IO:PASS", "CHK:OK", "MOD:RUN", "BUF:CLR", "SIG:99"
        ];

        //粒子系统
        private readonly List<NeonMaidPRT> neonParticles = [];
        private int neonParticleSpawnTimer;
        private readonly List<CircuitNodePRT> circuitNodes = [];
        private int circuitNodeSpawnTimer;
        private const float SideMargin = 22f;

        public void Update(Rectangle panelRect, bool active, bool closing) {
            Advance(ref neonPulseTimer, 0.028f);
            Advance(ref holoFlicker, 0.11f);
            Advance(ref dataFlowTimer, 0.018f);
            sweepTimer = (sweepTimer + 0.006f) % 1f;
            starSpinTimer += 0.012f;
            if (starSpinTimer > MathHelper.TwoPi) starSpinTimer -= MathHelper.TwoPi;

            //左侧数据流线相位
            for (int i = 0; i < dataLinePhases.Length; i++)
                dataLinePhases[i] = (dataLinePhases[i] + 0.015f + i * 0.004f) % 1f;

            //四角状态文字刷新（每50帧）
            statusUpdateClock++;
            if (statusUpdateClock >= 50) {
                statusUpdateClock = 0;
                for (int i = 0; i < cornerStatus.Length; i++)
                    cornerStatus[i] = StatusPool[Main.rand.Next(StatusPool.Length)];
            }

            //霓虹粒子（间隔24帧，上限10个）
            float scaleW = Main.UIScale;
            neonParticleSpawnTimer++;
            if (active && !closing && neonParticleSpawnTimer >= 24 && neonParticles.Count < 10) {
                neonParticleSpawnTimer = 0;
                float left = panelRect.X + SideMargin * scaleW;
                float right = panelRect.Right - SideMargin * scaleW;
                Vector2 p = new(Main.rand.NextFloat(left, right),
                    panelRect.Y + Main.rand.NextFloat(30f, panelRect.Height - 30f));
                neonParticles.Add(new NeonMaidPRT(p));
            }
            for (int i = neonParticles.Count - 1; i >= 0; i--) {
                Vector2 panelPos = new(panelRect.X, panelRect.Y);
                Vector2 panelSize = new(panelRect.Width, panelRect.Height);
                if (neonParticles[i].Update(panelPos, panelSize))
                    neonParticles.RemoveAt(i);
            }

            //电路节点（间隔34帧，上限5个）
            circuitNodeSpawnTimer++;
            if (active && !closing && circuitNodeSpawnTimer >= 34 && circuitNodes.Count < 5) {
                circuitNodeSpawnTimer = 0;
                float left = panelRect.X + SideMargin * scaleW;
                float right = panelRect.Right - SideMargin * scaleW;
                circuitNodes.Add(new CircuitNodePRT(
                    new Vector2(Main.rand.NextFloat(left, right),
                                panelRect.Y + Main.rand.NextFloat(30f, panelRect.Height - 30f))));
            }
            for (int i = circuitNodes.Count - 1; i >= 0; i--) {
                Vector2 panelPos = new(panelRect.X, panelRect.Y);
                Vector2 panelSize = new(panelRect.Width, panelRect.Height);
                if (circuitNodes[i].Update(panelPos, panelSize))
                    circuitNodes.RemoveAt(i);
            }
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle panelRect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //多层扩散阴影（偏紫黑色调，加厚）
            for (int d = 12; d >= 1; d--) {
                Rectangle s = panelRect;
                s.Inflate(d, d);
                s.Offset(5, 7);
                spriteBatch.Draw(px, s, new Rectangle(0, 0, 1, 1),
                    new Color(8, 4, 16) * (alpha * 0.065f * (12f - d) / 12f));
            }

            //深紫黑渐变背景+碳纤维对角线+二进制矩阵+内嵌面板+全息叠层
            DrawCyberMaidBackground(spriteBatch, panelRect, alpha);

            //左侧霓虹数据流线
            DrawLeftDataLines(spriteBatch, panelRect, alpha);

            //扫描线（多条拖影）
            float scanY = panelRect.Y + sweepTimer * panelRect.Height;
            for (int row = 0; row <= 4; row++) {
                float iy = scanY + row * 1.5f;
                if (iy < panelRect.Y || iy > panelRect.Bottom) continue;
                float fade = 1f - row * 0.22f;
                spriteBatch.Draw(px,
                    new Rectangle(panelRect.X + 6, (int)iy, panelRect.Width - 12, 1),
                    new Rectangle(0, 0, 1, 1),
                    new Color(60, 120, 220) * (alpha * 0.14f * fade));
            }

            //不对称重边框
            DrawNeonFrame(spriteBatch, panelRect, alpha);

            //右侧装饰条纹
            DrawRightOrnament(spriteBatch, panelRect, alpha);

            //左上角四角星纹样
            DrawCornerStarSymbol(spriteBatch, panelRect, alpha);

            //其余三角的金属褶边角饰
            DrawMetallicFrillCorners(spriteBatch, panelRect, alpha);

            //四角状态文字
            DrawCornerStatusText(spriteBatch, panelRect, alpha);

            //粒子
            foreach (var node in circuitNodes)
                node.Draw(spriteBatch, alpha * 0.6f);
            foreach (var particle in neonParticles)
                particle.Draw(spriteBatch, alpha * 0.6f);
        }

        public void DrawChoiceBackground(SpriteBatch spriteBatch, Rectangle choiceRect, bool enabled, float hoverProgress, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //选项背景（蓝紫色调）
            Color choiceBg = enabled
                ? Color.Lerp(new Color(6, 4, 18) * 0.3f, new Color(14, 10, 36) * 0.55f, hoverProgress)
                : new Color(8, 6, 14) * 0.15f;

            spriteBatch.Draw(px, choiceRect, new Rectangle(0, 0, 1, 1), choiceBg * alpha);

            Color neonColor = GetEdgeColor(alpha);
            if (enabled && hoverProgress > 0.01f) {
                DrawChoiceBorder(spriteBatch, choiceRect, neonColor * (hoverProgress * 0.6f));

                //悬停时左侧霓虹竖线指示
                DrawChoiceNeonIndicator(spriteBatch, choiceRect, neonColor, hoverProgress, alpha);

                //悬停时底部流光
                DrawChoiceHoverGlow(spriteBatch, choiceRect, hoverProgress, alpha);
            }
            else if (!enabled) {
                DrawChoiceBorder(spriteBatch, choiceRect, new Color(25, 20, 50) * (alpha * 0.2f));
            }
        }

        public Color GetEdgeColor(float alpha) {
            float pulse = MathF.Sin(neonPulseTimer * 1.2f) * 0.2f + 0.8f;
            return Color.Lerp(new Color(50, 140, 255), new Color(120, 80, 220), 0.3f) * (alpha * 0.85f * pulse);
        }

        public Color GetTextGlowColor(float alpha, float hoverProgress) {
            return GetEdgeColor(alpha);
        }

        public void DrawTitleDecoration(SpriteBatch spriteBatch, Vector2 titlePos, string title, float alpha) {
            //名字辉光晕（霓虹蓝紫色）
            Color nameGlow = new Color(80, 140, 255) * (alpha * 0.7f);
            for (int i = 0; i < 4; i++) {
                float a = MathHelper.TwoPi * i / 4f;
                Vector2 off = a.ToRotationVector2() * 1.8f;
                Utils.DrawBorderString(spriteBatch, title, titlePos + off, nameGlow * 0.5f, 0.95f);
            }

            //发光领结图标前缀
            float bowPulse = MathF.Sin(neonPulseTimer * 2f) * 0.2f + 0.8f;
            Color bowColor = new Color(100, 180, 255) * (alpha * 0.85f * bowPulse);
            Vector2 bowPos = titlePos - new Vector2(18f, -2f);
            Texture2D px = VaultAsset.placeholder2.Value;
            //中心结
            spriteBatch.Draw(px, bowPos, null, bowColor, 0f, new Vector2(0.5f),
                new Vector2(2f, 3f), SpriteEffects.None, 0f);
            //左翼
            spriteBatch.Draw(px, bowPos - new Vector2(3f, 0f), null, bowColor * 0.85f,
                MathHelper.PiOver4 * 0.3f, new Vector2(0.5f),
                new Vector2(4f, 2f), SpriteEffects.None, 0f);
            //右翼
            spriteBatch.Draw(px, bowPos + new Vector2(3f, 0f), null, bowColor * 0.85f,
                -MathHelper.PiOver4 * 0.3f, new Vector2(0.5f),
                new Vector2(4f, 2f), SpriteEffects.None, 0f);

            //名字下方霓虹渐变细线
            float nameW = Terraria.GameContent.FontAssets.MouseText.Value
                .MeasureString(title).X * 0.95f;
            int lineW = (int)(nameW * 0.7f);
            for (int seg = 0; seg < lineW; seg++) {
                float t = seg / (float)lineW;
                float bright = MathF.Sin(t * MathHelper.Pi) * 0.65f + 0.35f;
                Color lineC = Color.Lerp(new Color(60, 140, 255), new Color(140, 100, 230), t)
                    * (alpha * 0.5f * bright);
                spriteBatch.Draw(px, new Rectangle((int)titlePos.X + seg, (int)(titlePos.Y + 20f), 1, 1),
                    new Rectangle(0, 0, 1, 1), lineC);
            }
        }

        public void DrawDivider(SpriteBatch spriteBatch, Vector2 start, Vector2 end, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float len = end.X - start.X;
            if (len < 2f) return;

            //主实线（从蓝到紫渐变，1px）
            int segs = Math.Max(1, (int)(len / 8f));
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float segX = start.X + t * len;
                float segW = len / segs;
                float bright = MathF.Sin(t * MathHelper.Pi) * 0.5f + 0.5f;
                Color c = Color.Lerp(new Color(60, 140, 255), new Color(130, 90, 220), t)
                    * (alpha * 0.7f * bright);
                spriteBatch.Draw(px, new Rectangle((int)segX, (int)start.Y, (int)segW + 1, 1),
                    new Rectangle(0, 0, 1, 1), c);
            }

            //下方1px处的虚线装饰
            const int dashW = 4, gapW = 3;
            float flow = dataFlowTimer * 18f;
            float period = dashW + gapW;
            float x = start.X - (flow % period);
            while (x < end.X) {
                float segStart = Math.Max(x, start.X);
                float segEnd = Math.Min(x + dashW, end.X);
                if (segEnd > segStart) {
                    Color c = new Color(80, 100, 180) * (alpha * 0.25f);
                    spriteBatch.Draw(px, new Rectangle((int)segStart, (int)start.Y + 2, (int)(segEnd - segStart), 1),
                        new Rectangle(0, 0, 1, 1), c);
                }
                x += period;
            }
        }

        public void Reset() {
            neonPulseTimer = 0f;
            holoFlicker = 0f;
            dataFlowTimer = 0f;
            sweepTimer = 0f;
            starSpinTimer = 0f;
            statusUpdateClock = 0;
            neonParticles.Clear();
            circuitNodes.Clear();
            neonParticleSpawnTimer = 0;
            circuitNodeSpawnTimer = 0;
            for (int i = 0; i < dataLinePhases.Length; i++)
                dataLinePhases[i] = 0f;
            cornerStatus[0] = "LINK.OK";
            cornerStatus[1] = "SYS:RDY";
            cornerStatus[2] = "v2.07b";
            cornerStatus[3] = "SYNC..";
        }

        #region 工具方法

        private static void Advance(ref float t, float speed) {
            t += speed;
            if (t > MathHelper.TwoPi) t -= MathHelper.TwoPi;
        }

        /// <summary>
        /// 深紫黑渐变背景+碳纤维对角线纹理+二进制矩阵底纹+内嵌面板+全息叠层
        /// </summary>
        private void DrawCyberMaidBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //纵向渐变（深紫黑到深海蓝，32段，高不透明度）
            int segs = 32;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = rect.Y + (int)(t * rect.Height);
                int y2 = rect.Y + (int)(t2 * rect.Height);
                float pulse = MathF.Sin(neonPulseTimer * 0.45f + t * 1.8f) * 0.5f + 0.5f;

                Color topDark = new Color(8, 6, 18);
                Color botDark = new Color(6, 10, 24);
                Color mid = Color.Lerp(topDark, botDark, t);
                Color bright = Color.Lerp(new Color(16, 14, 34), new Color(12, 22, 42), t);
                Color c = Color.Lerp(mid, bright, pulse * 0.45f) * (alpha * 0.95f);

                sb.Draw(px, new Rectangle(rect.X, y1, rect.Width, Math.Max(1, y2 - y1)),
                    new Rectangle(0, 0, 1, 1), c);
            }

            //碳纤维对角线纹理（45°交叉细线）
            int dSpacing = 16;
            float dPhase = dataFlowTimer * 10f;
            Color diagColor = new Color(30, 50, 100) * (alpha * 0.04f);
            for (int col = -(rect.Height / dSpacing) - 1; col < (rect.Width / dSpacing) + 2; col++) {
                int ox = (int)(col * dSpacing + dPhase % dSpacing);
                for (int row = 0; row < rect.Height; row += 2) {
                    int px2 = rect.X + ox + row;
                    if (px2 >= rect.X && px2 < rect.Right)
                        sb.Draw(px, new Rectangle(px2, rect.Y + row, 1, 1), new Rectangle(0, 0, 1, 1), diagColor);
                    int px3 = rect.X + ox - row;
                    if (px3 >= rect.X && px3 < rect.Right)
                        sb.Draw(px, new Rectangle(px3, rect.Y + row, 1, 1), new Rectangle(0, 0, 1, 1), diagColor * 0.6f);
                }
            }

            //二进制矩阵底纹
            int gridSpacingX = 10;
            int gridSpacingY = 8;
            float matrixPhase = dataFlowTimer * 10f;
            Color matrixColor = new Color(50, 70, 140) * (alpha * 0.055f);
            for (int col = 0; col < rect.Width / gridSpacingX; col++) {
                int cx = rect.X + col * gridSpacingX + 5;
                if (cx >= rect.Right - 5) continue;
                float colPhase = matrixPhase + col * 0.7f;
                for (int row = 0; row < rect.Height / gridSpacingY; row++) {
                    int cy = rect.Y + row * gridSpacingY + 3;
                    if (cy >= rect.Bottom - 3) continue;
                    float hash = MathF.Sin(col * 13.7f + row * 7.3f + colPhase) * 0.5f + 0.5f;
                    if (hash > 0.5f) {
                        sb.Draw(px, new Rectangle(cx, cy, 1, 1),
                            new Rectangle(0, 0, 1, 1), matrixColor * hash);
                    }
                }
            }

            //内嵌面板效果（内缩6px的微弱亮边）
            Rectangle inset = rect;
            inset.Inflate(-6, -6);
            Color insetEdge = new Color(40, 70, 140) * (alpha * 0.12f);
            sb.Draw(px, new Rectangle(inset.X, inset.Y, inset.Width, 1), new Rectangle(0, 0, 1, 1), insetEdge);
            sb.Draw(px, new Rectangle(inset.X, inset.Bottom - 1, inset.Width, 1), new Rectangle(0, 0, 1, 1), insetEdge * 0.5f);
            sb.Draw(px, new Rectangle(inset.X, inset.Y, 1, inset.Height), new Rectangle(0, 0, 1, 1), insetEdge * 0.7f);
            sb.Draw(px, new Rectangle(inset.Right - 1, inset.Y, 1, inset.Height), new Rectangle(0, 0, 1, 1), insetEdge * 0.4f);

            //全息闪烁叠层
            float flicker = MathF.Sin(holoFlicker * 1.5f) * 0.5f + 0.5f;
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1),
                new Color(14, 10, 32) * (alpha * 0.2f * flicker));
        }

        /// <summary>
        /// 左侧数据流线（3条，加宽增亮+常驻底条）
        /// </summary>
        private void DrawLeftDataLines(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            int[] xOffsets = [8, 14, 21];
            int[] widths = [2, 1, 2];

            for (int lineIdx = 0; lineIdx < 3; lineIdx++) {
                int lx = rect.X + xOffsets[lineIdx];
                int lw = widths[lineIdx];
                float phase = dataLinePhases[lineIdx];
                int lineLen = (int)(rect.Height * 0.5f);
                int startY = rect.Y + (int)(phase * rect.Height);

                for (int dy = 0; dy < lineLen; dy++) {
                    int py = startY + dy;
                    if (py > rect.Bottom) py -= rect.Height;
                    if (py < rect.Y || py >= rect.Bottom) continue;

                    float t = dy / (float)lineLen;
                    float brightness = MathF.Sin(t * MathHelper.Pi) * 0.7f + 0.2f;
                    Color c = Color.Lerp(new Color(50, 130, 255), new Color(130, 80, 220), t)
                        * (alpha * brightness * 0.55f);
                    sb.Draw(px, new Rectangle(lx, py, lw, 1), new Rectangle(0, 0, 1, 1), c);
                }
            }

            //左侧竖向强调底条
            Color barTop = new Color(50, 130, 255) * (alpha * 0.2f);
            Color barBot = new Color(90, 60, 180) * (alpha * 0.08f);
            int barH = rect.Height / 2;
            sb.Draw(px, new Rectangle(rect.X + 5, rect.Y + 8, 1, barH), new Rectangle(0, 0, 1, 1), barTop);
            sb.Draw(px, new Rectangle(rect.X + 5, rect.Y + 8 + barH, 1, rect.Height - barH - 16), new Rectangle(0, 0, 1, 1), barBot);
        }

        /// <summary>
        /// 不对称重边框：顶部3px+1px双层强调+左侧4px渐变+轻量右/底线+顶部刻痕
        /// </summary>
        private void DrawNeonFrame(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float pulse = MathF.Sin(neonPulseTimer * 1.1f) * 0.2f + 0.8f;

            //顶部主强调线（3px亮 + 1px暗）
            Color topBright = new Color(55, 155, 255) * (alpha * 0.95f * pulse);
            Color topDim = new Color(35, 90, 190) * (alpha * 0.45f);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 3), new Rectangle(0, 0, 1, 1), topBright);
            sb.Draw(px, new Rectangle(rect.X, rect.Y + 3, rect.Width, 1), new Rectangle(0, 0, 1, 1), topDim);

            //顶部流动亮暗变化叠层
            for (int x = rect.X; x < rect.Right; x += 4) {
                float t = (float)(x - rect.X) / rect.Width;
                float wave = MathF.Sin(neonPulseTimer * 2.5f + t * MathHelper.TwoPi * 1.5f) * 0.3f + 0.7f;
                int w = Math.Min(4, rect.Right - x);
                sb.Draw(px, new Rectangle(x, rect.Y, w, 2), new Rectangle(0, 0, 1, 1),
                    new Color(80, 180, 255) * (alpha * 0.3f * wave * pulse));
            }

            //左侧强调竖条（4px全高，上亮下暗渐变）
            int halfH = rect.Height / 2;
            Color leftBright = new Color(50, 150, 255) * (alpha * 0.75f * pulse);
            Color leftDim = new Color(80, 60, 170) * (alpha * 0.35f);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 4, halfH), new Rectangle(0, 0, 1, 1), leftBright);
            sb.Draw(px, new Rectangle(rect.X, rect.Y + halfH, 4, rect.Height - halfH), new Rectangle(0, 0, 1, 1), leftDim);

            //右侧细线
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height),
                new Rectangle(0, 0, 1, 1), new Color(50, 70, 140) * (alpha * 0.4f));

            //底部细线（1px实线 + 1px紫调）
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), new Color(40, 80, 180) * (alpha * 0.4f));
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), new Color(80, 55, 150) * (alpha * 0.2f));

            //顶部左侧刻痕
            sb.Draw(px, new Rectangle(rect.X + 5, rect.Y, 1, 10), new Rectangle(0, 0, 1, 1), topBright * 0.8f);
            sb.Draw(px, new Rectangle(rect.X + 20, rect.Y, 1, 7), new Rectangle(0, 0, 1, 1), topBright * 0.5f);
            sb.Draw(px, new Rectangle(rect.X + 34, rect.Y, 1, 4), new Rectangle(0, 0, 1, 1), topBright * 0.3f);
        }

        /// <summary>
        /// 右侧装饰条纹（竖向刻度尺+霓虹流光）
        /// </summary>
        private void DrawRightOrnament(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            int rx = rect.Right - 12;
            int spacing = 10;
            int marks = rect.Height / spacing;
            float flow = neonPulseTimer * 0.3f;

            for (int i = 0; i < marks; i++) {
                float t = (float)i / marks;
                float bright = MathF.Sin((t + flow) * MathHelper.TwoPi) * 0.3f + 0.45f;
                int mLen = (i % 4 == 0) ? 8 : 4;
                Color mc = Color.Lerp(new Color(50, 130, 255), new Color(100, 70, 200), t)
                    * (alpha * bright * 0.55f);
                sb.Draw(px, new Rectangle(rx - mLen, rect.Y + i * spacing + 6, mLen, 1),
                    new Rectangle(0, 0, 1, 1), mc);
            }

            //右侧竖向流光线
            Color lineBase = new Color(50, 80, 160) * (alpha * 0.18f);
            sb.Draw(px, new Rectangle(rx + 1, rect.Y + 6, 1, rect.Height - 12),
                new Rectangle(0, 0, 1, 1), lineBase);
            float glowY = rect.Y + 6 + (sweepTimer * (rect.Height - 12));
            int glowLen = 20;
            for (int dy = 0; dy < glowLen; dy++) {
                int py = (int)glowY + dy;
                if (py < rect.Y + 6 || py >= rect.Bottom - 6) continue;
                float bright = MathF.Sin(dy / (float)glowLen * MathHelper.Pi) * 0.6f;
                Color gc = new Color(80, 160, 255) * (alpha * bright);
                sb.Draw(px, new Rectangle(rx + 1, py, 1, 1), new Rectangle(0, 0, 1, 1), gc);
            }
        }

        /// <summary>
        /// 左上角四角星纹样（双层旋转，增强存在感）
        /// </summary>
        private void DrawCornerStarSymbol(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            Vector2 center = new(rect.X + 16, rect.Y + 16);
            float pulse = MathF.Sin(neonPulseTimer * 1.5f) * 0.2f + 0.8f;
            Color starColor = new Color(80, 180, 255) * (alpha * 0.9f * pulse);

            //外层星（反向慢转）
            float rot2 = -starSpinTimer * 0.4f;
            for (int i = 0; i < 4; i++) {
                float angle = rot2 + i * MathHelper.PiOver2;
                sb.Draw(px, center, null, starColor * 0.35f, angle, new Vector2(0.5f, 0f),
                    new Vector2(1.2f, 12f), SpriteEffects.None, 0f);
            }

            //内层四角星
            float rot = starSpinTimer;
            for (int i = 0; i < 4; i++) {
                float angle = rot + i * MathHelper.PiOver2;
                sb.Draw(px, center, null, starColor, angle, new Vector2(0.5f, 0f),
                    new Vector2(1.8f, 9f), SpriteEffects.None, 0f);
            }

            //中心亮点
            sb.Draw(px, center, null, starColor * 1.3f, 0f, new Vector2(0.5f),
                new Vector2(3.2f), SpriteEffects.None, 0f);

            //外圈环形（12段弧环）
            float ringRadius = 12f;
            Color ringColor = new Color(60, 140, 240) * (alpha * 0.4f * pulse);
            for (int i = 0; i < 12; i++) {
                float angle = rot * 0.5f + i * (MathHelper.TwoPi / 12f);
                Vector2 rp = center + angle.ToRotationVector2() * ringRadius;
                sb.Draw(px, rp, null, ringColor, angle, new Vector2(0.5f),
                    new Vector2(3.5f, 1f), SpriteEffects.None, 0f);
            }
        }

        /// <summary>
        /// 三角金属褶边角饰+底部褶边条纹
        /// </summary>
        private void DrawMetallicFrillCorners(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float pulse = MathF.Sin(neonPulseTimer * 0.8f) * 0.12f + 0.88f;
            Color frillBase = new Color(70, 110, 180) * (alpha * 0.55f * pulse);
            Color frillHighlight = new Color(130, 170, 240) * (alpha * 0.4f * pulse);

            //右上角
            for (int i = 0; i < 7; i++) {
                int segW = 8 - i;
                sb.Draw(px, new Rectangle(rect.Right - 8 - i * 3, rect.Y + 5, segW, 1),
                    new Rectangle(0, 0, 1, 1), frillBase * (1f - i * 0.12f));
                sb.Draw(px, new Rectangle(rect.Right - 5, rect.Y + 5 + i * 3, 1, segW),
                    new Rectangle(0, 0, 1, 1), frillBase * (1f - i * 0.12f));
            }
            sb.Draw(px, new Vector2(rect.Right - 7, rect.Y + 7), null, frillHighlight,
                0f, new Vector2(0.5f), 2f, SpriteEffects.None, 0f);

            //左下角
            for (int i = 0; i < 6; i++) {
                int segH = 7 - i;
                sb.Draw(px, new Rectangle(rect.X + 5, rect.Bottom - 8 - i * 3, 1, segH),
                    new Rectangle(0, 0, 1, 1), frillBase * (1f - i * 0.14f));
                sb.Draw(px, new Rectangle(rect.X + 5 + i * 3, rect.Bottom - 5, segH, 1),
                    new Rectangle(0, 0, 1, 1), frillBase * (1f - i * 0.14f));
            }
            sb.Draw(px, new Vector2(rect.X + 7, rect.Bottom - 7), null, frillHighlight,
                0f, new Vector2(0.5f), 1.6f, SpriteEffects.None, 0f);

            //右下角
            for (int i = 0; i < 6; i++) {
                sb.Draw(px, new Rectangle(rect.Right - 8 - i * 2, rect.Bottom - 7 + i, 4, 1),
                    new Rectangle(0, 0, 1, 1), frillBase * (1f - i * 0.15f));
            }
            sb.Draw(px, new Vector2(rect.Right - 9, rect.Bottom - 7), null, frillHighlight,
                0f, new Vector2(0.5f), 1.6f, SpriteEffects.None, 0f);

            //底部装饰褶边条纹
            Color bottomFrill = new Color(60, 90, 160) * (alpha * 0.25f * pulse);
            for (int x = rect.X + 28; x < rect.Right - 28; x += 4) {
                int h = (x % 8 == 0) ? 4 : 2;
                sb.Draw(px, new Rectangle(x, rect.Bottom - 3 - h, 1, h),
                    new Rectangle(0, 0, 1, 1), bottomFrill);
            }
        }

        /// <summary>
        /// 四角状态文字
        /// </summary>
        private void DrawCornerStatusText(SpriteBatch sb, Rectangle rect, float alpha) {
            if (alpha < 0.04f) return;
            float blink = MathF.Sin(neonPulseTimer * 0.75f) * 0.15f + 0.85f;
            Color col = new Color(80, 140, 230) * (alpha * 0.5f * blink);
            float sc = 0.5f;
            var font = Terraria.GameContent.FontAssets.MouseText.Value;

            Utils.DrawBorderString(sb, cornerStatus[0],
                new Vector2(rect.X + 28f, rect.Y + 7f), col, sc);
            float w1 = font.MeasureString(cornerStatus[1]).X * sc;
            Utils.DrawBorderString(sb, cornerStatus[1],
                new Vector2(rect.Right - w1 - 16f, rect.Y + 7f), col, sc);
            Utils.DrawBorderString(sb, cornerStatus[2],
                new Vector2(rect.X + 8f, rect.Bottom - 16f), col * 0.65f, sc);
            float w3 = font.MeasureString(cornerStatus[3]).X * sc;
            Utils.DrawBorderString(sb, cornerStatus[3],
                new Vector2(rect.Right - w3 - 16f, rect.Bottom - 16f), col * 0.65f, sc);
        }

        /// <summary>
        /// 选项悬停时左侧霓虹竖线指示（蓝紫色竖向虚线）
        /// </summary>
        private void DrawChoiceNeonIndicator(SpriteBatch sb, Rectangle choiceRect, Color neonColor, float hoverProgress, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            const int dashH = 4, gapH = 3;
            float flow = dataFlowTimer * 20f;
            float period = dashH + gapH;
            Color dashColor = neonColor * (hoverProgress * 0.4f);

            float y = choiceRect.Y - (flow % period);
            while (y < choiceRect.Bottom) {
                float segStart = Math.Max(y, choiceRect.Y);
                float segEnd = Math.Min(y + dashH, choiceRect.Bottom);
                if (segEnd > segStart) {
                    sb.Draw(px,
                        new Rectangle(choiceRect.X, (int)segStart, 2, (int)(segEnd - segStart)),
                        new Rectangle(0, 0, 1, 1), dashColor);
                }
                y += period;
            }
        }

        /// <summary>
        /// 选项悬停时底部霓虹蓝紫流光
        /// </summary>
        private static void DrawChoiceHoverGlow(SpriteBatch sb, Rectangle choiceRect, float hoverProgress, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            int glowW = (int)(choiceRect.Width * hoverProgress * 0.6f);
            Color glowColor = new Color(50, 120, 255) * (alpha * hoverProgress * 0.2f);
            sb.Draw(px, new Rectangle(choiceRect.X, choiceRect.Bottom - 1, glowW, 1),
                new Rectangle(0, 0, 1, 1), glowColor);
        }

        private static void DrawChoiceBorder(SpriteBatch spriteBatch, Rectangle rect, Color color) {
            Texture2D px = VaultAsset.placeholder2.Value;
            spriteBatch.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), color);
            spriteBatch.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), color);
            spriteBatch.Draw(px, new Rectangle(rect.X, rect.Y, 1, rect.Height),
                new Rectangle(0, 0, 1, 1), color);
            spriteBatch.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height),
                new Rectangle(0, 0, 1, 1), color);
        }

        #endregion
    }
}
