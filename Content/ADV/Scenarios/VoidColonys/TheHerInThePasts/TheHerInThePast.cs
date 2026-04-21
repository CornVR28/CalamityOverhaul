using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.ADV.DialogueBoxs.Styles;
using CalamityOverhaul.Content.ADV.Scenarios.Shepel;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.GlitchWraith;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TimeShift;
using InnoVault.Actors;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TheHerInThePasts
{
    //在制作这个的过场中我他妈意识到千万别试图不靠选项分支来启动子场景，因为目前的对话框框架根本不支持你从一个对话场景中直接启动另一个新的场景，会有他妈的一堆bug！
    internal class TheHerInThePast : ADVScenarioBase, ILocalizedModType, IWorldInfo
    {
        public override string Key => nameof(TheHerInThePast);
        protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => BrimstoneDialogueBox.Instance;

        private static int triggerDelay;

        public static LocalizedText RolenameWitch { get; private set; }
        public static LocalizedText RolenameSHPC { get; private set; }

        public static LocalizedText WitchLine1 { get; private set; }
        public static LocalizedText ShpcLine1 { get; private set; }
        public static LocalizedText WitchLine2 { get; private set; }
        public static LocalizedText WitchLine3 { get; private set; }
        public static LocalizedText ShpcLine2 { get; private set; }
        public static LocalizedText WitchLine4 { get; private set; }
        public static LocalizedText WitchLine5 { get; private set; }
        public static LocalizedText ShpcLine3 { get; private set; }
        public static LocalizedText WitchLine6 { get; private set; }
        public static LocalizedText WitchLine7 { get; private set; }
        public static LocalizedText ShpcLine4 { get; private set; }

        void IWorldInfo.OnWorldLoad() {
            triggerDelay = 0;
        }

        public override void SetStaticDefaults() {
            RolenameWitch = this.GetLocalization(nameof(RolenameWitch), () => "硫火女巫");
            RolenameSHPC = this.GetLocalization(nameof(RolenameSHPC), () => "SHPC");

            WitchLine1 = this.GetLocalization(nameof(WitchLine1),
                () => "被这种脏东西追挺不好受的吧，幸好你们遇到了我。");
            ShpcLine1 = this.GetLocalization(nameof(ShpcLine1),
                () => "谢谢……但怎么可能，你属于过去，却可以动起来……");
            WitchLine2 = this.GetLocalization(nameof(WitchLine2),
                () => "你们可以从百年后入侵到这里，我自然也能做到类似的事情。");
            WitchLine3 = this.GetLocalization(nameof(WitchLine3),
                () => "不过我确实只是一个留在此处的影子。");
            ShpcLine2 = this.GetLocalization(nameof(ShpcLine2),
                () => "过去任意时间点的自己都可以独立使用能力吗？！");
            WitchLine4 = this.GetLocalization(nameof(WitchLine4),
                () => "你的关注点真可爱……快点离开吧，这些脏东西，还是和我一起葬在过去比较好。");
            WitchLine5 = this.GetLocalization(nameof(WitchLine5),
                () => "对了，小女仆。");
            ShpcLine3 = this.GetLocalization(nameof(ShpcLine3),
                () => "什么？");
            WitchLine6 = this.GetLocalization(nameof(WitchLine6),
                () => "你的疲惫简直透彻骨髓呢。");
            WitchLine7 = this.GetLocalization(nameof(WitchLine7),
                () => "别总是想着改变过去，要学会接受未来。");
            ShpcLine4 = this.GetLocalization(nameof(ShpcLine4),
                () => "……");
        }

        //取当前活动立绘，场景内所有切换都在它上面操作
        private static TheHerInThePastPortrait GetPortrait()
            => BrimstoneDialogueBox.Instance?.GetActiveFullBodyPortrait() as TheHerInThePastPortrait;

        //切到女巫，并推进着色
        private static void ToWitch(float coloration) {
            var portrait = GetPortrait();
            if (portrait == null) return;
            portrait.SwitchTo(TheHerInThePastPortrait.Role.Witch);
            if (!portrait.IsDissolving) portrait.SetColoration(coloration);
        }

        //切到SHPC，并设置表情与可选故障
        private static void ToSHPC(ShepelFullBodyPortrait.Face face, bool glitch = false) {
            var portrait = GetPortrait();
            if (portrait == null) return;
            portrait.SwitchTo(TheHerInThePastPortrait.Role.SHPC);
            portrait.SetSHPCFace(face);
            if (glitch) portrait.TriggerGlitch(0.6f, 0.7f);
        }

        protected override void OnScenarioStart() {
            BrimstoneDialogueBox.Instance?.ShowFullBodyPortrait<TheHerInThePastPortrait>();
            var portrait = GetPortrait();
            portrait?.SkipFadeIn();
            portrait?.SwitchTo(TheHerInThePastPortrait.Role.Witch);
            portrait?.SetColoration(0.35f);
        }

        protected override void Build() {
            //女巫1——起始着色
            Add(RolenameWitch.Value, WitchLine1.Value,
                onStart: () => ToWitch(0.35f));
            //SHPC1——震惊+故障
            Add(RolenameSHPC.Value, ShpcLine1.Value,
                onStart: () => ToSHPC(ShepelFullBodyPortrait.Face.Shocked, glitch: true));
            //女巫2
            Add(RolenameWitch.Value, WitchLine2.Value,
                onStart: () => ToWitch(0.5f));
            //女巫3——继续推进
            Add(RolenameWitch.Value, WitchLine3.Value,
                onStart: () => ToWitch(0.65f));
            //SHPC2——继续震惊
            Add(RolenameSHPC.Value, ShpcLine2.Value,
                onStart: () => ToSHPC(ShepelFullBodyPortrait.Face.Shocked));
            //女巫4
            Add(RolenameWitch.Value, WitchLine4.Value,
                onStart: () => ToWitch(0.8f));
            //女巫5
            Add(RolenameWitch.Value, WitchLine5.Value,
                onStart: () => ToWitch(0.9f));
            //SHPC3——严肃
            Add(RolenameSHPC.Value, ShpcLine3.Value,
                onStart: () => ToSHPC(ShepelFullBodyPortrait.Face.Serious));
            //女巫6
            Add(RolenameWitch.Value, WitchLine6.Value,
                onStart: () => ToWitch(1f));
            //女巫7
            Add(RolenameWitch.Value, WitchLine7.Value,
                onStart: () => ToWitch(1f));
            //SHPC4——沉默收尾
            Add(RolenameSHPC.Value, ShpcLine4.Value,
                onStart: () => ToSHPC(ShepelFullBodyPortrait.Face.Sad));
        }

        protected override void OnScenarioComplete() {
            WitchStatueActor.Current?.BeginDissolve();
            var portrait = GetPortrait();
            //结束前确保在女巫形态上完成像素剥落
            portrait?.SwitchTo(TheHerInThePastPortrait.Role.Witch);
            portrait?.StartPixelDissolve();

            foreach (var wraith in ActorLoader.GetActiveActors<GlitchWraithActor>()) {
                wraith?.ApplySelfDismember();
            }

            if (Main.LocalPlayer.TryGetADVSave(out var save)) {
                save.Get<VoidColonyADVData>().TheHerInThePast = true;
            }
        }

        public override void Update(ADVSave save, Player player) {
            if (save.Get<VoidColonyADVData>().TheHerInThePast) return;
            if (!VoidColony.Active) return;
            if (!VoidTimeShiftSystem.InPast) return;

            var statue = WitchStatueActor.Current;
            if (statue == null || !statue.IsSuppressing) {
                triggerDelay = 0;
                return;
            }

            if (++triggerDelay < 45) return;

            if (StartScenario()) {
                save.Get<VoidColonyADVData>().TheHerInThePast = true;
                triggerDelay = 0;
            }
        }
    }
}