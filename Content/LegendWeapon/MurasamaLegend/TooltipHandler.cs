using CalamityOverhaul.Common;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using static CalamityOverhaul.Content.LegendWeapon.MurasamaLegend.MurasamaOverride;

namespace CalamityOverhaul.Content.LegendWeapon.MurasamaLegend
{
    internal class TooltipHandler
    {
        public static void SetTooltip(Item item, ref List<TooltipLine> tooltips) {
            tooltips.InsertHotkeyBinding(CWRKeySystem.Murasama_TriggerKey, "[KEY1]", noneTip: CWRLocText.Instance.Notbound.Value);
            tooltips.InsertHotkeyBinding(CWRKeySystem.Murasama_DownKey, "[KEY2]", noneTip: CWRLocText.Instance.Notbound.Value);

            string text2 = CWRLocText.GetTextValue("Murasama_Text0");

            //试炼叙事文本已迁移至委托系统(MurasamaTrialQuestLine)，此处仅保留技能解锁与传奇状态
            tooltips.ReplacePlaceholder("[Lang1]", UnlockSkill1(item) ? $"[c/00ff00:{text2}]" : $"[c/808080:{CWRLocText.GetTextValue("Murasama_Text1")}]");
            tooltips.ReplacePlaceholder("[Lang2]", UnlockSkill2(item) ? $"[c/00ff00:{text2}]" : $"[c/808080:{CWRLocText.GetTextValue("Murasama_Text2")}]");
            tooltips.ReplacePlaceholder("[Lang3]", UnlockSkill3(item) ? $"[c/00ff00:{text2}]" : $"[c/808080:{CWRLocText.GetTextValue("Murasama_Text3")}]");

            tooltips.ReplacePlaceholder("legend_Text", CWRLocText.GetTextValue("Murasama_No_legend_Content_3"));

            //移除已迁移到委托系统的占位行，避免空行
            for (int i = tooltips.Count - 1; i >= 0; i--) {
                string text = tooltips[i].Text;
                if (text == "[Lang4]" || text == "[Text]") {
                    tooltips.RemoveAt(i);
                }
            }
        }
    }
}
