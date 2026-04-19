using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.PastBridges
{
    /// <summary>
    /// 过去物块全局注册表
    /// 每条记录描述一个格子的"过去形态"与"现在形态"配对
    /// 过去态的typePast必填，现在态的typePresent为0表示"现在该格为空气"
    /// 入侵过去时将记录替换为过去态，回到现在时还原为现在态
    /// 采用SOA数组布局以减少GC压力并提升批量遍历缓存命中
    /// </summary>
    internal static class PastTileRegistry
    {
        //结构初始化容量，按4千砖估算，可自动增长
        private const int InitialCapacity = 4096;

        private static int[] xs = new int[InitialCapacity];
        private static int[] ys = new int[InitialCapacity];
        private static ushort[] typePast = new ushort[InitialCapacity];
        private static ushort[] typePresent = new ushort[InitialCapacity];
        private static int count;

        //批量刷新边界框，Apply/Restore完成后一次性RangeFrame
        private static int minX, minY, maxX, maxY;
        private static bool hasBounds;

        /// <summary>当前记录的格子数</summary>
        public static int Count => count;

        /// <summary>清空注册表，世界生成开始时调用</summary>
        public static void Clear() {
            count = 0;
            hasBounds = false;
        }

        /// <summary>
        /// 追加一条过去态记录
        /// typePresentValue为0代表"现在该格应保持空气"，常用于桥梁类纯过去结构
        /// </summary>
        public static void Add(int x, int y, ushort typePastValue, ushort typePresentValue = 0) {
            if (count >= xs.Length) {
                int newCap = xs.Length * 2;
                Array.Resize(ref xs, newCap);
                Array.Resize(ref ys, newCap);
                Array.Resize(ref typePast, newCap);
                Array.Resize(ref typePresent, newCap);
            }
            xs[count] = x;
            ys[count] = y;
            typePast[count] = typePastValue;
            typePresent[count] = typePresentValue;
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

        /// <summary>
        /// 切换到过去态
        /// 空格直接置为过去方块；已有"现在态"的格替换为过去方块
        /// 跳过既不是空也不是现在态的格，防止覆盖玩家放置
        /// </summary>
        public static void Apply() {
            if (count == 0) return;

            int w = Main.maxTilesX;
            int h = Main.maxTilesY;
            for (int i = 0; i < count; i++) {
                int x = xs[i];
                int y = ys[i];
                if ((uint)x >= (uint)w || (uint)y >= (uint)h) continue;
                Tile tile = Main.tile[x, y];
                ushort past = typePast[i];
                ushort present = typePresent[i];

                if (!tile.HasTile) {
                    //现在为空气，允许放置过去方块
                    if (present != 0) continue;
                    tile.HasTile = true;
                    tile.TileType = past;
                    tile.IsHalfBlock = false;
                }
                else if (tile.TileType == present) {
                    //现在态匹配，原地替换为过去态
                    tile.TileType = past;
                    tile.IsHalfBlock = false;
                }
                //其他情况视为玩家修改或生成意外，保持不动
            }

            FrameBounds();
        }

        /// <summary>
        /// 切换回现在态
        /// 仅当当前格确实是过去方块时才动手，避免误删
        /// typePresent为0时清除格子，否则替换回现在态
        /// </summary>
        public static void Restore() {
            if (count == 0) return;

            int w = Main.maxTilesX;
            int h = Main.maxTilesY;
            for (int i = 0; i < count; i++) {
                int x = xs[i];
                int y = ys[i];
                if ((uint)x >= (uint)w || (uint)y >= (uint)h) continue;
                Tile tile = Main.tile[x, y];
                if (!tile.HasTile) continue;
                if (tile.TileType != typePast[i]) continue;

                ushort present = typePresent[i];
                if (present == 0) {
                    tile.HasTile = false;
                    tile.TileType = 0;
                }
                else {
                    tile.TileType = present;
                    tile.IsHalfBlock = false;
                }
            }

            FrameBounds();
        }

        /// <summary>一次性刷新所有记录外接矩形内的视觉分帧</summary>
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
