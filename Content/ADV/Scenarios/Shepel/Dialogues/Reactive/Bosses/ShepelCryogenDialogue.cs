using CalamityOverhaul.Content.ADV.DialogueBoxs;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.Dialogues.Reactive.Bosses
{
    internal class ShepelCryogenDialogue : ShepelReactiveDialogueBase
    {
        protected override ShepelReactiveEvent HandledEvent => ShepelReactiveEvent.BossDefeated;
        protected override int TargetBossNpcType => CWRID.NPC_Cryogen;

        public static LocalizedText RoleName { get; private set; }
        public static LocalizedText Line1 { get; private set; }
        public static LocalizedText Line2 { get; private set; }

        public override void SetStaticDefaults() {
            RoleName = this.GetLocalization(nameof(RoleName), () => "SHPC");
            Line1 = this.GetLocalization(nameof(Line1),
                () => "冷冻封印已突破。我的热成像传感器显示区域温度驾升，已回到正常范围。");
            Line2 = this.GetLocalization(nameof(Line2),
                () => "那股持续的绝对零度干扰终于消失了。我的某些传感器模块在它附近一直运行不稳定。总算舒服了。");
        }

        protected override void Build() {
            DialogueBoxBase.RegisterPortrait(RoleName.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(RoleName.Value, silhouette: false);
            ShepelADVData data = Main.LocalPlayer.GetModPlayer<ADVSavePlayer>().ADVSave.Get<ShepelADVData>();
            ConsumeEvent(data);

            Add(RoleName.Value, Line1.Value,
                onStart: () => ShowPortraitWithFace(ShepelFullBodyPortrait.Face.Serious));
            Add(RoleName.Value, Line2.Value,
                onStart: () => SetPortraitFace(ShepelFullBodyPortrait.Face.Smirk),
                onComplete: Complete);
        }
    }
}
