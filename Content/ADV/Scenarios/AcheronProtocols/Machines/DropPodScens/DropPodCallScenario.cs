using CalamityOverhaul.Content.ADV.IncomingCalls;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.AcheronProtocols.Machines.DropPodScens
{
    /// <summary>
    /// 空降仓坠落时嘉登的来电场景，在坠入大气层初期触发
    /// </summary>
    internal class DropPodCallScenario : IncomingCallScenarioBase
    {
        public static DropPodCallScenario Instance => GetScenario<DropPodCallScenario>();
        public static LocalizedText CallerNameText { get; private set; }
        public static LocalizedText Line1 { get; private set; }
        public static LocalizedText Line2 { get; private set; }
        public static LocalizedText Line3 { get; private set; }
        public static LocalizedText Line4 { get; private set; }
        public static LocalizedText Line5 { get; private set; }
        public static LocalizedText Line6 { get; private set; }
        public static LocalizedText Line7 { get; private set; }
        public static LocalizedText Line8 { get; private set; }
        public static LocalizedText Line9 { get; private set; }

        protected override string CallerName => CallerNameText.Value;

        public override void SetStaticDefaults() {
            CallerNameText = this.GetLocalization(nameof(CallerNameText), () => "嘉登");
            Line1 = this.GetLocalization(nameof(Line1), () => "比我想得要顺利许多，穿梭用时负43分钟");
            Line2 = this.GetLocalization(nameof(Line2), () => "你的体质非常适合亚空间航行");
            Line3 = this.GetLocalization(nameof(Line3), () => "你现在位于科尔托三号星的近地轨道");
            Line4 = this.GetLocalization(nameof(Line4), () => "注意避开那些星流机甲的残骸");
            Line5 = this.GetLocalization(nameof(Line5), () => "启示录级战争结束后，这颗星球的总质量增加了一倍，于是你便看到了这些");
            Line6 = this.GetLocalization(nameof(Line6), () => "我已经将权限刻录进芯片中，如果你遇到幸存的机体，它们会明白一切");
            Line7 = this.GetLocalization(nameof(Line7), () => "你马上就要进入虫族建立的屏蔽立场里面了，到时你和外界的一切联系会中断");
            Line8 = this.GetLocalization(nameof(Line8), () => "如果你成功杀死驻扎在这颗星球的节点生物，立场应该会解除，它应该位于接近地核的位置");
            Line9 = this.GetLocalization(nameof(Line9), () => "最后，祝你好运");
        }

        protected override void Build() {
            string caller = CallerName;
            RegisterPortrait(caller, ADVAsset.DraedonADV);

            Add(caller, Line1.Value, autoAdvanceDelay: 120);
            Add(caller, Line2.Value, autoAdvanceDelay: 120);
            Add(caller, Line3.Value, autoAdvanceDelay: 120);
            Add(caller, Line4.Value, autoAdvanceDelay: 120);
            Add(caller, Line5.Value, autoAdvanceDelay: 120);
            Add(caller, Line6.Value, autoAdvanceDelay: 120);
            Add(caller, Line7.Value, autoAdvanceDelay: 140);
            Add(caller, Line8.Value, autoAdvanceDelay: 140);
            Add(caller, Line9.Value, autoAdvanceDelay: 120);
        }
    }
}
