using CalamityOverhaul.Common;
using InnoVault;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityOverhaul.Content.ADV.EntrustManager
{
    internal class EntrustManagerLead : ModSystem
    {
        private enum LeadPhase
        {
            Inactive,
            KeyPrompt,
            PanelIntro,
            Complete
        }

        private static LeadPhase currentPhase = LeadPhase.Inactive;
        private static float animProgress = 0f;

        //阶段1卡片尺寸
        private const int CardW1 = 310;
        private const int CardH1 = 76;
        //阶段2卡片尺寸
        private const int CardW2 = 300;
        private const int CardH2 = 100;

        private const float AnimSpeed = 0.12f;

        public override void OnWorldUnload() {
            currentPhase = LeadPhase.Inactive;
            animProgress = 0f;
        }

        public override void UpdateUI(GameTime gameTime) {
            if (Main.gameMenu) return;

            var ui = QuestManagerUI.Instance;
            if (ui == null) return;

            switch (currentPhase) {
                case LeadPhase.Inactive:
                    //检测是否有委托且玩家从未见过引导
                    if (ui.HasAnyEntry && Main.LocalPlayer.TryGetADVSave(out var save)
                        && !save.Get<EntrustGuideModule>().GuideSeen) {
                        currentPhase = LeadPhase.KeyPrompt;
                        animProgress = 0f;
                    }
                    break;

                case LeadPhase.KeyPrompt:
                    animProgress = MathHelper.Lerp(animProgress, 1f, AnimSpeed);
                    //检测玩家打开了委托面板则进入第二阶段
                    if (ui.IsOpen) {
                        currentPhase = LeadPhase.PanelIntro;
                        animProgress = 0f;
                    }
                    break;

                case LeadPhase.PanelIntro:
                    animProgress = MathHelper.Lerp(animProgress, 1f, AnimSpeed);
                    break;

                case LeadPhase.Complete:
                    break;
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
            if (currentPhase != LeadPhase.KeyPrompt && currentPhase != LeadPhase.PanelIntro) return;

            int mouseTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (mouseTextIndex == -1) return;

            layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                "CWRMod: Entrust Guide Lead",
                delegate {
                    var sb = Main.spriteBatch;
                    if (currentPhase == LeadPhase.KeyPrompt) {
                        DrawKeyPromptCard(sb);
                    }
                    else if (currentPhase == LeadPhase.PanelIntro) {
                        DrawPanelIntroCard(sb);
                    }
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }

        /// <summary>标记引导完成并保存</summary>
        private static void MarkGuideSeen() {
            if (Main.LocalPlayer.TryGetADVSave(out var save)) {
                save.Get<EntrustGuideModule>().GuideSeen = true;
            }
            currentPhase = LeadPhase.Complete;
        }

        /// <summary>获取 QuestManager_Key 当前绑定的键名，未绑定时返回 null</summary>
        private static string GetBoundKeyName() {
            if (CWRKeySystem.QuestManager_Key == null) return null;
            var keys = CWRKeySystem.QuestManager_Key.GetAssignedKeys();
            return keys.Count > 0 ? keys[0] : null;
        }

        //阶段1：左下角按键提示卡
        private static void DrawKeyPromptCard(SpriteBatch sb) {
            int screenW = Main.screenWidth;
            int screenH = Main.screenHeight;
            var font = FontAssets.MouseText.Value;

            float slideOffset = (1f - animProgress) * 60f;
            float x = 20f;
            float y = screenH - CardH1 - 20f + slideOffset;
            float alpha = animProgress;

            var bg = new Rectangle((int)x, (int)y, CardW1, CardH1);
            FillRect(sb, bg, new Color(0, 0, 0, (int)(200 * alpha)));
            //细边框
            StrokeRect(sb, bg, 1, new Color(180, 180, 180, (int)(120 * alpha)));

            string boundKey = GetBoundKeyName();
            bool hasBinding = boundKey != null;
            string displayKey = hasBinding ? boundKey : "K";

            string mainLine = $"按 [{displayKey}] 打开委托面板";
            string subLine = hasBinding ? null : "（未绑定，当前使用默认键，可在 设置→控制 中绑定自定义键）";

            float mainScale = 0.85f;
            float subScale = 0.63f;
            var mainColor = new Color(255, 255, 255, (int)(255 * alpha));
            var subColor = new Color(200, 200, 200, (int)(200 * alpha));

            float paddingX = 12f;
            float paddingY = 12f;
            Utils.DrawBorderString(sb, mainLine, new Vector2(x + paddingX, y + paddingY), mainColor, mainScale);
            if (subLine != null) {
                float mainH = font.MeasureString(mainLine).Y * mainScale;
                Utils.DrawBorderString(sb, subLine,
                    new Vector2(x + paddingX, y + paddingY + mainH + 2f), subColor, subScale);
            }

            //关闭按钮
            if (DrawCloseButton(sb, bg, alpha)) {
                MarkGuideSeen();
            }
        }

        //阶段2：委托面板右侧说明卡
        private static void DrawPanelIntroCard(SpriteBatch sb) {
            var ui = QuestManagerUI.Instance;
            if (ui == null) return;

            int screenH = Main.screenHeight;
            float alpha = animProgress;

            float slideOffset = (1f - animProgress) * 80f;
            float x = ui.PanelRightEdge + 15f - slideOffset;
            float y = (screenH - CardH2) / 2f;

            var bg = new Rectangle((int)x, (int)y, CardW2, CardH2);
            FillRect(sb, bg, new Color(0, 0, 0, (int)(200 * alpha)));
            StrokeRect(sb, bg, 1, new Color(180, 180, 180, (int)(120 * alpha)));

            //左侧三角箭头指向面板
            DrawLeftArrow(sb, new Vector2(x - 6f, y + CardH2 / 2f), alpha);

            var font = FontAssets.MouseText.Value;
            float scale = 0.73f;
            float subScale = 0.63f;
            float px = x + 12f;
            float py = y + 10f;
            float lineH = font.MeasureString("A").Y * scale + 4f;

            var titleColor = new Color(230, 230, 100, (int)(255 * alpha));
            var bodyColor = new Color(220, 220, 220, (int)(230 * alpha));
            var subColor = new Color(170, 200, 170, (int)(200 * alpha));

            Utils.DrawBorderString(sb, "委托操作提示", new Vector2(px, py), titleColor, scale);
            py += lineH + 2f;
            Utils.DrawBorderString(sb, "右键单击条目 → 关注委托（在屏幕左侧追踪进度）", new Vector2(px, py), bodyColor, subScale);
            py += font.MeasureString("A").Y * subScale + 2f;
            Utils.DrawBorderString(sb, "中键单击条目 → 挂起委托（暂时隐藏该委托）", new Vector2(px, py), subColor, subScale);

            //明白了按钮
            if (DrawConfirmButton(sb, bg, alpha)) {
                MarkGuideSeen();
            }
        }

        /// <summary>绘制右上角关闭按钮，返回 true 时表示本帧被点击</summary>
        private static bool DrawCloseButton(SpriteBatch sb, Rectangle card, float alpha) {
            const int size = 16;
            const int margin = 6;
            var rect = new Rectangle(card.Right - size - margin, card.Top + margin, size, size);
            bool hovered = rect.Contains(Main.mouseX, Main.mouseY);
            var color = hovered
                ? new Color(255, 100, 100, (int)(255 * alpha))
                : new Color(200, 200, 200, (int)(180 * alpha));
            Utils.DrawBorderString(sb, "×",
                new Vector2(rect.X, rect.Y), color, 0.8f);
            if (hovered) {
                Main.LocalPlayer.mouseInterface = true;
            }
            return hovered && Main.mouseLeft && !Main.mouseLeftRelease;
        }

        /// <summary>绘制底部"明白了"确认按钮，返回 true 时表示本帧被点击</summary>
        private static bool DrawConfirmButton(SpriteBatch sb, Rectangle card, float alpha) {
            const int btnW = 72;
            const int btnH = 18;
            const int margin = 8;
            var rect = new Rectangle(card.Right - btnW - margin, card.Bottom - btnH - margin, btnW, btnH);
            bool hovered = rect.Contains(Main.mouseX, Main.mouseY);
            FillRect(sb, rect, new Color(40, 80, 40, (int)((hovered ? 220 : 150) * alpha)));
            StrokeRect(sb, rect, 1, new Color(130, 200, 130, (int)(160 * alpha)));
            var textColor = new Color(200, 255, 200, (int)(255 * alpha));
            Vector2 textSize = FontAssets.MouseText.Value.MeasureString("明白了") * 0.62f;
            Utils.DrawBorderString(sb, "明白了",
                new Vector2(rect.X + (rect.Width - textSize.X) * 0.5f, rect.Y + (rect.Height - textSize.Y) * 0.5f),
                textColor, 0.62f);
            if (hovered) {
                Main.LocalPlayer.mouseInterface = true;
            }
            return hovered && Main.mouseLeft && !Main.mouseLeftRelease;
        }

        /// <summary>绘制指向左边的三角箭头</summary>
        private static void DrawLeftArrow(SpriteBatch sb, Vector2 tip, float alpha) {
            var px = VaultAsset.placeholder2.Value;
            var color = new Color(180, 180, 180, (int)(140 * alpha));
            //用三个细矩形近似三角形
            for (int i = 0; i < 5; i++) {
                int halfH = 5 - i;
                sb.Draw(px, new Rectangle((int)tip.X + i, (int)tip.Y - halfH, 1, halfH * 2), color);
            }
        }

        private static void FillRect(SpriteBatch sb, Rectangle rect, Color color) {
            BaseManagerStyle.FillRect(sb, rect, color);
        }

        private static void StrokeRect(SpriteBatch sb, Rectangle rect, int bw, Color color) {
            BaseManagerStyle.StrokeRect(sb, rect, bw, color);
        }
    }
}

