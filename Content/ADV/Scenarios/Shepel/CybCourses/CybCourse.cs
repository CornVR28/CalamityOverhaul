namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.CybCourses
{
    //超梦教程关卡的入口控制
    //通过调用CybCourseWorld.Enter()进入，CybCourseWorld.Exit()退出
    internal class CybCourse
    {
        public static bool IsActive => CybCourseWorld.Active;

        public static void Enter() => CybCourseWorld.Enter();
        public static void Exit() => CybCourseWorld.Exit();
    }
}
