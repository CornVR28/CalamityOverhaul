namespace CalamityOverhaul.Content.HackTimes.Targets
{
    /// <summary>
    /// 液体目标种类工厂
    /// <br/>优先级低于物块，只有没有实体物块目标覆盖时才作为兜底分析对象
    /// </summary>
    internal class WaterTargetType : HackTargetType
    {
        public override HackTargetKind Kind => HackTargetKind.Water;

        public override int HoverPriority => -10;

        public override IHackTarget TryDetectHovered(Vector2 mouseWorld) {
            if (!WaterScannable.TryGetScannableLiquid(mouseWorld, out int tx, out int ty)) return null;
            return new WaterScannable(tx, ty);
        }
    }
}
