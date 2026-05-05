namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    internal sealed class BalancedGripModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Grip;
        //平衡青铜
        public override Color TintColor => new(220, 180, 120);

        public override void Apply(ref ShootContext ctx) {
            ctx.SpreadMul += -0.2f;
            ctx.AttackSpeedMul += 0.08f;
            ctx.DamageMul += 0.06f;
        }
    }
}
