using CalamityOverhaul.Content.ADV.QuestManager;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;

namespace CalamityOverhaul.Content.LegendWeapon.HalibutLegend.Quest
{
    /// <summary>
    /// 比目鱼传奇武器的单条试炼委托条目——<see cref="QuestEntryData"/> 子类，
    /// 动态追踪目标Boss的存活状态与血量，为追踪窗口提供战斗进度显示：<br/>
    /// · Boss不在场时提示等待召唤<br/>
    /// · Boss存在时显示实时血量百分比
    /// </summary>
    internal class HalibutTrialQuestEntry : QuestEntryData
    {
        /// <summary>本试炼需要击杀的目标Boss的NPC type列表（满足其一即可）</summary>
        public int[] TargetNpcTypes { get; init; }

        /// <summary>Boss不在场时的提示文本</summary>
        public LocalizedText WaitingHint { get; init; }

        /// <summary>Boss在场时的血量格式文本，{0}为Boss名，{1}为血量百分比</summary>
        public LocalizedText FightingFormat { get; init; }

        private bool isBossAlive;
        private float bossHealthRatio;
        private string activeBossName;

        public HalibutTrialQuestEntry(string key, LocalizedText title, LocalizedText summary, LocalizedText category)
            : base(key, title, summary, category) { }

        public override void OnUpdate() {
            if (Status == QuestEntryStatus.Completed || Status == QuestEntryStatus.Failed
                || Status == QuestEntryStatus.Suspended) return;

            isBossAlive = false;
            float bestRatio = 1f;
            string bestName = "";

            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.lifeMax <= 0) continue;
                if (Array.IndexOf(TargetNpcTypes, npc.type) < 0) continue;

                isBossAlive = true;
                float ratio = (float)npc.life / npc.lifeMax;
                if (ratio < bestRatio) {
                    bestRatio = ratio;
                    bestName = Lang.GetNPCNameValue(npc.type);
                }
            }

            if (isBossAlive) {
                bossHealthRatio = bestRatio;
                activeBossName = bestName;
                Progress = MathHelper.Clamp(1f - bossHealthRatio, 0f, 1f);
            }
            else {
                bossHealthRatio = 1f;
                activeBossName = "";
                Progress = 0f;
            }
        }

        public override List<string> GetTrackerDetails() {
            if (!isBossAlive) {
                return [WaitingHint?.Value ?? "..."];
            }

            return [string.Format(FightingFormat?.Value ?? "{0}: {1:0%}", activeBossName, bossHealthRatio)];
        }
    }
}
