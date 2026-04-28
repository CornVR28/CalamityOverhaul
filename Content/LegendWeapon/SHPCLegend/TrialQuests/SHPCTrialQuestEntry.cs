using CalamityOverhaul.Content.ADV.EntrustManager;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.TrialQuests
{
    internal class SHPCTrialQuestEntry : EntrustEntryData
    {
        public int[] TargetNpcTypes { get; init; }
        public LocalizedText WaitingHint { get; init; }
        public LocalizedText FightingFormat { get; init; }

        private bool isBossAlive;
        private float bossHealthRatio;
        private string activeBossName;

        public SHPCTrialQuestEntry(string key, LocalizedText title, LocalizedText summary, LocalizedText category)
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
