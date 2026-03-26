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

        //粒子系统
        private readonly List<NeonMaidPRT> neonParticles = [];
        private int neonParticleSpawnTimer;
        private readonly List<CircuitNodePRT> circuitNodes = [];
        private int circuitNodeSpawnTimer;
        private const float SideMargin = 24f;

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

            //霓虹粒子（间隔32帧，上限8个）
            neonParticleSpawnTimer++;
            if (Active && neonParticleSpawnTimer >= 32 && neonParticles.Count < 8) {
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

            //电路节点（间隔40帧，上限4个，密度低于嘉登保持优雅感）
            circuitNodeSpawnTimer++;
            if (Active && circuitNodeSpawnTimer >= 40 && circuitNodes.Count < 4) {
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

            //多层扩散阴影（偏紫黑色调）
            for (int d = 10; d >= 1; d--) {
                Rectangle s = panelRect;
                s.Inflate(d, d);
                s.Offset(4, 6);
                spriteBatch.Draw(px, s, new Rectangle(0, 0, 1, 1),
                    new Color(8, 4, 16) * (alpha * 0.06f * (10f - d) / 10f));
            }

            //深紫黑渐变背景+二进制矩阵底纹
            DrawCyberMaidBackground(spriteBatch, panelRect, alpha);

            //左侧霓虹数据流线
            DrawLeftDataLines(spriteBatch, panelRect, alpha);

            //缓慢扫描线
            float scanY = panelRect.Y + sweepTimer * panelRect.Height;
            for (int row = 0; row <= 2; row++) {
                float iy = scanY + row * 1.5f;
                if (iy < panelRect.Y || iy > panelRect.Bottom) continue;
                float fade = 1f - row * 0.35f;
                spriteBatch.Draw(px,
                    new Rectangle(panelRect.X + 10, (int)iy, panelRect.Width - 20, 1),
                    new Rectangle(0, 0, 1, 1),
                    new Color(60, 120, 220) * (alpha * 0.12f * fade));
            }

            //霓虹呼吸光条边框
            DrawNeonFrame(spriteBatch, panelRect, alpha);

            //左上角四角星纹样
            DrawCornerStarSymbol(spriteBatch, panelRect, alpha);

            //其余三角的金属褶边角饰
            DrawMetallicFrillCorners(spriteBatch, panelRect, alpha);

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
        /// 深紫黑渐变背景+极细二进制矩阵底纹+全息闪烁叠层
        /// </summary>
        private void DrawCyberMaidBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //纵向渐变（深紫黑到深海蓝，30段）
            int segs = 30;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = rect.Y + (int)(t * rect.Height);
                int y2 = rect.Y + (int)(t2 * rect.Height);
                float pulse = MathF.Sin(neonPulseTimer * 0.45f + t * 1.8f) * 0.5f + 0.5f;

                //从顶部深紫黑过渡到底部深海蓝
                Color topDark = new Color(6, 4, 14);
                Color botDark = new Color(4, 8, 20);
                Color mid = Color.Lerp(topDark, botDark, t);
                Color bright = Color.Lerp(new Color(12, 10, 28), new Color(10, 18, 36), t);
                Color c = Color.Lerp(mid, bright, pulse * 0.4f) * (alpha * 0.82f);

                sb.Draw(px, new Rectangle(rect.X, y1, rect.Width, Math.Max(1, y2 - y1)),
                    new Rectangle(0, 0, 1, 1), c);
            }

            //二进制矩阵底纹（极其微弱的竖向点阵，模拟数字矩阵效果）
            //用稀疏的点来暗示，不逐像素绘制以控制性能
            int gridSpacingX = 14;
            int gridSpacingY = 10;
            float matrixPhase = dataFlowTimer * 8f;
            Color matrixColor = new Color(40, 60, 120) * (alpha * 0.025f);
            for (int col = 0; col < rect.Width / gridSpacingX; col++) {
                int cx = rect.X + col * gridSpacingX + 6;
                if (cx >= rect.Right - 6) continue;
                //每列有不同的流动相位
                float colPhase = matrixPhase + col * 0.7f;
                for (int row = 0; row < rect.Height / gridSpacingY; row++) {
                    int cy = rect.Y + row * gridSpacingY + 4;
                    if (cy >= rect.Bottom - 4) continue;
                    //伪随机显隐（基于位置和时间的简单哈希）
                    float hash = MathF.Sin(col * 13.7f + row * 7.3f + colPhase) * 0.5f + 0.5f;
                    if (hash > 0.65f) {
                        sb.Draw(px, new Rectangle(cx, cy, 1, 1),
                            new Rectangle(0, 0, 1, 1), matrixColor * hash);
                    }
                }
            }

            //全息闪烁叠层（偏紫色）
            float flicker = MathF.Sin(holoFlicker * 1.5f) * 0.5f + 0.5f;
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1),
                new Color(12, 8, 28) * (alpha * 0.15f * flicker));
        }

        /// <summary>
        /// 左侧边缘的竖向霓虹数据流线（3条，不同相位）
        /// </summary>
        private void DrawLeftDataLines(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            //3条竖线分布在左侧边缘附近
            int[] xOffsets = [8, 13, 19];

            for (int lineIdx = 0; lineIdx < 3; lineIdx++) {
                int lx = rect.X + xOffsets[lineIdx];
                float phase = dataLinePhases[lineIdx];
                int lineLen = (int)(rect.Height * 0.4f);
                int startY = rect.Y + (int)(phase * rect.Height);

                for (int dy = 0; dy < lineLen; dy++) {
                    int py = startY + dy;
                    //循环回到顶部
                    if (py > rect.Bottom) py -= rect.Height;
                    if (py < rect.Y || py >= rect.Bottom) continue;

                    float t = dy / (float)lineLen;
                    float brightness = MathF.Sin(t * MathHelper.Pi) * 0.6f + 0.15f;
                    Color c = Color.Lerp(new Color(50, 130, 255), new Color(120, 80, 220), t)
                        * (alpha * brightness * 0.35f);
                    sb.Draw(px, new Rectangle(lx, py, 1, 1), new Rectangle(0, 0, 1, 1), c);
                }
            }
        }

        /// <summary>
        /// 霓虹呼吸光条边框：光条亮度带有脉冲变化，不均匀流动感
        /// </summary>
        private void DrawNeonFrame(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float pulse = MathF.Sin(neonPulseTimer * 1.1f) * 0.25f + 0.75f;

            //主霓虹蓝色调
            Color neonBright = new Color(50, 150, 255) * (alpha * 0.9f * pulse);
            Color neonDim = new Color(30, 80, 180) * (alpha * 0.5f);
            Color neonVioletDim = new Color(90, 60, 160) * (alpha * 0.3f);

            //顶部边（最亮，2px+1px流动变化层）
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 2), new Rectangle(0, 0, 1, 1), neonBright);
            //顶部流动亮暗变化（每段4px检查亮度）
            for (int x = rect.X; x < rect.Right; x += 4) {
                float t = (float)(x - rect.X) / rect.Width;
                float wave = MathF.Sin(neonPulseTimer * 2.5f + t * MathHelper.TwoPi * 1.5f) * 0.3f + 0.7f;
                int w = Math.Min(4, rect.Right - x);
                sb.Draw(px, new Rectangle(x, rect.Y + 2, w, 1), new Rectangle(0, 0, 1, 1),
                    neonBright * wave * 0.6f);
            }

            //底部边（较暗，1px实线+紫色调）
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), neonDim);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), neonVioletDim);

            //左侧边（较亮，2px）
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 2, rect.Height),
                new Rectangle(0, 0, 1, 1), neonBright * 0.85f);

            //右侧边（较暗，1px）
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height),
                new Rectangle(0, 0, 1, 1), neonDim * 0.8f);
        }

        /// <summary>
        /// 左上角四角星纹样（类似领口透镜的赛博符号，旋转发光）
        /// </summary>
        private void DrawCornerStarSymbol(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            Vector2 center = new(rect.X + 14, rect.Y + 14);
            float pulse = MathF.Sin(neonPulseTimer * 1.5f) * 0.25f + 0.75f;
            Color starColor = new Color(80, 180, 255) * (alpha * 0.85f * pulse);

            //四角星：4条射线从中心向外延伸，缓慢旋转
            float rot = starSpinTimer;
            float armLen = 7f;
            float armWidth = 1.5f;
            for (int i = 0; i < 4; i++) {
                float angle = rot + i * MathHelper.PiOver2;
                sb.Draw(px, center, null, starColor, angle, new Vector2(0.5f, 0f),
                    new Vector2(armWidth, armLen), SpriteEffects.None, 0f);
            }

            //中心亮点
            sb.Draw(px, center, null, starColor * 1.2f, 0f, new Vector2(0.5f),
                new Vector2(2.5f), SpriteEffects.None, 0f);

            //外圈同心圆（像素化的环形，用4条短弧段模拟）
            float ringRadius = 10f;
            Color ringColor = new Color(60, 140, 240) * (alpha * 0.35f * pulse);
            for (int i = 0; i < 8; i++) {
                float angle = rot * 0.5f + i * MathHelper.PiOver4;
                Vector2 rp = center + angle.ToRotationVector2() * ringRadius;
                sb.Draw(px, rp, null, ringColor, angle, new Vector2(0.5f),
                    new Vector2(3f, 0.8f), SpriteEffects.None, 0f);
            }
        }

        /// <summary>
        /// 其余三角的金属褶边角饰（像素化的硬质女仆装花边纹理）
        /// </summary>
        private void DrawMetallicFrillCorners(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float pulse = MathF.Sin(neonPulseTimer * 0.8f) * 0.15f + 0.85f;
            Color frillBase = new Color(70, 100, 160) * (alpha * 0.45f * pulse);
            Color frillHighlight = new Color(120, 160, 230) * (alpha * 0.3f * pulse);

            //右上角：水平+竖直短褶
            for (int i = 0; i < 5; i++) {
                int segW = 6 - i;
                //水平褶
                sb.Draw(px, new Rectangle(rect.Right - 6 - i * 3, rect.Y + 4, segW, 1),
                    new Rectangle(0, 0, 1, 1), frillBase * (1f - i * 0.15f));
                //竖直褶
                sb.Draw(px, new Rectangle(rect.Right - 4, rect.Y + 4 + i * 3, 1, segW),
                    new Rectangle(0, 0, 1, 1), frillBase * (1f - i * 0.15f));
            }
            //点缀小亮点
            sb.Draw(px, new Vector2(rect.Right - 6, rect.Y + 6), null, frillHighlight,
                0f, new Vector2(0.5f), 1.5f, SpriteEffects.None, 0f);

            //左下角：竖直短褶
            for (int i = 0; i < 4; i++) {
                int segH = 5 - i;
                sb.Draw(px, new Rectangle(rect.X + 4, rect.Bottom - 6 - i * 3, 1, segH),
                    new Rectangle(0, 0, 1, 1), frillBase * (1f - i * 0.18f));
                sb.Draw(px, new Rectangle(rect.X + 4 + i * 3, rect.Bottom - 4, segH, 1),
                    new Rectangle(0, 0, 1, 1), frillBase * (1f - i * 0.18f));
            }

            //右下角：对角短线装饰
            for (int i = 0; i < 4; i++) {
                sb.Draw(px, new Rectangle(rect.Right - 6 - i * 2, rect.Bottom - 6 + i, 3, 1),
                    new Rectangle(0, 0, 1, 1), frillBase * (1f - i * 0.2f));
            }
            sb.Draw(px, new Vector2(rect.Right - 8, rect.Bottom - 6), null, frillHighlight,
                0f, new Vector2(0.5f), 1.2f, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// 矩形线框
        /// </summary>
        private static void DrawRect(SpriteBatch sb, Texture2D px, Rectangle r, int bw, Color c) {
            sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, bw), new Rectangle(0, 0, 1, 1), c);
            sb.Draw(px, new Rectangle(r.X, r.Bottom - bw, r.Width, bw), new Rectangle(0, 0, 1, 1), c * 0.7f);
            sb.Draw(px, new Rectangle(r.X, r.Y, bw, r.Height), new Rectangle(0, 0, 1, 1), c * 0.85f);
            sb.Draw(px, new Rectangle(r.Right - bw, r.Y, bw, r.Height), new Rectangle(0, 0, 1, 1), c * 0.85f);
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
