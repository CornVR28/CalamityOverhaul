namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    internal sealed class AdaptiveOpticModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Optic;
        //智能跟踪洋红
        public override Color TintColor => new(255, 70, 200);

        public override void Apply(ref ShootContext ctx) {
            ctx.HomingMul += 0.5f;
            ctx.AttackSpeedMul += 0.05f;
            ctx.CritAdd += 5;
        }
    }
}
