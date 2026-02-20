using DimensionalRelease.Common;
using System;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines
{
    internal class MachineGen : GenPass
    {
        //安全边距，防止PlaceTile和KillTile访问相邻格子时越界
        private const int SafePadding = 10;

        public MachineGen() : base("Machine World", 110) { }

        //检查坐标是否在世界安全范围内，预留边距防止越界
        private static bool InWorldSafe(int x, int y) {
            return x >= SafePadding && x < Main.maxTilesX - SafePadding
                && y >= SafePadding && y < Main.maxTilesY - SafePadding;
        }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration) {
            int typeDark = ModContent.TileType<MechanicalDark>();
            int typeStone = ModContent.TileType<MechanicalStone>();
            int[] mechTiles = [typeDark, typeStone, TileID.AdamantiteBeam, TileID.Cog, TileID.LihzahrdBrick];

            GenerationProgress cache = WorldGenerator.CurrentGenerationProgress;

            //先生成一个正常的原版世界作为基底，然后在上面进行机械风格替换
            try {
                WorldGen.GenerateWorld(Main.ActiveWorldFileData.Seed);
            }
            catch {
                //原版生成中某些非关键步骤可能抛出异常，忽略后继续机械化改造
            }

            int worldWidth = Main.maxTilesX;
            int worldHeight = Main.maxTilesY;
            int surfaceY = Math.Clamp((int)Main.worldSurface, SafePadding, worldHeight - SafePadding);

            //替换地表和地下已有物块为机械风格
            for (int x = SafePadding; x < worldWidth - SafePadding; x++) {
                for (int y = SafePadding; y < worldHeight - SafePadding; y++) {
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile || tile.LiquidType != 0)
                        continue;

                    //80%概率替换为主要机械物块，20%概率随机选择其他机械物块
                    int newTileType = WorldGen.genRand.NextFloat() < 0.8f
                        ? typeDark
                        : mechTiles[WorldGen.genRand.Next(mechTiles.Length)];
                    tile.TileType = (ushort)newTileType;
                }
            }

            //生成高耸尖峰地形
            int minSpikeHeight = 80;
            int maxSpikeHeight = 130;
            int spikeBaseWidth = 20;
            float noiseScale = 0.06f;
            FastNoiseLite noise = new FastNoiseLite(Main.ActiveWorldFileData.Seed);
            noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);

            int halfBase = spikeBaseWidth / 2;
            for (int x = SafePadding; x < worldWidth - SafePadding; x += halfBase) {
                //基础高度在地表线附近波动
                int baseHeight = surfaceY + (int)(noise.GetNoise(x * noiseScale, 0) * 20);
                baseHeight = Math.Clamp(baseHeight, SafePadding + maxSpikeHeight, worldHeight - SafePadding);
                int spikeHeight = WorldGen.genRand.Next(minSpikeHeight, maxSpikeHeight + 1);

                //生成尖峰，从底部到顶部宽度递减
                for (int sy = 0; sy < spikeHeight; sy++) {
                    float t = sy / (float)spikeHeight;
                    int curWidth = (int)MathHelper.Lerp(spikeBaseWidth, 1, t);
                    int halfW = curWidth / 2;
                    float baseRadius = halfW * halfW;

                    for (int dx = -halfW; dx <= halfW; dx++) {
                        float dist = dx * dx;
                        //在基础半径上施加随机扰动
                        float threshold = baseRadius * (1f + WorldGen.genRand.NextFloat(-0.3f, 0.3f));
                        if (dist > threshold)
                            continue;

                        int px = x + dx;
                        int py = baseHeight - sy;
                        if (InWorldSafe(px, py)) {
                            WorldGen.PlaceTile(px, py, typeStone, mute: true);
                        }
                    }
                }

                //尖峰之间填充平坦机械地面
                int groundY = baseHeight + WorldGen.genRand.Next(-5, 6);
                groundY = Math.Clamp(groundY, SafePadding, worldHeight - SafePadding - 1);
                for (int dx = -halfBase; dx < halfBase; dx++) {
                    int px = x + dx;
                    if (InWorldSafe(px, groundY)) {
                        WorldGen.PlaceTile(px, groundY, typeDark, mute: true);
                    }
                }
            }

            //添加机械装饰（管道、金属棒等）
            for (int x = SafePadding; x < worldWidth - SafePadding; x += 10) {
                int y = surfaceY + WorldGen.genRand.Next(-10, 10);
                int decoY = y - 1;
                if (!InWorldSafe(x, y) || !InWorldSafe(x, decoY))
                    continue;
                if (!Framing.GetTileSafely(x, y).HasTile)
                    continue;
                if (WorldGen.genRand.NextFloat() >= 0.3f)
                    continue;

                int decoType = WorldGen.genRand.Next(3) switch {
                    0 => TileID.MetalBars,
                    1 => TileID.IronBrick,
                    _ => TileID.PressurePlates
                };
                WorldGen.PlaceTile(x, decoY, decoType, mute: true);
            }

            //生成地下机械矿脉
            int num1 = 0;
            int verticalLimit = worldHeight / 6 * 5;
            int oreVeinWidth = 120;
            int oreVeinHeight = 100;
            int horizontalSpacing = 160;
            int verticalSpacing = 160;
            //矿脉中心距世界边缘至少留出半个矿脉宽高加上安全边距
            int marginX = oreVeinWidth / 2 + SafePadding;
            int marginY = oreVeinHeight / 2 + SafePadding;

            while (num1 < verticalLimit) {
                int layerY = surfaceY + 100 + num1;
                if (layerY >= worldHeight - marginY)
                    break;

                int numVeins = Math.Max(0, (worldWidth - marginX * 2) / horizontalSpacing);
                for (int i = 0; i < numVeins; i++) {
                    int baseX = marginX + i * horizontalSpacing;
                    int offsetX = WorldGen.genRand.Next(-20, 20);
                    int offsetY = WorldGen.genRand.Next(-30, 30);
                    int centerX = Math.Clamp(baseX + offsetX, marginX, worldWidth - marginX);
                    int centerY = Math.Clamp(layerY + offsetY, marginY, worldHeight - marginY);

                    GenerateMechanicalOreVein(centerX, centerY, width: oreVeinWidth, height: oreVeinHeight, corridorSpacing: 16);
                }

                //下一层推进，保证至少前进一定距离避免死循环
                num1 += verticalSpacing + WorldGen.genRand.Next(0, 60);
            }

            WorldGenerator.CurrentGenerationProgress = cache;
        }

        /// <summary>
        /// 生成一个具有"机械风格"的矿脉结构，模拟工业化矿区的视觉效果。
        /// 该矿脉为长条矩形区块，内部以走廊分隔形成多个矿室，
        /// 矿室填充机械风格的矿石方块，随机插入装饰元素增强科技感。
        /// </summary>
        /// <param name="centerX">矿脉中心X坐标</param>
        /// <param name="centerY">矿脉中心Y坐标</param>
        /// <param name="width">矿脉整体宽度</param>
        /// <param name="height">矿脉整体高度</param>
        /// <param name="corridorSpacing">走廊间距，决定矿室排列密度</param>
        internal static void GenerateMechanicalOreVein(int centerX, int centerY, int width = 60, int height = 30, int corridorSpacing = 6) {
            int oreType = TileID.IronBrick;
            int[] mechTiles = [TileID.AdamantiteBeam, TileID.Cog, TileID.LihzahrdBrick];
            int halfW = width / 2;
            int halfH = height / 2;

            //构造矩形矿脉区块，中心为机械矿石，边缘加入辅助机械物块
            for (int x = -halfW; x <= halfW; x++) {
                for (int y = -halfH; y <= halfH; y++) {
                    int px = centerX + x;
                    int py = centerY + y;
                    if (!InWorldSafe(px, py))
                        continue;

                    //边缘区域杂质概率更高
                    float distRatio = MathF.Abs(x) / (float)halfW + MathF.Abs(y) / (float)halfH;
                    float edgeChance = MathHelper.Clamp(distRatio - 0.8f, 0f, 1f);

                    int tileType = WorldGen.genRand.NextFloat() < 1f - edgeChance
                        ? oreType
                        : mechTiles[WorldGen.genRand.Next(mechTiles.Length)];
                    WorldGen.PlaceTile(px, py, tileType, mute: true, forced: true);
                }
            }

            //生成管道式空洞，横纵向走廊网络
            for (int x = -halfW; x <= halfW; x++) {
                if (x % corridorSpacing != 0)
                    continue;
                for (int y = -halfH; y <= halfH; y++) {
                    int px = centerX + x;
                    int py = centerY + y;
                    if (InWorldSafe(px, py)) {
                        WorldGen.KillTile(px, py, noItem: true);
                    }
                }
            }

            for (int y = -halfH; y <= halfH; y++) {
                if (y % corridorSpacing != 0)
                    continue;
                for (int x = -halfW; x <= halfW; x++) {
                    int px = centerX + x;
                    int py = centerY + y;
                    if (InWorldSafe(px, py)) {
                        WorldGen.KillTile(px, py, noItem: true);
                    }
                }
            }

            //四个角落添加接线井结构
            int nodeSize = 2;
            for (int dx = -1; dx <= 1; dx += 2) {
                for (int dy = -1; dy <= 1; dy += 2) {
                    int nx = centerX + dx * (halfW - 4);
                    int ny = centerY + dy * (halfH - 4);

                    for (int i = -nodeSize; i <= nodeSize; i++) {
                        for (int j = -nodeSize; j <= nodeSize; j++) {
                            int px = nx + i;
                            int py = ny + j;
                            if (InWorldSafe(px, py)) {
                                WorldGen.KillTile(px, py, noItem: true);
                            }
                        }
                    }
                }
            }
        }
    }
}
