using CalamityOverhaul.Common;
using CalamityOverhaul.Content.HackTimes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.SignalTowers.Decryption
{
    /// <summary>
    /// BreachMinigame的绘制层：在主体Body矩形内划分左矩阵/右面板两列
    /// 不持有状态，所有状态来自传入的BreachMinigame实例
    /// </summary>
    internal static class BreachMinigameRenderer
    {
        public static void Draw(SpriteBatch sb, BreachMinigame game, Rectangle body, float eased, Color accent) {
            if (game == null) return;

            //整体分两列：左70%矩阵 + 右30%目标列表/缓冲区/计时
            int leftW = (int)(body.Width * 0.62f);
            Rectangle leftArea = new Rectangle(body.X + 4, body.Y + 4, leftW, body.Height - 8);
            Rectangle rightArea = new Rectangle(leftArea.Right + 10, body.Y + 4,
                body.Width - leftW - 18, body.Height - 8);

            DrawMatrixPane(sb, game, leftArea, eased, accent);
            DrawInfoPane(sb, game, rightArea, eased, accent);
            DrawCooldownOverlay(sb, game, body, eased);
        }

        //═══════════════════════════════════════════════════════════
        // 矩阵面板
        //═══════════════════════════════════════════════════════════
        private static void DrawMatrixPane(SpriteBatch sb, BreachMinigame game, Rectangle area, float eased, Color accent) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;

            //标题条
            string title = "CODE MATRIX";
            Utils.DrawBorderString(sb, title, new Vector2(area.X + 6, area.Y + 4),
                accent * (eased * 0.95f), 0.62f);

            //轴约束提示
            string axis = game.AxisLock == 0
                ? BreachMinigame.LabelAxisRow.Format(game.AxisIndex + 1)
                : BreachMinigame.LabelAxisCol.Format(game.AxisIndex + 1);
            Vector2 axSize = font.MeasureString(axis) * 0.55f;
            Utils.DrawBorderString(sb, axis,
                new Vector2(area.Right - axSize.X - 6, area.Y + 6),
                new Color(180, 220, 255) * (eased * 0.9f), 0.55f);

            //网格区域
            int n = BreachMinigame.MatrixSize;
            Rectangle inner = new Rectangle(area.X + 6, area.Y + 26, area.Width - 12, area.Height - 32);
            //计算正方形单元格尺寸
            int gap = 4;
            int cellSize = Math.Min((inner.Width - gap * (n - 1)) / n,
                (inner.Height - gap * (n - 1)) / n);
            if (cellSize < 10) cellSize = 10;
            int totalW = cellSize * n + gap * (n - 1);
            int totalH = totalW;
            Rectangle cellArea = new Rectangle(
                inner.X + (inner.Width - totalW) / 2,
                inner.Y + (inner.Height - totalH) / 2,
                totalW, totalH);

            //背景浅色带
            sb.Draw(px, cellArea, Color.Black * (0.22f * eased));

            //轴约束条 + 最近点击行列双梁，均用着色器渲染以获得流光质感
            DrawAxisHighlightBeams(sb, game, cellArea, cellSize, gap, eased, accent);

            //处理输入（仅Breaching且面板完全展开时）
            if (eased > 0.85f && !game.HasSolved && game.Cooldown <= 0f) {
                game.HandleMatrixInput(cellArea, cellSize, gap);
            }

            //绘制单元格
            for (int r = 0; r < n; r++) {
                for (int c = 0; c < n; c++) {
                    int x = cellArea.X + c * (cellSize + gap);
                    int y = cellArea.Y + r * (cellSize + gap);
                    var rc = new Rectangle(x, y, cellSize, cellSize);
                    bool taken = game.Taken[r, c];
                    bool hover = game.HoverCell.HasValue && game.HoverCell.Value == (r, c);
                    bool canSel = game.CanSelect(r, c);

                    Color bg = taken
                        ? Color.Black * (0.75f * eased)
                        : (canSel ? (accent * 0.18f) : Color.Black * (0.4f * eased));
                    if (hover && canSel) bg = accent * 0.38f;
                    sb.Draw(px, rc, bg);

                    //边框
                    Color border = taken
                        ? new Color(120, 120, 120) * (eased * 0.4f)
                        : canSel ? accent * (eased * 0.9f) : accent * (eased * 0.35f);
                    if (hover && canSel) border = accent * eased;
                    sb.Draw(px, new Rectangle(rc.X, rc.Y, rc.Width, 1), border);
                    sb.Draw(px, new Rectangle(rc.X, rc.Bottom - 1, rc.Width, 1), border);
                    sb.Draw(px, new Rectangle(rc.X, rc.Y, 1, rc.Height), border);
                    sb.Draw(px, new Rectangle(rc.Right - 1, rc.Y, 1, rc.Height), border);

                    //值
                    byte val = game.Matrix[r, c];
                    string sVal = val.ToString("X2");
                    float scale = cellSize / 36f * 0.9f;
                    Vector2 ms = font.MeasureString(sVal) * scale;
                    Vector2 tp = new Vector2(rc.X + (rc.Width - ms.X) / 2, rc.Y + (rc.Height - ms.Y) / 2);
                    Color tc = taken
                        ? new Color(120, 140, 150) * (eased * 0.55f)
                        : canSel ? new Color(250, 235, 200) * eased : HackTheme.TextNormal * (eased * 0.7f);
                    if (hover && canSel) tc = new Color(255, 255, 255) * eased;
                    Utils.DrawBorderString(sb, sVal, tp, tc, scale);
                }
            }
        }

        //═══════════════════════════════════════════════════════════
        // 轴约束 & 最近点击 的行列高亮（Shader驱动，干净的蓝色科技色带）
        //═══════════════════════════════════════════════════════════
        private static void DrawAxisHighlightBeams(SpriteBatch sb, BreachMinigame game,
            Rectangle cellArea, int cellSize, int gap, float eased, Color accent) {
            Effect shader = EffectLoader.BreachMatrixAxisHighlight?.Value;
            if (shader == null) return;
            Texture2D px = TextureAssets.MagicPixel.Value;

            //科技蓝统一色：不跟随阶段accent，避免在橙色阶段变得刺眼
            Color beamCol = new Color(130, 205, 255);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearClamp, DepthStencilState.None,
                RasterizerState.CullNone, shader, Main.UIScaleMatrix);

            //① 轴约束条：恒常存在的弱面光，表达“下一步允许的方向”
            bool canPlay = !game.HasSolved && game.Cooldown <= 0f;
            if (canPlay) {
                DrawBeam(sb, shader, px, cellArea, cellSize, gap,
                    game.AxisLock == 0, game.AxisIndex,
                    beamCol, intensity: 0.75f * eased, coreWeight: 0.7f);
            }

            //② 点击反馈：最近一次点击行列交叉的高亮带，强度大但快速淡出
            if (game.LastSelRow >= 0 && game.SelectionPulseTime < BreachMinigame.SelectionPulseDuration) {
                float t = MathHelper.Clamp(game.SelectionPulseTime / BreachMinigame.SelectionPulseDuration, 0f, 1f);
                float strength = (1f - t) * (1f - t);
                DrawBeam(sb, shader, px, cellArea, cellSize, gap,
                    horizontal: true, index: game.LastSelRow,
                    beamCol, intensity: strength * eased * 1.3f, coreWeight: 1f);
                DrawBeam(sb, shader, px, cellArea, cellSize, gap,
                    horizontal: false, index: game.LastSelCol,
                    beamCol, intensity: strength * eased * 1.3f, coreWeight: 1f);
            }

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.UIScaleMatrix);

            //③ 被选中单元格的外框辉光：不使用着色器，纯加色矩形拼接，更锐利地呼应图二风格
            DrawSelectedCellGlow(sb, game, cellArea, cellSize, gap, eased, beamCol);
        }

        private static void DrawBeam(SpriteBatch sb, Effect shader, Texture2D px,
            Rectangle cellArea, int cellSize, int gap,
            bool horizontal, int index,
            Color color, float intensity, float coreWeight) {
            if (intensity <= 0.001f) return;
            Rectangle rc;
            if (horizontal) {
                int y = cellArea.Y + index * (cellSize + gap);
                rc = new Rectangle(cellArea.X - 4, y - 2, cellArea.Width + 8, cellSize + 4);
            }
            else {
                int x = cellArea.X + index * (cellSize + gap);
                rc = new Rectangle(x - 2, cellArea.Y - 4, cellSize + 4, cellArea.Height + 8);
            }
            shader.Parameters["uTime"]?.SetValue(DecryptionSession.SessionTime);
            shader.Parameters["uIntensity"]?.SetValue(intensity);
            shader.Parameters["uColor"]?.SetValue(color.ToVector4());
            shader.Parameters["uResolution"]?.SetValue(new Vector2(rc.Width, rc.Height));
            shader.Parameters["uOrientation"]?.SetValue(horizontal ? 0f : 1f);
            shader.Parameters["uCoreWeight"]?.SetValue(coreWeight);
            shader.CurrentTechnique.Passes[0].Apply();
            sb.Draw(px, rc, Color.White);
        }

        /// <summary>被选中字节块自身的外框辉光：锐利的内描边 + 四角L形标记 + 外扩柔光，随SelectionPulseTime淡出</summary>
        private static void DrawSelectedCellGlow(SpriteBatch sb, BreachMinigame game,
            Rectangle cellArea, int cellSize, int gap, float eased, Color beamCol) {
            if (game.LastSelRow < 0) return;
            float t = MathHelper.Clamp(game.SelectionPulseTime / BreachMinigame.SelectionPulseDuration, 0f, 1f);
            //前半段强亮、后半段缓落
            float strength = MathHelper.Clamp(1f - t * 0.65f, 0f, 1f);
            float pulseAlpha = strength * eased;
            Texture2D px = TextureAssets.MagicPixel.Value;
            int x = cellArea.X + game.LastSelCol * (cellSize + gap);
            int y = cellArea.Y + game.LastSelRow * (cellSize + gap);
            Rectangle cell = new Rectangle(x, y, cellSize, cellSize);

            //外扩柔光：连续几层递减Alpha的矩形向外扩张，近似halo
            for (int k = 1; k <= 4; k++) {
                float falloff = 1f - k / 5f;
                float a = pulseAlpha * 0.22f * falloff;
                Rectangle halo = new Rectangle(cell.X - k * 2, cell.Y - k * 2,
                    cell.Width + k * 4, cell.Height + k * 4);
                sb.Draw(px, new Rectangle(halo.X, halo.Y, halo.Width, 1), beamCol * a);
                sb.Draw(px, new Rectangle(halo.X, halo.Bottom - 1, halo.Width, 1), beamCol * a);
                sb.Draw(px, new Rectangle(halo.X, halo.Y, 1, halo.Height), beamCol * a);
                sb.Draw(px, new Rectangle(halo.Right - 1, halo.Y, 1, halo.Height), beamCol * a);
            }

            //内描边：亮蓝色双层描边
            Color sharp = beamCol * pulseAlpha;
            Color inner = Color.Lerp(beamCol, Color.White, 0.35f) * pulseAlpha;
            sb.Draw(px, new Rectangle(cell.X - 1, cell.Y - 1, cell.Width + 2, 2), sharp);
            sb.Draw(px, new Rectangle(cell.X - 1, cell.Bottom - 1, cell.Width + 2, 2), sharp);
            sb.Draw(px, new Rectangle(cell.X - 1, cell.Y - 1, 2, cell.Height + 2), sharp);
            sb.Draw(px, new Rectangle(cell.Right - 1, cell.Y - 1, 2, cell.Height + 2), sharp);

            //四角L形标记：向外伸出，提高瞄准感
            int armLen = Math.Max(4, cellSize / 4);
            //左上
            sb.Draw(px, new Rectangle(cell.X - 3, cell.Y - 3, armLen, 2), inner);
            sb.Draw(px, new Rectangle(cell.X - 3, cell.Y - 3, 2, armLen), inner);
            //右上
            sb.Draw(px, new Rectangle(cell.Right - armLen + 3, cell.Y - 3, armLen, 2), inner);
            sb.Draw(px, new Rectangle(cell.Right + 1, cell.Y - 3, 2, armLen), inner);
            //左下
            sb.Draw(px, new Rectangle(cell.X - 3, cell.Bottom + 1, armLen, 2), inner);
            sb.Draw(px, new Rectangle(cell.X - 3, cell.Bottom - armLen + 3, 2, armLen), inner);
            //右下
            sb.Draw(px, new Rectangle(cell.Right - armLen + 3, cell.Bottom + 1, armLen, 2), inner);
            sb.Draw(px, new Rectangle(cell.Right + 1, cell.Bottom - armLen + 3, 2, armLen), inner);
        }

        //═══════════════════════════════════════════════════════════
        // 右侧信息面板（目标序列 + 缓冲区 + 时间 + 重试按钮）
        //═══════════════════════════════════════════════════════════
        private static void DrawInfoPane(SpriteBatch sb, BreachMinigame game, Rectangle area, float eased, Color accent) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            int y = area.Y;

            //===== 缓冲区 =====
            string bufTitle = BreachMinigame.LabelBuffer.Format(game.Buffer.Count, BreachMinigame.BufferCapacity);
            Utils.DrawBorderString(sb, bufTitle, new Vector2(area.X + 4, y), accent * (eased * 0.95f), 0.62f);
            y += 18;

            int slotW = (area.Width - 4) / BreachMinigame.BufferCapacity;
            int slotH = 26;
            for (int i = 0; i < BreachMinigame.BufferCapacity; i++) {
                Rectangle slot = new Rectangle(area.X + i * slotW, y, slotW - 2, slotH);
                sb.Draw(px, slot, Color.Black * (0.55f * eased));
                Color border = i < game.Buffer.Count ? accent * eased : accent * (eased * 0.3f);
                sb.Draw(px, new Rectangle(slot.X, slot.Y, slot.Width, 1), border);
                sb.Draw(px, new Rectangle(slot.X, slot.Bottom - 1, slot.Width, 1), border);
                if (i == game.Buffer.Count && game.Cooldown <= 0f && !game.HasSolved) {
                    //下一个空位闪烁边
                    float flash = 0.5f + MathF.Sin(DecryptionSession.SessionTime * 6f) * 0.5f;
                    sb.Draw(px, new Rectangle(slot.X, slot.Y, 1, slot.Height), accent * (eased * flash));
                    sb.Draw(px, new Rectangle(slot.Right - 1, slot.Y, 1, slot.Height), accent * (eased * flash));
                }
                if (i < game.Buffer.Count) {
                    string sv = game.Buffer[i].ToString("X2");
                    Vector2 ms = font.MeasureString(sv) * 0.6f;
                    Vector2 tp = new Vector2(slot.X + (slot.Width - ms.X) / 2, slot.Y + (slot.Height - ms.Y) / 2);
                    Utils.DrawBorderString(sb, sv, tp, new Color(255, 240, 200) * eased, 0.6f);
                }
            }
            y += slotH + 10;

            //===== 目标序列 =====
            Utils.DrawBorderString(sb, BreachMinigame.LabelTargets.Value,
                new Vector2(area.X + 4, y), accent * (eased * 0.95f), 0.62f);
            y += 18;

            foreach (var t in game.Targets) {
                //名字
                Utils.DrawBorderString(sb, t.Name, new Vector2(area.X + 6, y),
                    (t.Matched ? new Color(130, 255, 170) : t.TintColor) * (eased * 0.95f), 0.52f);
                //右侧对勾
                if (t.Matched) {
                    Utils.DrawBorderString(sb, "✓", new Vector2(area.Right - 16, y),
                        new Color(130, 255, 170) * eased, 0.7f);
                }
                y += 16;

                //bytes cells
                int bw = 26;
                int bh = 20;
                for (int i = 0; i < t.Sequence.Length; i++) {
                    Rectangle slot = new Rectangle(area.X + 6 + i * (bw + 3), y, bw, bh);
                    sb.Draw(px, slot, Color.Black * (0.55f * eased));
                    Color bc = (t.Matched ? new Color(130, 255, 170) : t.TintColor) * (eased * (t.Matched ? 1f : 0.75f));
                    sb.Draw(px, new Rectangle(slot.X, slot.Y, slot.Width, 1), bc);
                    sb.Draw(px, new Rectangle(slot.X, slot.Bottom - 1, slot.Width, 1), bc);
                    sb.Draw(px, new Rectangle(slot.X, slot.Y, 1, slot.Height), bc);
                    sb.Draw(px, new Rectangle(slot.Right - 1, slot.Y, 1, slot.Height), bc);
                    string sv = t.Sequence[i].ToString("X2");
                    Vector2 ms = font.MeasureString(sv) * 0.55f;
                    Vector2 tp = new Vector2(slot.X + (slot.Width - ms.X) / 2, slot.Y + (slot.Height - ms.Y) / 2);
                    Color tc = t.Matched ? new Color(220, 255, 230) * eased : new Color(240, 235, 220) * eased;
                    Utils.DrawBorderString(sb, sv, tp, tc, 0.55f);
                }
                y += bh + 8;
            }

            //===== 计时 / 冷却 =====
            int barY = area.Bottom - 56;
            //时间条
            string tLabel;
            float tFrac;
            Color barCol;
            if (game.Cooldown > 0f) {
                tLabel = BreachMinigame.LabelCooldown.Format(game.Cooldown);
                tFrac = 1f - (game.Cooldown / BreachMinigame.FailCooldownSeconds);
                barCol = HackTheme.Danger;
            }
            else if (game.HasSolved) {
                tLabel = "BREACHED";
                tFrac = 1f;
                barCol = new Color(120, 255, 170);
            }
            else {
                tLabel = BreachMinigame.LabelTimer.Value + $"  {game.TimeLeft:0.0}s";
                tFrac = game.TimeLeft / BreachMinigame.AttemptTimeSeconds;
                barCol = Color.Lerp(HackTheme.Danger, accent, MathHelper.Clamp(tFrac, 0f, 1f));
            }
            Utils.DrawBorderString(sb, tLabel, new Vector2(area.X + 4, barY),
                barCol * (eased * 0.95f), 0.6f);
            Rectangle bar = new Rectangle(area.X + 4, barY + 18, area.Width - 8, 6);
            sb.Draw(px, bar, Color.Black * (0.6f * eased));
            int fillW = (int)(bar.Width * MathHelper.Clamp(tFrac, 0f, 1f));
            sb.Draw(px, new Rectangle(bar.X, bar.Y, fillW, bar.Height), barCol * eased);
            sb.Draw(px, new Rectangle(bar.X, bar.Y, bar.Width, 1), barCol * (eased * 0.7f));
            sb.Draw(px, new Rectangle(bar.X, bar.Bottom - 1, bar.Width, 1), barCol * (eased * 0.7f));

            //===== 重试按钮（仅冷却结束且未解出时可用） =====
            string attempt = BreachMinigame.LabelAttempt.Format(game.AttemptCount);
            Utils.DrawBorderString(sb, attempt, new Vector2(area.X + 4, area.Bottom - 26),
                HackTheme.TextNormal * (eased * 0.85f), 0.55f);
            string restart = BreachMinigame.LabelRestart.Value;
            Vector2 rs = font.MeasureString(restart) * 0.6f;
            Rectangle btn = new Rectangle(area.Right - (int)rs.X - 16, area.Bottom - 28,
                (int)rs.X + 12, 22);
            bool over = btn.Contains(Main.mouseX, Main.mouseY);
            bool enabled = !game.HasSolved && game.Cooldown <= 0f;
            Color bCol = enabled ? (over ? accent * eased : accent * (eased * 0.75f)) : accent * (eased * 0.35f);
            sb.Draw(px, btn, Color.Black * (0.6f * eased));
            sb.Draw(px, new Rectangle(btn.X, btn.Y, btn.Width, 1), bCol);
            sb.Draw(px, new Rectangle(btn.X, btn.Bottom - 1, btn.Width, 1), bCol);
            Utils.DrawBorderString(sb, restart,
                new Vector2(btn.X + 6, btn.Y + (btn.Height - rs.Y) / 2),
                bCol, 0.6f);
            if (enabled && over && Main.mouseLeft && Main.mouseLeftRelease && eased > 0.85f) {
                game.ResetAttempt();
                Main.mouseLeftRelease = false;
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuClose with { Pitch = 0.3f });
            }
        }

        //═══════════════════════════════════════════════════════════
        // 冷却覆盖层：提示玩家协议正在重置
        //═══════════════════════════════════════════════════════════
        private static void DrawCooldownOverlay(SpriteBatch sb, BreachMinigame game, Rectangle body, float eased) {
            if (game.Cooldown <= 0f) return;
            Texture2D px = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.MouseText.Value;

            float t = game.Cooldown / BreachMinigame.FailCooldownSeconds;
            sb.Draw(px, body, Color.Black * (0.45f * eased * t));

            string msg = $"CONNECTION RESET  {game.Cooldown:0.0}s";
            float scale = 1.1f;
            Vector2 ms = font.MeasureString(msg) * scale;
            Vector2 pos = new Vector2(body.X + (body.Width - ms.X) / 2, body.Y + (body.Height - ms.Y) / 2);
            //故障色散
            float aber = 2f + MathF.Sin(DecryptionSession.SessionTime * 22f) * 1.6f;
            Utils.DrawBorderStringFourWay(sb, font, msg, pos.X - aber, pos.Y,
                new Color(255, 70, 90) * (eased * 0.7f), Color.Black * eased, Vector2.Zero, scale);
            Utils.DrawBorderStringFourWay(sb, font, msg, pos.X + aber, pos.Y + 0.6f,
                new Color(70, 170, 255) * (eased * 0.7f), Color.Black * eased, Vector2.Zero, scale);
            Utils.DrawBorderStringFourWay(sb, font, msg, pos.X, pos.Y,
                HackTheme.Danger * eased, Color.Black * eased, Vector2.Zero, scale);
        }
    }
}
