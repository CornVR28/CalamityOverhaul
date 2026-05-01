using Terraria;

namespace CalamityOverhaul.Content.RAMSystems
{
    internal class RamCapacityUpgradeChip : BaseRamUpgradeChip
    {
        protected override bool CanApplyUpgrade => RamSystem.CanUseCapacityUpgradeChip;

        protected override void ApplyUpgrade(Player player) => RamSystem.TryUseCapacityUpgradeChip();
    }
}
