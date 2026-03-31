using CalamityOverhaul.Common;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.HackTime
{
    /// <summary>
    /// 骇客时间目标选择系统
    /// <br/>处理按键切换、光标悬停检测和点击选择逻辑
    /// <br/>作为ModPlayer运行，每帧检测鼠标下的可骇入NPC
    /// </summary>
    internal class HackTimeTargeting : ModPlayer
    {
        //鼠标是否在上一帧处于按下状态，用于检测点击边沿
        private bool wasMouseDown;
        //是否正在接管缩放控制
        private bool wasHackZoomActive;
        //进入骇客时间前保存的原始缩放目标值
        private float savedZoomTarget;

        public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet) {
            if (Player.whoAmI != Main.myPlayer) return;
            if (CWRKeySystem.HackTime_Toggle != null && CWRKeySystem.HackTime_Toggle.JustPressed) {
                HackTime.Toggle();
            }
        }

        public override void PostUpdate() {
            if (Player.whoAmI != Main.myPlayer) return;
            if (!HackTime.Active) {
                wasMouseDown = false;
                return;
            }

            UpdateHoverDetection();
            UpdateClickSelection();
        }

        /// <summary>
        /// 检测光标下方的可骇入目标
        /// </summary>
        private void UpdateHoverDetection() {
            //获取鼠标在世界中的位置
            Vector2 mouseWorld = Main.MouseWorld;
            int bestIndex = -1;
            float bestDistSq = float.MaxValue;

            //遍历所有活跃NPC，找到离鼠标最近的可骇入目标
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC npc = Main.npc[i];
                if (!HackTime.IsHackableTarget(npc)) continue;

                //检查鼠标是否在NPC的碰撞箱范围内（略微放大判定区域）
                float expandMargin = 16f;
                float left = npc.position.X - expandMargin;
                float top = npc.position.Y - expandMargin;
                float right = npc.position.X + npc.width + expandMargin;
                float bottom = npc.position.Y + npc.height + expandMargin;

                if (mouseWorld.X >= left && mouseWorld.X <= right &&
                    mouseWorld.Y >= top && mouseWorld.Y <= bottom) {
                    float dx = mouseWorld.X - npc.Center.X;
                    float dy = mouseWorld.Y - npc.Center.Y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq < bestDistSq) {
                        bestDistSq = distSq;
                        bestIndex = i;
                    }
                }
            }

            HackTime.HoveredTargetIndex = bestIndex;
        }

        /// <summary>
        /// 检测点击事件并选中目标
        /// </summary>
        private void UpdateClickSelection() {
            bool mouseDown = Main.mouseLeft;
            //检测鼠标按下的上升沿（本帧按下且上一帧未按下）
            bool clicked = mouseDown && !wasMouseDown;
            wasMouseDown = mouseDown;

            if (!clicked) return;

            var panel = HackTimeRender.Panel;

            //如果鼠标在面板内，优先交给面板处理点击
            if (panel.ContainsMouse()) {
                panel.HandleClick();
                return;
            }

            int hovered = HackTime.HoveredTargetIndex;

            if (hovered >= 0) {
                //如果点击了一个新目标
                if (hovered != HackTime.SelectedTargetIndex) {
                    HackTime.SelectTarget(hovered);
                    panel.Show();
                }
            }
            else {
                //点击空白处取消选中
                if (HackTime.SelectedTargetIndex >= 0) {
                    HackTime.DeselectTarget();
                    panel.Hide();
                }
            }
        }

        /// <summary>
        /// 在骇客时间中应用运镜偏移和缩放
        /// </summary>
        public override void ModifyScreenPosition() {
            bool needControl = HackTime.Active || HackTime.Intensity >= 0.001f;

            if (!needControl) {
                //完全退出后恢复原始缩放
                if (wasHackZoomActive) {
                    Main.GameZoomTarget = savedZoomTarget;
                    wasHackZoomActive = false;
                }
                return;
            }

            //首次进入时保存当前缩放
            if (!wasHackZoomActive) {
                savedZoomTarget = Main.GameZoomTarget;
                wasHackZoomActive = true;
            }

            //应用运镜偏移
            if (HackTime.CameraOffset != Vector2.Zero) {
                Main.screenPosition += HackTime.CameraOffset;
            }

            //用set而非add方式设置缩放，确保退出时能恢复
            float zoomBoost = HackTime.GetZoomBoost();
            Main.GameZoomTarget = MathHelper.Clamp(
                savedZoomTarget + zoomBoost, 0.1f, 10f);
        }
    }
}
