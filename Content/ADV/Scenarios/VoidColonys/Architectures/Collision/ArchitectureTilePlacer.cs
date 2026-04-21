using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.Collision
{
    /// <summary>
    /// 建筑隐形碰撞方块的放置与清除助手
    /// 把<see cref="ArchitectureCollisionMask"/>提供的字符网格烘焙到实际世界tile
    /// 所有放置位置会被记入调用方给出的列表，便于建筑消失时精准回收
    /// </summary>
    internal static class ArchitectureTilePlacer
    {
        /// <summary>
        /// 以贴图左上角tile坐标为锚点，按字符网格批量放置隐形碰撞方块
        /// 仅覆盖当前为空气的tile，避免砸掉玩家建造物或生成阶段的已有方块
        /// </summary>
        /// <param name="mask">字符网格，'#'=实心 '='=平台 其他忽略</param>
        /// <param name="topLeftTileX">蒙版左上角对应的世界tile X</param>
        /// <param name="topLeftTileY">蒙版左上角对应的世界tile Y</param>
        /// <param name="placed">追加记录放置位置的列表，由Actor持久持有</param>
        public static void Place(string[] mask, int topLeftTileX, int topLeftTileY, List<Point16> placed) {
            if (mask == null || mask.Length == 0) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            ushort solidType = (ushort)ModContent.TileType<VoidArchitectureSolidTile>();
            ushort platformType = (ushort)ModContent.TileType<VoidArchitecturePlatformTile>();

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            for (int j = 0; j < mask.Length; j++) {
                string row = mask[j];
                if (string.IsNullOrEmpty(row)) continue;
                for (int i = 0; i < row.Length; i++) {
                    char c = row[i];
                    if (c != ArchitectureCollisionMask.Solid && c != ArchitectureCollisionMask.Platform) continue;

                    int x = topLeftTileX + i;
                    int y = topLeftTileY + j;
                    if (!WorldGen.InWorld(x, y, 2)) continue;

                    Tile t = Main.tile[x, y];
                    if (t.HasTile) continue;

                    ushort type = c == ArchitectureCollisionMask.Platform ? platformType : solidType;
                    t.TileType = type;
                    t.HasTile = true;
                    t.Slope = SlopeType.Solid;
                    t.IsHalfBlock = false;
                    //平台样式沿用原版style0的帧图
                    if (c == ArchitectureCollisionMask.Platform) {
                        t.TileFrameX = 0;
                        t.TileFrameY = 0;
                    }

                    placed.Add(new Point16(x, y));
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (placed.Count > 0 && Main.netMode == NetmodeID.Server) {
                NetMessage.SendTileSquare(-1, minX, minY, maxX - minX + 1, maxY - minY + 1);
            }
        }

        /// <summary>
        /// 在一段水平区间内放置一行隐形平台，连接段Actor专用
        /// </summary>
        public static void PlaceRow(int tileX1, int tileX2, int tileY, List<Point16> placed) {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            ushort platformType = (ushort)ModContent.TileType<VoidArchitecturePlatformTile>();

            int lo = System.Math.Min(tileX1, tileX2);
            int hi = System.Math.Max(tileX1, tileX2);
            int count = 0;
            for (int x = lo; x <= hi; x++) {
                if (!WorldGen.InWorld(x, tileY, 2)) continue;
                Tile t = Main.tile[x, tileY];
                if (t.HasTile) continue;
                t.TileType = platformType;
                t.HasTile = true;
                t.Slope = SlopeType.Solid;
                t.IsHalfBlock = false;
                t.TileFrameX = 0;
                t.TileFrameY = 0;
                placed.Add(new Point16(x, tileY));
                count++;
            }
            if (count > 0 && Main.netMode == NetmodeID.Server) {
                NetMessage.SendTileSquare(-1, lo, tileY, hi - lo + 1, 1);
            }
        }

        /// <summary>
        /// 清除此前由Place/PlaceRow登记的所有碰撞方块
        /// 只会移除本体确实属于隐形建筑tile的格子，避免误伤玩家手动置入的方块
        /// </summary>
        public static void Clear(List<Point16> placed) {
            if (placed == null || placed.Count == 0) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) { placed.Clear(); return; }

            ushort solidType = (ushort)ModContent.TileType<VoidArchitectureSolidTile>();
            ushort platformType = (ushort)ModContent.TileType<VoidArchitecturePlatformTile>();

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            for (int i = 0; i < placed.Count; i++) {
                Point16 p = placed[i];
                if (!WorldGen.InWorld(p.X, p.Y, 2)) continue;
                Tile t = Main.tile[p.X, p.Y];
                if (!t.HasTile) continue;
                if (t.TileType != solidType && t.TileType != platformType) continue;

                t.HasTile = false;
                t.TileType = 0;
                t.TileFrameX = 0;
                t.TileFrameY = 0;

                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }

            if (minX != int.MaxValue && Main.netMode == NetmodeID.Server) {
                NetMessage.SendTileSquare(-1, minX, minY, maxX - minX + 1, maxY - minY + 1);
            }

            placed.Clear();
        }
    }
}
