namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    internal sealed class KineticDamperModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Stock;
        //减震橄榄绿
        public override Color TintColor => new(140, 180, 90);

        public override void Apply(ref ShootContext ctx) {
            ctx.SpreadMul += -0.5f;
            ctx.AttackSpeedMul += -0.08f;
            ctx.CritAdd += 3;
        }
    }
}
