using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures
{
    /// <summary>
    /// 虚空聚落建筑布局规划器
    /// 在世界生成末尾按岛屿布局自动选点放置建筑，并用桥段与管段串联核心与各卫星岛
    /// 只负责计算位置与写入<see cref="ArchitectureRegistry"/>，不直接生成Actor
    /// </summary>
    internal static class ArchitecturePlacer
    {
        //卫星岛标签到建筑类型的映射表
        private static readonly (string tag, ArchitectureType type)[] SatelliteBuildings = [
            ("能量控制站", ArchitectureType.EnergyControlStation),
            ("核心亚空间能量分析站", ArchitectureType.EnergyControlStation),
            ("超凡材料分析实验室", ArchitectureType.MidSizeMaterialAnalysisLab),
            ("亚空间异界生物实验室_上", ArchitectureType.MidSizeMaterialAnalysisLab),
            ("亚空间异界生物实验室_下", ArchitectureType.MidSizeMaterialAnalysisLab),
        ];

        //观测哨数量上限
        private const int ObservationPostCount = 4;
        //观测哨挑选时与其他观测哨的最小距离（格）
        private const int ObservationPostMinSpacing = 260;

        //桥段高度像素，用于垂直居中放置
        private const int BridgeHeightPx = 150;
        //管段相对于桥段的垂直偏移（管段位于桥段之上）
        private const int TunnelOffsetAboveBridge = 140;
        private const int TunnelHeightPx = 122;
        //每段桥和管道的宽度（px），略小于贴图宽以允许少量重叠避免接缝
        private const int BridgeSegmentSpacingPx = 540;
        private const int TunnelSegmentSpacingPx = 376;

        /// <summary>
        /// 执行完整的建筑布局规划
        /// 需要在<see cref="IslandRegistry.ScanAllSurfaces"/>之后调用，以便使用各岛屿的精确表面高度
        /// </summary>
        public static void BuildAll() {
            ArchitectureRegistry.Clear();

            var core = IslandRegistry.FindByTag("核心实验室");
            if (core == null) {
                return;
            }

            //核心实验室落在核心岛上表面正中
            PlaceBuildingOnIsland(core, ArchitectureType.CoreVoidLab);

            //按映射表放置所有卫星建筑，同时为每座卫星向核心方向搭桥+铺管
            foreach (var (tag, type) in SatelliteBuildings) {
                var sat = IslandRegistry.FindByTag(tag);
                if (sat == null) continue;
                PlaceBuildingOnIsland(sat, type);
                ConnectToCore(sat, core);
            }

            //外圈挑选数座观测哨
            PlaceObservationPosts(core);
        }

        /// <summary>
        /// 将建筑贴图以"底部中心对齐岛屿表面、水平居中对齐岛屿中心"的方式放置
        /// </summary>
        private static void PlaceBuildingOnIsland(IslandData island, ArchitectureType type) {
            var tex = ArchitectureAsset.Get(type);
            if (tex == null) return;

            int surfaceTileY = island.SurfaceY > 0 ? island.SurfaceY : island.CenterY;
            int pixelX = island.CenterX * 16 - tex.Width / 2;
            int pixelY = surfaceTileY * 16 - tex.Height;

            ArchitectureRegistry.Add(type, pixelX, pixelY);
        }

        /// <summary>
        /// 从卫星岛向核心岛搭建一条水平的"桥段+管段"并行廊道
        /// 桥段位于卫星表面高度，管段沿桥段之上平行铺设
        /// </summary>
        private static void ConnectToCore(IslandData satellite, IslandData core) {
            var bridgeTex = ArchitectureAsset.Get(ArchitectureType.ConnectionBridgeSegment);
            var tunnelTex = ArchitectureAsset.Get(ArchitectureType.TubularConnectorTunnel);
            if (bridgeTex == null || tunnelTex == null) return;

            bool satRightOfCore = satellite.CenterX >= core.CenterX;
            //起点为卫星朝向核心那一侧的边缘
            int startTileX = satRightOfCore
                ? satellite.CenterX - satellite.HalfWidth
                : satellite.CenterX + satellite.HalfWidth;
            //终点为核心朝向卫星那一侧的边缘
            int endTileX = satRightOfCore
                ? core.CenterX + core.HalfWidth
                : core.CenterX - core.HalfWidth;

            int spanPx = Math.Abs(endTileX - startTileX) * 16;
            if (spanPx < BridgeSegmentSpacingPx) return;

            //桥段Y：让桥段中心对齐卫星表面的略上方，让玩家视觉上看到桥挂在岛边
            int bridgeCenterPxY = satellite.SurfaceY * 16 - 8;
            int bridgePxY = bridgeCenterPxY - BridgeHeightPx / 2;

            int bridgeCount = Math.Max(1, spanPx / BridgeSegmentSpacingPx);
            int direction = satRightOfCore ? -1 : 1;
            //桥段起始X，让起始段的外侧刚好贴在卫星边缘
            int bridgeStartPxX = startTileX * 16;
            if (direction < 0) bridgeStartPxX -= bridgeTex.Width;

            for (int i = 0; i < bridgeCount; i++) {
                int px = bridgeStartPxX + direction * i * BridgeSegmentSpacingPx;
                ArchitectureRegistry.Add(ArchitectureType.ConnectionBridgeSegment, px, bridgePxY);
            }

            //管段：位于桥段上方，独立计算数量，段间距略小
            int tunnelPxY = bridgePxY - TunnelOffsetAboveBridge - TunnelHeightPx;
            int tunnelCount = Math.Max(1, spanPx / TunnelSegmentSpacingPx);
            int tunnelStartPxX = startTileX * 16;
            if (direction < 0) tunnelStartPxX -= tunnelTex.Width;

            for (int i = 0; i < tunnelCount; i++) {
                int px = tunnelStartPxX + direction * i * TunnelSegmentSpacingPx;
                ArchitectureRegistry.Add(ArchitectureType.TubularConnectorTunnel, px, tunnelPxY);
            }
        }

        /// <summary>
        /// 在距离核心较远的哨站岛上放置观测哨
        /// 每座观测哨之间保持一定间距，避免挤成一团
        /// </summary>
        private static void PlaceObservationPosts(IslandData core) {
            var outposts = IslandRegistry.GetByTier(IslandTier.Outpost);
            if (outposts.Count == 0) return;

            //按与核心距离降序排序，优先选择外圈
            outposts.Sort((a, b) => core.DistanceTo(b).CompareTo(core.DistanceTo(a)));

            List<IslandData> chosen = [];
            foreach (var candidate in outposts) {
                if (candidate.HalfWidth < 12) continue;
                bool tooClose = false;
                foreach (var picked in chosen) {
                    float dx = candidate.CenterX - picked.CenterX;
                    float dy = candidate.CenterY - picked.CenterY;
                    if (dx * dx + dy * dy < ObservationPostMinSpacing * ObservationPostMinSpacing) {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;
                chosen.Add(candidate);
                if (chosen.Count >= ObservationPostCount) break;
            }

            foreach (var island in chosen) {
                PlaceBuildingOnIsland(island, ArchitectureType.ObservationPostTelescope);
            }
        }
    }
}
