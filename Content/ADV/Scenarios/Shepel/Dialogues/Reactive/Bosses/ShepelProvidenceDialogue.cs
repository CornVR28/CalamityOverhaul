using CalamityOverhaul.Content.ADV.DialogueBoxs;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.Dialogues.Reactive.Bosses
{
    //普罗维登斯是神明级目标，Shepel感到真正的震惊
    internal class ShepelProvidenceDialogue : ShepelReactiveDialogueBase
    {
        protected override ShepelReactiveEvent HandledEvent => ShepelReactiveEvent.BossDefeated;
        protected override int TargetBossNpcType => CWRID.NPC_Providence;

        public static LocalizedText RoleName { get; private set; }
        public static LocalizedText Line1 { get; private set; }
        public static LocalizedText Line2 { get; private set; }
        public static LocalizedText Line3 { get; private set; }

        public override void SetStaticDefaults() {
            RoleName = this.GetLocalization(nameof(RoleName), () => "SHPC");
            Line1 = this.GetLocalization(nameof(Line1),
                () => "神圣实体：确认终止……主人，我需要一点时间，重新校准一下。");
            Line2 = this.GetLocalization(nameof(Line2),
                () => "在我的模型里，'神明'这一类别被标注为理论级威胁。从未预期会真正遭遇，更别说击败了。");
            Line3 = this.GetLocalization(nameof(Line3),
                () => "新的参考系已经建立。我在更新所有战斗评估算法。主人，您继续突破边界，我来跟上。");
        }

        protected override void Build() {
            DialogueBoxBase.RegisterPortrait(RoleName.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(RoleName.Value, silhouette: false);
            ShepelADVData data = Main.LocalPlayer.GetModPlayer<ADVSavePlayer>().ADVSave.Get<ShepelADVData>();
            ConsumeEvent(data);

            Add(RoleName.Value, Line1.Value,
                onStart: () => ShowPortraitWithFace(ShepelFullBodyPortrait.Face.Shocked));
            Add(RoleName.Value, Line2.Value,
                onStart: () => SetPortraitFace(ShepelFullBodyPortrait.Face.Serious));
            Add(RoleName.Value, Line3.Value, onComplete: Complete);
        }
    }
}
