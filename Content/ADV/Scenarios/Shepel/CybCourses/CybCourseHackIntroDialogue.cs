using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.ADV.DialogueBoxs.Styles;
using CalamityOverhaul.Content.ADV.Scenarios;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.CybCourses
{
    //SHPC在SHPC HUD教学结束后介绍骇客时间的场景
    //由HackTimeTutorialLead.AutoTriggerHackIntro在检测到SHPCTutorialStep=-1时自动触发
    internal class CybCourseHackIntroDialogue : ADVScenarioBase, ILocalizedModType
    {
        public new string LocalizationCategory => "ADV.Shepel";

        public static LocalizedText SpeakerName { get; private set; }
        public static LocalizedText Line1 { get; private set; }
        public static LocalizedText Line2 { get; private set; }
        public static LocalizedText Line3 { get; private set; }

        protected override Func<DialogueBoxBase> DefaultDialogueStyle
            => () => SHPCDialogueBox.Instance;

        public override void SetStaticDefaults() {
            SpeakerName = this.GetLocalization(nameof(SpeakerName), () => "SHPC");
            Line1 = this.GetLocalization(nameof(Line1),
                () => "接口解析完毕。下一项训练：骇客时间。");
            Line2 = this.GetLocalization(nameof(Line2),
                () => "骇客时间是SHPC专属的神经干预协议。激活后，外部时间流将冻结，你可以从容选择目标并上传定制骇入程序。默认按键是 [N]。");
            Line3 = this.GetLocalization(nameof(Line3),
                () => "前方的测试单元已固定就位。按下 [N] 进入骇客时间，然后点击锁定它。");
        }

        protected override void Build() {
            DialogueBoxBase.RegisterPortrait(SpeakerName.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(SpeakerName.Value, silhouette: false);

            AddTimed(SpeakerName.Value, Line1.Value, 4.5f, onStart: OnStart);
            AddTimed(SpeakerName.Value, Line2.Value, 6.5f);
            Add(SpeakerName.Value, Line3.Value);
        }

        private void OnStart() {
            SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
            if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                portrait.currentFace = ShepelFullBodyPortrait.Face.Happy;
            }
        }

        protected override void OnScenarioComplete() {
            HackTimeTutorialLead.BeginHackTimeTutorial();
        }
    }
}
