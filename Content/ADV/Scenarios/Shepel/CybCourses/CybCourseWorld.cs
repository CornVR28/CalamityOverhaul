using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.WorldBuilding;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.CybCourses
{
    internal class CybCourseWorld : Subworld
    {
        //世界宽高保持极小，够用就行，不浪费内存
        public override int Width => 400;
        public override int Height => 250;

        public static bool Active => SubworldSystem.IsActive<CybCourseWorld>();

        public override List<GenPass> Tasks => [new CybCourseGen()];

        public static void Enter() => SubworldSystem.Enter<CybCourseWorld>();
        public static void Exit() => SubworldSystem.Exit();

        public override void OnEnter() { }

        public override void OnExit() { }

        public override void OnLoad() {
            //固定为永夜，强化赛博朋克氛围
            Main.dayTime = false;
            Main.time = 0;
            //把地表线和岩层线推到世界底部，令游戏认为整个世界处于地面以上
            //这样环境光照正常工作，不会出现地下黑暗
            Main.worldSurface = Height - 5;
            Main.rockLayer = Height - 4;
        }

        public override void Update() { }
    }
}
