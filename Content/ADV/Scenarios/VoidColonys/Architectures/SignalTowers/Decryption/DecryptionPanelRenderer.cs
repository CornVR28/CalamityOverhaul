using CalamityOverhaul.Common;
using CalamityOverhaul.Content.HackTimes;
using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.SignalTowers.Decryption
{
    /// <summary>
    /// 信号塔解密面板的本地化文本集合，统一在UI分类下注册
    /// </summary>
    internal class DecryptionStrings : ModSystem, ILocalizedModType
    {
        public string LocalizationCategory => "UI";

        public static LocalizedText Title { get; private set; }
        public static LocalizedText SubtitleLocked { get; private set; }
        public static LocalizedText SubtitleBreaching { get; private set; }
        public static LocalizedText SubtitleDataFlood { get; private set; }
        public static LocalizedText SubtitleMessage { get; private set; }
        public static LocalizedText TerminalIdFormat { get; private set; }
        public static LocalizedText CoordinateFormat { get; private set; }
        public static LocalizedText SignalLockLabel { get; private set; }
        public static LocalizedText SignalLockValue { get; private set; }
        public static LocalizedText FooterLegend { get; private set; }
        public static LocalizedText HintLocked { get; private set; }
        public static LocalizedText HintBreaching { get; private set; }
        public static LocalizedText HintDataFlood { get; private set; }
        public static LocalizedText HintMessage { get; private set; }
        public static LocalizedText BeginBreachAction { get; private set; }
        public static LocalizedText CloseAction { get; private set; }
        public static LocalizedText StatusLocked { get; private set; }
        public static LocalizedText StatusBreaching { get; private set; }
        public static LocalizedText StatusDataFlood { get; private set; }
        public static LocalizedText StatusMessage { get; private set; }

        public override void SetStaticDefaults() {
            Title = this.GetLocalization("DecryptionPanelTitle", () => "虚空·信号终端接入");
            SubtitleLocked = this.GetLocalization("DecryptionPanelSubtitleLocked", () => "协议封锁·需要人工破译");
            SubtitleBreaching = this.GetLocalization("DecryptionPanelSubtitleBreaching", () => "破译进行中");
            SubtitleDataFlood = this.GetLocalization("DecryptionPanelSubtitleDataFlood", () => "数据洪流注入");
            SubtitleMessage = this.GetLocalization("DecryptionPanelSubtitleMessage", () => "解码信号回放");
            TerminalIdFormat = this.GetLocalization("DecryptionPanelTerminalId", () => "TERM-ID ▸ {0}");
            CoordinateFormat = this.GetLocalization("DecryptionPanelCoordinate", () => "ΔPOS ▸ {0}, {1}");
            SignalLockLabel = this.GetLocalization("DecryptionPanelSignalLock", () => "信号锁定强度");
            SignalLockValue = this.GetLocalization("DecryptionPanelSignalLockValue", () => "{0}%");
            FooterLegend = this.GetLocalization("DecryptionPanelFooter", () => "[ESC] 断开连接    [左键] 交互    [?] 帮助");
            HintLocked = this.GetLocalization("DecryptionPanelHintLocked", () => "信号层被加密栅格封闭。按 [空格] 或点击 [启动破译] 进入输入序列。");
            HintBreaching = this.GetLocalization("DecryptionPanelHintBreaching", () => "按序点击矩阵单元，填入目标序列。");
            HintDataFlood = this.GetLocalization("DecryptionPanelHintDataFlood", () => "协议通道打开，正在注入数据洪流...");
            HintMessage = this.GetLocalization("DecryptionPanelHintMessage", () => "解码完成，以下为截获片段。");
            BeginBreachAction = this.GetLocalization("DecryptionPanelBeginBreach", () => "▷ 启动破译");
            CloseAction = this.GetLocalization("DecryptionPanelClose", () => "✕ 断开");
            StatusLocked = this.GetLocalization("DecryptionPanelStatusLocked", () => "LOCKED");
            StatusBreaching = this.GetLocalization("DecryptionPanelStatusBreaching", () => "BREACHING");
            StatusDataFlood = this.GetLocalization("DecryptionPanelStatusDataFlood", () => "INJECTING");
            StatusMessage = this.GetLocalization("DecryptionPanelStatusMessage", () => "DECODED");
            BreachMinigame.RegisterLocalization(this);
        }
    }

    /// <summary>
    /// 信号塔解密面板UI
    /// Step2基架：背景shader + 头部信息带 + 主体占位容器 + 底部legend
    /// 后续Step3-5将向Body注入小游戏、数据洪流、消息展示
    /// 由InnoVault的UIHandle驱动：Active表示是否渲染，Draw由框架在正确的UI层自动调用
    /// </summary>
    internal class DecryptionPanelUI : UIHandle
    {
        public static DecryptionPanelUI Instance => UIHandleLoader.GetUIHandleOfType<DecryptionPanelUI>();

        //== 基础布局 ==
        private const int EdgePad = 14;
        private const int HeaderHeight = 78;
        private const int FooterHeight = 42;
        private const int BodyPadding = 18;

        //== 面板尺寸（基于UI逻辑像素，配合UIScaleMatrix） ==
        private const int PanelWidth = 920;
        private const int PanelHeight = 560;

        /// <summary>供外部查询鼠标是否在面板矩形内（用于阻止世界交互穿透）</summary>
        public static bool MouseInside { get; private set; }

        /// <summary>面板的世界UI矩形（仅展开时有效）</summary>
        public static Rectangle PanelRect { get; private set; }

        /// <summary>主体可用区域（供小游戏/数据流/消息Step使用）</summary>
        public static Rectangle BodyRect { get; private set; }

        public override bool Active => !Main.gameMenu && (DecryptionSession.IsOpen || DecryptionSession.OpenProgress > 0.005f);

        public override void Update() {
            //动画与阶段切换由DecryptionSession.Update()驱动，这里只同步HitBox
            if (DecryptionSession.OpenProgress > 0.05f) {
                UIHitBox = PanelRect;
            }
            else {
                UIHitBox = Rectangle.Empty;
            }
        }

        public override void Draw(SpriteBatch sb) {
            if (!DecryptionSession.IsOpen && DecryptionSession.OpenProgress <= 0.005f) {
                MouseInside = false;
                return;
            }

            float open = DecryptionSession.OpenProgress;
            //缓动：更自然的展开曲线
            float eased = 1f - (float)Math.Pow(1f - MathHelper.Clamp(open, 0f, 1f), 3);

            //计算面板矩形（屏幕居中，带展开缩放）
            int screenW = Main.screenWidth;
            int screenH = Main.screenHeight;
            int w = PanelWidth;
            int h = PanelHeight;
            int cx = screenW / 2;
            int cy = screenH / 2;

            float scale = 0.85f + 0.15f * eased;
            int dw = (int)(w * scale);
            int dh = (int)(h * scale);
            Rectangle rect = new Rectangle(cx - dw / 2, cy - dh / 2, dw, dh);
            PanelRect = rect;

            //屏幕全局变暗幕
            Texture2D px = TextureAssets.MagicPixel.Value;
            sb.Draw(px, new Rectangle(0, 0, screenW, screenH), Color.Black * (0.55f * eased));

            //1) 背景shader
            DrawShaderBackground(sb, rect, eased);

            //2) Header / Body / Footer 分层
            Rectangle header = new Rectangle(rect.X + BodyPadding, rect.Y + BodyPadding,
                rect.Width - BodyPadding * 2, HeaderHeight);
            Rectangle footer = new Rectangle(rect.X + BodyPadding,
                rect.Bottom - BodyPadding - FooterHeight,
                rect.Width - BodyPadding * 2, FooterHeight);
            Rectangle body = new Rectangle(header.X, header.Bottom + 8,
                header.Width, footer.Y - header.Bottom - 16);
            BodyRect = body;

            DrawHeader(sb, header, eased);
            DrawBodyFrame(sb, body, eased);
            DrawFooter(sb, footer, eased);

            //3) 鼠标占用
            MouseInside = rect.Contains(Main.mouseX, Main.mouseY) && eased > 0.2f;
            if (MouseInside) {
                Main.LocalPlayer.mouseInterface = true;
                Main.LocalPlayer.cursorItemIconEnabled = false;
            }
        }

        //═══════════════════════════════════════════════════════════
        // 背景shader绘制
        //═══════════════════════════════════════════════════════════
        private void DrawShaderBackground(SpriteBatch sb, Rectangle rect, float eased) {
            Rectangle ext = rect;
            ext.Inflate(EdgePad, EdgePad);

            float phase = DecryptionSession.Phase switch {
                DecryptionPhase.Locked => 0f,
                DecryptionPhase.Breaching => 1f,
                DecryptionPhase.DataFlood => 1.5f,
                DecryptionPhase.MessageReveal => 2f,
                DecryptionPhase.Closing => 1f,
                _ => 0f,
            };

            Effect shader = EffectLoader.DecryptionPanelBackground?.Value;
            if (shader == null) {
                //降级：纯色
                Texture2D px = TextureAssets.MagicPixel.Value;
                sb.Draw(px, rect, Color.Black * (0.92f * eased));
                return;
            }

            shader.Parameters["uTime"]?.SetValue(DecryptionSession.SessionTime);
            shader.Parameters["uAlpha"]?.SetValue(eased * 0.98f);
            shader.Parameters["uResolution"]?.SetValue(new Vector2(ext.Width, ext.Height));
            shader.Parameters["uEdgePad"]?.SetValue((float)EdgePad);
            shader.Parameters["uOpenProgress"]?.SetValue(eased);
            shader.Parameters["uPhase"]?.SetValue(phase);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.None,
                RasterizerState.CullNone, shader, Main.UIScaleMatrix);
            sb.Draw(TextureAssets.MagicPixel.Value, ext, Color.White);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }

        //═══════════════════════════════════════════════════════════
        // 头部：Title + Subtitle + Terminal ID + 坐标 + 状态角标
        //═══════════════════════════════════════════════════════════
        private void DrawHeader(SpriteBatch sb, Rectangle rect, float eased) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;

            //分区亚底：略暗矩形铺垫
            sb.Draw(px, rect, Color.Black * (0.28f * eased));

            //上下两道青色装饰线
            Color lineCol = GetAccentColor() * eased;
            sb.Draw(px, new Rectangle(rect.X, rect.Y - 2, rect.Width, 1), lineCol * 0.7f);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom + 1, rect.Width, 1), lineCol * 0.55f);

            //左侧电子角标：三道竖向短线（之前用旋转DrawLine画45度，
            //在某些SamplerState/缩放组合下会被拉伸成贯穿屏幕的巨型色块，改用轴向矩形稳妥）
            for (int k = 0; k < 3; k++) {
                int sx = rect.X + 6 + k * 5;
                Color cc = lineCol * (0.6f + k * 0.15f);
                sb.Draw(px, new Rectangle(sx, rect.Y + 10, 2, rect.Height - 20), cc);
            }

            //标题 + 副标题
            string title = DecryptionStrings.Title.Value;
            string subtitle = DecryptionSession.Phase switch {
                DecryptionPhase.Breaching => DecryptionStrings.SubtitleBreaching.Value,
                DecryptionPhase.DataFlood => DecryptionStrings.SubtitleDataFlood.Value,
                DecryptionPhase.MessageReveal => DecryptionStrings.SubtitleMessage.Value,
                _ => DecryptionStrings.SubtitleLocked.Value,
            };
            //追加阶段实时进度
            string subtitleSuffix = DecryptionSession.Phase switch {
                DecryptionPhase.Breaching =>
                    $"  ·  BUF {DecryptionSession.Minigame.Buffer.Count:00}/{BreachMinigame.BufferCapacity:00}  ·  T-{DecryptionSession.Minigame.TimeLeft:00.0}s",
                DecryptionPhase.DataFlood =>
                    $"  ·  {(int)(DecryptionSession.DataFlood.Progress * 100):000}%",
                DecryptionPhase.MessageReveal =>
                    $"  ·  {(int)DecryptionSession.Message.RevealedChars:0000}/{DecryptionSession.Message.TotalChars:0000}",
                _ => string.Empty,
            };
            if (!string.IsNullOrEmpty(subtitleSuffix)) subtitle += subtitleSuffix;

            Vector2 titlePos = new Vector2(rect.X + 48, rect.Y + 8);
            //轻微色散
            float aber = 0.8f + MathF.Sin(DecryptionSession.SessionTime * 5f) * 0.3f;
            Utils.DrawBorderStringFourWay(sb, font, title, titlePos.X - aber, titlePos.Y,
                new Color(255, 70, 90) * (eased * 0.35f), Color.Black * (eased * 0.1f), Vector2.Zero, 0.8f);
            Utils.DrawBorderStringFourWay(sb, font, title, titlePos.X + aber, titlePos.Y + 0.4f,
                new Color(70, 170, 255) * (eased * 0.35f), Color.Black * (eased * 0.1f), Vector2.Zero, 0.8f);
            Utils.DrawBorderStringFourWay(sb, font, title, titlePos.X, titlePos.Y,
                GetTitleColor() * eased, Color.Black * eased, Vector2.Zero, 0.8f);

            Vector2 subPos = new Vector2(rect.X + 48, rect.Y + 42);
            Utils.DrawBorderString(sb, subtitle, subPos, GetAccentColor() * (eased * 0.9f), 0.68f);

            //右上：Terminal ID + ΔPos
            if (DecryptionSession.CurrentTower != null) {
                var tower = DecryptionSession.CurrentTower;
                int termId = tower.WhoAmI & 0xFFFF;
                int tx = (int)(tower.Position.X / 16f);
                int ty = (int)(tower.Position.Y / 16f);
                string idStr = DecryptionStrings.TerminalIdFormat.Format($"0x{termId:X4}");
                string coordStr = DecryptionStrings.CoordinateFormat.Format(tx, ty);
                Vector2 idSize = font.MeasureString(idStr) * 0.6f;
                Vector2 coSize = font.MeasureString(coordStr) * 0.6f;
                Utils.DrawBorderString(sb, idStr,
                    new Vector2(rect.Right - idSize.X - 12, rect.Y + 10),
                    GetAccentColor() * eased, 0.6f);
                Utils.DrawBorderString(sb, coordStr,
                    new Vector2(rect.Right - coSize.X - 12, rect.Y + 34),
                    HackTheme.TextNormal * (eased * 0.9f), 0.6f);
            }

            //右下：状态角标（LOCKED/BREACHING/INJECTING/DECODED），带呼吸
            string status = DecryptionSession.Phase switch {
                DecryptionPhase.Breaching => DecryptionStrings.StatusBreaching.Value,
                DecryptionPhase.DataFlood => DecryptionStrings.StatusDataFlood.Value,
                DecryptionPhase.MessageReveal => DecryptionStrings.StatusMessage.Value,
                _ => DecryptionStrings.StatusLocked.Value,
            };
            Vector2 statusSize = font.MeasureString(status) * 0.55f;
            float pulse = 0.72f + MathF.Sin(DecryptionSession.SessionTime * 4.2f) * 0.28f;
            Vector2 stPos = new Vector2(rect.Right - statusSize.X - 14, rect.Bottom - statusSize.Y - 6);
            Rectangle stBg = new Rectangle((int)stPos.X - 8, (int)stPos.Y - 4,
                (int)statusSize.X + 16, (int)statusSize.Y + 8);
            sb.Draw(px, stBg, Color.Black * (0.55f * eased));
            sb.Draw(px, new Rectangle(stBg.X, stBg.Y, stBg.Width, 1), GetStatusColor() * (eased * pulse));
            sb.Draw(px, new Rectangle(stBg.X, stBg.Bottom - 1, stBg.Width, 1), GetStatusColor() * (eased * pulse));
            Utils.DrawBorderString(sb, status, stPos, GetStatusColor() * (eased * pulse), 0.55f);
        }

        //═══════════════════════════════════════════════════════════
        // 主体占位框架：Step3+会在此Hook小游戏渲染
        //═══════════════════════════════════════════════════════════
        private void DrawBodyFrame(SpriteBatch sb, Rectangle rect, float eased) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Color accent = GetAccentColor();

            //四个角切角：用短线模拟（左上/右上/左下/右下）
            int corner = 18;
            Color cornerCol = accent * (eased * 0.9f);
            //左上
            sb.Draw(px, new Rectangle(rect.X, rect.Y, corner, 1), cornerCol);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 1, corner), cornerCol);
            //右上
            sb.Draw(px, new Rectangle(rect.Right - corner, rect.Y, corner, 1), cornerCol);
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, corner), cornerCol);
            //左下
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, corner, 1), cornerCol);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - corner, 1, corner), cornerCol);
            //右下
            sb.Draw(px, new Rectangle(rect.Right - corner, rect.Bottom - 1, corner, 1), cornerCol);
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Bottom - corner, 1, corner), cornerCol);

            //中间的内容区稍暗底
            Rectangle inner = rect;
            inner.Inflate(-6, -6);
            sb.Draw(px, inner, Color.Black * (0.22f * eased));

            //中心提示文本（Locked阶段显示，Breaching/DataFlood/Message由各自的模块接管主体）
            if (DecryptionSession.Phase == DecryptionPhase.Breaching) {
                BreachMinigameRenderer.Draw(sb, DecryptionSession.Minigame, inner, eased, GetAccentColor());
                return;
            }
            if (DecryptionSession.Phase == DecryptionPhase.DataFlood) {
                DataFloodRenderer.Draw(sb, DecryptionSession.DataFlood, inner, eased, GetAccentColor());
                return;
            }
            if (DecryptionSession.Phase == DecryptionPhase.MessageReveal) {
                MessageRevealRenderer.Draw(sb, DecryptionSession.Message, inner, eased, GetAccentColor());
                HandleMessageInput();
                return;
            }

            string hint = DecryptionSession.Phase switch {
                DecryptionPhase.Breaching => DecryptionStrings.HintBreaching.Value,
                DecryptionPhase.DataFlood => DecryptionStrings.HintDataFlood.Value,
                DecryptionPhase.MessageReveal => DecryptionStrings.HintMessage.Value,
                _ => DecryptionStrings.HintLocked.Value,
            };

            //按照宽度自动换行
            string[] rawLines = Utils.WordwrapString(hint, font,
                (int)(inner.Width * 0.9f / 0.7f), 6, out _);
            string wrapped = hint;
            if (rawLines != null) {
                var filtered = new System.Collections.Generic.List<string>();
                foreach (var ln in rawLines) if (!string.IsNullOrEmpty(ln)) filtered.Add(ln.TrimEnd('-', ' '));
                if (filtered.Count > 0) wrapped = string.Join("\n", filtered);
            }
            Vector2 size = font.MeasureString(wrapped) * 0.7f;
            Vector2 pos = new Vector2(inner.X + (inner.Width - size.X) / 2,
                inner.Y + (inner.Height - size.Y) / 2);
            Utils.DrawBorderString(sb, wrapped, pos, HackTheme.TextBright * (eased * 0.92f), 0.7f);

            //仅Locked阶段显示"启动破译"按钮占位（Step3会取而代之）
            if (DecryptionSession.Phase == DecryptionPhase.Locked) {
                DrawBeginBreachHint(sb, inner, eased);
            }
        }

        //Locked阶段的按钮占位：画一个动感的"启动破译"按钮，Step3替换为真实游戏入口
        private void DrawBeginBreachHint(SpriteBatch sb, Rectangle inner, float eased) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string txt = DecryptionStrings.BeginBreachAction.Value;
            Vector2 size = font.MeasureString(txt) * 0.75f;
            int pad = 18;
            int bw = (int)size.X + pad * 2;
            int bh = (int)size.Y + pad;
            Rectangle btn = new Rectangle(
                inner.X + (inner.Width - bw) / 2,
                inner.Bottom - bh - 22,
                bw, bh);
            bool over = btn.Contains(Main.mouseX, Main.mouseY);
            float pulse = 0.65f + MathF.Sin(DecryptionSession.SessionTime * 3f) * 0.35f;
            Color bgC = over ? Color.Black * (0.75f * eased) : Color.Black * (0.55f * eased);
            Color frame = GetAccentColor() * (eased * (over ? 1f : 0.7f) * pulse);
            sb.Draw(px, btn, bgC);
            //描边
            sb.Draw(px, new Rectangle(btn.X, btn.Y, btn.Width, 1), frame);
            sb.Draw(px, new Rectangle(btn.X, btn.Bottom - 1, btn.Width, 1), frame);
            sb.Draw(px, new Rectangle(btn.X, btn.Y, 1, btn.Height), frame);
            sb.Draw(px, new Rectangle(btn.Right - 1, btn.Y, 1, btn.Height), frame);
            Vector2 tp = new Vector2(btn.X + pad, btn.Y + pad / 2);
            Utils.DrawBorderString(sb, txt, tp, GetAccentColor() * (eased * (over ? 1f : 0.85f)), 0.75f);

            //交互：点击切换到Breaching阶段
            if (over && Main.mouseLeft && Main.mouseLeftRelease && eased > 0.85f) {
                DecryptionSession.TransitionTo(DecryptionPhase.Breaching);
                Main.mouseLeftRelease = false;
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
            }
        }

        //═══════════════════════════════════════════════════════════
        // 底部：提示图例 + 关闭按钮
        //═══════════════════════════════════════════════════════════
        private void DrawFooter(SpriteBatch sb, Rectangle rect, float eased) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;

            sb.Draw(px, rect, Color.Black * (0.28f * eased));
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1), GetAccentColor() * (eased * 0.55f));

            //左侧legend
            string legend = DecryptionStrings.FooterLegend.Value;
            Utils.DrawBorderString(sb, legend,
                new Vector2(rect.X + 12, rect.Y + (rect.Height - 14) / 2),
                HackTheme.TextNormal * (eased * 0.9f), 0.62f);

            //右侧关闭按钮
            string closeTxt = DecryptionStrings.CloseAction.Value;
            Vector2 cs = font.MeasureString(closeTxt) * 0.65f;
            Rectangle close = new Rectangle(rect.Right - (int)cs.X - 24, rect.Y + 5,
                (int)cs.X + 16, rect.Height - 10);
            bool over = close.Contains(Main.mouseX, Main.mouseY);
            sb.Draw(px, close, Color.Black * ((over ? 0.65f : 0.35f) * eased));
            sb.Draw(px, new Rectangle(close.X, close.Y, close.Width, 1),
                HackTheme.Danger * (eased * (over ? 1f : 0.7f)));
            sb.Draw(px, new Rectangle(close.X, close.Bottom - 1, close.Width, 1),
                HackTheme.Danger * (eased * (over ? 1f : 0.7f)));
            Utils.DrawBorderString(sb, closeTxt,
                new Vector2(close.X + 8, close.Y + (close.Height - cs.Y) / 2),
                HackTheme.Danger * (eased * (over ? 1f : 0.9f)), 0.65f);

            if (over && Main.mouseLeft && Main.mouseLeftRelease && eased > 0.5f) {
                DecryptionSession.RequestClose();
                Main.mouseLeftRelease = false;
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
            }
        }

        //═══════════════════════════════════════════════════════════
        // 颜色/工具
        //═══════════════════════════════════════════════════════════
        private static Color GetAccentColor() => DecryptionSession.Phase switch {
            DecryptionPhase.Breaching => new Color(255, 180, 90),
            DecryptionPhase.DataFlood => new Color(120, 230, 255),
            DecryptionPhase.MessageReveal => new Color(120, 255, 180),
            _ => new Color(255, 110, 80),//Locked：锈橙
        };

        private static Color GetTitleColor() => DecryptionSession.Phase switch {
            DecryptionPhase.DataFlood => new Color(200, 240, 255),
            DecryptionPhase.MessageReveal => new Color(220, 255, 230),
            _ => new Color(240, 225, 200),
        };

        private static Color GetStatusColor() => DecryptionSession.Phase switch {
            DecryptionPhase.Breaching => new Color(255, 200, 100),
            DecryptionPhase.DataFlood => new Color(120, 230, 255),
            DecryptionPhase.MessageReveal => new Color(140, 255, 200),
            _ => new Color(255, 90, 80),
        };

        private static void DrawLine(SpriteBatch sb, Vector2 a, Vector2 b, Color c, float thickness) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            Vector2 dir = b - a;
            float len = dir.Length();
            float rot = MathF.Atan2(dir.Y, dir.X);
            sb.Draw(px, a, null, c, rot, Vector2.Zero, new Vector2(len, thickness), SpriteEffects.None, 0f);
        }

        //消息展示阶段的输入：空格跳过或关闭，左键跳过
        private static bool prevSpaceDown;
        private void HandleMessageInput() {
            var msg = DecryptionSession.Message;
            bool space = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space);
            bool spaceEdge = space && !prevSpaceDown;
            prevSpaceDown = space;

            if (!msg.FullyRevealed) {
                if (spaceEdge || (Main.mouseLeft && Main.mouseLeftRelease)) {
                    msg.SkipToEnd();
                    Main.mouseLeftRelease = false;
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick with { Pitch = 0.5f });
                }
            }
            else {
                //空格关闭；ESC已由session处理
                if (spaceEdge) {
                    DecryptionSession.RequestClose();
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose);
                }
            }
        }
    }
}
