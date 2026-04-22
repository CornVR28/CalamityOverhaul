using CalamityOverhaul.Common;
using System;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys
{
    /// <summary>
    /// 虚空聚落地形生成器
    /// 生成漂浮在虚空中的岛屿群：核心岛→卫星岛→多圈哨站岛→碎片岛，由内向外扩散至地图边缘
    /// 所有岛屿信息注册到 <see cref="IslandRegistry"/>，供后续建筑放置、桥梁连接等系统使用
    /// </summary>
    internal class VoidColonyGen : GenPass
    {
        private const int SafePadding = 10;

        //物块类型缓存
        private static int typePlating;
        private static int typeFramework;

        public VoidColonyGen() : base("Void Colony", 110) { }

        private static bool InWorldSafe(int x, int y) {
            return x >= SafePadding && x < Main.maxTilesX - SafePadding
                && y >= SafePadding && y < Main.maxTilesY - SafePadding;
        }

        private static int ChooseTileType(int surfaceDepth, float distFromCenter) {
            if (surfaceDepth < 2) return typePlating;
            if (surfaceDepth < 5)
                return WorldGen.genRand.NextFloat() < 0.7f ? typePlating : typeFramework;
            if (WorldGen.genRand.NextFloat() < 0.12f) return typePlating;
            return typeFramework;
        }

        /// <summary>
        /// 检查候选岛屿是否在世界安全范围内
        /// </summary>
        private static bool IslandInBounds(int cx, int cy, int hw, int topT, int botD,
            int worldWidth, int worldHeight) {
            return cx - hw - 5 >= SafePadding
                && cx + hw + 5 < worldWidth - SafePadding
                && cy - topT - 5 >= SafePadding
                && cy + botD + 5 < worldHeight - SafePadding;
        }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration) {
            int worldWidth = Main.maxTilesX;
            int worldHeight = Main.maxTilesY;
            int centerX = worldWidth / 2;
            int centerY = worldHeight / 2;
            int seed = Main.ActiveWorldFileData.Seed;

            typePlating = ModContent.TileType<VoidPlating>();
            typeFramework = ModContent.TileType<VoidFramework>();

            //清空注册表
            IslandRegistry.Clear();

            //============ 第一步：清空世界 ============
            progress.Message = "正在撕裂亚空间屏障...";
            ClearWorld(worldWidth, worldHeight);

            //============ 第二步：核心岛 ============
            progress.Message = "正在凝聚核心岛屿...";
            //半宽收窄到刚好覆盖核心实验室贴图与两侧外挑桁架，两翼留出虚空便于桥梁演出
            PlaceAndRegister(IslandTier.Core, centerX, centerY, 70, 28, 90, seed + 5000,
                "核心实验室", worldWidth, worldHeight);

            //============ 第三步：卫星岛（固定布局，对应草图标注位置） ============
            progress.Message = "正在牵引卫星岛屿...";
            //前两条为核心岛左右等高配对的桥梁中继浮岛，TopT对齐核心岛的极高，两翼的附属建筑然将落在它们上面
            //建筑间距InterBuildingGapPx=2800px≈五段桥拼接，故能源站中心距核心约-226tile，分析实验室中心距核心约+235tile
            //中继岛需覆盖建筑整个贴图宽度：能源站30tile/分析实验室48tile，因此hw向外放大以确保建筑底部完全落在岛面
            (int ox, int oy, int hw, int topT, int botD, string tag)[] satellites = [
                (-226, 0, 48, 28, 48, "桥梁中继_左"),
                (235, 0, 55, 28, 48, "桥梁中继_右"),
                (-380, -180, 55, 18, 48, "亚空间异界生物实验室_上"),
                (300, -220, 50, 16, 42, "超凡材料分析实验室"),
                (-420, 60, 45, 15, 38, "亚空间异界生物实验室_下"),
                (480, -50, 48, 16, 40, "能量控制站"),
                (0, 280, 52, 17, 44, "核心亚空间能量分析站"),
            ];

            for (int i = 0; i < satellites.Length; i++) {
                var (ox, oy, hw, topT, botD, tag) = satellites[i];
                PlaceAndRegister(IslandTier.Satellite, centerX + ox, centerY + oy,
                    hw, topT, botD, seed + 6000 + i * 333, tag, worldWidth, worldHeight);
            }

            //============ 第四步：多圈哨站岛，由内向外扩散至地图边缘 ============
            progress.Message = "正在展开哨站网络...";
            GenerateExpandingOutposts(centerX, centerY, worldWidth, worldHeight, seed);

            //============ 第五步：碎片岛，填充所有剩余空间 ============
            progress.Message = "正在散布亚空间碎片...";
            GenerateFragments(centerX, centerY, worldWidth, worldHeight, seed);

            //============ 最终：扫描表面 & 设置出生点 ============
            IslandRegistry.ScanAllSurfaces();

            //============ 第六步：规划并登记科技建筑，由ArchitectureSpawner负责生成Actor ============
            progress.Message = "正在部署虚空科技建筑...";
            Architectures.ArchitecturePlacer.BuildAll();

            var coreIsland = IslandRegistry.FindByTag("核心实验室");
            if (coreIsland != null) {
                Main.spawnTileX = coreIsland.CenterX;
                Main.spawnTileY = coreIsland.SurfaceY - 3;
            }
            else {
                Main.spawnTileX = centerX;
                Main.spawnTileY = centerY - 30;
            }

            Main.worldSurface = worldHeight - 2;
            Main.rockLayer = worldHeight - 1;

            for (int i = 0; i < Main.maxNPCs; i++) {
                Main.npc[i] = new NPC();
            }
        }

        /// <summary>
        /// 放置岛屿并注册到 IslandRegistry
        /// </summary>
        private static IslandData PlaceAndRegister(IslandTier tier, int cx, int cy,
            int hw, int topT, int botD, int noiseSeed, string tag,
            int worldWidth, int worldHeight) {
            if (!IslandInBounds(cx, cy, hw, topT, botD, worldWidth, worldHeight))
                return null;

            var data = IslandRegistry.Register(tier, cx, cy, hw, topT, botD, noiseSeed, tag);
            GenerateNaturalIsland(cx, cy, hw, topT, botD, noiseSeed);
            return data;
        }

        /// <summary>
        /// 尝试在指定位置放置岛屿，先检查与已有岛屿的重叠
        /// </summary>
        private static IslandData TryPlaceIsland(IslandTier tier, int cx, int cy,
            int hw, int topT, int botD, int noiseSeed, int minGap,
            int worldWidth, int worldHeight, string tag = null) {
            if (!IslandInBounds(cx, cy, hw, topT, botD, worldWidth, worldHeight))
                return null;
            if (IslandRegistry.HasOverlap(cx, cy, hw, topT, botD, minGap))
                return null;

            return PlaceAndRegister(tier, cx, cy, hw, topT, botD, noiseSeed, tag, worldWidth, worldHeight);
        }

        #region 清空世界

        private static void ClearWorld(int worldWidth, int worldHeight) {
            for (int x = 0; x < worldWidth; x++) {
                for (int y = 0; y < worldHeight; y++) {
                    Tile tile = Main.tile[x, y];
                    tile.HasTile = false;
                    tile.WallType = WallID.None;
                    tile.LiquidAmount = 0;
                    tile.LiquidType = LiquidID.Water;
                }
            }
        }

        #endregion

        #region 多圈哨站扩散

        /// <summary>
        /// 从核心向外生成多圈哨站岛，逐圈扩大半径直到覆盖整个地图
        /// 外圈岛屿更小更稀疏，模拟从实验室集群向外扩散的布局
        /// </summary>
        private static void GenerateExpandingOutposts(int cx, int cy,
            int worldWidth, int worldHeight, int seed) {
            //计算地图对角线的一半作为最大半径（确保覆盖到角落）
            float maxRadiusX = (worldWidth - SafePadding * 2) * 0.5f;
            float maxRadiusY = (worldHeight - SafePadding * 2) * 0.5f;
            float maxRadius = MathF.Sqrt(maxRadiusX * maxRadiusX + maxRadiusY * maxRadiusY);

            //第一圈从卫星岛外侧开始（约350格处）
            float ringRadius = 350f;
            //圈间距随距离增大
            float ringSpacing = 180f;
            int ringIndex = 0;

            while (ringRadius < maxRadius) {
                //每圈的岛屿数量：内圈密，外圈按比例增加保持角间距大致一致
                float circumference = MathHelper.TwoPi * ringRadius;
                //每80~120格角距放一个岛
                float angularSpacing = 90f + ringIndex * 8f;
                int islandCount = Math.Max(6, (int)(circumference / angularSpacing));

                //外圈岛屿逐渐变小
                float sizeFactor = Math.Clamp(1f - (ringRadius - 350f) / (maxRadius - 350f), 0.25f, 1f);

                //初始角度偏移，每圈错开避免径向对齐
                float angleOffset = ringIndex * 0.618f * MathHelper.TwoPi; //黄金角偏移

                FastNoiseLite ringNoise = new(seed + 8000 + ringIndex * 137);
                ringNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
                ringNoise.SetFrequency(0.01f);

                for (int i = 0; i < islandCount; i++) {
                    float angle = angleOffset + MathHelper.TwoPi * i / islandCount;
                    //径向扰动
                    float radialNoise = ringNoise.GetNoise(MathF.Cos(angle) * 100, MathF.Sin(angle) * 100) * ringSpacing * 0.35f;
                    float r = ringRadius + radialNoise;
                    //角度扰动
                    float angleNoise = ringNoise.GetNoise(i * 50f, ringIndex * 50f) * 0.15f;
                    angle += angleNoise;

                    //椭圆映射（世界通常比高度宽），Y轴压缩
                    float yScale = (float)worldHeight / worldWidth;
                    int ix = cx + (int)(MathF.Cos(angle) * r);
                    int iy = cy + (int)(MathF.Sin(angle) * r * yScale);

                    //岛屿尺寸随外圈递减
                    int hw = (int)(18 + WorldGen.genRand.Next(16) * sizeFactor);
                    int topT = (int)(7 + WorldGen.genRand.Next(7) * sizeFactor);
                    int botD = (int)(16 + WorldGen.genRand.Next(20) * sizeFactor);

                    int noiseSeed2 = seed + 8100 + ringIndex * 1000 + i * 77;
                    //最小间距：避免岛屿重叠，小岛间距小一些
                    int minGap = Math.Max(8, hw / 2);

                    TryPlaceIsland(IslandTier.Outpost, ix, iy, hw, topT, botD,
                        noiseSeed2, minGap, worldWidth, worldHeight);
                }

                //圈间距逐渐增大（外围更稀疏）
                ringRadius += ringSpacing;
                ringSpacing += 15f;
                ringIndex++;
            }
        }

        #endregion

        #region 碎片岛填充

        /// <summary>
        /// 用碎片岛填充整个世界的空隙
        /// 采用网格+随机抖动的方式确保均匀分布，同时用碰撞检测避免重叠
        /// </summary>
        private static void GenerateFragments(int cx, int cy,
            int worldWidth, int worldHeight, int seed) {
            //网格间距（每个网格格子中尝试放一个碎片）
            int gridSpacing = 60;
            int gridCols = (worldWidth - SafePadding * 2) / gridSpacing;
            int gridRows = (worldHeight - SafePadding * 2) / gridSpacing;

            FastNoiseLite densityNoise = new(seed + 12000);
            densityNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            densityNoise.SetFrequency(0.004f);

            int placed = 0;

            for (int gx = 0; gx < gridCols; gx++) {
                for (int gy = 0; gy < gridRows; gy++) {
                    //网格中心
                    int baseCX = SafePadding + gx * gridSpacing + gridSpacing / 2;
                    int baseCY = SafePadding + gy * gridSpacing + gridSpacing / 2;

                    //密度噪声：某些区域碎片更密集
                    float density = densityNoise.GetNoise(baseCX, baseCY);
                    //太靠近核心的区域（已经有大岛了）降低碎片密度
                    float distToCenter = MathF.Sqrt(
                        (baseCX - cx) * (baseCX - cx) + (baseCY - cy) * (baseCY - cy));
                    if (distToCenter < 180) continue; //核心区完全跳过

                    //密度阈值：基础40%放置率，核心附近降低
                    float threshold = 0.6f;
                    if (distToCenter < 400) threshold += (400 - distToCenter) / 400 * 0.3f;
                    //密度噪声调节
                    threshold -= density * 0.2f;

                    if (WorldGen.genRand.NextFloat() > 1f - threshold) continue;

                    //在网格内随机抖动位置
                    int jitterX = WorldGen.genRand.Next(-gridSpacing / 3, gridSpacing / 3);
                    int jitterY = WorldGen.genRand.Next(-gridSpacing / 3, gridSpacing / 3);
                    int fragX = baseCX + jitterX;
                    int fragY = baseCY + jitterY;

                    //碎片尺寸
                    int hw = 3 + WorldGen.genRand.Next(7);
                    int topT = 2 + WorldGen.genRand.Next(4);
                    int botD = 4 + WorldGen.genRand.Next(10);
                    int noiseSeed2 = seed + 12100 + placed * 131;

                    if (TryPlaceIsland(IslandTier.Fragment, fragX, fragY, hw, topT, botD,
                        noiseSeed2, 5, worldWidth, worldHeight) != null) {
                        placed++;
                    }
                }
            }
        }

        #endregion

        #region 自然浮岛核心算法

        /// <summary>
        /// 生成一个自然形态的浮岛
        /// 上表面有起伏的丘陵轮廓，下方是多层递减的倒锥/钟乳石形态
        /// 内部有侵蚀空洞
        /// </summary>
        private static void GenerateNaturalIsland(int cx, int cy, int halfWidth,
            int topThickness, int bottomDepth, int noiseSeed) {
            //大尺度轮廓
            FastNoiseLite shapeNoise = new(noiseSeed);
            shapeNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            shapeNoise.SetFrequency(0.012f);

            //中尺度细节
            FastNoiseLite edgeNoise = new(noiseSeed + 100);
            edgeNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            edgeNoise.SetFrequency(0.035f);

            //小尺度粗糙度
            FastNoiseLite roughNoise = new(noiseSeed + 200);
            roughNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
            roughNoise.SetFrequency(0.08f);

            //侵蚀噪声
            FastNoiseLite erosionNoise = new(noiseSeed + 300);
            erosionNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            erosionNoise.SetFrequency(0.05f);

            //钟乳石噪声
            FastNoiseLite stalactiteNoise = new(noiseSeed + 400);
            stalactiteNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            stalactiteNoise.SetFrequency(0.06f);

            int scanHalfWidth = halfWidth + 5;

            for (int x = cx - scanHalfWidth; x <= cx + scanHalfWidth; x++) {
                if (!InWorldSafe(x, cy)) continue;

                float dx = (x - cx) / (float)halfWidth;
                float absDx = MathF.Abs(dx);

                float shapeMod = shapeNoise.GetNoise(x, cy) * 0.15f;
                float edgeMod = edgeNoise.GetNoise(x, cy) * 0.08f;
                float effectiveEdge = absDx - shapeMod - edgeMod;

                //上部
                float surfaceUndulation = shapeNoise.GetNoise(x, cy - 50) * 6f
                    + edgeNoise.GetNoise(x, cy - 50) * 3f
                    + roughNoise.GetNoise(x, cy - 50) * 1.5f;

                float topFalloff = 1f - MathF.Pow(Math.Clamp(effectiveEdge, 0f, 1f), 2.0f);
                if (topFalloff <= 0.02f) continue;

                int surfaceY = cy - (int)(topThickness * 0.4f + surfaceUndulation * topFalloff);
                int effectiveThickness = Math.Max(3, (int)(topThickness * topFalloff));

                for (int dy = 0; dy < effectiveThickness; dy++) {
                    int py = surfaceY + dy;
                    if (!InWorldSafe(x, py)) continue;
                    if (topFalloff < 0.3f && roughNoise.GetNoise(x * 2, py * 2) > 0.3f)
                        continue;
                    WorldGen.PlaceTile(x, py, ChooseTileType(dy, absDx), mute: true, forced: true);
                }

                //下部
                int bottomStartY = surfaceY + effectiveThickness;
                float bottomFalloff = 1f - MathF.Pow(Math.Clamp(effectiveEdge, 0f, 1f), 1.5f);
                if (bottomFalloff <= 0.01f) continue;

                float baseDepth = bottomDepth * bottomFalloff;
                float stalactiteMod = stalactiteNoise.GetNoise(x, cy + 100);
                if (stalactiteMod > 0.2f) {
                    baseDepth += (stalactiteMod - 0.2f) * bottomDepth * 0.6f;
                }
                baseDepth += edgeNoise.GetNoise(x, cy + 80) * 8f;
                int totalDepth = Math.Max(3, (int)baseDepth);

                for (int dy = 0; dy < totalDepth; dy++) {
                    int py = bottomStartY + dy;
                    if (!InWorldSafe(x, py)) continue;

                    float depthProgress = dy / (float)totalDepth;
                    float widthCurve = 1f - MathF.Pow(depthProgress, 1.3f + absDx * 0.5f);
                    if (effectiveEdge > widthCurve) continue;

                    float erosionVal = erosionNoise.GetNoise(x, py);
                    float erosionThreshold = 0.35f - depthProgress * 0.15f;
                    if (erosionVal > erosionThreshold && depthProgress > 0.3f && depthProgress < 0.85f)
                        continue;

                    if (widthCurve - effectiveEdge < 0.15f && roughNoise.GetNoise(x * 2, py * 2) > 0.2f)
                        continue;

                    WorldGen.PlaceTile(x, py, ChooseTileType(effectiveThickness + dy, absDx), mute: true, forced: true);
                }
            }

            //钟乳石尖刺（只对较大的岛屿生成）
            if (halfWidth >= 12) {
                GenerateStalactites(cx, cy, halfWidth, bottomDepth, noiseSeed + 500);
            }
        }

        /// <summary>
        /// 在岛屿底部生成独立的钟乳石尖刺
        /// </summary>
        private static void GenerateStalactites(int cx, int cy, int halfWidth, int bottomDepth, int noiseSeed) {
            FastNoiseLite placeNoise = new(noiseSeed);
            placeNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            placeNoise.SetFrequency(0.04f);

            for (int x = cx - halfWidth + 5; x <= cx + halfWidth - 5; x += 3) {
                float placeVal = placeNoise.GetNoise(x, 0);
                if (placeVal < 0.1f) continue;
                if (WorldGen.genRand.NextFloat() > 0.4f) continue;

                int startY = cy + bottomDepth / 2;
                for (int searchY = cy; searchY < cy + bottomDepth + 30; searchY++) {
                    if (InWorldSafe(x, searchY) && !Main.tile[x, searchY].HasTile) {
                        startY = searchY;
                        break;
                    }
                }

                float heightFactor = (placeVal - 0.1f) / 0.9f;
                int spikeHeight = (int)(8 + heightFactor * 18 + WorldGen.genRand.Next(5));
                int baseHalfWidth = Math.Max(1, (int)(1 + heightFactor * 3));

                for (int dy = 0; dy < spikeHeight; dy++) {
                    float t = dy / (float)spikeHeight;
                    float currentHW = baseHalfWidth * (1f - MathF.Pow(t, 1.2f));
                    int intHW = (int)MathF.Ceiling(currentHW);
                    for (int ddx = -intHW; ddx <= intHW; ddx++) {
                        if (MathF.Abs(ddx) > currentHW) continue;
                        int px = x + ddx;
                        int py = startY + dy;
                        if (InWorldSafe(px, py) && !Main.tile[px, py].HasTile) {
                            WorldGen.PlaceTile(px, py, typeFramework, mute: true, forced: true);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
