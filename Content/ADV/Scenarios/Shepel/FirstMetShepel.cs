using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.ADV.DialogueBoxs.Styles;
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
        //对话文本
        public static LocalizedText Line1 { get; private set; }
        public static LocalizedText Line2 { get; private set; }

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
            RolenameSHPC = this.GetLocalization(nameof(RolenameSHPC), () => "SHPC");
            Line1 = this.GetLocalization(nameof(Line1), () => "......检测到生物信号接近，启动交互协议");
            Line2 = this.GetLocalization(nameof(Line2), () => "您好，我是超级家庭个人计算机，编号S.H.P.C");
        }

        protected override void Build() {
            DialogueBoxBase.RegisterPortrait(RolenameSHPC.Value, texture: null);
            DialogueBoxBase.SetPortraitStyle(RolenameSHPC.Value, silhouette: false);

            //对话序列
            Add(RolenameSHPC.Value, Line1.Value);
            Add(RolenameSHPC.Value, Line2.Value);
        }
        protected override void OnScenarioStart() {
            SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
        }
    }
}
