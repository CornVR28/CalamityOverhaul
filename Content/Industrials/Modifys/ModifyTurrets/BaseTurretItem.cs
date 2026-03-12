using InnoVault.GameSystem;
using Terraria;

namespace CalamityOverhaul.Content.Industrials.Modifys.ModifyTurrets
{
    internal abstract class BaseTurretItem : ItemOverride
    {
        public override bool CanLoadLocalization => false;
        public override void SetDefaults(Item item) {
            item.CWR().StorageUE = true;
            item.CWR().ConsumeUseUE = 1000;
        }
    }
}
