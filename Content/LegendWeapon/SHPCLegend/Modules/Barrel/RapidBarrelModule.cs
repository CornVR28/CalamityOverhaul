namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    internal sealed class RapidBarrelModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Barrel;
        //快速节奏的青色霓虹
        public override Color TintColor => new(0, 240, 220);

        public override void Apply(ref ShootContext ctx) {
            ctx.AttackSpeedMul += 0.5f;
            ctx.DamageMul += -0.2f;
            ctx.SpreadMul += 0.3f;
        }
    }
}
