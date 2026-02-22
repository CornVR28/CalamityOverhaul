using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.ADV.DialogueBoxs.Styles;
using System;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors
{
    /// <summary>
    /// 阿波利娅靠近玩家后的对话场景
    /// </summary>
    internal class ApolliaDialogueScenario : ADVScenarioBase, ILocalizedModType
    {
        public override string Key => nameof(ApolliaDialogueScenario);
        protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => StarStreamDialogueBox.Instance;

        public static LocalizedText Rolename { get; private set; }
        public static LocalizedText Line1 { get; private set; }
        public static LocalizedText Line2 { get; private set; }

        public override void SetStaticDefaults() {
            Rolename = this.GetLocalization(nameof(Rolename), () => "阿波利娅");
            Line1 = this.GetLocalization(nameof(Line1), () => "……系统自检完毕，生物特征锁定。你就是刚刚从空降仓里弹出来的那位？");
            Line2 = this.GetLocalization(nameof(Line2), () => "嘉登博士让我在这里等你。看起来一切都在他的计划之中——除了你的着陆姿势");
        }

        protected override void OnScenarioStart() {
            StarStreamDialogueBox.Instance?.ShowFullBodyPortrait<ApolliaFullBodyPortrait>();
        }

        protected override void Build() {
            DialogueBoxBase.RegisterPortrait(Rolename.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(Rolename.Value, silhouette: true);

            Add(Rolename.Value, Line1.Value);
            Add(Rolename.Value, Line2.Value);
        }
    }
}
