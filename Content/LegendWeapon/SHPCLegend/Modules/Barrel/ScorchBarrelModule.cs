namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    /// <summary>
    /// 灼烧激光枪管：激光命中NPC时施加持续灼烧debuff
    /// 单次伤害较低但DoT填补输出缺口，通过 ScorchOnHit 字段消费
    /// </summary>
    internal sealed class ScorchBarrelModule : SHPCModuleItem
    {
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Barrel;
        //灼焰橙红
        public override Color TintColor => new(255, 80, 20);

        public override void Apply(ref ShootContext ctx) {
            ctx.LaserMode = true;
            ctx.LaserScorchOnHit = true;
            ctx.LaserScorchDuration = 240;
            ctx.DamageMul += -0.2f;
            ctx.ManaCostMul += 0.25f;
        }
    }
}
