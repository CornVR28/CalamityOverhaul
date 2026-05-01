using CalamityOverhaul.Content.HackTimes.Scannables;

namespace CalamityOverhaul.Content.HackTimes.Targets
{
    /// <summary>
    /// 物块目标种类工厂
    /// <br/>悬停优先级最低，作为兜底（无 NPC、灵异、信号塔、炮台时才检测物块）
    /// </summary>
    internal class TileTargetType : HackTargetType
    {
        public override HackTargetKind Kind => HackTargetKind.Tile;

        public override int HoverPriority => 0;

        public override IHackTarget TryDetectHovered(Vector2 mouseWorld) {
            if (!TileScannable.TryGetScannableTile(mouseWorld, out int tx, out int ty)) return null;
            return new TileScannable(tx, ty);
        }
    }
}
