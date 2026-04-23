using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.LaserCannons;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.SignalTowers;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Strongholds
{
#if DEBUG
    /// <summary>
    /// 调试指令：聊天里输入 /twincannon 把玩家瞬移到双层炮台堡垒锚点
    /// 默认把玩家放到上层桥面正上方的空位，避免直接卡进桥面碰撞
    /// </summary>
    internal class TwinDeckCannonDebugCommand : ModCommand
    {
        public override string Command => "twincannon";
        public override string Description => "Debug: 传送到双层炮台堡垒锚点";
        public override CommandType Type => CommandType.Chat;

        public override void Action(CommandCaller caller, string input, string[] args) {
            var stronghold = new TwinDeckCannonStronghold();
            if (!stronghold.TryPickAnchor(out int tileX, out int tileY)) {
                caller.Reply("TwinDeckCannon锚点不可用（世界尺寸可能过小）。");
                return;
            }

            //tile坐标→像素，然后抬高一点避免被桥面碰撞卡住
            Vector2 worldPos = new(tileX * 16f, tileY * 16f - 160f);
            caller.Player.Teleport(worldPos, 1, 0);
            caller.Player.velocity = Vector2.Zero;
            caller.Reply($"已传送到双层炮台堡垒：tile({tileX},{tileY})。");
        }
    }
#endif

    /// <summary>
    /// 双层炮台堡垒：顶部中央以信号塔为地标，上层为宽桥+两座外侧巨型激光炮，下层为较窄桥+两座外侧巨型激光炮
    /// 上下两层之间通过多段X桁架斜桥级联形成倒梯形阶梯，制造出巨型炮台所需的纵深空间
    /// 四座激光炮分别从上层左右与下层左右指向中央，构成X形交叉火力封锁
    /// </summary>
    internal class TwinDeckCannonStronghold : Stronghold
    {
        public override string Name => "TwinDeckCannonBattery";

        //X桁架斜桥贴图与端口常量，与ArchitecturePorts登记一致
        private const int SlopeWidthPx = 266;
        private const int SlopeHighLocalY = 30;
        private const int SlopeLowLocalY = 160;
        //单段斜桥两端的Y差，用于按段累加估算双层垂直落差
        private const int SlopeYDiffPx = SlopeLowLocalY - SlopeHighLocalY;

        //每侧级联斜桥段数，控制双层桥纵深；段数越多上下层落差越大、级联水平跨度也越大
        //4段≈ 520px落差/1064px水平跨，已能完整容纳一座激光炮的高度
        private const int SlopeSegmentsPerSide = 4;
        //双层桥的垂直落差，由级联斜桥总Y差决定
        private const int DeckGapPx = SlopeSegmentsPerSide * SlopeYDiffPx;
        //一侧级联斜桥总水平跨度
        private const int SlopeChainSpanPx = SlopeSegmentsPerSide * SlopeWidthPx;

        //上层桥单侧外延，回到与零号站点接近的尺寸，避免炮台拉得过远
        private const int UpperHalfSpanPx = 1800;
        //下层桥端相对斜桥低端再向外延伸的距离，给底层激光炮底座留出整段桥面
        private const int LowerDeckExtraSpanPx = 750;
        //下层桥单侧外延：斜桥低端世界X = ±(UpperHalfSpan - SlopeChainSpan)，再向外推 LowerDeckExtraSpan
        private const int LowerHalfSpanPx = UpperHalfSpanPx - SlopeChainSpanPx + LowerDeckExtraSpanPx;

        //巨型激光炮底座尺寸，与LaserCannonPedestal.png一致
        private const int LaserCannonPedestalWidthPx = 870;
        private const int LaserCannonPedestalHeightPx = 472;
        //上层激光炮中心相对锚点的水平偏移，落在上层桥外端略向内一段
        private const int UpperLaserCenterOffsetPx = UpperHalfSpanPx - 450;
        //下层激光炮中心相对锚点的水平偏移，落在下层桥外端略向内一段
        private const int LowerLaserCenterOffsetPx = LowerHalfSpanPx - 350;

        //中型物料分析实验室常量，作为辅助建筑摆在上层桥上塔与炮台之间
        private const int MidLabLeftLocalX = 0;
        private const int MidLabRightLocalX = 382;
        private const int MidLabPortLocalY = 174;
        //能源控制站常量，作为辅助建筑摆在下层桥两端
        private const int EnergyStationLeftLocalX = 0;
        private const int EnergyStationRightLocalX = 240;
        private const int EnergyStationPortLocalY = 125;

        public override bool TryPickAnchor(out int anchorTileX, out int anchorTileY) {
            //镜像于零号站点，落在世界左侧靠边
            int marginTilesX = 600;
            anchorTileX = marginTilesX;
            //放在世界垂直中线略偏下，与零号站点错开高度，避免视觉重叠
            anchorTileY = Main.maxTilesY / 2 + 80;
            //世界过小时退回
            if (anchorTileX >= Main.maxTilesX / 2 - 200) {
                anchorTileY = 0;
                return false;
            }
            return true;
        }

        public override void Build(int anchorPixelX, int anchorPixelY) {
            int upperBridgeY = anchorPixelY;
            int lowerBridgeY = upperBridgeY + DeckGapPx;

            //顶部中央信号塔：底部桥口对齐上层桥面
            int towerLeftX = anchorPixelX - SignalTower.WidthPx / 2;
            int towerTopY = upperBridgeY - SignalTower.BridgePortLocalY;
            ArchitectureRegistry.Add(ArchitectureType.SignalTower, towerLeftX, towerTopY);
            int towerLeftPortX = towerLeftX;
            int towerRightPortX = towerLeftX + SignalTower.WidthPx;

            //上层桥两端外延位置
            int upperLeftEndX = anchorPixelX - UpperHalfSpanPx;
            int upperRightEndX = anchorPixelX + UpperHalfSpanPx;

            //在塔与上层桥端之间各放一座中型物料分析实验室作为辅助建筑
            int midLabInsetPx = 700;
            int leftMidLabRightPortX = anchorPixelX - midLabInsetPx;
            int rightMidLabLeftPortX = anchorPixelX + midLabInsetPx;
            RegisterAlignedToBridgeY(ArchitectureType.MidSizeMaterialAnalysisLab,
                leftMidLabRightPortX, upperBridgeY, MidLabRightLocalX, MidLabPortLocalY);
            RegisterAlignedToBridgeY(ArchitectureType.MidSizeMaterialAnalysisLab,
                rightMidLabLeftPortX, upperBridgeY, MidLabLeftLocalX, MidLabPortLocalY);
            int leftMidLabLeftPortX = leftMidLabRightPortX - MidLabRightLocalX;
            int rightMidLabRightPortX = rightMidLabLeftPortX + MidLabRightLocalX;

            //上层主桥从左到右贯穿：左外延→左实验室→塔下→右实验室→右外延
            RegisterBridge(upperLeftEndX, leftMidLabLeftPortX, upperBridgeY);
            RegisterBridge(leftMidLabRightPortX, towerLeftPortX, upperBridgeY);
            RegisterBridge(towerLeftPortX, towerRightPortX, upperBridgeY);
            RegisterBridge(towerRightPortX, rightMidLabLeftPortX, upperBridgeY);
            RegisterBridge(rightMidLabRightPortX, upperRightEndX, upperBridgeY);

            //倒梯形阶梯：左侧斜桥从上层外端向下向内级联，右侧镜像
            //左侧使用FlipX：每段Left(0,30)为高端、Right(266,160)为低端，按段叠加向右下延伸
            int leftStairTopWorldX = upperLeftEndX;
            for (int i = 0; i < SlopeSegmentsPerSide; i++) {
                int segLeftPortWorldX = leftStairTopWorldX + i * SlopeWidthPx;
                int segLeftPortWorldY = upperBridgeY + i * SlopeYDiffPx;
                RegisterAlignedToBridgeY(ArchitectureType.ConnectionBridgeSlope,
                    segLeftPortWorldX, segLeftPortWorldY, 0, SlopeHighLocalY, flipX: true);
            }
            int leftStairBottomWorldX = leftStairTopWorldX + SlopeChainSpanPx;

            //右侧未翻转：每段Left(0,160)为低端、Right(266,30)为高端，按段叠加向左下延伸
            //从上层右端Right端口出发，每往内一段，左端口位于右端口X-266，Y较高端低SlopeYDiff
            int rightStairTopWorldX = upperRightEndX;
            for (int i = 0; i < SlopeSegmentsPerSide; i++) {
                int segRightPortWorldX = rightStairTopWorldX - i * SlopeWidthPx;
                int segRightPortWorldY = upperBridgeY + i * SlopeYDiffPx;
                RegisterAlignedToBridgeY(ArchitectureType.ConnectionBridgeSlope,
                    segRightPortWorldX, segRightPortWorldY, SlopeWidthPx, SlopeHighLocalY);
            }
            int rightStairBottomWorldX = rightStairTopWorldX - SlopeChainSpanPx;

            //下层桥两端外延位置：斜桥低端再向外推一段
            int lowerLeftEndX = anchorPixelX - LowerHalfSpanPx;
            int lowerRightEndX = anchorPixelX + LowerHalfSpanPx;

            //下层桥端各登记一座能源控制站作为辅助建筑，桥口与下层桥面对齐
            RegisterAlignedToBridgeY(ArchitectureType.EnergyControlStation,
                lowerLeftEndX, lowerBridgeY, EnergyStationRightLocalX, EnergyStationPortLocalY);
            RegisterAlignedToBridgeY(ArchitectureType.EnergyControlStation,
                lowerRightEndX, lowerBridgeY, EnergyStationLeftLocalX, EnergyStationPortLocalY);

            //下层桥三段：左端→左斜桥落点、左右斜桥落点之间、右斜桥落点→右端
            RegisterBridge(lowerLeftEndX, leftStairBottomWorldX, lowerBridgeY);
            RegisterBridge(leftStairBottomWorldX, rightStairBottomWorldX, lowerBridgeY);
            RegisterBridge(rightStairBottomWorldX, lowerRightEndX, lowerBridgeY);

            //四座巨型激光炮：上下层左右各一座，全部坐落在桥面上、炮口指向中央，构成X形交叉火力
            int upperPedestalTopY = upperBridgeY - LaserCannonPedestalHeightPx;
            int lowerPedestalTopY = lowerBridgeY - LaserCannonPedestalHeightPx;
            PlaceLaserCannon(anchorPixelX - UpperLaserCenterOffsetPx, upperPedestalTopY, faceLeft: false);
            PlaceLaserCannon(anchorPixelX + UpperLaserCenterOffsetPx, upperPedestalTopY, faceLeft: true);
            PlaceLaserCannon(anchorPixelX - LowerLaserCenterOffsetPx, lowerPedestalTopY, faceLeft: false);
            PlaceLaserCannon(anchorPixelX + LowerLaserCenterOffsetPx, lowerPedestalTopY, faceLeft: true);
        }

        /// <summary>
        /// 以底座中心X、底座顶部世界Y为参数登记一座巨型激光炮
        /// </summary>
        private static void PlaceLaserCannon(int pedestalCenterX, int pedestalTopY, bool faceLeft) {
            int leftX = pedestalCenterX - LaserCannonPedestalWidthPx / 2;
            LaserCannonRegistry.Add(leftX, pedestalTopY, faceLeft);
        }
    }
}
