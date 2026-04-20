using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime
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

            int hovered = HackTime.HoveredTargetIndex;

            if (hovered >= 0) {
                //点击NPC目标
                if (hovered != HackTime.SelectedTargetIndex) {
                    HackTime.SelectTarget(hovered);
                    Panel.Show(HackTargetKind.Npc);
                }
            }
            else if (HackTimeTargeting.HoveredWraith != null) {
                //点击灵异Actor目标
                var wraith = HackTimeTargeting.HoveredWraith;
                if (HackTime.CurrentScanTarget != wraith) {
                    HackTime.SelectWraithScan(wraith);
                    Panel.Show(HackTargetKind.Wraith);
                }
            }
            else if (HackTimeTargeting.HoveredTileX >= 0) {
                //点击可扫描物块，同时显示物块协议面板
                HackTime.SelectTileScan(HackTimeTargeting.HoveredTileX, HackTimeTargeting.HoveredTileY);
                Panel.Show(HackTargetKind.Tile);
            }
            else {
                //点击空白处取消选中
                if (HackTime.SelectedTargetIndex >= 0 || HackTime.CurrentScanTarget != null) {
                    HackTime.DeselectTarget();
                    Panel.Hide();
                }
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
