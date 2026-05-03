using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.EntrustManager;
using CalamityOverhaul.Content.ADV.Scenarios.Helen;
using CalamityOverhaul.Content.ADV.Scenarios.Helen.Quest;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.HalibutLegend.TrialQuests
{
    /// <summary>
    /// 比目鱼传奇试炼线——将14段试炼注册到 <see cref="QuestManagerUI"/>，
    /// 并根据 <see cref="InWorldBossPhase.Halibut_Level"/> 实时同步状态。<br/>
    /// 同时显示当前进行中的试炼和所有已完成的试炼
    /// </summary>
    internal class HalibutTrialQuestLine : ModSystem, ILocalizedModType
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

        /// <summary>每条试炼独立的完成判定，与等级顺序解耦，以避免乱序击败后试炼仍不能完成的问题</summary>
        private static Func<bool>[] trialCompletedChecks;

        public override void SetStaticDefaults() {
            QuestCategory = this.GetLocalization(nameof(QuestCategory), () => "比目鱼传说");
            TrackerWaiting = this.GetLocalization(nameof(TrackerWaiting), () => "目标不在场，等待召唤...");
            TrackerFighting = this.GetLocalization(nameof(TrackerFighting), () => "{0}: {1:0%}");

            TrialTitles = new LocalizedText[TRIAL_COUNT];

            //标题风格参考赛博朋克2077，以比目鱼伙伴的口吻暗示目标
            string[] defaultTitles = [
                "开胃菜",           //0 史莱姆王
                "不速之瞳",         //1 克苏鲁之眼
                "丛林拜访",         //2 蜂后
                "安息与启程",       //3 骷髅王+血肉墙
                "钢铁潮汐",         //4 机械Boss/渊海灾虫
                "拙劣的复制品",     //5 灾厄之影/世纪之花
                "给遗迹塞电池",     //6 石巨人
                "月球背面",         //7 月球领主
                "冷水澡",           //8 亵渎天神
                "不屈亡魂",         //9 噬魂幽花
                "弑神者",           //10 神明吞噬者
                "升温",             //11 丛林龙
                "造物巅峰",         //12 星流巨械+至尊灾厄
                "回到海里",         //13 始源妖龙
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

            //以下完成判定与 InWorldBossPhase.Halibut_Level() 中的跳级条件一一对应，仅去除“前置全部达成”的顺序锁
            trialCompletedChecks = new Func<bool>[TRIAL_COUNT];
            trialCompletedChecks[0] = InWorldBossPhase.DownedV0;
            trialCompletedChecks[1] = InWorldBossPhase.DownedV1;
            trialCompletedChecks[2] = InWorldBossPhase.DownedV3;
            trialCompletedChecks[3] = () => InWorldBossPhase.DownedV4.Invoke() && Main.hardMode;
            trialCompletedChecks[4] = () => InWorldBossPhase.DownedV5.Invoke() || InWorldBossPhase.Downed8.Invoke();
            trialCompletedChecks[5] = () => InWorldBossPhase.Downed10.Invoke() || InWorldBossPhase.VDownedV7.Invoke();
            trialCompletedChecks[6] = InWorldBossPhase.DownedV7;
            trialCompletedChecks[7] = InWorldBossPhase.VDownedV16;
            trialCompletedChecks[8] = InWorldBossPhase.Downed19;
            trialCompletedChecks[9] = InWorldBossPhase.Downed23;
            trialCompletedChecks[10] = InWorldBossPhase.Downed27;
            trialCompletedChecks[11] = InWorldBossPhase.Downed28;
            trialCompletedChecks[12] = () => InWorldBossPhase.Downed29.Invoke() && InWorldBossPhase.Downed30.Invoke();
            trialCompletedChecks[13] = () => InWorldBossPhase.Downed31.Invoke() || InWorldBossPhase.Downed32.Invoke();
        }

        public override void PostUpdateWorld() {
            if (Main.dedServ || Main.gameMenu) return;

            if (!Main.LocalPlayer.TryGetADVSave(out var save)) {
                return;
            }

            if (!Main.LocalPlayer.HasHalibut()) {
                return;//未获得比目鱼前不显示任何试炼
            }

            if (!save.Get<HalibutADVData>().FirstMet) {
                return;//未获得比目鱼前不显示任何试炼
            }

            if (!save.Get<HalibutADVData>().PostFirstMetIsComplete) {
                return;//未获得比目鱼前不显示任何试炼
            }

            var manager = QuestManagerUI.Instance;
            if (manager == null) return;

            int level = InWorldBossPhase.Halibut_Level();

            for (int i = 0; i < TRIAL_COUNT; i++) {
                SyncTrial(manager, i, level);
            }
        }

        /// <summary>
        /// 同步单条试炼的注册与状态<br/>
        /// 优先看独立完成判定：一旦命中直接Completed（防止乱序击败后试炼锁死在本地等级上）<br/>
        /// 否则仅在 trialIndex == currentLevel 时作为Active显示，未来试炼从管理器移除
        /// </summary>
        private void SyncTrial(QuestManagerUI manager, int trialIndex, int currentLevel) {
            string key = KEY_PREFIX + trialIndex;

            //两个条件取OR：独立判定命中，或等级系统已推进过该关
            //任一为真都算完成，防止极端情况下等级推进了但独立判定漏判
            bool isDone = (trialCompletedChecks[trialIndex]?.Invoke() == true) || (trialIndex < currentLevel);

            if (trialIndex > currentLevel) {
                //等级还未到达的试炼，即使提前打了Boss也不提前显示
                manager.UnregisterQuest(key);
            }
            else if (isDone) {
                //已完成的试炼（保留显示）
                var entry = EnsureTrialEntry(manager, trialIndex, completed: true);
                if (entry != null && entry.Status != QuestEntryStatus.Completed) {
                    manager.SetEntryStatus(key, QuestEntryStatus.Completed, 1f);
                }
            }
            else {
                //trialIndex == currentLevel 且未完成，当前进行中的试炼
                var entry = EnsureTrialEntry(manager, trialIndex);
                if (entry == null) {
                    return;
                }
                else if (entry.Status == QuestEntryStatus.Completed) {
                    manager.SetEntryStatus(key, QuestEntryStatus.Active, 0f);
                }
            }
        }

        private HalibutTrialQuestEntry EnsureTrialEntry(QuestManagerUI manager, int trialIndex, bool completed = false) {
            string key = KEY_PREFIX + trialIndex;
            var entry = manager.GetEntry(key) as HalibutTrialQuestEntry;
            if (entry != null) return entry;

            entry = CreateTrialEntry(trialIndex);
            if (completed) {
                entry.Status = QuestEntryStatus.Completed;
                entry.Progress = 1f;
            }
            manager.RegisterQuest(entry);
            return entry;
        }

        private HalibutTrialQuestEntry CreateTrialEntry(int trialIndex) {
            //使用CWRLocText中已有的长叙事文本作为摘要，
            //在管理器面板中折叠时自动截断，展开后显示完整描述
            var summaryText = CWRLocText.GetText($"Halibut_TextDictionary_Content_{trialIndex}");
            return new HalibutTrialQuestEntry(KEY_PREFIX + trialIndex,
                TrialTitles[trialIndex], summaryText, QuestCategory) {
                Priority = TRIAL_COUNT - trialIndex,
                EntryStyle = new OceanEntryStyle(),
                TrackerStyle = new OceanTrackerWidgetStyle(),
                TargetNpcTypes = trialTargetNpcs[trialIndex],
                IsCompletedCheck = trialCompletedChecks[trialIndex],
                WaitingHint = TrackerWaiting,
                FightingFormat = TrackerFighting,
            };
        }
    }
}
