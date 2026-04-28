using CalamityOverhaul.Content.ADV.DialogueBoxs;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.Dialogues.Reactive.Bosses
{
    //双子眼，Retinazer或Spazmatism任意一只最后死亡均可触发
    internal class ShepelTwinsDialogue : ShepelReactiveDialogueBase
    {
        protected override ShepelReactiveEvent HandledEvent => ShepelReactiveEvent.BossDefeated;

        public static LocalizedText RoleName { get; private set; }
        public static LocalizedText Line1 { get; private set; }
        public static LocalizedText Line2 { get; private set; }

        public override void SetStaticDefaults() {
            RoleName = this.GetLocalization(nameof(RoleName), () => "SHPC");
            Line1 = this.GetLocalization(nameof(Line1),
                () => "两台机械眼双双离线。成对互补的设计，协同战术值得记一下。");
            Line2 = this.GetLocalization(nameof(Line2),
                () => "终究只是末日机械的粗糙复刻。跟我比起来，相当原始。");
        }

        //双子眼可能以任意一只的NPC类型触发BossDefeated，需要同时检测两种类型
        protected override bool CheckConditions(Player player, ADVSave save) {
            ShepelADVData data = save.Get<ShepelADVData>();
            return ShepelReactiveEvents.HasFlag(data, HandledEvent)
                && (data.LastDefeatedBossNpcType == NPCID.Retinazer
                    || data.LastDefeatedBossNpcType == NPCID.Spazmatism);
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
