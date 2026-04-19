using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.PastBridges
{
    /// <summary>
    /// 过去残垣生成器
    /// 在核心岛与卫星岛表面搭建一批用过去虚空骨架组成的残缺房间
    /// 所有方块只写入PastTileRegistry不直接放置，由时空切换系统按需驱动
    /// </summary>
    internal static class PastRoomGen
    {
        //房间尺寸范围
        private const int CoreRoomWidth = 26;
        private const int CoreRoomHeight = 14;
        private const int SatelliteRoomWidth = 16;
        private const int SatelliteRoomHeight = 9;

        //房顶每格缺失概率，营造破损感
        private const float RoofGapChance = 0.32f;
        //墙体随机缺口概率
        private const float WallGapChance = 0.08f;
        //地板接地探测最大下探深度
        private const int FloorProbeDepth = 24;

        /// <summary>
        /// 构建所有过去残垣并注册
        /// </summary>
        public static void BuildAll() {
            PastTileRegistry.Clear();

            ushort pastType = (ushort)ModContent.TileType<VoidFrameworkPast>();

            var core = IslandRegistry.FindByTag("核心实验室");
            if (core != null) {
                BuildRoomOnIsland(core, CoreRoomWidth, CoreRoomHeight, pastType, seedOffset: 0);
            }

            var satellites = IslandRegistry.GetByTier(IslandTier.Satellite);
            for (int i = 0; i < satellites.Count; i++) {
                var sat = satellites[i];
                BuildRoomOnIsland(sat, SatelliteRoomWidth, SatelliteRoomHeight, pastType, seedOffset: 1000 + i * 37);
            }
        }

        /// <summary>
        /// 在单个岛屿表面上建造一间残缺房间
        /// 先探测岛屿中心向外一段范围的地面高度，用均值决定地板Y
        /// 房间左右墙与屋顶带有破损缺口
        /// </summary>
        private static void BuildRoomOnIsland(IslandData island, int roomWidth, int roomHeight,
            ushort pastType, int seedOffset) {
            int left = island.CenterX - roomWidth / 2;
            int right = left + roomWidth - 1;

            //采样地板高度，取有效点平均值，避免倾斜地形造成悬空
            int sumY = 0;
            int samples = 0;
            for (int x = left + 1; x < right; x += 2) {
                int sy = island.GetSurfaceAt(x);
                if (sy > 0) {
                    sumY += sy;
                    samples++;
                }
            }
            if (samples < 3) return;
            int floorY = sumY / samples;
            int roofY = floorY - roomHeight;

            //局部确定性随机，避免跨存档差异
            int seed = island.NoiseSeed ^ seedOffset;
            var rng = new Random(seed);

            int w = Main.maxTilesX;
            int h = Main.maxTilesY;

            //地板层：沿岛屿实际表面向下一格，填满底座让房间粘合在岛上
            for (int x = left; x <= right; x++) {
                int localFloor = island.GetSurfaceAt(x);
                if (localFloor < 0) localFloor = floorY;
                //把地板向下填1格做加固，向上对齐标准floorY
                int yAnchor = Math.Min(floorY, localFloor);
                if ((uint)yAnchor >= (uint)h) continue;
                PastTileRegistry.Add(x, yAnchor, pastType);
            }

            //左右墙
            for (int y = roofY + 1; y < floorY; y++) {
                if ((uint)y >= (uint)h) continue;
                if (rng.NextDouble() > WallGapChance) {
                    PastTileRegistry.Add(left, y, pastType);
                }
                if (rng.NextDouble() > WallGapChance) {
                    PastTileRegistry.Add(right, y, pastType);
                }
            }

            //屋顶：中间段密集，两端渐稀；再叠加随机缺口
            for (int x = left; x <= right; x++) {
                if ((uint)roofY >= (uint)h) continue;
                //两端最外一格为必放承接立柱头
                bool edge = x == left || x == right;
                if (!edge && rng.NextDouble() < RoofGapChance) continue;
                PastTileRegistry.Add(x, roofY, pastType);
            }

            //屋顶再上一层的装饰性残柱，仅在两端与中央
            int midX = (left + right) / 2;
            int yPeak = roofY - 1;
            if ((uint)yPeak < (uint)h) {
                PastTileRegistry.Add(left, yPeak, pastType);
                PastTileRegistry.Add(right, yPeak, pastType);
                if (rng.NextDouble() < 0.7f) {
                    PastTileRegistry.Add(midX, yPeak, pastType);
                }
            }

            //屋内残留家具轮廓：一段矮墙/残桌，横跨2到4格，放在地板上一层
            int furnitureCount = rng.Next(1, 3);
            for (int i = 0; i < furnitureCount; i++) {
                int fLen = rng.Next(2, 5);
                int fx = left + 2 + rng.Next(Math.Max(1, roomWidth - 4 - fLen));
                int fy = floorY - 1;
                if ((uint)fy >= (uint)h) continue;
                for (int k = 0; k < fLen; k++) {
                    PastTileRegistry.Add(fx + k, fy, pastType);
                }
                //偶尔再叠一层，形成小柜子
                if (rng.NextDouble() < 0.45f) {
                    int fy2 = fy - 1;
                    if ((uint)fy2 < (uint)h) {
                        PastTileRegistry.Add(fx, fy2, pastType);
                        PastTileRegistry.Add(fx + fLen - 1, fy2, pastType);
                    }
                }
            }
        }
    }
}
