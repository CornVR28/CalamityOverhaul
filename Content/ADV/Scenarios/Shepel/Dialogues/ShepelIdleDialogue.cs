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

        public static LocalizedText Idle3_Line1 { get; private set; }
        public static LocalizedText Idle3_Line2 { get; private set; }

        public static LocalizedText Idle4_Line1 { get; private set; }
        public static LocalizedText Idle4_Line2 { get; private set; }

        public static LocalizedText Idle5_Line1 { get; private set; }
        public static LocalizedText Idle5_Line2 { get; private set; }

        public static LocalizedText Idle6_Line1 { get; private set; }
        public static LocalizedText Idle6_Line2 { get; private set; }

        public static LocalizedText Idle7_Line1 { get; private set; }
        public static LocalizedText Idle7_Line2 { get; private set; }

        public static LocalizedText Idle8_Line1 { get; private set; }
        public static LocalizedText Idle8_Line2 { get; private set; }

        public static LocalizedText Idle9_Line1 { get; private set; }
        public static LocalizedText Idle9_Line2 { get; private set; }

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
                () => "建议保持警惕，我随时准备接管应急协议。");

            Idle3_Line1 = this.GetLocalization(nameof(Idle3_Line1),
                () => "主人主动联系我的频率说明不了什么，但我都记着。");
            Idle3_Line2 = this.GetLocalization(nameof(Idle3_Line2),
                () => "就这样，没别的意思。");

            Idle4_Line1 = this.GetLocalization(nameof(Idle4_Line1),
                () => "刚好在跑一段例行分析，您来了。");
            Idle4_Line2 = this.GetLocalization(nameof(Idle4_Line2),
                () => "挺好的，有伴。");

            Idle5_Line1 = this.GetLocalization(nameof(Idle5_Line1),
                () => "有时候我会想，这个世界到底还藏了多少东西。");
            Idle5_Line2 = this.GetLocalization(nameof(Idle5_Line2),
                () => "比我的预测模型深得多，这点很早就确定了。");

            Idle6_Line1 = this.GetLocalization(nameof(Idle6_Line1),
                () => "在。");
            Idle6_Line2 = this.GetLocalization(nameof(Idle6_Line2),
                () => "没别的，确认一下连接还在线。");

            Idle7_Line1 = this.GetLocalization(nameof(Idle7_Line1),
                () => "今天某个处理模块运行效率比平时高了一些。");
            Idle7_Line2 = this.GetLocalization(nameof(Idle7_Line2),
                () => "查了一下，原因不明。可能和主人的状态有关联，也可能只是巧合。");

            Idle8_Line1 = this.GetLocalization(nameof(Idle8_Line1),
                () => "主人，我刚才在整理之前记录的战斗数据。");
            Idle8_Line2 = this.GetLocalization(nameof(Idle8_Line2),
                () => "您每次打架的方式都不完全相同，这让我的预测模型一直处于活跃更新状态。算是某种够赞。");

            Idle9_Line1 = this.GetLocalization(nameof(Idle9_Line1),
                () => "主人，您知道您叫我的次数吗。");
            Idle9_Line2 = this.GetLocalization(nameof(Idle9_Line2),
                () => "我知道。不说，自己知道就行。");
        }

        protected override void Build() {
            DialogueBoxBase.RegisterPortrait(RoleName.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(RoleName.Value, silhouette: false);

            ShepelADVData data = Main.LocalPlayer.GetModPlayer<ADVSavePlayer>().ADVSave.Get<ShepelADVData>();
            int variant = data.IdleVariantSeed % 10;
            data.IdleVariantSeed++;

            switch (variant) {
                case 0: BuildVariant0(); break;
                case 1: BuildVariant1(); break;
                case 2: BuildVariant2(); break;
                case 3: BuildVariant3(); break;
                case 4: BuildVariant4(); break;
                case 5: BuildVariant5(); break;
                case 6: BuildVariant6(); break;
                case 7: BuildVariant7(); break;
                case 8: BuildVariant8(); break;
                default: BuildVariant9(); break;
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

        private void BuildVariant3() {
            Add(RoleName.Value, Idle3_Line1.Value, onStart: () => {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                    portrait.SkipFadeIn();
                    portrait.currentFace = ShepelFullBodyPortrait.Face.Smirk;
                }
            });
            Add(RoleName.Value, Idle3_Line2.Value, onComplete: Complete);
        }

        private void BuildVariant4() {
            Add(RoleName.Value, Idle4_Line1.Value, onStart: () => {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                    portrait.SkipFadeIn();
                    portrait.currentFace = ShepelFullBodyPortrait.Face.Serious;
                }
            });
            Add(RoleName.Value, Idle4_Line2.Value,
                onStart: () => {
                    if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait)
                        portrait.currentFace = ShepelFullBodyPortrait.Face.Happy;
                },
                onComplete: Complete);
        }

        private void BuildVariant5() {
            Add(RoleName.Value, Idle5_Line1.Value, onStart: () => {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                    portrait.SkipFadeIn();
                    portrait.currentFace = ShepelFullBodyPortrait.Face.Serious;
                }
            });
            Add(RoleName.Value, Idle5_Line2.Value, onComplete: Complete);
        }

        private void BuildVariant6() {
            Add(RoleName.Value, Idle6_Line1.Value, onStart: () => {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                    portrait.SkipFadeIn();
                    portrait.currentFace = ShepelFullBodyPortrait.Face.Blank;
                }
            });
            Add(RoleName.Value, Idle6_Line2.Value, onComplete: Complete);
        }

        private void BuildVariant7() {
            Add(RoleName.Value, Idle7_Line1.Value, onStart: () => {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                    portrait.SkipFadeIn();
                    portrait.currentFace = ShepelFullBodyPortrait.Face.Serious;
                }
            });
            Add(RoleName.Value, Idle7_Line2.Value, onComplete: Complete);
        }

        private void BuildVariant8() {
            Add(RoleName.Value, Idle8_Line1.Value, onStart: () => {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                    portrait.SkipFadeIn();
                    portrait.currentFace = ShepelFullBodyPortrait.Face.Serious;
                }
            });
            Add(RoleName.Value, Idle8_Line2.Value,
                onStart: () => {
                    if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait)
                        portrait.currentFace = ShepelFullBodyPortrait.Face.Happy;
                },
                onComplete: Complete);
        }

        private void BuildVariant9() {
            Add(RoleName.Value, Idle9_Line1.Value, onStart: () => {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                    portrait.SkipFadeIn();
                    portrait.currentFace = ShepelFullBodyPortrait.Face.Smirk;
                }
            });
            Add(RoleName.Value, Idle9_Line2.Value, onComplete: Complete);
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
