using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.SignalTowers;
using Microsoft.Xna.Framework;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Strongholds
{
    /// <summary>
    /// 零号站点：放置在地图外围的小型前哨基地
    /// 桥梁作为地板贯穿东西，两侧各架一座中型物料分析实验室，正中竖立一座信号塔
    /// 桥面与四周点缀大量加特林炮台，模拟严密的外围防线
    /// </summary>
    internal class ZeroSiteStronghold : Stronghold
    {
        public override string Name => "ZeroSite";

        //单侧桥跨度px：信号塔到旁边实验室的水平距离
        //桥段贴图276px，约对应7~8段拼接，给炮台留出足够间隔
        private const int SideBridgeSpanPx = 2000;

        //桥段中均匀分布的加特林数量（单侧）
        private const int GatlinPerBridge = 4;
        //外侧实验室外延再补一段悬空炮台的水平间距
        private const int OuterFlankGatlinOffsetPx = 600;

        public override bool TryPickAnchor(out int anchorTileX, out int anchorTileY) {
            //贴近世界右边缘，预留足够空间避免被截断
            int marginTilesX = 600;
            int worldHalfHeight = Main.maxTilesY / 2;
            anchorTileX = Main.maxTilesX - marginTilesX;
            //放在世界垂直中线略偏上，远离碎片岛密集带
            anchorTileY = worldHalfHeight - 80;
            //世界过小时退出
            if (anchorTileX <= Main.maxTilesX / 2 + 200) {
                anchorTileY = 0;
                return false;
            }
            return true;
        }

        public override void Build(int anchorPixelX, int anchorPixelY) {
            //桥面端口Y就是基地的统一桥面世界Y
            int bridgePortY = anchorPixelY;

            //信号塔位置：让其底部桥梁端口对齐到桥面Y、X居中于锚点
            int signalTowerLeftX = anchorPixelX - SignalTower.WidthPx / 2;
            int signalTowerPixelY = bridgePortY - SignalTower.BridgePortLocalY;
            ArchitectureRegistry.Add(ArchitectureType.SignalTower, signalTowerLeftX, signalTowerPixelY);

            //信号塔两端桥口世界X
            int towerLeftPortX = signalTowerLeftX;
            int towerRightPortX = signalTowerLeftX + SignalTower.WidthPx;

            //左侧实验室：以其Right端口对齐到(towerLeftPortX - SideBridgeSpanPx, bridgePortY)
            //MidLab的右端口LocalX=382, LocalY=174
            const int MidLabRightLocalX = 382;
            const int MidLabRightLocalY = 174;
            int leftLabRightPortX = towerLeftPortX - SideBridgeSpanPx;
            var leftLab = RegisterAlignedToBridgeY(ArchitectureType.MidSizeMaterialAnalysisLab,
                leftLabRightPortX, bridgePortY, MidLabRightLocalX, MidLabRightLocalY);

            //右侧实验室：以其Left端口对齐到(towerRightPortX + SideBridgeSpanPx, bridgePortY)
            //MidLab的左端口LocalX=0, LocalY=174
            const int MidLabLeftLocalX = 0;
            int rightLabLeftPortX = towerRightPortX + SideBridgeSpanPx;
            var rightLab = RegisterAlignedToBridgeY(ArchitectureType.MidSizeMaterialAnalysisLab,
                rightLabLeftPortX, bridgePortY, MidLabLeftLocalX, MidLabRightLocalY);

            //左桥：实验室右端口 → 信号塔左端口
            RegisterBridge(leftLabRightPortX, towerLeftPortX, bridgePortY);
            //右桥：信号塔右端口 → 实验室左端口
            RegisterBridge(towerRightPortX, rightLabLeftPortX, bridgePortY);

            //桥面密布加特林炮台：左桥朝外（左方），右桥朝外（右方）
            ScatterGatlinOnBridge(leftLabRightPortX, towerLeftPortX, bridgePortY, faceLeft: true);
            ScatterGatlinOnBridge(towerRightPortX, rightLabLeftPortX, bridgePortY, faceLeft: false);

            //信号塔正下方桥面再补两座背靠背的炮台，作为塔基贴身护卫
            int towerCenterX = anchorPixelX;
            RegisterGatlinOnBridge(towerCenterX - GatlinPedestalWidthPx, bridgePortY, faceLeft: true);
            RegisterGatlinOnBridge(towerCenterX + GatlinPedestalWidthPx, bridgePortY, faceLeft: false);

            //外侧实验室更外延的悬空位置再放两座外向炮台，强化"外围防线"观感
            //桥面继续延伸一小段至炮台底座位置，避免炮台漂浮在虚空里没踩点
            int leftOuterCenterX = leftLab != null ? leftLab.PixelX - OuterFlankGatlinOffsetPx : leftLabRightPortX - OuterFlankGatlinOffsetPx;
            int leftOuterBridgeLeftX = leftOuterCenterX - GatlinPedestalWidthPx;
            RegisterBridge(leftOuterBridgeLeftX, leftLab != null ? leftLab.PixelX : leftLabRightPortX, bridgePortY);
            RegisterGatlinOnBridge(leftOuterCenterX, bridgePortY, faceLeft: true);

            int rightOuterCenterX = rightLab != null ? rightLab.PixelX + rightLab.WidthPx + OuterFlankGatlinOffsetPx : rightLabLeftPortX + OuterFlankGatlinOffsetPx;
            int rightOuterBridgeRightX = rightOuterCenterX + GatlinPedestalWidthPx;
            int rightLabRightEdgeX = rightLab != null ? rightLab.PixelX + rightLab.WidthPx : rightLabLeftPortX;
            RegisterBridge(rightLabRightEdgeX, rightOuterBridgeRightX, bridgePortY);
            RegisterGatlinOnBridge(rightOuterCenterX, bridgePortY, faceLeft: false);
        }

        /// <summary>
        /// 在(leftX,rightX)桥段上等距分布若干炮台，全部统一朝向
        /// </summary>
        private static void ScatterGatlinOnBridge(int leftX, int rightX, int bridgePortY, bool faceLeft) {
            int span = rightX - leftX;
            if (span <= GatlinPedestalWidthPx) return;
            for (int i = 1; i <= GatlinPerBridge; i++) {
                float t = i / (float)(GatlinPerBridge + 1);
                int centerX = leftX + (int)(span * t);
                RegisterGatlinOnBridge(centerX, bridgePortY, faceLeft);
            }
        }
    }
}
