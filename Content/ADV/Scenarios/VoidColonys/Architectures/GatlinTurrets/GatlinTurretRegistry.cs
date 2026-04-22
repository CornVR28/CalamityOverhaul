using System.Collections.Generic;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.GatlinTurrets
{
    /// <summary>
    /// 加特林炮台放置记录，坐标为炮台底座左上角的世界像素
    /// </summary>
    internal readonly struct GatlinTurretEntry(int pedestalPixelX, int pedestalPixelY, bool initialFaceLeft)
    {
        public readonly int PedestalPixelX = pedestalPixelX;
        public readonly int PedestalPixelY = pedestalPixelY;
        /// <summary>无目标时的静止朝向，仅决定闲置时枪管指向</summary>
        public readonly bool InitialFaceLeft = initialFaceLeft;
    }

    /// <summary>
    /// 加特林炮台放置注册表
    /// 由ArchitecturePlacer在规划核心簇时填充，Spawner进入子世界后据此生成炮台Actor
    /// </summary>
    internal static class GatlinTurretRegistry
    {
        public static List<GatlinTurretEntry> Entries { get; } = [];

        public static void Clear() => Entries.Clear();

        public static void Add(int pedestalPixelX, int pedestalPixelY, bool initialFaceLeft)
            => Entries.Add(new GatlinTurretEntry(pedestalPixelX, pedestalPixelY, initialFaceLeft));
    }
}
