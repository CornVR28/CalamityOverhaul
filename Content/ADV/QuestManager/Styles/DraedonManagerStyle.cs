using CalamityOverhaul.Content.ADV.UIEffect;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.QuestManager.Styles
{
    /// <summary>
    /// 嘉登·战术数据链风格，任务管理器默认样式，
    /// 左侧垂直时间线脊柱、菱形状态节点、雪佛龙指示器、
    /// 虎纹扫描带、六边形蜂巢底纹、雷达旋转弧装饰
    /// </summary>
    internal class DraedonManagerStyle : BaseManagerStyle
    {
        #region 动画计时器

        private float scanBandPos;
        private float radarSweep;
        private float chevronPulse;
        private float spineGlowPhase;
        private float hexGridPhase;
        private float glitchTimer;
        private float headerGlowPhase;
        private float dataFlowTimer;

        private readonly List<DraedonDataPRT> bgParticles = [];
        private int bgParticleSpawnTimer;
        private readonly List<CircuitNodePRT> circuitNodes = [];
        private int circuitNodeSpawnTimer;

        #endregion

        #region 色板（偏冷白蓝色调，区别于其他嘉登UI的纯青/纯蓝）

        private static readonly Color PrimaryBright = new(140, 210, 255);   //冷白蓝（区别于来电框的40,220,255纯青）
        private static readonly Color PrimaryMid = new(60, 150, 220);       //中蓝
        private static readonly Color PrimaryDim = new(30, 80, 140);        //暗蓝
        private static readonly Color AccentCyan = new(80, 255, 220);       //薄荷青强调（区别于对话框的50,210,185青绿）
        private static readonly Color AccentWarm = new(255, 200, 100);      //暖琥珀（警告/关注态）
        private static readonly Color BgDeep = new(4, 8, 20);              //极深背景
        private static readonly Color BgMid = new(10, 18, 36);             //中层背景
        private static readonly Color StatusComplete = new(60, 220, 140);   //已完成绿
        private static readonly Color StatusFailed = new(220, 60, 70);      //失败红
        private static readonly Color StatusSuspended = new(160, 140, 100); //挂起暗金

        #endregion

        #region 生命周期

        public override void Update(Rectangle panelRect, float openProgress) {
            base.Update(panelRect, openProgress);

            //虎纹扫描带，从上往下循环，比单线更宽（覆盖约8%面板高度）
            scanBandPos += 0.004f;
            if (scanBandPos > 1.15f) scanBandPos = -0.15f;

            //雷达旋转弧
            radarSweep += 0.018f;
            if (radarSweep > MathHelper.TwoPi) radarSweep -= MathHelper.TwoPi;

            chevronPulse += 0.065f;
            if (chevronPulse > MathHelper.TwoPi) chevronPulse -= MathHelper.TwoPi;

            spineGlowPhase += 0.03f;
            if (spineGlowPhase > MathHelper.TwoPi) spineGlowPhase -= MathHelper.TwoPi;

            hexGridPhase += 0.008f;
            if (hexGridPhase > MathHelper.TwoPi) hexGridPhase -= MathHelper.TwoPi;

            headerGlowPhase += 0.04f;
            if (headerGlowPhase > MathHelper.TwoPi) headerGlowPhase -= MathHelper.TwoPi;

            dataFlowTimer += 0.015f;
            if (dataFlowTimer > MathHelper.TwoPi) dataFlowTimer -= MathHelper.TwoPi;

            glitchTimer += 0.12f;
            if (glitchTimer > MathHelper.TwoPi) glitchTimer -= MathHelper.TwoPi;

            //背景粒子
            Vector2 panelPos = new(panelRect.X, panelRect.Y);
            Vector2 panelSize = new(panelRect.Width, panelRect.Height);
            bgParticleSpawnTimer++;
            if (openProgress > 0.3f && bgParticleSpawnTimer >= 35 && bgParticles.Count < 12) {
                bgParticleSpawnTimer = 0;
                Vector2 p = panelPos + new Vector2(
                    Main.rand.NextFloat(20f, panelSize.X - 20f),
                    Main.rand.NextFloat(60f, panelSize.Y - 20f));
                bgParticles.Add(new DraedonDataPRT(p));
            }
            for (int i = bgParticles.Count - 1; i >= 0; i--) {
                if (bgParticles[i].Update(panelPos, panelSize))
                    bgParticles.RemoveAt(i);
            }

            //电路节点
            circuitNodeSpawnTimer++;
            if (openProgress > 0.5f && circuitNodeSpawnTimer >= 45 && circuitNodes.Count < 5) {
                circuitNodeSpawnTimer = 0;
                circuitNodes.Add(new CircuitNodePRT(
                    panelPos + new Vector2(
                        Main.rand.NextFloat(20f, panelSize.X - 20f),
                        Main.rand.NextFloat(60f, panelSize.Y - 30f))));
            }
            for (int i = circuitNodes.Count - 1; i >= 0; i--) {
                if (circuitNodes[i].Update(panelPos, panelSize))
                    circuitNodes.RemoveAt(i);
            }
        }

        public override void Reset() {
            base.Reset();
            scanBandPos = 0f;
            radarSweep = 0f;
            chevronPulse = 0f;
            spineGlowPhase = 0f;
            hexGridPhase = 0f;
            glitchTimer = 0f;
            headerGlowPhase = 0f;
            dataFlowTimer = 0f;
            bgParticles.Clear();
            circuitNodes.Clear();
            bgParticleSpawnTimer = 0;
            circuitNodeSpawnTimer = 0;
        }

        #endregion

        #region 面板背景

        public override void DrawPanelBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            // 多层扩散阴影 
            DrawShadowLayers(sb, rect, alpha, 10, 4, 5);

            // 纵向渐变底色（30段，轻微脉冲呼吸）
            int segs = 30;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = rect.Y + (int)(t * rect.Height);
                int y2 = rect.Y + (int)(t2 * rect.Height);
                float breath = MathF.Sin(pulseTimer * 0.4f + t * 1.8f) * 0.5f + 0.5f;
                Color c = Color.Lerp(BgDeep, BgMid, t * 0.4f + breath * 0.15f) * (alpha * 0.96f);
                FillRect(sb, new Rectangle(rect.X, y1, rect.Width, Math.Max(1, y2 - y1)), c);
            }

            // 六边形蜂巢底纹（比TrackerStyle更大更稀疏，加等高线偏移感）
            DrawHexTopography(sb, rect, alpha * 0.04f);

            // 左侧时间线脊柱背景辉光 
            int spineX = rect.X + 28;
            float spineGlow = MathF.Sin(spineGlowPhase) * 0.15f + 0.25f;
            for (int gy = rect.Y + 50; gy < rect.Bottom - 20; gy += 2) {
                float yRatio = (gy - rect.Y) / (float)rect.Height;
                float localGlow = MathF.Sin(spineGlowPhase + yRatio * 4f) * 0.2f + 0.2f;
                Color gc = PrimaryDim * (alpha * (spineGlow + localGlow) * 0.3f);
                sb.Draw(Px, new Rectangle(spineX - 6, gy, 12, 1), new Rectangle(0, 0, 1, 1), gc);
            }
        }

        /// <summary>六边形加等高线拓扑纹理</summary>
        private void DrawHexTopography(SpriteBatch sb, Rectangle rect, float alpha) {
            if (alpha < 0.005f) return;
            float cellW = 24f, cellH = 20f;
            int cols = (int)(rect.Width / cellW) + 2;
            int rows = (int)(rect.Height / cellH) + 2;
            for (int row = 0; row < rows; row++) {
                for (int col = 0; col < cols; col++) {
                    float ox = row % 2 == 0 ? 0f : cellW * 0.5f;
                    float px2 = rect.X + col * cellW + ox;
                    float py = rect.Y + row * cellH;
                    if (px2 < rect.X || px2 >= rect.Right || py < rect.Y || py >= rect.Bottom) continue;

                    //等高线式明暗起伏
                    float topo = MathF.Sin(col * 0.5f + row * 0.3f + hexGridPhase * 0.7f);
                    float brightness = 0.3f + topo * 0.25f;
                    Color c = PrimaryDim * (alpha * brightness);
                    sb.Draw(Px, new Rectangle((int)px2, (int)py, 2, 2), new Rectangle(0, 0, 1, 1), c);
                }
            }
        }

        #endregion

        #region 面板边框

        public override void DrawPanelFrame(SpriteBatch sb, Rectangle rect, float alpha) {
            // 顶部双层重线（4px亮+2px暗），和对话框的3+1、来电框的L括号都不同 
            Color topBright = PrimaryBright * (alpha * 0.92f);
            Color topDim = PrimaryMid * (alpha * 0.4f);
            HLine(sb, rect.X, rect.Y, rect.Width, 4, topBright);
            HLine(sb, rect.X, rect.Y + 4, rect.Width, 2, topDim);

            // 底部渐变线（左亮右暗）
            DrawGradientHLine(sb, rect.X, rect.Bottom - 2, rect.Width,
                PrimaryBright * (alpha * 0.65f), PrimaryDim * (alpha * 0.15f), 20);
            HLine(sb, rect.X, rect.Bottom - 1, rect.Width, PrimaryDim * (alpha * 0.2f));

            // 左侧强调竖条（3px，上2/3亮+下1/3暗），区别于对话框的4px全高 
            int leftBarH = (int)(rect.Height * 0.66f);
            VLine(sb, rect.X, rect.Y, leftBarH, 3, PrimaryBright * (alpha * 0.6f));
            VLine(sb, rect.X, rect.Y + leftBarH, rect.Height - leftBarH, 3, PrimaryDim * (alpha * 0.25f));

            // 右侧细线 + 中段刻度标尺 
            VLine(sb, rect.Right - 1, rect.Y, rect.Height, PrimaryDim * (alpha * 0.35f));
            DrawRightRuler(sb, rect, alpha);

            // 顶部左侧三道刻痕（机械感，间距递减）
            VLine(sb, rect.X + 5, rect.Y, 12, 1, topBright * 0.7f);
            VLine(sb, rect.X + 14, rect.Y, 8, 1, topBright * 0.45f);
            VLine(sb, rect.X + 21, rect.Y, 5, 1, topBright * 0.25f);

            // 右上角斜切装饰（15px三角缺口），区别于对话框的大斜切 
            int cut = 15;
            for (int row = 0; row < cut; row++) {
                int segLen = cut - row;
                FillRect(sb, new Rectangle(rect.Right - segLen, rect.Y + row, segLen, 1),
                    BgDeep * (alpha * 0.98f));
            }
            //斜线高亮
            for (int row = 0; row < cut; row++) {
                float fade = 1f - (float)row / cut;
                FillRect(sb, new Rectangle(rect.Right - (cut - row), rect.Y + row, 1, 1),
                    AccentCyan * (alpha * 0.6f * fade));
            }
        }

        /// <summary>右侧刻度标尺，带流光效果</summary>
        private void DrawRightRuler(SpriteBatch sb, Rectangle rect, float alpha) {
            int rx = rect.Right - 8;
            int spacing = 8;
            int marks = (rect.Height - 20) / spacing;
            for (int i = 0; i < marks; i++) {
                float t = (float)i / marks;
                float flow = MathF.Sin((t + dataFlowTimer * 0.3f) * MathHelper.TwoPi) * 0.3f + 0.4f;
                int mLen = i % 5 == 0 ? 6 : 3;
                Color mc = PrimaryDim * (alpha * flow);
                HLine(sb, rx - mLen, rect.Y + 10 + i * spacing, mLen, mc);
            }
        }

        #endregion

        #region 标题栏

        public override void DrawHeader(SpriteBatch sb, Rectangle headerRect, string title, float alpha) {
            // 标题栏背景（比面板背景深一层）
            FillRect(sb, headerRect, new Color(3, 6, 16) * (alpha * 0.7f));

            // 雷达旋转弧装饰（标题左侧，替代来电框的信号弧/对话框的HEX读出）
            Vector2 radarCenter = new(headerRect.X + 24f, headerRect.Y + headerRect.Height / 2f);
            DrawRadarArc(sb, radarCenter, 14f, alpha);

            // 标题文字——超出宽度时截断加省略号
            var font = FontAssets.MouseText.Value;
            float headerBlink = MathF.Sin(headerGlowPhase) * 0.12f + 0.88f;
            Vector2 titlePos = new(headerRect.X + 46f, headerRect.Y + (headerRect.Height - 18f) / 2f);
            float maxHeaderTitleW = headerRect.Width - 120f;
            if (font.MeasureString(title).X * 0.88f > maxHeaderTitleW) {
                while (title.Length > 3 && font.MeasureString(title + "...").X * 0.88f > maxHeaderTitleW)
                    title = title[..^1];
                title += "...";
            }
            Utils.DrawBorderString(sb, title, titlePos, PrimaryBright * (alpha * headerBlink), 0.88f);

            //标题右侧数据流标签
            float tagBlink = MathF.Sin(headerGlowPhase * 1.6f) * 0.35f + 0.65f;
            string tag = QuestManagerUI.HeaderStatusTag.Value;
            float tagW = font.MeasureString(tag).X * 0.6f;
            Utils.DrawBorderString(sb, tag,
                new Vector2(headerRect.Right - tagW - 14f, headerRect.Y + (headerRect.Height - 14f) / 2f),
                AccentCyan * (alpha * 0.55f * tagBlink), 0.6f);

            // 底部分隔线（虎纹虚线，短实线+间隙交替，区别于对话框的流动虚线）
            DrawTigerDash(sb, headerRect.X + 8, headerRect.Bottom - 1, headerRect.Width - 16, alpha * 0.5f);
        }

        /// <summary>雷达旋转弧，任务管理器专属标识</summary>
        private void DrawRadarArc(SpriteBatch sb, Vector2 center, float radius, float alpha) {
            int segments = 12;
            for (int arc = 0; arc < 2; arc++) {
                float baseAngle = radarSweep + arc * MathHelper.Pi;
                float arcSpan = MathHelper.PiOver4 * 1.2f;
                for (int i = 0; i < segments; i++) {
                    float t = (float)i / segments;
                    float angle = baseAngle + t * arcSpan;
                    float fade = MathF.Sin(t * MathHelper.Pi) * 0.7f + 0.3f;
                    Vector2 p = center + angle.ToRotationVector2() * radius;
                    FillRect(sb, new Rectangle((int)p.X, (int)p.Y, 2, 2),
                        PrimaryBright * (alpha * 0.5f * fade));
                }
            }
            //中心亮点
            FillRect(sb, new Rectangle((int)(center.X - 1), (int)(center.Y - 1), 3, 3),
                AccentCyan * (alpha * 0.4f));
        }

        /// <summary>虎纹虚线，4px实线加2px间隙的紧密排列</summary>
        private void DrawTigerDash(SpriteBatch sb, int x, int y, int w, float alpha) {
            int dashW = 4, gapW = 2;
            float flow = dataFlowTimer * 18f;
            int period = dashW + gapW;
            float cx = x - flow % period;
            while (cx < x + w) {
                float segStart = Math.Max(cx, x);
                float segEnd = Math.Min(cx + dashW, x + w);
                if (segEnd > segStart) {
                    float bright = 0.5f + MathF.Sin((segStart - x) / w * MathHelper.Pi) * 0.4f;
                    HLine(sb, (int)segStart, y, (int)(segEnd - segStart),
                        PrimaryMid * (alpha * bright));
                }
                cx += period;
            }
        }

        #endregion

        #region 分类选项卡

        public override void DrawCategoryTabs(SpriteBatch sb, Rectangle tabRect, string[] categories,
            int selectedIndex, float alpha) {
            var font = FontAssets.MouseText.Value;
            float scale = 0.72f;
            float tabX = tabRect.X + 6f;

            for (int i = 0; i < categories.Length; i++) {
                string label = categories[i];
                Vector2 size = font.MeasureString(label) * scale;
                float tabW = size.X + 18f;
                Rectangle tab = new((int)tabX, tabRect.Y + 2, (int)tabW, tabRect.Height - 4);

                bool selected = i == selectedIndex;
                Color bgC = selected ? PrimaryDim * (alpha * 0.4f) : new Color(6, 12, 28) * (alpha * 0.3f);
                FillRect(sb, tab, bgC);

                if (selected) {
                    //选中态底部高亮线
                    HLine(sb, tab.X, tab.Bottom - 1, tab.Width, 2, AccentCyan * (alpha * 0.85f));
                    //轻微辉光
                    FillRect(sb, new Rectangle(tab.X, tab.Y, tab.Width, 1), PrimaryBright * (alpha * 0.15f));
                }

                Color textC = selected ? PrimaryBright * alpha : PrimaryMid * (alpha * 0.65f);
                Utils.DrawBorderString(sb, label,
                    new Vector2(tab.X + (tab.Width - size.X) / 2f, tab.Y + (tab.Height - size.Y) / 2f),
                    textC, scale);

                tabX += tabW + 3f;
            }

            //整行底部线
            HLine(sb, tabRect.X, tabRect.Bottom - 1, tabRect.Width, PrimaryDim * (alpha * 0.25f));
        }

        #endregion

        #region 滚动条

        public override void DrawScrollbar(SpriteBatch sb, Rectangle trackRect, float scrollRatio,
            float viewRatio, float alpha) {
            //轨道
            FillRect(sb, trackRect, new Color(8, 16, 32) * (alpha * 0.5f));
            VLine(sb, trackRect.X, trackRect.Y, trackRect.Height, PrimaryDim * (alpha * 0.25f));

            //滑块
            float clampedView = MathHelper.Clamp(viewRatio, 0.1f, 1f);
            int thumbH = Math.Max(20, (int)(trackRect.Height * clampedView));
            int thumbY = trackRect.Y + (int)((trackRect.Height - thumbH) * MathHelper.Clamp(scrollRatio, 0f, 1f));
            Rectangle thumb = new(trackRect.X + 1, thumbY, trackRect.Width - 2, thumbH);

            float thumbPulse = MathF.Sin(pulseTimer * 1.5f) * 0.15f + 0.6f;
            FillRect(sb, thumb, PrimaryMid * (alpha * thumbPulse));
            //滑块中3道刻线
            int midY = thumb.Y + thumb.Height / 2;
            for (int j = -1; j <= 1; j++) {
                HLine(sb, thumb.X + 2, midY + j * 3, thumb.Width - 4, PrimaryBright * (alpha * 0.35f));
            }
        }

        #endregion

        #region 底部状态栏

        public override void DrawFooter(SpriteBatch sb, Rectangle footerRect, int totalQuests,
            int activeQuests, float alpha) {
            //背景
            FillRect(sb, footerRect, new Color(3, 6, 16) * (alpha * 0.65f));
            //顶部分隔
            HLine(sb, footerRect.X + 8, footerRect.Y, footerRect.Width - 16, PrimaryDim * (alpha * 0.35f));

            //统计文本
            string statsText = string.Format(QuestManagerUI.FooterStatsFormat.Value, totalQuests, activeQuests);
            float statsBlink = MathF.Sin(globalTimer * 1.2f) * 0.1f + 0.9f;
            Utils.DrawBorderString(sb, statsText,
                new Vector2(footerRect.X + 12f, footerRect.Y + (footerRect.Height - 12f) / 2f),
                PrimaryMid * (alpha * 0.7f * statsBlink), 0.62f);

            //右侧版本标记
            string verTag = CWRMod.Instance.Version.ToString();
            var font = FontAssets.MouseText.Value;
            float vw = font.MeasureString(verTag).X * 0.55f;
            Utils.DrawBorderString(sb, verTag,
                new Vector2(footerRect.Right - vw - 10f, footerRect.Y + (footerRect.Height - 12f) / 2f),
                PrimaryDim * (alpha * 0.4f), 0.55f);
        }

        #endregion

        #region 任务条目

        public override void DrawQuestEntry(SpriteBatch sb, Rectangle entryRect, QuestEntryData entry,
            bool isSelected, bool isHovered, float alpha, int entryIndex) {
            var font = FontAssets.MouseText.Value;
            var customStyle = entry.EntryStyle;

            // === 背景 ===
            bool bgHandled = customStyle?.DrawEntryBackground(sb, entryRect, entry, isSelected, isHovered, alpha) ?? false;
            if (!bgHandled) {
                if (isSelected) {
                    FillRect(sb, entryRect, PrimaryDim * (alpha * 0.22f));
                    VLine(sb, entryRect.X, entryRect.Y, entryRect.Height, 2, AccentCyan * (alpha * 0.7f));
                }
                else if (isHovered) {
                    FillRect(sb, entryRect, PrimaryDim * (alpha * 0.12f));
                    VLine(sb, entryRect.X, entryRect.Y, entryRect.Height, 1, PrimaryMid * (alpha * 0.4f));
                }

                Color statusBarColor = GetStatusColor(entry.Status, alpha);
                VLine(sb, entryRect.X + 4, entryRect.Y + 4, entryRect.Height - 8, 3, statusBarColor);
            }

            // 时间线脊柱上的菱形节点 
            int spineX = entryRect.X + 28;
            int spineNodeY = entryRect.Y + entryRect.Height / 2;
            DrawDiamondNode(sb, new Vector2(spineX, spineNodeY), entry.Status, alpha, entryIndex);

            // 连接线（到下一条目的虚线，在条目绘制时只画下半段）
            Color spineLineC = PrimaryDim * (alpha * 0.3f);
            for (int ly = spineNodeY + 6; ly < entryRect.Bottom; ly += 3) {
                FillRect(sb, new Rectangle(spineX, ly, 1, 1), spineLineC);
            }
            //上半段
            for (int ly = entryRect.Y; ly < spineNodeY - 5; ly += 3) {
                FillRect(sb, new Rectangle(spineX, ly, 1, 1), spineLineC);
            }

            // === 图标 + 标题 ===
            float titleX = entryRect.X + 42f;
            float titleY = entryRect.Y + 6f;

            //自定义图标（可能右移标题位置）
            float iconOffset = customStyle?.DrawEntryIcon(sb, new Vector2(titleX, titleY), entry, alpha) ?? 0f;
            titleX += iconOffset;

            Color titleColor = customStyle?.GetTitleColor(entry.Status, alpha)
                ?? (entry.Status == QuestEntryStatus.Completed
                    ? StatusComplete * (alpha * 0.75f)
                    : PrimaryBright * alpha);
            if (entry.IsNew) {
                float newBlink = MathF.Sin(chevronPulse * 2f) * 0.3f + 0.7f;
                titleColor = Color.Lerp(titleColor, AccentWarm, newBlink * 0.4f);
            }
            //截断过长的标题
            string displayTitle = entry.Title ?? "";
            float maxEntryTitleW = entryRect.Width - 60f - iconOffset;
            if (font.MeasureString(displayTitle).X * 0.78f > maxEntryTitleW) {
                while (displayTitle.Length > 3 && font.MeasureString(displayTitle + "...").X * 0.78f > maxEntryTitleW)
                    displayTitle = displayTitle[..^1];
                displayTitle += "...";
            }
            Utils.DrawBorderString(sb, displayTitle, new Vector2(titleX, titleY), titleColor, 0.78f);

            // 雪佛龙关注指示器（Tracked态在标题右侧显示 ››）
            if (entry.Status == QuestEntryStatus.Tracked) {
                float chevBlink = MathF.Sin(chevronPulse * 1.5f) * 0.4f + 0.6f;
                float titleW = font.MeasureString(displayTitle).X * 0.78f;
                Color chevColor = customStyle?.GetAccentColor(QuestEntryStatus.Tracked, alpha * chevBlink)
                    ?? AccentCyan * (alpha * chevBlink);
                Utils.DrawBorderString(sb, "››",
                    new Vector2(titleX + titleW + 6f, titleY + 1f),
                    chevColor, 0.7f);
            }

            // 摘要文本 
            float summaryY = titleY + 20f;
            Color summaryColor = PrimaryMid * (alpha * 0.6f);
            string summary = entry.Summary ?? "";
            //截断过长的摘要
            float maxSummaryW = entryRect.Width - 60f - iconOffset;
            if (font.MeasureString(summary).X * 0.65f > maxSummaryW) {
                while (summary.Length > 3 && font.MeasureString(summary + "...").X * 0.65f > maxSummaryW)
                    summary = summary[..^1];
                summary += "...";
            }
            //未展开时显示截断摘要，展开后摘要淡出
            float collapsedAlpha = 1f - entry.ExpandProgress;
            if (collapsedAlpha > 0.01f) {
                Utils.DrawBorderString(sb, summary, new Vector2(titleX, summaryY),
                    summaryColor * collapsedAlpha, 0.65f);
            }

            //展开指示器（标题行右端，▼/▲）
            if (!string.IsNullOrEmpty(entry.Summary)) {
                string expandIcon = entry.IsExpanded ? "▲" : "▼";
                float iconAlpha = isHovered ? 0.7f : 0.35f;
                float iconX = entryRect.Right - 18f;
                Utils.DrawBorderString(sb, expandIcon, new Vector2(iconX, titleY + 2f),
                    PrimaryMid * (alpha * iconAlpha), 0.6f);
            }

            // 展开内容区域（动画插值）
            if (entry.ExpandProgress > 0.01f) {
                int baseH = GetEntryHeight();
                float expandAlpha = alpha * entry.ExpandProgress;
                float descY = entryRect.Y + baseH - 4f;

                //展开区域半透明背景
                int expandAreaH = entryRect.Height - baseH;
                if (expandAreaH > 0) {
                    Rectangle expandBg = new(entryRect.X + 34, (int)descY - 2, entryRect.Width - 38, expandAreaH + 2);
                    FillRect(sb, expandBg, new Color(6, 12, 28) * (expandAlpha * 0.35f));
                }

                //分隔虚线
                Color sepColor = PrimaryDim * (expandAlpha * 0.4f);
                float sepX = titleX;
                float sepEndX = entryRect.Right - 14f;
                int dashW = 3, gapW = 3;
                float cx = sepX;
                while (cx < sepEndX) {
                    float segEnd = Math.Min(cx + dashW, sepEndX);
                    if (segEnd > cx) {
                        HLine(sb, (int)cx, (int)descY, (int)(segEnd - cx), sepColor);
                    }
                    cx += dashW + gapW;
                }
                descY += 5f;

                //自动换行的完整描述文本
                string fullText = entry.Summary ?? "";
                float descScale = 0.62f;
                int wrapWidth = (int)((entryRect.Width - 50f) / descScale);
                Color descColor = new Color(170, 200, 220) * (expandAlpha * 0.75f);

                //先按换行符拆分，再对每段做自动换行
                string[] paragraphs = fullText.Split('\n');
                foreach (string paragraph in paragraphs) {
                    string trimmedPara = paragraph.Trim();
                    if (string.IsNullOrEmpty(trimmedPara)) continue;
                    string[] wrapped = Utils.WordwrapString(trimmedPara, font, wrapWidth, 99, out _);
                    foreach (string wl in wrapped) {
                        if (string.IsNullOrEmpty(wl)) continue;
                        if (descY > entryRect.Bottom - 4f) break;
                        string trimmed = wl.TrimEnd('-', ' ');
                        Utils.DrawBorderString(sb, trimmed, new Vector2(titleX, descY), descColor, descScale);
                        descY += (int)(font.MeasureString(trimmed).Y * descScale) + 2;
                    }
                }
            }

            // 进度条（如果有进度数据）
            if (entry.Progress > 0f && entry.Status != QuestEntryStatus.Completed) {
                //展开时进度条靠近条目底部，折叠时紧跟摘要下方
                float barY = entry.ExpandProgress > 0.5f
                    ? entryRect.Bottom - 14f
                    : summaryY + 18f;
                int barW = Math.Min(120, entryRect.Width - 60);
                Rectangle barRect = new((int)titleX, (int)barY, barW, 5);
                DrawProgressBar(sb, barRect, entry.Progress,
                    new Color(8, 16, 32) * alpha,
                    PrimaryMid * alpha, AccentCyan * alpha,
                    PrimaryDim * (alpha * 0.4f), pulseTimer);

                //进度文本
                if (entry.ProgressText != null) {
                    Utils.DrawBorderString(sb, entry.ProgressText,
                        new Vector2(barRect.Right + 6f, barRect.Y - 2f),
                        PrimaryMid * (alpha * 0.55f), 0.55f);
                }
            }

            // === 前景特效 ===
            customStyle?.DrawEntryOverlay(sb, entryRect, entry, alpha);
        }

        /// <summary>
        /// 菱形状态节点，空心=进行中，实心=已完成，叉号=失败
        /// </summary>
        private void DrawDiamondNode(SpriteBatch sb, Vector2 center, QuestEntryStatus status, float alpha, int index) {
            float nodeSize = 4f;
            float pulse = MathF.Sin(spineGlowPhase + index * 0.8f) * 0.2f + 0.8f;
            Color nodeColor = GetStatusColor(status, alpha * pulse);

            if (status == QuestEntryStatus.Completed) {
                //实心菱形（两个旋转矩形叠加）
                sb.Draw(Px, center, null, nodeColor, MathHelper.PiOver4, new Vector2(0.5f),
                    new Vector2(nodeSize * 1.4f), SpriteEffects.None, 0f);
            }
            else if (status == QuestEntryStatus.Failed) {
                //叉号
                sb.Draw(Px, center, null, nodeColor, MathHelper.PiOver4, new Vector2(0.5f),
                    new Vector2(nodeSize * 1.2f, nodeSize * 0.3f), SpriteEffects.None, 0f);
                sb.Draw(Px, center, null, nodeColor, -MathHelper.PiOver4, new Vector2(0.5f),
                    new Vector2(nodeSize * 1.2f, nodeSize * 0.3f), SpriteEffects.None, 0f);
            }
            else {
                //空心菱形（外框）
                sb.Draw(Px, center, null, nodeColor, MathHelper.PiOver4, new Vector2(0.5f),
                    new Vector2(nodeSize * 1.4f), SpriteEffects.None, 0f);
                sb.Draw(Px, center, null, BgDeep * (alpha * 0.9f), MathHelper.PiOver4, new Vector2(0.5f),
                    new Vector2(nodeSize * 0.8f), SpriteEffects.None, 0f);

                //Tracked 态：外层光晕
                if (status == QuestEntryStatus.Tracked) {
                    float glowPulse = MathF.Sin(chevronPulse + index) * 0.3f + 0.3f;
                    sb.Draw(Px, center, null, AccentCyan * (alpha * glowPulse), MathHelper.PiOver4,
                        new Vector2(0.5f), new Vector2(nodeSize * 2.2f), SpriteEffects.None, 0f);
                }
            }
        }

        public override void DrawEntrySeparator(SpriteBatch sb, Vector2 start, Vector2 end, float alpha) {
            //极细虚线
            float w = end.X - start.X;
            int dashW = 3, gap = 4;
            float x = start.X;
            while (x < end.X) {
                float segEnd = Math.Min(x + dashW, end.X);
                if (segEnd > x) {
                    HLine(sb, (int)x, (int)start.Y, (int)(segEnd - x), PrimaryDim * (alpha * 0.15f));
                }
                x += dashW + gap;
            }
        }

        #endregion

        #region 颜色

        public override Color GetShadowColor(float alpha) => Color.Black * (alpha * 0.45f);

        public override Color GetHeaderTextColor(float alpha) => PrimaryBright * alpha;

        public override Color GetStatusColor(QuestEntryStatus status, float alpha) {
            return status switch {
                QuestEntryStatus.Active => PrimaryMid * alpha,
                QuestEntryStatus.Tracked => AccentCyan * alpha,
                QuestEntryStatus.Suspended => StatusSuspended * alpha,
                QuestEntryStatus.Completed => StatusComplete * alpha,
                QuestEntryStatus.Failed => StatusFailed * alpha,
                _ => PrimaryDim * alpha,
            };
        }

        #endregion

        #region 粒子与特效

        public override void DrawParticles(SpriteBatch sb, Rectangle panelRect, float alpha) {
            foreach (var node in circuitNodes)
                node.Draw(sb, alpha * 0.55f);
            foreach (var prt in bgParticles)
                prt.Draw(sb, alpha * 0.45f);
        }

        public override void DrawOverlayEffects(SpriteBatch sb, Rectangle panelRect, float alpha) {
            // 虎纹扫描带（8%面板高度的渐变亮带，从上向下循环）
            float bandH = panelRect.Height * 0.08f;
            float bandY = panelRect.Y + scanBandPos * (panelRect.Height + bandH) - bandH;
            int bandTop = Math.Max(panelRect.Y, (int)bandY);
            int bandBot = Math.Min(panelRect.Bottom, (int)(bandY + bandH));
            if (bandBot > bandTop) {
                for (int y = bandTop; y < bandBot; y++) {
                    float t = (y - bandY) / bandH;
                    float intensity = MathF.Sin(t * MathHelper.Pi) * 0.12f;
                    HLine(sb, panelRect.X + 4, y, panelRect.Width - 8,
                        PrimaryBright * (alpha * intensity));
                }
            }

            // 偶发故障横条（低频，约1/6触发）
            float gf = MathF.Sin(glitchTimer * 2.6f);
            if (gf > 0.95f) {
                float gy = panelRect.Y + glitchTimer * 113f % panelRect.Height;
                HLine(sb, panelRect.X + 4, (int)gy, panelRect.Width - 8, 2,
                    PrimaryBright * (alpha * (gf - 0.95f) * 4f));
            }

            // 全息闪烁叠层（极低透明度的全面板覆盖）
            float flicker = MathF.Sin(globalTimer * 2.4f) * 0.5f + 0.5f;
            FillRect(sb, panelRect, new Color(20, 40, 60) * (alpha * 0.04f * flicker));
        }

        #endregion
    }
}
