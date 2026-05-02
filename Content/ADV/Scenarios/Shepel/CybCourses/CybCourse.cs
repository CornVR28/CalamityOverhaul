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
        //接受教程后进入子世界前置为true，退出时发放超梦接入凭证
        //用静态字段而非存档标记，避免子世界存档与主世界存档不同步导致标记丢失
        private static bool _grantMewtwoOnExit;

        public static bool IsActive => CybCourseWorld.Active;

        public static void Enter() => CybCourseWorld.Enter();

        //由FirstMetShepel_CybCourseAccept在进入子世界前调用，标记回到主世界后需发放凭证
        internal static void ScheduleMewtwoGrant() => _grantMewtwoOnExit = true;

        //回到主世界时由CybCoursePlayer.OnEnterWorld调用，返回true表示需要发放
        internal static bool TryConsumeGrantMewtwo() {
            if (!_grantMewtwoOnExit) return false;
            _grantMewtwoOnExit = false;
            return true;
        }

        /// <summary>
        /// 退出教程子世界
        /// 清理 InfiniteHack、Outro 场景与完成面板，并按需发放超梦接入凭证
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

            //3. 回滚物块到生成时的快照（包括墙体/帧数据/液体/坡度），并重新挂载MK2 的 TP 实体
            CybCourseGen.RestoreSnapshot();

            //4. 重置 ScenarioManager 已运行场景，让自动开场重新生效
            ScenarioManager.Reset<CybCourseIntroDialogue>();
            ScenarioManager.Reset<CybCourseHackIntroDialogue>();
            ScenarioManager.Reset<CybCourseOutroDialogue>();

            //5. 重置玩家位置 / RAM / 骇客时间
            HackTime.Reset();
            RamSystem.Refill();
            Player p = Main.LocalPlayer;
            if (p != null && p.active) {
                p.Center = new Vector2(
                    CybCourseGen.SpawnTileX * 16f + 8f,
                    CybCourseGen.SpawnTileY * 16f - p.height * 0.5f);
                p.velocity = Vector2.Zero;
                p.statLife = p.statLifeMax2;
            }

            //6. 触发开场对话；CybTutorialLead.AutoTriggerIntro 会接力推进流程
            //   _introAttempted 已在 ResetForRetry 中清零
        }
    }
}
