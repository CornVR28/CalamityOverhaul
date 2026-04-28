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
        }

        public override void Update() { }
    }
}
