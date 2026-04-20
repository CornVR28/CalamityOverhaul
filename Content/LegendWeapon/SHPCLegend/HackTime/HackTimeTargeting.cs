using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.GlitchWraith;
using InnoVault.Actors;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime
{
    /// <summary>
    /// 骇客时间目标选择系统
    /// <br/>处理按键切换、光标悬停检测和运镜缩放
    /// <br/>点击选择逻辑由HackTimeUI(UIHandle)负责
    /// </summary>
    internal class HackTimeTargeting : ModPlayer
    {
        //是否正在接管缩放控制
        private bool wasHackZoomActive;
        //进入骇客时间前保存的原始缩放目标值
        private float savedZoomTarget;

        /// <summary>
        /// 当前悬停的可扫描物块坐标，负数表示无悬停
        /// </summary>
        public static int HoveredTileX { get; set; } = -1;
        public static int HoveredTileY { get; set; } = -1;

        /// <summary>
        /// 当前悬停的灵异Actor引用，null表示无悬停
        /// </summary>
        public static GlitchWraithActor HoveredWraith { get; set; }

        public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet) {
            if (Player.whoAmI != Main.myPlayer) return;
            if (CWRKeySystem.HackTime_Toggle != null && CWRKeySystem.HackTime_Toggle.JustPressed) {
                HackTime.Toggle();
            }
        }

        public override void PostUpdate() {
            if (Player.whoAmI != Main.myPlayer) return;
            if (!HackTime.Active) return;

            UpdateHoverDetection();
        }

        /// <summary>
        /// 检测光标下方的可骇入目标或可扫描物块
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

            //NPC优先，没有悬停NPC时再检测灵异Actor和物块
            if (bestIndex >= 0) {
                HoveredTileX = -1;
                HoveredTileY = -1;
                HoveredWraith = null;
            }
            else {
                GlitchWraithActor wraith = TryGetHoveredWraith(mouseWorld);
                if (wraith != null) {
                    HoveredWraith = wraith;
                    HoveredTileX = -1;
                    HoveredTileY = -1;
                }
                else {
                    HoveredWraith = null;
                    if (TileScannable.TryGetScannableTile(mouseWorld, out int tx, out int ty)) {
                        HoveredTileX = tx;
                        HoveredTileY = ty;
                    }
                    else {
                        HoveredTileX = -1;
                        HoveredTileY = -1;
                    }
                }
            }
        }

        /// <summary>
        /// 判定鼠标下方是否有足够可见的灵异Actor可供骇入
        /// </summary>
        private static GlitchWraithActor TryGetHoveredWraith(Vector2 mouseWorld) {
            List<GlitchWraithActor> list = ActorLoader.GetActiveActors<GlitchWraithActor>();
            if (list == null || list.Count == 0) return null;
            GlitchWraithActor best = null;
            float bestDistSq = float.MaxValue;
            foreach (GlitchWraithActor w in list) {
                //可见度过低的灵异体无法被扫描锁定，避免玩家隔空选中隐形目标
                if (w.Visibility < 0.3f) continue;
                float expandMargin = 16f;
                float left = w.Position.X - expandMargin;
                float top = w.Position.Y - expandMargin;
                float right = w.Position.X + w.Width + expandMargin;
                float bottom = w.Position.Y + w.Height + expandMargin;
                if (mouseWorld.X < left || mouseWorld.X > right) continue;
                if (mouseWorld.Y < top || mouseWorld.Y > bottom) continue;
                float dx = mouseWorld.X - w.Center.X;
                float dy = mouseWorld.Y - w.Center.Y;
                float distSq = dx * dx + dy * dy;
                if (distSq < bestDistSq) {
                    bestDistSq = distSq;
                    best = w;
                }
            }
            return best;
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
