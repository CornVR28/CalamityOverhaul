using CalamityOverhaul.Content.ADV.EntrustManager;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;

namespace CalamityOverhaul.Content.LegendWeapon.HalibutLegend.TrialQuests
{
    /// <summary>
    /// 比目鱼传奇武器的单条试炼委托条目——<see cref="EntrustEntryData"/> 子类，
    /// 动态追踪目标Boss的存活状态与血量，为追踪窗口提供战斗进度显示：<br/>
    /// · Boss不在场时提示等待召唤<br/>
    /// · Boss存在时显示实时血量百分比
    /// </summary>
    internal class HalibutTrialQuestEntry : EntrustEntryData
    {
        /// <summary>本试炼需要击杀的目标Boss的NPC type列表（满足其一即可）</summary>
        public int[] TargetNpcTypes { get; init; }

        /// <summary>Boss不在场时的提示文本</summary>
        public LocalizedText WaitingHint { get; init; }

        /// <summary>Boss在场时的血量格式文本，{0}为Boss名，{1}为血量百分比</summary>
        public LocalizedText FightingFormat { get; init; }

        /// <summary>追踪窗口"一行简介"的格式串（{0}=Boss名列表），未设置则只显示Boss名本身</summary>
        public LocalizedText BriefFormat { get; init; }

        /// <summary>独立的完成判定，命中后无论等级是否推进都直接视为已完成（典型实现是读取对应Boss的Downed标志）</summary>
        public Func<bool> IsCompletedCheck { get; init; }

        private bool isBossAlive;
        private float bossHealthRatio;
        private string activeBossName;

        public HalibutTrialQuestEntry(string key, LocalizedText title, LocalizedText summary, LocalizedText category)
            : base(key, title, summary, category) { }

        public override void OnUpdate() {
            if (Status == QuestEntryStatus.Completed || Status == QuestEntryStatus.Failed
                || Status == QuestEntryStatus.Suspended) return;

            //已经达成完成判定：锁定显示，避免Boss死亡瞬间Progress跳回0造成视觉抖动
            if (IsCompletedCheck != null && IsCompletedCheck.Invoke()) {
                isBossAlive = false;
                bossHealthRatio = 0f;
                activeBossName = "";
                Progress = 1f;
                return;
            }

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
            var lines = new List<string>(2);

            //第一行：CP2077式"一行简介"，自动从目标Boss列表生成
            string brief = BuildBrief();
            if (!string.IsNullOrEmpty(brief)) lines.Add(brief);

            //第二行：动态状态——Boss不在场→等待提示；Boss在场→当前血量百分比
            if (!isBossAlive) {
                lines.Add(WaitingHint?.Value ?? "...");
            }
            else {
                lines.Add(string.Format(FightingFormat?.Value ?? "{0}: {1:0%}",
                    activeBossName, bossHealthRatio));
            }

            return lines;
        }

        private string BuildBrief() {
            if (TargetNpcTypes == null || TargetNpcTypes.Length == 0) return "";

            string list = string.Join(" / ", TargetNpcTypes
                .Select(static t => Lang.GetNPCNameValue(t))
                .Where(static n => !string.IsNullOrEmpty(n)));
            if (string.IsNullOrEmpty(list)) return "";

            string fmt = BriefFormat?.Value;
            return string.IsNullOrEmpty(fmt) ? list : string.Format(fmt, list);
        }
    }
}
