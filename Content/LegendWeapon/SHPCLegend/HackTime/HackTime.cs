using CalamityOverhaul.Common;
using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces;
using Terraria;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime
{
    /// <summary>
    /// 骇客时间核心状态管理器
    /// <br/>控制骇客模式的激活、目标选择、运镜和时间冻结
    /// <br/>按键切换进入或退出，进入后世界冻结，屏幕叠加赛博科技感滤镜
    /// </summary>
    internal class HackTime : ModSystem, ILocalizedModType
    {
        public string LocalizationCategory => "UI";

        public override void Unload() => Reset();

        #region 本地化字段

        public static LocalizedText Locked { get; private set; }
        public static LocalizedText Done { get; private set; }
        public static LocalizedText Queued { get; private set; }
        public static LocalizedText UploadingText { get; private set; }
        public static LocalizedText BreachReady { get; private set; }
        public static LocalizedText UploadComplete { get; private set; }
        public static LocalizedText UploadQueue { get; private set; }
        public static LocalizedText TargetLocked { get; private set; }
        public static LocalizedText HpFormat { get; private set; }
        public static LocalizedText Protocols { get; private set; }
        public static LocalizedText RamDepleted { get; private set; }
        public static LocalizedText LowRam { get; private set; }
        public static LocalizedText Scanning { get; private set; }
        public static LocalizedText AnalysisComplete { get; private set; }
        public static LocalizedText TypeLabel { get; private set; }
        public static LocalizedText BossClass { get; private set; }
        public static LocalizedText EliteUnit { get; private set; }
        public static LocalizedText HostileEntity { get; private set; }
        public static LocalizedText ThreatLabel { get; private set; }
        public static LocalizedText ThreatExtreme { get; private set; }
        public static LocalizedText ThreatHigh { get; private set; }
        public static LocalizedText ThreatModerate { get; private set; }
        public static LocalizedText ThreatLow { get; private set; }
        public static LocalizedText DefLabel { get; private set; }
        public static LocalizedText DmgLabel { get; private set; }
        public static LocalizedText KbResLabel { get; private set; }
        public static LocalizedText Breach { get; private set; }
        public static LocalizedText InitBreach { get; private set; }
        public static LocalizedText SystemBreach { get; private set; }
        public static LocalizedText Rebooting { get; private set; }
        public static LocalizedText SystemOnline { get; private set; }
        public static LocalizedText MemoryWiped { get; private set; }
        public static LocalizedText Cyberpsychosis { get; private set; }
        public static LocalizedText RamRefund { get; private set; }
        public static LocalizedText ActiveText { get; private set; }
        public static LocalizedText ActivePct { get; private set; }
        public static LocalizedText Complete { get; private set; }
        public static LocalizedText UploadingPct { get; private set; }
        public static LocalizedText CatLethal { get; private set; }
        public static LocalizedText CatControl { get; private set; }
        public static LocalizedText CatCovert { get; private set; }
        public static LocalizedText CatContagion { get; private set; }
        public static LocalizedText CatUnknown { get; private set; }

        public override void SetStaticDefaults() {
            Locked = this.GetLocalization(nameof(Locked));
            Done = this.GetLocalization(nameof(Done));
            Queued = this.GetLocalization(nameof(Queued));
            UploadingText = this.GetLocalization("Uploading");
            BreachReady = this.GetLocalization(nameof(BreachReady));
            UploadComplete = this.GetLocalization(nameof(UploadComplete));
            UploadQueue = this.GetLocalization(nameof(UploadQueue));
            TargetLocked = this.GetLocalization(nameof(TargetLocked));
            HpFormat = this.GetLocalization(nameof(HpFormat));
            Protocols = this.GetLocalization(nameof(Protocols));
            RamDepleted = this.GetLocalization(nameof(RamDepleted));
            LowRam = this.GetLocalization(nameof(LowRam));
            Scanning = this.GetLocalization(nameof(Scanning));
            AnalysisComplete = this.GetLocalization(nameof(AnalysisComplete));
            TypeLabel = this.GetLocalization(nameof(TypeLabel));
            BossClass = this.GetLocalization(nameof(BossClass));
            EliteUnit = this.GetLocalization(nameof(EliteUnit));
            HostileEntity = this.GetLocalization(nameof(HostileEntity));
            ThreatLabel = this.GetLocalization(nameof(ThreatLabel));
            ThreatExtreme = this.GetLocalization(nameof(ThreatExtreme));
            ThreatHigh = this.GetLocalization(nameof(ThreatHigh));
            ThreatModerate = this.GetLocalization(nameof(ThreatModerate));
            ThreatLow = this.GetLocalization(nameof(ThreatLow));
            DefLabel = this.GetLocalization(nameof(DefLabel));
            DmgLabel = this.GetLocalization(nameof(DmgLabel));
            KbResLabel = this.GetLocalization(nameof(KbResLabel));
            Breach = this.GetLocalization(nameof(Breach));
            InitBreach = this.GetLocalization(nameof(InitBreach));
            SystemBreach = this.GetLocalization(nameof(SystemBreach));
            Rebooting = this.GetLocalization(nameof(Rebooting));
            SystemOnline = this.GetLocalization(nameof(SystemOnline));
            MemoryWiped = this.GetLocalization(nameof(MemoryWiped));
            Cyberpsychosis = this.GetLocalization(nameof(Cyberpsychosis));
            RamRefund = this.GetLocalization(nameof(RamRefund));
            ActiveText = this.GetLocalization("Active");
            ActivePct = this.GetLocalization(nameof(ActivePct));
            Complete = this.GetLocalization(nameof(Complete));
            UploadingPct = this.GetLocalization(nameof(UploadingPct));
            CatLethal = this.GetLocalization(nameof(CatLethal));
            CatControl = this.GetLocalization(nameof(CatControl));
            CatCovert = this.GetLocalization(nameof(CatCovert));
            CatContagion = this.GetLocalization(nameof(CatContagion));
            CatUnknown = this.GetLocalization(nameof(CatUnknown));
        }

        #endregion

        /// <summary>
        /// 骇客时间是否处于激活状态
        /// </summary>
        public static bool Active { get; private set; }
        /// <summary>
        /// 屏幕效果强度(0到1)，用于着色器参数插值
        /// </summary>
        public static float Intensity { get; set; }
        /// <summary>
        /// 当前选中的骇入目标NPC索引，负数表示未选中
        /// </summary>
        public static int SelectedTargetIndex { get; set; } = -1;
        /// <summary>
        /// 当前光标悬停的可骇入目标NPC索引，负数表示无悬停
        /// </summary>
        public static int HoveredTargetIndex { get; set; } = -1;
        /// <summary>
        /// 运镜进度(0到1)，选中目标后平滑过渡到目标中心
        /// </summary>
        public static float CameraProgress { get; set; }
        /// <summary>
        /// 运镜缩放进度(0到1)，选中目标后画面放大
        /// </summary>
        public static float ZoomProgress { get; set; }
        /// <summary>
        /// 目标选中时的光圈动画计时器
        /// </summary>
        public static float ReticleTimer { get; set; }
        /// <summary>
        /// 运镜偏移量，每帧在ModifyScreenPosition中应用
        /// </summary>
        public static Vector2 CameraOffset { get; set; }

        //无限骇入模式（无限袭击终态演出用）
        public static bool InfiniteHack { get; set; }

        private static float targetIntensity;
        //运镜目标位置（选中NPC的中心世界坐标）
        private static Vector2 cameraTo;

        //基础缩放增量，选中目标后画面放大倍率
        private const float TargetZoomBoost = 0.35f;
        //运镜插值速度
        private const float CameraLerpSpeed = 0.06f;
        //效果淡入速度
        private const float FadeInSpeed = 0.055f;
        //效果淡出速度
        private const float FadeOutSpeed = 0.07f;

        /// <summary>
        /// 切换骇客时间的开关状态
        /// </summary>
        public static void Toggle() {
            if (Active) {
                Deactivate();
            }
            else if (Intensity > 0.001f) {
                //正在淡出中，直接反转回来，无需重置状态
                Active = true;
                targetIntensity = 1f;
                HackTimeFreeze.Activate();
            }
            else {
                Activate();
            }
        }

        /// <summary>
        /// 激活骇客时间
        /// </summary>
        public static void Activate() {
            if (Main.gameMenu) return;
            Active = true;
            targetIntensity = 1f;
            SelectedTargetIndex = -1;
            HoveredTargetIndex = -1;
            CameraProgress = 0f;
            ZoomProgress = 0f;
            ReticleTimer = 0f;
            CameraOffset = Vector2.Zero;
            cameraTo = Vector2.Zero;
            HackTimeFreeze.Activate();

            if (!VaultUtils.isServer) {
                SoundEngine.PlaySound(CWRSound.Scanning, Main.LocalPlayer.Center);
            }
        }

        /// <summary>
        /// 退出骇客时间
        /// </summary>
        public static void Deactivate() {
            Active = false;
            targetIntensity = 0f;
            SelectedTargetIndex = -1;
            HoveredTargetIndex = -1;
            HackTimeFreeze.Deactivate();
            HackTimeUI.Instance?.Panel.Hide();
        }

        /// <summary>
        /// 选中一个骇入目标，触发运镜
        /// </summary>
        public static void SelectTarget(int npcIndex) {
            if (!Active || npcIndex < 0 || npcIndex >= Main.maxNPCs) return;

            NPC npc = Main.npc[npcIndex];
            if (!npc.active) return;

            //切换目标时取消正在进行的上传
            HackTimeUI.Instance?.Panel.CancelUpload();

            bool freshSelect = SelectedTargetIndex < 0;
            SelectedTargetIndex = npcIndex;
            cameraTo = npc.Center;

            //首次选中时从零开始推进；切换目标时保持当前进度，让偏移量平滑重定向
            if (freshSelect) {
                CameraProgress = 0f;
                ZoomProgress = 0f;
            }

            if (!VaultUtils.isServer) {
                SoundEngine.PlaySound(CWRSound.Hacker, Main.LocalPlayer.Center);
            }
        }

        /// <summary>
        /// 取消选中目标，运镜平滑回归
        /// </summary>
        public static void DeselectTarget() {
            SelectedTargetIndex = -1;
            //不立即归零CameraProgress/CameraOffset，由UpdateCamera平滑回归
            HackTimeUI.Instance?.Panel.Hide();
        }

        /// <summary>
        /// 世界更新：RAM恢复、骇入效果驱动、队列上传推进
        /// </summary>
        public override void PostUpdateEverything() {
            //RAM自动恢复
            HackTimeRAM.Update();
            //骇入效果全局驱动，退出骇客时间后仍持续生效
            HackEffectTracker.Update();
            //队列上传+消费：退出骇客时间后实时推进上传并施加完成的效果
            var queue = HackTimeUI.Instance?.Queue;
            queue?.Update();
            queue?.ConsumeAndApplyAll();
        }

        /// <summary>
        /// 每帧逻辑更新
        /// </summary>
        public static void Update() {
            if (Main.gameMenu) {
                Reset();
                return;
            }

            //效果强度插值
            float fadeSpeed = Active ? FadeInSpeed : FadeOutSpeed;
            Intensity = MathHelper.Lerp(Intensity, targetIntensity, fadeSpeed);

            //淡出完毕后彻底清理残余状态
            if (!Active && targetIntensity <= 0f && Intensity < 0.005f) {
                Intensity = 0f;
                CameraOffset = Vector2.Zero;
                CameraProgress = 0f;
                ZoomProgress = 0f;
                return;
            }

            //光圈动画计时
            ReticleTimer += 0.016f;

            //处理运镜逻辑
            UpdateCamera();
        }

        private static void UpdateCamera() {
            if (SelectedTargetIndex >= 0) {
                NPC target = Main.npc[SelectedTargetIndex];
                //目标失效时自动取消
                if (!target.active) {
                    DeselectTarget();
                    return;
                }

                //更新运镜目标位置
                cameraTo = target.Center;

                //平滑推进运镜进度和缩放
                CameraProgress = MathHelper.Lerp(CameraProgress, 1f, CameraLerpSpeed);
                ZoomProgress = MathHelper.Lerp(ZoomProgress, 1f, CameraLerpSpeed * 0.8f);

                //偏移量独立平滑插值到目标位置（切换目标时从当前偏移重定向，而非瞬跳）
                Vector2 desiredOffset = cameraTo - Main.LocalPlayer.Center;
                CameraOffset = Vector2.Lerp(CameraOffset, desiredOffset, CameraLerpSpeed);
            }
            else {
                //无目标时平滑回归
                float returnSpeed = CameraLerpSpeed * 1.5f;
                CameraProgress = MathHelper.Lerp(CameraProgress, 0f, returnSpeed);
                ZoomProgress = MathHelper.Lerp(ZoomProgress, 0f, returnSpeed);
                CameraOffset = Vector2.Lerp(CameraOffset, Vector2.Zero, returnSpeed);

                if (CameraProgress < 0.005f && CameraOffset.LengthSquared() < 0.5f) {
                    CameraProgress = 0f;
                    ZoomProgress = 0f;
                    CameraOffset = Vector2.Zero;
                }
            }
        }

        /// <summary>
        /// 获取当前运镜产生的额外缩放值
        /// </summary>
        public static float GetZoomBoost() {
            return TargetZoomBoost * ZoomProgress * Intensity;
        }

        /// <summary>
        /// 判断指定NPC是否为可骇入目标
        /// </summary>
        public static bool IsHackableTarget(NPC npc) {
            if (npc == null || !npc.active) return false;
            //在赛博空间范围内的敌对生物为可骇入目标
            if (!Cyberspace.Active) return true;
            float dx = npc.Center.X - Main.LocalPlayer.Center.X;
            float dy = npc.Center.Y - Main.LocalPlayer.Center.Y;
            float effectiveRadius = Cyberspace.Radius * Cyberspace.ExpandProgress;
            return dx * dx + dy * dy <= effectiveRadius * effectiveRadius;
        }

        /// <summary>
        /// 立即重置所有状态
        /// </summary>
        public static void Reset() {
            Active = false;
            Intensity = 0f;
            targetIntensity = 0f;
            SelectedTargetIndex = -1;
            HoveredTargetIndex = -1;
            CameraProgress = 0f;
            ZoomProgress = 0f;
            ReticleTimer = 0f;
            CameraOffset = Vector2.Zero;
            cameraTo = Vector2.Zero;
            InfiniteHack = false;
            HackTimeFreeze.Deactivate();
            HackTimeRAM.Reset();
        }
    }
}
