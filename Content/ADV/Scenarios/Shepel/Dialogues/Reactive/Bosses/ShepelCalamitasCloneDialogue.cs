using CalamityOverhaul.Content.ADV.DialogueBoxs;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.Dialogues.Reactive.Bosses
{
    //灾厄克隆体是大boss，Shepel特别提醒这只是副本，真正的灾厄还在更深处
    internal class ShepelCalamitasCloneDialogue : ShepelReactiveDialogueBase
    {
        protected override ShepelReactiveEvent HandledEvent => ShepelReactiveEvent.BossDefeated;
        protected override int TargetBossNpcType => CWRID.NPC_CalamitasClone;

        public static LocalizedText RoleName { get; private set; }
        public static LocalizedText Line1 { get; private set; }
        public static LocalizedText Line2 { get; private set; }
        public static LocalizedText Line3 { get; private set; }

        public override void SetStaticDefaults() {
            RoleName = this.GetLocalization(nameof(RoleName), () => "SHPC");
            Line1 = this.GetLocalization(nameof(Line1),
                () => "灾厄克隆体：终止。主人请注意，扫描确认，这只是个副本。");
            Line2 = this.GetLocalization(nameof(Line2),
                () => "原体的信号仍然存在，位置更深，威胁等级远超这个复刻版本。如果说这只是预演……");
            Line3 = this.GetLocalization(nameof(Line3),
                () => "我已将真正的灾厄列为最高优先监控目标。主人，请保持警惕。");
        }

        protected override void Build() {
            DialogueBoxBase.RegisterPortrait(RoleName.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(RoleName.Value, silhouette: false);
            ShepelADVData data = Main.LocalPlayer.GetModPlayer<ADVSavePlayer>().ADVSave.Get<ShepelADVData>();
            ConsumeEvent(data);

            Add(RoleName.Value, Line1.Value,
                onStart: () => ShowPortraitWithFace(ShepelFullBodyPortrait.Face.Serious));
            Add(RoleName.Value, Line2.Value);
            Add(RoleName.Value, Line3.Value, onComplete: Complete);
        }
    }
}
