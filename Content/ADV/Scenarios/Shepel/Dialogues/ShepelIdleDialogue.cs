using CalamityOverhaul.Content.ADV.ADVChoices;
using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.ADV.DialogueBoxs.Styles;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.Dialogues
{
    /// <summary>
    /// 空闲状态下的日常对话，优先级最低作为兜底
    /// 每次触发都会轮换一段台词，让重复对话保持新鲜感
    /// </summary>
    internal class ShepelIdleDialogue : SHPCDialogueScenarioBase, ILocalizedModType
    {
        public new string LocalizationCategory => "ADV.Shepel";
        public override int DialoguePriority => 0;

        public static LocalizedText RoleName { get; private set; }

        public static LocalizedText Idle0_Line1 { get; private set; }
        public static LocalizedText Idle0_Reply { get; private set; }
        public static LocalizedText Idle0_Choice_HowAreYou { get; private set; }
        public static LocalizedText Idle0_Choice_Nothing { get; private set; }
        public static LocalizedText Idle0_HowAreYou_Response { get; private set; }
        public static LocalizedText Idle0_Nothing_Response { get; private set; }

        public static LocalizedText Idle1_Line1 { get; private set; }
        public static LocalizedText Idle1_Line2 { get; private set; }

        public static LocalizedText Idle2_Line1 { get; private set; }
        public static LocalizedText Idle2_Line2 { get; private set; }

        //轮换计数器从ShepelADVData.IdleVariantSeed读写，持久化跨存档
        protected override Func<DialogueBoxBase> DefaultDialogueStyle
            => () => SHPCDialogueBox.Instance;

        public override void SetStaticDefaults() {
            RoleName = this.GetLocalization(nameof(RoleName), () => "SHPC");

            Idle0_Line1 = this.GetLocalization(nameof(Idle0_Line1),
                () => "主人，有什么需要我协助的吗？");
            Idle0_Reply = this.GetLocalization(nameof(Idle0_Reply),
                () => "系统运行正常，所有模块待命中。");
            Idle0_Choice_HowAreYou = this.GetLocalization(nameof(Idle0_Choice_HowAreYou),
                () => "你还好吗？");
            Idle0_Choice_Nothing = this.GetLocalization(nameof(Idle0_Choice_Nothing),
                () => "没事，随便看看");
            Idle0_HowAreYou_Response = this.GetLocalization(nameof(Idle0_HowAreYou_Response),
                () => "我很好，主人。只要您在，我就在。");
            Idle0_Nothing_Response = this.GetLocalization(nameof(Idle0_Nothing_Response),
                () => "好的，有需要随时呼叫。");

            Idle1_Line1 = this.GetLocalization(nameof(Idle1_Line1),
                () => "主人，您似乎在思考什么问题。");
            Idle1_Line2 = this.GetLocalization(nameof(Idle1_Line2),
                () => "如果是关于SHPC的性能数据，我可以随时调出报告。");

            Idle2_Line1 = this.GetLocalization(nameof(Idle2_Line1),
                () => "主人，这附近的电磁环境有些不稳定。");
            Idle2_Line2 = this.GetLocalization(nameof(Idle2_Line2),
                () => "建议保持警惕，SHPC随时准备接管应急协议。");
        }

        protected override void Build() {
            DialogueBoxBase.RegisterPortrait(RoleName.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(RoleName.Value, silhouette: false);

            ShepelADVData data = Main.LocalPlayer.GetModPlayer<ADVSavePlayer>().ADVSave.Get<ShepelADVData>();
            int variant = data.IdleVariantSeed % 3;
            data.IdleVariantSeed++;

            switch (variant) {
                case 0:
                    BuildVariant0();
                    break;
                case 1:
                    BuildVariant1();
                    break;
                default:
                    BuildVariant2();
                    break;
            }
        }

        private void BuildVariant0() {
            AddWithChoices(RoleName.Value, Idle0_Line1.Value, [
                new Choice(Idle0_Choice_HowAreYou.Value, OnChoiceHowAreYou),
                new Choice(Idle0_Choice_Nothing.Value, OnChoiceNothing),
            ], choiceBoxStyle: ADVChoiceBox.ChoiceBoxStyle.SHPC);
        }

        private void OnChoiceHowAreYou() {
            ScenarioManager.Start<ShepelIdleDialogue_HowAreYou>();
            Complete();
        }

        private void OnChoiceNothing() {
            ScenarioManager.Start<ShepelIdleDialogue_Nothing>();
            Complete();
        }

        private void BuildVariant1() {
            Add(RoleName.Value, Idle1_Line1.Value, onStart: () => {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                    portrait.SkipFadeIn();
                }
            });
            Add(RoleName.Value, Idle1_Line2.Value, onComplete: Complete);
        }

        private void BuildVariant2() {
            Add(RoleName.Value, Idle2_Line1.Value, onStart: () => {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                    portrait.SkipFadeIn();
                    portrait.currentFace = ShepelFullBodyPortrait.Face.Serious;
                }
            });
            Add(RoleName.Value, Idle2_Line2.Value, onComplete: Complete);
        }

        protected override void OnScenarioStart() {
            SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
            if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                portrait.SkipFadeIn();
                portrait.currentFace = ShepelFullBodyPortrait.Face.None;
            }
        }

        //选择分支子场景：关心状态
        private class ShepelIdleDialogue_HowAreYou : ADVScenarioBase
        {
            public override string Key => nameof(ShepelIdleDialogue_HowAreYou);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle
                => () => SHPCDialogueBox.Instance;
            protected override void Build() {
                Add(RoleName.Value, Idle0_Reply.Value);
                Add(RoleName.Value, Idle0_HowAreYou_Response.Value, onStart: () => {
                    if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                        portrait.currentFace = ShepelFullBodyPortrait.Face.Happy;
                    }
                }, onComplete: Complete);
            }
            protected override void OnScenarioStart() {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                    portrait.SkipFadeIn();
                }
            }
        }

        //选择分支子场景：随便看看
        private class ShepelIdleDialogue_Nothing : ADVScenarioBase
        {
            public override string Key => nameof(ShepelIdleDialogue_Nothing);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle
                => () => SHPCDialogueBox.Instance;
            protected override void Build() {
                Add(RoleName.Value, Idle0_Nothing_Response.Value, onComplete: Complete);
            }
            protected override void OnScenarioStart() {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                    portrait.SkipFadeIn();
                    portrait.currentFace = ShepelFullBodyPortrait.Face.Smirk;
                }
            }
        }
    }
}
