using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.Cyberwares.Implementation.Sandevistans
{
    /// <summary>
    /// 赛博朋克2077风格斯安威斯坦冷却HUD
    /// 四段式弧形环 + 色差分离 + 故障碎片 + 数据粒子 + 神经脉冲 + 扫描线
    /// </summary>
    internal class SandevistanHUD : UIHandle
    {
        public override bool Active => Sandevistan.GetEquipped(Main.LocalPlayer) != null;

        #region 状态

        private float displayRatio = 1f;
        private float timer;
        private float activePulse;
        private bool wasActive;
        private float transitionFlash;
        private float scanAngle;
        private float neuralBurst;
        private float capFlutter;
        //故障碎片
        private float glitchCD;
        private readonly List<GlitchRect> glitches = new(16);
        //轨道粒子
        private readonly OrbitalDot[] dots = new OrbitalDot[14];
        private bool dotsInited;

        #endregion

        #region 布局

        private const float R_Main = 44f;
        private const float T_Main = 3.5f;
        private const float T_Glow = 9f;
        private const float T_Core = 1.2f;
        private const float R_TickIn = 50f;
        private const float R_TickOut = 55f;
        private const int TickCount = 32;
        //四段弧，每段之间2°间隙
        private const int SegCount = 4;
        private static readonly float GapRad = MathHelper.ToRadians(2f);
        private static readonly float SegAngle = (MathHelper.TwoPi - SegCount * GapRad) / SegCount;
        //弧从顶部 (-PI/2) 顺时针
        private const float ArcOrigin = -MathHelper.PiOver2;

        #endregion

        #region 颜色

        private static readonly Color CyanHi = new(0, 245, 245);
        private static readonly Color CyanDim = new(0, 60, 70);
        private static readonly Color RedHi = new(255, 50, 50);
        private static readonly Color YellowMid = new(255, 200, 50);

        #endregion

        #region 内部结构

        private struct GlitchRect
        {
            public Vector2 Pos;
            public float W, H, Life, MaxLife;
            public Color Tint;
        }

        private struct OrbitalDot
        {
            public float Angle, Speed, Radius, Alpha, Size;
        }

        #endregion

        #region 更新

        public override void Update() {
            float target = Sandevistan.CooldownRatio;
            displayRatio += (target - displayRatio) * 0.1f;
            timer += 0.016f;
            bool active = Sandevistan.IsActive;

            if (active) {
                activePulse += 0.12f;
                scanAngle += 0.06f;
            }
            else {
                activePulse *= 0.93f;
                scanAngle += 0.015f;
            }
            if (scanAngle > MathHelper.TwoPi) scanAngle -= MathHelper.TwoPi;

            //状态转换检测
            if (active && !wasActive) {
                transitionFlash = 1f;
                neuralBurst = 1f;
                SpawnGlitchBurst(6);
            }
            else if (!active && wasActive) {
                transitionFlash = 0.7f;
                neuralBurst = 0.6f;
                SpawnGlitchBurst(4);
            }
            wasActive = active;

            //衰减
            transitionFlash *= 0.88f;
            if (transitionFlash < 0.01f) transitionFlash = 0;
            neuralBurst *= 0.92f;
            if (neuralBurst < 0.01f) neuralBurst = 0;
            capFlutter += 0.25f;

            //故障碎片更新和随机生成
            UpdateGlitches(active);

            //粒子初始化和更新
            if (!dotsInited) {
                InitDots();
                dotsInited = true;
            }
            UpdateDots(active);
        }

        private void SpawnGlitchBurst(int count) {
            var rng = Main.rand;
            for (int i = 0; i < count; i++) {
                float angle = rng.NextFloat() * MathHelper.TwoPi;
                float dist = 25f + rng.NextFloat() * 35f;
                glitches.Add(new GlitchRect {
                    Pos = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist,
                    W = 4 + rng.NextFloat() * 18,
                    H = 2 + rng.NextFloat() * 5,
                    MaxLife = 6 + rng.Next(8),
                    Life = 6 + rng.Next(8),
                    Tint = rng.Next(3) switch {
                        0 => CyanHi,
                        1 => RedHi,
                        _ => new Color(255, 255, 255),
                    }
                });
            }
        }

        private void UpdateGlitches(bool active) {
            for (int i = glitches.Count - 1; i >= 0; i--) {
                var g = glitches[i];
                g.Life--;
                if (g.Life <= 0) { glitches.RemoveAt(i); continue; }
                glitches[i] = g;
            }
            //持续的随机故障（激活时更频繁）
            glitchCD--;
            if (glitchCD <= 0) {
                glitchCD = active ? 8 + Main.rand.Next(12) : 30 + Main.rand.Next(40);
                SpawnGlitchBurst(active ? 2 : 1);
            }
        }

        private void InitDots() {
            var rng = Main.rand;
            for (int i = 0; i < dots.Length; i++) {
                dots[i] = new OrbitalDot {
                    Angle = rng.NextFloat() * MathHelper.TwoPi,
                    Speed = 0.008f + rng.NextFloat() * 0.015f,
                    Radius = R_Main - 6 + rng.NextFloat() * 22f,
                    Alpha = 0.2f + rng.NextFloat() * 0.5f,
                    Size = 1f + rng.NextFloat() * 1.5f,
                };
            }
        }

        private void UpdateDots(bool active) {
            float targetAlphaMul = active ? 1.4f : 0.6f;
            for (int i = 0; i < dots.Length; i++) {
                var d = dots[i];
                d.Angle += d.Speed * (active ? 2.5f : 1f);
                if (d.Angle > MathHelper.TwoPi) d.Angle -= MathHelper.TwoPi;
                d.Alpha += (targetAlphaMul * (0.3f + (float)i / dots.Length * 0.4f) - d.Alpha) * 0.05f;
                dots[i] = d;
            }
        }

        #endregion

        #region 绘制

        public override void Draw(SpriteBatch sb) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float ratio = Math.Clamp(displayRatio, 0f, 1f);
            bool active = Sandevistan.IsActive;
            Vector2 center = new(Main.screenWidth * 0.5f, Main.screenHeight - 82f);

            DrawBackgroundRing(sb, px, center);
            DrawFilledArc(sb, px, center, ratio, active);
            DrawTickMarks(sb, px, center, ratio, active);
            DrawScanSweep(sb, px, center, active);
            DrawDataParticles(sb, px, center);
            DrawArcCap(sb, px, center, ratio, active);
            DrawNeuralPulse(sb, px, center);
            DrawGlitchFragments(sb, px, center);
            DrawCenterContent(sb, font, center, ratio, active);
            DrawCircuitTraces(sb, px, center, active);
            DrawTransitionFlash(sb, px, center);
        }

        //暗色背景全环（四段）
        private void DrawBackgroundRing(SpriteBatch sb, Texture2D px, Vector2 c) {
            for (int s = 0; s < SegCount; s++) {
                float start = ArcOrigin + s * (SegAngle + GapRad);
                DrawArc(sb, px, c, R_Main, start, SegAngle, T_Main, CyanDim * 0.35f, 20);
            }
        }

        //多层填充弧：外辉光 → 色差偏移 → 主弧 → 内核心亮线
        private void DrawFilledArc(SpriteBatch sb, Texture2D px, Vector2 c, float ratio, bool active) {
            if (ratio <= 0.001f) return;
            Color arcColor = GetArcColor(ratio);
            float pulse = active ? 0.8f + MathF.Sin(activePulse * 2f) * 0.2f : 1f;
            float totalFill = ratio * SegAngle * SegCount;

            for (int s = 0; s < SegCount; s++) {
                float segStart = ArcOrigin + s * (SegAngle + GapRad);
                float consumed = s * SegAngle;
                float remaining = totalFill - consumed;
                if (remaining <= 0) break;
                float sweep = MathF.Min(remaining, SegAngle);
                int segs = Math.Max(6, (int)(sweep / MathHelper.TwoPi * 70));

                //外辉光层
                DrawArc(sb, px, c, R_Main, segStart, sweep, T_Glow, arcColor * (0.12f * pulse), segs);

                //色差偏移：红通道偏左，蓝通道偏右
                DrawArc(sb, px, c, R_Main, segStart, sweep, T_Glow * 0.7f,
                    new Color(255, 30, 30) * (0.06f * pulse), segs, new Vector2(-1.5f, 0));
                DrawArc(sb, px, c, R_Main, segStart, sweep, T_Glow * 0.7f,
                    new Color(30, 60, 255) * (0.06f * pulse), segs, new Vector2(1.5f, 0));

                //主弧
                DrawArc(sb, px, c, R_Main, segStart, sweep, T_Main, arcColor * pulse, segs);

                //内核心高亮
                DrawArc(sb, px, c, R_Main, segStart, sweep, T_Core, Color.White * (0.6f * pulse), segs);
            }

            //临界值抖动（<20%）
            if (ratio < 0.2f) {
                float flicker = MathF.Sin(timer * 30f) > 0.3f ? 1f : 0.3f;
                float reSweep = ratio * SegAngle * SegCount;
                for (int s = 0; s < SegCount; s++) {
                    float segStart = ArcOrigin + s * (SegAngle + GapRad);
                    float consumed = s * SegAngle;
                    float remaining = reSweep - consumed;
                    if (remaining <= 0) break;
                    float sweep = MathF.Min(remaining, SegAngle);
                    int segs = Math.Max(4, (int)(sweep / MathHelper.TwoPi * 40));
                    DrawArc(sb, px, c, R_Main, segStart, sweep, T_Main + 1f, RedHi * (0.3f * flicker), segs);
                }
            }
        }

        //弧末端光标：一个明亮的点标识当前充能位置
        private void DrawArcCap(SpriteBatch sb, Texture2D px, Vector2 c, float ratio, bool active) {
            if (ratio <= 0.001f || ratio >= 0.999f) return;
            float fillAngle = GetFillAngle(ratio);
            Vector2 capPos = c + new Vector2(MathF.Cos(fillAngle), MathF.Sin(fillAngle)) * R_Main;
            Color arcCol = GetArcColor(ratio);
            float flutter = 0.7f + MathF.Sin(capFlutter) * 0.3f;

            //外辉光
            float glowR = active ? 7f : 5f;
            sb.Draw(px, new Rectangle((int)(capPos.X - glowR), (int)(capPos.Y - glowR),
                (int)(glowR * 2), (int)(glowR * 2)), arcCol * (0.15f * flutter));
            //内核
            float coreR = active ? 3f : 2f;
            sb.Draw(px, new Rectangle((int)(capPos.X - coreR), (int)(capPos.Y - coreR),
                (int)(coreR * 2), (int)(coreR * 2)), Color.White * (0.9f * flutter));
        }

        //32个刻度标记
        private void DrawTickMarks(SpriteBatch sb, Texture2D px, Vector2 c, float ratio, bool active) {
            float fillAngle = GetFillAngle(ratio);
            for (int i = 0; i < TickCount; i++) {
                float angle = ArcOrigin + (float)i / TickCount * MathHelper.TwoPi;
                Vector2 inner = c + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * R_TickIn;
                Vector2 outer = c + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * R_TickOut;
                bool isMajor = i % 8 == 0;
                float thick = isMajor ? 1.8f : 0.8f;

                //判断刻度是否在填充范围内
                float normalAngle = NormalizeAngle(angle - ArcOrigin);
                float normalFill = NormalizeAngle(fillAngle - ArcOrigin);
                bool inFill = normalAngle <= normalFill + 0.01f;

                Color tickCol;
                if (inFill) {
                    Color arc = GetArcColor(ratio);
                    float proximity = 1f - Math.Clamp(Math.Abs(normalAngle - normalFill) / 0.3f, 0, 1);
                    tickCol = Color.Lerp(arc * 0.7f, Color.White, proximity * 0.4f);
                    if (active) tickCol *= 0.7f + MathF.Sin(activePulse + i * 0.5f) * 0.3f;
                }
                else {
                    tickCol = CyanDim * 0.3f;
                }

                DrawLine(sb, px, inner, outer, thick, tickCol);
            }
        }

        //旋转扫描线（带拖尾渐隐）
        private void DrawScanSweep(SpriteBatch sb, Texture2D px, Vector2 c, bool active) {
            float alpha = active ? 0.45f : 0.1f;
            int tailCount = active ? 6 : 2;
            for (int t = 0; t <= tailCount; t++) {
                float a = scanAngle - t * 0.04f;
                Vector2 dir = new(MathF.Cos(a), MathF.Sin(a));
                Vector2 inner = c + dir * (R_Main - 10);
                Vector2 outer = c + dir * (R_TickOut + 3);
                float fade = 1f - (float)t / (tailCount + 1);
                DrawLine(sb, px, inner, outer, 1f, CyanHi * (alpha * fade * fade));
            }
        }

        //神经脉冲：激活/停用瞬间从中心放射的线条
        private void DrawNeuralPulse(SpriteBatch sb, Texture2D px, Vector2 c) {
            if (neuralBurst < 0.02f) return;
            int rays = 10;
            float maxLen = 35f * neuralBurst;
            for (int i = 0; i < rays; i++) {
                float angle = (float)i / rays * MathHelper.TwoPi + timer * 0.3f;
                Vector2 dir = new(MathF.Cos(angle), MathF.Sin(angle));
                Vector2 start = c + dir * 12f;
                Vector2 end = c + dir * (12f + maxLen);
                float alpha = neuralBurst * (0.4f + MathF.Sin(i * 1.7f) * 0.2f);
                DrawLine(sb, px, start, end, 1.2f, CyanHi * alpha);
            }
        }

        //故障碎片绘制
        private void DrawGlitchFragments(SpriteBatch sb, Texture2D px, Vector2 c) {
            foreach (var g in glitches) {
                float a = g.Life / g.MaxLife;
                Vector2 pos = c + g.Pos;
                //整数化位置模拟像素感
                Rectangle rect = new((int)pos.X, (int)pos.Y, (int)g.W, (int)g.H);
                sb.Draw(px, rect, g.Tint * (a * 0.5f));
            }
        }

        //轨道数据粒子
        private void DrawDataParticles(SpriteBatch sb, Texture2D px, Vector2 c) {
            foreach (var d in dots) {
                Vector2 pos = c + new Vector2(MathF.Cos(d.Angle), MathF.Sin(d.Angle)) * d.Radius;
                int sz = Math.Max(1, (int)d.Size);
                sb.Draw(px, new Rectangle((int)pos.X, (int)pos.Y, sz, sz), CyanHi * d.Alpha);
            }
        }

        //中心文本：百分比 + 状态
        private void DrawCenterContent(SpriteBatch sb, DynamicSpriteFont font, Vector2 c, float ratio, bool active) {
            //百分比数字
            int pct = (int)(ratio * 100);
            string pctStr = pct.ToString();
            float pctScale = 0.8f;
            Vector2 pctSize = font.MeasureString(pctStr) * pctScale;
            Vector2 pctPos = c - pctSize * 0.5f + new Vector2(0, -8);

            Color pctColor = active ? Color.Lerp(GetArcColor(ratio), Color.White, 0.3f) : GetArcColor(ratio);

            //色差偏移文字
            if (active || transitionFlash > 0.05f) {
                float shift = active ? 1.2f : transitionFlash * 2f;
                Utils.DrawBorderString(sb, pctStr, pctPos + new Vector2(-shift, 0), new Color(255, 30, 30) * 0.3f, pctScale);
                Utils.DrawBorderString(sb, pctStr, pctPos + new Vector2(shift, 0), new Color(30, 60, 255) * 0.3f, pctScale);
            }
            Utils.DrawBorderString(sb, pctStr, pctPos, pctColor, pctScale);

            //百分号（略小，偏右）
            float symScale = 0.45f;
            Vector2 symPos = pctPos + new Vector2(pctSize.X + 2, pctSize.Y * 0.2f);
            Utils.DrawBorderString(sb, "%", symPos, pctColor * 0.7f, symScale);

            //状态行
            string statusStr;
            Color statusCol;
            if (active) {
                statusStr = "ACTIVE";
                float flicker = MathF.Sin(timer * 18f) > 0 ? 1f : 0.5f;
                statusCol = CyanHi * flicker;
            }
            else if (ratio >= 0.99f) {
                statusStr = "READY";
                statusCol = new Color(0, 220, 80);
            }
            else {
                statusStr = "CHARGING";
                statusCol = YellowMid * 0.8f;
            }
            float stScale = 0.38f;
            Vector2 stSize = font.MeasureString(statusStr) * stScale;
            Vector2 stPos = c - stSize * 0.5f + new Vector2(0, 14);
            Utils.DrawBorderString(sb, statusStr, stPos, statusCol, stScale);
        }

        //电路走线装饰
        private void DrawCircuitTraces(SpriteBatch sb, Texture2D px, Vector2 c, bool active) {
            Color traceCol = active ? CyanHi * 0.18f : CyanDim * 0.3f;
            float thick = 1f;

            //右上走线
            Vector2 a1 = c + new Vector2(R_TickOut + 4, -20);
            Vector2 b1 = a1 + new Vector2(18, 0);
            Vector2 c1 = b1 + new Vector2(0, -10);
            DrawLine(sb, px, a1, b1, thick, traceCol);
            DrawLine(sb, px, b1, c1, thick, traceCol);
            sb.Draw(px, new Rectangle((int)c1.X - 1, (int)c1.Y - 1, 3, 3), traceCol * 2f);

            //左下走线
            Vector2 a2 = c + new Vector2(-R_TickOut - 4, 15);
            Vector2 b2 = a2 + new Vector2(-14, 0);
            Vector2 c2 = b2 + new Vector2(0, 12);
            DrawLine(sb, px, a2, b2, thick, traceCol);
            DrawLine(sb, px, b2, c2, thick, traceCol);
            sb.Draw(px, new Rectangle((int)c2.X - 1, (int)c2.Y - 1, 3, 3), traceCol * 2f);

            //下方走线
            Vector2 a3 = c + new Vector2(10, R_TickOut + 3);
            Vector2 b3 = a3 + new Vector2(0, 10);
            Vector2 c3 = b3 + new Vector2(12, 0);
            DrawLine(sb, px, a3, b3, thick, traceCol);
            DrawLine(sb, px, b3, c3, thick, traceCol);
            sb.Draw(px, new Rectangle((int)c3.X - 1, (int)c3.Y - 1, 3, 3), traceCol * 2f);
        }

        //激活/停用闪光叠层
        private void DrawTransitionFlash(SpriteBatch sb, Texture2D px, Vector2 c) {
            if (transitionFlash < 0.01f) return;
            float r = R_TickOut + 10;
            Rectangle rect = new((int)(c.X - r), (int)(c.Y - r), (int)(r * 2), (int)(r * 2));
            sb.Draw(px, rect, Color.White * (transitionFlash * 0.15f));
        }

        #endregion

        #region 工具方法

        //绘制弧线：用连续短线段近似
        private static void DrawArc(SpriteBatch sb, Texture2D px, Vector2 center,
            float radius, float startAngle, float sweepAngle, float thickness, Color color,
            int segments, Vector2 offset = default) {
            if (segments < 1 || sweepAngle <= 0) return;
            float step = sweepAngle / segments;
            Vector2 prev = center + offset + new Vector2(MathF.Cos(startAngle), MathF.Sin(startAngle)) * radius;
            for (int i = 1; i <= segments; i++) {
                float angle = startAngle + step * i;
                Vector2 cur = center + offset + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                DrawLine(sb, px, prev, cur, thickness, color);
                prev = cur;
            }
        }

        private static void DrawLine(SpriteBatch sb, Texture2D px, Vector2 start, Vector2 end, float thickness, Color color) {
            Vector2 diff = end - start;
            float length = diff.Length();
            if (length < 0.5f) return;
            sb.Draw(px, start, new Rectangle(0, 0, 1, 1), color, diff.ToRotation(),
                new Vector2(0, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        //根据充能比例返回弧线颜色
        private static Color GetArcColor(float ratio) {
            if (ratio > 0.5f) return CyanHi;
            if (ratio > 0.25f) {
                float t = (ratio - 0.25f) / 0.25f;
                return Color.Lerp(YellowMid, CyanHi, t);
            }
            float t2 = ratio / 0.25f;
            return Color.Lerp(RedHi, YellowMid, t2);
        }

        //获取当前填充比例对应的绝对角度
        private static float GetFillAngle(float ratio) {
            float totalFill = ratio * SegAngle * SegCount;
            float angle = ArcOrigin;
            float remain = totalFill;
            for (int s = 0; s < SegCount; s++) {
                if (remain <= 0) break;
                float fill = MathF.Min(remain, SegAngle);
                angle = ArcOrigin + s * (SegAngle + GapRad) + fill;
                remain -= SegAngle;
            }
            return angle;
        }

        //将角度归一化到 [0, TwoPi)
        private static float NormalizeAngle(float a) {
            a %= MathHelper.TwoPi;
            if (a < 0) a += MathHelper.TwoPi;
            return a;
        }

        #endregion
    }
}
