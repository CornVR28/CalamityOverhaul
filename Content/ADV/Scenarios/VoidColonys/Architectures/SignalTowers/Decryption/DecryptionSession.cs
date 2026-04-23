using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.SignalTowers.Decryption
{
    /// <summary>
    /// 解密面板的阶段枚举
    /// Locked(未破译) → Breaching(破译进行中) → DataFlood(数据洪流演出) → MessageReveal(消息展示)
    /// Closing为关闭动画阶段，动画完成后真正移除面板
    /// </summary>
    internal enum DecryptionPhase
    {
        /// <summary>初始锁定，呈现警戒红色，展示目标信息+引导玩家进入破译</summary>
        Locked,
        /// <summary>破译小游戏进行中（Step3填充交互逻辑）</summary>
        Breaching,
        /// <summary>破译成功后的数据洪流演出（Step4填充）</summary>
        DataFlood,
        /// <summary>解码消息逐行展示（Step5填充）</summary>
        MessageReveal,
        /// <summary>关闭动画</summary>
        Closing,
    }

    /// <summary>
    /// 信号塔解密面板的全局会话状态
    /// 负责阶段切换、展开/关闭动画、全局计时，供 DecryptionPanelRenderer 读取渲染
    /// </summary>
    public static class DecryptionSession
    {
        /// <summary>当前正在被解密的信号塔，null表示面板未开启</summary>
        internal static SignalTowerActor CurrentTower { get; private set; }
        /// <summary>当前阶段</summary>
        internal static DecryptionPhase Phase { get; private set; } = DecryptionPhase.Locked;
        /// <summary>本阶段已经过的帧数</summary>
        internal static int PhaseTicks { get; private set; }
        /// <summary>面板展开动画进度 0~1，关闭阶段会反向插值</summary>
        internal static float OpenProgress { get; private set; }
        /// <summary>会话总时间（秒），shader时间/粒子计时使用</summary>
        internal static float SessionTime { get; private set; }
        /// <summary>破译小游戏实例，Breaching阶段驱动</summary>
        internal static BreachMinigame Minigame { get; } = new();
        /// <summary>数据洪流阶段实例</summary>
        internal static DataFloodStage DataFlood { get; } = new();
        /// <summary>消息展示阶段实例</summary>
        internal static MessageRevealStage Message { get; } = new();
        /// <summary>面板是否处于显示状态（含关闭动画过程）</summary>
        public static bool IsOpen => CurrentTower != null;
        /// <summary>是否正在抓取鼠标（用于阻止穿透到世界）</summary>
        public static bool IsCapturingMouse => IsOpen && OpenProgress > 0.05f;

        private static bool prevEscDown;

        /// <summary>尝试对指定信号塔开启解密面板</summary>
        internal static void Open(SignalTowerActor tower) {
            if (tower == null) return;
            //同一座塔重复右键：若已经在同座会话里，不重置状态；如果正在关闭动画中，则取消关闭继续显示
            if (CurrentTower == tower && Phase != DecryptionPhase.Closing) return;
            bool sameTower = CurrentTower == tower;
            CurrentTower = tower;
            Phase = DecryptionPhase.Locked;
            PhaseTicks = 0;
            if (!sameTower) SessionTime = 0f;
            //OpenProgress不归零，允许从其他塔无缝切换
        }

        /// <summary>请求关闭（进入Closing阶段播关闭动画）</summary>
        internal static void RequestClose() {
            if (!IsOpen || Phase == DecryptionPhase.Closing) return;
            TransitionTo(DecryptionPhase.Closing);
        }

        /// <summary>立即关闭（不走动画），仅在强制情形使用</summary>
        internal static void Close() {
            CurrentTower = null;
            Phase = DecryptionPhase.Locked;
            PhaseTicks = 0;
            OpenProgress = 0f;
            prevEscDown = false;
            //清理各阶段状态，避免下次打开残留
            Minigame.Clear();
            DataFlood.Reset();
            Message.Clear();
        }

        /// <summary>阶段切换，重置本阶段计时</summary>
        internal static void TransitionTo(DecryptionPhase next) {
            Phase = next;
            PhaseTicks = 0;
            if (next == DecryptionPhase.Breaching) {
                Minigame.NewPuzzle();
            }
            else if (next == DecryptionPhase.DataFlood) {
                DataFlood.Reset();
            }
            else if (next == DecryptionPhase.MessageReveal) {
                Message.SetContent(MessageStrings.BuildDefaultPayload(CurrentTower));
            }
        }

        /// <summary>每帧驱动：由<see cref="DecryptionSessionSystem"/>调用</summary>
        internal static void Update() {
            if (!IsOpen) {
                //无塔时确保OpenProgress收敛到0
                if (OpenProgress > 0f) OpenProgress = MathHelper.Max(0f, OpenProgress - 0.08f);
                return;
            }

            //塔失效立即关闭
            if (CurrentTower == null || !CurrentTower.Active || !VoidColony.Active) {
                Close();
                return;
            }

            SessionTime += 1f / 60f;
            PhaseTicks++;

            //小游戏驱动
            if (Phase == DecryptionPhase.Breaching) {
                Minigame.Update(1f / 60f);
                //破译完成后不立即切换，先保持SolvedHoldDuration秒展示成功过渡，再进入数据洪流
                if (Minigame.HasSolved && Minigame.SolvedHoldTime >= BreachMinigame.SolvedHoldDuration) {
                    TransitionTo(DecryptionPhase.DataFlood);
                }
            }
            else if (Phase == DecryptionPhase.DataFlood) {
                DataFlood.Update(1f / 60f);
                if (DataFlood.Completed) {
                    TransitionTo(DecryptionPhase.MessageReveal);
                }
            }
            else if (Phase == DecryptionPhase.MessageReveal) {
                Message.Update(1f / 60f);
            }

            //展开动画
            float target = Phase == DecryptionPhase.Closing ? 0f : 1f;
            OpenProgress = MathHelper.Lerp(OpenProgress, target, 0.15f);
            if (Math.Abs(OpenProgress - target) < 0.005f) OpenProgress = target;

            //关闭动画播完真正清除
            if (Phase == DecryptionPhase.Closing && OpenProgress <= 0.01f) {
                Close();
                return;
            }

            //ESC边沿触发关闭
            bool escDown = Main.keyState.IsKeyDown(Keys.Escape);
            if (escDown && !prevEscDown && Phase != DecryptionPhase.Closing) {
                RequestClose();
            }
            prevEscDown = escDown;
        }
    }

    /// <summary>解密面板会话的ModSystem驱动：Update每帧tick</summary>
    public class DecryptionSessionSystem : ModSystem
    {
        public override void PostUpdateEverything() => DecryptionSession.Update();
        public override void OnWorldUnload() => DecryptionSession.Close();
    }
}
