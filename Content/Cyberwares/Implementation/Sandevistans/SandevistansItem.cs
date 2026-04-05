using CalamityOverhaul.Content.LegendWeapon.SHPCLegend;

namespace CalamityOverhaul.Content.Cyberwares.Implementation.Sandevistans
{
    internal class SandevistansItem : BaseCyberware
    {
        public override string Texture => CWRConstant.Item_Other + "SandevistansItem";

        public override CyberwareSlotCategory SlotCategory => CyberwareSlotCategory.NervousSystem;

        public override int CapacityCost => 3;
    }
}
