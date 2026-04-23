using CalamityOverhaul.Common;
using CalamityOverhaul.Content.HackTimes;
using Microsoft.Xna.Framework;
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
    /// 数据洪流演出
    /// 破译成功 → 长条进度 + 字符雨 + 分段状态指标 + 波形图
    /// 持续约5秒后自动进入MessageReveal
    /// </summary>
    internal class DataFloodStage
    {
        public const float DurationSeconds = 5.5f;
        public const int ColumnCount = 48;
        public const int WaveSamples = 96;

        private readonly float[] columnOffsets = new float[ColumnCount];
        private readonly float[] columnSpeeds = new float[ColumnCount];
        private readonly float[] waveValues = new float[WaveSamples];
        private readonly float[] waveTargets = new float[WaveSamples];
        private float elapsed;
        private bool started;

        public float Progress => MathHelper.Clamp(elapsed / DurationSeconds, 0f, 1f);
        public bool Completed => elapsed >= DurationSeconds;

        public void Reset() {
            elapsed = 0f;
            started = false;
        }

        public void Update(float dt) {
            if (!started) {
                var rng = new FastRandom(unchecked((long)((ulong)Main.GameUpdateCount * 2685821657736338717UL + 1)));
                for (int i = 0; i < ColumnCount; i++) {
                    columnOffsets[i] = rng.Next(1000) * 0.01f;
                    columnSpeeds[i] = 3f + rng.Next(100) * 0.04f;
                }
                for (int i = 0; i < WaveSamples; i++) waveValues[i] = 0.5f;
                started = true;
            }
            elapsed += dt;
            for (int i = 0; i < ColumnCount; i++) columnOffsets[i] += columnSpeeds[i] * dt;

            //波形：EQ风格缓动到随机目标
            if ((int)(elapsed * 30f) % 4 == 0) {
                var rng = new FastRandom(unchecked((long)((ulong)Main.GameUpdateCount * 48271 + 7)));
                for (int i = 0; i < WaveSamples; i++) {
                    waveTargets[i] = 0.15f + rng.Next(1000) * 0.00085f;
                }
            }
            for (int i = 0; i < WaveSamples; i++) {
                waveValues[i] = MathHelper.Lerp(waveValues[i], waveTargets[i], 0.18f);
            }
        }

        public void SkipToEnd() {
            elapsed = DurationSeconds;
        }

        public float GetColumnOffset(int i) => columnOffsets[i % ColumnCount];
        public float GetWaveValue(int i) => waveValues[i % WaveSamples];
    }

    /// <summary>数据洪流绘制</summary>
    internal static class DataFloodRenderer
    {
        private const string HexCharset = "0123456789ABCDEF";

        public static void Draw(SpriteBatch sb, DataFloodStage stage, Rectangle body, float eased, Color accent) {
            if (stage == null) return;
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;

            //主体分区：上方字符雨(占60%) + 中部波形(12%) + 底部状态列表(剩余)
            int topH = (int)(body.Height * 0.6f);
            int midH = (int)(body.Height * 0.14f);
            Rectangle topArea = new Rectangle(body.X + 4, body.Y + 4, body.Width - 8, topH);
            Rectangle midArea = new Rectangle(body.X + 4, topArea.Bottom + 4, body.Width - 8, midH);
            Rectangle botArea = new Rectangle(body.X + 4, midArea.Bottom + 4,
                body.Width - 8, body.Bottom - midArea.Bottom - 8);

            DrawCharRain(sb, stage, topArea, eased, accent);
            DrawWaveform(sb, stage, midArea, eased, accent);
            DrawStatus(sb, stage, botArea, eased, accent);
            DrawProgressBar(sb, stage, body, eased, accent);
        }

        private static void DrawCharRain(SpriteBatch sb, DataFloodStage stage, Rectangle area, float eased, Color accent) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;

            sb.Draw(px, area, Color.Black * (0.55f * eased));
            //顶/底细线
            sb.Draw(px, new Rectangle(area.X, area.Y, area.Width, 1), accent * (eased * 0.7f));
            sb.Draw(px, new Rectangle(area.X, area.Bottom - 1, area.Width, 1), accent * (eased * 0.45f));

            int cols = DataFloodStage.ColumnCount;
            float colW = (float)area.Width / cols;
            float charH = 11f;
            int visibleRows = (int)(area.Height / charH) + 2;

            //按列绘制可见字符
            for (int c = 0; c < cols; c++) {
                float xCenter = area.X + colW * (c + 0.5f);
                float offset = stage.GetColumnOffset(c);
                for (int r = 0; r < visibleRows; r++) {
                    float y = area.Y + ((r * charH + offset * charH) % (area.Height + charH)) - charH;
                    if (y < area.Y - charH || y > area.Bottom) continue;
                    //字符由列+行+offset派生哈希选取
                    int hash = (int)((c * 73856093L ^ r * 19349663L ^ (long)(offset * 1000)) & 0x7FFFFFFF);
                    char ch = HexCharset[hash % HexCharset.Length];
                    //从顶部下落：越往下越亮，最底部为头部白色
                    float t = (r + (offset % 1f)) / visibleRows;
                    float fade = MathHelper.Clamp(t * 1.2f, 0f, 1f);
                    Color col;
                    if (r == visibleRows - 1) {
                        col = Color.White * (eased * 0.9f);
                    }
                    else {
                        col = Color.Lerp(accent * 0.25f, accent, fade) * (eased * (0.4f + 0.6f * fade));
                    }
                    Vector2 tp = new Vector2(xCenter - 3f, y);
                    sb.DrawString(font, ch.ToString(), tp, col, 0f, Vector2.Zero,
                        0.5f, SpriteEffects.None, 0f);
                }
            }

            //CRT扫描线
            float scanY = area.Y + ((DecryptionSession.SessionTime * 0.5f) % 1f) * area.Height;
            sb.Draw(px, new Rectangle(area.X, (int)scanY, area.Width, 2), Color.White * (eased * 0.18f));
        }

        private static void DrawWaveform(SpriteBatch sb, DataFloodStage stage, Rectangle area, float eased, Color accent) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            sb.Draw(px, area, Color.Black * (0.55f * eased));
            sb.Draw(px, new Rectangle(area.X, area.Y, area.Width, 1), accent * (eased * 0.55f));
            sb.Draw(px, new Rectangle(area.X, area.Bottom - 1, area.Width, 1), accent * (eased * 0.4f));

            int n = DataFloodStage.WaveSamples;
            float barW = (float)area.Width / n;
            for (int i = 0; i < n; i++) {
                float v = stage.GetWaveValue(i);
                int h = (int)(v * (area.Height - 4));
                int x = (int)(area.X + i * barW);
                int bw = Math.Max(1, (int)(barW * 0.72f));
                int y = area.Bottom - 2 - h;
                Color col = Color.Lerp(accent, new Color(255, 255, 255), v * 0.4f) * (eased * 0.92f);
                sb.Draw(px, new Rectangle(x, y, bw, h), col);
                //顶部高亮
                sb.Draw(px, new Rectangle(x, y, bw, 1), Color.White * (eased * 0.85f));
            }
        }

        private static void DrawStatus(SpriteBatch sb, DataFloodStage stage, Rectangle area, float eased, Color accent) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            sb.Draw(px, area, Color.Black * (0.5f * eased));

            //五行分段状态，基于stage.Progress分段点亮
            string[] stages = [
                "▸ HANDSHAKE      SYN/ACK",
                "▸ KEY EXCHANGE   RSA-4096",
                "▸ STREAM DECRYPT AES-GCM",
                "▸ PACKET REASSEMBLE",
                "▸ SIGNATURE VERIFY",
            ];
            float p = stage.Progress;
            int lineH = area.Height / stages.Length;
            for (int i = 0; i < stages.Length; i++) {
                float segStart = i / (float)stages.Length;
                float segEnd = (i + 1) / (float)stages.Length;
                float local = MathHelper.Clamp((p - segStart) / (segEnd - segStart), 0f, 1f);
                int y = area.Y + i * lineH + 2;
                Color lineCol = local <= 0f
                    ? HackTheme.TextNormal * (eased * 0.4f)
                    : Color.Lerp(accent * 0.8f, new Color(140, 255, 200), local) * (eased * 0.95f);
                Utils.DrawBorderString(sb, stages[i],
                    new Vector2(area.X + 6, y),
                    lineCol, 0.58f);

                //右侧状态字
                string tag = local >= 1f ? "[ OK ]" : (local > 0f ? $"[{(int)(local * 100),3}%]" : "[ -- ]");
                DynamicSpriteFont f = font;
                Vector2 ts = f.MeasureString(tag) * 0.55f;
                Utils.DrawBorderString(sb, tag,
                    new Vector2(area.Right - ts.X - 8, y),
                    lineCol, 0.55f);

                //进度微条
                int miniY = y + 16;
                int miniW = area.Width - 12;
                sb.Draw(px, new Rectangle(area.X + 6, miniY, miniW, 2),
                    Color.Black * (0.7f * eased));
                int fw = (int)(miniW * local);
                if (fw > 0)
                    sb.Draw(px, new Rectangle(area.X + 6, miniY, fw, 2), lineCol);
            }
        }

        private static void DrawProgressBar(SpriteBatch sb, DataFloodStage stage, Rectangle body, float eased, Color accent) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            //顶部覆盖一条8px大进度条
            Rectangle bar = new Rectangle(body.X + 4, body.Y - 10, body.Width - 8, 4);
            sb.Draw(px, bar, Color.Black * (0.8f * eased));
            int fw = (int)(bar.Width * stage.Progress);
            if (fw > 0) {
                sb.Draw(px, new Rectangle(bar.X, bar.Y, fw, bar.Height),
                    new Color(160, 250, 255) * eased);
                //前端闪光
                int glowW = 6;
                sb.Draw(px, new Rectangle(bar.X + fw - glowW, bar.Y - 1, glowW, bar.Height + 2),
                    Color.White * (eased * 0.85f));
            }
            //百分数文字
            string pct = $"{(int)(stage.Progress * 100),3}%";
            Vector2 ps = font.MeasureString(pct) * 0.58f;
            Utils.DrawBorderString(sb, pct,
                new Vector2(body.Right - ps.X - 10, body.Y - 30),
                new Color(200, 240, 255) * eased, 0.58f);
        }
    }
}
