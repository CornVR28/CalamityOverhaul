using CalamityOverhaul.Content.ADV.DialogueBoxs;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.Dialogues.Reactive.Bosses
{
    //月亮领主是原版最终Boss，Shepel给予最高规格的战后评估
    internal class ShepelMoonLordDialogue : ShepelReactiveDialogueBase
    {
        protected override ShepelReactiveEvent HandledEvent => ShepelReactiveEvent.BossDefeated;
        protected override int TargetBossNpcType => NPCID.MoonLordCore;

        public static LocalizedText RoleName { get; private set; }
        public static LocalizedText Line1 { get; private set; }
        public static LocalizedText Line2 { get; private set; }
        public static LocalizedText Line3 { get; private set; }

        public override void SetStaticDefaults() {
            RoleName = this.GetLocalization(nameof(RoleName), () => "SHPC");
            Line1 = this.GetLocalization(nameof(Line1),
                () => "古代实体确认终止。主人，那是我迄今记录过的最高威胁等级目标。");
            Line2 = this.GetLocalization(nameof(Line2),
                () => "星空的电磁辐射正在逐渐稳定。我的系统也全面恢复了正常。");
            Line3 = this.GetLocalization(nameof(Line3),
                () => "干得漂亮。不过要提醒主人一句，这个世界还有更深的层次，我正在更新参数。");
        }

        protected override void Build() {
            DialogueBoxBase.RegisterPortrait(RoleName.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(RoleName.Value, silhouette: false);
            ShepelADVData data = Main.LocalPlayer.GetModPlayer<ADVSavePlayer>().ADVSave.Get<ShepelADVData>();
            ConsumeEvent(data);

            Add(RoleName.Value, Line1.Value,
                onStart: () => ShowPortraitWithFace(ShepelFullBodyPortrait.Face.Serious));
            Add(RoleName.Value, Line2.Value);
            Add(RoleName.Value, Line3.Value,
                onStart: () => SetPortraitFace(ShepelFullBodyPortrait.Face.Happy),
                onComplete: Complete);
        }
    }
}
