using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.CybCourses
{
    //超梦教程入场演出层
    //在玩家刚进入 CybCourse 子世界的前 ~2.7 秒，将六角能量网格揭示叠加层
    //插到所有界面层之上（含鼠标指针），形成一个完整的"接入"式开场过场。
    //
    //单一职责：仅注册界面层；演出本身的状态机与时间推进归 CybCourseWorld 管理，
    //单一来源避免不同系统重复推进时间。
    internal class CybCourseEntryRevealLayer : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
            //仅在教程子世界激活、且演出仍在窗口内时注入
            if (!CybCourseWorld.Active) {
                return;
            }
            if (!CybCourseWorld.EntryRevealActive) {
                return;
            }

            //放到最末尾 → 绘制顺序最靠后 → 盖住一切（包括默认鼠标指针）
            //短暂的盖住是有意为之：开场仪式感更强，演出结束自动撤层
            layers.Add(new LegacyGameInterfaceLayer(
                "CWRMod: CybCourse Entry Reveal",
                delegate {
                    CybCourseWorld.DrawEntryRevealOverlay(Main.spriteBatch);
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }
}
