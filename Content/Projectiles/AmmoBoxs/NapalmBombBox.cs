using Terraria;

namespace CalamityOverhaul.Content.Projectiles.AmmoBoxs
{
    internal class NapalmBombBox : BaseAmmoBox
    {
        public override bool ClickBehavior(Player player, CWRItem cwr) {
            return false;
        }
    }
}
