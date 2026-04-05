using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.QuestManager;
using CalamityOverhaul.Content.ADV.Scenarios.Helen.Quest;
using CalamityOverhaul.Content.ADV.Scenarios.Helen.Quest.FishoilQuest;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.HalibutLegend.Quest
{
    /// <summary>
    /// 比目鱼传奇试炼线——将14段试炼注册到 <see cref="QuestManagerUI"/>，
    /// 并根据 <see cref="InWorldBossPhase.Halibut_Level"/> 实时同步状态。<br/>
    /// 同时仅显示当前进行中的试炼和上一个已完成的试炼
    /// </summary>
    internal class HalibutQuestLine : ModSystem, ILocalizedModType
    {
        public string LocalizationCategory => "Legend";

        /// <summary>试炼总数（对应等级0-13的试炼，等级14为全部完成）</summary>
        private const int TRIAL_COUNT = 14;
        private const string KEY_PREFIX = "Halibut_Trial_";

        #region 本地化文本

        public static LocalizedText QuestCategory { get; private set; }
        public static LocalizedText TrackerWaiting { get; private set; }
        public static LocalizedText TrackerFighting { get; private set; }

        /// <summary>每条试炼的标题</summary>
        public static LocalizedText[] TrialTitles { get; private set; }

        #endregion

        /// <summary>每条试炼对应需要击杀的Boss NPC type列表</summary>
        private static int[][] trialTargetNpcs;

        public override void SetStaticDefaults() {
            QuestCategory = this.GetLocalization(nameof(QuestCategory), () => "比目鱼传说");
            TrackerWaiting = this.GetLocalization(nameof(TrackerWaiting), () => "目标不在场，等待召唤...");
            TrackerFighting = this.GetLocalization(nameof(TrackerFighting), () => "{0}: {1:0%}");

            TrialTitles = new LocalizedText[TRIAL_COUNT];
            string[] defaultTitles = [
                "试炼 1",
                "试炼 2",
                "试炼 3",
                "试炼 4",
                "试炼 5",
                "试炼 6",
                "试炼 7",
                "试炼 8",
                "试炼 9",
                "试炼 10",
                "试炼 11",
                "试炼 12",
                "试炼 13",
                "试炼 14",
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
            //试炼2 (等级2→3): 蜂后
            trialTargetNpcs[2] = [NPCID.QueenBee];
            //试炼3 (等级3→4): 骷髅王 + 血肉墙(进入困难模式)
            trialTargetNpcs[3] = [NPCID.SkeletronHead, NPCID.WallofFlesh];
            //试炼4 (等级4→5): 任意机械Boss 或 渊海灾虫
            trialTargetNpcs[4] = [NPCID.TheDestroyer, NPCID.SkeletronPrime,
                NPCID.Retinazer, NPCID.Spazmatism, CWRID.NPC_AquaticScourgeHead];
            //试炼5 (等级5→6): 灾厄之影 或 世纪之花
            trialTargetNpcs[5] = [CWRID.NPC_CalamitasClone, NPCID.Plantera];
            //试炼6 (等级6→7): 石巨人
            trialTargetNpcs[6] = [NPCID.Golem, NPCID.GolemHead];
            //试炼7 (等级7→8): 月球领主
            trialTargetNpcs[7] = [NPCID.MoonLordCore];
            //试炼8 (等级8→9): 亵渎天神
            trialTargetNpcs[8] = [CWRID.NPC_Providence];
            //试炼9 (等级9→10): 噬魂幽花
            trialTargetNpcs[9] = [CWRID.NPC_Polterghast];
            //试炼10 (等级10→11): 神明吞噬者
            trialTargetNpcs[10] = [CWRID.NPC_DevourerofGodsHead];
            //试炼11 (等级11→12): 丛林龙
            trialTargetNpcs[11] = [CWRID.NPC_Yharon];
            //试炼12 (等级12→13): 星流巨械 与 至尊灾厄
            trialTargetNpcs[12] = [CWRID.NPC_AresBody, CWRID.NPC_Apollo,
                CWRID.NPC_Artemis, CWRID.NPC_ThanatosHead, CWRID.NPC_SupremeCalamitas];
            //试炼13 (等级13→14): 始源妖龙
            //Halibut_Level()用 Downed31 || Downed32，Boss Rush无可追踪NPC
            //但完成Boss Rush后level直接变14，试炼13会被立即标记为Completed
            trialTargetNpcs[13] = [CWRID.NPC_PrimordialWyrmHead];
        }

        public override void PostUpdateWorld() {
            if (Main.dedServ || Main.gameMenu) return;

            var manager = QuestManagerUI.Instance;
            if (manager == null) return;

            int level = InWorldBossPhase.Halibut_Level();

            for (int i = 0; i < TRIAL_COUNT; i++) {
                SyncTrial(manager, i, level);
            }
        }

        /// <summary>
        /// 同步单条试炼的注册与状态<br/>
        /// 当前等级 == 试炼索引 → Active（进行中）<br/>
        /// 当前等级 == 试炼索引+1 → Completed（最近完成，保留显示）<br/>
        /// 其他 → 从管理器移除
        /// </summary>
        private void SyncTrial(QuestManagerUI manager, int trialIndex, int currentLevel) {
            string key = KEY_PREFIX + trialIndex;

            if (trialIndex == currentLevel && currentLevel < TRIAL_COUNT) {
                //当前进行中的试炼
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
                //上一条已完成的试炼（保留显示）
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

        private HalibutTrialQuestEntry CreateTrialEntry(int trialIndex) {
            LocalizedText summary = CWRLocText.GetText($"Halibut_TextDictionary_Content_{trialIndex}");
            return new HalibutTrialQuestEntry(KEY_PREFIX + trialIndex,
                TrialTitles[trialIndex], summary, QuestCategory) {
                Priority = TRIAL_COUNT - trialIndex,
                EntryStyle = new OceanEntryStyle(),
                TrackerStyle = new OceanTrackerWidgetStyle(),
                TargetNpcTypes = trialTargetNpcs[trialIndex],
                WaitingHint = TrackerWaiting,
                FightingFormat = TrackerFighting,
            };
        }
    }
}
