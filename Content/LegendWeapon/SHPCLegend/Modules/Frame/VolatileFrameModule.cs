namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    internal sealed class VolatileFrameModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Frame;
        //不稳定毒黄
        public override Color TintColor => new(220, 255, 40);

        public override void Apply(ref ShootContext ctx) {
            ctx.BeamCountAdd += 1;
            ctx.CritAdd += 10;
            ctx.ManaCostMul += 0.40f;
            ctx.SpreadMul += 0.15f;
        }
    }
}
