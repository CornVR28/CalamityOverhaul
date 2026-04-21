using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityOverhaul.Content.HackTimes
{
    /// <summary>
    /// 骇客时间UI层管理
    /// <br/>激活骇客模式时屏蔽原版HUD层（快捷栏、生命条、魔力条等）
    /// <br/>避免常规游戏UI干扰沉浸式骇入界面
    /// </summary>
    internal class HackTimeInterfaceSystem : ModSystem
    {
        //骇客时间激活时需要隐藏的原版UI层
        private static readonly HashSet<string> HiddenLayers = [
            "Vanilla: Hotbar",
            "Vanilla: Resource Bars",
            "Vanilla: Inventory",
            "Vanilla: Info Accessories Bar",
            "Vanilla: Map / Minimap",
            "Vanilla: Diagnose Net",
            "Vanilla: Diagnose Video",
            "Vanilla: Entity Health Bars",
            "Vanilla: Emote Bubbles",
            "Vanilla: Builder Accessories",
            "Vanilla: Radial Hotbars",
        ];

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
            //仅在骇客时间有效果时隐藏（包括淡出过程）
            if (!HackTime.Active && HackTime.Intensity < 0.5f) return;

            foreach (var layer in layers) {
                if (HiddenLayers.Contains(layer.Name)) {
                    layer.Active = false;
                }
            }
        }
    }
}
