using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.HackTime
{
    /// <summary>
    /// 骇入面板渲染器(赛博朋克风格)
    /// <br/>选中目标后协议条目从屏幕右侧依次飞入，无边框容器
    /// <br/>带有故障抖动、扫描线、电路树连接线等赛博动态效果
    /// <br/>所有UI锚定到屏幕坐标，不进行世界坐标转换
    /// </summary>
    internal class HackPanelRenderer
    {
        //每个槽位的飞入进度(0到1)
        private readonly float[] slotFlyIn = new float[QuickHackRegistry.All.Length];
        //每个槽位的悬停动画值
        private readonly float[] slotHoverAnim = new float[QuickHackRegistry.All.Length];
        //每个槽位的故障抖动随机种子
        private readonly float[] slotGlitchSeed = new float[QuickHackRegistry.All.Length];
        //各槽位的实际绘制矩形（用于悬停检测）
        private readonly Rectangle[] slotRects = new Rectangle[QuickHackRegistry.All.Length];
        //当前悬停的槽位索引
        private int hoveredSlot = -1;
        //当前正在上传的槽位索引
        private int uploadingSlot = -1;
        //上传进度(0到1)
        private float uploadProgress;
        //上传完成后的闪烁计时
        private float uploadFlashTimer;
        //全局计时器
        private float timer;
        //是否显示
        private bool visible;
        //上传完成标记
        private bool uploadComplete;
        //列表展开计时(秒)，Show()时归零
        private float revealTime;

        //条目布局常量
        private const float ItemWidth = 280f;
        private const float ItemHeight = 42f;
        private const float ItemGap = 4f;
        private const float RightMargin = 30f;
        //左侧斜切宽度（平行四边形切角效果）
        private const float SlashWidth = 14f;
        //电路树主干距条目左边缘的偏移
        private const float TrunkOffsetX = 36f;
        //首个条目飞入前的基础延迟(秒)
        private const float BaseEntryDelay = 0.2f;
        //每个条目之间的飞入间隔(秒)
        private const float EntryStagger = 0.07f;

        public int UploadingSlot => uploadingSlot;
        public float UploadProgressValue => uploadProgress;
        public bool HasUploadCompleted => uploadComplete;

        public void Show() {
            visible = true;
            hoveredSlot = -1;
            uploadingSlot = -1;
            uploadProgress = 0f;
            uploadFlashTimer = 0f;
            uploadComplete = false;
            revealTime = 0f;
            Array.Clear(slotFlyIn);
            Array.Clear(slotHoverAnim);
            for (int i = 0; i < slotGlitchSeed.Length; i++)
                slotGlitchSeed[i] = Main.rand.NextFloat() * 100f;
        }

        public void Hide() {
            visible = false;
            hoveredSlot = -1;
            CancelUpload();
        }

        public void CancelUpload() {
            uploadingSlot = -1;
            uploadProgress = 0f;
            uploadFlashTimer = 0f;
            uploadComplete = false;
        }

        public QuickHackDef ConsumeUploadResult() {
            if (!uploadComplete || uploadingSlot < 0) return null;
            var hack = QuickHackRegistry.All[uploadingSlot];
            uploadComplete = false;
            uploadingSlot = -1;
            uploadProgress = 0f;
            return hack;
        }

        public void Update() {
            timer += 0.016f;

            if (!visible) {
                //收起：所有条目平滑飞出
                for (int i = 0; i < slotFlyIn.Length; i++)
                    slotFlyIn[i] = MathHelper.Lerp(slotFlyIn[i], 0f, 0.15f);
                revealTime = Math.Max(revealTime - 0.032f, 0f);
                return;
            }

            revealTime += 0.016f;

            //各条目依次飞入，带有追赶式加速
            for (int i = 0; i < slotFlyIn.Length; i++) {
                float delay = BaseEntryDelay + i * EntryStagger;
                float elapsed = revealTime - delay;
                if (elapsed <= 0f) continue;
                float speed = 0.1f + elapsed * 0.25f;
                slotFlyIn[i] = MathHelper.Lerp(slotFlyIn[i], 1f, Math.Min(speed, 0.22f));
                if (slotFlyIn[i] > 0.995f) slotFlyIn[i] = 1f;
            }

            //上传进度推进
            if (uploadingSlot >= 0 && !uploadComplete) {
                var hack = QuickHackRegistry.All[uploadingSlot];
                uploadProgress += 1f / hack.UploadTime;
                if (uploadProgress >= 1f) {
                    uploadProgress = 1f;
                    uploadComplete = true;
                    uploadFlashTimer = 0f;
                }
            }

            if (uploadComplete)
                uploadFlashTimer += 0.016f;

            UpdateHover();
        }

        private void UpdateHover() {
            hoveredSlot = -1;
            int mx = Main.mouseX;
            int my = Main.mouseY;
            for (int i = 0; i < slotRects.Length; i++) {
                if (slotFlyIn[i] < 0.8f) continue;
                if (slotRects[i].Contains(mx, my)) {
                    hoveredSlot = i;
                    break;
                }
            }

            for (int i = 0; i < slotHoverAnim.Length; i++) {
                float target = 0f;
                if (i == hoveredSlot) target = 1f;
                else if (i == uploadingSlot) target = 0.5f;
                slotHoverAnim[i] = MathHelper.Lerp(slotHoverAnim[i], target, 0.2f);
            }
        }

        public void HandleClick() {
            if (!visible) return;
            if (hoveredSlot >= 0 && hoveredSlot != uploadingSlot) {
                uploadingSlot = hoveredSlot;
                uploadProgress = 0f;
                uploadComplete = false;
                uploadFlashTimer = 0f;
            }
        }

        public bool ContainsMouse() {
            if (!visible) return false;
            int mx = Main.mouseX;
            int my = Main.mouseY;
            for (int i = 0; i < slotRects.Length; i++) {
                if (slotFlyIn[i] < 0.5f) continue;
                if (slotRects[i].Contains(mx, my)) return true;
            }
            return false;
        }

        #region 绘制入口

        public void Draw(SpriteBatch sb) {
            DrawTargetFrame(sb);

            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;
            float alpha = HackTime.Intensity;
            if (alpha < 0.01f) return;

            DrawConnectorTree(sb, px, alpha);
            DrawItems(sb, px, alpha);
            DrawStatusBar(sb, px, alpha);
        }

        #endregion

        #region 电路树连接线

        /// <summary>
        /// 绘制从屏幕中心到条目列表的电路树连接线
        /// <br/>激活时水平线先延伸，然后垂直主干展开，最后分支连向各条目
        /// </summary>
        private void DrawConnectorTree(SpriteBatch sb, Texture2D px, float alpha) {
            if (HackTime.SelectedTargetIndex < 0) return;

            int count = QuickHackRegistry.All.Length;
            float totalH = count * (ItemHeight + ItemGap) - ItemGap;
            float listStartY = (Main.screenHeight - totalH) * 0.5f;
            float baseX = Main.screenWidth - RightMargin - ItemWidth;
            float trunkX = baseX - TrunkOffsetX;
            Vector2 screenCenter = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);

            //电路激活进度
            float wireProgress = Math.Clamp(revealTime * 3f, 0f, 1f);
            float wireAlpha = alpha * wireProgress * 0.4f;
            Color wireColor = HackTheme.Accent * wireAlpha;

            //主干水平线：从屏幕中心向右延伸到树干位置
            float hLineEnd = MathHelper.Lerp(screenCenter.X, trunkX, EaseOutCubic(wireProgress));
            DrawLine(sb, px, screenCenter, new Vector2(hLineEnd, screenCenter.Y), 1f, wireColor);

            //水平线上的间隔节点（小方点）
            float nodeSpacing = 40f;
            for (float nx = screenCenter.X + nodeSpacing; nx < hLineEnd; nx += nodeSpacing) {
                sb.Draw(px, new Vector2(nx - 1, screenCenter.Y - 1),
                    new Rectangle(0, 0, 1, 1), HackTheme.Accent * (wireAlpha * 0.4f),
                    0, Vector2.Zero, 3f, SpriteEffects.None, 0);
            }

            //垂直树干（延迟展开）
            if (wireProgress > 0.3f) {
                float trunkProg = Math.Clamp((wireProgress - 0.3f) / 0.7f, 0f, 1f);
                float trunkTop = MathHelper.Lerp(screenCenter.Y, listStartY + ItemHeight * 0.5f, trunkProg);
                float trunkBot = MathHelper.Lerp(screenCenter.Y, listStartY + totalH - ItemHeight * 0.5f, trunkProg);
                DrawLine(sb, px, new Vector2(trunkX, trunkTop), new Vector2(trunkX, trunkBot), 1f, wireColor * 0.7f);

                //各条目的分支线
                for (int i = 0; i < count; i++) {
                    float fly = slotFlyIn[i];
                    if (fly < 0.05f) continue;
                    float itemCY = listStartY + i * (ItemHeight + ItemGap) + ItemHeight * 0.5f;
                    float branchEnd = MathHelper.Lerp(trunkX, baseX - 4, fly);
                    Color branchColor = wireColor * 0.5f;
                    if (i == hoveredSlot) branchColor = HackTheme.Accent * (wireAlpha * 0.8f);
                    if (i == uploadingSlot) branchColor = HackTheme.Uploading * (wireAlpha * 0.7f);
                    DrawLine(sb, px, new Vector2(trunkX, itemCY), new Vector2(branchEnd, itemCY), 1f, branchColor);
                    //分支节点
                    sb.Draw(px, new Vector2(trunkX - 1, itemCY - 1),
                        new Rectangle(0, 0, 1, 1), branchColor * 1.5f,
                        0, Vector2.Zero, 3f, SpriteEffects.None, 0);
                }
            }

            //沿电路流动的数据光点
            float flowT = (timer * 0.8f) % 1f;
            Vector2 flowPos;
            if (flowT < 0.5f) {
                float t = flowT / 0.5f;
                flowPos = Vector2.Lerp(screenCenter, new Vector2(trunkX, screenCenter.Y), t);
            }
            else {
                float t = (flowT - 0.5f) / 0.5f;
                float tTop = listStartY + ItemHeight * 0.5f;
                float tBot = listStartY + totalH - ItemHeight * 0.5f;
                flowPos = new Vector2(trunkX, MathHelper.Lerp(tTop, tBot, t));
            }
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null) {
                Color dotGlow = HackTheme.Accent * (alpha * 0.35f);
                dotGlow.A = 0;
                sb.Draw(glow, flowPos, null, dotGlow, 0, glow.Size() / 2, 0.07f, SpriteEffects.None, 0);
            }
        }

        #endregion

        #region 协议条目列表

        private void DrawItems(SpriteBatch sb, Texture2D px, float alpha) {
            int count = QuickHackRegistry.All.Length;
            float totalH = count * (ItemHeight + ItemGap) - ItemGap;
            float startY = (Main.screenHeight - totalH) * 0.5f;
            float baseX = Main.screenWidth - RightMargin - ItemWidth;

            for (int i = 0; i < count; i++) {
                float fly = slotFlyIn[i];
                if (fly < 0.01f) {
                    slotRects[i] = Rectangle.Empty;
                    continue;
                }

                QuickHackDef hack = QuickHackRegistry.All[i];
                float hover = slotHoverAnim[i];
                float y = startY + i * (ItemHeight + ItemGap);

                //飞入偏移（弹性过冲缓动）
                float flyOffset = (1f - EaseOutBack(fly)) * 350f;
                //故障抖动：飞入时水平随机偏移，靠近终点时衰减
                float glitch = 0f;
                if (fly < 0.85f) {
                    float seed = slotGlitchSeed[i] + timer * 25f;
                    glitch = (MathF.Sin(seed) + MathF.Sin(seed * 2.7f) * 0.5f) * (1f - fly) * 12f;
                }

                float x = baseX + flyOffset + glitch;
                //悬停时向左扩展
                float hoverExpand = hover * 14f;
                float w = ItemWidth + hoverExpand;
                Rectangle rect = new((int)(x - hoverExpand), (int)y, (int)w, (int)ItemHeight);
                slotRects[i] = rect;

                float itemAlpha = alpha * Math.Min(fly * 2.5f, 1f);
                bool isUploading = (i == uploadingSlot && !uploadComplete);
                bool isCompleted = (i == uploadingSlot && uploadComplete);

                DrawSingleItem(sb, px, itemAlpha, rect, hack, i, hover, isUploading, isCompleted);
            }
        }

        private void DrawSingleItem(SpriteBatch sb, Texture2D px, float alpha, Rectangle rect,
            QuickHackDef hack, int index, float hover, bool isUploading, bool isCompleted) {

            //=== 背景 ===
            Color bgColor = Color.Lerp(HackTheme.BgSlot, HackTheme.BgSlotHover, hover * 0.6f);
            if (isUploading) bgColor = Color.Lerp(bgColor, HackTheme.Uploading, 0.06f);
            if (isCompleted) {
                float flash = MathF.Sin(uploadFlashTimer * 10f) * 0.5f + 0.5f;
                bgColor = Color.Lerp(bgColor, HackTheme.Accent, flash * 0.12f);
            }
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), bgColor * (alpha * 0.88f));

            //左侧斜切遮罩（深色三角覆盖，模拟切角）
            DrawSlashCut(sb, px, rect, alpha);

            //=== 类别色条（斜切线内侧） ===
            Color catColor = GetCategoryColor(hack.Category);
            float barGlow = isUploading ? 0.9f : (0.3f + hover * 0.6f);
            int barX = rect.X + (int)SlashWidth;
            sb.Draw(px, new Rectangle(barX, rect.Y + 3, 2, rect.Height - 6),
                new Rectangle(0, 0, 1, 1), catColor * (alpha * barGlow));

            //=== 扫描线效果（悬停/上传时横扫） ===
            if (hover > 0.1f || isUploading) {
                float scanSpeed = isUploading ? 2.5f : 1.8f;
                float scanPos = ((timer * scanSpeed + index * 0.4f) % 1.4f) - 0.2f;
                DrawScanLine(sb, px, rect, scanPos, alpha * (isUploading ? 0.25f : hover * 0.18f));
            }

            //=== 边框线 ===
            Color borderCol = isUploading
                ? Color.Lerp(HackTheme.Border, HackTheme.Uploading, 0.5f)
                : Color.Lerp(HackTheme.Border, HackTheme.Accent, hover * 0.5f);
            //顶边（从斜切位置开始）
            sb.Draw(px, new Rectangle(rect.X + (int)SlashWidth, rect.Y, rect.Width - (int)SlashWidth, 1),
                new Rectangle(0, 0, 1, 1), borderCol * (alpha * 0.5f));
            //底边
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), borderCol * (alpha * 0.35f));
            //右边
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height),
                new Rectangle(0, 0, 1, 1), borderCol * (alpha * 0.3f));
            //斜切边线
            DrawSlashEdge(sb, px, rect, borderCol * (alpha * 0.45f));

            //=== 悬停外发光 ===
            if (hover > 0.1f) {
                Texture2D glow = CWRAsset.SoftGlow?.Value;
                if (glow != null) {
                    Color slotGlow = catColor * (alpha * hover * 0.05f);
                    slotGlow.A = 0;
                    sb.Draw(glow, rect.Center.ToVector2(), null, slotGlow, 0,
                        glow.Size() / 2, new Vector2(rect.Width / 35f, rect.Height / 35f),
                        SpriteEffects.None, 0);
                }
            }

            //=== 协议名称 ===
            Color nameColor = Color.Lerp(HackTheme.TextNormal, HackTheme.TextBright, hover);
            if (isUploading) nameColor = Color.Lerp(nameColor, HackTheme.Uploading, 0.35f);
            if (isCompleted) nameColor = HackTheme.Accent;
            Vector2 namePos = new(rect.X + SlashWidth + 10, rect.Y + 5);
            Utils.DrawBorderString(sb, hack.Name, namePos, nameColor * alpha, 0.36f);

            //=== 效果描述 ===
            Vector2 descPos = new(rect.X + SlashWidth + 10, rect.Y + 23);
            Utils.DrawBorderString(sb, hack.Desc, descPos, HackTheme.TextDim * (alpha * 0.6f), 0.22f);

            //=== 右侧状态区 ===
            if (isUploading) {
                DrawUploadIndicator(sb, px, alpha, rect);
            }
            else if (isCompleted) {
                float flash = MathF.Sin(uploadFlashTimer * 8f) * 0.3f + 0.7f;
                Vector2 okSize = FontAssets.MouseText.Value.MeasureString("DONE") * 0.26f;
                Vector2 okPos = new(rect.Right - okSize.X - 10, rect.Y + (rect.Height - okSize.Y) * 0.5f);
                Utils.DrawBorderString(sb, "DONE", okPos, HackTheme.Accent * (alpha * flash), 0.26f);
            }
            else {
                //上传耗时标签
                float sec = hack.UploadTime / 60f;
                string timeStr = $"{sec:F1}s";
                Vector2 ts = FontAssets.MouseText.Value.MeasureString(timeStr) * 0.24f;
                Vector2 tp = new(rect.Right - ts.X - 10, rect.Y + (rect.Height - ts.Y) * 0.5f);
                Utils.DrawBorderString(sb, timeStr, tp, HackTheme.TextDim * (alpha * 0.4f), 0.24f);
            }
        }

        #endregion

        #region 视觉效果

        //左侧斜切遮罩：深色三角逐行覆盖，模拟平行四边形切角
        private static void DrawSlashCut(SpriteBatch sb, Texture2D px, Rectangle rect, float alpha) {
            Color mask = HackTheme.BgDarkest * (alpha * 0.95f);
            int slashW = (int)SlashWidth;
            for (int dy = 0; dy < rect.Height; dy++) {
                float t = (float)dy / rect.Height;
                int cutW = (int)(slashW * (1f - t));
                if (cutW > 0)
                    sb.Draw(px, new Rectangle(rect.X, rect.Y + dy, cutW, 1),
                        new Rectangle(0, 0, 1, 1), mask);
            }
        }

        //斜切边线（沿切角边缘画对角线）
        private static void DrawSlashEdge(SpriteBatch sb, Texture2D px, Rectangle rect, Color color) {
            Vector2 top = new(rect.X + SlashWidth, rect.Y);
            Vector2 bottom = new(rect.X, rect.Bottom);
            DrawLine(sb, px, top, bottom, 1f, color);
        }

        //扫描线：一道竖向亮线从左到右横穿条目
        private static void DrawScanLine(SpriteBatch sb, Texture2D px, Rectangle rect, float pos, float alpha) {
            int lineX = rect.X + (int)(rect.Width * pos);
            if (lineX < rect.X || lineX > rect.Right - 2) return;
            sb.Draw(px, new Rectangle(lineX, rect.Y + 1, 2, rect.Height - 2),
                new Rectangle(0, 0, 1, 1), HackTheme.Accent * alpha);
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null) {
                Color gc = HackTheme.Accent * (alpha * 0.5f);
                gc.A = 0;
                sb.Draw(glow, new Vector2(lineX, rect.Center.Y), null, gc, 0,
                    glow.Size() / 2, new Vector2(0.06f, rect.Height / 50f), SpriteEffects.None, 0);
            }
        }

        //条目内上传进度指示器
        private void DrawUploadIndicator(SpriteBatch sb, Texture2D px, float alpha, Rectangle rect) {
            int barW = 55;
            int barH = 3;
            int barX = rect.Right - barW - 10;
            int barY = rect.Bottom - barH - 5;

            sb.Draw(px, new Rectangle(barX, barY, barW, barH),
                new Rectangle(0, 0, 1, 1), HackTheme.ProgressBg * alpha);
            int fillW = (int)(barW * uploadProgress);
            if (fillW > 0) {
                sb.Draw(px, new Rectangle(barX, barY, fillW, barH),
                    new Rectangle(0, 0, 1, 1), HackTheme.ProgressFill * (alpha * 0.85f));
                Texture2D glow = CWRAsset.SoftGlow?.Value;
                if (glow != null && fillW > 2) {
                    Color tipGlow = HackTheme.ProgressGlow * (alpha * 0.35f);
                    tipGlow.A = 0;
                    sb.Draw(glow, new Vector2(barX + fillW, barY + barH * 0.5f), null,
                        tipGlow, 0, glow.Size() / 2, new Vector2(0.1f, 0.03f), SpriteEffects.None, 0);
                }
            }

            string pct = $"{(int)(uploadProgress * 100)}%";
            Vector2 ps = FontAssets.MouseText.Value.MeasureString(pct) * 0.24f;
            Vector2 pp = new(rect.Right - ps.X - 8, rect.Y + 5);
            Utils.DrawBorderString(sb, pct, pp, HackTheme.Uploading * alpha, 0.24f);
        }

        #endregion

        #region 底部状态栏

        private void DrawStatusBar(SpriteBatch sb, Texture2D px, float alpha) {
            bool anyVisible = false;
            for (int i = 0; i < slotFlyIn.Length; i++) {
                if (slotFlyIn[i] > 0.5f) { anyVisible = true; break; }
            }
            if (!anyVisible) return;

            int count = QuickHackRegistry.All.Length;
            float totalH = count * (ItemHeight + ItemGap) - ItemGap;
            float startY = (Main.screenHeight - totalH) * 0.5f;
            float bottomY = startY + totalH + 10f;
            float baseX = Main.screenWidth - RightMargin - ItemWidth;

            //状态脉冲灯
            float pulse = MathF.Sin(timer * 3.5f) > 0 ? 1f : 0.3f;
            Color dotColor = uploadingSlot >= 0
                ? HackTheme.Uploading * (alpha * pulse)
                : new Color(40, 200, 100) * (alpha * pulse);
            sb.Draw(px, new Vector2(baseX, bottomY + 2),
                new Rectangle(0, 0, 1, 1), dotColor, 0, Vector2.Zero, 3f, SpriteEffects.None, 0);

            //状态文字
            string status = uploadingSlot >= 0 ? "UPLOADING..." : "READY";
            if (uploadComplete) status = "UPLOAD COMPLETE";
            Utils.DrawBorderString(sb, status, new Vector2(baseX + 10, bottomY),
                HackTheme.TextDim * alpha, 0.24f);

            //滚动十六进制标签
            string tag = $"NET::0x{((int)(timer * 100) % 0xFFFF):X4}";
            Utils.DrawBorderString(sb, tag, new Vector2(baseX + ItemWidth - 90, bottomY),
                HackTheme.Accent * (alpha * 0.2f), 0.2f);
        }

        #endregion

        #region 屏幕中央目标锁定框

        /// <summary>
        /// 绘制屏幕中央的目标锁定框
        /// <br/>运镜将NPC推到屏幕中心后，四角括号收拢锁定目标
        /// <br/>附带刻度标记、扫描线、目标名称等HUD信息
        /// </summary>
        private void DrawTargetFrame(SpriteBatch sb) {
            int selIdx = HackTime.SelectedTargetIndex;
            if (selIdx < 0 || selIdx >= Main.maxNPCs) return;

            float camProg = HackTime.CameraProgress;
            if (camProg < 0.01f) return;

            NPC npc = Main.npc[selIdx];
            if (!npc.active) return;

            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            float alpha = HackTime.Intensity * camProg;
            Vector2 center = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);

            //框体大小基于NPC尺寸
            float baseHalfW = Math.Max(npc.width, 32) * 0.6f + 24f;
            float baseHalfH = Math.Max(npc.height, 32) * 0.6f + 24f;

            //收拢动画：从1.8倍缩至最终大小
            float ease = EaseOutCubic(camProg);
            float expand = 1f + (1f - ease) * 0.8f;
            float halfW = baseHalfW * expand;
            float halfH = baseHalfH * expand;

            int armLen = (int)(18f * ease);
            if (armLen < 2) return;
            Color frameColor = HackTheme.Accent * (alpha * 0.7f);
            Color dimColor = HackTheme.Accent * (alpha * 0.25f);

            //四角锁定括号
            DrawFrameBracket(sb, px, center, -halfW, -halfH, armLen, frameColor);
            DrawFrameBracket(sb, px, center, halfW, -halfH, armLen, frameColor);
            DrawFrameBracket(sb, px, center, -halfW, halfH, armLen, frameColor);
            DrawFrameBracket(sb, px, center, halfW, halfH, armLen, frameColor);

            //边缘刻度标记
            if (ease > 0.5f) {
                float tickAlpha = (ease - 0.5f) * 2f * alpha * 0.3f;
                Color tickColor = HackTheme.Border * tickAlpha;
                int tickCount = 5;
                for (int i = 1; i < tickCount; i++) {
                    float t = (float)i / tickCount;
                    float tx = MathHelper.Lerp(center.X - halfW, center.X + halfW, t);
                    float ty = MathHelper.Lerp(center.Y - halfH, center.Y + halfH, t);
                    //水平刻度（顶底）
                    sb.Draw(px, new Rectangle((int)tx, (int)(center.Y - halfH), 1, 4),
                        new Rectangle(0, 0, 1, 1), tickColor);
                    sb.Draw(px, new Rectangle((int)tx, (int)(center.Y + halfH - 4), 1, 4),
                        new Rectangle(0, 0, 1, 1), tickColor);
                    //垂直刻度（左右）
                    sb.Draw(px, new Rectangle((int)(center.X - halfW), (int)ty, 4, 1),
                        new Rectangle(0, 0, 1, 1), tickColor);
                    sb.Draw(px, new Rectangle((int)(center.X + halfW - 4), (int)ty, 4, 1),
                        new Rectangle(0, 0, 1, 1), tickColor);
                }
            }

            //中心细十字
            float crossLen = 6f * ease;
            sb.Draw(px, new Rectangle((int)(center.X - crossLen), (int)center.Y, (int)(crossLen * 2), 1),
                new Rectangle(0, 0, 1, 1), dimColor);
            sb.Draw(px, new Rectangle((int)center.X, (int)(center.Y - crossLen), 1, (int)(crossLen * 2)),
                new Rectangle(0, 0, 1, 1), dimColor);

            //文字标签（延迟出现）
            if (ease > 0.4f) {
                float labelAlpha = (ease - 0.4f) / 0.6f * alpha;

                //顶部"TARGET LOCKED"
                string label = "// TARGET LOCKED";
                Vector2 labelSize = FontAssets.MouseText.Value.MeasureString(label) * 0.26f;
                Vector2 labelPos = new(center.X - labelSize.X * 0.5f, center.Y - halfH - 18f);
                Utils.DrawBorderString(sb, label, labelPos, HackTheme.Accent * (labelAlpha * 0.65f), 0.26f);

                //底部NPC名称
                string npcName = npc.FullName;
                Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(npcName) * 0.28f;
                Vector2 namePos = new(center.X - nameSize.X * 0.5f, center.Y + halfH + 6f);
                Utils.DrawBorderString(sb, npcName, namePos, HackTheme.TextBright * (labelAlpha * 0.55f), 0.28f);
            }

            //水平扫描线（周期性从上到下扫过框体区域）
            float scanT = (timer * 0.6f) % 1f;
            float scanY = center.Y - halfH + scanT * halfH * 2;
            float scanFade = 1f - Math.Abs(scanT - 0.5f) * 2f;
            sb.Draw(px, new Rectangle((int)(center.X - halfW), (int)scanY, (int)(halfW * 2), 1),
                new Rectangle(0, 0, 1, 1), HackTheme.Accent * (alpha * 0.12f * scanFade));
        }

        //绘制单个角的L形括号（臂方向朝内）
        private static void DrawFrameBracket(SpriteBatch sb, Texture2D px,
            Vector2 center, float ox, float oy, int armLen, Color color) {
            int cx = (int)(center.X + ox);
            int cy = (int)(center.Y + oy);
            int dirH = ox < 0 ? 1 : -1;
            int dirV = oy < 0 ? 1 : -1;

            int hx = dirH > 0 ? cx : cx - armLen;
            sb.Draw(px, new Rectangle(hx, cy, armLen, 2), new Rectangle(0, 0, 1, 1), color);

            int vy = dirV > 0 ? cy : cy - armLen;
            sb.Draw(px, new Rectangle(cx, vy, 2, armLen), new Rectangle(0, 0, 1, 1), color);
        }

        #endregion

        #region 工具函数

        private static Color GetCategoryColor(QuickHackCategory cat) => cat switch {
            QuickHackCategory.Lethal => HackTheme.Danger,
            QuickHackCategory.Control => HackTheme.Uploading,
            QuickHackCategory.Covert => HackTheme.AccentAlt,
            QuickHackCategory.Contagion => new Color(160, 40, 200),
            _ => HackTheme.Accent,
        };

        private static void DrawLine(SpriteBatch sb, Texture2D px, Vector2 start, Vector2 end, float thickness, Color color) {
            Vector2 diff = end - start;
            float length = diff.Length();
            if (length < 1f) return;
            sb.Draw(px, start, new Rectangle(0, 0, 1, 1), color, diff.ToRotation(),
                Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        private static float EaseOutCubic(float t) {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        //弹性缓出（轻微过冲后回弹，用于飞入动画）
        private static float EaseOutBack(float t) {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float inv = t - 1f;
            return 1f + c3 * inv * inv * inv + c1 * inv * inv;
        }

        #endregion
    }
}
