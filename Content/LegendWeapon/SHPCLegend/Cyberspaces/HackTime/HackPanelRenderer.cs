using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.HackTime
{
    /// <summary>
    /// 骇入面板渲染器(赛博朋克2077风格)
    /// <br/>选中目标后协议条目从屏幕右侧依次飞入，无边框容器
    /// <br/>故障抖动、CRT扫描线、色散文字、电路树、角标等赛博动态效果
    /// <br/>所有UI锚定到屏幕坐标
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
        //全局故障带Y坐标（周期性横扫列表区域的色偏带）
        private float glitchBandY;
        //故障带激活倒计时
        private float glitchBandCooldown;

        //===== 条目布局常量(加大) =====
        private const float ItemWidth = 380f;
        private const float ItemHeight = 62f;
        private const float ItemGap = 6f;
        private const float RightMargin = 36f;
        //左侧斜切宽度（平行四边形切角效果）
        private const float SlashWidth = 20f;
        //电路树主干距条目左边缘的偏移
        private const float TrunkOffsetX = 50f;
        //首个条目飞入前的基础延迟(秒)
        private const float BaseEntryDelay = 0.2f;
        //每个条目之间的飞入间隔(秒)
        private const float EntryStagger = 0.07f;
        //===== 字体尺寸 =====
        private const float FontName = 0.50f;
        private const float FontDesc = 0.34f;
        private const float FontIndex = 0.30f;
        private const float FontTime = 0.32f;
        private const float FontStatus = 0.30f;

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
            glitchBandY = -100f;
            glitchBandCooldown = 0.5f;
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
                for (int i = 0; i < slotFlyIn.Length; i++)
                    slotFlyIn[i] = MathHelper.Lerp(slotFlyIn[i], 0f, 0.15f);
                revealTime = Math.Max(revealTime - 0.032f, 0f);
                return;
            }

            revealTime += 0.016f;

            //各条目依次飞入
            for (int i = 0; i < slotFlyIn.Length; i++) {
                float delay = BaseEntryDelay + i * EntryStagger;
                float elapsed = revealTime - delay;
                if (elapsed <= 0f) continue;
                float speed = 0.1f + elapsed * 0.25f;
                slotFlyIn[i] = MathHelper.Lerp(slotFlyIn[i], 1f, Math.Min(speed, 0.22f));
                if (slotFlyIn[i] > 0.995f) slotFlyIn[i] = 1f;
            }

            //上传进度
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

            //故障带更新：周期性从上到下快速横扫
            glitchBandCooldown -= 0.016f;
            if (glitchBandCooldown <= 0f) {
                glitchBandY += 600f * 0.016f; //快速下移
                int count = QuickHackRegistry.All.Length;
                float totalH = count * (ItemHeight + ItemGap);
                float startY = (Main.screenHeight - totalH) * 0.5f;
                if (glitchBandY > startY + totalH + 50f) {
                    glitchBandY = startY - 50f;
                    glitchBandCooldown = 2f + Main.rand.NextFloat() * 3f; //随机间隔
                }
            }

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

            DrawAmbientNoise(sb, px, alpha);
            DrawConnectorTree(sb, px, alpha);
            DrawItems(sb, px, alpha);
            DrawGlitchBand(sb, px, alpha);
            DrawStatusBar(sb, px, alpha);
        }

        #endregion

        #region 环境噪波背景

        //在条目列表区域背后绘制微弱的水平噪波线条，增加CRT质感
        private void DrawAmbientNoise(SpriteBatch sb, Texture2D px, float alpha) {
            bool anyVisible = false;
            for (int i = 0; i < slotFlyIn.Length; i++) {
                if (slotFlyIn[i] > 0.3f) { anyVisible = true; break; }
            }
            if (!anyVisible) return;

            int count = QuickHackRegistry.All.Length;
            float totalH = count * (ItemHeight + ItemGap) - ItemGap;
            float startY = (Main.screenHeight - totalH) * 0.5f - 10f;
            float baseX = Main.screenWidth - RightMargin - ItemWidth - TrunkOffsetX - 20;
            float endX = Main.screenWidth - RightMargin + 10;
            float regionH = totalH + 20f;

            float noiseAlpha = alpha * 0.025f;
            //每隔几个像素画一条水平线，亮度随时间微变
            for (int dy = 0; dy < (int)regionH; dy += 3) {
                float seed = dy * 0.73f + timer * 8f;
                float brightness = MathF.Sin(seed) * 0.5f + 0.5f;
                if (brightness < 0.3f) continue;
                sb.Draw(px, new Rectangle((int)baseX, (int)(startY + dy), (int)(endX - baseX), 1),
                    new Rectangle(0, 0, 1, 1), HackTheme.Accent * (noiseAlpha * brightness));
            }
        }

        #endregion

        #region 故障色偏带

        //周期性从上到下横扫的故障带：水平色偏+亮度异常
        private void DrawGlitchBand(SpriteBatch sb, Texture2D px, float alpha) {
            if (glitchBandCooldown > 0f) return;

            float bandH = 4f + MathF.Sin(timer * 30f) * 2f;
            float bandAlpha = alpha * 0.15f;
            float baseX = Main.screenWidth - RightMargin - ItemWidth - TrunkOffsetX - 10;
            float endX = Main.screenWidth - RightMargin + 5;

            //主色偏带（青色）
            sb.Draw(px, new Rectangle((int)(baseX + 3), (int)glitchBandY, (int)(endX - baseX), (int)bandH),
                new Rectangle(0, 0, 1, 1), HackTheme.Accent * bandAlpha);
            //偏移红色伪影
            sb.Draw(px, new Rectangle((int)(baseX - 2), (int)(glitchBandY + 1), (int)(endX - baseX), (int)(bandH * 0.5f)),
                new Rectangle(0, 0, 1, 1), new Color(200, 30, 60) * (bandAlpha * 0.4f));
        }

        #endregion

        #region 电路树连接线

        private void DrawConnectorTree(SpriteBatch sb, Texture2D px, float alpha) {
            if (HackTime.SelectedTargetIndex < 0) return;

            int count = QuickHackRegistry.All.Length;
            float totalH = count * (ItemHeight + ItemGap) - ItemGap;
            float listStartY = (Main.screenHeight - totalH) * 0.5f;
            float baseX = Main.screenWidth - RightMargin - ItemWidth;
            float trunkX = baseX - TrunkOffsetX;
            Vector2 screenCenter = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);

            float wireProgress = Math.Clamp(revealTime * 3f, 0f, 1f);
            float wireAlpha = alpha * wireProgress * 0.45f;
            Color wireColor = HackTheme.Accent * wireAlpha;

            //主干水平线
            float hLineEnd = MathHelper.Lerp(screenCenter.X, trunkX, EaseOutCubic(wireProgress));
            DrawLine(sb, px, screenCenter, new Vector2(hLineEnd, screenCenter.Y), 1.5f, wireColor);

            //水平线节点
            float nodeSpacing = 36f;
            for (float nx = screenCenter.X + nodeSpacing; nx < hLineEnd; nx += nodeSpacing) {
                float nodePulse = MathF.Sin(timer * 4f + nx * 0.02f) * 0.3f + 0.7f;
                sb.Draw(px, new Vector2(nx - 1.5f, screenCenter.Y - 1.5f),
                    new Rectangle(0, 0, 1, 1), HackTheme.Accent * (wireAlpha * 0.5f * nodePulse),
                    0, Vector2.Zero, 3f, SpriteEffects.None, 0);
            }

            //垂直树干
            if (wireProgress > 0.3f) {
                float trunkProg = Math.Clamp((wireProgress - 0.3f) / 0.7f, 0f, 1f);
                float trunkTop = MathHelper.Lerp(screenCenter.Y, listStartY + ItemHeight * 0.5f, trunkProg);
                float trunkBot = MathHelper.Lerp(screenCenter.Y, listStartY + totalH - ItemHeight * 0.5f, trunkProg);
                DrawLine(sb, px, new Vector2(trunkX, trunkTop), new Vector2(trunkX, trunkBot), 1.5f, wireColor * 0.7f);

                //分支线 + 节点
                for (int i = 0; i < count; i++) {
                    float fly = slotFlyIn[i];
                    if (fly < 0.05f) continue;
                    float itemCY = listStartY + i * (ItemHeight + ItemGap) + ItemHeight * 0.5f;
                    float branchEnd = MathHelper.Lerp(trunkX, baseX - 4, fly);
                    Color branchColor = wireColor * 0.5f;
                    if (i == hoveredSlot) branchColor = HackTheme.Accent * (wireAlpha * 1f);
                    if (i == uploadingSlot) branchColor = HackTheme.Uploading * (wireAlpha * 0.8f);
                    DrawLine(sb, px, new Vector2(trunkX, itemCY), new Vector2(branchEnd, itemCY), 1f, branchColor);

                    //分支节点（菱形感：两层）
                    sb.Draw(px, new Vector2(trunkX - 2, itemCY - 2),
                        new Rectangle(0, 0, 1, 1), branchColor * 2f,
                        0, Vector2.Zero, 4f, SpriteEffects.None, 0);
                    sb.Draw(px, new Vector2(trunkX - 1, itemCY - 1),
                        new Rectangle(0, 0, 1, 1), HackTheme.BgDarkest,
                        0, Vector2.Zero, 2f, SpriteEffects.None, 0);

                    //分支末端小箭头
                    sb.Draw(px, new Vector2(branchEnd - 1, itemCY - 2),
                        new Rectangle(0, 0, 1, 1), branchColor * 1.2f,
                        0, Vector2.Zero, new Vector2(2, 4), SpriteEffects.None, 0);
                }
            }

            //多个数据光点沿电路流动（间隔排列）
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null) {
                for (int d = 0; d < 3; d++) {
                    float flowT = ((timer * 0.6f + d * 0.33f) % 1f);
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
                    float dotIntensity = 1f - d * 0.25f;
                    Color dotGlow = HackTheme.Accent * (alpha * 0.3f * dotIntensity);
                    dotGlow.A = 0;
                    sb.Draw(glow, flowPos, null, dotGlow, 0, glow.Size() / 2, 0.08f, SpriteEffects.None, 0);
                }
            }

            //上传时在对应分支上有额外脉冲
            if (uploadingSlot >= 0 && glow != null && wireProgress > 0.3f) {
                float itemCY = listStartY + uploadingSlot * (ItemHeight + ItemGap) + ItemHeight * 0.5f;
                float pulseT = (timer * 2f) % 1f;
                float pulseX = MathHelper.Lerp(trunkX, baseX - 4, pulseT);
                Color pulseCol = HackTheme.Uploading * (alpha * 0.4f * (1f - pulseT));
                pulseCol.A = 0;
                sb.Draw(glow, new Vector2(pulseX, itemCY), null, pulseCol, 0, glow.Size() / 2, 0.06f, SpriteEffects.None, 0);
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

                //飞入偏移（弹性过冲）
                float flyOffset = (1f - EaseOutBack(fly)) * 400f;
                //故障抖动
                float glitch = 0f;
                if (fly < 0.85f) {
                    float seed = slotGlitchSeed[i] + timer * 25f;
                    glitch = (MathF.Sin(seed) + MathF.Sin(seed * 2.7f) * 0.5f) * (1f - fly) * 16f;
                }

                float x = baseX + flyOffset + glitch;
                float hoverExpand = hover * 18f;
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
            if (isUploading) bgColor = Color.Lerp(bgColor, HackTheme.Uploading, 0.08f);
            if (isCompleted) {
                float flash = MathF.Sin(uploadFlashTimer * 10f) * 0.5f + 0.5f;
                bgColor = Color.Lerp(bgColor, HackTheme.Accent, flash * 0.15f);
            }
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), bgColor * (alpha * 0.90f));

            //斜切遮罩
            DrawSlashCut(sb, px, rect, alpha);

            //=== CRT扫描线纹理覆盖（每隔2px一条暗线） ===
            DrawCRTOverlay(sb, px, rect, alpha * 0.06f);

            //=== 类别色条（呼吸脉冲） ===
            Color catColor = GetCategoryColor(hack.Category);
            float breathe = MathF.Sin(timer * 2.5f + index * 0.8f) * 0.15f + 0.85f;
            float barGlow = isUploading ? 1f : (0.4f + hover * 0.6f);
            barGlow *= breathe;
            int barX = rect.X + (int)SlashWidth;
            sb.Draw(px, new Rectangle(barX, rect.Y + 4, 3, rect.Height - 8),
                new Rectangle(0, 0, 1, 1), catColor * (alpha * barGlow));
            //色条发光
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null && barGlow > 0.5f) {
                Color barGlowCol = catColor * (alpha * barGlow * 0.08f);
                barGlowCol.A = 0;
                sb.Draw(glow, new Vector2(barX + 1, rect.Center.Y), null, barGlowCol,
                    0, glow.Size() / 2, new Vector2(0.04f, rect.Height / 40f), SpriteEffects.None, 0);
            }

            //=== 扫描线（悬停/上传时横扫） ===
            if (hover > 0.1f || isUploading) {
                float scanSpeed = isUploading ? 2.5f : 1.8f;
                float scanPos = ((timer * scanSpeed + index * 0.4f) % 1.4f) - 0.2f;
                DrawScanLine(sb, px, rect, scanPos, alpha * (isUploading ? 0.3f : hover * 0.22f));
            }

            //=== 边框线 ===
            Color borderCol = isUploading
                ? Color.Lerp(HackTheme.Border, HackTheme.Uploading, 0.5f)
                : Color.Lerp(HackTheme.Border, HackTheme.Accent, hover * 0.5f);
            //顶边
            sb.Draw(px, new Rectangle(rect.X + (int)SlashWidth, rect.Y, rect.Width - (int)SlashWidth, 1),
                new Rectangle(0, 0, 1, 1), borderCol * (alpha * 0.55f));
            //底边
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1),
                new Rectangle(0, 0, 1, 1), borderCol * (alpha * 0.4f));
            //右边
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height),
                new Rectangle(0, 0, 1, 1), borderCol * (alpha * 0.35f));
            //斜切边线
            DrawSlashEdge(sb, px, rect, borderCol * (alpha * 0.5f));

            //=== 悬停角标（四角L形技术指示器） ===
            if (hover > 0.15f) {
                DrawCornerAccents(sb, px, rect, alpha * hover, catColor);
            }

            //=== 悬停外发光 ===
            if (hover > 0.1f && glow != null) {
                Color slotGlow = catColor * (alpha * hover * 0.06f);
                slotGlow.A = 0;
                sb.Draw(glow, rect.Center.ToVector2(), null, slotGlow, 0,
                    glow.Size() / 2, new Vector2(rect.Width / 30f, rect.Height / 30f),
                    SpriteEffects.None, 0);
            }

            //=== 序号 ===
            string idxStr = $"{index + 1:D2}";
            Color idxColor = Color.Lerp(HackTheme.TextDim, catColor, hover * 0.5f) * (alpha * 0.5f);
            Vector2 idxPos = new(rect.X + SlashWidth + 10, rect.Y + 9);
            Utils.DrawBorderString(sb, idxStr, idxPos, idxColor, FontIndex);

            //=== 协议名称（带色散效果） ===
            float nameX = rect.X + SlashWidth + 42;
            float nameY = rect.Y + 8;
            Color nameColor = Color.Lerp(HackTheme.TextNormal, HackTheme.TextBright, hover);
            if (isUploading) nameColor = Color.Lerp(nameColor, HackTheme.Uploading, 0.35f);
            if (isCompleted) nameColor = HackTheme.Accent;
            //色散：悬停时在偏移位置画红/蓝色副本
            if (hover > 0.2f) {
                float aberration = hover * 1.5f;
                Color redGhost = new Color(200, 40, 40) * (alpha * hover * 0.18f);
                Color blueGhost = new Color(40, 80, 200) * (alpha * hover * 0.18f);
                Utils.DrawBorderString(sb, hack.Name, new Vector2(nameX - aberration, nameY), redGhost, FontName);
                Utils.DrawBorderString(sb, hack.Name, new Vector2(nameX + aberration, nameY + 0.5f), blueGhost, FontName);
            }
            Utils.DrawBorderString(sb, hack.Name, new Vector2(nameX, nameY), nameColor * alpha, FontName);

            //=== 效果描述 ===
            Vector2 descPos = new(rect.X + SlashWidth + 42, rect.Y + 36);
            Color descColor = Color.Lerp(HackTheme.TextDim, HackTheme.TextNormal, hover * 0.3f);
            Utils.DrawBorderString(sb, hack.Desc, descPos, descColor * (alpha * 0.65f), FontDesc);

            //=== 右侧状态区 ===
            if (isUploading) {
                DrawUploadIndicator(sb, px, alpha, rect);
            }
            else if (isCompleted) {
                float flash = MathF.Sin(uploadFlashTimer * 8f) * 0.3f + 0.7f;
                Vector2 okSize = FontAssets.MouseText.Value.MeasureString("DONE") * 0.36f;
                Vector2 okPos = new(rect.Right - okSize.X - 12, rect.Y + (rect.Height - okSize.Y) * 0.5f);
                Utils.DrawBorderString(sb, "DONE", okPos, HackTheme.Accent * (alpha * flash), 0.36f);
            }
            else {
                //上传耗时
                float sec = hack.UploadTime / 60f;
                string timeStr = $"{sec:F1}s";
                Vector2 ts = FontAssets.MouseText.Value.MeasureString(timeStr) * FontTime;
                Vector2 tp = new(rect.Right - ts.X - 12, rect.Y + (rect.Height - ts.Y) * 0.5f);
                Utils.DrawBorderString(sb, timeStr, tp, HackTheme.TextDim * (alpha * 0.45f), FontTime);
            }

            //=== 上传时底部进度光带（贯穿条目底边） ===
            if (isUploading) {
                int fillW = (int)(rect.Width * uploadProgress);
                sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 2, fillW, 2),
                    new Rectangle(0, 0, 1, 1), HackTheme.Uploading * (alpha * 0.5f));
                if (glow != null && fillW > 4) {
                    Color tipGlow = HackTheme.Uploading * (alpha * 0.25f);
                    tipGlow.A = 0;
                    sb.Draw(glow, new Vector2(rect.X + fillW, rect.Bottom - 1), null,
                        tipGlow, 0, glow.Size() / 2, new Vector2(0.12f, 0.02f), SpriteEffects.None, 0);
                }
            }
        }

        #endregion

        #region 视觉效果

        //左侧斜切遮罩
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

        //斜切边线
        private static void DrawSlashEdge(SpriteBatch sb, Texture2D px, Rectangle rect, Color color) {
            Vector2 top = new(rect.X + SlashWidth, rect.Y);
            Vector2 bottom = new(rect.X, rect.Bottom);
            DrawLine(sb, px, top, bottom, 1f, color);
        }

        //CRT水平暗线叠加（每隔几像素的暗纹）
        private static void DrawCRTOverlay(SpriteBatch sb, Texture2D px, Rectangle rect, float alpha) {
            Color line = HackTheme.BgDarkest * alpha;
            for (int dy = 0; dy < rect.Height; dy += 3) {
                sb.Draw(px, new Rectangle(rect.X, rect.Y + dy, rect.Width, 1),
                    new Rectangle(0, 0, 1, 1), line);
            }
        }

        //悬停角标：四角的小L形括号
        private static void DrawCornerAccents(SpriteBatch sb, Texture2D px, Rectangle rect, float alpha, Color color) {
            int arm = 8;
            Color c = color * (alpha * 0.6f);
            //左上
            sb.Draw(px, new Rectangle(rect.X + (int)SlashWidth, rect.Y, arm, 1), new Rectangle(0, 0, 1, 1), c);
            sb.Draw(px, new Rectangle(rect.X + (int)SlashWidth, rect.Y, 1, arm), new Rectangle(0, 0, 1, 1), c);
            //右上
            sb.Draw(px, new Rectangle(rect.Right - arm, rect.Y, arm, 1), new Rectangle(0, 0, 1, 1), c);
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, arm), new Rectangle(0, 0, 1, 1), c);
            //左下
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, arm, 1), new Rectangle(0, 0, 1, 1), c);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - arm, 1, arm), new Rectangle(0, 0, 1, 1), c);
            //右下
            sb.Draw(px, new Rectangle(rect.Right - arm, rect.Bottom - 1, arm, 1), new Rectangle(0, 0, 1, 1), c);
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Bottom - arm, 1, arm), new Rectangle(0, 0, 1, 1), c);
        }

        //扫描线：一道竖向亮线从左到右横穿条目
        private static void DrawScanLine(SpriteBatch sb, Texture2D px, Rectangle rect, float pos, float alpha) {
            int lineX = rect.X + (int)(rect.Width * pos);
            if (lineX < rect.X || lineX > rect.Right - 2) return;
            sb.Draw(px, new Rectangle(lineX, rect.Y + 1, 2, rect.Height - 2),
                new Rectangle(0, 0, 1, 1), HackTheme.Accent * alpha);
            //宽域辉光
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null) {
                Color gc = HackTheme.Accent * (alpha * 0.6f);
                gc.A = 0;
                sb.Draw(glow, new Vector2(lineX, rect.Center.Y), null, gc, 0,
                    glow.Size() / 2, new Vector2(0.1f, rect.Height / 40f), SpriteEffects.None, 0);
            }
        }

        //上传进度指示器（右上角百分比+底部进度条+粒子拖尾）
        private void DrawUploadIndicator(SpriteBatch sb, Texture2D px, float alpha, Rectangle rect) {
            int barW = 80;
            int barH = 5;
            int barX = rect.Right - barW - 14;
            int barY = rect.Bottom - barH - 8;

            //背景槽
            sb.Draw(px, new Rectangle(barX, barY, barW, barH),
                new Rectangle(0, 0, 1, 1), HackTheme.ProgressBg * alpha);
            int fillW = (int)(barW * uploadProgress);
            if (fillW > 0) {
                //填充
                sb.Draw(px, new Rectangle(barX, barY, fillW, barH),
                    new Rectangle(0, 0, 1, 1), HackTheme.ProgressFill * (alpha * 0.9f));
                //进度条前端辉光
                Texture2D glow = CWRAsset.SoftGlow?.Value;
                if (glow != null && fillW > 2) {
                    Color tipGlow = HackTheme.ProgressGlow * (alpha * 0.4f);
                    tipGlow.A = 0;
                    sb.Draw(glow, new Vector2(barX + fillW, barY + barH * 0.5f), null,
                        tipGlow, 0, glow.Size() / 2, new Vector2(0.12f, 0.04f), SpriteEffects.None, 0);
                }
                //进度条内高光线
                sb.Draw(px, new Rectangle(barX, barY, fillW, 1),
                    new Rectangle(0, 0, 1, 1), HackTheme.TextBright * (alpha * 0.15f));
            }

            //百分比文字
            string pct = $"{(int)(uploadProgress * 100)}%";
            Vector2 ps = FontAssets.MouseText.Value.MeasureString(pct) * FontTime;
            Vector2 pp = new(rect.Right - ps.X - 10, rect.Y + 8);
            Utils.DrawBorderString(sb, pct, pp, HackTheme.Uploading * alpha, FontTime);
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
            float bottomY = startY + totalH + 14f;
            float baseX = Main.screenWidth - RightMargin - ItemWidth;

            //分隔线
            sb.Draw(px, new Rectangle((int)baseX, (int)(bottomY - 4), (int)ItemWidth, 1),
                new Rectangle(0, 0, 1, 1), HackTheme.Border * (alpha * 0.3f));

            //状态脉冲灯
            float pulse = (MathF.Sin(timer * 3.5f) + 1f) * 0.5f;
            Color dotColor = uploadingSlot >= 0
                ? Color.Lerp(HackTheme.Uploading * 0.4f, HackTheme.Uploading, pulse) * alpha
                : Color.Lerp(new Color(20, 100, 50), new Color(40, 200, 100), pulse) * alpha;
            sb.Draw(px, new Vector2(baseX, bottomY + 3),
                new Rectangle(0, 0, 1, 1), dotColor, 0, Vector2.Zero, 4f, SpriteEffects.None, 0);

            //脉冲灯辉光
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null) {
                Color dglow = dotColor * 0.15f;
                dglow.A = 0;
                sb.Draw(glow, new Vector2(baseX + 2, bottomY + 5), null, dglow,
                    0, glow.Size() / 2, 0.04f, SpriteEffects.None, 0);
            }

            //状态文字
            string status = uploadingSlot >= 0 ? "UPLOADING..." : "BREACH PROTOCOL // READY";
            if (uploadComplete) status = ">> UPLOAD COMPLETE <<";
            Utils.DrawBorderString(sb, status, new Vector2(baseX + 14, bottomY),
                HackTheme.TextDim * alpha, FontStatus);

            //滚动十六进制标签
            string tag = $"NET::0x{((int)(timer * 100) % 0xFFFF):X4}";
            Utils.DrawBorderString(sb, tag, new Vector2(baseX + ItemWidth - 110, bottomY),
                HackTheme.Accent * (alpha * 0.22f), 0.24f);

            //协议计数
            string countStr = $"[{count} PROTOCOLS]";
            Utils.DrawBorderString(sb, countStr, new Vector2(baseX + ItemWidth - 110, bottomY + 20),
                HackTheme.TextDim * (alpha * 0.25f), 0.22f);
        }

        #endregion

        #region 屏幕中央目标锁定框

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

            float baseHalfW = Math.Max(npc.width, 32) * 0.6f + 28f;
            float baseHalfH = Math.Max(npc.height, 32) * 0.6f + 28f;

            float ease = EaseOutCubic(camProg);
            float expand = 1f + (1f - ease) * 0.8f;
            float halfW = baseHalfW * expand;
            float halfH = baseHalfH * expand;

            int armLen = (int)(24f * ease);
            if (armLen < 2) return;
            Color frameColor = HackTheme.Accent * (alpha * 0.75f);
            Color dimColor = HackTheme.Accent * (alpha * 0.25f);

            //四角锁定括号
            DrawFrameBracket(sb, px, center, -halfW, -halfH, armLen, frameColor);
            DrawFrameBracket(sb, px, center, halfW, -halfH, armLen, frameColor);
            DrawFrameBracket(sb, px, center, -halfW, halfH, armLen, frameColor);
            DrawFrameBracket(sb, px, center, halfW, halfH, armLen, frameColor);

            //四角辉光
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null && ease > 0.3f) {
                Color cg = HackTheme.Accent * (alpha * 0.08f * ease);
                cg.A = 0;
                Vector2[] corners = {
                    center + new Vector2(-halfW, -halfH),
                    center + new Vector2(halfW, -halfH),
                    center + new Vector2(-halfW, halfH),
                    center + new Vector2(halfW, halfH)
                };
                foreach (var c in corners)
                    sb.Draw(glow, c, null, cg, 0, glow.Size() / 2, 0.1f, SpriteEffects.None, 0);
            }

            //边缘刻度标记
            if (ease > 0.5f) {
                float tickAlpha = (ease - 0.5f) * 2f * alpha * 0.35f;
                Color tickColor = HackTheme.Border * tickAlpha;
                int tickCount = 6;
                for (int i = 1; i < tickCount; i++) {
                    float t = (float)i / tickCount;
                    float tx = MathHelper.Lerp(center.X - halfW, center.X + halfW, t);
                    float ty = MathHelper.Lerp(center.Y - halfH, center.Y + halfH, t);
                    sb.Draw(px, new Rectangle((int)tx, (int)(center.Y - halfH), 1, 5),
                        new Rectangle(0, 0, 1, 1), tickColor);
                    sb.Draw(px, new Rectangle((int)tx, (int)(center.Y + halfH - 5), 1, 5),
                        new Rectangle(0, 0, 1, 1), tickColor);
                    sb.Draw(px, new Rectangle((int)(center.X - halfW), (int)ty, 5, 1),
                        new Rectangle(0, 0, 1, 1), tickColor);
                    sb.Draw(px, new Rectangle((int)(center.X + halfW - 5), (int)ty, 5, 1),
                        new Rectangle(0, 0, 1, 1), tickColor);
                }
            }

            //中心十字
            float crossLen = 8f * ease;
            sb.Draw(px, new Rectangle((int)(center.X - crossLen), (int)center.Y, (int)(crossLen * 2), 1),
                new Rectangle(0, 0, 1, 1), dimColor);
            sb.Draw(px, new Rectangle((int)center.X, (int)(center.Y - crossLen), 1, (int)(crossLen * 2)),
                new Rectangle(0, 0, 1, 1), dimColor);

            //旋转外框指示环（用4条短斜线模拟）
            if (ease > 0.6f) {
                float ringAlpha = (ease - 0.6f) / 0.4f * alpha * 0.2f;
                float rot = timer * 0.5f;
                float ringR = Math.Max(halfW, halfH) + 12f;
                Color ringCol = HackTheme.Accent * ringAlpha;
                for (int a = 0; a < 4; a++) {
                    float angle = rot + a * MathHelper.PiOver2;
                    Vector2 dir = new(MathF.Cos(angle), MathF.Sin(angle));
                    Vector2 p1 = center + dir * (ringR - 6);
                    Vector2 p2 = center + dir * (ringR + 6);
                    DrawLine(sb, px, p1, p2, 1.5f, ringCol);
                }
            }

            //文字标签
            if (ease > 0.4f) {
                float labelAlpha = (ease - 0.4f) / 0.6f * alpha;

                string label = "// TARGET LOCKED";
                Vector2 labelSize = FontAssets.MouseText.Value.MeasureString(label) * 0.34f;
                Vector2 labelPos = new(center.X - labelSize.X * 0.5f, center.Y - halfH - 22f);
                Utils.DrawBorderString(sb, label, labelPos, HackTheme.Accent * (labelAlpha * 0.7f), 0.34f);

                string npcName = npc.FullName;
                Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(npcName) * 0.36f;
                Vector2 namePos = new(center.X - nameSize.X * 0.5f, center.Y + halfH + 8f);
                Utils.DrawBorderString(sb, npcName, namePos, HackTheme.TextBright * (labelAlpha * 0.6f), 0.36f);

                //生命值百分比
                if (npc.lifeMax > 0) {
                    float hpPct = (float)npc.life / npc.lifeMax;
                    string hpStr = $"HP {(int)(hpPct * 100)}%";
                    Vector2 hpSize = FontAssets.MouseText.Value.MeasureString(hpStr) * 0.26f;
                    Vector2 hpPos = new(center.X - hpSize.X * 0.5f, center.Y + halfH + 30f);
                    Color hpColor = hpPct > 0.5f ? HackTheme.AccentAlt : (hpPct > 0.25f ? HackTheme.Uploading : HackTheme.Danger);
                    Utils.DrawBorderString(sb, hpStr, hpPos, hpColor * (labelAlpha * 0.45f), 0.26f);
                }
            }

            //水平扫描线
            float scanT = (timer * 0.6f) % 1f;
            float scanY = center.Y - halfH + scanT * halfH * 2;
            float scanFade = 1f - Math.Abs(scanT - 0.5f) * 2f;
            sb.Draw(px, new Rectangle((int)(center.X - halfW), (int)scanY, (int)(halfW * 2), 1),
                new Rectangle(0, 0, 1, 1), HackTheme.Accent * (alpha * 0.14f * scanFade));
            //扫描线辉光
            if (glow != null) {
                Color sg = HackTheme.Accent * (alpha * 0.04f * scanFade);
                sg.A = 0;
                sb.Draw(glow, new Vector2(center.X, scanY), null, sg, 0,
                    glow.Size() / 2, new Vector2(halfW / 20f, 0.02f), SpriteEffects.None, 0);
            }
        }

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

        private static float EaseOutBack(float t) {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float inv = t - 1f;
            return 1f + c3 * inv * inv * inv + c1 * inv * inv;
        }

        #endregion
    }
}
