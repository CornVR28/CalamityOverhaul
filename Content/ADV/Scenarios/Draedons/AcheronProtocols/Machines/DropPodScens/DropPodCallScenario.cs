using CalamityOverhaul.Content.ADV.IncomingCalls;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.DropPodScens
{
    /// <summary>
    /// 空降仓坠落时嘉登的来电场景，在坠入大气层初期触发
    /// </summary>
    internal class DropPodCallScenario : IncomingCallScenarioBase
    {
        public static DropPodCallScenario Instance { get; } = new();

        protected override string CallerName => "嘉登";

        protected override void Build() {
            RegisterPortrait("嘉登", ADVAsset.DraedonADV);

            Add("嘉登", "比我想得要顺利许多，穿梭用时负43分钟",
                autoAdvanceDelay: 120);
            Add("嘉登", "你的体质非常适合亚空间航行",
                autoAdvanceDelay: 120);
            Add("嘉登", "你现在位于科尔托三号星的近地轨道",
                autoAdvanceDelay: 120);
            Add("嘉登", "注意避开那些星流机甲的残骸",
                autoAdvanceDelay: 120);
            Add("嘉登", "启示录级战争结束后，这颗星球的总质量增加了一倍，于是你便看到了这些",
                autoAdvanceDelay: 120);
            Add("嘉登", "我已经将权限刻录进芯片中，如果你遇到幸存的机体，它们会明白一切",
                autoAdvanceDelay: 120);
            Add("嘉登", "你马上就要进入虫族建立的屏蔽立场里面了，到时你和外界的一切联系会中断",
                autoAdvanceDelay: 140);
            Add("嘉登", "如果你成功杀死驻扎在这颗星球的节点生物，立场应该会解除，它应该位于接近地核的位置",
                autoAdvanceDelay: 140);
            Add("嘉登", "最后，祝你好运",
                autoAdvanceDelay: 120);
        }
    }
}
