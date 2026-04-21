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
    /// <summary>
    /// 过去的她——虚空聚落过去时代的一段演出入口
    /// 按说话者切分为多个独立的嵌套子场景，链式推进：
    /// 父场景=女巫L1 → SHPC01 → 女巫02 → SHPC02 → 女巫03 → SHPC03 → 女巫04 → SHPC04
    /// 每个子场景独自声明 DefaultDialogueStyle 与初始立绘，切换对话框由 ADV 框架自动处理
    /// </summary>
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

        //父场景初始化女巫立绘，一次性执行
        protected override void OnScenarioStart() {
            BrimstoneDialogueBox.Instance?.ShowFullBodyPortrait<WitchPastFullBodyPortrait>();
            BrimstoneDialogueBox.Instance?.GetActiveFullBodyPortrait()?.SkipFadeIn();
            SetWitchColoration(0.35f);
        }

        protected override void Build() {
            Add(RolenameWitch.Value, WitchLine1.Value);
        }

        protected override void OnScenarioComplete() {
            ScenarioManager.Reset<Segment_SHPC_01>();
            ScenarioManager.Start<Segment_SHPC_01>();
        }

        //仅更新着色进度，不重复 ShowFullBodyPortrait
        protected static void SetWitchColoration(float coloration) {
            if (BrimstoneDialogueBox.Instance?.GetActiveFullBodyPortrait() is WitchPastFullBodyPortrait portrait
                && !portrait.IsDissolving) {
                portrait.SetColoration(coloration);
            }
        }

        //仅更新SHPC表情，不重复 ShowFullBodyPortrait
        protected static void SetSHPCFace(ShepelFullBodyPortrait.Face face, bool glitch = false) {
            if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                portrait.currentFace = face;
                if (glitch) portrait.TriggerGlitch(0.6f, 0.7f);
            }
        }

        //演出收尾：雕像剥落+立绘剥落+鬼乱码自毁+写存档
        protected static void FinishPerformance() {
            WitchStatueActor.Current?.BeginDissolve();
            if (BrimstoneDialogueBox.Instance?.GetActiveFullBodyPortrait() is WitchPastFullBodyPortrait portrait) {
                portrait.StartPixelDissolve();
            }

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
                //写入存档，避免多次触发
                save.Get<VoidColonyADVData>().TheHerInThePast = true;
                triggerDelay = 0;
            }
        }

        //SHPC第1段：识破身份前的震惊
        internal class Segment_SHPC_01 : ADVScenarioBase
        {
            public override string Key => nameof(Segment_SHPC_01);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => SHPCDialogueBox.Instance;

            protected override void OnScenarioStart() {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait()?.SkipFadeIn();
                SetSHPCFace(ShepelFullBodyPortrait.Face.Shocked, glitch: true);
            }

            protected override void Build() {
                Add(RolenameSHPC.Value, ShpcLine1.Value);
            }

            protected override void OnScenarioComplete() {
                ScenarioManager.Reset<Segment_Witch_02>();
                ScenarioManager.Start<Segment_Witch_02>();
            }
        }

        //女巫第2段：解释能动+影子自白
        internal class Segment_Witch_02 : ADVScenarioBase
        {
            public override string Key => nameof(Segment_Witch_02);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => BrimstoneDialogueBox.Instance;

            protected override void OnScenarioStart() {
                BrimstoneDialogueBox.Instance?.ShowFullBodyPortrait<WitchPastFullBodyPortrait>();
                BrimstoneDialogueBox.Instance?.GetActiveFullBodyPortrait()?.SkipFadeIn();
                SetWitchColoration(0.5f);
            }

            protected override void Build() {
                Add(RolenameWitch.Value, WitchLine2.Value);
                Add(RolenameWitch.Value, WitchLine3.Value,
                    onStart: () => SetWitchColoration(0.65f));
            }

            protected override void OnScenarioComplete() {
                ScenarioManager.Reset<Segment_SHPC_02>();
                ScenarioManager.Start<Segment_SHPC_02>();
            }
        }

        //SHPC第2段：追问
        internal class Segment_SHPC_02 : ADVScenarioBase
        {
            public override string Key => nameof(Segment_SHPC_02);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => SHPCDialogueBox.Instance;

            protected override void OnScenarioStart() {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait()?.SkipFadeIn();
                SetSHPCFace(ShepelFullBodyPortrait.Face.Shocked);
            }

            protected override void Build() {
                Add(RolenameSHPC.Value, ShpcLine2.Value);
            }

            protected override void OnScenarioComplete() {
                ScenarioManager.Reset<Segment_Witch_03>();
                ScenarioManager.Start<Segment_Witch_03>();
            }
        }

        //女巫第3段：打趣+叫住SHPC
        internal class Segment_Witch_03 : ADVScenarioBase
        {
            public override string Key => nameof(Segment_Witch_03);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => BrimstoneDialogueBox.Instance;

            protected override void OnScenarioStart() {
                BrimstoneDialogueBox.Instance?.ShowFullBodyPortrait<WitchPastFullBodyPortrait>();
                BrimstoneDialogueBox.Instance?.GetActiveFullBodyPortrait()?.SkipFadeIn();
                SetWitchColoration(0.8f);
            }

            protected override void Build() {
                Add(RolenameWitch.Value, WitchLine4.Value);
                Add(RolenameWitch.Value, WitchLine5.Value,
                    onStart: () => SetWitchColoration(0.9f));
            }

            protected override void OnScenarioComplete() {
                ScenarioManager.Reset<Segment_SHPC_03>();
                ScenarioManager.Start<Segment_SHPC_03>();
            }
        }

        //SHPC第3段：回应
        internal class Segment_SHPC_03 : ADVScenarioBase
        {
            public override string Key => nameof(Segment_SHPC_03);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => SHPCDialogueBox.Instance;

            protected override void OnScenarioStart() {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait()?.SkipFadeIn();
                SetSHPCFace(ShepelFullBodyPortrait.Face.Serious);
            }

            protected override void Build() {
                Add(RolenameSHPC.Value, ShpcLine3.Value);
            }

            protected override void OnScenarioComplete() {
                ScenarioManager.Reset<Segment_Witch_04>();
                ScenarioManager.Start<Segment_Witch_04>();
            }
        }

        //女巫第4段：关键台词
        internal class Segment_Witch_04 : ADVScenarioBase
        {
            public override string Key => nameof(Segment_Witch_04);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => BrimstoneDialogueBox.Instance;

            protected override void OnScenarioStart() {
                BrimstoneDialogueBox.Instance?.ShowFullBodyPortrait<WitchPastFullBodyPortrait>();
                BrimstoneDialogueBox.Instance?.GetActiveFullBodyPortrait()?.SkipFadeIn();
                SetWitchColoration(1f);
            }

            protected override void Build() {
                Add(RolenameWitch.Value, WitchLine6.Value);
                Add(RolenameWitch.Value, WitchLine7.Value);
            }

            protected override void OnScenarioComplete() {
                ScenarioManager.Reset<Segment_SHPC_04>();
                ScenarioManager.Start<Segment_SHPC_04>();
            }
        }

        //SHPC第4段：沉默，由末行自动触发 Complete → OnScenarioComplete 收尾
        internal class Segment_SHPC_04 : ADVScenarioBase
        {
            public override string Key => nameof(Segment_SHPC_04);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => SHPCDialogueBox.Instance;

            protected override void OnScenarioStart() {
                SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
                SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait()?.SkipFadeIn();
                SetSHPCFace(ShepelFullBodyPortrait.Face.Sad);
            }

            protected override void OnScenarioComplete() => FinishPerformance();

            protected override void Build() {
                Add(RolenameSHPC.Value, ShpcLine4.Value);
            }
        }
    }
}
