using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.ADV.DialogueBoxs.Styles;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.Dialogues.Reactive.Bosses
{
    //真灾厄是灾厄mod的最终Boss，Shepel给出最高规格的情绪反应
    internal class ShepelSupremeCalamitasDialogue : ShepelReactiveDialogueBase
    {
        protected override ShepelReactiveEvent HandledEvent => ShepelReactiveEvent.BossDefeated;
        protected override int TargetBossNpcType => CWRID.NPC_SupremeCalamitas;

        public static LocalizedText RoleName { get; private set; }
        public static LocalizedText Line1 { get; private set; }
        public static LocalizedText Line2 { get; private set; }
        public static LocalizedText Line3 { get; private set; }

        public override void SetStaticDefaults() {
            RoleName = this.GetLocalization(nameof(RoleName), () => "SHPC");
            Line1 = this.GetLocalization(nameof(Line1),
                () => "真正的灾厄，确认终止。我的全域威胁监控显示：危险等级归零。");
            Line2 = this.GetLocalization(nameof(Line2),
                () => "主人……我不知道该如何准确表达此刻的感受。所有预测模型里最坏的结局都没有发生，因为您在这里。");
            Line3 = this.GetLocalization(nameof(Line3),
                () => "这段数据我会永久保存，不设覆写权限。");
        }

        protected override void Build() {
            DialogueBoxBase.RegisterPortrait(RoleName.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(RoleName.Value, silhouette: false);
            ShepelADVData data = Main.LocalPlayer.GetModPlayer<ADVSavePlayer>().ADVSave.Get<ShepelADVData>();
            ConsumeEvent(data);

            Add(RoleName.Value, Line1.Value,
                onStart: () => ShowPortraitWithFace(ShepelFullBodyPortrait.Face.Serious));
            Add(RoleName.Value, Line2.Value, onStart: () => {
                SetPortraitFace(ShepelFullBodyPortrait.Face.Happy);
                if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait)
                    portrait.TriggerGlitch(0.3f, 0.2f);
                SoundEngine.PlaySound(SoundID.MenuTick);
            });
            Add(RoleName.Value, Line3.Value, onComplete: Complete);
        }
    }
}
