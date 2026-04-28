using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.Dialogues
{
    /// <summary>
    /// 赛博领域激活时的专属对话，优先级高于空闲问候
    /// Shepel会针对当前领域层级给出状态简报
    /// </summary>
    internal class ShepelCyberActiveDialogue : SHPCDialogueScenarioBase, ILocalizedModType
    {
        public new string LocalizationCategory => "ADV.Shepel";
        public override int DialoguePriority => 10;

        public static LocalizedText RoleName { get; private set; }
        public static LocalizedText Line_Intro { get; private set; }
        public static LocalizedText Line_LayerReport { get; private set; }
        public static LocalizedText Line_Warning { get; private set; }
        public static LocalizedText Line_MaxLayer { get; private set; }

        protected override Func<DialogueBoxBase> DefaultDialogueStyle
            => () => ADV.DialogueBoxs.Styles.SHPCDialogueBox.Instance;

        public override void SetStaticDefaults() {
            RoleName = this.GetLocalization(nameof(RoleName), () => "SHPC");
            Line_Intro = this.GetLocalization(nameof(Line_Intro),
                () => "领域已激活，主人。当前层级 {0}，外部信号已完全隔离。");
            Line_LayerReport = this.GetLocalization(nameof(Line_LayerReport),
                () => "领域越深，可干预的目标范围越广，但能量消耗也成倍增加。请注意RAM余量。");
            Line_Warning = this.GetLocalization(nameof(Line_Warning),
                () => "检测到领域边界有波动迹象，建议提高警惕。");
            Line_MaxLayer = this.GetLocalization(nameof(Line_MaxLayer),
                () => "主人，当前已处于最深层级。此处的规则已与外界完全分离，请谨慎行动。");
        }

        protected override bool CheckConditions(Player player, ADVSave save) => Cyberspace.Active;

        protected override void Build() {
            ADV.DialogueBoxs.DialogueBoxBase.RegisterPortrait(RoleName.Value, texture: null);
            ADV.DialogueBoxs.DialogueBoxBase.SetPortraitStyle(RoleName.Value, silhouette: false);

            int layer = Cyberspace.CurrentLayer;
            bool isMaxLayer = layer >= Cyberspace.MaxLayerCount;

            string introText = string.Format(Line_Intro.Value, layer);
            Add(RoleName.Value, introText, onStart: () => {
                ADV.DialogueBoxs.Styles.SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                if (ADV.DialogueBoxs.Styles.SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                    portrait.SkipFadeIn();
                    portrait.currentFace = ShepelFullBodyPortrait.Face.Serious;
                }
            });

            if (isMaxLayer) {
                Add(RoleName.Value, Line_MaxLayer.Value, onStart: () => {
                    if (ADV.DialogueBoxs.Styles.SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                        portrait.currentFace = ShepelFullBodyPortrait.Face.Shocked;
                    }
                }, onComplete: Complete);
            }
            else {
                Add(RoleName.Value, Line_LayerReport.Value);
                Add(RoleName.Value, Line_Warning.Value, onComplete: Complete);
            }
        }

        protected override void OnScenarioStart() {
            ADV.DialogueBoxs.Styles.SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
            if (ADV.DialogueBoxs.Styles.SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                portrait.SkipFadeIn();
                portrait.currentFace = ShepelFullBodyPortrait.Face.None;
            }
        }
    }
}
