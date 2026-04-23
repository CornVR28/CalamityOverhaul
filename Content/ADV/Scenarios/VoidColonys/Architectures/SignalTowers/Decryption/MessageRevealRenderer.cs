using CalamityOverhaul.Common;
using CalamityOverhaul.Content.HackTimes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.SignalTowers.Decryption
{
    /// <summary>
    /// 破译消息片段：表示一条可以逐字符显示的日志行
    /// 源（Source）显示在行首方括号内
    /// </summary>
    internal class DecryptedLine
    {
        public string Source;
        public string Body;
        public Color Tint;
        public bool IsHeader;
    }

    /// <summary>
    /// 解码消息阶段
    /// 打字机方式逐字符把多行日志吐到屏幕上，所有行显示完毕允许玩家手动关闭
    /// 具体条目内容预留给本地化/外部脚本注入
    /// </summary>
    internal class MessageRevealStage
    {
        private const float CharsPerSecond = 60f;

        public List<DecryptedLine> Lines { get; } = [];
        public float RevealedChars { get; private set; }
        public int TotalChars { get; private set; }
        public bool FullyRevealed => RevealedChars >= TotalChars;
        public float IdleTimer { get; private set; }

        public void SetContent(IEnumerable<DecryptedLine> lines) {
            Lines.Clear();
            Lines.AddRange(lines);
            RevealedChars = 0f;
            TotalChars = 0;
            foreach (var ln in Lines) TotalChars += (ln.Body?.Length ?? 0);
            IdleTimer = 0f;
        }

        public void Clear() {
            Lines.Clear();
            RevealedChars = 0f;
            TotalChars = 0;
            IdleTimer = 0f;
        }

        public void Update(float dt) {
            if (!FullyRevealed) {
                RevealedChars += CharsPerSecond * dt;
                if (RevealedChars > TotalChars) RevealedChars = TotalChars;
                //每8字符嘀一声
                int before = (int)(RevealedChars - CharsPerSecond * dt);
                int after = (int)RevealedChars;
                if (after / 8 > before / 8 && after <= TotalChars) {
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.25f, Pitch = 0.3f });
                }
            }
            else {
                IdleTimer += dt;
            }
        }

        public void SkipToEnd() {
            RevealedChars = TotalChars;
        }
    }

    /// <summary>
    /// 消息展示本地化
    /// </summary>
    internal class MessageStrings : ModSystem, ILocalizedModType
    {
        public string LocalizationCategory => "UI";
        public static LocalizedText Header { get; private set; }
        public static LocalizedText SourceSystem { get; private set; }
        public static LocalizedText SourceSignal { get; private set; }
        public static LocalizedText SourceArchive { get; private set; }
        public static LocalizedText FragmentTag { get; private set; }
        public static LocalizedText SignatureTag { get; private set; }
        public static LocalizedText[] DefaultBody { get; private set; }
        public static LocalizedText SkipHint { get; private set; }
        public static LocalizedText CloseHint { get; private set; }

        public override void SetStaticDefaults() {
            Header = this.GetLocalization("MsgHeader", () => "── 截获片段 ▸ 来源 0x{0} / {1} ──");
            SourceSystem = this.GetLocalization("MsgSrcSystem", () => "SYS");
            SourceSignal = this.GetLocalization("MsgSrcSignal", () => "SIG");
            SourceArchive = this.GetLocalization("MsgSrcArchive", () => "ARC");
            FragmentTag = this.GetLocalization("MsgFragment", () => "FRAGMENT");
            SignatureTag = this.GetLocalization("MsgSignature", () => "SIGN   ▸ {0}");
            //默认占位内容：按顺序占位，后续剧情脚本可覆盖
            DefaultBody = new LocalizedText[]{
                this.GetLocalization("MsgBody0", () => "终端已接入。校验密钥匹配度 98.4%。"),
                this.GetLocalization("MsgBody1", () => "...噪声中仍残留未标注的脉冲信号。"),
                this.GetLocalization("MsgBody2", () => "坐标偏移量指向坍缩核心的深层回声。"),
                this.GetLocalization("MsgBody3", () => "该节点被标记为 ██CLASSIFIED██，相关片段已封存。"),
                this.GetLocalization("MsgBody4", () => "下一个协议窗口将由你的行动触发。"),
            };
            SkipHint = this.GetLocalization("MsgSkipHint", () => "[空格/左键] 跳过逐字显示");
            CloseHint = this.GetLocalization("MsgCloseHint", () => "[空格/ESC] 断开连接");
        }

        /// <summary>从当前塔的WhoAmI生成一份默认消息</summary>
        public static List<DecryptedLine> BuildDefaultPayload(SignalTowerActor tower) {
            int id = tower?.WhoAmI ?? 0;
            string idHex = $"{id & 0xFFFF:X4}";
            var list = new List<DecryptedLine> {
                new() {
                    Source = "",
                    Body = Header.Format(idHex, SourceSignal.Value),
                    Tint = new Color(120, 230, 255),
                    IsHeader = true,
                },
            };
            //主体5行
            string[] srcs = [SourceSystem.Value, SourceSignal.Value, SourceSignal.Value, SourceArchive.Value, SourceSystem.Value];
            Color[] tints = [
                new(200, 230, 255),
                new(210, 240, 220),
                new(255, 230, 160),
                new(220, 180, 255),
                new(200, 255, 220),
            ];
            for (int i = 0; i < DefaultBody.Length; i++) {
                list.Add(new DecryptedLine {
                    Source = srcs[i % srcs.Length],
                    Body = DefaultBody[i].Value,
                    Tint = tints[i % tints.Length],
                    IsHeader = false,
                });
            }
            //签名
            list.Add(new DecryptedLine {
                Source = "",
                Body = SignatureTag.Format($"SHA-{unchecked((uint)HashCode.Combine(id, 0xA17)):X8}"),
                Tint = new Color(180, 200, 220),
                IsHeader = true,
            });
            return list;
        }
    }

    /// <summary>消息阶段渲染</summary>
    internal static class MessageRevealRenderer
    {
        public static void Draw(SpriteBatch sb, MessageRevealStage stage, Rectangle body, float eased, Color accent) {
            if (stage == null) return;
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;

            //整体背景板
            sb.Draw(px, body, Color.Black * (0.55f * eased));
            sb.Draw(px, new Rectangle(body.X, body.Y, body.Width, 1), accent * (eased * 0.65f));
            sb.Draw(px, new Rectangle(body.X, body.Bottom - 1, body.Width, 1), accent * (eased * 0.45f));

            int lineHeight = 22;
            int padX = 14;
            int y = body.Y + 12;
            float textScale = 0.66f;

            //逐行绘制，计算字符预算
            int budget = (int)stage.RevealedChars;
            foreach (var ln in stage.Lines) {
                if (y > body.Bottom - lineHeight) break;
                int want = ln.Body?.Length ?? 0;
                int show = Math.Min(want, budget);
                budget -= show;
                string shownText = ln.Body == null ? string.Empty : ln.Body.Substring(0, show);

                //源tag
                int textX = body.X + padX;
                if (!string.IsNullOrEmpty(ln.Source)) {
                    string tag = $"[{ln.Source}]";
                    Vector2 ts = font.MeasureString(tag) * 0.6f;
                    Rectangle tb = new Rectangle(textX - 2, y - 1, (int)ts.X + 6, (int)ts.Y + 2);
                    sb.Draw(px, tb, Color.Black * (0.55f * eased));
                    sb.Draw(px, new Rectangle(tb.X, tb.Y, tb.Width, 1), ln.Tint * (eased * 0.9f));
                    sb.Draw(px, new Rectangle(tb.X, tb.Bottom - 1, tb.Width, 1), ln.Tint * (eased * 0.5f));
                    Utils.DrawBorderString(sb, tag, new Vector2(textX + 2, y), ln.Tint * eased, 0.6f);
                    textX += (int)ts.X + 12;
                }

                Color bodyCol = ln.IsHeader
                    ? ln.Tint * (eased * 0.95f)
                    : HackTheme.TextBright * (eased * 0.92f);
                //阴影
                Utils.DrawBorderString(sb, shownText,
                    new Vector2(textX, y), bodyCol, textScale);

                //未完的光标
                if (show < want) {
                    Vector2 ms = font.MeasureString(shownText) * textScale;
                    float blink = 0.5f + 0.5f * MathF.Sin(DecryptionSession.SessionTime * 10f);
                    sb.Draw(px, new Rectangle((int)(textX + ms.X + 2), y + 2, 2, lineHeight - 4),
                        accent * (eased * blink));
                    break;
                }
                y += lineHeight;
            }

            //底部状态栏
            string hintStr = stage.FullyRevealed
                ? MessageStrings.CloseHint.Value
                : MessageStrings.SkipHint.Value;
            Vector2 hs = font.MeasureString(hintStr) * 0.6f;
            Utils.DrawBorderString(sb, hintStr,
                new Vector2(body.X + (body.Width - hs.X) / 2, body.Bottom - hs.Y - 6),
                new Color(180, 220, 255) * (eased * 0.9f), 0.6f);
        }
    }
}
