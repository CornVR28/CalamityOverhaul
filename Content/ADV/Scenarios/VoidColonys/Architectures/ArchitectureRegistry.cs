using System.Collections.Generic;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures
{
    /// <summary>一条建筑放置记录，坐标为贴图左上角的世界像素</summary>
    internal readonly struct ArchitectureEntry(ArchitectureType type, int pixelX, int pixelY)
    {
        public readonly ArchitectureType Type = type;
        public readonly int PixelX = pixelX;
        public readonly int PixelY = pixelY;
    }

    /// <summary>一条水平连接段记录，端点为世界像素坐标整数</summary>
    internal readonly struct ConnectorEntry(PortKind kind, int startX, int startY, int endX)
    {
        public readonly PortKind Kind = kind;
        public readonly int StartX = startX;
        public readonly int StartY = startY;
        public readonly int EndX = endX;
    }

    /// <summary>
    /// 虚空聚落建筑放置注册表
    /// 由世界生成阶段的ArchitecturePlacer填充，Spawner在进入子世界后据此生成Actor
    /// 仅在当前子世界会话内有效，重新生成世界时会被清空
    /// </summary>
    internal static class ArchitectureRegistry
    {
        public static List<ArchitectureEntry> Entries { get; } = [];
        public static List<ConnectorEntry> Connectors { get; } = [];

        public static void Clear() {
            Entries.Clear();
            Connectors.Clear();
        }

        public static void Add(ArchitectureType type, int pixelX, int pixelY)
            => Entries.Add(new ArchitectureEntry(type, pixelX, pixelY));

        public static void AddConnector(PortKind kind, int startX, int startY, int endX)
            => Connectors.Add(new ConnectorEntry(kind, startX, startY, endX));
    }
}
