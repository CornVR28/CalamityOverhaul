using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.HackTimes
{
    /// <summary>
    /// 骇客时间UI面板
    /// <br/>基于UIHandle系统在正确的UI层级中绘制和处理交互
    /// <br/>负责骇入面板的显示、协议选择和上传进度管理
    /// </summary>
    internal class HackTimeUI : UIHandle
    {
        public static HackTimeUI Instance => UIHandleLoader.GetUIHandleOfType<HackTimeUI>();

        //骇入面板渲染器
        internal HackPanelRenderer Panel { get; private set; } = new();
        //左侧上传队列渲染器
        internal HackQueueRenderer Queue { get; private set; } = new();
        //无限骇入弹窗风暴渲染器
        internal InfiniteHackRenderer InfiniteHack { get; private set; } = new();
        //目标扫描信息面板渲染器
        internal ScanInfoRenderer ScanInfo { get; private set; } = new();
        //RAM弧形资源HUD渲染器
        internal HackRamRenderer Ram { get; private set; } = new();

        public override bool Active => HackTime.Active || HackTime.Intensity >= 0.001f;

        public override void Load() {
            Panel.Queue = Queue;
        }

        public override void Update() {
            Panel.Update();
            //Queue.Update由CWRWorld.PostUpdateEverything全局驱动，此处不再调用
            InfiniteHack.Update();
            ScanInfo.Update();
            Ram.Update();

            //处理点击交互
            bool mouseOnPanel = Panel.ContainsMouse();
            UpdateClickSelection(mouseOnPanel);

            //面板悬停时阻止游戏交互穿透
            hoverInMainPage = mouseOnPanel;
            if (hoverInMainPage) {
                player.mouseInterface = true;
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            //脱离UIScaleMatrix，面板使用原始像素坐标绘制
            Ram.Draw(spriteBatch);
            ScanInfo.Draw(spriteBatch);
            Panel.Draw(spriteBatch);
            //左侧：无限模式用弹窗风暴，否则用普通队列
            if (HackTime.InfiniteHack)
                InfiniteHack.Draw(spriteBatch);
            else
                Queue.Draw(spriteBatch);
        }

        //处理点击选择逻辑
        private void UpdateClickSelection(bool mouseOnPanel) {
            if (!HackTime.Active) return;

            //右键取消选中
            if (keyRightPressState == KeyPressState.Pressed && !mouseOnPanel) {
                if (HackTime.CurrentScanTarget != null) {
                    HackTime.DeselectTarget();
                    Panel.Hide();
                }
                return;
            }

            if (keyLeftPressState != KeyPressState.Pressed) return;

            //面板内点击优先交给面板处理
            if (mouseOnPanel) {
                //无限模式：点击任意协议触发蓄力
                if (HackTime.InfiniteHack) {
                    if (Panel.HasHoveredSlot && !InfiniteHack.IsActive)
                        InfiniteHack.BeginCharge();
                }
                else {
                    Panel.HandleClick();
                }
                return;
            }

            //世界中点击：所有目标种类通过 IHackTarget 统一进入选择流程
            IHackTarget hovered = HackTimeTargeting.HoveredTarget;

            if (hovered != null) {
                if (HackTime.CurrentScanTarget == null
                    || !hovered.TargetEquals(HackTime.CurrentScanTarget)) {
                    HackTime.Select(hovered);
                    //协议面板按目标种类过滤可用协议
                    Panel.Show(hovered.TargetType.Kind);
                }
            }
            else if (HackTime.CurrentScanTarget != null) {
                //点击空白处取消选中
                HackTime.DeselectTarget();
                Panel.Hide();
            }
        }

        public override void UnLoad() {
            Panel = null;
            Queue = null;
            InfiniteHack = null;
            ScanInfo = null;
            Ram = null;
        }
    }
}
