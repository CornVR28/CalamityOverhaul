namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    /// <summary>
    /// 狙击瞄具：大幅提升弹速与射程，牺牲追踪能力与攻速
    /// 配合聚束枪管形成超远程单点打击
    /// </summary>
    internal sealed class SniperOpticModule : SHPCModuleItem
    {
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Optic;
        //狙击冷白
        public override Color TintColor => new(220, 240, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.BeamSpeedMul += 1.5f;
            ctx.BeamLifeMul += 0.8f;
            ctx.DamageMul += 0.3f;
            ctx.AttackSpeedMul += -0.35f;
            ctx.HomingMul += -0.7f;
            ctx.SpreadMul += -0.5f;
        }
    }
}
