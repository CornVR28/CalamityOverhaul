using CalamityOverhaul.Common;
using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.UI
{
    /// <summary>
    /// SHPC 三级模块面板
    /// 列出玩家背包中与给定槽位类别匹配的改件，支持装备/卸载与自定义悬浮描述
    /// 由 <see cref="SHPCUI"/> 在玩家点击 <see cref="SHPCModPanel"/> 槽位后驱动渲染
    /// </summary>
    internal static class SHPCModuleSelectPanel
    {
        private const float Scale = 1f;
        private const float FontScale = 1.2f;

        public const float PanelW = 300f * Scale;
        public const float PanelH = 260f * Scale;

        //顶部标题区高度
        private const float HeaderH = 38f * Scale;
        //每个改件行高度
        private const float RowH = 34f * Scale;
        //行内左侧图标尺寸
        private const float IconSize = 26f * Scale;
        //行内边距
        private const float RowPadX = 8f * Scale;
        //底部按钮区高度
        private const float FooterH = 28f * Scale;

        public enum HitKind
        {
            None,
            Row,
            Unequip,
            Close,
        }

        public struct Layout
        {
            public Rectangle Panel;
            public Rectangle ListArea;
            public Rectangle UnequipBtn;
            public Rectangle CloseBtn;
            //当前命中的行索引（-1 表示未命中）
            public int HoveredRow;
        }

        /// <summary>
        /// 用于从 Layout 中得知当前候选物品列表（每帧重建以追踪背包变化）
        /// </summary>
        private static readonly List<Item> _candidates = new();

        public static IReadOnlyList<Item> CurrentCandidates => _candidates;

        public static void RefreshCandidates(SHPCSlotCategory category) {
            _candidates.Clear();
            Player p = Main.LocalPlayer;
            if (p == null || !p.active) {
                return;
            }
            for (int i = 0; i < Main.InventorySlotsTotal && i < p.inventory.Length; i++) {
                Item it = p.inventory[i];
                if (it == null || it.IsAir) {
                    continue;
                }
                if (it.ModItem is SHPCModuleItem mod && mod.SlotCategory == category) {
                    _candidates.Add(it);
                }
            }
        }

        public static Layout Compute(Vector2 anchor, float midAngle, float panelAlpha) {
            Vector2 outDir = SHPCRenderer.AngleDir(midAngle);
            float slide = (1f - panelAlpha) * 14f;
            Vector2 panelPos = anchor + outDir * (SHPCTheme.InfoPanelGap + slide);
            panelPos.Y -= PanelH * 0.5f;
            Rectangle panel = new((int)panelPos.X, (int)panelPos.Y, (int)PanelW, (int)PanelH);

            Rectangle list = new(panel.X + 6, panel.Y + (int)HeaderH,
                panel.Width - 12, panel.Height - (int)HeaderH - (int)FooterH - 4);

            int btnW = (panel.Width - 18) / 2;
            Rectangle unequip = new(panel.X + 6,
                panel.Bottom - (int)FooterH + 2, btnW, (int)FooterH - 6);
            Rectangle close = new(panel.X + panel.Width - 6 - btnW,
                panel.Bottom - (int)FooterH + 2, btnW, (int)FooterH - 6);

            return new Layout {
                Panel = panel,
                ListArea = list,
                UnequipBtn = unequip,
                CloseBtn = close,
                HoveredRow = -1,
            };
        }

        public static HitKind HitTest(ref Layout layout, Vector2 mouse, int rowCount) {
            if (layout.CloseBtn.Contains((int)mouse.X, (int)mouse.Y)) {
                return HitKind.Close;
            }
            if (layout.UnequipBtn.Contains((int)mouse.X, (int)mouse.Y)) {
                return HitKind.Unequip;
            }
            if (layout.ListArea.Contains((int)mouse.X, (int)mouse.Y)) {
                int rel = (int)mouse.Y - layout.ListArea.Y;
                int idx = rel / (int)RowH;
                if (idx >= 0 && idx < rowCount) {
                    layout.HoveredRow = idx;
                    return HitKind.Row;
                }
            }
            return HitKind.None;
        }

        public static void Draw(SpriteBatch sb, Texture2D px, in Layout layout,
            float panelAlpha, float globalAlpha, HitKind hover, int slotIdx, Item equipped) {
            if (panelAlpha < 0.02f) {
                return;
            }
            float a = panelAlpha * globalAlpha;
            Rectangle rect = layout.Panel;

            //投影
            SHPCRenderer.DrawFilledRect(sb, px,
                new Rectangle(rect.X + 3, rect.Y + 4, rect.Width, rect.Height),
                new Color(0, 0, 0) * (0.55f * a));

            //背景
            SHPCRenderer.DrawFilledRect(sb, px, rect, new Color(4, 14, 22) * (0.96f * a));

            //外框与四角L形装饰
            SHPCRenderer.DrawRectStroke(sb, px, rect, 1.2f, SHPCTheme.Border * (0.9f * a));
            SHPCRenderer.DrawCornerBrackets(sb, px, rect, 10f * Scale, 1.5f, SHPCTheme.BorderHi * (0.9f * a));

            //顶部色带
            SHPCRenderer.DrawFilledRect(sb, px,
                new Rectangle(rect.X, rect.Y, rect.Width, (int)(3 * Scale)),
                SHPCTheme.Cyan * (0.85f * a));

            DynamicSpriteFont font = FontAssets.MouseText.Value;

            //标题
            string slotName = SlotLabel(slotIdx);
            Utils.DrawBorderString(sb, string.Format(SHPCUI.Modify_InstallSlot.Value, slotName),
                new Vector2(rect.X + 10f * Scale, rect.Y + 7f * Scale), SHPCTheme.Text * a, 0.62f * FontScale);
            string subtitle = equipped != null
                ? string.Format(SHPCUI.Modify_Current.Value, equipped.Name)
                : SHPCUI.Modify_SlotEmpty.Value;
            Utils.DrawBorderString(sb, subtitle,
                new Vector2(rect.X + 10f * Scale, rect.Y + 24f * Scale), SHPCTheme.TextDim * a, 0.40f * FontScale);

            //列表区背景
            SHPCRenderer.DrawFilledRect(sb, px, layout.ListArea,
                new Color(2, 8, 14) * (0.6f * a));
            SHPCRenderer.DrawRectStroke(sb, px, layout.ListArea, 1f, SHPCTheme.Border * (0.5f * a));

            //列表行
            int count = _candidates.Count;
            if (count == 0) {
                string empty = SHPCUI.Modify_NoMatch.Value;
                Vector2 sz = font.MeasureString(empty) * (0.45f * FontScale);
                Utils.DrawBorderString(sb, empty,
                    new Vector2(layout.ListArea.X + (layout.ListArea.Width - sz.X) * 0.5f,
                                layout.ListArea.Y + (layout.ListArea.Height - sz.Y) * 0.5f),
                    SHPCTheme.TextDim * a, 0.45f * FontScale);
            }
            else {
                for (int i = 0; i < count; i++) {
                    Rectangle row = new(layout.ListArea.X + 2,
                        layout.ListArea.Y + 2 + i * (int)RowH,
                        layout.ListArea.Width - 4, (int)RowH - 2);
                    if (row.Bottom > layout.ListArea.Bottom) break;

                    bool isHover = hover == HitKind.Row && layout.HoveredRow == i;
                    bool isEquipped = equipped != null && _candidates[i].type == equipped.type;

                    //背景
                    Color rowBg = isHover ? new Color(12, 50, 70) * (0.85f * a)
                                          : new Color(6, 20, 30) * (0.7f * a);
                    SHPCRenderer.DrawFilledRect(sb, px, row, rowBg);
                    Color rowBorder = isHover ? SHPCTheme.CyanHi * (0.9f * a)
                                              : SHPCTheme.Border * (0.55f * a);
                    SHPCRenderer.DrawRectStroke(sb, px, row, 1f, rowBorder);

                    //左侧色条
                    SHPCRenderer.DrawFilledRect(sb, px,
                        new Rectangle(row.X, row.Y, 3, row.Height),
                        (isEquipped ? SHPCTheme.Accent : (isHover ? SHPCTheme.Cyan : SHPCTheme.Border)) * (0.85f * a));

                    //图标
                    DrawItemIcon(sb, _candidates[i],
                        new Vector2(row.X + RowPadX + IconSize * 0.5f, row.Y + row.Height * 0.5f),
                        IconSize, a);

                    //名称
                    Vector2 textPos = new(row.X + RowPadX + IconSize + 6f * Scale,
                        row.Y + (row.Height - font.LineSpacing * 0.5f) * 0.5f);
                    Color nameCol = (isEquipped ? SHPCTheme.Accent
                        : (isHover ? SHPCTheme.Text : SHPCTheme.TextDim)) * a;
                    Utils.DrawBorderString(sb, _candidates[i].Name, textPos, nameCol, 0.5f * FontScale);

                    //右侧标记
                    if (isEquipped) {
                        string mark = SHPCUI.Modify_Equipped.Value;
                        Vector2 ms = font.MeasureString(mark) * (0.42f * FontScale);
                        Utils.DrawBorderString(sb, mark,
                            new Vector2(row.Right - 6f * Scale - ms.X, textPos.Y),
                            SHPCTheme.Accent * a, 0.42f * FontScale);
                    }
                }
            }

            //底部按钮
            DrawSmallButton(sb, px, font, layout.UnequipBtn,
                SHPCUI.Modify_Unequip.Value, hover == HitKind.Unequip, equipped != null, a);
            DrawSmallButton(sb, px, font, layout.CloseBtn,
                SHPCUI.Modify_Close.Value, hover == HitKind.Close, true, a);

            //悬浮 tooltip
            if (hover == HitKind.Row && layout.HoveredRow >= 0 && layout.HoveredRow < count) {
                DrawCustomTooltip(sb, px, font, _candidates[layout.HoveredRow], a);
            }
        }

        private static void DrawSmallButton(SpriteBatch sb, Texture2D px, DynamicSpriteFont font,
            Rectangle r, string label, bool isHover, bool enabled, float a) {
            Color bg = !enabled ? new Color(20, 20, 20) * (0.6f * a)
                : isHover ? new Color(18, 60, 80) * (0.9f * a)
                          : new Color(8, 26, 36) * (0.85f * a);
            SHPCRenderer.DrawFilledRect(sb, px, r, bg);
            Color border = !enabled ? SHPCTheme.Disabled * (0.7f * a)
                : isHover ? SHPCTheme.CyanHi * (0.95f * a)
                          : SHPCTheme.Border * (0.7f * a);
            SHPCRenderer.DrawRectStroke(sb, px, r, 1f, border);

            float scale = 0.45f * FontScale;
            Vector2 sz = font.MeasureString(label) * scale;
            Color textCol = !enabled ? SHPCTheme.Disabled * a
                : (isHover ? SHPCTheme.Text : SHPCTheme.TextDim) * a;
            Utils.DrawBorderString(sb, label,
                new Vector2(r.X + (r.Width - sz.X) * 0.5f, r.Y + (r.Height - sz.Y) * 0.5f),
                textCol, scale);
        }

        private static void DrawItemIcon(SpriteBatch sb, Item item, Vector2 center, float maxSize, float a) {
            if (item == null || item.IsAir) {
                return;
            }
            Main.instance.LoadItem(item.type);
            Texture2D tex = TextureAssets.Item[item.type]?.Value;
            if (tex == null) {
                return;
            }
            //适配贴图最大尺寸
            float tw = tex.Width;
            float th = tex.Height;
            Rectangle frame = Main.itemAnimations[item.type] != null
                ? Main.itemAnimations[item.type].GetFrame(tex)
                : tex.Bounds;
            tw = frame.Width; th = frame.Height;
            float scale = MathF.Min(maxSize / tw, maxSize / th);
            if (scale > 1f) scale = 1f;

            //改件物品额外走赛博朋克滤镜，使背包列表中也能按色调区分
            if (item.ModItem is SHPCModuleItem mod
                && SHPCModuleRender.Begin(sb, mod.TintColor,
                    new Vector2(tex.Width, tex.Height), Main.UIScaleMatrix, mod.TintIntensity)) {
                sb.Draw(tex, center, frame, Color.White * a, 0f,
                    new Vector2(tw * 0.5f, th * 0.5f), scale, SpriteEffects.None, 0f);
                SHPCModuleRender.End(sb);
            }
            else {
                sb.Draw(tex, center, frame, Color.White * a, 0f,
                    new Vector2(tw * 0.5f, th * 0.5f), scale, SpriteEffects.None, 0f);
            }
        }

        private static void DrawCustomTooltip(SpriteBatch sb, Texture2D px, DynamicSpriteFont font,
            Item item, float a) {
            //收集物品名 + 普通 tooltip + 改件属性差值
            List<string> lines = new();
            List<Color> colors = new();

            lines.Add(item.Name);
            colors.Add(SHPCTheme.Text);

            string tip = item.ToolTip?.GetLine(0);
            if (!string.IsNullOrEmpty(tip)) {
                int n = item.ToolTip.Lines;
                for (int i = 0; i < n; i++) {
                    string ln = item.ToolTip.GetLine(i);
                    if (!string.IsNullOrWhiteSpace(ln)) {
                        lines.Add(ln);
                        colors.Add(SHPCTheme.TextDim);
                    }
                }
            }

            if (item.ModItem is SHPCModuleItem mod) {
                foreach (string ln in mod.GetStatLines()) {
                    if (string.IsNullOrEmpty(ln)) continue;
                    lines.Add(ln);
                    //正负号决定颜色
                    Color c = ln.StartsWith("-") ? new Color(255, 120, 110) : new Color(120, 255, 170);
                    colors.Add(c);
                }
            }

            //计算尺寸
            float scale = 0.45f * FontScale;
            float maxW = 0f;
            float totalH = 0f;
            float lineH = font.LineSpacing * scale;
            for (int i = 0; i < lines.Count; i++) {
                Vector2 sz = font.MeasureString(lines[i]) * scale;
                if (sz.X > maxW) maxW = sz.X;
                totalH += lineH;
            }
            const float padX = 8f;
            const float padY = 6f;
            Vector2 mouse = Main.MouseScreen;
            Rectangle box = new((int)(mouse.X + 16f), (int)(mouse.Y + 16f),
                (int)(maxW + padX * 2), (int)(totalH + padY * 2));
            //避免越界
            if (box.Right > Main.screenWidth) box.X = Main.screenWidth - box.Width - 4;
            if (box.Bottom > Main.screenHeight) box.Y = Main.screenHeight - box.Height - 4;

            //背景
            SHPCRenderer.DrawFilledRect(sb, px,
                new Rectangle(box.X + 3, box.Y + 4, box.Width, box.Height),
                new Color(0, 0, 0) * (0.6f * a));
            SHPCRenderer.DrawFilledRect(sb, px, box, new Color(4, 14, 22) * (0.96f * a));
            SHPCRenderer.DrawRectStroke(sb, px, box, 1.2f, SHPCTheme.Border * (0.9f * a));
            SHPCRenderer.DrawCornerBrackets(sb, px, box, 6f, 1.2f, SHPCTheme.BorderHi * (0.9f * a));

            //文字
            float y = box.Y + padY;
            for (int i = 0; i < lines.Count; i++) {
                Utils.DrawBorderString(sb, lines[i],
                    new Vector2(box.X + padX, y), colors[i] * a, scale);
                y += lineH;
            }
        }

        private static string SlotLabel(int idx) {
            return idx switch {
                0 => SHPCUI.Modify_Slot_Barrel.Value,
                1 => SHPCUI.Modify_Slot_Optic.Value,
                2 => SHPCUI.Modify_Slot_Power.Value,
                3 => SHPCUI.Modify_Slot_Stock.Value,
                4 => SHPCUI.Modify_Slot_Grip.Value,
                5 => SHPCUI.Modify_Slot_Frame.Value,
                _ => "?",
            };
        }
    }
}
