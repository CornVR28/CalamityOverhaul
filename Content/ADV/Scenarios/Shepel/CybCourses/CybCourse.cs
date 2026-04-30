using CalamityOverhaul.Content.HackTimes;
using CalamityOverhaul.Content.RAMSystems;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.CybCourses
{
    //超梦教程关卡的入口控制
    //通过调用CybCourseWorld.Enter()进入，CybCourseWorld.Exit()退出
    //RETRY软重启时调用CybCourse.Restart()，不需要重新加载子世界
    internal class CybCourse
    {
        public static bool IsActive => CybCourseWorld.Active;

        public static void Enter() => CybCourseWorld.Enter();

        /// <summary>
        /// 退出教程子世界
        /// 同步清理 InfiniteHack、Outro 场景注册表与完成面板，避免下次进入时残留
        /// </summary>
        public static void Exit() {
            CybCourseCompletePanel.Hide();
            ScenarioManager.Reset<CybCourseOutroDialogue>();
            HackTime.InfiniteHack = false;
            CybCourseWorld.Exit();
        }

        /// <summary>
        /// 软重启训练（RETRY）
        /// 不重新加载子世界：清理教程状态 → 重置玩家位置/RAM → 重启 ScenarioManager → 重生测试 NPC → 触发开场对话
        /// </summary>
        public static void Restart() {
            //1. 关闭面板
            CybCourseCompletePanel.Hide();

            //2. 重置教程 ModSystem 内部状态（包括清理 SantaNK1）
            CybTutorialLead.ResetForRetry();
            HackTimeTutorialLead.ResetForRetry();

            //3. 重置 ScenarioManager 已运行场景，让自动开场重新生效
            ScenarioManager.Reset<CybCourseIntroDialogue>();
            ScenarioManager.Reset<CybCourseHackIntroDialogue>();
            ScenarioManager.Reset<CybCourseOutroDialogue>();

            //4. 重置玩家位置 / RAM / 骇客时间
            HackTime.Reset();
            RamSystem.Refill();
            HackTime.InfiniteHack = true;
            Player p = Main.LocalPlayer;
            if (p != null && p.active) {
                p.Center = new Vector2(
                    CybCourseGen.SpawnTileX * 16f + 8f,
                    CybCourseGen.SpawnTileY * 16f - p.height * 0.5f);
                p.velocity = Vector2.Zero;
                p.statLife = p.statLifeMax2;
            }

            //5. 触发开场对话；CybTutorialLead.AutoTriggerIntro 会接力推进流程
            //   _introAttempted 已在 ResetForRetry 中清零
        }
    }
}
