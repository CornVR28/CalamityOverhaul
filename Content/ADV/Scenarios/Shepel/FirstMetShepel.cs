using CalamityOverhaul.Content.ADV.ADVChoices;
using CalamityOverhaul.Content.ADV.Common;
using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.ADV.DialogueBoxs.Styles;
using System;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel
{
    /// <summary>
    /// 初遇SHPC场景
    /// </summary>
    internal class FirstMetShepel : ADVScenarioBase, ILocalizedModType
    {
        //角色名称
        public static LocalizedText RolenameSHPC { get; private set; }
        //对话文本
        public static LocalizedText Line1 { get; private set; }

        //选择分支
        public static LocalizedText Choice1Text { get; private set; }
        public static LocalizedText Choice2Text { get; private set; }
        public static LocalizedText Choice1Silence { get; private set; }
        public static LocalizedText Choice1Response { get; private set; }

        //使用SHPC专属赛博女仆风格
        protected override Func<DialogueBoxBase> DefaultDialogueStyle
            => () => SHPCDialogueBox.Instance;

        public override void SetStaticDefaults() {
            RolenameSHPC = this.GetLocalization(nameof(RolenameSHPC), () => "SHPC");
            Line1 = this.GetLocalization(nameof(Line1), () => "主人！很高兴再见到您！");
            Choice1Text = this.GetLocalization(nameof(Choice1Text), () => "你认错人了吧？");
            Choice2Text = this.GetLocalization(nameof(Choice2Text), () => "...好久不见");
            Choice1Silence = this.GetLocalization(nameof(Choice1Silence), () => "......");
            Choice1Response = this.GetLocalization(nameof(Choice1Response), () => "...是的，只要是您，我每次都愿意认错");
        }

        protected override void Build() {
            DialogueBoxBase.RegisterPortrait(RolenameSHPC.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(RolenameSHPC.Value, silhouette: false);

            //对话 + 选择
            AddWithChoices(RolenameSHPC.Value, Line1.Value, [
                new Choice(Choice1Text.Value, Choice1),
                new Choice(Choice2Text.Value, Choice2),
            ], choiceBoxStyle: ADVChoiceBox.ChoiceBoxStyle.SHPC);
        }

        protected override void OnScenarioStart() {
            SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
            if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                portrait.SkipFadeIn();
                portrait.currentFace = ShepelFullBodyPortrait.Face.None;
            }
        }

        public void Choice1() {
            ScenarioManager.Start<FirstMetShepel_Choice1>();
            Complete();
        }

        private class FirstMetShepel_Choice1 : ADVScenarioBase
        {
            public override string Key => nameof(FirstMetShepel_Choice1);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle
                => () => SHPCDialogueBox.Instance;
            protected override void Build() {
                Add(RolenameSHPC.Value, Choice1Silence.Value, onStart: () => {
                    if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                        portrait.TriggerGlitch(1f, 1f);
                    }
                });
                Add(RolenameSHPC.Value, Choice1Response.Value, onStart: () => {
                    if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                        portrait.currentFace = ShepelFullBodyPortrait.Face.Smirk;
                    }
                });
            }
            protected override void OnScenarioStart() {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                    portrait.SkipFadeIn();
                    portrait.currentFace = ShepelFullBodyPortrait.Face.None;
                }
            }
        }

        public void Choice2() {
            //TODO: 选择2的后续场景
            Complete();
        }
    }
}
