using CalamityOverhaul.Content.ADV.IncomingCalls;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.DropPodScens
{
    /// <summary>
    /// 空降仓坠落时嘉登的来电场景——在坠入大气层初期触发，
    /// 通知玩家再入灼烧即将开始并提示操作方式
    /// </summary>
    internal class DropPodCallScenario : IncomingCallScenarioBase
    {
        public static DropPodCallScenario Instance { get; } = new();

        protected override string CallerName => "嘉登";

        protected override void Build() {
            RegisterPortrait("嘉登", ADVAsset.DraedonADV);

            Add("嘉登", "空降仓已进入大气层，再入灼烧即将开始",
                autoAdvanceDelay: 150);
            Add("嘉登", "注意规避轨道残骸，使用方向键控制水平偏移",
                autoAdvanceDelay: 180);
        }
    }
}
