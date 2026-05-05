namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    internal sealed class MultiCellFrameModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Frame;
        //多重荧绿
        public override Color TintColor => new(100, 255, 80);

        public override void Apply(ref ShootContext ctx) {
            ctx.BeamCountAdd += 2;
            ctx.DamageMul += -0.2f;
            ctx.SpreadMul += 0.4f;
        }
    }
}
