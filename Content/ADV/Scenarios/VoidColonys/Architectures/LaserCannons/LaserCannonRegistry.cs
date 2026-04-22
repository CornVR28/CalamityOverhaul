using System.Collections.Generic;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.LaserCannons
{
    /// <summary>
    /// 巨型激光炮台放置记录，坐标为底座左上角的世界像素
    /// </summary>
    internal readonly struct LaserCannonEntry(int pedestalPixelX, int pedestalPixelY, bool initialFaceLeft)
    {
        public readonly int PedestalPixelX = pedestalPixelX;
        public readonly int PedestalPixelY = pedestalPixelY;
        /// <summary>无目标时的静止朝向，true代表朝左</summary>
        public readonly bool InitialFaceLeft = initialFaceLeft;
    }

    /// <summary>
    /// 巨型激光炮台放置注册表，由ArchitecturePlacer填充，Spawner据此生成Actor
    /// </summary>
    internal static class LaserCannonRegistry
    {
        public static List<LaserCannonEntry> Entries { get; } = [];

        public static void Clear() => Entries.Clear();

        public static void Add(int pedestalPixelX, int pedestalPixelY, bool initialFaceLeft)
            => Entries.Add(new LaserCannonEntry(pedestalPixelX, pedestalPixelY, initialFaceLeft));
    }
}
