using CalamityOverhaul.Content.QuestLogs.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityOverhaul.Content.QuestLogs.Styles
{
    public class HotwindQuestLogStyle : IQuestLogStyle
    {
        //动画计时器
        private float flowTimer;
        private float pulseTimer;
        private float bloomTimer;

        public void UpdateStyle() {
            flowTimer += 0.025f;
            if (flowTimer > MathHelper.TwoPi) flowTimer -= MathHelper.TwoPi;
            pulseTimer += 0.025f;
            bloomTimer += 0.015f;
        }

        public void DrawBackground(SpriteBatch spriteBatch, QuestLog log, Rectangle panelRect) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            bool nightMode = log.NightMode;
            float alpha = log.MainPanelAlpha;
            float pulse = (float)Math.Sin(pulseTimer * 2f) * 0.5f + 0.5f;

            //多层柔和阴影，营造悬浮厚重感
            for (int s = 3; s >= 1; s--) {
                Rectangle shadowRect = panelRect;
                shadowRect.Inflate(s * 2, s * 2);
                shadowRect.Offset(s * 3, s * 3);
                spriteBatch.Draw(pixel, shadowRect, Color.Black * (0.22f * s / 3f) * alpha);
            }

            //深色实底背景（有颜色倾向的深色，不是纯透明黑色）
            Color baseFill = nightMode ? new Color(8, 12, 22) : new Color(18, 12, 8);
            spriteBatch.Draw(pixel, panelRect, baseFill * alpha);

            //三段纵向渐变，营造从上到下的明暗层次
            Color gradTop = nightMode ? new Color(14, 20, 38) : new Color(30, 20, 12);
            Color gradMid = nightMode ? new Color(8, 14, 28) : new Color(22, 14, 8);
            Color gradBot = nightMode ? new Color(5, 8, 18) : new Color(14, 8, 5);

            int gradientSteps = 30;
            for (int i = 0; i < gradientSteps; i++) {
                float t = i / (float)gradientSteps;
                int y = panelRect.Y + (int)(t * panelRect.Height);
                int height = Math.Max(1, panelRect.Height / gradientSteps + 1);
                Rectangle gradRect = new Rectangle(panelRect.X, y, panelRect.Width, height);
                Color gradColor = t < 0.5f
                    ? Color.Lerp(gradTop, gradMid, t * 2f)
                    : Color.Lerp(gradMid, gradBot, (t - 0.5f) * 2f);
                spriteBatch.Draw(pixel, gradRect, gradColor * (0.5f * alpha));
            }

            //暗角效果：边缘渐暗，中央聚焦
            int vigBands = 8;
            int vigDepth = 50;
            for (int v = 0; v < vigBands; v++) {
                float vt = 1f - (v / (float)vigBands);
                float vAlpha = vt * vt * 0.28f * alpha;
                int bandH = vigDepth / vigBands;
                int offset = v * bandH;
                spriteBatch.Draw(pixel, new Rectangle(panelRect.X, panelRect.Y + offset, panelRect.Width, bandH), Color.Black * vAlpha);
                spriteBatch.Draw(pixel, new Rectangle(panelRect.X, panelRect.Bottom - offset - bandH, panelRect.Width, bandH), Color.Black * vAlpha);
                spriteBatch.Draw(pixel, new Rectangle(panelRect.X + offset, panelRect.Y, bandH, panelRect.Height), Color.Black * (vAlpha * 0.5f));
                spriteBatch.Draw(pixel, new Rectangle(panelRect.Right - offset - bandH, panelRect.Y, bandH, panelRect.Height), Color.Black * (vAlpha * 0.5f));
            }

            //纵向泛光动画
            DrawBloomEffect(spriteBatch, pixel, panelRect, alpha, nightMode);

            //柔和脉冲光覆盖
            Color pulseBase = nightMode ? new Color(40, 100, 200) : new Color(200, 100, 40);
            Color pulseColor = pulseBase * (0.04f * pulse * alpha);
            spriteBatch.Draw(pixel, panelRect, pulseColor);

            //外边框——斜面浮雕效果（上/左高光，下/右阴影）
            int outerBorder = 3;
            Color highlightColor = nightMode
                ? Color.Lerp(new Color(55, 110, 190), new Color(75, 140, 220), pulse)
                : Color.Lerp(new Color(170, 110, 55), new Color(200, 140, 75), pulse);
            Color shadowColor = nightMode
                ? new Color(8, 16, 40)
                : new Color(35, 20, 8);

            spriteBatch.Draw(pixel, new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, outerBorder), highlightColor * (0.9f * alpha));
            spriteBatch.Draw(pixel, new Rectangle(panelRect.X, panelRect.Y, outerBorder, panelRect.Height), highlightColor * (0.7f * alpha));
            spriteBatch.Draw(pixel, new Rectangle(panelRect.X, panelRect.Bottom - outerBorder, panelRect.Width, outerBorder), shadowColor * (0.95f * alpha));
            spriteBatch.Draw(pixel, new Rectangle(panelRect.Right - outerBorder, panelRect.Y, outerBorder, panelRect.Height), shadowColor * (0.85f * alpha));

            //内凹边框——反向斜面，营造内嵌内容区域感
            Rectangle innerRect = panelRect;
            innerRect.Inflate(-8, -8);
            int innerBorder = 2;
            Color innerShadow = nightMode ? new Color(3, 6, 14) : new Color(10, 6, 3);
            Color innerHighlight = nightMode
                ? new Color(35, 70, 140) * (0.35f + 0.12f * pulse)
                : new Color(110, 70, 30) * (0.35f + 0.12f * pulse);

            spriteBatch.Draw(pixel, new Rectangle(innerRect.X, innerRect.Y, innerRect.Width, innerBorder), innerShadow * (0.8f * alpha));
            spriteBatch.Draw(pixel, new Rectangle(innerRect.X, innerRect.Y, innerBorder, innerRect.Height), innerShadow * (0.7f * alpha));
            spriteBatch.Draw(pixel, new Rectangle(innerRect.X, innerRect.Bottom - innerBorder, innerRect.Width, innerBorder), innerHighlight * alpha);
            spriteBatch.Draw(pixel, new Rectangle(innerRect.Right - innerBorder, innerRect.Y, innerBorder, innerRect.Height), innerHighlight * (0.85f * alpha));

            //角落装饰
            DrawCornerMark(spriteBatch, new Vector2(panelRect.X + 14, panelRect.Y + 14), pulse, alpha, nightMode);
            DrawCornerMark(spriteBatch, new Vector2(panelRect.Right - 14, panelRect.Y + 14), pulse, alpha, nightMode);
            DrawCornerMark(spriteBatch, new Vector2(panelRect.X + 14, panelRect.Bottom - 14), pulse * 0.7f, alpha, nightMode);
            DrawCornerMark(spriteBatch, new Vector2(panelRect.Right - 14, panelRect.Bottom - 14), pulse * 0.7f, alpha, nightMode);
        }

        private void DrawBloomEffect(SpriteBatch spriteBatch, Texture2D pixel, Rectangle panelRect, float alphaMult, bool nightMode) {
            //简化的双层泛光效果，柔和但有存在感
            int bloomLayers = 2;

            for (int layer = 0; layer < bloomLayers; layer++) {
                float layerSpeed = 0.6f + layer * 0.2f;
                float layerOffset = (bloomTimer * layerSpeed + layer * 1.5f) % MathHelper.TwoPi;
                float bloomPosition = (float)Math.Sin(layerOffset) * 0.5f + 0.5f;

                int centerX = panelRect.X + (int)(bloomPosition * panelRect.Width);
                int bloomWidth = 200 + layer * 60;
                int bloomSteps = 30;

                for (int i = 0; i < bloomSteps; i++) {
                    float t = i / (float)bloomSteps;
                    float distance = Math.Abs(t - 0.5f) * 2f;
                    float alpha = 1f - distance;
                    alpha = (float)Math.Pow(alpha, 4f);

                    int x = centerX - bloomWidth / 2 + (int)(t * bloomWidth);
                    if (x < panelRect.X || x >= panelRect.Right) continue;

                    int width = Math.Max(1, bloomWidth / bloomSteps + 1);
                    Rectangle bloomRect = new Rectangle(x, panelRect.Y, width, panelRect.Height);

                    Color bloomColor1 = nightMode ? new Color(18, 55, 140) : new Color(150, 65, 18);
                    Color bloomColor2 = nightMode ? new Color(35, 90, 170) : new Color(190, 100, 35);
                    float colorPhase = (t + layer * 0.3f) % 1f;
                    Color finalColor = Color.Lerp(bloomColor1, bloomColor2, colorPhase);

                    float layerAlpha = 0.055f - layer * 0.015f;
                    spriteBatch.Draw(pixel, bloomRect, finalColor * (alpha * layerAlpha * alphaMult));
                }
            }
        }

        public void DrawNode(SpriteBatch spriteBatch, QuestNode node, Vector2 drawPos, float scale, bool isHovered, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            int size = (int)(48 * scale);
            Rectangle nodeRect = new Rectangle((int)drawPos.X - size / 2, (int)drawPos.Y - size / 2, size, size);

            //更深沉的基色调
            Color baseColor = node.IsCompleted ? new Color(50, 150, 70) :
                             (node.IsUnlocked ? new Color(180, 100, 40) : new Color(55, 55, 65));
            Color baseDark = node.IsCompleted ? new Color(25, 80, 35) :
                             (node.IsUnlocked ? new Color(100, 55, 20) : new Color(32, 32, 40));

            if (isHovered) {
                baseColor = Color.Lerp(baseColor, Color.White, 0.35f);
                baseDark = Color.Lerp(baseDark, Color.White, 0.2f);
            }

            //双层柔和阴影
            Rectangle shadow2 = nodeRect;
            shadow2.Offset(5, 5);
            shadow2.Inflate(2, 2);
            spriteBatch.Draw(pixel, shadow2, Color.Black * 0.3f * alpha);
            Rectangle shadow1 = nodeRect;
            shadow1.Offset(3, 3);
            spriteBatch.Draw(pixel, shadow1, Color.Black * 0.55f * alpha);

            //节点深色底
            spriteBatch.Draw(pixel, nodeRect, baseDark * alpha);

            //内部渐变——上亮下暗
            int gradSteps = 8;
            for (int g = 0; g < gradSteps; g++) {
                float gt = g / (float)gradSteps;
                int gy = nodeRect.Y + (int)(gt * nodeRect.Height);
                int gh = Math.Max(1, nodeRect.Height / gradSteps + 1);
                Color gc = Color.Lerp(baseColor, baseDark, gt);
                spriteBatch.Draw(pixel, new Rectangle(nodeRect.X, gy, nodeRect.Width, gh), gc * (0.6f * alpha));
            }

            //宽柔光环
            if (node.IsUnlocked || node.IsCompleted) {
                float glowPulse = (float)Math.Sin(Main.GameUpdateCount * 0.05f) * 0.5f + 0.5f;
                Color glowColor = node.IsCompleted ? new Color(70, 200, 90) : new Color(200, 130, 60);

                Rectangle glowRect = nodeRect;
                glowRect.Inflate(4, 4);
                spriteBatch.Draw(pixel, glowRect, glowColor * (0.15f * alpha));
                glowRect.Inflate(3, 3);
                spriteBatch.Draw(pixel, glowRect, glowColor * (0.07f * glowPulse * alpha));
            }

            //绘制任务图标
            DrawQuestIcon(spriteBatch, node, drawPos, scale, alpha);

            //斜面浮雕边框
            int borderWidth = 2;
            Color edgeHighlight = node.IsCompleted ? new Color(110, 240, 130) :
                             (node.IsUnlocked ? new Color(240, 160, 80) : new Color(100, 100, 115));
            Color edgeShadow = node.IsCompleted ? new Color(28, 90, 38) :
                             (node.IsUnlocked ? new Color(90, 45, 12) : new Color(38, 38, 46));

            if (isHovered) {
                edgeHighlight = Color.White;
                edgeShadow = new Color(140, 140, 150);
                borderWidth = 3;
            }

            //上（高光）、左（高光）
            spriteBatch.Draw(pixel, new Rectangle(nodeRect.X, nodeRect.Y, nodeRect.Width, borderWidth), edgeHighlight * alpha);
            spriteBatch.Draw(pixel, new Rectangle(nodeRect.X, nodeRect.Y, borderWidth, nodeRect.Height), edgeHighlight * (0.85f * alpha));
            //下（阴影）、右（阴影）
            spriteBatch.Draw(pixel, new Rectangle(nodeRect.X, nodeRect.Bottom - borderWidth, nodeRect.Width, borderWidth), edgeShadow * alpha);
            spriteBatch.Draw(pixel, new Rectangle(nodeRect.Right - borderWidth, nodeRect.Y, borderWidth, nodeRect.Height), edgeShadow * (0.9f * alpha));

            Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(node.DisplayName?.Value) * 0.75f;
            //绘制节点名称
            Vector2 namePos = new Vector2(drawPos.X, drawPos.Y + size / 2 + 8);

            Color textColor = node.IsCompleted ? new Color(120, 230, 145) :
                             (node.IsUnlocked ? new Color(235, 185, 125) : new Color(125, 125, 140));

            if (isHovered) {
                textColor = Color.White;
            }

            Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, node.DisplayName?.Value,
                namePos.X, namePos.Y, textColor * alpha, Color.Black * alpha, nameSize / 2, 0.75f);
        }

        public void DrawConnection(SpriteBatch spriteBatch, Vector2 start, Vector2 end, bool isUnlocked, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            Vector2 diff = end - start;
            float length = diff.Length();
            float rotation = diff.ToRotation();

            //加粗的连接线宽度
            int lineWidth = 8;

            //绘制外层阴影
            Color shadowColor = Color.Black * 0.4f;
            spriteBatch.Draw(pixel, start + new Vector2(2, 2).RotatedBy(rotation),
                new Rectangle(0, 0, (int)length, lineWidth), shadowColor * alpha, rotation,
                new Vector2(0, lineWidth / 2f), 1f, SpriteEffects.None, 0f);

            //绘制基础暗色背景层
            Color lineColor = isUnlocked ? new Color(60, 45, 30) : new Color(40, 40, 45);
            spriteBatch.Draw(pixel, start, new Rectangle(0, 0, (int)length, lineWidth),
                lineColor * 0.9f * alpha, rotation, new Vector2(0, lineWidth / 2f), 1f, SpriteEffects.None, 0f);

            if (isUnlocked) {
                //绘制主动流动的渐变动画
                DrawFlowingGradient(spriteBatch, pixel, start, end, length, rotation, lineWidth, alpha);

                //绘制外发光效果
                Color glowColor = new Color(255, 140, 60) * 0.3f;
                int glowWidth = lineWidth + 6;
                spriteBatch.Draw(pixel, start, new Rectangle(0, 0, (int)length, glowWidth),
                    glowColor * alpha, rotation, new Vector2(0, glowWidth / 2f), 1f, SpriteEffects.None, 0f);
            }
            else {
                //未解锁状态的暗淡虚线效果
                DrawDashedLine(spriteBatch, pixel, start, length, rotation, lineWidth, alpha);
            }
        }

        private void DrawFlowingGradient(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, float length, float rotation, int lineWidth, float alpha) {
            //创建持续流动的渐变效果，从起点流向终点
            int segments = Math.Max((int)(length / 12f), 3);

            //流动偏移，确保是从0到1的连续运动
            float flowProgress = (flowTimer * 0.2f) % 1f;

            for (int i = 0; i < segments; i++) {
                float t = (float)i / segments;
                float dist = t * length;
                Vector2 pos = start + new Vector2(dist, 0).RotatedBy(rotation);

                //计算流动亮度
                float wave = (float)Math.Sin((t - flowProgress) * MathHelper.TwoPi * 2f);
                float brightness = (wave * 0.5f + 0.5f);

                Color color = Color.Lerp(new Color(150, 80, 40), new Color(255, 180, 80), brightness);

                spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, (int)(length / segments) + 1, lineWidth),
                    color * alpha, rotation, new Vector2(0, lineWidth / 2f), 1f, SpriteEffects.None, 0f);
            }

            //添加流动的能量脉冲点
            int pulseCount = Math.Max((int)(length / 60f), 2);
            for (int i = 0; i < pulseCount; i++) {
                float t = ((flowTimer * 0.5f + i * (1f / pulseCount)) % 1f);
                Vector2 pos = Vector2.Lerp(start, end, t);

                float size = 4f + (float)Math.Sin(flowTimer * 5f) * 2f;
                spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), Color.White * alpha, rotation,
                    new Vector2(0.5f, 0.5f), new Vector2(size * 2f, size), SpriteEffects.None, 0f);
            }
        }

        private void DrawDashedLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, float length, float rotation, int lineWidth, float alpha) {
            //绘制虚线效果表示未解锁
            int dashLength = 14;
            int gapLength = 10;
            int totalLength = dashLength + gapLength;
            int dashCount = (int)(length / totalLength);

            for (int i = 0; i < dashCount; i++) {
                float dashStart = i * totalLength;
                Vector2 dashPos = start + new Vector2(dashStart, 0).RotatedBy(rotation);

                Color dashColor = new Color(70, 70, 80) * 0.6f * alpha;
                spriteBatch.Draw(pixel, dashPos, new Rectangle(0, 0, dashLength, lineWidth),
                    dashColor, rotation, new Vector2(0, lineWidth / 2f), 1f, SpriteEffects.None, 0f);
            }
        }

        public Vector4 GetPadding() {
            return new Vector4(15, 35, 15, 15);
        }

        public Rectangle GetCloseButtonRect(Rectangle panelRect) {
            return new Rectangle(
                panelRect.Right - 40,
                panelRect.Y + 10,
                30,
                30
            );
        }

        public Rectangle GetRewardButtonRect(Rectangle panelRect) {
            int padding = 20;
            return new Rectangle(
                panelRect.X + panelRect.Width / 2 - 60,
                panelRect.Bottom - padding - 40,
                120,
                35
            );
        }

        public void DrawQuestDetail(SpriteBatch spriteBatch, QuestNode node, Rectangle panelRect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;

            //绘制半透明背景遮罩
            Rectangle fullScreen = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
            spriteBatch.Draw(pixel, fullScreen, Color.Black * (0.65f * alpha));

            //多层柔和阴影
            for (int s = 3; s >= 1; s--) {
                Rectangle shadowRect = panelRect;
                shadowRect.Inflate(s * 2, s * 2);
                shadowRect.Offset(s * 3, s * 3);
                spriteBatch.Draw(pixel, shadowRect, Color.Black * (0.25f * s / 3f) * alpha);
            }

            //深色实底背景
            spriteBatch.Draw(pixel, panelRect, new Color(14, 10, 6) * alpha);

            //多段纵向渐变
            int gradSteps = 25;
            for (int i = 0; i < gradSteps; i++) {
                float t = i / (float)gradSteps;
                int y = panelRect.Y + (int)(t * panelRect.Height);
                int h = Math.Max(1, panelRect.Height / gradSteps + 1);
                Rectangle gRect = new Rectangle(panelRect.X, y, panelRect.Width, h);
                Color gTop = new Color(30, 20, 12);
                Color gMid = new Color(22, 14, 8);
                Color gBot = new Color(12, 8, 5);
                Color gColor = t < 0.5f
                    ? Color.Lerp(gTop, gMid, t * 2f)
                    : Color.Lerp(gMid, gBot, (t - 0.5f) * 2f);
                spriteBatch.Draw(pixel, gRect, gColor * (0.5f * alpha));
            }

            //暗角效果
            int vigBands = 6;
            int vigDepth = 35;
            for (int v = 0; v < vigBands; v++) {
                float vt = 1f - (v / (float)vigBands);
                float vAlpha = vt * vt * 0.25f * alpha;
                int bandH = vigDepth / vigBands;
                int offset = v * bandH;
                spriteBatch.Draw(pixel, new Rectangle(panelRect.X, panelRect.Y + offset, panelRect.Width, bandH), Color.Black * vAlpha);
                spriteBatch.Draw(pixel, new Rectangle(panelRect.X, panelRect.Bottom - offset - bandH, panelRect.Width, bandH), Color.Black * vAlpha);
                spriteBatch.Draw(pixel, new Rectangle(panelRect.X + offset, panelRect.Y, bandH, panelRect.Height), Color.Black * (vAlpha * 0.45f));
                spriteBatch.Draw(pixel, new Rectangle(panelRect.Right - offset - bandH, panelRect.Y, bandH, panelRect.Height), Color.Black * (vAlpha * 0.45f));
            }

            //斜面浮雕外边框
            float pulse = (float)Math.Sin(pulseTimer * 2.5f) * 0.5f + 0.5f;
            Color highlightEdge = Color.Lerp(new Color(190, 120, 50), new Color(220, 155, 75), pulse) * alpha;
            Color shadowEdge = new Color(45, 25, 10) * alpha;

            int border = 4;
            spriteBatch.Draw(pixel, new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, border), highlightEdge);
            spriteBatch.Draw(pixel, new Rectangle(panelRect.X, panelRect.Y, border, panelRect.Height), highlightEdge * 0.8f);
            spriteBatch.Draw(pixel, new Rectangle(panelRect.X, panelRect.Bottom - border, panelRect.Width, border), shadowEdge);
            spriteBatch.Draw(pixel, new Rectangle(panelRect.Right - border, panelRect.Y, border, panelRect.Height), shadowEdge * 0.9f);

            //内凹边框
            Rectangle innerFrame = panelRect;
            innerFrame.Inflate(-6, -6);
            Color innerDark = new Color(6, 4, 2) * (0.7f * alpha);
            Color innerLight = new Color(95, 60, 28) * (0.3f * alpha);
            spriteBatch.Draw(pixel, new Rectangle(innerFrame.X, innerFrame.Y, innerFrame.Width, 1), innerDark);
            spriteBatch.Draw(pixel, new Rectangle(innerFrame.X, innerFrame.Y, 1, innerFrame.Height), innerDark);
            spriteBatch.Draw(pixel, new Rectangle(innerFrame.X, innerFrame.Bottom - 1, innerFrame.Width, 1), innerLight);
            spriteBatch.Draw(pixel, new Rectangle(innerFrame.Right - 1, innerFrame.Y, 1, innerFrame.Height), innerLight);

            //绘制内容
            DrawDetailContent(spriteBatch, node, panelRect, alpha);
        }

        private void DrawDetailContent(SpriteBatch spriteBatch, QuestNode node, Rectangle panelRect, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            int padding = 20;
            int currentY = panelRect.Y + padding;

            //绘制任务标题
            Vector2 titlePos = new Vector2(panelRect.X + padding, currentY);
            Color titleColor = node.IsCompleted ? new Color(115, 220, 135) : new Color(225, 175, 115);
            Utils.DrawBorderString(spriteBatch, node.DisplayName?.Value, titlePos, titleColor * alpha, 1.2f);
            currentY += (int)(FontAssets.MouseText.Value.MeasureString(node.DisplayName?.Value).Y * 1.2f) + 10;

            //绘制分隔线（带凹槽效果）
            Rectangle dividerTop = new Rectangle(panelRect.X + padding, currentY, panelRect.Width - padding * 2, 1);
            Rectangle dividerBot = new Rectangle(panelRect.X + padding, currentY + 1, panelRect.Width - padding * 2, 1);
            spriteBatch.Draw(pixel, dividerTop, new Color(8, 5, 2) * (alpha * 0.8f));
            spriteBatch.Draw(pixel, dividerBot, new Color(170, 105, 45) * (alpha * 0.4f));
            currentY += 15;

            //绘制任务描述
            string description = string.IsNullOrEmpty(node.DetailedDescription?.Value) ? node.Description?.Value : node.DetailedDescription?.Value;
            if (!string.IsNullOrEmpty(description)) {
                int maxTextWidth = panelRect.Width - padding * 2;
                string[] lines = Utils.WordwrapString(description, FontAssets.MouseText.Value, (int)(maxTextWidth / 0.85f), 99, out int lineCount);
                foreach (string line in lines) {
                    if (string.IsNullOrEmpty(line)) {
                        continue;
                    }
                    Utils.DrawBorderString(spriteBatch, line.TrimEnd('-', ' '), new Vector2(panelRect.X + padding, currentY), Color.White * alpha, 0.85f);
                    currentY += (int)(FontAssets.MouseText.Value.MeasureString(line).Y * 0.85f) + 4;
                }
                currentY += 10;
            }

            //绘制任务目标
            if (node.Objectives != null && node.Objectives.Count > 0) {
                Utils.DrawBorderString(spriteBatch, QuestLog.ObjectiveText.Value + ":", new Vector2(panelRect.X + padding, currentY),
                    new Color(215, 165, 105) * alpha, 0.9f);
                currentY += 25;

                foreach (var objective in node.Objectives) {
                    string objText = $"• {objective.Description} ({objective.CurrentProgress}/{objective.RequiredProgress})";
                    Color objColor = objective.IsCompleted ? new Color(140, 255, 160) : Color.White;
                    Utils.DrawBorderString(spriteBatch, objText, new Vector2(panelRect.X + padding + 10, currentY),
                        objColor * alpha, 0.8f);

                    //如果存在目标物品，绘制图标
                    if (objective.TargetItemID > 0) {
                        //计算图标位置(文本右侧)
                        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(objText) * 0.8f;
                        Rectangle itemRect = new Rectangle(
                            (int)(panelRect.X + padding + 10 + textSize.X + 10),
                            currentY - 4,
                            24,
                            24
                        );

                        //绘制背景
                        spriteBatch.Draw(pixel, itemRect, new Color(0, 0, 0, 100) * alpha);

                        //绘制物品
                        Main.instance.LoadItem(objective.TargetItemID);
                        Texture2D itemTex = TextureAssets.Item[objective.TargetItemID].Value;
                        if (itemTex != null) {
                            Rectangle frame = Main.itemAnimations[objective.TargetItemID] != null
                                ? Main.itemAnimations[objective.TargetItemID].GetFrame(itemTex)
                                : itemTex.Frame();

                            float scale = 1f;
                            if (frame.Width > 20 || frame.Height > 20) {
                                scale = 20f / Math.Max(frame.Width, frame.Height);
                            }

                            Vector2 origin = frame.Size() / 2f;
                            Vector2 drawPos = itemRect.Center.ToVector2();
                            spriteBatch.Draw(itemTex, drawPos, frame, Color.White * alpha, 0f, origin, scale, SpriteEffects.None, 0f);
                        }

                        //悬停检测
                        if (itemRect.Contains(Main.MouseScreen.ToPoint()) && ContentSamples.ItemsByType.TryGetValue(objective.TargetItemID, out var item)) {
                            Main.HoverItem = item;
                            Main.hoverItemName = item.Name;
                        }
                    }

                    currentY += 22;
                }
                currentY += 10;
            }

            //绘制任务奖励
            if (node.Rewards != null && node.Rewards.Count > 0) {
                Utils.DrawBorderString(spriteBatch, QuestLog.RewardText.Value + ":", new Vector2(panelRect.X + padding, currentY),
                    new Color(215, 165, 105) * alpha, 0.9f);
                currentY += 25;

                int rewardX = panelRect.X + padding + 10;
                foreach (var reward in node.Rewards) {
                    //绘制奖励物品图标
                    Rectangle rewardRect = new Rectangle(rewardX, currentY, 32, 32);
                    Color rewardColor = reward.Claimed ? new Color(100, 100, 110) : new Color(255, 200, 120);

                    if (rewardRect.Contains(Main.MouseScreen.ToPoint()) && ContentSamples.ItemsByType.TryGetValue(reward.ItemType, out var item)) {
                        Main.HoverItem = item;
                        Main.hoverItemName = item.Name;
                    }

                    //绘制背景框
                    spriteBatch.Draw(pixel, rewardRect, rewardColor * (alpha * 0.3f));

                    //绘制真实物品图标
                    Main.instance.LoadItem(reward.ItemType);
                    Texture2D itemTexture = TextureAssets.Item[reward.ItemType].Value;
                    if (itemTexture != null) {
                        Rectangle frame = Main.itemAnimations[reward.ItemType] != null
                            ? Main.itemAnimations[reward.ItemType].GetFrame(itemTexture)
                            : itemTexture.Frame();

                        float scale = 1f;
                        if (frame.Width > 32 || frame.Height > 32) {
                            scale = 32f / Math.Max(frame.Width, frame.Height);
                        }

                        Vector2 itemPos = new Vector2(rewardRect.X + 16, rewardRect.Y + 16);
                        Vector2 origin = frame.Size() / 2f;

                        spriteBatch.Draw(itemTexture, itemPos, frame, Color.White * alpha, 0f, origin, scale, SpriteEffects.None, 0f);
                    }

                    //绘制奖励数量
                    string amountText = $"x{reward.Amount}";
                    Vector2 amountPos = new Vector2(rewardX + 36, currentY + 8);
                    Utils.DrawBorderString(spriteBatch, amountText, amountPos, Color.White * alpha, 0.75f);

                    rewardX += 100;
                    if (rewardX > panelRect.Right - padding - 100) {
                        rewardX = panelRect.X + padding + 10;
                        currentY += 40;
                    }
                }
                currentY += 50;
            }

            //绘制领取按钮(如果任务已完成但未领取奖励)
            if (node.IsCompleted && node.Rewards != null && node.Rewards.Exists(r => !r.Claimed)) {
                Rectangle buttonRect = GetRewardButtonRect(panelRect);

                bool hoverButton = buttonRect.Contains(Main.MouseScreen.ToPoint());
                Color buttonColor = hoverButton ? new Color(255, 180, 100) : new Color(200, 120, 60);

                spriteBatch.Draw(pixel, buttonRect, buttonColor * alpha);

                //按钮边框
                int btnBorder = 2;
                Color btnEdge = new Color(255, 200, 120) * alpha;
                spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Y, buttonRect.Width, btnBorder), btnEdge);
                spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Bottom - btnBorder, buttonRect.Width, btnBorder), btnEdge);
                spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Y, btnBorder, buttonRect.Height), btnEdge);
                spriteBatch.Draw(pixel, new Rectangle(buttonRect.Right - btnBorder, buttonRect.Y, btnBorder, buttonRect.Height), btnEdge);

                //按钮文字
                string btnText = QuestLog.ReceiveAwardText.Value;
                Vector2 btnTextSize = FontAssets.MouseText.Value.MeasureString(btnText) * 0.85f;
                Vector2 btnTextPos = new Vector2(buttonRect.X + buttonRect.Width / 2, buttonRect.Y + buttonRect.Height / 2);
                Utils.DrawBorderString(spriteBatch, btnText, btnTextPos, Color.White * alpha, 0.85f, 0.5f, 0.5f);
            }
        }

        public void DrawProgressBar(SpriteBatch spriteBatch, QuestLog log, Rectangle panelRect) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            float alpha = log.MainPanelAlpha;
            bool nightMode = log.NightMode;

            //计算进度
            int total = 0;
            int completed = 0;
            foreach (var node in QuestNode.AllQuests) {
                total++;
                if (node.IsCompleted) completed++;
            }
            float progress = total > 0 ? (float)completed / total : 0f;

            //进度条区域
            int barHeight = log.ShowProgressBar ? 24 : 8;
            int barWidth = panelRect.Width - 40;
            Rectangle barRect = new Rectangle(panelRect.X + 20, panelRect.Bottom + 10, barWidth, barHeight);

            //绘制背景
            spriteBatch.Draw(pixel, barRect, Color.Black * 0.7f * alpha);

            //绘制边框
            Color borderColor = nightMode ? new Color(60, 140, 255) : new Color(255, 140, 60);
            borderColor *= alpha;
            int border = 2;
            spriteBatch.Draw(pixel, new Rectangle(barRect.X, barRect.Y, barRect.Width, border), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(barRect.X, barRect.Bottom - border, barRect.Width, border), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(barRect.X, barRect.Y, border, barRect.Height), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(barRect.Right - border, barRect.Y, border, barRect.Height), borderColor);

            //绘制进度填充
            if (total > 0) {
                int fillWidth = (int)((barWidth - border * 2) * progress);
                Rectangle fillRect = new Rectangle(barRect.X + border, barRect.Y + border, fillWidth, barHeight - border * 2);

                //渐变填充
                Color fillColor = nightMode ? new Color(80, 160, 255) : new Color(255, 180, 60);
                fillColor *= 0.6f * alpha;
                spriteBatch.Draw(pixel, fillRect, fillColor);

                //流光效果
                float flow = (flowTimer * 2f) % 1f;
                int flowX = fillRect.X + (int)(flow * fillRect.Width);
                if (flowX < fillRect.Right) {
                    spriteBatch.Draw(pixel, new Rectangle(flowX, fillRect.Y, 2, fillRect.Height), Color.White * 0.5f * alpha);
                }
            }

            if (log.ShowProgressBar) {
                //绘制文字
                string text = $"{QuestLog.ProgressText.Value}: {completed}/{total} ({(int)(progress * 100)}%)";
                Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * 0.8f;
                Vector2 textPos = new Vector2(
                    barRect.X + barRect.Width / 2 - textSize.X / 2,
                    barRect.Y + barRect.Height / 2 - textSize.Y / 2 + 2
                );
                Utils.DrawBorderString(spriteBatch, text, textPos, Color.White * alpha, 0.8f);
            }

            //绘制切换按钮(小箭头)
            Rectangle toggleRect = new Rectangle(barRect.Right + 5, barRect.Y + barHeight / 2 - 10, 20, 20);
            bool hoverToggle = toggleRect.Contains(Main.MouseScreen.ToPoint());
            Color toggleColor = hoverToggle ? new Color(255, 200, 100) : new Color(200, 150, 80);

            Utils.DrawBorderString(spriteBatch, log.ShowProgressBar ? "▲" : "▼", toggleRect.TopLeft(), toggleColor * alpha, 1f);

            //处理点击
            if (hoverToggle) {
                Main.LocalPlayer.mouseInterface = true;
                if (Main.mouseLeft && Main.mouseLeftRelease) {
                    log.ShowProgressBar = !log.ShowProgressBar;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
            }
        }

        private void DrawQuestIcon(SpriteBatch spriteBatch, QuestNode node, Vector2 center, float scale, float alpha) {
            Texture2D iconTexture = node.GetIconTexture();
            if (iconTexture == null) return;

            Rectangle? sourceRect = node.GetIconSourceRect(iconTexture);
            if (!sourceRect.HasValue) return;

            //计算图标绘制区域(节点内部，留出边距)
            int iconSize = (int)(40 * scale);
            Rectangle iconDrawRect = new Rectangle(
                (int)(center.X - iconSize / 2),
                (int)(center.Y - iconSize / 2),
                iconSize,
                iconSize
            );

            //计算缩放以适应图标区域
            float iconScale = 1f;
            Rectangle frame = sourceRect.Value;
            if (frame.Width > iconSize || frame.Height > iconSize) {
                iconScale = iconSize / (float)Math.Max(frame.Width, frame.Height);
            }

            //确定颜色(未解锁时变暗)
            Color iconColor = node.IsUnlocked ? Color.White : new Color(100, 100, 110);

            //已完成时添加绿色调
            if (node.IsCompleted) {
                iconColor = new Color(200, 255, 200);
            }

            //绘制图标
            Vector2 iconPos = new Vector2(iconDrawRect.X + iconDrawRect.Width / 2, iconDrawRect.Y + iconDrawRect.Height / 2);
            Vector2 origin = frame.Size() / 2f;

            spriteBatch.Draw(iconTexture, iconPos, frame, iconColor * alpha, 0f, origin, iconScale, SpriteEffects.None, 0f);
        }

        private void DrawCornerMark(SpriteBatch spriteBatch, Vector2 pos, float pulse, float alphaMult, bool nightMode) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            float size = 8f;
            Color markColor = nightMode ? new Color(50, 110, 190) : new Color(190, 110, 50);
            Color glowColor = nightMode ? new Color(35, 80, 160) : new Color(160, 80, 35);

            //外层柔和辉光
            spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), glowColor * (0.25f * pulse * alphaMult), 0f,
                new Vector2(0.5f, 0.5f), new Vector2(size * 2.2f, size * 2.2f), SpriteEffects.None, 0f);

            //十字形装饰——水平
            spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), markColor * (pulse * alphaMult), 0f,
                new Vector2(0.5f, 0.5f), new Vector2(size * 1.6f, size * 0.3f), SpriteEffects.None, 0f);
            //十字形装饰——垂直
            spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), markColor * (0.85f * pulse * alphaMult), MathHelper.PiOver2,
                new Vector2(0.5f, 0.5f), new Vector2(size * 1.6f, size * 0.3f), SpriteEffects.None, 0f);

            //中心实心方块
            spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), markColor * (0.9f * pulse * alphaMult), 0f,
                new Vector2(0.5f, 0.5f), new Vector2(size * 0.5f, size * 0.5f), SpriteEffects.None, 0f);
            //中心高光点
            spriteBatch.Draw(pixel, pos, new Rectangle(0, 0, 1, 1), Color.White * (0.35f * pulse * alphaMult), 0f,
                new Vector2(0.5f, 0.5f), new Vector2(size * 0.2f, size * 0.2f), SpriteEffects.None, 0f);
        }

        public Rectangle GetStyleSwitchButtonRect(Rectangle panelRect) {
            return new Rectangle(
                panelRect.X + 15,
                panelRect.Bottom - 45,
                30,
                30
            );
        }

        public void DrawStyleSwitchButton(SpriteBatch spriteBatch, Rectangle panelRect, bool isHovered, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            Rectangle buttonRect = GetStyleSwitchButtonRect(panelRect);
            Vector2 center = buttonRect.Center.ToVector2();

            //绘制背景
            Color bgColor = isHovered ? new Color(100, 100, 120) : new Color(60, 60, 70);
            spriteBatch.Draw(pixel, buttonRect, bgColor * alpha);

            //绘制边框
            Color borderColor = isHovered ? Color.White : new Color(180, 180, 200);
            int border = 2;
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Y, buttonRect.Width, border), borderColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Bottom - border, buttonRect.Width, border), borderColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Y, border, buttonRect.Height), borderColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.Right - border, buttonRect.Y, border, buttonRect.Height), borderColor * alpha);

            //绘制图标 (类似书本或层叠页面)
            Color iconColor = isHovered ? Color.White : new Color(220, 220, 220);

            //后页
            Rectangle page1 = new Rectangle(0, 0, 16, 20);
            spriteBatch.Draw(pixel, center + new Vector2(2, -2), page1, iconColor * 0.5f * alpha, 0f, new Vector2(8, 10), 1f, SpriteEffects.None, 0f);

            //前页
            Rectangle page2 = new Rectangle(0, 0, 16, 20);
            spriteBatch.Draw(pixel, center + new Vector2(-2, 2), page2, iconColor * alpha, 0f, new Vector2(8, 10), 1f, SpriteEffects.None, 0f);

            //页面纹理
            spriteBatch.Draw(pixel, center + new Vector2(-2, 2) + new Vector2(-4, -5), new Rectangle(0, 0, 8, 2), Color.Black * 0.5f * alpha, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(pixel, center + new Vector2(-2, 2) + new Vector2(-4, 0), new Rectangle(0, 0, 8, 2), Color.Black * 0.5f * alpha, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(pixel, center + new Vector2(-2, 2) + new Vector2(-4, 5), new Rectangle(0, 0, 6, 2), Color.Black * 0.5f * alpha, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }

        public Rectangle GetNightModeButtonRect(Rectangle panelRect) {
            return new Rectangle(
                panelRect.X + 55,
                panelRect.Bottom - 45,
                30,
                30
            );
        }

        public void DrawNightModeButton(SpriteBatch spriteBatch, Rectangle panelRect, bool isHovered, float alpha, bool isNightMode) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            Rectangle buttonRect = GetNightModeButtonRect(panelRect);
            Vector2 center = buttonRect.Center.ToVector2();

            //绘制背景
            Color bgColor = isHovered ? new Color(80, 80, 100) : new Color(40, 40, 50);
            spriteBatch.Draw(pixel, buttonRect, bgColor * alpha);

            //绘制边框
            Color borderColor = isHovered ? Color.White : new Color(150, 150, 170);
            int border = 2;
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Y, buttonRect.Width, border), borderColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Bottom - border, buttonRect.Width, border), borderColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Y, border, buttonRect.Height), borderColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.Right - border, buttonRect.Y, border, buttonRect.Height), borderColor * alpha);

            //绘制图标 (月亮/太阳)
            Color iconColor = isHovered ? Color.White : new Color(255, 255, 200);

            if (isNightMode) {
                //月亮图标
                //绘制一个圆形
                spriteBatch.Draw(pixel, center, new Rectangle(0, 0, 16, 16), iconColor * alpha, 0f, new Vector2(8, 8), 1f, SpriteEffects.None, 0f);
                //绘制遮罩圆形形成月牙
                spriteBatch.Draw(pixel, center + new Vector2(4, -2), new Rectangle(0, 0, 14, 14), bgColor * alpha, 0f, new Vector2(7, 7), 1f, SpriteEffects.None, 0f);
            }
            else {
                //太阳图标
                //中心圆
                spriteBatch.Draw(pixel, center, new Rectangle(0, 0, 10, 10), iconColor * alpha, 0f, new Vector2(5, 5), 1f, SpriteEffects.None, 0f);
                //光芒
                float time = Main.GameUpdateCount * 0.02f;
                for (int i = 0; i < 8; i++) {
                    float rot = i * MathHelper.PiOver4 + time;
                    Vector2 offset = new Vector2(0, -9).RotatedBy(rot);
                    spriteBatch.Draw(pixel, center + offset, new Rectangle(0, 0, 2, 4), iconColor * alpha, rot, new Vector2(1, 2), 1f, SpriteEffects.None, 0f);
                }
            }
        }

        public Rectangle GetClaimAllButtonRect(Rectangle panelRect) {
            return new Rectangle(
                panelRect.X + panelRect.Width / 2 - 70,
                panelRect.Bottom + 40,
                140,
                35
            );
        }

        public void DrawClaimAllButton(SpriteBatch spriteBatch, Rectangle panelRect, bool isHovered, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            Rectangle buttonRect = GetClaimAllButtonRect(panelRect);
            bool nightMode = QuestLog.Instance?.NightMode ?? false;

            //动态脉冲
            float pulse = (float)Math.Sin(Main.GameUpdateCount * 0.1f) * 0.5f + 0.5f;

            //背景渐变
            Color colorTop, colorBottom;
            if (nightMode) {
                colorTop = isHovered ? new Color(70, 130, 200) : new Color(70, 130, 200);
                colorBottom = isHovered ? new Color(100, 180, 255) : new Color(100, 180, 255);
            }
            else {
                colorTop = isHovered ? new Color(255, 150, 70) : new Color(255, 150, 70);
                colorBottom = isHovered ? new Color(255, 200, 120) : new Color(255, 200, 120);
            }

            //绘制渐变背景
            int steps = 110;
            for (int i = 0; i < steps; i++) {
                float t = i / (float)steps;
                int y = (int)(buttonRect.Y + (float)(t * buttonRect.Height));
                int h = Math.Max(1, buttonRect.Height / steps);
                Color c = Color.Lerp(colorTop, colorBottom, t);
                spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, y, buttonRect.Width, h), c * alpha * 0.8f);
            }

            //绘制发光边框
            Color glowColor;
            if (nightMode) {
                glowColor = Color.Lerp(new Color(100, 180, 255), new Color(150, 220, 255), pulse);
            }
            else {
                glowColor = Color.Lerp(new Color(150, 255, 150), new Color(255, 255, 200), pulse);
            }
            if (isHovered) glowColor = Color.White;

            //内发光
            Rectangle innerRect = buttonRect;
            innerRect.Inflate(-2, -2);
            spriteBatch.Draw(pixel, new Rectangle(innerRect.X, innerRect.Y, innerRect.Width, 1), glowColor * alpha * 0.5f);
            spriteBatch.Draw(pixel, new Rectangle(innerRect.X, innerRect.Bottom - 1, innerRect.Width, 1), glowColor * alpha * 0.5f);

            //外边框装饰
            int border = 2;
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Y, buttonRect.Width, border), glowColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Bottom - border, buttonRect.Width, border), glowColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Y, border, buttonRect.Height), glowColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.Right - border, buttonRect.Y, border, buttonRect.Height), glowColor * alpha);

            //角落装饰
            int cornerSize = 6;
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Y, cornerSize, border), glowColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Y, border, cornerSize), glowColor * alpha);

            spriteBatch.Draw(pixel, new Rectangle(buttonRect.Right - cornerSize, buttonRect.Y, cornerSize, border), glowColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.Right - border, buttonRect.Y, border, cornerSize), glowColor * alpha);

            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Bottom - border, cornerSize, border), glowColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Bottom - cornerSize, border, cornerSize), glowColor * alpha);

            spriteBatch.Draw(pixel, new Rectangle(buttonRect.Right - cornerSize, buttonRect.Bottom - border, cornerSize, border), glowColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.Right - border, buttonRect.Bottom - cornerSize, border, cornerSize), glowColor * alpha);

            //文字
            string text = QuestLog.QuickReceiveAwardText.Value;
            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * 0.85f;
            Vector2 textPos = new Vector2(buttonRect.X + buttonRect.Width / 2, buttonRect.Y + buttonRect.Height / 2);

            //文字发光
            Color textColor;
            if (nightMode) {
                textColor = isHovered ? new Color(200, 230, 255) : Color.White;
            }
            else {
                textColor = isHovered ? new Color(200, 255, 200) : Color.White;
            }

            Utils.DrawBorderString(spriteBatch, text, textPos, textColor * alpha, 0.85f, 0.5f, 0.5f);
        }

        public Rectangle GetResetViewButtonRect(Rectangle panelRect) {
            return new Rectangle(
                panelRect.Right - 45,
                panelRect.Bottom - 48,
                36,
                36
            );
        }

        public void DrawResetViewButton(SpriteBatch spriteBatch, Rectangle panelRect, Vector2 directionToCenter, bool isHovered, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            Rectangle buttonRect = GetResetViewButtonRect(panelRect);
            Vector2 center = buttonRect.Center.ToVector2();

            //绘制背景阴影
            Rectangle shadowRect = buttonRect;
            shadowRect.Offset(2, 2);
            spriteBatch.Draw(pixel, shadowRect, Color.Black * 0.5f * alpha);

            //绘制主体
            Color baseColor = isHovered ? new Color(255, 220, 150) : new Color(200, 150, 80);
            spriteBatch.Draw(pixel, buttonRect, baseColor * 0.8f * alpha);

            //绘制边框
            Color borderColor = new Color(255, 240, 200);
            int border = 2;
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Y, buttonRect.Width, border), borderColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Bottom - border, buttonRect.Width, border), borderColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.X, buttonRect.Y, border, buttonRect.Height), borderColor * alpha);
            spriteBatch.Draw(pixel, new Rectangle(buttonRect.Right - border, buttonRect.Y, border, buttonRect.Height), borderColor * alpha);

            //绘制指南针刻度装饰
            float time = Main.GameUpdateCount * 0.02f;
            for (int i = 0; i < 4; i++) {
                float rot = i * MathHelper.PiOver2 + time;
                Vector2 offset = new Vector2(0, -14).RotatedBy(rot);
                spriteBatch.Draw(pixel, center + offset, new Rectangle(0, 0, 2, 4), Color.White * 0.5f * alpha, rot, new Vector2(1, 2), 1f, SpriteEffects.None, 0f);
            }

            //箭头
            float rotation = directionToCenter.ToRotation();
            float arrowPulse = (float)Math.Sin(Main.GameUpdateCount * 0.15f) * 0.2f + 1f;

            //箭头主体
            Color arrowColor = isHovered ? Color.White : new Color(255, 255, 220);

            //绘制箭头杆
            spriteBatch.Draw(pixel, center, new Rectangle(0, 0, 16, 3), arrowColor * alpha, rotation, new Vector2(0, 1.5f), 1f, SpriteEffects.None, 0f);

            //绘制箭头头部 (三角形)
            float headSize = 8f * arrowPulse;
            Vector2 headPos = center + new Vector2(8, 0).RotatedBy(rotation);

            //使用两个旋转的矩形模拟箭头头部
            spriteBatch.Draw(pixel, headPos, new Rectangle(0, 0, (int)headSize, 2), arrowColor * alpha, rotation + MathHelper.Pi * 0.75f, new Vector2(0, 1), 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(pixel, headPos, new Rectangle(0, 0, (int)headSize, 2), arrowColor * alpha, rotation - MathHelper.Pi * 0.75f, new Vector2(0, 1), 1f, SpriteEffects.None, 0f);

            //中心点
            spriteBatch.Draw(pixel, center, new Rectangle(0, 0, 4, 4), Color.Red * alpha, 0f, new Vector2(2, 2), 1f, SpriteEffects.None, 0f);
        }
    }
}
