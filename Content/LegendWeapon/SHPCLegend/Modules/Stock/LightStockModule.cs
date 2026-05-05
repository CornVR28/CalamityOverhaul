namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    internal sealed class LightStockModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Stock;
        //碳纤维浅青
        public override Color TintColor => new(160, 240, 240);

        public override void Apply(ref ShootContext ctx) {
            ctx.AttackSpeedMul += 0.35f;
            ctx.DamageMul += -0.15f;
            ctx.SpreadMul += 0.2f;
        }
    }
}
