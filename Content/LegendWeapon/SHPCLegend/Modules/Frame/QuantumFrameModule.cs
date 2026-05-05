namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    internal sealed class QuantumFrameModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Frame;
        //量子超紫
        public override Color TintColor => new(140, 80, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.HomingMul += 0.4f;
            ctx.OrbSpeedMul += 0.4f;
            ctx.ManaCostMul += 0.15f;
        }
    }
}
