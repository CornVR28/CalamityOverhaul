namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    internal sealed class CrystalGripModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Grip;
        //水晶幻紫
        public override Color TintColor => new(200, 130, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.ManaCostMul += -0.2f;
            ctx.CritAdd += 5;
            ctx.ChargeTimeMul += 0.10f;
        }
    }
}
