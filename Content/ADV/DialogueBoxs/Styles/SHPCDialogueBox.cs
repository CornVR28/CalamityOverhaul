using CalamityOverhaul.Content.ADV.UIEffect;
using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.ADV.DialogueBoxs.Styles
{
    /// <summary>
    /// SHPC专属赛博女仆风格对话框<br/>
    /// 「未来科技」与「优雅复古」的融合，深海蓝碳纤维底板搭配霓虹蓝光条<br/>
    /// 左上角四角星纹样、金属褶边角饰、二进制矩阵底纹、左侧霓虹数据流线<br/>
    /// 以及发光领结文本前缀符号和全息名称标签
    /// </summary>
    internal class SHPCDialogueBox : DialogueBoxBase
    {
        public static SHPCDialogueBox Instance => UIHandleLoader.GetUIHandleOfType<SHPCDialogueBox>();
        public override string LocalizationCategory => "UI";

        private const float FixedWidth = 540f;
        protected override float PanelWidth => FixedWidth;

        //动画计时器
        private float neonPulseTimer;      //霓虹呼吸灯
        private float dataFlowTimer;       //数据流线/二进制底纹流动
        private float holoFlicker;         //全息闪烁
        private float sweepTimer;          //扫描线循环
        private float starSpinTimer;       //左上角四角星旋转

        //左侧数据流线（面板左边缘的竖向霓虹流动线）
        private readonly float[] dataLinePhases = new float[3];

        //四角状态文字（女仆系统状态读出，类似嘉登的HEX但以女仆主题呈现）
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
        private const float SideMargin = 24f;

        //斜切角参数（形成非矩形六角轮廓）
        private const int TopRightChamfer = 24;
        private const int BottomLeftChamfer = 18;

        #region 样式配置重写

        protected override float PortraitScaleMin => 0.88f;
        protected override float TopNameOffsetBase => 12f;
        protected override float TextBlockOffsetBase => 38f;
        protected override float NameScale => 0.95f;
        protected override float TextScale => 0.82f;
        protected override float NameGlowRadius => 2f;
        protected override float PortraitAvailHeightOffset => 50f;
        protected override float PortraitMinHeight => 100f;
        protected override float PortraitMaxHeight => 270f;
        protected override float PortraitFramePadding => 6f;
        protected override float PortraitGlowPadding => 3f;
        protected override float PortraitLeftMargin => 22f;
        protected override float ContinueHintScale => 0.82f;
        protected override float FastHintScale => 0.72f;

        protected override Color GetSilhouetteColor(ContentDrawContext ctx)
            => new Color(18, 12, 35) * 0.88f;

        //文字行保持稳定不做抖动
        protected override Vector2 ApplyTextLineOffset(ContentDrawContext ctx, Vector2 basePosition, int lineIndex)
            => basePosition;

        protected override Color GetTextLineColor(ContentDrawContext ctx, int lineIndex)
            => Color.Lerp(new Color(210, 225, 255), Color.White, 0.18f) * ctx.ContentAlpha;

        //浅紫蓝色提示
        protected override Color GetContinueHintColor(ContentDrawContext ctx, float blink)
            => new Color(120, 160, 255) * (blink * ctx.ContentAlpha);

        protected override Color GetFastHintColor(ContentDrawContext ctx)
            => new Color(100, 130, 210) * (0.45f * ctx.ContentAlpha);

        #endregion

        #region 模板方法实现

        /// <summary>
        /// 头像框：深色底板+双层霓虹蓝边框+底部褶边装饰+右下角小菱形
        /// </summary>
        protected override void DrawPortraitFrame(ContentDrawContext ctx, Rectangle frameRect) {
            SpriteBatch sb = ctx.SpriteBatch;
            Texture2D px = VaultAsset.placeholder2.Value;
            float alpha = ctx.Alpha * ctx.PortraitData.Fade * ctx.PortraitExtraAlpha;

            //深紫黑底板
            sb.Draw(px, frameRect, new Rectangle(0, 0, 1, 1),
                new Color(8, 6, 18) * (alpha * 0.92f));

            //外层主边框（霓虹蓝，2px）
            float pulse = MathF.Sin(neonPulseTimer * 1.2f) * 0.2f + 0.8f;
            Color neonEdge = new Color(50, 140, 255) * (alpha * 0.8f * pulse);
            DrawRect(sb, px, frameRect, 2, neonEdge);

            //内层细边框（浅紫调，1px，内缩3px）
            Rectangle inner = frameRect;
            inner.Inflate(-3, -3);
            Color innerGlow = new Color(130, 100, 200) * (alpha * 0.3f);
            DrawRect(sb, px, inner, 1, innerGlow);

            //底部褶边装饰（像素化的金属褶皱感，间隔3px的短竖线）
            Color frillColor = new Color(80, 130, 220) * (alpha * 0.35f);
            for (int x = frameRect.X + 4; x < frameRect.Right - 4; x += 3) {
                int h = (x % 6 == 0) ? 4 : 2;
                sb.Draw(px, new Rectangle(x, frameRect.Bottom - 2 - h, 1, h),
                    new Rectangle(0, 0, 1, 1), frillColor);
            }

            //右下角小菱形点缀
            DrawSmallDiamond(sb, px,
                new Vector2(frameRect.Right - 8, frameRect.Bottom - 8),
                new Color(100, 160, 255) * (alpha * 0.6f * pulse), 3f);
        }

        /// <summary>
        /// 头像辉光：霓虹蓝紫色脉冲光晕
        /// </summary>
        protected override void DrawPortraitGlow(ContentDrawContext ctx, Rectangle glowRect) {
            SpriteBatch sb = ctx.SpriteBatch;
            Texture2D px = VaultAsset.placeholder2.Value;
            float pulse = MathF.Sin(neonPulseTimer * 1.3f) * 0.3f + 0.7f;
            Color glow = new Color(60, 120, 240) *
                (ctx.ContentAlpha * 0.45f * pulse * ctx.PortraitData.Fade * ctx.PortraitExtraAlpha);

            sb.Draw(px, glowRect, new Rectangle(0, 0, 1, 1), glow * 0.1f);
            int b = 2;
            sb.Draw(px, new Rectangle(glowRect.X, glowRect.Y, glowRect.Width, b), new Rectangle(0, 0, 1, 1), glow * 0.7f);
            sb.Draw(px, new Rectangle(glowRect.X, glowRect.Bottom - b, glowRect.Width, b), new Rectangle(0, 0, 1, 1), glow * 0.4f);
            sb.Draw(px, new Rectangle(glowRect.X, glowRect.Y, b, glowRect.Height), new Rectangle(0, 0, 1, 1), glow * 0.55f);
            sb.Draw(px, new Rectangle(glowRect.Right - b, glowRect.Y, b, glowRect.Height), new Rectangle(0, 0, 1, 1), glow * 0.55f);
        }

        /// <summary>
        /// 名称装饰：发光领结图标前缀+全息风名称辉光+名字下方霓虹细线
        /// </summary>
        protected override void DrawNameGlow(ContentDrawContext ctx, Vector2 position, float alpha) {
            SpriteBatch sb = ctx.SpriteBatch;
            Texture2D px = VaultAsset.placeholder2.Value;

            //名字辉光晕（霓虹蓝紫色）
            Color nameGlow = new Color(80, 140, 255) * (alpha * 0.7f);
            for (int i = 0; i < NameGlowCount; i++) {
                float a = MathHelper.TwoPi * i / NameGlowCount;
                Vector2 off = a.ToRotationVector2() * NameGlowRadius * ctx.SwitchEase;
                Utils.DrawBorderString(sb, current.Speaker, position + off, nameGlow * 0.5f, NameScale);
            }

            //发光领结图标前缀（用像素化的蝴蝶结形状：中心小点+左右两个三角）
            float bowPulse = MathF.Sin(neonPulseTimer * 2f) * 0.2f + 0.8f;
            Color bowColor = new Color(100, 180, 255) * (alpha * 0.85f * bowPulse);
            Vector2 bowPos = position - new Vector2(18f, -2f);
            //中心结
            sb.Draw(px, bowPos, null, bowColor, 0f, new Vector2(0.5f),
                new Vector2(2f, 3f), SpriteEffects.None, 0f);
            //左翼
            sb.Draw(px, bowPos - new Vector2(3f, 0f), null, bowColor * 0.85f,
                MathHelper.PiOver4 * 0.3f, new Vector2(0.5f),
                new Vector2(4f, 2f), SpriteEffects.None, 0f);
            //右翼
            sb.Draw(px, bowPos + new Vector2(3f, 0f), null, bowColor * 0.85f,
                -MathHelper.PiOver4 * 0.3f, new Vector2(0.5f),
                new Vector2(4f, 2f), SpriteEffects.None, 0f);

            //名字下方霓虹渐变细线
            float nameW = Terraria.GameContent.FontAssets.MouseText.Value
                .MeasureString(current.Speaker).X * NameScale;
            int lineW = (int)(nameW * 0.7f);
            for (int seg = 0; seg < lineW; seg++) {
                float t = seg / (float)lineW;
                float bright = MathF.Sin(t * MathHelper.Pi) * 0.65f + 0.35f;
                Color lineC = Color.Lerp(new Color(60, 140, 255), new Color(140, 100, 230), t)
                    * (alpha * 0.5f * bright);
                sb.Draw(px, new Rectangle((int)position.X + seg, (int)(position.Y + 20f), 1, 1),
                    new Rectangle(0, 0, 1, 1), lineC);
            }
        }

        /// <summary>
        /// 分割线：实线+浅虚线双层，带霓虹蓝紫渐变色
        /// </summary>
        protected override void DrawDividerLine(ContentDrawContext ctx, Vector2 start, Vector2 end, float alpha) {
            SpriteBatch sb = ctx.SpriteBatch;
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
                sb.Draw(px, new Rectangle((int)segX, (int)start.Y, (int)segW + 1, 1),
                    new Rectangle(0, 0, 1, 1), c);
            }

            //下方1px处的虚线装饰（更暗更细）
            const int dashW = 4, gapW = 3;
            float flow = dataFlowTimer * 18f;
            float period = dashW + gapW;
            float x = start.X - (flow % period);
            while (x < end.X) {
                float segStart = Math.Max(x, start.X);
                float segEnd = Math.Min(x + dashW, end.X);
                if (segEnd > segStart) {
                    float t = (segStart - start.X) / len;
                    Color c = new Color(80, 100, 180) * (alpha * 0.25f);
                    sb.Draw(px, new Rectangle((int)segStart, (int)start.Y + 2, (int)(segEnd - segStart), 1),
                        new Rectangle(0, 0, 1, 1), c);
                }
                x += period;
            }
        }

        #endregion

        protected override void StyleUpdate(Vector2 panelPos, Vector2 panelSize) {
            Advance(ref neonPulseTimer, 0.028f);
            Advance(ref holoFlicker, 0.11f);
            Advance(ref dataFlowTimer, 0.018f);
            sweepTimer = (sweepTimer + 0.006f) % 1f;
            starSpinTimer += 0.012f;
            if (starSpinTimer > MathHelper.TwoPi) starSpinTimer -= MathHelper.TwoPi;

            //左侧数据流线相位更新
            for (int i = 0; i < dataLinePhases.Length; i++)
                dataLinePhases[i] = (dataLinePhases[i] + 0.015f + i * 0.004f) % 1f;

            //四角状态文字刷新（每50帧）
            statusUpdateClock++;
            if (statusUpdateClock >= 50) {
                statusUpdateClock = 0;
                for (int i = 0; i < cornerStatus.Length; i++)
                    cornerStatus[i] = StatusPool[Main.rand.Next(StatusPool.Length)];
            }

            //霓虹粒子（间隔22帧，上限12个）
            neonParticleSpawnTimer++;
            if (Active && neonParticleSpawnTimer >= 22 && neonParticles.Count < 12) {
                neonParticleSpawnTimer = 0;
                float scaleW = Main.UIScale;
                float left = panelPos.X + SideMargin * scaleW;
                float right = panelPos.X + panelSize.X - SideMargin * scaleW;
                Vector2 p = new(Main.rand.NextFloat(left, right),
                    panelPos.Y + Main.rand.NextFloat(35f, panelSize.Y - 35f));
                neonParticles.Add(new NeonMaidPRT(p));
            }
            for (int i = neonParticles.Count - 1; i >= 0; i--) {
                if (neonParticles[i].Update(panelPos, panelSize))
                    neonParticles.RemoveAt(i);
            }

            //电路节点（间隔32帧，上限6个）
            circuitNodeSpawnTimer++;
            if (Active && circuitNodeSpawnTimer >= 32 && circuitNodes.Count < 6) {
                circuitNodeSpawnTimer = 0;
                float scaleW = Main.UIScale;
                float left = panelPos.X + SideMargin * scaleW;
                float right = panelPos.X + panelSize.X - SideMargin * scaleW;
                circuitNodes.Add(new CircuitNodePRT(
                    new Vector2(Main.rand.NextFloat(left, right),
                                panelPos.Y + Main.rand.NextFloat(35f, panelSize.Y - 35f))));
            }
            for (int i = circuitNodes.Count - 1; i >= 0; i--) {
                if (circuitNodes[i].Update(panelPos, panelSize))
                    circuitNodes.RemoveAt(i);
            }
        }

        private static void Advance(ref float t, float speed) {
            t += speed;
            if (t > MathHelper.TwoPi) t -= MathHelper.TwoPi;
        }

        protected override void DrawStyle(SpriteBatch spriteBatch, Rectangle panelRect, float alpha,
            float contentAlpha, float easedProgress) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //多层扩散阴影（偏紫黑色调，加深加厚）
            for (int d = 14; d >= 1; d--) {
                Rectangle s = panelRect;
                s.Inflate(d, d);
                s.Offset(5, 7);
                spriteBatch.Draw(px, s, new Rectangle(0, 0, 1, 1),
                    new Color(8, 4, 16) * (alpha * 0.07f * (14f - d) / 14f));
            }

            //深紫黑渐变背景+二进制矩阵底纹
            DrawCyberMaidBackground(spriteBatch, panelRect, alpha);

            //斜切角遮罩（产生非矩形六角轮廓，覆盖右上和左下角的背景）
            DrawChamferMask(spriteBatch, px, panelRect, alpha);

            //左侧霓虹数据流线
            DrawLeftDataLines(spriteBatch, panelRect, alpha);

            //缓慢扫描线（带辉光扩散的宽扫描带，增强实体质感）
            float scanY = panelRect.Y + sweepTimer * panelRect.Height;
            for (int row = -6; row <= 6; row++) {
                float iy = scanY + row * 1.5f;
                if (iy < panelRect.Y || iy > panelRect.Bottom) continue;
                float dist = Math.Abs(row) / 6f;
                float fade = (1f - dist) * (1f - dist);
                spriteBatch.Draw(px,
                    new Rectangle(panelRect.X + 6, (int)iy, panelRect.Width - 12, 1),
                    new Rectangle(0, 0, 1, 1),
                    new Color(55, 120, 220) * (alpha * 0.22f * fade));
            }

            //顶部标题区域暗化条带（适配斜切轮廓，右侧缩进避开chamfer区域）
            int headerH = 32;
            int headerW = panelRect.Width - 10 - TopRightChamfer;
            spriteBatch.Draw(px,
                new Rectangle(panelRect.X + 5, panelRect.Y + 5, headerW, headerH),
                new Rectangle(0, 0, 1, 1),
                new Color(4, 3, 12) * (alpha * 0.35f));
            //标题区域底线（加深加亮增强层次分离）
            spriteBatch.Draw(px,
                new Rectangle(panelRect.X + 8, panelRect.Y + 5 + headerH, headerW - 6, 1),
                new Rectangle(0, 0, 1, 1),
                new Color(50, 100, 200) * (alpha * 0.2f));
            //标题底线下方暗沟（增加区域分割的纵深感）
            spriteBatch.Draw(px,
                new Rectangle(panelRect.X + 8, panelRect.Y + 6 + headerH, headerW - 6, 1),
                new Rectangle(0, 0, 1, 1),
                new Color(2, 2, 6) * (alpha * 0.3f));

            //不对称重边框（替代均匀薄框）
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
                particle.Draw(spriteBatch, alpha * 0.65f);

            DrawTimedProgressIndicator(spriteBatch, panelRect, alpha);

            if (current == null || contentAlpha <= 0.01f)
                return;

            DrawPortraitAndText(spriteBatch, panelRect, alpha, contentAlpha);
        }

        #region 样式工具函数

        /// <summary>
        /// 深紫黑渐变背景+碳纤维交叉编织纹理+水平磨砂微纹+二进制矩阵底纹
        /// +面板接缝线+内嵌斜面光照(上左亮/下右暗)+边缘暗角+全息闪烁叠层
        /// </summary>
        private void DrawCyberMaidBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            // ── 1. 纵向渐变（32段，深紫黑→深海蓝，高不透明度）──
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
                Color c = Color.Lerp(mid, bright, pulse * 0.5f) * (alpha * 0.97f);

                sb.Draw(px, new Rectangle(rect.X, y1, rect.Width, Math.Max(1, y2 - y1)),
                    new Rectangle(0, 0, 1, 1), c);
            }

            // ── 2. 碳纤维交叉编织纹理（双向45°交叉，间距更密，可见度更高）──
            int dSpacing = 14;
            float dPhase = dataFlowTimer * 8f;
            Color diagColorA = new Color(35, 55, 110) * (alpha * 0.065f);
            Color diagColorB = new Color(28, 45, 95) * (alpha * 0.045f);
            for (int col = -(rect.Height / dSpacing) - 2; col < (rect.Width / dSpacing) + 3; col++) {
                int ox = (int)(col * dSpacing + dPhase % dSpacing);
                for (int row = 0; row < rect.Height; row += 2) {
                    int px2 = rect.X + ox + row;
                    if (px2 >= rect.X && px2 < rect.Right)
                        sb.Draw(px, new Rectangle(px2, rect.Y + row, 1, 1), new Rectangle(0, 0, 1, 1), diagColorA);
                    int px3 = rect.X + ox - row + rect.Height;
                    if (px3 >= rect.X && px3 < rect.Right)
                        sb.Draw(px, new Rectangle(px3, rect.Y + row, 1, 1), new Rectangle(0, 0, 1, 1), diagColorB);
                }
            }

            // ── 3. 水平磨砂微纹线（模拟金属面板横向拉丝质感）──
            for (int row = 0; row < rect.Height; row += 3) {
                float rowBright = (row % 6 == 0) ? 0.035f : 0.018f;
                sb.Draw(px, new Rectangle(rect.X + 5, rect.Y + row, rect.Width - 10, 1),
                    new Rectangle(0, 0, 1, 1), new Color(30, 50, 90) * (alpha * rowBright));
            }

            // ── 4. 二进制矩阵底纹（更密、更可见的活跃数据点阵）──
            int gridSpacingX = 8;
            int gridSpacingY = 7;
            float matrixPhase = dataFlowTimer * 12f;
            Color matrixColor = new Color(55, 80, 150) * (alpha * 0.075f);
            for (int col = 0; col < rect.Width / gridSpacingX; col++) {
                int cx = rect.X + col * gridSpacingX + 4;
                if (cx >= rect.Right - 4) continue;
                float colPhase = matrixPhase + col * 0.7f;
                for (int row = 0; row < rect.Height / gridSpacingY; row++) {
                    int cy = rect.Y + row * gridSpacingY + 3;
                    if (cy >= rect.Bottom - 3) continue;
                    float hash = MathF.Sin(col * 13.7f + row * 7.3f + colPhase) * 0.5f + 0.5f;
                    if (hash > 0.42f) {
                        float dotAlpha = (hash - 0.42f) / 0.58f;
                        sb.Draw(px, new Rectangle(cx, cy, 1, 1),
                            new Rectangle(0, 0, 1, 1), matrixColor * dotAlpha);
                    }
                }
            }

            // ── 5. 面板接缝线（每55px一条水平凹槽：暗线+下方高光反射线）──
            Color seamDark = new Color(3, 4, 10) * (alpha * 0.35f);
            Color seamLight = new Color(55, 85, 155) * (alpha * 0.06f);
            for (int sy = rect.Y + 55; sy < rect.Bottom - 20; sy += 55) {
                sb.Draw(px, new Rectangle(rect.X + 8, sy, rect.Width - 16, 1),
                    new Rectangle(0, 0, 1, 1), seamDark);
                sb.Draw(px, new Rectangle(rect.X + 8, sy + 1, rect.Width - 16, 1),
                    new Rectangle(0, 0, 1, 1), seamLight);
            }

            // ── 6. 内嵌斜面光照（上/左高光 + 下/右阴影 —— 立体感核心）──
            Rectangle inset = rect;
            inset.Inflate(-6, -6);
            Color bevelLight = new Color(55, 90, 170) * (alpha * 0.18f);
            Color bevelShadow = new Color(2, 3, 8) * (alpha * 0.4f);
            // 上边高光（2px渐弱）
            sb.Draw(px, new Rectangle(inset.X, inset.Y, inset.Width, 1), new Rectangle(0, 0, 1, 1), bevelLight);
            sb.Draw(px, new Rectangle(inset.X, inset.Y + 1, inset.Width, 1), new Rectangle(0, 0, 1, 1), bevelLight * 0.45f);
            // 左边高光（2px渐弱）
            sb.Draw(px, new Rectangle(inset.X, inset.Y, 1, inset.Height), new Rectangle(0, 0, 1, 1), bevelLight * 0.8f);
            sb.Draw(px, new Rectangle(inset.X + 1, inset.Y, 1, inset.Height), new Rectangle(0, 0, 1, 1), bevelLight * 0.3f);
            // 下边阴影（2px渐弱）
            sb.Draw(px, new Rectangle(inset.X, inset.Bottom - 1, inset.Width, 1), new Rectangle(0, 0, 1, 1), bevelShadow);
            sb.Draw(px, new Rectangle(inset.X, inset.Bottom - 2, inset.Width, 1), new Rectangle(0, 0, 1, 1), bevelShadow * 0.5f);
            // 右边阴影（2px渐弱）
            sb.Draw(px, new Rectangle(inset.Right - 1, inset.Y, 1, inset.Height), new Rectangle(0, 0, 1, 1), bevelShadow * 0.75f);
            sb.Draw(px, new Rectangle(inset.Right - 2, inset.Y, 1, inset.Height), new Rectangle(0, 0, 1, 1), bevelShadow * 0.3f);

            // ── 7. 边缘暗角（vignette：四边内侧渐暗条带，增加面板纵深）──
            int vigW = 28;
            for (int v = 0; v < vigW; v += 3) {
                float vFade = (1f - (float)v / vigW) * 0.14f;
                Color vColor = new Color(2, 2, 8) * (alpha * vFade);
                int thickness = Math.Max(1, 3 - v / 10);
                sb.Draw(px, new Rectangle(rect.X + v, rect.Y, thickness, rect.Height), new Rectangle(0, 0, 1, 1), vColor);
                sb.Draw(px, new Rectangle(rect.Right - v - thickness, rect.Y, thickness, rect.Height), new Rectangle(0, 0, 1, 1), vColor);
            }
            int vigH = 18;
            for (int v = 0; v < vigH; v += 3) {
                float vFade = (1f - (float)v / vigH) * 0.11f;
                Color vColor = new Color(2, 2, 8) * (alpha * vFade);
                int thickness = Math.Max(1, 3 - v / 8);
                sb.Draw(px, new Rectangle(rect.X, rect.Y + v, rect.Width, thickness), new Rectangle(0, 0, 1, 1), vColor);
                sb.Draw(px, new Rectangle(rect.X, rect.Bottom - v - thickness, rect.Width, thickness), new Rectangle(0, 0, 1, 1), vColor);
            }

            // ── 8. 全息闪烁叠层（偏紫色）──
            float flicker = MathF.Sin(holoFlicker * 1.5f) * 0.5f + 0.5f;
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1),
                new Color(14, 10, 32) * (alpha * 0.22f * flicker));
        }

        /// <summary>
        /// 左侧边缘的竖向霓虹数据流线（3条，加宽增亮，带辉光侧翼扩散）
        /// </summary>
        private void DrawLeftDataLines(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            int[] xOffsets = [8, 15, 22];
            int[] widths = [3, 2, 3];

            for (int lineIdx = 0; lineIdx < 3; lineIdx++) {
                int lx = rect.X + xOffsets[lineIdx];
                int lw = widths[lineIdx];
                float phase = dataLinePhases[lineIdx];
                int lineLen = (int)(rect.Height * 0.55f);
                int startY = rect.Y + (int)(phase * rect.Height);

                for (int dy = 0; dy < lineLen; dy++) {
                    int py = startY + dy;
                    if (py > rect.Bottom) py -= rect.Height;
                    if (py < rect.Y || py >= rect.Bottom) continue;

                    float t = dy / (float)lineLen;
                    float brightness = MathF.Sin(t * MathHelper.Pi) * 0.75f + 0.22f;
                    Color c = Color.Lerp(new Color(50, 130, 255), new Color(130, 80, 220), t)
                        * (alpha * brightness * 0.62f);
                    // 主线
                    sb.Draw(px, new Rectangle(lx, py, lw, 1), new Rectangle(0, 0, 1, 1), c);
                    // 左右侧翼辉光
                    sb.Draw(px, new Rectangle(lx - 1, py, 1, 1), new Rectangle(0, 0, 1, 1), c * 0.22f);
                    sb.Draw(px, new Rectangle(lx + lw, py, 1, 1), new Rectangle(0, 0, 1, 1), c * 0.22f);
                }
            }

            // 左侧竖向强调底条（常驻，加宽至2px，给面板左边缘重量感）
            Color barTop = new Color(50, 130, 255) * (alpha * 0.25f);
            Color barBot = new Color(90, 60, 180) * (alpha * 0.1f);
            int barH = rect.Height / 2;
            sb.Draw(px, new Rectangle(rect.X + 5, rect.Y + 8, 2, barH), new Rectangle(0, 0, 1, 1), barTop);
            sb.Draw(px, new Rectangle(rect.X + 5, rect.Y + 8 + barH, 2, rect.Height - barH - 16), new Rectangle(0, 0, 1, 1), barBot);
        }

        /// <summary>
        /// 绘制斜切角遮罩三角形，使面板呈现非矩形的六角轮廓
        /// </summary>
        private static void DrawChamferMask(SpriteBatch sb, Texture2D px, Rectangle rect, float alpha) {
            Color mask = new Color(0, 0, 2) * (alpha * 0.97f);

            // 右上角斜切遮罩（从(Right-chamfer, Top) → (Right, Top+chamfer) 以外区域）
            for (int row = 0; row < TopRightChamfer; row++) {
                int cutW = TopRightChamfer - row;
                sb.Draw(px, new Rectangle(rect.Right - cutW, rect.Y + row, cutW, 1),
                    new Rectangle(0, 0, 1, 1), mask);
            }

            // 左下角斜切遮罩（从(Left, Bottom-chamfer) → (Left+chamfer, Bottom) 以外区域）
            for (int row = 0; row < BottomLeftChamfer; row++) {
                int cutW = BottomLeftChamfer - row;
                sb.Draw(px, new Rectangle(rect.X, rect.Bottom - BottomLeftChamfer + row, cutW, 1),
                    new Rectangle(0, 0, 1, 1), mask);
            }

            // 遮罩边缘柔化（1px暗边过渡，消除硬切边的锯齿感）
            Color softEdge = new Color(4, 3, 10) * (alpha * 0.5f);
            for (int row = 0; row < TopRightChamfer; row++) {
                int edgeX = rect.Right - (TopRightChamfer - row) - 1;
                if (edgeX >= rect.X && edgeX < rect.Right)
                    sb.Draw(px, new Rectangle(edgeX, rect.Y + row, 1, 1),
                        new Rectangle(0, 0, 1, 1), softEdge);
            }
            for (int row = 0; row < BottomLeftChamfer; row++) {
                int edgeX = rect.X + (BottomLeftChamfer - row);
                int edgeY = rect.Bottom - BottomLeftChamfer + row;
                if (edgeX >= rect.X && edgeX < rect.Right)
                    sb.Draw(px, new Rectangle(edgeX, edgeY, 1, 1),
                        new Rectangle(0, 0, 1, 1), softEdge);
            }
        }

        /// <summary>
        /// 斜切六角边框：非矩形轮廓 —— 右上斜切+左下斜切+不对称重线<br/>
        /// 拐角处精确接合避免像素重叠，辉光沿斜切边缘扩散
        /// </summary>
        private void DrawNeonFrame(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float pulse = MathF.Sin(neonPulseTimer * 1.1f) * 0.2f + 0.8f;
            int trCut = TopRightChamfer;
            int blCut = BottomLeftChamfer;
            int topLineW = rect.Width - trCut;
            int leftLineH = rect.Height - blCut;
            int botLineW = rect.Width - blCut;
            int rightLineH = rect.Height - trCut;

            // ── 外侧辉光溢出（沿六角轮廓扩散）──
            for (int g = 1; g <= 4; g++) {
                float gAlpha = (1f - g / 5f) * 0.16f;
                Color gColor = new Color(50, 140, 255) * (alpha * gAlpha * pulse);
                // 顶部辉光（到斜切起点）
                sb.Draw(px, new Rectangle(rect.X, rect.Y - g, topLineW, 1),
                    new Rectangle(0, 0, 1, 1), gColor);
                // 右侧辉光（从斜切终点开始）
                sb.Draw(px, new Rectangle(rect.Right + g, rect.Y + trCut, 1, rightLineH),
                    new Rectangle(0, 0, 1, 1), gColor * 0.4f);
            }
            for (int g = 1; g <= 3; g++) {
                float gAlpha = (1f - g / 4f) * 0.12f;
                Color gColor = new Color(50, 140, 255) * (alpha * gAlpha * pulse);
                // 左侧辉光（到斜切起点）
                sb.Draw(px, new Rectangle(rect.X - g, rect.Y, 1, leftLineH),
                    new Rectangle(0, 0, 1, 1), gColor);
            }

            // ── 顶部主强调线（3px亮 + 1px暗，截止于右上斜切起点）──
            Color topBright = new Color(55, 155, 255) * (alpha * 0.97f * pulse);
            Color topDim = new Color(35, 90, 190) * (alpha * 0.48f);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, topLineW, 3),
                new Rectangle(0, 0, 1, 1), topBright);
            sb.Draw(px, new Rectangle(rect.X, rect.Y + 3, topLineW - 4, 1),
                new Rectangle(0, 0, 1, 1), topDim);

            // 顶部线内侧暗沟
            sb.Draw(px, new Rectangle(rect.X + 5, rect.Y + 4, topLineW - 14, 1),
                new Rectangle(0, 0, 1, 1), new Color(3, 5, 12) * (alpha * 0.5f));

            // 顶部流动波纹
            for (int x = rect.X; x < rect.X + topLineW; x += 4) {
                float t = (float)(x - rect.X) / topLineW;
                float wave = MathF.Sin(neonPulseTimer * 2.5f + t * MathHelper.TwoPi * 1.5f) * 0.35f + 0.65f;
                int w = Math.Min(4, rect.X + topLineW - x);
                sb.Draw(px, new Rectangle(x, rect.Y, w, 2), new Rectangle(0, 0, 1, 1),
                    new Color(80, 180, 255) * (alpha * 0.35f * wave * pulse));
            }

            // ── 右上斜切边缘（对角霓虹线：从顶部终点到右侧起点）──
            Color chamferBright = new Color(80, 170, 255) * (alpha * 0.88f * pulse);
            Color chamferGlow = new Color(50, 120, 220) * (alpha * 0.32f * pulse);
            for (int i = 0; i < trCut; i++) {
                float t = i / (float)trCut;
                float fade = 1f - t * 0.35f;
                int dx = rect.Right - trCut + i;
                int dy = rect.Y + i;
                // 主线2px
                sb.Draw(px, new Rectangle(dx, dy, 2, 1), new Rectangle(0, 0, 1, 1), chamferBright * fade);
                // 内侧辉光扩散
                if (dx - 1 >= rect.X)
                    sb.Draw(px, new Rectangle(dx - 1, dy, 1, 1), new Rectangle(0, 0, 1, 1), chamferGlow * fade);
                // 外侧辉光扩散（2层）
                sb.Draw(px, new Rectangle(dx + 2, dy, 1, 1), new Rectangle(0, 0, 1, 1), chamferGlow * fade * 0.6f);
                sb.Draw(px, new Rectangle(dx + 3, dy, 1, 1), new Rectangle(0, 0, 1, 1), chamferGlow * fade * 0.2f);
            }
            // 斜切中点发光宝石
            DrawSmallDiamond(sb, px,
                new Vector2(rect.Right - trCut * 0.45f, rect.Y + trCut * 0.45f),
                new Color(120, 200, 255) * (alpha * 0.7f * pulse), 3f);

            // ── 左侧强调竖条（4px，截止于左下斜切起点）+ 辉光扩散 ──
            int halfLeftH = leftLineH / 2;
            Color leftBright = new Color(50, 150, 255) * (alpha * 0.78f * pulse);
            Color leftDim = new Color(80, 60, 170) * (alpha * 0.38f);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 4, halfLeftH),
                new Rectangle(0, 0, 1, 1), leftBright);
            sb.Draw(px, new Rectangle(rect.X, rect.Y + halfLeftH, 4, leftLineH - halfLeftH),
                new Rectangle(0, 0, 1, 1), leftDim);
            for (int g = 1; g <= 4; g++) {
                float gAlpha = (1f - g / 5f) * 0.09f;
                int gH = Math.Max(20, halfLeftH - g * 12);
                sb.Draw(px, new Rectangle(rect.X + 4 + g, rect.Y + 4, g, gH),
                    new Rectangle(0, 0, 1, 1), new Color(45, 130, 240) * (alpha * gAlpha * pulse));
            }
            // 左侧内侧暗沟
            sb.Draw(px, new Rectangle(rect.X + 4, rect.Y + 5, 1, leftLineH - 10),
                new Rectangle(0, 0, 1, 1), new Color(3, 5, 12) * (alpha * 0.35f));

            // ── 左下斜切边缘（对角线：从左侧终点到底部起点，紫蓝渐变）──
            Color blChamferBright = new Color(90, 70, 210) * (alpha * 0.75f * pulse);
            Color blChamferGlow = new Color(55, 45, 160) * (alpha * 0.28f * pulse);
            for (int i = 0; i < blCut; i++) {
                float t = i / (float)blCut;
                float fade = 0.55f + t * 0.45f;
                int dx = rect.X + i;
                int dy = rect.Bottom - blCut + i;
                // 主线1-2px
                sb.Draw(px, new Rectangle(dx, dy, 1, 2), new Rectangle(0, 0, 1, 1), blChamferBright * fade);
                // 辉光扩散
                if (dy - 1 >= rect.Y)
                    sb.Draw(px, new Rectangle(dx, dy - 1, 1, 1), new Rectangle(0, 0, 1, 1), blChamferGlow * fade);
                sb.Draw(px, new Rectangle(dx, dy + 2, 1, 1), new Rectangle(0, 0, 1, 1), blChamferGlow * fade * 0.5f);
            }
            // 斜切中点发光宝石
            DrawSmallDiamond(sb, px,
                new Vector2(rect.X + blCut * 0.5f, rect.Bottom - blCut * 0.5f),
                new Color(130, 90, 230) * (alpha * 0.55f * pulse), 2.5f);

            // ── 右侧双线（从斜切终点开始到底部）──
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y + trCut, 1, rightLineH),
                new Rectangle(0, 0, 1, 1), new Color(50, 70, 140) * (alpha * 0.48f));
            sb.Draw(px, new Rectangle(rect.Right - 2, rect.Y + trCut + 2, 1, rightLineH - 4),
                new Rectangle(0, 0, 1, 1), new Color(40, 55, 120) * (alpha * 0.18f));
            sb.Draw(px, new Rectangle(rect.Right - 3, rect.Y + trCut + 5, 1, rightLineH - 10),
                new Rectangle(0, 0, 1, 1), new Color(2, 3, 8) * (alpha * 0.25f));

            // ── 底部三层线（从斜切终点开始到右侧）──
            sb.Draw(px, new Rectangle(rect.X + blCut, rect.Bottom - 1, botLineW, 1),
                new Rectangle(0, 0, 1, 1), new Color(40, 80, 180) * (alpha * 0.48f));
            sb.Draw(px, new Rectangle(rect.X + blCut, rect.Bottom - 2, botLineW, 1),
                new Rectangle(0, 0, 1, 1), new Color(80, 55, 150) * (alpha * 0.25f));
            sb.Draw(px, new Rectangle(rect.X + blCut + 5, rect.Bottom - 3, botLineW - 10, 1),
                new Rectangle(0, 0, 1, 1), new Color(2, 3, 8) * (alpha * 0.3f));

            // ── 顶部左侧刻痕（机械装饰感）──
            sb.Draw(px, new Rectangle(rect.X + 5, rect.Y, 1, 10), new Rectangle(0, 0, 1, 1), topBright * 0.8f);
            sb.Draw(px, new Rectangle(rect.X + 20, rect.Y, 1, 7), new Rectangle(0, 0, 1, 1), topBright * 0.5f);
            sb.Draw(px, new Rectangle(rect.X + 34, rect.Y, 1, 4), new Rectangle(0, 0, 1, 1), topBright * 0.3f);

            // ── 右上斜切角内侧暗沟（增强切面立体感）──
            for (int i = 1; i < trCut - 1; i++) {
                float t = i / (float)trCut;
                int dx = rect.Right - trCut + i - 2;
                int dy = rect.Y + i + 1;
                if (dx >= rect.X)
                    sb.Draw(px, new Rectangle(dx, dy, 1, 1), new Rectangle(0, 0, 1, 1),
                        new Color(2, 3, 8) * (alpha * 0.3f * (1f - t)));
            }
        }

        /// <summary>
        /// 左上角四角星纹样（加大、加亮、双层旋转，增强存在感）
        /// </summary>
        private void DrawCornerStarSymbol(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            Vector2 center = new(rect.X + 16, rect.Y + 16);
            float pulse = MathF.Sin(neonPulseTimer * 1.5f) * 0.2f + 0.8f;
            Color starColor = new Color(80, 180, 255) * (alpha * 0.9f * pulse);

            //外层星（反向慢转，营造双层旋转机械感）
            float rot2 = -starSpinTimer * 0.4f;
            for (int i = 0; i < 4; i++) {
                float angle = rot2 + i * MathHelper.PiOver2;
                sb.Draw(px, center, null, starColor * 0.35f, angle, new Vector2(0.5f, 0f),
                    new Vector2(1.2f, 12f), SpriteEffects.None, 0f);
            }

            //内层四角星（4条射线从中心向外延伸，缓慢旋转）
            float rot = starSpinTimer;
            float armLen = 9f;
            float armWidth = 1.8f;
            for (int i = 0; i < 4; i++) {
                float angle = rot + i * MathHelper.PiOver2;
                sb.Draw(px, center, null, starColor, angle, new Vector2(0.5f, 0f),
                    new Vector2(armWidth, armLen), SpriteEffects.None, 0f);
            }

            //中心亮点（加大）
            sb.Draw(px, center, null, starColor * 1.3f, 0f, new Vector2(0.5f),
                new Vector2(3.2f), SpriteEffects.None, 0f);

            //外圈环形（12段弧环，更密更完整）
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
        /// 角饰装饰（适配斜切六角轮廓：右上/左下角改为沿斜切边的平行纹饰，保留右下角L角）
        /// </summary>
        private void DrawMetallicFrillCorners(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float pulse = MathF.Sin(neonPulseTimer * 0.8f) * 0.12f + 0.88f;
            Color frillBase = new Color(70, 110, 180) * (alpha * 0.55f * pulse);
            Color frillHighlight = new Color(130, 170, 240) * (alpha * 0.4f * pulse);

            //右上角：沿斜切边平行的短纹饰（呼应chamfer对角线）
            int trCut = TopRightChamfer;
            for (int i = 0; i < 5; i++) {
                float t = (i + 1) / 6f;
                int startX = rect.Right - (int)(trCut * (1f - t));
                int startY = rect.Y + (int)(trCut * t);
                int segLen = 6 - i;
                // 沿斜切方向的平行短线（偏移到内侧）
                for (int s = 0; s < segLen; s++) {
                    sb.Draw(px, new Rectangle(startX - 6 + s, startY + s, 1, 1),
                        new Rectangle(0, 0, 1, 1), frillBase * (1f - i * 0.15f));
                }
            }

            //左下角：沿斜切边平行的短纹饰
            int blCut = BottomLeftChamfer;
            for (int i = 0; i < 4; i++) {
                float t = (i + 1) / 5f;
                int startX = rect.X + (int)(blCut * t);
                int startY = rect.Bottom - (int)(blCut * (1f - t));
                int segLen = 5 - i;
                for (int s = 0; s < segLen; s++) {
                    sb.Draw(px, new Rectangle(startX + 5 + s, startY - 5 + s, 1, 1),
                        new Rectangle(0, 0, 1, 1), frillBase * (1f - i * 0.16f));
                }
            }

            //右下角：对角短线+L角装饰（保留，这个角没有斜切）
            for (int i = 0; i < 6; i++) {
                sb.Draw(px, new Rectangle(rect.Right - 8 - i * 2, rect.Bottom - 7 + i, 4, 1),
                    new Rectangle(0, 0, 1, 1), frillBase * (1f - i * 0.15f));
            }
            sb.Draw(px, new Vector2(rect.Right - 9, rect.Bottom - 7), null, frillHighlight,
                0f, new Vector2(0.5f), 1.6f, SpriteEffects.None, 0f);

            //底部装饰褶边条（从斜切终点开始，适配新底线起始位置）
            Color bottomFrill = new Color(60, 90, 160) * (alpha * 0.25f * pulse);
            for (int x = rect.X + blCut + 8; x < rect.Right - 28; x += 4) {
                int h = (x % 8 == 0) ? 4 : 2;
                sb.Draw(px, new Rectangle(x, rect.Bottom - 3 - h, 1, h),
                    new Rectangle(0, 0, 1, 1), bottomFrill);
            }
        }

        /// <summary>
        /// 右侧装饰条纹（竖向刻度尺+霓虹流光，填充右侧空白区域）
        /// </summary>
        private void DrawRightOrnament(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            int rx = rect.Right - 12;
            int spacing = 10;
            int marks = rect.Height / spacing;
            float flow = neonPulseTimer * 0.3f;

            //竖向刻度尺（等距短横线，每4格加长，参考嘉登右侧刻度尺的设计）
            for (int i = 0; i < marks; i++) {
                float t = (float)i / marks;
                float bright = MathF.Sin((t + flow) * MathHelper.TwoPi) * 0.3f + 0.45f;
                int mLen = (i % 4 == 0) ? 8 : 4;
                Color mc = Color.Lerp(new Color(50, 130, 255), new Color(100, 70, 200), t)
                    * (alpha * bright * 0.55f);
                sb.Draw(px, new Rectangle(rx - mLen, rect.Y + i * spacing + 6, mLen, 1),
                    new Rectangle(0, 0, 1, 1), mc);
            }

            //右侧竖向流光线（1px常驻底线+流动亮点）
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
        /// 四角状态文字（适配斜切轮廓：右上/左下文字内缩避开斜切区域）
        /// </summary>
        private void DrawCornerStatusText(SpriteBatch sb, Rectangle rect, float alpha) {
            if (alpha < 0.04f) return;
            float blink = MathF.Sin(neonPulseTimer * 0.75f) * 0.15f + 0.85f;
            Color col = new Color(80, 140, 230) * (alpha * 0.5f * blink);
            float sc = 0.5f;
            var font = Terraria.GameContent.FontAssets.MouseText.Value;

            //左上（星形符号旁偏移，这个角没有斜切）
            Utils.DrawBorderString(sb, cornerStatus[0],
                new Vector2(rect.X + 28f, rect.Y + 7f), col, sc);
            //右上（内缩避开斜切区域）
            float w1 = font.MeasureString(cornerStatus[1]).X * sc;
            Utils.DrawBorderString(sb, cornerStatus[1],
                new Vector2(rect.Right - w1 - 16f - TopRightChamfer * 0.6f, rect.Y + 7f), col, sc);
            //左下（内缩避开斜切区域）
            Utils.DrawBorderString(sb, cornerStatus[2],
                new Vector2(rect.X + 8f + BottomLeftChamfer * 0.7f, rect.Bottom - 16f), col * 0.65f, sc);
            //右下（这个角没有斜切）
            float w3 = font.MeasureString(cornerStatus[3]).X * sc;
            Utils.DrawBorderString(sb, cornerStatus[3],
                new Vector2(rect.Right - w3 - 16f, rect.Bottom - 16f), col * 0.65f, sc);
        }

        /// <summary>
        /// 矩形线框（修正拐角重叠：左右侧线避开上下线区域）
        /// </summary>
        private static void DrawRect(SpriteBatch sb, Texture2D px, Rectangle r, int bw, Color c) {
            sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, bw), new Rectangle(0, 0, 1, 1), c);
            sb.Draw(px, new Rectangle(r.X, r.Bottom - bw, r.Width, bw), new Rectangle(0, 0, 1, 1), c * 0.7f);
            int innerH = r.Height - bw * 2;
            if (innerH > 0) {
                sb.Draw(px, new Rectangle(r.X, r.Y + bw, bw, innerH), new Rectangle(0, 0, 1, 1), c * 0.85f);
                sb.Draw(px, new Rectangle(r.Right - bw, r.Y + bw, bw, innerH), new Rectangle(0, 0, 1, 1), c * 0.85f);
            }
        }

        /// <summary>
        /// 小菱形点缀（由两个十字旋转45°叠加）
        /// </summary>
        private static void DrawSmallDiamond(SpriteBatch sb, Texture2D px, Vector2 pos, Color c, float size) {
            sb.Draw(px, pos, null, c, MathHelper.PiOver4, new Vector2(0.5f),
                new Vector2(size, size * 0.3f), SpriteEffects.None, 0f);
            sb.Draw(px, pos, null, c * 0.8f, MathHelper.PiOver4 + MathHelper.PiOver2, new Vector2(0.5f),
                new Vector2(size, size * 0.3f), SpriteEffects.None, 0f);
        }

        #endregion
    }
}
