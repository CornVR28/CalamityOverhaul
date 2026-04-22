using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.GatlinTurrets;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.LaserCannons;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures
{
    /// <summary>
    /// 已放置建筑的运行时记录，坐标单位为像素
    /// </summary>
    internal class PlacedBuilding
    {
        public ArchitectureType Type;
        public int PixelX;
        public int PixelY;
        public int WidthPx;
        public int HeightPx;

        public Vector2 GetPortWorld(int portIndex) {
            var port = ArchitecturePorts.Get(Type)[portIndex];
            return ArchitecturePorts.ToWorldPixel(port, PixelX, PixelY);
        }

        public ArchitecturePort GetPort(int portIndex) => ArchitecturePorts.Get(Type)[portIndex];
    }

    /// <summary>
    /// 虚空聚落建筑布局规划器
    /// 以核心岛为中心排布一条水平的建筑群：观测哨-能源站-核心实验室-分析实验室-观测哨
    /// 所有连接为纯水平桥梁，端点Y通过端口对齐计算保证直线贴合
    /// 卫星岛与外圈哨站岛各自独立放置一栋建筑，不做跨岛连接，避免需要拐角的错配
    /// 只负责计算位置与写入<see cref="ArchitectureRegistry"/>，不直接生成Actor
    /// </summary>
    internal static class ArchitecturePlacer
    {
        private const int PixelsPerTile = 16;
        //相邻建筑之间的水平间距px
        //桥段贴图当前宽276px（已缩至原始的一半），设为2800约等于十段拼接，拉开核心与附属建筑的跨距
        private const int InterBuildingGapPx = 2800;

        //卫星岛标签→建筑类型
        private static readonly Dictionary<string, ArchitectureType> SatelliteBuildings = new() {
            ["能量控制站"] = ArchitectureType.EnergyControlStation,
            ["核心亚空间能量分析站"] = ArchitectureType.EnergyControlStation,
            ["超凡材料分析实验室"] = ArchitectureType.MidSizeMaterialAnalysisLab,
            ["亚空间异界生物实验室_上"] = ArchitectureType.MidSizeMaterialAnalysisLab,
            ["亚空间异界生物实验室_下"] = ArchitectureType.MidSizeMaterialAnalysisLab,
        };

        public static void BuildAll() {
            ArchitectureRegistry.Clear();
            GatlinTurretRegistry.Clear();
            LaserCannonRegistry.Clear();

            var core = IslandRegistry.FindByTag("核心实验室");
            if (core == null) return;

            BuildCoreCluster(core);
            BuildSatelliteStandalones();
            BuildOutpostStandalones(core);
            //外围基地：零号站点等独立基地由Stronghold子系统负责，与岛屿无关
            Strongholds.StrongholdPlacer.BuildAll();
        }

        #region 核心岛簇：直线桥梁串联

        /// <summary>
        /// 在核心岛上沿中心横轴一字排开五栋建筑，按端口对齐Y逐对牵直线桥梁
        /// 核心建筑落在岛面，其余建筑依靠端口Y强制对齐而浮空，由桁架平台补地
        /// </summary>
        private static void BuildCoreCluster(IslandData core) {
            var coreTex = ArchitectureAsset.Get(ArchitectureType.CoreVoidLab);
            if (coreTex == null) return;

            //核心建筑以底边对齐岛面中心柱的方式落点
            int surfacePx = core.SurfaceY * PixelsPerTile;
            int coreCenterPx = core.CenterX * PixelsPerTile;
            int corePixelX = coreCenterPx - coreTex.Width / 2;
            int corePixelY = surfacePx - coreTex.Height;
            var coreBuilding = RegisterPlaced(ArchitectureType.CoreVoidLab, corePixelX, corePixelY, coreTex);

            //核心的左右桥梁口（使用上层，LocalY=580）
            int coreLeftBridgeIdx = FindPortIndex(coreBuilding, PortKind.Bridge, PortSide.Left);
            int coreRightBridgeIdx = FindPortIndex(coreBuilding, PortKind.Bridge, PortSide.Right);

            //向左依次挂接：能源站→观测哨
            if (coreLeftBridgeIdx >= 0) {
                var energy = AppendFlankTowardsLeft(coreBuilding, coreLeftBridgeIdx,
                    ArchitectureType.EnergyControlStation, surfacePx);
                if (energy != null) {
                    //炮台立在桥远离核心的一端（能源站朝核心那侧的端口前方一小段）
                    //玩家从核心过来要先走完长桥才会进入触发圈，避免刚出生就被扫射
                    int energyInnerPortIdx = FindPortIndex(energy, PortKind.Bridge, PortSide.Right);
                    PlaceGatlinOnBridgeFarEnd(energy, energyInnerPortIdx, side: -1);
                    int energyOuterLeft = FindPortIndex(energy, PortKind.Bridge, PortSide.Left);
                    if (energyOuterLeft >= 0) {
                        var leftPost = AppendFlankTowardsLeft(energy, energyOuterLeft,
                            ArchitectureType.ObservationPostTelescope, surfacePx);
                        if (leftPost != null) {
                            //激光炮台悬浮在最外端观测哨更远处的高空，朝核心方向瞄准
                            PlaceLaserCannonBeyond(leftPost, side: -1);
                        }
                    }
                }
            }

            //向右依次挂接：分析实验室→观测哨
            if (coreRightBridgeIdx >= 0) {
                var midlab = AppendFlankTowardsRight(coreBuilding, coreRightBridgeIdx,
                    ArchitectureType.MidSizeMaterialAnalysisLab, surfacePx);
                if (midlab != null) {
                    int midInnerPortIdx = FindPortIndex(midlab, PortKind.Bridge, PortSide.Left);
                    PlaceGatlinOnBridgeFarEnd(midlab, midInnerPortIdx, side: +1);
                    int midOuterRight = FindPortIndex(midlab, PortKind.Bridge, PortSide.Right);
                    if (midOuterRight >= 0) {
                        var rightPost = AppendFlankTowardsRight(midlab, midOuterRight,
                            ArchitectureType.ObservationPostTelescope, surfacePx);
                        if (rightPost != null) {
                            PlaceLaserCannonBeyond(rightPost, side: +1);
                        }
                    }
                }
            }
        }

        //激光炮底座像素尺寸，与LaserCannonPedestal.png一致
        private const int LaserCannonPedestalWidthPx = 870;
        private const int LaserCannonPedestalHeightPx = 472;
        //激光炮中心距邻居建筑外侧端口的水平距离，留出远景悬浮感
        private const int LaserCannonOutwardPx = 900;
        //激光炮悬浮在观测哨上方的垂直抬升距离，营造俯冲压迫视角
        private const int LaserCannonFloatLiftPx = 720;

        /// <summary>
        /// 在最外端观测哨的外侧悬空处放置巨型激光炮
        /// side=-1代表左侧，炮朝右瞄准核心；side=+1代表右侧，炮朝左瞄准核心
        /// </summary>
        private static void PlaceLaserCannonBeyond(PlacedBuilding outerPost, int side) {
            //观测哨外侧端口：左侧观测哨的外端是Left，右侧观测哨的外端是Right
            PortSide outerSide = side < 0 ? PortSide.Left : PortSide.Right;
            int outerIdx = FindPortIndex(outerPost, PortKind.Bridge, outerSide);
            if (outerIdx < 0) return;

            Vector2 outerPortWorld = outerPost.GetPortWorld(outerIdx);
            int pedestalCenterX = (int)outerPortWorld.X + side * LaserCannonOutwardPx;
            int pedestalTopY = (int)outerPortWorld.Y - LaserCannonPedestalHeightPx - LaserCannonFloatLiftPx;
            int pedestalLeftX = pedestalCenterX - LaserCannonPedestalWidthPx / 2;
            //朝核心方向：左侧激光炮朝右(faceLeft=false)，右侧激光炮朝左(faceLeft=true)
            bool faceLeft = side > 0;
            LaserCannonRegistry.Add(pedestalLeftX, pedestalTopY, faceLeft);
        }

        //加特林炮台底座贴图尺寸，与源素材GatlinPedestal.png一致
        private const int GatlinPedestalWidthPx = 354;
        private const int GatlinPedestalHeightPx = 166;
        //底座中心离邻居端口的水平距离，向桥中间内缩避免贴着邻居建筑墙体
        private const int GatlinBridgeInwardPx = 260;

        /// <summary>
        /// 在桥远离核心的一端（邻居建筑一侧）放置炮台
        /// side=-1表示邻居位于核心左方，炮台静止朝右（迎接从核心方向过来的玩家）
        /// side=+1表示邻居位于核心右方，炮台静止朝左
        /// </summary>
        private static void PlaceGatlinOnBridgeFarEnd(PlacedBuilding neighbor, int neighborInnerPortIdx, int side) {
            if (neighborInnerPortIdx < 0) return;
            Vector2 portWorld = neighbor.GetPortWorld(neighborInnerPortIdx);
            //从邻居的内侧端口朝核心方向内缩一段，使炮台清晰落在桥面而不是嵌进邻居墙体
            int inward = -side * GatlinBridgeInwardPx;
            int pedestalCenterX = (int)portWorld.X + inward;
            //桥梁的可行走表面位于端口Y向下1格，与ArchitectureConnectorActor里的tileY=StartY/16+1保持一致
            int bridgeDeckY = (((int)portWorld.Y / PixelsPerTile) + 1) * PixelsPerTile;
            int pedestalLeftX = pedestalCenterX - GatlinPedestalWidthPx / 2;
            int pedestalTopY = bridgeDeckY - GatlinPedestalHeightPx;
            //朝向核心方向即玩家来向：side=-1(邻居在左，核心在右)朝右；side=+1反之
            bool faceLeft = side > 0;
            GatlinTurretRegistry.Add(pedestalLeftX, pedestalTopY, faceLeft);
        }

        /// <summary>
        /// 在给定基准建筑的左侧追加一栋邻居，端口Y对齐后生成直线桥梁并补桁架地基
        /// </summary>
        private static PlacedBuilding AppendFlankTowardsLeft(PlacedBuilding anchor, int anchorPortIdx,
            ArchitectureType type, int surfacePx) {
            var tex = ArchitectureAsset.Get(type);
            if (tex == null) return null;

            //邻居使用其Right桥梁口与基准的Left桥梁口对齐
            var ports = ArchitecturePorts.Get(type);
            int neighborPortIdx = FindPortIndexInTable(ports, PortKind.Bridge, PortSide.Right);
            if (neighborPortIdx < 0) return null;

            Vector2 anchorPortWorld = anchor.GetPortWorld(anchorPortIdx);
            var neighborPort = ports[neighborPortIdx];
            int pixelY = (int)anchorPortWorld.Y - neighborPort.LocalY;
            //邻居右端口X = 基准左端口X - 间距，倒推出pixelX
            int pixelX = (int)anchorPortWorld.X - InterBuildingGapPx - neighborPort.LocalX;

            var placed = RegisterPlaced(type, pixelX, pixelY, tex);
            FillTrussUnderFootprint(placed, surfacePx);

            //桥梁：Y使用对齐后的端口Y
            int bridgeY = (int)anchorPortWorld.Y;
            int bridgeLeftX = pixelX + neighborPort.LocalX;
            int bridgeRightX = (int)anchorPortWorld.X;
            ArchitectureRegistry.AddConnector(PortKind.Bridge, bridgeLeftX, bridgeY, bridgeRightX);
            return placed;
        }

        /// <summary>
        /// 在给定基准建筑的右侧追加一栋邻居，端口Y对齐后生成直线桥梁并补桁架地基
        /// </summary>
        private static PlacedBuilding AppendFlankTowardsRight(PlacedBuilding anchor, int anchorPortIdx,
            ArchitectureType type, int surfacePx) {
            var tex = ArchitectureAsset.Get(type);
            if (tex == null) return null;

            var ports = ArchitecturePorts.Get(type);
            int neighborPortIdx = FindPortIndexInTable(ports, PortKind.Bridge, PortSide.Left);
            if (neighborPortIdx < 0) return null;

            Vector2 anchorPortWorld = anchor.GetPortWorld(anchorPortIdx);
            var neighborPort = ports[neighborPortIdx];
            int pixelY = (int)anchorPortWorld.Y - neighborPort.LocalY;
            int pixelX = (int)anchorPortWorld.X + InterBuildingGapPx - neighborPort.LocalX;

            var placed = RegisterPlaced(type, pixelX, pixelY, tex);
            FillTrussUnderFootprint(placed, surfacePx);

            int bridgeY = (int)anchorPortWorld.Y;
            int bridgeLeftX = (int)anchorPortWorld.X;
            int bridgeRightX = pixelX + neighborPort.LocalX;
            ArchitectureRegistry.AddConnector(PortKind.Bridge, bridgeLeftX, bridgeY, bridgeRightX);
            return placed;
        }

        #endregion

        #region 卫星岛 / 哨站岛独立建筑

        private static void BuildSatelliteStandalones() {
            foreach (var island in IslandRegistry.Islands) {
                if (island.Tier != IslandTier.Satellite) continue;
                if (!SatelliteBuildings.TryGetValue(island.Tag ?? "", out var type)) continue;
                PlaceOnIslandSurface(island, type);
            }
        }

        /// <summary>
        /// 哨站岛随机点缀一些观测哨，作为远景装饰
        /// </summary>
        private static void BuildOutpostStandalones(IslandData core) {
            var outposts = IslandRegistry.GetByTier(IslandTier.Outpost);
            if (outposts.Count == 0) return;
            //按距核心岛由远到近选取前若干，突出边缘存在感
            outposts.Sort((a, b) => core.DistanceTo(b).CompareTo(core.DistanceTo(a)));
            const int MaxOutpostBuildings = 3;
            int placed = 0;
            foreach (var island in outposts) {
                if (placed >= MaxOutpostBuildings) break;
                if (island.HalfWidth < 12) continue;
                if (PlaceOnIslandSurface(island, ArchitectureType.ObservationPostTelescope) != null) {
                    placed++;
                }
            }
        }

        /// <summary>
        /// 以贴图底部中心对齐岛屿表面Y的方式放置建筑
        /// </summary>
        private static PlacedBuilding PlaceOnIslandSurface(IslandData island, ArchitectureType type) {
            var tex = ArchitectureAsset.Get(type);
            if (tex == null) return null;
            int surfaceTileY = island.SurfaceY > 0 ? island.SurfaceY : island.CenterY;
            int pixelX = island.CenterX * PixelsPerTile - tex.Width / 2;
            int pixelY = surfaceTileY * PixelsPerTile - tex.Height;
            return RegisterPlaced(type, pixelX, pixelY, tex);
        }

        #endregion

        #region 桁架地基与注册

        /// <summary>
        /// 在建筑浮空时，沿其底部到岛面之间填充一层一层VoidPlating/VoidFramework作支撑
        /// 只在当前格为空时落砖，避免破坏已有地面或岛屿外缘的自然剖面
        /// </summary>
        private static void FillTrussUnderFootprint(PlacedBuilding building, int surfacePx) {
            int bottomPx = building.PixelY + building.HeightPx;
            if (bottomPx >= surfacePx) return;

            int leftTile = building.PixelX / PixelsPerTile;
            int rightTile = (building.PixelX + building.WidthPx - 1) / PixelsPerTile;
            int topTile = bottomPx / PixelsPerTile;
            int bottomTile = surfacePx / PixelsPerTile;

            int plating = ModContent.TileType<VoidPlating>();
            int framework = ModContent.TileType<VoidFramework>();

            for (int x = leftTile; x <= rightTile; x++) {
                if (x < 0 || x >= Main.maxTilesX) continue;
                for (int y = topTile; y < bottomTile; y++) {
                    if (y < 0 || y >= Main.maxTilesY) continue;
                    if (Main.tile[x, y].HasTile) continue;
                    //外边沿走桁架，内部走装甲板
                    bool isEdge = x == leftTile || x == rightTile || y == topTile;
                    int tileType = isEdge ? framework : plating;
                    WorldGen.PlaceTile(x, y, tileType, mute: true, forced: true);
                }
            }
        }

        private static PlacedBuilding RegisterPlaced(ArchitectureType type,
            int pixelX, int pixelY, Microsoft.Xna.Framework.Graphics.Texture2D tex) {
            ArchitectureRegistry.Add(type, pixelX, pixelY);
            var placed = new PlacedBuilding {
                Type = type,
                PixelX = pixelX,
                PixelY = pixelY,
                WidthPx = tex.Width,
                HeightPx = tex.Height,
            };
            return placed;
        }

        #endregion

        #region 端口查询

        private static int FindPortIndex(PlacedBuilding b, PortKind kind, PortSide side)
            => FindPortIndexInTable(ArchitecturePorts.Get(b.Type), kind, side);

        private static int FindPortIndexInTable(ArchitecturePort[] ports, PortKind kind, PortSide side) {
            for (int i = 0; i < ports.Length; i++) {
                if (ports[i].Kind == kind && ports[i].Side == side) return i;
            }
            return -1;
        }

        #endregion
    }
}
