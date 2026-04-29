using CalamityOverhaul.Content.HackTimes;
using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces;
using System;
using System.Collections.Generic;
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
        /// 消耗 RAM 后到开始恢复的延迟（秒）
        /// </summary>
        public const float RecoveryDelay = 1.5f;
        //tModLoader 固定每秒 60 tick
        private const float TickSeconds = 1f / 60f;

        //存档键
        private const string SaveKey_BaseMax = "CWRRam_BaseMax";
        private const string SaveKey_BaseRecover = "CWRRam_BaseRecover";

        #endregion

        #region 永久基础值（可持久化）

        private static int _baseMaxRam = DefaultBaseMaxRam;
        /// <summary>
        /// 永久 RAM 基础上限，供物品/进度等永久升级写入，会被持久化到玩家存档
        /// </summary>
        public static int BaseMaxRam {
            get => _baseMaxRam;
            set => _baseMaxRam = Math.Clamp(value, MinBaseMaxRam, SoftMaxBaseMaxRam);
        }

        private static float _baseRecoveryRate = DefaultBaseRecoveryRate;
        /// <summary>
        /// 永久基础每秒恢复量，写入会持久化
        /// </summary>
        public static float BaseRecoveryRate {
            get => _baseRecoveryRate;
            set => _baseRecoveryRate = Math.Max(value, 0f);
        }

        #endregion

        #region 生效值（聚合后只读）

        /// <summary>
        /// 当前生效的 RAM 上限（基础 + 所有激活修饰器）
        /// </summary>
        public static int MaxRam { get; private set; } = DefaultBaseMaxRam;
        /// <summary>
        /// 当前生效的每秒恢复量（基础 + 所有激活修饰器）
        /// </summary>
        public static float RecoveryRate { get; private set; } = DefaultBaseRecoveryRate;

        private static float _currentRam = DefaultBaseMaxRam;
        /// <summary>
        /// 当前可用 RAM（精确浮点值，显示时取整）
        /// </summary>
        public static float CurrentRam {
            get => _currentRam;
            set => _currentRam = Math.Clamp(value, 0f, MaxRam);
        }

        /// <summary>
        /// 当前可用 RAM 的整数显示值
        /// </summary>
        public static int DisplayCurrent => (int)CurrentRam;
        /// <summary>
        /// 当前 RAM 占最大值的比例(0~1)
        /// </summary>
        public static float Ratio => MaxRam > 0 ? CurrentRam / MaxRam : 0f;

        #endregion

        #region 动态修饰器注册表

        private static readonly List<IRamModifierProvider> _providers = [];

        /// <summary>
        /// 注册一个动态 RAM 修饰来源，重复注册会被忽略
        /// </summary>
        public static void RegisterProvider(IRamModifierProvider provider) {
            if (provider == null) {
                return;
            }
            if (!_providers.Contains(provider)) {
                _providers.Add(provider);
            }
        }

        /// <summary>
        /// 注销修饰来源
        /// </summary>
        public static void UnregisterProvider(IRamModifierProvider provider) {
            if (provider != null) {
                _providers.Remove(provider);
            }
        }

        /// <summary>
        /// 当前已注册的修饰器数量（仅供调试/UI 展示）
        /// </summary>
        public static int ProviderCount => _providers.Count;

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

        #endregion

        #region 事件

        /// <summary>
        /// 当 RAM 由非零跌至零时触发，可被外部系统订阅做"系统崩溃"反应
        /// </summary>
        public static event Action OnDepleted;

        #endregion

        #region 消耗与恢复

        private static float recoveryCooldown;

        /// <summary>
        /// 检查是否有足够 RAM 执行指定一次性消耗
        /// </summary>
        public static bool CanAfford(int cost) {
            if (HackTime.InfiniteHack) {
                return true;
            }
            return CurrentRam >= cost;
        }

        /// <summary>
        /// 一次性消耗 RAM，返回是否成功
        /// </summary>
        public static bool TryConsume(int cost) {
            if (HackTime.InfiniteHack) {
                return true;
            }
            if (CurrentRam < cost) {
                return false;
            }
            float prev = CurrentRam;
            CurrentRam -= cost;
            if (CurrentRam < 0f) {
                CurrentRam = 0f;
            }
            recoveryCooldown = RecoveryDelay;
            if (prev > 0f && CurrentRam <= 0f) {
                OnDepleted?.Invoke();
            }
            return true;
        }

        /// <summary>
        /// 持续消耗：按"每秒消耗量"在当前帧扣除一帧的份额
        /// <br/>不触发恢复冷却（连续消耗自身已抑制恢复，避免冷却被反复刷新）
        /// </summary>
        public static void ConsumeOverTime(float ramPerSecond) {
            if (HackTime.InfiniteHack) {
                return;
            }
            if (ramPerSecond <= 0f) {
                return;
            }
            float prev = CurrentRam;
            CurrentRam -= ramPerSecond * TickSeconds;
            if (CurrentRam < 0f) {
                CurrentRam = 0f;
            }
            if (prev > 0f && CurrentRam <= 0f) {
                OnDepleted?.Invoke();
            }
        }

        /// <summary>
        /// 击杀回收：恢复指定量 RAM（不超过上限，不触发冷却）
        /// </summary>
        public static void Restore(float amount) {
            if (amount <= 0f) {
                return;
            }
            CurrentRam = Math.Min(CurrentRam + amount, MaxRam);
        }

        /// <summary>
        /// 将 RAM 充满（仅用于初始化或重置状态）
        /// </summary>
        public static void Refill() {
            CurrentRam = MaxRam;
            recoveryCooldown = 0f;
        }

        #endregion

        #region 每帧更新

        /// <summary>
        /// 重新聚合所有修饰器得到当前生效值，并在 <see cref="MaxRam"/> 变小时夹紧 <see cref="CurrentRam"/>
        /// </summary>
        private static void RecomputeEffective() {
            int max = BaseMaxRam;
            float rec = BaseRecoveryRate;
            for (int i = 0; i < _providers.Count; i++) {
                IRamModifierProvider p = _providers[i];
                if (p == null || !p.IsActive) {
                    continue;
                }
                max += p.MaxRamBonus;
                rec += p.RecoveryRateBonus;
            }
            if (max < MinBaseMaxRam) {
                max = MinBaseMaxRam;
            }
            if (max > SoftMaxBaseMaxRam) {
                max = SoftMaxBaseMaxRam;
            }
            if (rec < 0f) {
                rec = 0f;
            }
            MaxRam = max;
            RecoveryRate = rec;
            if (_currentRam > MaxRam) {
                _currentRam = MaxRam;
            }
        }

        /// <summary>
        /// 每帧推进：聚合修饰器、推进恢复
        /// <br/>骇客时间或赛博空间领域激活期间冻结自动恢复
        /// </summary>
        public static void Update() {
            RecomputeEffective();

            //骇客时间或赛博空间激活期间不恢复（资源消耗与恢复不应同时进行）
            if (HackTime.Active || Cyberspace.Active) {
                return;
            }

            if (recoveryCooldown > 0f) {
                recoveryCooldown -= TickSeconds;
                return;
            }

            if (CurrentRam < MaxRam) {
                CurrentRam += RecoveryRate * TickSeconds;
                if (CurrentRam > MaxRam) {
                    CurrentRam = MaxRam;
                }
            }
        }

        #endregion

        #region 重置

        /// <summary>
        /// 软重置：保留 <see cref="BaseMaxRam"/> 与 <see cref="BaseRecoveryRate"/>，仅清理瞬时状态
        /// <br/>用于回到主菜单等非卸载场景，避免清掉玩家进度
        /// </summary>
        public static void Reset() {
            recoveryCooldown = 0f;
            RecomputeEffective();
            CurrentRam = MaxRam;
        }

        /// <summary>
        /// 全量重置：连基础值与修饰器表都清空，仅由模组卸载路径调用
        /// </summary>
        public static void UnloadReset() {
            _providers.Clear();
            OnDepleted = null;
            BaseMaxRam = DefaultBaseMaxRam;
            BaseRecoveryRate = DefaultBaseRecoveryRate;
            MaxRam = DefaultBaseMaxRam;
            RecoveryRate = DefaultBaseRecoveryRate;
            _currentRam = DefaultBaseMaxRam;
            recoveryCooldown = 0f;
        }

        #endregion

        #region 持久化

        /// <summary>
        /// 写入存档：基础上限与基础恢复速度
        /// <br/>由 <c>CWRPlayer.SaveData</c> 在保存玩家时调用
        /// </summary>
        public static void WriteSave(TagCompound tag) {
            tag[SaveKey_BaseMax] = BaseMaxRam;
            tag[SaveKey_BaseRecover] = BaseRecoveryRate;
        }

        /// <summary>
        /// 读取存档：未命中字段时回落到默认值，确保新角色与旧角色数据兼容
        /// </summary>
        public static void ReadSave(TagCompound tag) {
            BaseMaxRam = tag != null && tag.TryGet(SaveKey_BaseMax, out int max)
                ? Math.Clamp(max, MinBaseMaxRam, SoftMaxBaseMaxRam)
                : DefaultBaseMaxRam;
            BaseRecoveryRate = tag != null && tag.TryGet(SaveKey_BaseRecover, out float rec)
                ? Math.Max(rec, 0f)
                : DefaultBaseRecoveryRate;
            RecomputeEffective();
            Refill();
        }

        #endregion
    }
}
