using CalamityOverhaul.Content.ADV.UIEffect;
using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.IncomingCalls.Styles
{
    /// <summary>
    /// 嘉登科技风格来电——蓝色全息扫描线、电路脉冲、数据粒子
    /// </summary>
    internal class DraedonIncomingCall : IncomingCallBase
    {
        public static DraedonIncomingCall Instance => UIHandleLoader.GetUIHandleOfType<DraedonIncomingCall>();

        #region 动画参数

        private float scanLineTimer;
        private float circuitPulseTimer;
        private float hologramFlicker;
        private float dataStreamTimer;

        private readonly List<DraedonDataPRT> dataParticles = [];
        private int dataParticleSpawnTimer;
        private readonly List<CircuitNodePRT> circuitNodes = [];
        private int circuitNodeSpawnTimer;

        /// <summary>
        /// 来电信号波动画——环绕头像的三条弧线
        /// </summary>
        private float signalArcTimer;

        /// <summary>
        /// 接听按钮脉冲
        /// </summary>
        private float answerBtnPulse;

        /// <summary>
        /// 通话状态指示器闪烁
        /// </summary>
        private float callIndicatorBlink;

        #endregion

        #region 样式参数

        protected override float RingingPanelWidth => 280f;
        protected override float RingingPanelHeight => 110f;
        protected override float SpeakingPanelWidth => 400f;
        protected override float SpeakingPanelHeight => 220f;
        protected override float LeftMargin => 24f;
        protected override float TopRatio => 0.22f;
        protected override float PortraitSize => 56f;
        protected override float TextScale => 0.82f;
        protected override float NameScale => 0.9f;
        protected override int TypeInterval => 2;
        protected override int AutoHangUpDelay => 120;

        #endregion

        #region 生命周期

        protected override void OnCallStarted() {
            scanLineTimer = 0f;
            circuitPulseTimer = 0f;
            hologramFlicker = 0f;
            dataStreamTimer = 0f;
            signalArcTimer = 0f;
            answerBtnPulse = 0f;
            callIndicatorBlink = 0f;
            dataParticles.Clear();
            circuitNodes.Clear();
            dataParticleSpawnTimer = 0;
            circuitNodeSpawnTimer = 0;
        }

        #endregion

        #region 更新

        protected override void StyleUpdate() {
            scanLineTimer += 0.048f;
            circuitPulseTimer += 0.025f;
            hologramFlicker += 0.12f;
            dataStreamTimer += 0.055f;
            signalArcTimer += 0.06f;
            answerBtnPulse += 0.08f;
            callIndicatorBlink += 0.04f;

            WrapTimer(ref scanLineTimer);
            WrapTimer(ref circuitPulseTimer);
            WrapTimer(ref hologramFlicker);
            WrapTimer(ref dataStreamTimer);
            WrapTimer(ref signalArcTimer);
            WrapTimer(ref answerBtnPulse);
            WrapTimer(ref callIndicatorBlink);

            Rectangle panelRect = GetCurrentPanelRect();
            Vector2 panelPos = new(panelRect.X, panelRect.Y);
            Vector2 panelSize = new(panelRect.Width, panelRect.Height);

            //数据粒子
            dataParticleSpawnTimer++;
            if (State != IncomingCallState.Idle && State != IncomingCallState.Ending
                && dataParticleSpawnTimer >= 20 && dataParticles.Count < 10) {
                dataParticleSpawnTimer = 0;
                Vector2 p = panelPos + new Vector2(
                    Main.rand.NextFloat(10f, panelSize.X - 10f),
                    Main.rand.NextFloat(10f, panelSize.Y - 10f));
                dataParticles.Add(new DraedonDataPRT(p));
            }
            for (int i = dataParticles.Count - 1; i >= 0; i--) {
                if (dataParticles[i].Update(panelPos, panelSize))
                    dataParticles.RemoveAt(i);
            }

            //电路节点
            circuitNodeSpawnTimer++;
            if (State != IncomingCallState.Idle && State != IncomingCallState.Ending
                && circuitNodeSpawnTimer >= 30 && circuitNodes.Count < 5) {
                circuitNodeSpawnTimer = 0;
                Vector2 start = panelPos + new Vector2(
                    Main.rand.NextFloat(10f, panelSize.X - 10f),
                    Main.rand.NextFloat(10f, panelSize.Y - 10f));
                circuitNodes.Add(new CircuitNodePRT(start));
            }
            for (int i = circuitNodes.Count - 1; i >= 0; i--) {
                if (circuitNodes[i].Update())
                    circuitNodes.RemoveAt(i);
            }
        }

        private static void WrapTimer(ref float timer) {
            if (timer > MathHelper.TwoPi) timer -= MathHelper.TwoPi;
        }

        #endregion

        #region 绘制——振铃

        protected override void DrawRingingPanel(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //阴影
            Rectangle shadow = rect;
            shadow.Offset(4, 5);
            sb.Draw(px, shadow, new Rectangle(0, 0, 1, 1), Color.Black * (alpha * 0.55f));

            //背景
            DrawTechBackground(sb, rect, alpha);

            //边框
            DrawTechBorder(sb, rect, alpha);

            //粒子
            DrawParticles(sb, alpha);

            //头像区域
            float portraitDrawSize = PortraitSize;
            Vector2 portraitCenter = new(rect.X + 16 + portraitDrawSize / 2f, rect.Y + rect.Height / 2f);
            DrawPortraitFrame(sb, portraitCenter, portraitDrawSize, alpha);
            DrawPortrait(sb, portraitCenter, portraitDrawSize * 0.85f, alpha);

            //振铃信号弧
            DrawSignalArcs(sb, portraitCenter, portraitDrawSize, alpha);

            //来电者名称
            float textX = rect.X + 16 + portraitDrawSize + 14f;
            float nameY = rect.Y + rect.Height * 0.28f;
            Color nameColor = new Color(80, 220, 255) * alpha;
            Utils.DrawBorderString(sb, callerName ?? "???", new Vector2(textX, nameY), nameColor, NameScale);

            //来电提示文字（闪烁）
            float blinkAlpha = (MathF.Sin(answerBtnPulse * 2f) * 0.5f + 0.5f);
            Color hintColor = new Color(140, 200, 255) * (alpha * 0.7f * blinkAlpha);
            string hintText = "▶ INCOMING";
            Utils.DrawBorderString(sb, hintText, new Vector2(textX, nameY + 26), hintColor, 0.72f);

            //接听提示
            Color clickHint = new Color(60, 180, 255) * (alpha * 0.55f);
            Utils.DrawBorderString(sb, "[CLICK TO ANSWER]", new Vector2(textX, nameY + 48), clickHint, 0.6f);

            //扫描线
            DrawScanLine(sb, rect, alpha);
        }

        #endregion

        #region 绘制——通话

        protected override void DrawSpeakingPanel(SpriteBatch sb, Rectangle rect, float alpha, float contentAlpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //阴影
            Rectangle shadow = rect;
            shadow.Offset(4, 5);
            sb.Draw(px, shadow, new Rectangle(0, 0, 1, 1), Color.Black * (alpha * 0.55f));

            //背景
            DrawTechBackground(sb, rect, alpha);

            //边框
            DrawTechBorder(sb, rect, alpha);

            //粒子
            DrawParticles(sb, alpha);

            //通话状态指示器
            float indicatorBlink = MathF.Sin(callIndicatorBlink * 3f) * 0.5f + 0.5f;
            Color indicatorColor = new Color(40, 255, 120) * (alpha * indicatorBlink);
            Vector2 indicatorPos = new(rect.X + rect.Width - 22, rect.Y + 10);
            sb.Draw(px, indicatorPos, new Rectangle(0, 0, 1, 1), indicatorColor, 0f,
                new Vector2(0.5f), new Vector2(6f), SpriteEffects.None, 0f);

            //头像
            float portraitDrawSize = PortraitSize;
            Vector2 portraitCenter = new(rect.X + 16 + portraitDrawSize / 2f, rect.Y + 16 + portraitDrawSize / 2f);
            DrawPortraitFrame(sb, portraitCenter, portraitDrawSize, alpha);
            DrawPortrait(sb, portraitCenter, portraitDrawSize * 0.85f, alpha);

            //来电者名称
            float nameX = rect.X + 16 + portraitDrawSize + 12f;
            float nameY = rect.Y + 16;
            string speakerName = current?.Speaker ?? callerName ?? "???";
            Color nameColor = new Color(80, 220, 255) * alpha * contentAlpha;
            Utils.DrawBorderString(sb, speakerName, new Vector2(nameX, nameY), nameColor, NameScale);

            //分割线
            float divY = nameY + 22;
            Color divColor = new Color(60, 160, 240) * (alpha * 0.6f * contentAlpha);
            sb.Draw(px, new Rectangle((int)nameX, (int)divY, rect.Width - (int)(nameX - rect.X) - 14, 1),
                new Rectangle(0, 0, 1, 1), divColor);

            //台词文本
            if (wrappedLines != null && wrappedLines.Length > 0) {
                Vector2 textStart = new(nameX, divY + 8);
                Color textColor = Color.Lerp(new Color(200, 240, 255), Color.White, 0.25f);
                DrawTypedText(sb, textStart, contentAlpha * alpha, textColor);
            }

            //底部提示
            if (finishedCurrent && current != null) {
                float hintBlink = MathF.Sin(answerBtnPulse * 2.5f) * 0.5f + 0.5f;
                Color hintColor = new Color(80, 200, 255) * (alpha * contentAlpha * 0.6f * hintBlink);
                string hint = queue.Count > 0 ? "▶" : "■ END";
                Vector2 hintPos = new(rect.X + rect.Width - 40, rect.Y + rect.Height - 22);
                Utils.DrawBorderString(sb, hint, hintPos, hintColor, 0.7f);
            }

            //扫描线
            DrawScanLine(sb, rect, alpha);
        }

        protected override Vector2 ApplyTextLineOffset(Vector2 basePos, int lineIndex) {
            float shift = MathF.Sin(dataStreamTimer * 1.8f + lineIndex * 0.45f) * 0.6f;
            return basePos + new Vector2(shift, 0);
        }

        #endregion

        #region 样式绘制工具

        private void DrawTechBackground(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            int segs = 20;
            for (int i = 0; i < segs; i++) {
                float t = i / (float)segs;
                float t2 = (i + 1) / (float)segs;
                int y1 = rect.Y + (int)(t * rect.Height);
                int y2 = rect.Y + (int)(t2 * rect.Height);
                Rectangle r = new(rect.X, y1, rect.Width, Math.Max(1, y2 - y1));

                Color dark = new Color(8, 12, 22);
                Color mid = new Color(18, 28, 42);

                float pulse = MathF.Sin(circuitPulseTimer * 0.6f + t * 2f) * 0.5f + 0.5f;
                Color c = Color.Lerp(dark, mid, pulse) * (alpha * 0.92f);
                sb.Draw(px, r, new Rectangle(0, 0, 1, 1), c);
            }

            //全息闪烁叠加
            float flicker = MathF.Sin(hologramFlicker * 1.5f) * 0.5f + 0.5f;
            Color overlay = new Color(15, 30, 45) * (alpha * 0.2f * flicker);
            sb.Draw(px, rect, new Rectangle(0, 0, 1, 1), overlay);
        }

        private static void DrawTechBorder(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            Color edge = new Color(50, 170, 240) * (alpha * 0.85f);

            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 2), new Rectangle(0, 0, 1, 1), edge);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), new Rectangle(0, 0, 1, 1), edge * 0.7f);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 2, rect.Height), new Rectangle(0, 0, 1, 1), edge * 0.85f);
            sb.Draw(px, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), new Rectangle(0, 0, 1, 1), edge * 0.85f);

            //内框
            Rectangle inner = rect;
            inner.Inflate(-4, -4);
            Color innerC = new Color(80, 200, 255) * (alpha * 0.15f);
            sb.Draw(px, new Rectangle(inner.X, inner.Y, inner.Width, 1), new Rectangle(0, 0, 1, 1), innerC);
            sb.Draw(px, new Rectangle(inner.X, inner.Bottom - 1, inner.Width, 1), new Rectangle(0, 0, 1, 1), innerC * 0.7f);
            sb.Draw(px, new Rectangle(inner.X, inner.Y, 1, inner.Height), new Rectangle(0, 0, 1, 1), innerC * 0.85f);
            sb.Draw(px, new Rectangle(inner.Right - 1, inner.Y, 1, inner.Height), new Rectangle(0, 0, 1, 1), innerC * 0.85f);

            //四角电路
            DrawCornerMark(sb, new Vector2(rect.X + 8, rect.Y + 8), alpha);
            DrawCornerMark(sb, new Vector2(rect.Right - 8, rect.Y + 8), alpha);
            DrawCornerMark(sb, new Vector2(rect.X + 8, rect.Bottom - 8), alpha * 0.7f);
            DrawCornerMark(sb, new Vector2(rect.Right - 8, rect.Bottom - 8), alpha * 0.7f);
        }

        private static void DrawCornerMark(SpriteBatch sb, Vector2 pos, float a) {
            Texture2D px = VaultAsset.placeholder2.Value;
            Color c = new Color(100, 220, 255) * a;
            float size = 4f;
            sb.Draw(px, pos, new Rectangle(0, 0, 1, 1), c, 0f, new Vector2(0.5f),
                new Vector2(size, size * 0.2f), SpriteEffects.None, 0f);
            sb.Draw(px, pos, new Rectangle(0, 0, 1, 1), c * 0.85f, MathHelper.PiOver2, new Vector2(0.5f),
                new Vector2(size, size * 0.2f), SpriteEffects.None, 0f);
        }

        private void DrawScanLine(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float scanY = rect.Y + MathF.Sin(scanLineTimer) * 0.5f * rect.Height + rect.Height * 0.5f;

            for (int i = -1; i <= 1; i++) {
                float offsetY = scanY + i * 2f;
                if (offsetY < rect.Y || offsetY > rect.Bottom) continue;
                float intensity = 1f - MathF.Abs(i) * 0.35f;
                Color scanColor = new Color(60, 180, 255) * (alpha * 0.12f * intensity);
                sb.Draw(px, new Rectangle(rect.X + 4, (int)offsetY, rect.Width - 8, 1),
                    new Rectangle(0, 0, 1, 1), scanColor);
            }
        }

        private static void DrawPortraitFrame(SpriteBatch sb, Vector2 center, float size, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float half = size / 2f;
            Rectangle frame = new((int)(center.X - half - 3), (int)(center.Y - half - 3),
                (int)(size + 6), (int)(size + 6));

            //背景
            Color bg = new Color(8, 16, 30) * (alpha * 0.85f);
            sb.Draw(px, frame, new Rectangle(0, 0, 1, 1), bg);

            //边框
            Color edge = new Color(60, 170, 240) * (alpha * 0.6f);
            int bw = 2;
            sb.Draw(px, new Rectangle(frame.X, frame.Y, frame.Width, bw), new Rectangle(0, 0, 1, 1), edge);
            sb.Draw(px, new Rectangle(frame.X, frame.Bottom - bw, frame.Width, bw), new Rectangle(0, 0, 1, 1), edge * 0.75f);
            sb.Draw(px, new Rectangle(frame.X, frame.Y, bw, frame.Height), new Rectangle(0, 0, 1, 1), edge * 0.85f);
            sb.Draw(px, new Rectangle(frame.Right - bw, frame.Y, bw, frame.Height), new Rectangle(0, 0, 1, 1), edge * 0.85f);
        }

        private void DrawSignalArcs(SpriteBatch sb, Vector2 center, float size, float alpha) {
            Texture2D px = VaultAsset.placeholder2.Value;

            //绘制振铃脉冲环
            foreach (float progress in ringPulses) {
                float radius = size * 0.5f + progress * size * 0.6f;
                float ringAlpha = (1f - progress) * alpha * 0.6f;
                Color ringColor = new Color(80, 220, 255) * ringAlpha;

                //简化版弧线——用短线段模拟三条弧
                for (int arc = 0; arc < 3; arc++) {
                    float baseAngle = MathHelper.TwoPi / 3f * arc + signalArcTimer;
                    float arcLength = MathHelper.Pi * 0.35f;
                    int segments = 8;

                    for (int s = 0; s < segments; s++) {
                        float t = s / (float)segments;
                        float angle = baseAngle - arcLength / 2f + arcLength * t;
                        Vector2 pos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                        sb.Draw(px, pos, new Rectangle(0, 0, 1, 1), ringColor * (1f - t * 0.3f), 0f,
                            new Vector2(0.5f), new Vector2(2.5f), SpriteEffects.None, 0f);
                    }
                }
            }

            //静态信号弧线（常驻，表示通讯活跃）
            float staticAlpha = MathF.Sin(signalArcTimer * 1.5f) * 0.3f + 0.4f;
            for (int arc = 0; arc < 3; arc++) {
                float baseAngle = MathHelper.TwoPi / 3f * arc + signalArcTimer * 0.5f + MathHelper.PiOver4;
                float arcLength = MathHelper.Pi * 0.25f;
                float radius = size * 0.55f;
                int segments = 6;

                for (int s = 0; s < segments; s++) {
                    float t = s / (float)segments;
                    float angle = baseAngle - arcLength / 2f + arcLength * t;
                    Vector2 pos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                    Color c = new Color(40, 160, 240) * (alpha * staticAlpha * (1f - t * 0.4f));
                    sb.Draw(px, pos, new Rectangle(0, 0, 1, 1), c, 0f,
                        new Vector2(0.5f), new Vector2(2f), SpriteEffects.None, 0f);
                }
            }
        }

        private void DrawParticles(SpriteBatch sb, float alpha) {
            foreach (var node in circuitNodes) {
                node.Draw(sb, alpha * 0.7f);
            }
            foreach (var particle in dataParticles) {
                particle.Draw(sb, alpha * 0.6f);
            }
        }

        #endregion
    }
}
