using CalamityOverhaul.Content.HackTimes;
using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.RAMSystems
{
    /// <summary>
    /// CWR 全局 RAM 资源系统
    /// <br/>从原 <c>HackTimeRAM</c> 升级而来，定位为独立的资源管理框架而非骇客时间附属机制
    /// <br/>提供"永久基础值（持久化） + 动态修饰器（运行时聚合）"双层架构：
    /// <list type="bullet">
    ///   <item><see cref="BaseMaxRam"/>/<see cref="BaseRecoveryRate"/>：通过物品/进度永久提升，跨存档保留</item>
    ///   <item><see cref="IRamModifierProvider"/>：义体、Buff、Cyberware 等动态来源每帧聚合</item>
    /// </list>
    /// 每帧得出生效值 <see cref="MaxRam"/>/<see cref="RecoveryRate"/> 供外部读取
    /// </summary>
    internal class RamSystem : ICWRLoader
    {
        void ICWRLoader.UnLoadData() => UnloadReset();

        private static RAMPlayer Local => Main.LocalPlayer.GetModPlayer<RAMPlayer>();

        #region 默认值与边界

        /// <summary>
        /// 默认基础 RAM 上限
        /// </summary>
        public const int DefaultBaseMaxRam = 8;
        /// <summary>
        /// 默认基础每秒恢复量（约 5 秒恢复 1 格）
        /// </summary>
        public const float DefaultBaseRecoveryRate = 0.2f;
        /// <summary>
        /// 基础上限的最小值（防极端配置）
        /// </summary>
        public const int MinBaseMaxRam = 1;
        /// <summary>
        /// 基础上限的软上限（与 HUD 弧条最大跨度联动，防止绕一整圈）
        /// </summary>
        public const int SoftMaxBaseMaxRam = 64;
        /// <summary>
        /// RAM 上限芯片最多可使用次数
        /// </summary>
        public const int MaxCapacityUpgradeChips = 24;
        /// <summary>
        /// RAM 恢复速度芯片最多可使用次数
        /// </summary>
        public const int MaxRecoveryUpgradeChips = 30;
        /// <summary>
        /// 单个 RAM 上限芯片提供的基础上限
        /// </summary>
        public const int CapacityUpgradeChipBonus = 1;
        /// <summary>
        /// 单个 RAM 恢复速度芯片提供的基础每秒恢复量
        /// </summary>
        public const float RecoveryUpgradeChipBonus = 0.05f;
        /// <summary>
        /// 消耗 RAM 后到开始恢复的延迟（秒）
        /// </summary>
        public const float RecoveryDelay = 1.5f;
        //tModLoader 固定每秒 60 tick
        private const float TickSeconds = 1f / 60f;

        #endregion

        #region 永久基础值（委托至 RAMPlayer 实例）

        public static int UsedCapacityUpgradeChips => Local.UsedCapacityUpgradeChips;
        public static int UsedRecoveryUpgradeChips => Local.UsedRecoveryUpgradeChips;

        public static int BaseMaxRam {
            get => Local.BaseMaxRam;
            set => Local.BaseMaxRam = value;
        }

        public static float BaseRecoveryRate {
            get => Local.BaseRecoveryRate;
            set => Local.BaseRecoveryRate = value;
        }

        #endregion

        #region 生效值（委托至 RAMPlayer 实例）

        public static int MaxRam => Local.MaxRam;
        public static float RecoveryRate => Local.RecoveryRate;

        public static float CurrentRam {
            get => Local.CurrentRam;
            set => Local.CurrentRam = value;
        }

        public static int DisplayCurrent => Local.DisplayCurrent;
        public static float Ratio => Local.Ratio;

        #endregion

        #region 动态修饰器注册表

        public static void RegisterProvider(IRamModifierProvider provider) {
            if (provider == null) {
                return;
            }
            if (!Local.Providers.Contains(provider)) {
                Local.Providers.Add(provider);
            }
        }

        public static void UnregisterProvider(IRamModifierProvider provider) {
            if (provider != null) {
                Local.Providers.Remove(provider);
            }
        }

        /// <summary>
        /// 当前已注册的修饰器数量（仅供调试/UI 展示）
        /// </summary>
        public static int ProviderCount => Local.Providers.Count;

        #endregion

        #region 永久升级 API

        /// <summary>
        /// 永久增加 RAM 基础上限（差值会写入持久化基础值）
        /// </summary>
        public static void IncreaseBaseMaxRamBy(int delta) => BaseMaxRam = BaseMaxRam + delta;
        /// <summary>
        /// 永久增加每秒基础恢复量（差值会写入持久化基础值）
        /// </summary>
        public static void IncreaseBaseRecoveryRateBy(float delta) => BaseRecoveryRate = BaseRecoveryRate + delta;
        /// <summary>
        /// 当前是否还能使用 RAM 上限芯片
        /// </summary>
        public static bool CanUseCapacityUpgradeChip => UsedCapacityUpgradeChips < MaxCapacityUpgradeChips;
        /// <summary>
        /// 当前是否还能使用 RAM 恢复速度芯片
        /// </summary>
        public static bool CanUseRecoveryUpgradeChip => UsedRecoveryUpgradeChips < MaxRecoveryUpgradeChips;

        /// <summary>
        /// 使用一枚 RAM 上限芯片，并同步永久基础值
        /// </summary>
        public static bool TryUseCapacityUpgradeChip() {
            if (!CanUseCapacityUpgradeChip) {
                return false;
            }
            var local = Local;
            local.UsedCapacityUpgradeChips++;
            local.BaseMaxRam = DefaultBaseMaxRam + local.UsedCapacityUpgradeChips * CapacityUpgradeChipBonus;
            local.RecomputeEffective();
            Restore(CapacityUpgradeChipBonus);
            return true;
        }

        public static bool TryUseRecoveryUpgradeChip() {
            if (!CanUseRecoveryUpgradeChip) {
                return false;
            }
            var local = Local;
            local.UsedRecoveryUpgradeChips++;
            local.BaseRecoveryRate = DefaultBaseRecoveryRate + local.UsedRecoveryUpgradeChips * RecoveryUpgradeChipBonus;
            local.RecomputeEffective();
            return true;
        }

        #endregion

        #region 消耗与恢复

        public static bool CanAfford(int cost) {
            if (HackTime.InfiniteHack) {
                return true;
            }
            return Local.CurrentRam >= cost;
        }

        public static bool TryConsume(int cost) {
            if (HackTime.InfiniteHack) {
                return true;
            }
            var local = Local;
            if (local.CurrentRam < cost) {
                return false;
            }
            float prev = local.CurrentRam;
            local.CurrentRam -= cost;
            if (local.CurrentRam < 0f) {
                local.CurrentRam = 0f;
            }
            local.RecoveryCooldown = RecoveryDelay;
            if (prev > 0f && local.CurrentRam <= 0f) {
                local.InvokeOnDepleted();
            }
            return true;
        }

        public static void ConsumeOverTime(float ramPerSecond) {
            if (HackTime.InfiniteHack) {
                return;
            }
            if (ramPerSecond <= 0f) {
                return;
            }
            var local = Local;
            float prev = local.CurrentRam;
            local.CurrentRam -= ramPerSecond * TickSeconds;
            if (local.CurrentRam < 0f) {
                local.CurrentRam = 0f;
            }
            if (prev > 0f && local.CurrentRam <= 0f) {
                local.InvokeOnDepleted();
            }
        }

        public static void Restore(float amount) {
            if (amount <= 0f) {
                return;
            }
            var local = Local;
            local.CurrentRam = Math.Min(local.CurrentRam + amount, local.MaxRam);
        }

        public static void Refill() => Local.Refill();

        #endregion

        #region 每帧更新

        public static void Update() {
            var local = Local;
            local.RecomputeEffective();
            if (HackTime.Active) {
                return;
            }
            if (local.RecoveryCooldown > 0f) {
                local.RecoveryCooldown -= TickSeconds;
                return;
            }
            if (local.CurrentRam < local.MaxRam) {
                local.CurrentRam += local.RecoveryRate * TickSeconds;
                if (local.CurrentRam > local.MaxRam) {
                    local.CurrentRam = local.MaxRam;
                }
            }
        }

        #endregion

        #region 重置

        public static void Reset() {
            var local = Local;
            local.RecoveryCooldown = 0f;
            local.RecomputeEffective();
            local.CurrentRam = local.MaxRam;
        }

        public static void UnloadReset() {
            //数据生命周期由 ModPlayer 实例管理，无需手动清理
        }

        #endregion
    }
}
