using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.HackTime
{
    /// <summary>
    /// 骇入面板渲染器
    /// <br/>当选中目标后在目标右侧展开骇入协议列表面板
    /// <br/>黑墙风格，深色底板配冷青色装饰线和六角形元素
    /// </summary>
    internal class HackPanelRenderer
    {
        //面板展开进度(0到1)
        private float openProgress;
        //每个槽位的独立展开延迟进度
        private readonly float[] slotRevealProgress = new float[QuickHackRegistry.All.Length];
        //每个槽位的悬停动画值
        private readonly float[] slotHoverAnim = new float[QuickHackRegistry.All.Length];
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
        //面板矩形缓存
        private Rectangle panelRect;
        //是否显示
        private bool visible;
        //上传完成标记
        private bool uploadComplete;

        //面板布局常量
        private const float PanelWidth = 260f;
        private const float SlotHeight = 34f;
        private const float SlotGap = 3f;
        private const float HeaderHeight = 30f;
        private const float FooterHeight = 28f;
        private const float PaddingH = 8f;
        private const float PaddingV = 6f;
        //面板距目标NPC的水平偏移
        private const float TargetOffset = 80f;

        /// <summary>
        /// 获取当前正在上传的协议索引，负数表示未上传
        /// </summary>
        public int UploadingSlot => uploadingSlot;
        /// <summary>
        /// 获取上传进度(0到1)
        /// </summary>
        public float UploadProgressValue => uploadProgress;
        /// <summary>
        /// 是否有上传刚刚完成
        /// </summary>
        public bool HasUploadCompleted => uploadComplete;

        /// <summary>
        /// 显示面板
        /// </summary>
        public void Show() {
            visible = true;
            openProgress = 0f;
            hoveredSlot = -1;
            uploadingSlot = -1;
            uploadProgress = 0f;
            uploadFlashTimer = 0f;
            uploadComplete = false;
            Array.Clear(slotRevealProgress);
            Array.Clear(slotHoverAnim);
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public void Hide() {
            visible = false;
            hoveredSlot = -1;
            CancelUpload();
        }

        /// <summary>
        /// 取消当前上传
        /// </summary>
        public void CancelUpload() {
            uploadingSlot = -1;
            uploadProgress = 0f;
            uploadFlashTimer = 0f;
            uploadComplete = false;
        }

        /// <summary>
        /// 确认并消费上传完成标记
        /// </summary>
        public QuickHackDef ConsumeUploadResult() {
            if (!uploadComplete || uploadingSlot < 0) return null;
            var hack = QuickHackRegistry.All[uploadingSlot];
            uploadComplete = false;
            uploadingSlot = -1;
            uploadProgress = 0f;
            return hack;
        }

        /// <summary>
        /// 每帧更新逻辑
        /// </summary>
        public void Update() {
            if (!visible) {
                openProgress = MathHelper.Lerp(openProgress, 0f, 0.12f);
                return;
            }

            timer += 0.016f;

            //面板展开动画
            openProgress = MathHelper.Lerp(openProgress, 1f, 0.1f);
            if (openProgress > 0.995f) openProgress = 1f;

            //各槽位依次展开的延迟动画
            for (int i = 0; i < slotRevealProgress.Length; i++) {
                float delay = i * 0.06f;
                float target = Math.Clamp((openProgress - delay) / (1f - delay), 0f, 1f);
                slotRevealProgress[i] = MathHelper.Lerp(slotRevealProgress[i], target, 0.15f);
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

            if (uploadComplete) {
                uploadFlashTimer += 0.016f;
            }

            //计算面板位置
            UpdatePanelRect();
            //更新悬停检测
            UpdateHover();
        }

        private void UpdatePanelRect() {
            int selIdx = HackTime.SelectedTargetIndex;
            if (selIdx < 0 || selIdx >= Main.maxNPCs) return;

            NPC npc = Main.npc[selIdx];
            if (!npc.active) return;

            //NPC世界坐标转换为屏幕坐标（考虑游戏缩放）
            Vector2 npcScreen = WorldToScreen(npc.Center);
            float totalHeight = HeaderHeight + FooterHeight + PaddingV * 2
                + QuickHackRegistry.All.Length * (SlotHeight + SlotGap);

            int px = (int)(npcScreen.X + TargetOffset);
            int py = (int)(npcScreen.Y - totalHeight * 0.5f);

            //确保面板不超出屏幕
            if (px + PanelWidth > Main.screenWidth - 10)
                px = (int)(npcScreen.X - TargetOffset - PanelWidth);
            if (py < 10) py = 10;
            if (py + totalHeight > Main.screenHeight - 10)
                py = (int)(Main.screenHeight - 10 - totalHeight);

            panelRect = new Rectangle(px, py, (int)PanelWidth, (int)totalHeight);
        }

        private void UpdateHover() {
            hoveredSlot = -1;
            if (openProgress < 0.5f) return;

            int mx = Main.mouseX;
            int my = Main.mouseY;

            if (!panelRect.Contains(mx, my)) return;

            float slotStartY = panelRect.Y + HeaderHeight + PaddingV;
            float slotLeft = panelRect.X + PaddingH;
            float slotWidth = PanelWidth - PaddingH * 2;

            for (int i = 0; i < QuickHackRegistry.All.Length; i++) {
                float sy = slotStartY + i * (SlotHeight + SlotGap);
                Rectangle slotRect = new((int)slotLeft, (int)sy, (int)slotWidth, (int)SlotHeight);
                if (slotRect.Contains(mx, my)) {
                    hoveredSlot = i;
                    break;
                }
            }

            //悬停动画插值
            for (int i = 0; i < slotHoverAnim.Length; i++) {
                float target = 0f;
                if (i == hoveredSlot) target = 1f;
                else if (i == uploadingSlot) target = 0.6f;
                slotHoverAnim[i] = MathHelper.Lerp(slotHoverAnim[i], target, 0.18f);
            }
        }

        /// <summary>
        /// 处理鼠标点击事件
        /// </summary>
        public void HandleClick() {
            if (!visible || openProgress < 0.8f) return;

            if (hoveredSlot >= 0 && hoveredSlot != uploadingSlot) {
                //点击新协议开始上传
                uploadingSlot = hoveredSlot;
                uploadProgress = 0f;
                uploadComplete = false;
                uploadFlashTimer = 0f;
            }
        }

        /// <summary>
        /// 判断鼠标是否在面板区域内
        /// </summary>
        public bool ContainsMouse() {
            return visible && panelRect.Contains(Main.mouseX, Main.mouseY);
        }

        /// <summary>
        /// 绘制完整的骇入面板
        /// </summary>
        public void Draw(SpriteBatch sb) {
            if (openProgress < 0.01f) return;
            if (HackTime.SelectedTargetIndex < 0) return;

            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            float alpha = openProgress * HackTime.Intensity;
            if (alpha < 0.01f) return;

            //应用展开动画变换
            float easedOpen = EaseOutCubic(openProgress);
            int drawWidth = (int)(panelRect.Width * easedOpen);
            Rectangle drawRect = new(
                panelRect.X + (panelRect.Width - drawWidth) / 2,
                panelRect.Y,
                drawWidth,
                panelRect.Height
            );

            DrawPanelBackground(sb, px, alpha, drawRect);
            DrawHeader(sb, px, alpha, drawRect);

            if (openProgress > 0.3f) {
                DrawSlots(sb, px, alpha, drawRect);
            }

            DrawFooter(sb, px, alpha, drawRect);
            DrawCorners(sb, px, alpha, drawRect);
            DrawConnectionLine(sb, px, alpha, drawRect);
        }

        /// <summary>
        /// 绘制面板背景
        /// </summary>
        private void DrawPanelBackground(SpriteBatch sb, Texture2D px, float alpha, Rectangle rect) {
            //主背景填充
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), HackTheme.BgPanel * (alpha * 0.92f));

            //内侧暗角渐变
            Color vigColor = HackTheme.InnerShadow * (alpha * 0.9f);
            int vigSize = 6;
            for (int i = 0; i < vigSize; i++) {
                float fade = 1f - (float)i / vigSize;
                sb.Draw(px, new Rectangle(rect.X, rect.Y + i, rect.Width, 1),
                    new Rectangle(0, 0, 1, 1), vigColor * fade);
                sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1 - i, rect.Width, 1),
                    new Rectangle(0, 0, 1, 1), vigColor * fade);
                sb.Draw(px, new Rectangle(rect.X + i, rect.Y, 1, rect.Height),
                    new Rectangle(0, 0, 1, 1), vigColor * (fade * 0.5f));
                sb.Draw(px, new Rectangle(rect.Right - 1 - i, rect.Y, 1, rect.Height),
                    new Rectangle(0, 0, 1, 1), vigColor * (fade * 0.5f));
            }

            //外发光
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null) {
                Color panelGlow = HackTheme.Accent * (alpha * 0.04f);
                panelGlow.A = 0;
                Vector2 center = rect.Center.ToVector2();
                sb.Draw(glow, center, null, panelGlow, 0, glow.Size() / 2,
                    new Vector2(rect.Width / 40f, rect.Height / 40f), SpriteEffects.None, 0);
            }

            //极淡的网格线——增加面板质感
            Color gridColor = HackTheme.GridLine * (alpha * 0.12f);
            float spacing = 20f;
            for (float gx = rect.X + spacing; gx < rect.Right; gx += spacing) {
                sb.Draw(px, new Rectangle((int)gx, rect.Y, 1, rect.Height),
                    new Rectangle(0, 0, 1, 1), gridColor);
            }
            for (float gy = rect.Y + spacing; gy < rect.Bottom; gy += spacing) {
                sb.Draw(px, new Rectangle(rect.X, (int)gy, rect.Width, 1),
                    new Rectangle(0, 0, 1, 1), gridColor);
            }
        }

        /// <summary>
        /// 绘制标题栏
        /// </summary>
        private void DrawHeader(SpriteBatch sb, Texture2D px, float alpha, Rectangle rect) {
            //标题栏背景
            Rectangle headerRect = new(rect.X + 1, rect.Y + 1, rect.Width - 2, (int)HeaderHeight);
            sb.Draw(px, headerRect, new Rectangle(0, 0, 1, 1), HackTheme.BgSection * (alpha * 0.85f));

            //顶部强调线，带呼吸脉冲
            float breathe = MathF.Sin(timer * 2.2f) * 0.12f + 0.88f;
            Color topLine = HackTheme.Accent * (alpha * 0.75f * breathe);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 2),
                new Rectangle(0, 0, 1, 1), topLine);

            //标题文字
            string title = "// BREACH PROTOCOL";
            Vector2 titleSize = FontAssets.MouseText.Value.MeasureString(title) * 0.38f;
            Vector2 titlePos = new(rect.X + 10, rect.Y + (HeaderHeight - titleSize.Y) * 0.5f + 1);
            Utils.DrawBorderString(sb, title, titlePos, HackTheme.Accent * (alpha * 0.9f), 0.38f);

            //标题后的脉冲光标
            float cursorBlink = MathF.Sin(timer * 5f) > 0 ? 1f : 0f;
            sb.Draw(px, new Vector2(titlePos.X + titleSize.X + 4, titlePos.Y + 2),
                new Rectangle(0, 0, 1, 1), HackTheme.Accent * (alpha * 0.6f * cursorBlink),
                0, Vector2.Zero, new Vector2(2, titleSize.Y - 4), SpriteEffects.None, 0);

            //标题下分割线
            int divY = rect.Y + (int)HeaderHeight;
            Color divColor = HackTheme.Border * (alpha * 0.5f);
            sb.Draw(px, new Rectangle(rect.X + 6, divY, rect.Width - 12, 1),
                new Rectangle(0, 0, 1, 1), divColor);

            //分割线上的小菱形节点
            int notchX = rect.X + rect.Width / 2;
            DrawDiamond(sb, px, new Vector2(notchX, divY), 3f, HackTheme.Accent * (alpha * 0.5f));
        }

        /// <summary>
        /// 绘制协议槽位列表
        /// </summary>
        private void DrawSlots(SpriteBatch sb, Texture2D px, float alpha, Rectangle rect) {
            float slotStartY = rect.Y + HeaderHeight + PaddingV;
            float slotLeft = rect.X + PaddingH;
            float slotWidth = rect.Width - PaddingH * 2;

            for (int i = 0; i < QuickHackRegistry.All.Length; i++) {
                QuickHackDef hack = QuickHackRegistry.All[i];
                float reveal = slotRevealProgress[i];
                if (reveal < 0.01f) continue;

                float hover = slotHoverAnim[i];
                float sy = slotStartY + i * (SlotHeight + SlotGap);
                //展开时从右侧滑入
                float slideOffset = (1f - EaseOutCubic(reveal)) * 40f;
                Rectangle slotRect = new(
                    (int)(slotLeft + slideOffset),
                    (int)sy, (int)(slotWidth * reveal), (int)SlotHeight);

                float slotAlpha = alpha * reveal;
                bool isUploading = (i == uploadingSlot && !uploadComplete);
                bool isCompleted = (i == uploadingSlot && uploadComplete);

                DrawSingleSlot(sb, px, slotAlpha, slotRect, hack, i, hover, isUploading, isCompleted);
            }
        }

        /// <summary>
        /// 绘制单个协议槽位
        /// </summary>
        private void DrawSingleSlot(SpriteBatch sb, Texture2D px, float alpha, Rectangle rect,
            QuickHackDef hack, int index, float hover, bool isUploading, bool isCompleted) {

            //槽位背景
            Color bgColor = Color.Lerp(HackTheme.BgSlot, HackTheme.BgSlotHover, hover * 0.5f);
            if (isUploading) bgColor = Color.Lerp(bgColor, HackTheme.Uploading, 0.08f);
            if (isCompleted) {
                float flash = MathF.Sin(uploadFlashTimer * 12f) * 0.5f + 0.5f;
                bgColor = Color.Lerp(bgColor, HackTheme.Accent, flash * 0.15f);
            }
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), bgColor * (alpha * 0.9f));

            //内部凹陷阴影
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), HackTheme.InnerShadow * (alpha * 0.6f));
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 1, rect.Height),
                new Rectangle(0, 0, 1, 1), HackTheme.InnerShadow * (alpha * 0.3f));

            //左侧类别色条
            Color catColor = GetCategoryColor(hack.Category);
            float barAlpha = isUploading ? 0.9f : (0.4f + hover * 0.5f);
            sb.Draw(px, new Rectangle(rect.X, rect.Y + 2, 3, rect.Height - 4),
                new Rectangle(0, 0, 1, 1), catColor * (alpha * barAlpha));

            //悬停时左边框发光
            if (hover > 0.1f) {
                Texture2D glow = CWRAsset.SoftGlow?.Value;
                if (glow != null) {
                    Color slotGlow = HackTheme.Accent * (alpha * hover * 0.08f);
                    slotGlow.A = 0;
                    sb.Draw(glow, rect.Center.ToVector2(), null, slotGlow, 0,
                        glow.Size() / 2, new Vector2(rect.Width / 50f, rect.Height / 50f),
                        SpriteEffects.None, 0);
                }
            }

            //边框
            Color borderCol = isUploading
                ? Color.Lerp(HackTheme.Border, HackTheme.Uploading, 0.4f)
                : Color.Lerp(HackTheme.Border, HackTheme.Accent, hover * 0.4f);
            //顶边
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), borderCol * (alpha * 0.5f));
            //底边
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), borderCol * (alpha * 0.3f));
            //右边
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height),
                new Rectangle(0, 0, 1, 1), borderCol * (alpha * 0.25f));

            //协议名称
            Color nameColor = Color.Lerp(HackTheme.TextNormal, HackTheme.TextBright, hover);
            if (isUploading) nameColor = Color.Lerp(nameColor, HackTheme.Uploading, 0.3f);
            if (isCompleted) nameColor = HackTheme.Accent;
            Vector2 namePos = new(rect.X + 10, rect.Y + 4);
            Utils.DrawBorderString(sb, hack.Name, namePos, nameColor * alpha, 0.34f);

            //效果描述
            Vector2 descPos = new(rect.X + 10, rect.Y + 18);
            Utils.DrawBorderString(sb, hack.Desc, descPos, HackTheme.TextDim * (alpha * 0.7f), 0.24f);

            //右侧状态指示
            if (isUploading) {
                DrawUploadBar(sb, px, alpha, rect);
            }
            else if (isCompleted) {
                //完成标记
                string doneText = "[OK]";
                Vector2 doneSize = FontAssets.MouseText.Value.MeasureString(doneText) * 0.28f;
                Vector2 donePos = new(rect.Right - doneSize.X - 8, rect.Y + (rect.Height - doneSize.Y) * 0.5f);
                float flash = MathF.Sin(uploadFlashTimer * 8f) * 0.3f + 0.7f;
                Utils.DrawBorderString(sb, doneText, donePos, HackTheme.Accent * (alpha * flash), 0.28f);
            }
            else {
                //上传时间标签
                float seconds = hack.UploadTime / 60f;
                string timeText = $"{seconds:F1}s";
                Vector2 timeSize = FontAssets.MouseText.Value.MeasureString(timeText) * 0.26f;
                Vector2 timePos = new(rect.Right - timeSize.X - 8, rect.Y + (rect.Height - timeSize.Y) * 0.5f);
                Utils.DrawBorderString(sb, timeText, timePos, HackTheme.TextDim * (alpha * 0.5f), 0.26f);
            }
        }

        /// <summary>
        /// 绘制上传进度条
        /// </summary>
        private void DrawUploadBar(SpriteBatch sb, Texture2D px, float alpha, Rectangle slotRect) {
            int barH = 4;
            int barW = slotRect.Width - 16;
            int barX = slotRect.X + 8;
            int barY = slotRect.Bottom - barH - 3;

            //进度条背景
            sb.Draw(px, new Rectangle(barX, barY, barW, barH),
                new Rectangle(0, 0, 1, 1), HackTheme.ProgressBg * alpha);

            //已完成部分
            int fillW = (int)(barW * uploadProgress);
            if (fillW > 0) {
                sb.Draw(px, new Rectangle(barX, barY, fillW, barH),
                    new Rectangle(0, 0, 1, 1), HackTheme.ProgressFill * (alpha * 0.85f));

                //填充前端的亮点
                Texture2D glow = CWRAsset.SoftGlow?.Value;
                if (glow != null && fillW > 2) {
                    Color tipGlow = HackTheme.ProgressGlow * (alpha * 0.4f);
                    tipGlow.A = 0;
                    sb.Draw(glow, new Vector2(barX + fillW, barY + barH * 0.5f), null,
                        tipGlow, 0, glow.Size() / 2, new Vector2(0.12f, 0.04f), SpriteEffects.None, 0);
                }
            }

            //进度条边框
            sb.Draw(px, new Rectangle(barX, barY, barW, 1),
                new Rectangle(0, 0, 1, 1), HackTheme.Border * (alpha * 0.3f));
            sb.Draw(px, new Rectangle(barX, barY + barH - 1, barW, 1),
                new Rectangle(0, 0, 1, 1), HackTheme.Border * (alpha * 0.2f));

            //百分比文字
            string pctText = $"{(int)(uploadProgress * 100)}%";
            Vector2 pctSize = FontAssets.MouseText.Value.MeasureString(pctText) * 0.24f;
            Vector2 pctPos = new(slotRect.Right - pctSize.X - 6, slotRect.Y + 4);
            Utils.DrawBorderString(sb, pctText, pctPos, HackTheme.Uploading * alpha, 0.24f);
        }

        /// <summary>
        /// 绘制底部状态栏
        /// </summary>
        private void DrawFooter(SpriteBatch sb, Texture2D px, float alpha, Rectangle rect) {
            int footerY = rect.Bottom - (int)FooterHeight;
            Rectangle footerRect = new(rect.X + 1, footerY, rect.Width - 2, (int)FooterHeight - 1);
            sb.Draw(px, footerRect, new Rectangle(0, 0, 1, 1), HackTheme.BgSection * (alpha * 0.7f));

            //上方分割线
            sb.Draw(px, new Rectangle(rect.X + 6, footerY - 1, rect.Width - 12, 1),
                new Rectangle(0, 0, 1, 1), HackTheme.Border * (alpha * 0.35f));

            //状态指示灯
            float statusPulse = MathF.Sin(timer * 3.5f) > 0 ? 1f : 0.3f;
            Color dotColor = uploadingSlot >= 0
                ? HackTheme.Uploading * (alpha * statusPulse)
                : new Color(40, 200, 100) * (alpha * statusPulse);
            sb.Draw(px, new Vector2(rect.X + 8, footerY + 8),
                new Rectangle(0, 0, 1, 1), dotColor, 0, Vector2.Zero, 3f, SpriteEffects.None, 0);

            //状态文字
            string statusText = uploadingSlot >= 0 ? "UPLOADING..." : "READY";
            if (uploadComplete) statusText = "UPLOAD COMPLETE";
            Utils.DrawBorderString(sb, statusText, new Vector2(rect.X + 16, footerY + 5),
                HackTheme.TextDim * alpha, 0.24f);

            //右下角滚动数据标签
            string netTag = $"0x{((int)(timer * 100) % 0xFFFF):X4}";
            Utils.DrawBorderString(sb, netTag, new Vector2(rect.Right - 56, footerY + 5),
                HackTheme.Accent * (alpha * 0.25f), 0.22f);
        }

        /// <summary>
        /// 绘制四角装饰
        /// </summary>
        private void DrawCorners(SpriteBatch sb, Texture2D px, float alpha, Rectangle rect) {
            Color cBright = HackTheme.Accent * (alpha * 0.65f);
            Color cDim = HackTheme.Accent * (alpha * 0.25f);
            int armLen = 16;
            int armInner = 8;
            int inset = 4;

            //左上
            sb.Draw(px, new Rectangle(rect.X, rect.Y, armLen, 2), new Rectangle(0, 0, 1, 1), cBright);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 2, armLen), new Rectangle(0, 0, 1, 1), cBright);
            sb.Draw(px, new Rectangle(rect.X + inset, rect.Y + inset, armInner, 1), new Rectangle(0, 0, 1, 1), cDim);
            sb.Draw(px, new Rectangle(rect.X + inset, rect.Y + inset, 1, armInner), new Rectangle(0, 0, 1, 1), cDim);
            //右上
            sb.Draw(px, new Rectangle(rect.Right - armLen, rect.Y, armLen, 2), new Rectangle(0, 0, 1, 1), cBright);
            sb.Draw(px, new Rectangle(rect.Right - 2, rect.Y, 2, armLen), new Rectangle(0, 0, 1, 1), cBright);
            sb.Draw(px, new Rectangle(rect.Right - inset - armInner, rect.Y + inset, armInner, 1), new Rectangle(0, 0, 1, 1), cDim);
            sb.Draw(px, new Rectangle(rect.Right - inset - 1, rect.Y + inset, 1, armInner), new Rectangle(0, 0, 1, 1), cDim);
            //左下
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 2, armLen, 2), new Rectangle(0, 0, 1, 1), cBright * 0.6f);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - armLen, 2, armLen), new Rectangle(0, 0, 1, 1), cBright * 0.6f);
            sb.Draw(px, new Rectangle(rect.X + inset, rect.Bottom - inset - 1, armInner, 1), new Rectangle(0, 0, 1, 1), cDim * 0.7f);
            sb.Draw(px, new Rectangle(rect.X + inset, rect.Bottom - inset - armInner, 1, armInner), new Rectangle(0, 0, 1, 1), cDim * 0.7f);
            //右下
            sb.Draw(px, new Rectangle(rect.Right - armLen, rect.Bottom - 2, armLen, 2), new Rectangle(0, 0, 1, 1), cBright * 0.6f);
            sb.Draw(px, new Rectangle(rect.Right - 2, rect.Bottom - armLen, 2, armLen), new Rectangle(0, 0, 1, 1), cBright * 0.6f);
            sb.Draw(px, new Rectangle(rect.Right - inset - armInner, rect.Bottom - inset - 1, armInner, 1), new Rectangle(0, 0, 1, 1), cDim * 0.7f);
            sb.Draw(px, new Rectangle(rect.Right - inset - 1, rect.Bottom - inset - armInner, 1, armInner), new Rectangle(0, 0, 1, 1), cDim * 0.7f);

            //顶部边缘脉冲光
            float pulsePos = (timer * 0.4f) % 1f;
            int pulseX = rect.X + (int)(pulsePos * rect.Width);
            sb.Draw(px, new Rectangle(pulseX - 12, rect.Y, 24, 2),
                new Rectangle(0, 0, 1, 1), HackTheme.EdgeGlow * (alpha * 0.45f));

            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null) {
                Color pulseGlow = HackTheme.EdgeGlow * (alpha * 0.2f);
                pulseGlow.A = 0;
                sb.Draw(glow, new Vector2(pulseX, rect.Y), null, pulseGlow, 0,
                    glow.Size() / 2, new Vector2(0.3f, 0.06f), SpriteEffects.None, 0);
            }
        }

        /// <summary>
        /// 绘制面板到目标的连接线
        /// </summary>
        private void DrawConnectionLine(SpriteBatch sb, Texture2D px, float alpha, Rectangle rect) {
            int selIdx = HackTime.SelectedTargetIndex;
            if (selIdx < 0 || selIdx >= Main.maxNPCs) return;

            NPC npc = Main.npc[selIdx];
            if (!npc.active) return;

            Vector2 npcScreen = WorldToScreen(npc.Center);
            //连接线起点为面板左侧中点
            Vector2 lineStart = new(rect.X, rect.Y + rect.Height * 0.5f);
            //如果面板在NPC左边则从右侧连接
            if (rect.X + rect.Width * 0.5f < npcScreen.X) {
                lineStart.X = rect.Right;
            }

            float lineAlpha = alpha * openProgress * 0.6f;
            Color lineColor = HackTheme.Accent * lineAlpha;

            //折线连接：从面板边缘水平延伸一段，再垂直转向NPC高度，最后水平连到NPC
            //水平延伸距离：面板到NPC距离的30%，至少20px
            float hExtend = Math.Max(Math.Abs(npcScreen.X - lineStart.X) * 0.3f, 20f);
            //判断面板在NPC哪一侧来决定延伸方向
            float dir = (lineStart.X < npcScreen.X) ? -1f : 1f;
            Vector2 midPoint = new(lineStart.X, lineStart.Y + dir * hExtend);
            Vector2 turnPoint = new(midPoint.X, npcScreen.Y);

            DrawLine(sb, px, lineStart, midPoint, 1f, lineColor);
            DrawLine(sb, px, midPoint, turnPoint, 1f, lineColor * 0.7f);
            DrawLine(sb, px, turnPoint, npcScreen, 1f, lineColor * 0.5f);

            //连接线上的流动数据点
            float dataFlow = (timer * 1.5f) % 1f;
            Vector2 flowPos = EvaluatePath(dataFlow, lineStart, midPoint, turnPoint, npcScreen);
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null) {
                Color dotGlow = HackTheme.Accent * (lineAlpha * 0.5f);
                dotGlow.A = 0;
                sb.Draw(glow, flowPos, null, dotGlow, 0, glow.Size() / 2, 0.06f, SpriteEffects.None, 0);
            }
        }

        //获取类别对应的色条颜色
        private static Color GetCategoryColor(QuickHackCategory cat) => cat switch {
            QuickHackCategory.Lethal => HackTheme.Danger,
            QuickHackCategory.Control => HackTheme.Uploading,
            QuickHackCategory.Covert => HackTheme.AccentAlt,
            QuickHackCategory.Contagion => new Color(160, 40, 200),
            _ => HackTheme.Accent,
        };

        //绘制小菱形标记
        private static void DrawDiamond(SpriteBatch sb, Texture2D px, Vector2 center, float size, Color color) {
            DrawLine(sb, px, center + new Vector2(-size, 0), center + new Vector2(0, -size), 1f, color);
            DrawLine(sb, px, center + new Vector2(0, -size), center + new Vector2(size, 0), 1f, color);
            DrawLine(sb, px, center + new Vector2(size, 0), center + new Vector2(0, size), 1f, color);
            DrawLine(sb, px, center + new Vector2(0, size), center + new Vector2(-size, 0), 1f, color);
        }

        //绘制直线
        private static void DrawLine(SpriteBatch sb, Texture2D px, Vector2 start, Vector2 end, float thickness, Color color) {
            Vector2 diff = end - start;
            float length = diff.Length();
            if (length < 1f) return;
            sb.Draw(px, start, new Rectangle(0, 0, 1, 1), color, diff.ToRotation(),
                Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        //将世界坐标转换为屏幕像素坐标，考虑游戏视图缩放
        private static Vector2 WorldToScreen(Vector2 worldPos) {
            Vector2 raw = worldPos - Main.screenPosition;
            Vector2 screenCenter = new Vector2(Main.screenWidth, Main.screenHeight) / 5;
            float zoom = Main.GameViewMatrix.Zoom.X;
            return raw - screenCenter * zoom;
        }

        //在折线路径上求值
        private static Vector2 EvaluatePath(float t, Vector2 a, Vector2 b, Vector2 c, Vector2 d) {
            float dAB = Vector2.Distance(a, b);
            float dBC = Vector2.Distance(b, c);
            float dCD = Vector2.Distance(c, d);
            float total = dAB + dBC + dCD;
            if (total < 1f) return a;
            float dist = t * total;
            if (dist <= dAB) return Vector2.Lerp(a, b, dist / dAB);
            dist -= dAB;
            if (dist <= dBC) return Vector2.Lerp(b, c, dist / dBC);
            dist -= dBC;
            return Vector2.Lerp(c, d, Math.Clamp(dist / dCD, 0, 1));
        }

        private static float EaseOutCubic(float t) {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }
    }
}
