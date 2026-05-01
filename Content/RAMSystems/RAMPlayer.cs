using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.RAMSystems
{
    internal class RAMPlayer : ModPlayer
    {
        private const string SaveKey_CapacityChips = "CWRRam_CapacityChips";
        private const string SaveKey_RecoveryChips = "CWRRam_RecoveryChips";
        private const string SaveKey_BaseMax = "CWRRam_BaseMax";
        private const string SaveKey_BaseRecover = "CWRRam_BaseRecover";

        #region 持久化字段

        private int _usedCapacityUpgradeChips;
        public int UsedCapacityUpgradeChips {
            get => _usedCapacityUpgradeChips;
            internal set => _usedCapacityUpgradeChips = Math.Clamp(value, 0, RamSystem.MaxCapacityUpgradeChips);
        }

        private int _usedRecoveryUpgradeChips;
        public int UsedRecoveryUpgradeChips {
            get => _usedRecoveryUpgradeChips;
            internal set => _usedRecoveryUpgradeChips = Math.Clamp(value, 0, RamSystem.MaxRecoveryUpgradeChips);
        }

        private int _baseMaxRam = RamSystem.DefaultBaseMaxRam;
        public int BaseMaxRam {
            get => _baseMaxRam;
            internal set => _baseMaxRam = Math.Clamp(value, RamSystem.MinBaseMaxRam, RamSystem.SoftMaxBaseMaxRam);
        }

        private float _baseRecoveryRate = RamSystem.DefaultBaseRecoveryRate;
        public float BaseRecoveryRate {
            get => _baseRecoveryRate;
            internal set => _baseRecoveryRate = Math.Max(value, 0f);
        }

        #endregion

        #region 运行时字段

        public int MaxRam { get; internal set; } = RamSystem.DefaultBaseMaxRam;
        public float RecoveryRate { get; internal set; } = RamSystem.DefaultBaseRecoveryRate;

        private float _currentRam = RamSystem.DefaultBaseMaxRam;
        public float CurrentRam {
            get => _currentRam;
            internal set => _currentRam = Math.Clamp(value, 0f, MaxRam);
        }

        public int DisplayCurrent => (int)CurrentRam;
        public float Ratio => MaxRam > 0 ? CurrentRam / MaxRam : 0f;

        internal float RecoveryCooldown;
        internal readonly List<IRamModifierProvider> Providers = [];
        internal event Action OnDepleted;

        #endregion

        public override void SaveData(TagCompound tag) {
            tag[SaveKey_CapacityChips] = UsedCapacityUpgradeChips;
            tag[SaveKey_RecoveryChips] = UsedRecoveryUpgradeChips;
        }

        public override void LoadData(TagCompound tag) {
            if (tag == null) {
                UsedCapacityUpgradeChips = 0;
                UsedRecoveryUpgradeChips = 0;
            }
            else {
                UsedCapacityUpgradeChips = tag.TryGet(SaveKey_CapacityChips, out int cap)
                    ? cap : GetLegacyCapacityChipCount(tag);
                UsedRecoveryUpgradeChips = tag.TryGet(SaveKey_RecoveryChips, out int rec)
                    ? rec : GetLegacyRecoveryChipCount(tag);
            }
            BaseMaxRam = RamSystem.DefaultBaseMaxRam + UsedCapacityUpgradeChips * RamSystem.CapacityUpgradeChipBonus;
            BaseRecoveryRate = RamSystem.DefaultBaseRecoveryRate + UsedRecoveryUpgradeChips * RamSystem.RecoveryUpgradeChipBonus;
            RecomputeEffective();
            Refill();
        }

        public override void OnEnterWorld() {
            RecomputeEffective();
            Refill();
        }

        internal void RecomputeEffective() {
            int max = BaseMaxRam;
            float rec = BaseRecoveryRate;
            for (int i = 0; i < Providers.Count; i++) {
                IRamModifierProvider p = Providers[i];
                if (p == null || !p.IsActive) {
                    continue;
                }
                max += p.MaxRamBonus;
                rec += p.RecoveryRateBonus;
            }
            max = Math.Clamp(max, RamSystem.MinBaseMaxRam, RamSystem.SoftMaxBaseMaxRam);
            if (rec < 0f) {
                rec = 0f;
            }
            MaxRam = max;
            RecoveryRate = rec;
            if (_currentRam > MaxRam) {
                _currentRam = MaxRam;
            }
        }

        internal void Refill() {
            CurrentRam = MaxRam;
            RecoveryCooldown = 0f;
        }

        internal void InvokeOnDepleted() => OnDepleted?.Invoke();

        private int GetLegacyCapacityChipCount(TagCompound tag) {
            if (!tag.TryGet(SaveKey_BaseMax, out int max)) {
                return 0;
            }
            return Math.Clamp(max - RamSystem.DefaultBaseMaxRam, 0, RamSystem.MaxCapacityUpgradeChips);
        }

        private int GetLegacyRecoveryChipCount(TagCompound tag) {
            if (!tag.TryGet(SaveKey_BaseRecover, out float rec)) {
                return 0;
            }
            int count = (int)MathF.Round((rec - RamSystem.DefaultBaseRecoveryRate) / RamSystem.RecoveryUpgradeChipBonus);
            return Math.Clamp(count, 0, RamSystem.MaxRecoveryUpgradeChips);
        }
    }
}
