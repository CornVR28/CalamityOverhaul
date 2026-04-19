using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.PastBridges
{
    /// <summary>
    /// 过去桥梁全局注册表，记录每个"过去方块"的坐标与类型
    /// 使用SOA数组布局，减少GC压力并提升批量遍历缓存命中
    /// Apply在入侵过去时放置方块，Restore在回到现在时清除
    /// </summary>
    internal static class PastBridgeRegistry
    {
        //结构初始化容量，按4千砖估算，可自动增长
        private const int InitialCapacity = 4096;

        private static int[] xs = new int[InitialCapacity];
        private static int[] ys = new int[InitialCapacity];
        private static ushort[] types = new ushort[InitialCapacity];
        private static int count;

        //批量刷新边界框，Apply/Restore完成后一次性RangeFrame
        private static int minX, minY, maxX, maxY;
        private static bool hasBounds;

        //过去方块类型缓存，避免Apply每次ModContent.TileType查找
        private static ushort pastFrameworkType;

        /// <summary>当前记录的过去方块数</summary>
        public static int Count => count;

        /// <summary>清空注册表，世界生成开始时调用</summary>
        public static void Clear() {
            count = 0;
            hasBounds = false;
        }

        /// <summary>
        /// 追加一个过去方块记录
        /// 地图生成阶段被PastBridgeGen频繁调用
        /// </summary>
        public static void Add(int x, int y, ushort type) {
            if (count >= xs.Length) {
                int newCap = xs.Length * 2;
                Array.Resize(ref xs, newCap);
                Array.Resize(ref ys, newCap);
                Array.Resize(ref types, newCap);
            }
            xs[count] = x;
            ys[count] = y;
            types[count] = type;
            count++;

            if (!hasBounds) {
                minX = maxX = x;
                minY = maxY = y;
                hasBounds = true;
            }
            else {
                if (x < minX) minX = x;
                else if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                else if (y > maxY) maxY = y;
            }
        }

        /// <summary>解析并缓存过去方块类型</summary>
        private static void EnsureTypeCache() {
            if (pastFrameworkType == 0) {
                pastFrameworkType = (ushort)ModContent.TileType<VoidFrameworkPast>();
            }
        }

        /// <summary>
        /// 将所有记录的过去方块放置到世界中
        /// 只在当前格为空时写入，避免覆盖现有岛屿地形
        /// </summary>
        public static void Apply() {
            if (count == 0) return;
            EnsureTypeCache();

            int w = Main.maxTilesX;
            int h = Main.maxTilesY;
            for (int i = 0; i < count; i++) {
                int x = xs[i];
                int y = ys[i];
                if ((uint)x >= (uint)w || (uint)y >= (uint)h) continue;
                Tile tile = Main.tile[x, y];
                if (tile.HasTile) continue;
                tile.HasTile = true;
                tile.TileType = types[i];
                tile.IsHalfBlock = false;
            }

            FrameBounds();
        }

        /// <summary>
        /// 抹除所有记录的过去方块
        /// 仅当当前格为过去方块类型时清除，防止误删玩家放置或岛屿方块
        /// </summary>
        public static void Restore() {
            if (count == 0) return;
            EnsureTypeCache();

            int w = Main.maxTilesX;
            int h = Main.maxTilesY;
            for (int i = 0; i < count; i++) {
                int x = xs[i];
                int y = ys[i];
                if ((uint)x >= (uint)w || (uint)y >= (uint)h) continue;
                Tile tile = Main.tile[x, y];
                if (!tile.HasTile) continue;
                if (tile.TileType != types[i]) continue;
                tile.HasTile = false;
                tile.TileType = 0;
            }

            FrameBounds();
        }

        /// <summary>一次性刷新所有桥梁边界框内的视觉分帧</summary>
        private static void FrameBounds() {
            if (!hasBounds) return;
            int x0 = Math.Max(0, minX - 1);
            int y0 = Math.Max(0, minY - 1);
            int x1 = Math.Min(Main.maxTilesX - 1, maxX + 1);
            int y1 = Math.Min(Main.maxTilesY - 1, maxY + 1);
            WorldGen.RangeFrame(x0, y0, x1, y1);
        }
    }
}
