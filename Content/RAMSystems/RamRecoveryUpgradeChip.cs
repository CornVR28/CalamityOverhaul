using Terraria;

namespace CalamityOverhaul.Content.RAMSystems
{
    internal class RamRecoveryUpgradeChip : BaseRamUpgradeChip
    {
        protected override bool CanApplyUpgrade => RamSystem.CanUseRecoveryUpgradeChip;

        protected override void ApplyUpgrade(Player player) => RamSystem.TryUseRecoveryUpgradeChip();
    }
}
