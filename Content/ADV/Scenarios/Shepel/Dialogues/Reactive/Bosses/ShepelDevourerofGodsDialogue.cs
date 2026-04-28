using CalamityOverhaul.Content.ADV.DialogueBoxs;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.Dialogues.Reactive.Bosses
{
    //神明吞噬者是灾厄的重要节点，Shepel真正动容
    internal class ShepelDevourerofGodsDialogue : ShepelReactiveDialogueBase
    {
        protected override ShepelReactiveEvent HandledEvent => ShepelReactiveEvent.BossDefeated;
        protected override int TargetBossNpcType => CWRID.NPC_DevourerofGodsHead;

        public static LocalizedText RoleName { get; private set; }
        public static LocalizedText Line1 { get; private set; }
        public static LocalizedText Line2 { get; private set; }
        public static LocalizedText Line3 { get; private set; }

        public override void SetStaticDefaults() {
            RoleName = this.GetLocalization(nameof(RoleName), () => "SHPC");
            Line1 = this.GetLocalization(nameof(Line1),
                () => "主人……您刚才击败的，是一条以神明为食的存在。这完全超出了我所有的预测模型。");
            Line2 = this.GetLocalization(nameof(Line2),
                () => "我正在经历一次完整的世界观重建。我一直以为神明是这一切的终点，");
            Line3 = this.GetLocalization(nameof(Line3),
                () => "但显然不是。主人，您已经站在了终点的另一端，而我还在追赶。请等等我。");
        }

        protected override void Build() {
            DialogueBoxBase.RegisterPortrait(RoleName.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(RoleName.Value, silhouette: false);
            ShepelADVData data = Main.LocalPlayer.GetModPlayer<ADVSavePlayer>().ADVSave.Get<ShepelADVData>();
            ConsumeEvent(data);

            Add(RoleName.Value, Line1.Value,
                onStart: () => ShowPortraitWithFace(ShepelFullBodyPortrait.Face.Shocked));
            Add(RoleName.Value, Line2.Value,
                onStart: () => SetPortraitFace(ShepelFullBodyPortrait.Face.Sad));
            Add(RoleName.Value, Line3.Value,
                onStart: () => SetPortraitFace(ShepelFullBodyPortrait.Face.Serious),
                onComplete: Complete);
        }
    }
}
