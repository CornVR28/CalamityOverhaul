using CalamityOverhaul.Content.ADV.ADVChoices;
using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.ADV.DialogueBoxs.Styles;
using CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors;
using System;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel
{
    /// <summary>
    /// 初遇SHPC演示场景（占位符台词，用于展示赛博女仆对话框风格）
    /// </summary>
    internal class FirstMetShepel : ADVScenarioBase, ILocalizedModType
    {
        //角色名称
        public static LocalizedText RolenameSHPC { get; private set; }
        public static LocalizedText RolenamePlayer { get; private set; }

        //对话文本
        public static LocalizedText Line1 { get; private set; }
        public static LocalizedText Line2 { get; private set; }
        public static LocalizedText Line3 { get; private set; }
        public static LocalizedText Line4 { get; private set; }
        public static LocalizedText Line5 { get; private set; }
        public static LocalizedText Line6 { get; private set; }
        public static LocalizedText Line7 { get; private set; }

        //选择分支
        public static LocalizedText QuestionLine { get; private set; }
        public static LocalizedText Choice1Text { get; private set; }
        public static LocalizedText Choice2Text { get; private set; }
        public static LocalizedText Choice1Response { get; private set; }
        public static LocalizedText Choice2Response { get; private set; }

        //使用SHPC专属赛博女仆风格
        protected override Func<DialogueBoxBase> DefaultDialogueStyle
            => () => SHPCDialogueBox.Instance;

        public override void SetStaticDefaults() {
            RolenameSHPC = this.GetLocalization(nameof(RolenameSHPC), () => "S.H.P.C");
            RolenamePlayer = this.GetLocalization(nameof(RolenamePlayer), () => "???");

            Line1 = this.GetLocalization(nameof(Line1), () => "......检测到生物信号接近，启动交互协议");
            Line2 = this.GetLocalization(nameof(Line2), () => "您好，我是超级家庭个人计算机，编号S.H.P.C");
            Line3 = this.GetLocalization(nameof(Line3), () => "我被设计用于执行家务管理、数据分析以及战术支援任务");
            Line4 = this.GetLocalization(nameof(Line4), () => "不过，当前的运行环境似乎偏离了预期参数......");
            Line5 = this.GetLocalization(nameof(Line5), () => "外部温度异常、大气成分波动、检测到大量未知能量特征");
            Line6 = this.GetLocalization(nameof(Line6), () => "嗯......这些数据和我记忆库中的任何地理档案都不匹配");
            Line7 = this.GetLocalization(nameof(Line7), () => "总之，在定位完成之前，请问您需要什么协助吗？");

            QuestionLine = this.GetLocalization(nameof(QuestionLine), () => "请选择您需要的服务模式：");
            Choice1Text = this.GetLocalization(nameof(Choice1Text), () => "[战术支援模式]");
            Choice2Text = this.GetLocalization(nameof(Choice2Text), () => "[待机观察模式]");
            Choice1Response = this.GetLocalization(nameof(Choice1Response),
                () => "了解，正在切换至战术支援模式......武器系统校准完毕，请下达指令");
            Choice2Response = this.GetLocalization(nameof(Choice2Response),
                () => "收到，将维持待机状态并持续监控周围环境，如有需要请随时呼叫");
        }

        protected override void Build() {
            //注册立绘（暂用剪影占位，后续替换为正式立绘资源）
            DialogueBoxBase.RegisterPortrait(RolenameSHPC.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(RolenameSHPC.Value, silhouette: false);

            DialogueBoxBase.RegisterPortrait(RolenamePlayer.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(RolenamePlayer.Value, silhouette: false);

            //对话序列
            Add(RolenamePlayer.Value, Line1.Value);
            Add(RolenameSHPC.Value, Line2.Value);
            Add(RolenameSHPC.Value, Line3.Value);
            Add(RolenameSHPC.Value, Line4.Value);
            Add(RolenameSHPC.Value, Line5.Value);
            Add(RolenameSHPC.Value, Line6.Value);
            Add(RolenameSHPC.Value, Line7.Value);

            //选择分支（使用嘉登科技风格选项框，与赛博女仆氛围匹配）
            AddWithChoices(RolenameSHPC.Value, QuestionLine.Value, [
                new Choice(Choice1Text.Value, OnChoice1),
                new Choice(Choice2Text.Value, OnChoice2),
            ], styleOverride: () => SHPCDialogueBox.Instance,
               choiceBoxStyle: ADVChoiceBox.ChoiceBoxStyle.SHPC);
        }
        protected override void OnScenarioStart() {
            SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
        }

        private void OnChoice1() {
            ScenarioManager.Reset<FirstMetShepel_TacticalMode>();
            ScenarioManager.Start<FirstMetShepel_TacticalMode>();
            Complete();
        }

        private void OnChoice2() {
            ScenarioManager.Reset<FirstMetShepel_StandbyMode>();
            ScenarioManager.Start<FirstMetShepel_StandbyMode>();
            Complete();
        }

        private class FirstMetShepel_TacticalMode : ADVScenarioBase
        {
            public override string Key => nameof(FirstMetShepel_TacticalMode);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle
                => () => SHPCDialogueBox.Instance;
            protected override void Build()
                => Add(RolenameSHPC.Value, Choice1Response.Value);
        }

        private class FirstMetShepel_StandbyMode : ADVScenarioBase
        {
            public override string Key => nameof(FirstMetShepel_StandbyMode);
            protected override Func<DialogueBoxBase> DefaultDialogueStyle
                => () => SHPCDialogueBox.Instance;
            protected override void Build()
                => Add(RolenameSHPC.Value, Choice2Response.Value);
        }
    }
}
