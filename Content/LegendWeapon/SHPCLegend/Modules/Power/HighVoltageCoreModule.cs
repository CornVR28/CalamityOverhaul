namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    internal sealed class HighVoltageCoreModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Power;
        //高压电蓝
        public override Color TintColor => new(80, 180, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.DamageMul += 0.15f;
            ctx.MergedDamageBonus += 0.5f;
            ctx.ManaCostMul += 0.30f;
        }
    }
}
