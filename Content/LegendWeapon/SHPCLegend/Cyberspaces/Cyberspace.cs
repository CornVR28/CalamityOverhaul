using System;
using Terraria;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces
{
    /// <summary>
    /// 赛博空间领域系统的静态门面，所有运行时状态实际承载在 <see cref="CyberspacePlayer"/> 上
    /// <br/>本类仅保留：常量配置、跨玩家共享的视觉参数，以及对本地玩家的便捷读写转发
    /// <br/>这样既保持原有调用点零修改，又把数据真正下沉到玩家实例
    /// </summary>
    internal class Cyberspace : ICWRLoader
    {
        void ICWRLoader.UnLoadData() => Reset();

        // ====== 常量配置（跨玩家共享） ======

        /// <summary>
        /// 最大支持层数
        /// </summary>
        public const int MaxLayerCount = 3;

        /// <summary>
        /// 激活/升层最低应能维持的秒数
        /// </summary>
        public const float MinSustainSeconds = 1f;

        /// <summary>
        /// RAM 崩溃后强制锁定的剩余帧数
        /// </summary>
        internal const int CrashLockoutFrames = 90;

        /// <summary>
        /// 领域中心缓动总帧数
        /// </summary>
        internal const int DomainEaseTotal = 28;

        /// <summary>
        /// 玩家速度换算 MotionFade 的满速度阈值
        /// </summary>
        internal const float MotionFadeFullSpeed = 5.5f;

        //每层半径相对于基础半径的倍率
        private static readonly float[] LayerRadiusScale = { 1.0f, 1.7f, 2.6f };

        /// <summary>
        /// 各层维持领域时每秒消耗的 RAM 量
        /// </summary>
        public static readonly float[] LayerRamDrainPerSecond = { 0.2f, 1.6f, 6f };

        //爆发阶段每层持续帧数
        internal static readonly int[] BurstDurations = { 14, 24, 36 };

        //常规展开 lerp 速率，高层更缓
        internal static readonly float[] ExpandLerps = { 0.035f, 0.020f, 0.013f };

        //收缩 lerp 速率，高层更缓
        internal static readonly float[] ContractLerps = { 0.050f, 0.030f, 0.020f };

        /// <summary>
        /// 基础半径，跨玩家共享的视觉配置
        /// </summary>
        public static float BaseRadius = 600f;

        /// <summary>
        /// 方形栅格单元边长
        /// </summary>
        public static float GridSize = 24f;

        /// <summary>
        /// 场景压暗强度
        /// </summary>
        public static float DimStrength = 0.85f;

        // ====== 本地玩家访问 ======

        /// <summary>
        /// 本地玩家的领域状态实例；玩家未就绪时返回 null
        /// </summary>
        internal static CyberspacePlayer Local {
            get {
                Player p = Main.LocalPlayer;
                if (p == null || !p.active) {
                    return null;
                }
                return p.GetModPlayer<CyberspacePlayer>();
            }
        }

        // ====== 转发到本地玩家的属性 ======

        public static bool Active => Local?.Active ?? false;

        public static float Intensity {
            get => Local?.Intensity ?? 0f;
            set { if (Local is { } lp) lp.Intensity = value; }
        }

        public static float RestartCollapse {
            get => Local?.RestartCollapse ?? 0f;
            set { if (Local is { } lp) lp.RestartCollapse = value; }
        }

        public static Vector2 DomainCenter => Local?.DomainCenter ?? Vector2.Zero;

        public static int CurrentLayer => Local?.CurrentLayer ?? 0;

        public static int RenderLayerCount => Local?.RenderLayerCount ?? 0;

        public static float Radius => Local?.Radius ?? BaseRadius;

        public static float ExpandProgress => Local?.ExpandProgress ?? 0f;

        public static float EffectiveOuterRadius => Local?.EffectiveOuterRadius ?? 0f;

        public static float EffectTime => Local?.EffectTime ?? 0f;

        public static float MotionFade => Local?.MotionFade ?? 0f;

        public static bool IsCrashLockedOut => Local?.IsCrashLockedOut ?? false;

        // ====== 静态计算方法 ======

        /// <summary>
        /// 指定层(0-indexed)的完整半径，纯计算不依赖玩家
        /// </summary>
        public static float GetLayerRadius(int layerIndex) {
            layerIndex = Math.Clamp(layerIndex, 0, MaxLayerCount - 1);
            return BaseRadius * LayerRadiusScale[layerIndex];
        }

        /// <summary>
        /// 指定层(1..MaxLayerCount)的 RAM 每秒消耗量
        /// </summary>
        public static float GetLayerDrainRate(int layer) {
            if (layer < 1 || layer > MaxLayerCount) {
                return 0f;
            }
            return LayerRamDrainPerSecond[layer - 1];
        }

        /// <summary>
        /// 本地玩家当前层的 RAM 每秒消耗量
        /// </summary>
        public static float GetCurrentDrainRate() {
            int layer = CurrentLayer;
            if (!Active || layer < 1 || layer > MaxLayerCount) {
                return 0f;
            }
            return LayerRamDrainPerSecond[layer - 1];
        }

        public static float GetLayerExpand(int layerIndex) => Local?.GetLayerExpand(layerIndex) ?? 0f;

        public static bool CanAffordLayer(int layer) => Local?.CanAffordLayer(layer) ?? false;

        public static bool IsInsideDomain(Vector2 worldPos) => Local?.IsInsideDomain(worldPos) ?? false;

        // ====== 操作方法 ======

        /// <summary>
        /// 手动切换赛博空间领域开关，仅作用于传入玩家
        /// </summary>
        public static bool Toggle(Player owner) {
            if (owner == null) return false;
            return owner.GetModPlayer<CyberspacePlayer>().Toggle();
        }

        /// <summary>
        /// 激活赛博空间领域，仅作用于传入玩家
        /// </summary>
        public static void Activate(Player owner) {
            if (owner == null) return;
            owner.GetModPlayer<CyberspacePlayer>().Activate();
        }

        /// <summary>
        /// 设置层数；不传 owner 时默认作用于本地玩家
        /// </summary>
        public static void SetLayer(int layer, Player owner = null) {
            owner ??= Main.LocalPlayer;
            if (owner == null) return;
            owner.GetModPlayer<CyberspacePlayer>().SetLayer(layer);
        }

        /// <summary>
        /// 关闭领域，作用于本地玩家
        /// </summary>
        public static void Deactivate() => Local?.Deactivate();

        /// <summary>
        /// 触发系统崩溃，作用于本地玩家
        /// </summary>
        public static void TriggerSystemCrash() => Local?.TriggerSystemCrash();

        /// <summary>
        /// 通知瞬移锚点，作用于本地玩家
        /// </summary>
        public static void NotifyTeleport(Vector2 anchorCenter) => Local?.NotifyTeleport(anchorCenter);

        /// <summary>
        /// 主更新入口；遍历所有在线玩家更新各自的赛博空间状态
        /// <br/>视觉渲染目前仍只读本地玩家，遍历是为了让多玩家各自维护自己的状态机，
        /// 便于以后扩展为联机同步显示
        /// </summary>
        public static void Update() {
            for (int i = 0; i < Main.maxPlayers; i++) {
                Player p = Main.player[i];
                if (p == null || !p.active) continue;
                p.GetModPlayer<CyberspacePlayer>().Update();
            }
        }

        /// <summary>
        /// 重置所有在线玩家的领域状态
        /// </summary>
        public static void Reset() {
            for (int i = 0; i < Main.maxPlayers; i++) {
                Player p = Main.player[i];
                if (p == null || !p.active) continue;
                p.GetModPlayer<CyberspacePlayer>().Reset();
            }
        }
    }
}
