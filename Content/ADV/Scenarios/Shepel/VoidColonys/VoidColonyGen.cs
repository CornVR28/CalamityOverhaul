using CalamityOverhaul.Common;
using System;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.VoidColonys
{
    /// <summary>
    /// 虚空聚落地形生成器
    /// 生成漂浮在虚空中的岛屿群，核心岛屿在中心，周围环绕大小不一的卫星岛
    /// 建筑结构由后续的固定结构文件放置，此处只负责自然地形
    /// </summary>
    internal class VoidColonyGen : GenPass
    {
        private const int SafePadding = 10;

        //物块类型缓存，避免重复获取
        private static int typePlating;
        private static int typeFramework;

        public VoidColonyGen() : base("Void Colony", 110) { }

        private static bool InWorldSafe(int x, int y) {
            return x >= SafePadding && x < Main.maxTilesX - SafePadding
                && y >= SafePadding && y < Main.maxTilesY - SafePadding;
        }

        /// <summary>
        /// 根据深度和位置选择物块类型，增加材质变化
        /// surfaceDepth: 距离岛屿表面的深度, 0=表面
        /// </summary>
        private static int ChooseTileType(int surfaceDepth, float distFromCenter) {
            //表面2层使用镀层
            if (surfaceDepth < 2) return typePlating;
            //浅层混合
            if (surfaceDepth < 5) {
                return WorldGen.genRand.NextFloat() < 0.7f ? typePlating : typeFramework;
            }
            //深层以骨架为主，偶尔混入镀层
            if (WorldGen.genRand.NextFloat() < 0.12f) return typePlating;
            return typeFramework;
        }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration) {
            progress.Message = "正在撕裂亚空间屏障...";

            int worldWidth = Main.maxTilesX;
            int worldHeight = Main.maxTilesY;
            int centerX = worldWidth / 2;
            int centerY = worldHeight / 2;
            int seed = Main.ActiveWorldFileData.Seed;

            typePlating = ModContent.TileType<VoidPlating>();
            typeFramework = ModContent.TileType<VoidFramework>();

            //清空整个世界，制造纯虚空环境
            ClearWorld(worldWidth, worldHeight);

            progress.Message = "正在凝聚核心岛屿...";

            //核心浮岛（最大的中心岛屿，承载主实验室的基座）
            GenerateNaturalIsland(centerX, centerY, 130, 28, 90, seed + 5000);

            progress.Message = "正在牵引卫星岛屿...";

            //卫星浮岛（中型，对应图中标注的各实验室位置）
            //布局参考草图：上方两个、左右各一个、下方一个
            (int ox, int oy, int hw, int topT, int botD)[] satellites = [
                (-280, -180, 55, 18, 48),  //左上 - 亚空间异界生物实验室
                (200, -220, 50, 16, 42),   //右上 - 超凡材料分析实验室
                (-320, 60, 45, 15, 38),    //左   - 亚空间异界生物实验室
                (300, -50, 48, 16, 40),    //右   - 能量控制站
                (0, 220, 52, 17, 44),      //下方 - 核心亚空间能量分析站
            ];

            for (int i = 0; i < satellites.Length; i++) {
                var (ox, oy, hw, topT, botD) = satellites[i];
                int ix = centerX + ox;
                int iy = centerY + oy;
                if (ix - hw < SafePadding || ix + hw >= worldWidth - SafePadding) continue;
                if (iy < SafePadding + 60 || iy >= worldHeight - SafePadding - 60) continue;
                GenerateNaturalIsland(ix, iy, hw, topT, botD, seed + 6000 + i * 333);
                //卫星岛附近生成小型碎片岛
                GenerateDebrisField(ix, iy, hw, botD, seed + 6500 + i * 111, worldWidth, worldHeight);
            }

            progress.Message = "正在展开观察哨站...";

            //远端哨站岛（小型，环形分布在外围）
            GenerateOutpostRing(centerX, centerY, worldWidth, worldHeight, seed);

            progress.Message = "正在稳定亚空间锚点...";

            //极远处的微型碎片岛群，填充虚空中的空旷感
            GenerateScatteredFragments(centerX, centerY, worldWidth, worldHeight, seed);

            //核心岛周围额外的碎片场
            GenerateDebrisField(centerX, centerY, 130, 90, seed + 7777, worldWidth, worldHeight);

            //设置出生点在核心岛屿上表面
            Main.spawnTileX = centerX;
            Main.spawnTileY = FindIslandSurface(centerX, centerY) - 3;

            //推到底部防止地下背景渲染
            Main.worldSurface = worldHeight - 2;
            Main.rockLayer = worldHeight - 1;

            //清除所有NPC
            for (int i = 0; i < Main.maxNPCs; i++) {
                Main.npc[i] = new NPC();
            }
        }

        /// <summary>
        /// 清空整个世界
        /// </summary>
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

        /// <summary>
        /// 从某个Y坐标向下搜索，找到第一个有方块的位置（岛屿表面）
        /// </summary>
        private static int FindIslandSurface(int x, int startY) {
            for (int y = startY - 60; y < startY + 60; y++) {
                if (InWorldSafe(x, y) && Main.tile[x, y].HasTile) {
                    return y;
                }
            }
            return startY;
        }

        #region 自然浮岛核心算法

        /// <summary>
        /// 生成一个自然形态的浮岛
        /// 上表面有起伏的丘陵轮廓，下方是多层递减的倒锥/钟乳石形态
        /// 内部有侵蚀空洞，边缘有不规则碎裂
        /// </summary>
        /// <param name="cx">岛屿中心X</param>
        /// <param name="cy">岛屿中心Y（大约在上表面和下锥体的交界处）</param>
        /// <param name="halfWidth">岛屿半宽</param>
        /// <param name="topThickness">上部最大厚度</param>
        /// <param name="bottomDepth">下部最大深度</param>
        /// <param name="noiseSeed">噪声种子</param>
        private static void GenerateNaturalIsland(int cx, int cy, int halfWidth,
            int topThickness, int bottomDepth, int noiseSeed) {
            //============ 噪声层 ============
            //大尺度轮廓 - 控制岛屿整体形状的起伏
            FastNoiseLite shapeNoise = new(noiseSeed);
            shapeNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            shapeNoise.SetFrequency(0.012f);

            //中尺度细节 - 控制边缘凹凸
            FastNoiseLite edgeNoise = new(noiseSeed + 100);
            edgeNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            edgeNoise.SetFrequency(0.035f);

            //小尺度粗糙度 - 让表面不那么光滑
            FastNoiseLite roughNoise = new(noiseSeed + 200);
            roughNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
            roughNoise.SetFrequency(0.08f);

            //侵蚀噪声 - 用于在岛屿内部挖洞
            FastNoiseLite erosionNoise = new(noiseSeed + 300);
            erosionNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            erosionNoise.SetFrequency(0.05f);

            //钟乳石噪声 - 控制底部悬垂结构的分布
            FastNoiseLite stalactiteNoise = new(noiseSeed + 400);
            stalactiteNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            stalactiteNoise.SetFrequency(0.06f);

            //扩展宽度以容纳边缘碎裂
            int scanHalfWidth = halfWidth + 8;

            //============ 第一遍：构建岛屿主体 ============
            for (int x = cx - scanHalfWidth; x <= cx + scanHalfWidth; x++) {
                if (!InWorldSafe(x, cy)) continue;

                float dx = (x - cx) / (float)halfWidth; //归一化水平距离，-1到1
                float absDx = MathF.Abs(dx);

                //大尺度形状调制
                float shapeMod = shapeNoise.GetNoise(x, cy) * 0.15f;
                //中尺度边缘调制
                float edgeMod = edgeNoise.GetNoise(x, cy) * 0.08f;

                //有效宽度比 = 基础衰减 + 噪声扰动
                float effectiveEdge = absDx - shapeMod - edgeMod;

                //==== 上部 ====
                //上表面高度轮廓：中间略高，向边缘下降，叠加起伏
                float surfaceUndulation = shapeNoise.GetNoise(x, cy - 50) * 6f
                    + edgeNoise.GetNoise(x, cy - 50) * 3f
                    + roughNoise.GetNoise(x, cy - 50) * 1.5f;

                //边缘衰减曲线（柔和的平台边缘）
                float topFalloff = 1f - MathF.Pow(Math.Clamp(effectiveEdge, 0f, 1f), 2.0f);
                if (topFalloff <= 0.02f) continue; //完全在岛屿外

                //上表面Y：中心最高点
                int surfaceY = cy - (int)(topThickness * 0.4f + surfaceUndulation * topFalloff);
                //有效厚度
                int effectiveThickness = Math.Max(3, (int)(topThickness * topFalloff));

                //铺设上部方块
                for (int dy = 0; dy < effectiveThickness; dy++) {
                    int py = surfaceY + dy;
                    if (!InWorldSafe(x, py)) continue;

                    //小尺度粗糙：边缘随机缺失
                    if (topFalloff < 0.3f && roughNoise.GetNoise(x * 2, py * 2) > 0.3f)
                        continue;

                    int surfDepth = dy; //距表面深度
                    WorldGen.PlaceTile(x, py, ChooseTileType(surfDepth, absDx), mute: true, forced: true);
                }

                //==== 下部（倒锥/钟乳石） ====
                int bottomStartY = surfaceY + effectiveThickness;

                //基础下垂深度
                float bottomFalloff = 1f - MathF.Pow(Math.Clamp(effectiveEdge, 0f, 1f), 1.5f);
                if (bottomFalloff <= 0.01f) continue;

                float baseDepth = bottomDepth * bottomFalloff;
                //钟乳石局部加深
                float stalactiteMod = stalactiteNoise.GetNoise(x, cy + 100);
                if (stalactiteMod > 0.2f) {
                    //在高噪声区域形成较长的钟乳石尖
                    baseDepth += (stalactiteMod - 0.2f) * bottomDepth * 0.6f;
                }
                //中尺度扰动
                baseDepth += edgeNoise.GetNoise(x, cy + 80) * 8f;
                int totalDepth = Math.Max(3, (int)baseDepth);

                for (int dy = 0; dy < totalDepth; dy++) {
                    int py = bottomStartY + dy;
                    if (!InWorldSafe(x, py)) continue;

                    float depthProgress = dy / (float)totalDepth; //0=顶，1=底

                    //宽度随深度收缩，形成倒锥
                    //使用混合曲线：上半段缓慢收窄，下半段加速收窄
                    float widthCurve = 1f - MathF.Pow(depthProgress, 1.3f + absDx * 0.5f);
                    if (effectiveEdge > widthCurve) continue;

                    //深处侵蚀空洞
                    float erosionVal = erosionNoise.GetNoise(x, py);
                    //越深处越容易出现空洞
                    float erosionThreshold = 0.35f - depthProgress * 0.15f;
                    if (erosionVal > erosionThreshold && depthProgress > 0.3f && depthProgress < 0.85f)
                        continue;

                    //边缘粗糙
                    if (widthCurve - effectiveEdge < 0.15f && roughNoise.GetNoise(x * 2, py * 2) > 0.2f)
                        continue;

                    int surfDepth = effectiveThickness + dy;
                    WorldGen.PlaceTile(x, py, ChooseTileType(surfDepth, absDx), mute: true, forced: true);
                }
            }

            //============ 第二遍：生成悬挂的钟乳石尖刺 ============
            GenerateStalactites(cx, cy, halfWidth, bottomDepth, noiseSeed + 500);

            //============ 第三遍：边缘碎裂（在岛屿轮廓外侧零星放置散落方块） ============
            GenerateEdgeCrumble(cx, cy, halfWidth, topThickness, noiseSeed + 600);
        }

        /// <summary>
        /// 在岛屿底部生成独立的钟乳石尖刺
        /// 这些尖刺从岛屿底面向下延伸，粗细不一
        /// </summary>
        private static void GenerateStalactites(int cx, int cy, int halfWidth, int bottomDepth, int noiseSeed) {
            FastNoiseLite placeNoise = new(noiseSeed);
            placeNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            placeNoise.SetFrequency(0.04f);

            //在岛屿宽度范围内扫描
            for (int x = cx - halfWidth + 5; x <= cx + halfWidth - 5; x += 3) {
                float placeVal = placeNoise.GetNoise(x, 0);
                //只有噪声值足够高时才生成钟乳石
                if (placeVal < 0.1f) continue;
                //随机跳过一些，控制密度
                if (WorldGen.genRand.NextFloat() > 0.4f) continue;

                //从岛屿底面开始向下搜索，找到第一个没有方块的位置
                int startY = cy + bottomDepth / 2;
                for (int searchY = cy; searchY < cy + bottomDepth + 30; searchY++) {
                    if (InWorldSafe(x, searchY) && !Main.tile[x, searchY].HasTile) {
                        startY = searchY;
                        break;
                    }
                }

                //钟乳石参数
                float heightFactor = (placeVal - 0.1f) / 0.9f;
                int spikeHeight = (int)(8 + heightFactor * 18 + WorldGen.genRand.Next(5));
                int baseHalfWidth = Math.Max(1, (int)(1 + heightFactor * 3));

                //构建尖刺
                for (int dy = 0; dy < spikeHeight; dy++) {
                    float t = dy / (float)spikeHeight;
                    //从baseHalfWidth收窄到0
                    float currentHW = baseHalfWidth * (1f - MathF.Pow(t, 1.2f));

                    int intHW = (int)MathF.Ceiling(currentHW);
                    for (int dx = -intHW; dx <= intHW; dx++) {
                        if (MathF.Abs(dx) > currentHW) continue;
                        int px = x + dx;
                        int py = startY + dy;
                        if (InWorldSafe(px, py) && !Main.tile[px, py].HasTile) {
                            WorldGen.PlaceTile(px, py, typeFramework, mute: true, forced: true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 在岛屿边缘生成碎裂散落的方块
        /// 看起来像是岛屿边缘在亚空间侵蚀下崩碎
        /// </summary>
        private static void GenerateEdgeCrumble(int cx, int cy, int halfWidth, int topThickness, int noiseSeed) {
            FastNoiseLite crumbleNoise = new(noiseSeed);
            crumbleNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            crumbleNoise.SetFrequency(0.1f);

            //在岛屿边缘外围扫描
            int outerRange = 12;
            for (int x = cx - halfWidth - outerRange; x <= cx + halfWidth + outerRange; x++) {
                float absDx = MathF.Abs(x - cx) / (float)halfWidth;
                //只处理边缘区域（0.85~1.2范围）
                if (absDx < 0.82f || absDx > 1.3f) continue;

                for (int y = cy - topThickness; y < cy + topThickness * 2; y++) {
                    if (!InWorldSafe(x, y)) continue;
                    if (Main.tile[x, y].HasTile) continue; //已有方块跳过

                    float n = crumbleNoise.GetNoise(x, y);
                    //越靠近岛屿边缘越密集
                    float edgeDist = MathF.Abs(absDx - 1f);
                    float threshold = 0.2f + edgeDist * 2.5f;
                    if (n > threshold) continue;
                    if (WorldGen.genRand.NextFloat() > 0.35f) continue;

                    //散落方块
                    WorldGen.PlaceTile(x, y, WorldGen.genRand.NextBool() ? typePlating : typeFramework,
                        mute: true, forced: true);
                }
            }
        }

        #endregion

        #region 卫星岛与哨站

        /// <summary>
        /// 生成环形分布的远端哨站岛
        /// </summary>
        private static void GenerateOutpostRing(int cx, int cy, int worldWidth, int worldHeight, int seed) {
            FastNoiseLite posNoise = new(seed + 9000);
            posNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            posNoise.SetFrequency(0.005f);

            int outpostCount = 10 + WorldGen.genRand.Next(4);
            float baseRadius = 480f;

            for (int i = 0; i < outpostCount; i++) {
                float angle = MathHelper.TwoPi * i / outpostCount + WorldGen.genRand.NextFloat() * 0.35f;
                float radius = baseRadius + WorldGen.genRand.Next(-100, 140);
                float noiseOffset = posNoise.GetNoise(i * 100f, 0) * 70f;

                int islandX = cx + (int)(MathF.Cos(angle) * (radius + noiseOffset));
                int islandY = cy + (int)(MathF.Sin(angle) * (radius * 0.55f + noiseOffset * 0.4f));

                int hw = 18 + WorldGen.genRand.Next(14);
                if (islandX - hw - 10 < SafePadding || islandX + hw + 10 >= worldWidth - SafePadding) continue;
                if (islandY < SafePadding + 40 || islandY >= worldHeight - SafePadding - 40) continue;

                int topT = 8 + WorldGen.genRand.Next(6);
                int botD = 20 + WorldGen.genRand.Next(18);
                GenerateNaturalIsland(islandX, islandY, hw, topT, botD, seed + 9100 + i * 777);

                //哨站岛附近有少量碎片
                if (WorldGen.genRand.NextBool(3)) {
                    GenerateDebrisField(islandX, islandY, hw, botD, seed + 9200 + i * 111,
                        worldWidth, worldHeight);
                }
            }
        }

        /// <summary>
        /// 在岛屿附近生成碎片浮岛场
        /// 模拟亚空间侵蚀导致的碎裂漂浮物
        /// </summary>
        private static void GenerateDebrisField(int cx, int cy, int parentHW, int parentBotD,
            int noiseSeed, int worldWidth, int worldHeight) {
            int debrisCount = 3 + WorldGen.genRand.Next(5);

            for (int i = 0; i < debrisCount; i++) {
                //在父岛周围随机分布，但保持一定距离
                float angle = WorldGen.genRand.NextFloat() * MathHelper.TwoPi;
                float dist = parentHW + 20 + WorldGen.genRand.Next(30, 80);
                int dx = (int)(MathF.Cos(angle) * dist);
                int dy = (int)(MathF.Sin(angle) * dist * 0.7f);

                int fragX = cx + dx;
                int fragY = cy + dy;

                if (fragX < SafePadding + 20 || fragX >= worldWidth - SafePadding - 20) continue;
                if (fragY < SafePadding + 20 || fragY >= worldHeight - SafePadding - 20) continue;

                //微型浮岛
                int fragHW = 5 + WorldGen.genRand.Next(8);
                int fragTopT = 3 + WorldGen.genRand.Next(4);
                int fragBotD = 6 + WorldGen.genRand.Next(10);
                GenerateNaturalIsland(fragX, fragY, fragHW, fragTopT, fragBotD, noiseSeed + i * 333);
            }
        }

        /// <summary>
        /// 在整个世界范围内散布极小的碎片岛
        /// 填充虚空中的空旷感，营造亚空间中物质碎裂漂浮的氛围
        /// </summary>
        private static void GenerateScatteredFragments(int cx, int cy, int worldWidth, int worldHeight, int seed) {
            FastNoiseLite scatterNoise = new(seed + 11000);
            scatterNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            scatterNoise.SetFrequency(0.003f);

            int fragmentCount = 25 + WorldGen.genRand.Next(15);

            for (int i = 0; i < fragmentCount; i++) {
                //在世界范围内随机位置
                int fx = SafePadding + 100 + WorldGen.genRand.Next(worldWidth - SafePadding * 2 - 200);
                int fy = SafePadding + 100 + WorldGen.genRand.Next(worldHeight - SafePadding * 2 - 200);

                //避开核心岛附近（那里已经够密了）
                float distToCenter = MathF.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
                if (distToCenter < 200) continue;

                //噪声控制密度分布
                float n = scatterNoise.GetNoise(fx, fy);
                if (n < -0.1f) continue;

                int fragHW = 3 + WorldGen.genRand.Next(6);
                int fragTopT = 2 + WorldGen.genRand.Next(3);
                int fragBotD = 4 + WorldGen.genRand.Next(8);
                GenerateNaturalIsland(fx, fy, fragHW, fragTopT, fragBotD, seed + 11100 + i * 131);
            }
        }

        #endregion
    }
}
