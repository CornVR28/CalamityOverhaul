using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.GlitchWraith;
using CalamityOverhaul.Content.HackTimes.Scannables;
using CalamityOverhaul.Content.UIs.NotificationPopup;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.HackTimes
{
    /// <summary>
    /// 骇客时间目标选择系统
    /// <br/>处理按键切换、光标悬停检测和运镜缩放
    /// <br/>点击选择逻辑由 HackTimeUI(UIHandle) 负责
    /// <br/>悬停检测通过 <see cref="HackTargetType.DetectTopmostHover"/> 自动遍历所有注册的目标种类，无需在此维护按种类分支
    /// </summary>
    internal class HackTimeTargeting : ModPlayer
    {
        //是否正在接管缩放控制
        private bool wasHackZoomActive;
        //进入骇客时间前保存的原始缩放目标值
        private float savedZoomTarget;

        /// <summary>
        /// 当前光标下的可骇入目标，null 表示无悬停
        /// <br/>所有种类（NPC、物块、灵异、炮台、信号塔等）通过<see cref="IHackTarget"/>统一暴露
        /// </summary>
        public static IHackTarget HoveredTarget { get; private set; }

        //----- 兼容旧 API：从统一的 HoveredTarget 派生具体维度 -----

        /// <summary>当前悬停的可扫描物块 X，无悬停物块时返回 -1</summary>
        public static int HoveredTileX => HoveredTarget is TileScannable t ? t.TileCoordX : -1;
        /// <summary>当前悬停的可扫描物块 Y，无悬停物块时返回 -1</summary>
        public static int HoveredTileY => HoveredTarget is TileScannable t ? t.TileCoordY : -1;
        /// <summary>当前悬停的灵异 Actor，无悬停灵异时返回 null</summary>
        public static GlitchWraithActor HoveredWraith => HoveredTarget as GlitchWraithActor;
        /// <summary>当前悬停的可骇入炮台，无悬停炮台时返回 null</summary>
        public static IHackableTurret HoveredTurret => HoveredTarget as IHackableTurret;
        /// <summary>当前悬停的可骇入信号塔，无悬停信号塔时返回 null</summary>
        public static IHackableSignalTower HoveredSignalTower => HoveredTarget as IHackableSignalTower;

        //避免按键连按时弹窗刷屏，按"约 0.6 秒"节流
        private static int accessDeniedCooldown;

        public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet) {
            if (Player.whoAmI != Main.myPlayer) return;
            if (Player.dead) return;

            if (accessDeniedCooldown > 0) accessDeniedCooldown--;

            if (CWRKeySystem.HackTime_Toggle != null && CWRKeySystem.HackTime_Toggle.JustPressed) {
                TryToggleHackTime(Player);
            }
        }

        /// <summary>
        /// 统一的"按键尝试切换骇客时间"入口
        /// <br/>已激活时允许直接关闭，未激活时校验<see cref="HackTimeAccess"/>注册的条件
        /// <br/>不满足条件时通过<see cref="NotificationPopupSystem"/>抛出警告弹窗，并按短冷却节流
        /// </summary>
        public static void TryToggleHackTime(Player player) {
            //已激活的退出动作放行，避免玩家因为掉装备等原因被锁死在骇客时间内
            if (HackTime.Active) {
                HackTime.Toggle();
                return;
            }

            if (HackTimeAccess.CanUse(player)) {
                HackTime.Toggle();
                return;
            }

            //权限不足时显示警告弹窗（按短冷却节流，避免重复按键造成弹窗堆积）
            if (accessDeniedCooldown <= 0) {
                NotificationPopupSystem.Add(new HackTimeAccessDeniedEntry());
                accessDeniedCooldown = 36;
            }
        }

        public override void PostUpdate() {
            if (Player.whoAmI != Main.myPlayer) return;
            if (!HackTime.Active) {
                HoveredTarget = null;
                return;
            }
            UpdateHoverDetection();
        }

        /// <summary>
        /// 检测光标下方的可骇入目标，按目标种类工厂的<see cref="HackTargetType.HoverPriority"/>取最高优先级命中
        /// </summary>
        private void UpdateHoverDetection() {
            HoveredTarget = HackTargetType.DetectTopmostHover(Main.MouseWorld);
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
