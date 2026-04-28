using CalamityOverhaul.Content.ADV.EntrustManager;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.MurasamaLegend.TrialQuests
{
    /// <summary>
    /// 鬼妖村正试炼线——将15段试炼注册到 <see cref="QuestManagerUI"/>，
    /// 并根据 <see cref="InWorldBossPhase.Mura_Level"/> 实时同步状态。<br/>
    /// 同时仅显示当前进行中的试炼和上一个已完成的试炼（滑动窗口）
    /// </summary>
    internal class MurasamaTrialQuestLine : ModSystem, ILocalizedModType
    {
        public string LocalizationCategory => "Legend";

        /// <summary>试炼总数（对应等级0-13的试炼，等级14为全部完成）</summary>
        private const int TRIAL_COUNT = 14;
        private const string KEY_PREFIX = "Mura_Trial_";

        #region 本地化文本

        public static LocalizedText QuestCategory { get; private set; }
        public static LocalizedText TrackerWaiting { get; private set; }
        public static LocalizedText TrackerFighting { get; private set; }

        /// <summary>每条试炼的标题</summary>
        public static LocalizedText[] TrialTitles { get; private set; }
        public static LocalizedText[] TrialSummaries { get; private set; }

        #endregion

        /// <summary>每条试炼对应需要击杀的Boss NPC type列表</summary>
        private static int[][] trialTargetNpcs;

        public override void SetStaticDefaults() {
            QuestCategory = this.GetLocalization(nameof(QuestCategory), () => "鬼妖村正·试炼");
            TrackerWaiting = this.GetLocalization(nameof(TrackerWaiting), () => "目标不在场，等待召唤...");
            TrackerFighting = this.GetLocalization(nameof(TrackerFighting), () => "{0}: {1:0%}");

            TrialTitles = new LocalizedText[TRIAL_COUNT];

            //标题风格参考MGSV:TPP，以军事行动代号的口吻
            string[] defaultTitles = [
                "沙地幽影",         //0 史莱姆王+荒漠灾虫
                "入侵之者",           //1 克苏鲁之眼
                "腐化清除",         //2 世吞/克脑
                "寄生威胁",         //3 腐巢意志/血肉宿主
                "通向地狱",         //4 史莱姆之神+肉山
                "钢铁齿轮",         //5 三对机械Boss
                "灾影行动",         //6 灾厄之影+世纪之花
                "石化阵线",         //7 石巨人+石后三Boss
                "月球坠落",         //8 月球领主
                "幻影制裁",         //9 亵渎天神+噬魂幽花
                "弑神之蛇",         //10 神明吞噬者
                "丛林之炎",         //11 丛林龙犽戎
                "核心终结",         //12 星流巨械+至尊灾厄
                "原初回归",         //13 始源妖龙
            ];
            for (int i = 0; i < TRIAL_COUNT; i++) {
                int idx = i;
                TrialTitles[i] = this.GetLocalization($"Trial_{i}", () => defaultTitles[idx]);
            }

            TrialSummaries = new LocalizedText[TRIAL_COUNT];
            string[] defaultSummaries = [
                "新兵，向我证明你自己\n那个黏糊糊的蓝胖子，沙海之下的海妖余孽\n解决他们，这是你的第一步",
                "夜月当空，让我们去戳爆那颗在空中乱飞的大眼球，让那个伪神彻底变成瞎子",
                "伪神的残躯玷污着泰拉的大地，异端在世界尽头滋生，异形在大陆上横行\n去解放被腐化的大陆，砍碎那坨伪神的大脑，剁碎那条紫色蠕虫！",
                "腐化的大地还在像心脏一样颤动，血肉的寄生者还在活动，腐化的肿瘤仍旧在思考，去将他们彻底放逐！",
                "史莱姆王国流传着一个传说，它们有一个堕落的异神\n地牢门口的诅咒已经松动，我感觉到灵能在深牢中蠢动，夹杂着惨叫、哀嚎、低语...",
                "硫磺之海的弃儿阻挡了我们探寻深渊的宝藏，去终结它的吞噬和游荡\n一个巨大的钢铁蠕虫挡住了我们的征途，将它剁成碎片",
                "那个投入混沌的女巫，她有一个畸变的克隆姊妹，杀了那个异形，以血和火焰祭刀",
                "愚蠢的蜥蜴族只会信奉这些冥顽不灵的石头，让我们把它斩为齑粉，摧毁他们的信仰",
                "那个躲在月亮背面的伪神不过是个残缺的拼凑物。去斩断它的触须，挖出它的心脏，用它的血来痛饮",
                "靠吸食恒星热能苟延残喘的可怜神明，它的异端之火需要彻底熄灭",
                "那条傲慢的宇宙巨蟒在世界的帷幕后蠢蠢欲动，终结他的野望",
                "丛林巨龙，泰拉大陆仅存的金源龙裔，值得我们的尊重\n然而，眼下我们却需让它再次赴死，剥夺其身上的金源魄",
                "那个机械教会的异端笃信泰坦胜过神明，让我们将那几台泰坦归还自然的状态——一堆废铁\n曾拥有出色灵能天赋的女巫早已迷失于混沌之中，唯有将她放逐",
                "我们的征服之路早已不可阻挡，破碎那黑渊之下妖龙的铠甲\n使用那终焉之石，与异形展开最终决战",
            ];
            for (int i = 0; i < TRIAL_COUNT; i++) {
                int idx = i;
                TrialSummaries[i] = this.GetLocalization($"TrialSummary_{i}", () => defaultSummaries[idx]);
            }
        }

        public override void PostSetupContent() {
            trialTargetNpcs = new int[TRIAL_COUNT][];
            //试炼0 (等级0→1): 史莱姆王 + 荒漠灾虫
            trialTargetNpcs[0] = [NPCID.KingSlime, CWRID.NPC_DesertScourgeHead];
            //试炼1 (等级1→2): 克苏鲁之眼
            trialTargetNpcs[1] = [NPCID.EyeofCthulhu];
            //试炼2 (等级2→3): 世界吞噬者 / 克苏鲁之脑
            trialTargetNpcs[2] = [NPCID.EaterofWorldsHead, NPCID.BrainofCthulhu];
            //试炼3 (等级3→4): 腐巢意志 / 血肉宿主
            trialTargetNpcs[3] = [CWRID.NPC_HiveMind, CWRID.NPC_PerforatorHive];
            //试炼4 (等级4→5): 史莱姆之神 / 骷髅王 + 肉山(需hardMode)
            trialTargetNpcs[4] = [CWRID.NPC_SlimeGodCore, NPCID.SkeletronHead, NPCID.WallofFlesh];
            //试炼5 (等级5→6): 三对机械Boss全部
            //毁灭者+渊海灾虫, 双子魔眼+硫磺火元素, 机械骷髅王+极地冰灵
            trialTargetNpcs[5] = [NPCID.TheDestroyer, CWRID.NPC_AquaticScourgeHead,
                NPCID.Retinazer, NPCID.Spazmatism, CWRID.NPC_BrimstoneElemental,
                NPCID.SkeletronPrime, CWRID.NPC_Cryogen];
            //试炼6 (等级6→7): 灾厄之影 + 世纪之花
            trialTargetNpcs[6] = [CWRID.NPC_CalamitasClone, NPCID.Plantera];
            //试炼7 (等级7→8): 石巨人 + 石后三Boss(瘟疫使者+毁灭魔像+星神游龙)
            trialTargetNpcs[7] = [NPCID.Golem, NPCID.GolemHead,
                CWRID.NPC_PlaguebringerGoliath, CWRID.NPC_RavagerBody, CWRID.NPC_AstrumDeusHead];
            //试炼8 (等级8→9): 月球领主
            trialTargetNpcs[8] = [NPCID.MoonLordCore];
            //试炼9 (等级9→10): 亵渎天神 + 噬魂幽花
            trialTargetNpcs[9] = [CWRID.NPC_Providence, CWRID.NPC_Polterghast];
            //试炼10 (等级10→11): 神明吞噬者
            trialTargetNpcs[10] = [CWRID.NPC_DevourerofGodsHead];
            //试炼11 (等级11→12): 丛林龙犽戎
            trialTargetNpcs[11] = [CWRID.NPC_Yharon];
            //试炼12 (等级12→13): 星流巨械 + 至尊灾厄
            trialTargetNpcs[12] = [CWRID.NPC_AresBody, CWRID.NPC_Apollo,
                CWRID.NPC_Artemis, CWRID.NPC_ThanatosHead, CWRID.NPC_SupremeCalamitas];
            //试炼13 (等级13→14): 始源妖龙
            trialTargetNpcs[13] = [CWRID.NPC_PrimordialWyrmHead];
        }

        public override void PostUpdateWorld() {
            if (Main.dedServ || Main.gameMenu) return;

            if (!Main.LocalPlayer.HasItem(CWRID.Item_Murasama)) {
                //未持有村正时不显示试炼
                return;
            }

            //村正试炼基于世界Boss击杀进度，无需武器持有检查
            var manager = QuestManagerUI.Instance;
            if (manager == null) return;

            int level = InWorldBossPhase.Mura_Level();

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

        private MurasamaTrialQuestEntry CreateTrialEntry(int trialIndex) {
            return new MurasamaTrialQuestEntry(KEY_PREFIX + trialIndex,
                TrialTitles[trialIndex], TrialSummaries[trialIndex], QuestCategory) {
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
