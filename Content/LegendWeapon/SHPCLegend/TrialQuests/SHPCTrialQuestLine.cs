using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.EntrustManager;
using CalamityOverhaul.Content.LegendWeapon.MurasamaLegend.TrialQuests;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.TrialQuests
{
    internal class SHPCTrialQuestLine : ModSystem, ILocalizedModType
    {
        public string LocalizationCategory => "Legend";

        private const int TRIAL_COUNT = 16;
        private const string KEY_PREFIX = "SHPC_Trial_";

        #region 本地化文本

        public static LocalizedText QuestCategory { get; private set; }
        public static LocalizedText TrackerWaiting { get; private set; }
        public static LocalizedText TrackerFighting { get; private set; }
        public static LocalizedText[] TrialTitles { get; private set; }

        #endregion

        private static int[][] trialTargetNpcs;

        public override void SetStaticDefaults() {
            QuestCategory = this.GetLocalization(nameof(QuestCategory), () => "SHPC·试炼");
            TrackerWaiting = this.GetLocalization(nameof(TrackerWaiting), () => "目标不在场，等待召唤...");
            TrackerFighting = this.GetLocalization(nameof(TrackerFighting), () => "{0}: {1:0%}");

            TrialTitles = new LocalizedText[TRIAL_COUNT];
            string[] defaultTitles = [
                "史莱姆清除",   //0 史莱姆王
                "眼部解剖",     //1 克苏鲁之眼
                "生化样本",     //2 世界吞噬者/克苏鲁之脑
                "污秽提纯",     //3 史莱姆之神
                "封印突破",     //4 血肉墙
                "机械之夜",     //5 任意机械Boss
                "铁疙瘩清除",   //6 所有机械Boss
                "生态考察",     //7 世纪之花/灾厄之影
                "远古科技",     //8 石巨人
                "信息封锁",     //9 邪教徒
                "月背秘密",     //10 月球领主
                "地核探险",     //11 亵渎天神/噬魂幽花
                "神域入侵",     //12 神明吞噬者
                "获取龙羽",     //13 丛林龙犽戎
                "造物主访问",   //14 星流巨械/至尊灾厄
                "终焉大战",     //15 终焉之战
            ];
            for (int i = 0; i < TRIAL_COUNT; i++) {
                int idx = i;
                TrialTitles[i] = this.GetLocalization($"Trial_{i}", () => defaultTitles[idx]);
            }
        }

        public override void PostSetupContent() {
            trialTargetNpcs = new int[TRIAL_COUNT][];
            //试炼0 (等级0→1): 史莱姆王
            trialTargetNpcs[0] = [NPCID.KingSlime];
            //试炼1 (等级1→2): 克苏鲁之眼
            trialTargetNpcs[1] = [NPCID.EyeofCthulhu];
            //试炼2 (等级2→3): 世界吞噬者 / 克苏鲁之脑
            trialTargetNpcs[2] = [NPCID.EaterofWorldsHead, NPCID.BrainofCthulhu];
            //试炼3 (等级3→4): 史莱姆之神
            trialTargetNpcs[3] = [CWRID.NPC_SlimeGodCore];
            //试炼4 (等级4→5): 血肉墙
            trialTargetNpcs[4] = [NPCID.WallofFlesh];
            //试炼5 (等级5→6): 任意机械Boss（追踪血量最低的一台）
            trialTargetNpcs[5] = [NPCID.TheDestroyer, NPCID.Retinazer,
                NPCID.Spazmatism, NPCID.SkeletronPrime];
            //试炼6 (等级6→7): 所有机械Boss（仍追踪血量最低的一台）
            trialTargetNpcs[6] = [NPCID.TheDestroyer, NPCID.Retinazer,
                NPCID.Spazmatism, NPCID.SkeletronPrime];
            //试炼7 (等级7→8): 世纪之花 或 灾厄之影
            trialTargetNpcs[7] = [NPCID.Plantera, CWRID.NPC_CalamitasClone];
            //试炼8 (等级8→9): 石巨人
            trialTargetNpcs[8] = [NPCID.Golem, NPCID.GolemHead];
            //试炼9 (等级9→10): 邪教徒
            trialTargetNpcs[9] = [NPCID.CultistBoss];
            //试炼10 (等级10→11): 月球领主
            trialTargetNpcs[10] = [NPCID.MoonLordCore];
            //试炼11 (等级11→12): 亵渎天神 或 噬魂幽花
            trialTargetNpcs[11] = [CWRID.NPC_Providence, CWRID.NPC_Polterghast];
            //试炼12 (等级12→13): 神明吞噬者
            trialTargetNpcs[12] = [CWRID.NPC_DevourerofGodsHead];
            //试炼13 (等级13→14): 丛林龙犽戎
            trialTargetNpcs[13] = [CWRID.NPC_Yharon];
            //试炼14 (等级14→15): 星流巨械 或 至尊灾厄
            trialTargetNpcs[14] = [CWRID.NPC_AresBody, CWRID.NPC_Apollo,
                CWRID.NPC_Artemis, CWRID.NPC_ThanatosHead, CWRID.NPC_SupremeCalamitas];
            //试炼15 (等级15→16): 终焉之战，无可追踪的固定NPC，完成后等级直接变16
            trialTargetNpcs[15] = [];
        }

        public override void PostUpdateWorld() {
            if (Main.dedServ || Main.gameMenu) return;

            if (!Main.LocalPlayer.HasItem(CWRID.Item_SHPC)) {
                return;
            }

            var manager = QuestManagerUI.Instance;
            if (manager == null) return;

            int level = InWorldBossPhase.SHPC_Level();

            for (int i = 0; i < TRIAL_COUNT; i++) {
                SyncTrial(manager, i, level);
            }
        }

        private void SyncTrial(QuestManagerUI manager, int trialIndex, int currentLevel) {
            string key = KEY_PREFIX + trialIndex;

            if (trialIndex == currentLevel && currentLevel < TRIAL_COUNT) {
                var entry = manager.GetEntry(key);
                if (entry == null) {
                    entry = CreateTrialEntry(trialIndex);
                    manager.RegisterQuest(entry);
                }
                else if (entry.Status == QuestEntryStatus.Completed) {
                    manager.SetEntryStatus(key, QuestEntryStatus.Active, 0f);
                }
            }
            else if (trialIndex == currentLevel - 1 && currentLevel > 0) {
                var entry = manager.GetEntry(key);
                if (entry == null) {
                    entry = CreateTrialEntry(trialIndex);
                    entry.Status = QuestEntryStatus.Completed;
                    entry.Progress = 1f;
                    manager.RegisterQuest(entry);
                }
                else if (entry.Status != QuestEntryStatus.Completed) {
                    manager.SetEntryStatus(key, QuestEntryStatus.Completed, 1f);
                }
            }
            else {
                manager.UnregisterQuest(key);
            }
        }

        private SHPCTrialQuestEntry CreateTrialEntry(int trialIndex) {
            var summaryText = CWRLocText.GetText($"SHPC_TextDictionary_Content_{trialIndex}");
            return new SHPCTrialQuestEntry(KEY_PREFIX + trialIndex,
                TrialTitles[trialIndex], summaryText, QuestCategory) {
                Priority = TRIAL_COUNT - trialIndex,
                EntryStyle = new PhantomEntryStyle(),
                TrackerStyle = new PhantomTrackerWidgetStyle(),
                TargetNpcTypes = trialTargetNpcs[trialIndex],
                WaitingHint = TrackerWaiting,
                FightingFormat = TrackerFighting,
            };
        }
    }
}
