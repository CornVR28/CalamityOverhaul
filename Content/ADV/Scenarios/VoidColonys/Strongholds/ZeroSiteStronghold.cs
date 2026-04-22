using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.SignalTowers;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Strongholds
{
    /// <summary>
    /// 零号站点：双层桥基前哨，基地核心建筑为信号塔
    /// 上层由信号塔+左右实验室构成主要作战平台
    /// 下层比上层更宽、更密集的炮台群，通过两侧铁锈阶梯与上层形成Z型闭合结构
    /// </summary>
    internal class ZeroSiteStronghold : Stronghold
    {
        public override string Name => "ZeroSite";

        //单侧上层桥跨度px：信号塔到旁边实验室的水平距�?
        private const int SideBridgeSpanPx = 2000;
        //上层桥段中均匀分布的加特林数量（单侧），精简到稀疏警戒密度
        private const int GatlinPerBridge = 1;
        //上层外侧实验室外延再补一段悬空炮台的水平间距
        private const int OuterFlankGatlinOffsetPx = 600;

        //铁锈阶梯贴图尺寸与端�?
        private const int StairWidthPx = 362;
        private const int StairHighLocalY = 42;
        private const int StairLowLocalY = 188;
        //双层垂直落差px，等于一段铁锈阶梯端口的Y差（188-42=146），保持阶梯自然连接上下层桥面
        private const int DeckGapPx = StairLowLocalY - StairHighLocalY;
        //下层桥在阶梯低端外再延伸的长度，给两端留足炮台阵列空间，同时保持整体轮廓不至于过于狭长
        private const int LowerDeckExtraSpanPx = 1800;
        //下层桥加特林数量，保持比上层略多但整体稀疏
        private const int GatlinPerLowerBridge = 3;

        //MidLab端口常量
        private const int MidLabLeftLocalX = 0;
        private const int MidLabRightLocalX = 382;
        private const int MidLabPortLocalY = 174;

        public override bool TryPickAnchor(out int anchorTileX, out int anchorTileY) {
            //贴近世界右边缘，预留足够空间避免被截断
            int marginTilesX = 600;
            int worldHalfHeight = Main.maxTilesY / 2;
            anchorTileX = Main.maxTilesX - marginTilesX;
            //放在世界垂直中线略偏上，远离碎片岛密集带
            anchorTileY = worldHalfHeight - 80;
            //世界过小时退回
            if (anchorTileX <= Main.maxTilesX / 2 + 200) {
                anchorTileY = 0;
                return false;
            }
            return true;
        }

        public override void Build(int anchorPixelX, int anchorPixelY) {
            //上层桥面世界Y
            int upperBridgeY = anchorPixelY;

            //信号塔：底部桥口对齐上层桥面，X居中于锚点
            int signalTowerLeftX = anchorPixelX - SignalTower.WidthPx / 2;
            int signalTowerPixelY = upperBridgeY - SignalTower.BridgePortLocalY;
            ArchitectureRegistry.Add(ArchitectureType.SignalTower, signalTowerLeftX, signalTowerPixelY);

            int towerLeftPortX = signalTowerLeftX;
            int towerRightPortX = signalTowerLeftX + SignalTower.WidthPx;

            //左右实验室，右端口和左端口分别贴向信号塔方向
            int leftLabRightPortX = towerLeftPortX - SideBridgeSpanPx;
            var leftLab = RegisterAlignedToBridgeY(ArchitectureType.MidSizeMaterialAnalysisLab,
                leftLabRightPortX, upperBridgeY, MidLabRightLocalX, MidLabPortLocalY);

            int rightLabLeftPortX = towerRightPortX + SideBridgeSpanPx;
            var rightLab = RegisterAlignedToBridgeY(ArchitectureType.MidSizeMaterialAnalysisLab,
                rightLabLeftPortX, upperBridgeY, MidLabLeftLocalX, MidLabPortLocalY);

            //上层主桥：左实验室右口↔塔左口、塔右口↔右实验室左口
            RegisterBridge(leftLabRightPortX, towerLeftPortX, upperBridgeY);
            RegisterBridge(towerRightPortX, rightLabLeftPortX, upperBridgeY);
            //塔基下再补一段桥面贯穿塔宽，避免信号塔底部悬空
            RegisterBridge(towerLeftPortX, towerRightPortX, upperBridgeY);

            ScatterGatlinOnBridge(leftLabRightPortX, towerLeftPortX, upperBridgeY, GatlinPerBridge, faceLeft: true);
            ScatterGatlinOnBridge(towerRightPortX, rightLabLeftPortX, upperBridgeY, GatlinPerBridge, faceLeft: false);

            //上层外侧仍保留小段延伸桥面以维持轮廓，但不再放炮台
            int leftOuterBridgeLeftX = (leftLab != null ? leftLab.PixelX : leftLabRightPortX) - OuterFlankGatlinOffsetPx - GatlinPedestalWidthPx;
            RegisterBridge(leftOuterBridgeLeftX, leftLab != null ? leftLab.PixelX : leftLabRightPortX, upperBridgeY);

            int rightLabRightEdgeX = rightLab != null ? rightLab.PixelX + rightLab.WidthPx : rightLabLeftPortX;
            int rightOuterBridgeRightX = rightLabRightEdgeX + OuterFlankGatlinOffsetPx + GatlinPedestalWidthPx;
            RegisterBridge(rightLabRightEdgeX, rightOuterBridgeRightX, upperBridgeY);

            //下层布局
            BuildLowerDeck(leftLab, rightLab, leftLabRightPortX, rightLabLeftPortX, upperBridgeY);
        }

        /// <summary>
        /// 下层走廊与连接阶梯
        /// 左侧阶梯原朝向(低左高右)：高端挂到上层左实验室左端口，低端落到下层左端口
        /// 右侧阶梯镜像(高左低右)：高端挂到上层右实验室右端口，低端落到下层右端口
        /// 两段阶梯之间的下层桥面横贯基地底下，并向两翼再延长一段供密集火力部署
        /// </summary>
        private static void BuildLowerDeck(PlacedArchitecture leftLab, PlacedArchitecture rightLab,
            int leftLabRightPortX, int rightLabLeftPortX, int upperBridgeY) {
            int lowerBridgeY = upperBridgeY + DeckGapPx;

            //上层实验室的外侧端口X，阶梯以此为高端锚点
            int leftLabLeftPortX = (leftLab != null ? leftLab.PixelX : leftLabRightPortX - MidLabRightLocalX) + MidLabLeftLocalX;
            int rightLabRightPortX = (rightLab != null ? rightLab.PixelX : rightLabLeftPortX) + MidLabRightLocalX;

            //左阶梯未翻转：原RightPort(362,42)是高端，对齐到左实验室左端口
            RegisterAlignedToBridgeY(ArchitectureType.ReinforcedRustedPathway,
                leftLabLeftPortX, upperBridgeY, StairWidthPx, StairHighLocalY, flipX: false);
            //阶梯低端世界X = pixelX + LocalX(低端)=0，即左实验室左端口X - 宽度
            int leftStairLowWorldX = leftLabLeftPortX - StairWidthPx;

            //右阶梯翻转：翻转后高端位于视觉左上角(local 0,42)，对齐到右实验室右端口
            RegisterAlignedToBridgeY(ArchitectureType.ReinforcedRustedPathway,
                rightLabRightPortX, upperBridgeY, 0, StairHighLocalY, flipX: true);
            //翻转后低端位于视觉右下角(local 362,188)，世界X = pixelX + 362 = 右实验室右端口X + 362
            int rightStairLowWorldX = rightLabRightPortX + StairWidthPx;

            //下层桥面三段：左翼延伸、贯穿主体、右翼延伸；桥面Y与阶梯低端对齐，X根据阶梯低端再外扩一定跨度
            int lowerLeftX = leftStairLowWorldX - LowerDeckExtraSpanPx;
            int lowerRightX = rightStairLowWorldX + LowerDeckExtraSpanPx;
            RegisterBridge(lowerLeftX, leftStairLowWorldX, lowerBridgeY);
            RegisterBridge(leftStairLowWorldX, rightStairLowWorldX, lowerBridgeY);
            RegisterBridge(rightStairLowWorldX, lowerRightX, lowerBridgeY);

            //下层加特林呈外散分布，中线两侧分别朝外；信号塔正下方留空避免遮挡塔身
            int towerCenterX = (leftLabRightPortX + rightLabLeftPortX) / 2;
            ScatterGatlinBidirectional(lowerLeftX, lowerRightX, lowerBridgeY, GatlinPerLowerBridge,
                skipCenterX: towerCenterX, skipRadiusPx: SignalTower.WidthPx / 2 + GatlinPedestalWidthPx);
        }

        /// <summary>
        /// 桥段上等距分布若干炮台，全部统一朝向
        /// </summary>
        private static void ScatterGatlinOnBridge(int leftX, int rightX, int bridgePortY, int count, bool faceLeft) {
            int span = rightX - leftX;
            if (span <= GatlinPedestalWidthPx) return;
            for (int i = 1; i <= count; i++) {
                float t = i / (float)(count + 1);
                int centerX = leftX + (int)(span * t);
                RegisterGatlinOnBridge(centerX, bridgePortY, faceLeft);
            }
        }

        /// <summary>
        /// 在桥段上等距铺放炮台，以中点为界分别朝左右外散布，形成双向警戒；当桥段足够长时在中点留空避免遮挡信号塔
        /// </summary>
        private static void ScatterGatlinBidirectional(int leftX, int rightX, int bridgePortY, int count,
            int skipCenterX = int.MinValue, int skipRadiusPx = 0) {
            int span = rightX - leftX;
            if (span <= GatlinPedestalWidthPx) return;
            int midX = (leftX + rightX) / 2;
            for (int i = 1; i <= count; i++) {
                float t = i / (float)(count + 1);
                int centerX = leftX + (int)(span * t);
                //位于信号塔投影死区内时跳过，避免正下方炮台遮挡塔身
                if (skipRadiusPx > 0 && System.Math.Abs(centerX - skipCenterX) < skipRadiusPx) {
                    continue;
                }
                bool faceLeft = centerX < midX;
                RegisterGatlinOnBridge(centerX, bridgePortY, faceLeft);
            }
        }
    }
}