namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    internal sealed class HarmonyGripModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Grip;
        //节能薄荷绿
        public override Color TintColor => new(120, 255, 180);

        public override void Apply(ref ShootContext ctx) {
            ctx.ManaCostMul += -0.35f;
        }
    }
}
