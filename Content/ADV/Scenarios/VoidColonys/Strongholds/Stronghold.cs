using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.GatlinTurrets;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Strongholds
{
    /// <summary>
    /// 虚空聚落基地基类
    /// 一个基地由若干Architecture建筑+连接桥+加特林炮台共同构成，由<see cref="StrongholdRegistry"/>统一调度生成
    /// 子类只需在<see cref="Build"/>里基于锚点世界像素坐标向各注册表登记自己的内容
    /// </summary>
    internal abstract class Stronghold
    {
        protected const int PixelsPerTile = 16;

        /// <summary>基地的标识名，用于调试与查找</summary>
        public abstract string Name { get; }

        /// <summary>
        /// 选择基地在当前世界中的锚点tile坐标，返回false表示本世界不放置该基地
        /// 锚点的语义由各基地自行决定（通常是核心建筑桥面层中心）
        /// </summary>
        public abstract bool TryPickAnchor(out int anchorTileX, out int anchorTileY);

        /// <summary>
        /// 在指定锚点世界像素位置生成基地全部内容
        /// 实现需要分别调用<see cref="ArchitectureRegistry"/>、<see cref="GatlinTurretRegistry"/>等接口
        /// </summary>
        public abstract void Build(int anchorPixelX, int anchorPixelY);

        #region 子类共用工具

        /// <summary>
        /// 把单栋建筑登记到注册表，并按其端口要求把端口Y对齐到指定的桥面世界Y
        /// 返回登记后的建筑信息，便于继续布桥
        /// </summary>
        protected static PlacedArchitecture RegisterAlignedToBridgeY(ArchitectureType type,
            int pixelXLeft, int bridgePortWorldY, int portLocalX, int portLocalY, bool flipX = false) {
            Texture2D tex = ArchitectureAsset.Get(type);
            if (tex == null) return null;
            int pixelX = pixelXLeft - portLocalX;
            int pixelY = bridgePortWorldY - portLocalY;
            ArchitectureRegistry.Add(type, pixelX, pixelY, flipX);
            return new PlacedArchitecture(type, pixelX, pixelY, tex.Width, tex.Height, flipX);
        }

        /// <summary>
        /// 在桥面上以底座中心X、桥面Y为参考，登记一座加特林炮台
        /// </summary>
        protected static void RegisterGatlinOnBridge(int pedestalCenterX, int bridgePortWorldY, bool faceLeft) {
            //桥面平台所在tile与ArchitectureConnectorActor里的tileY=StartY/16+1保持一致
            int bridgeDeckY = (bridgePortWorldY / PixelsPerTile + 1) * PixelsPerTile;
            int pedestalLeftX = pedestalCenterX - GatlinPedestalWidthPx / 2;
            int pedestalTopY = bridgeDeckY - GatlinPedestalHeightPx;
            GatlinTurretRegistry.Add(pedestalLeftX, pedestalTopY, faceLeft);
        }

        /// <summary>
        /// 登记一段水平桥梁
        /// </summary>
        protected static void RegisterBridge(int leftWorldX, int rightWorldX, int bridgePortWorldY) {
            ArchitectureRegistry.AddConnector(PortKind.Bridge, leftWorldX, bridgePortWorldY, rightWorldX);
        }

        //加特林炮台底座尺寸，与GatlinPedestal.png一致，与ArchitecturePlacer.cs内常量保持同步
        protected const int GatlinPedestalWidthPx = 354;
        protected const int GatlinPedestalHeightPx = 166;

        #endregion
    }

    /// <summary>
    /// Stronghold已完成放置后留下的运行时记录，仅用于在基地内部串联桥梁
    /// </summary>
    internal sealed class PlacedArchitecture(ArchitectureType type, int pixelX, int pixelY, int widthPx, int heightPx, bool flipX = false)
    {
        public readonly ArchitectureType Type = type;
        public readonly int PixelX = pixelX;
        public readonly int PixelY = pixelY;
        public readonly int WidthPx = widthPx;
        public readonly int HeightPx = heightPx;
        public readonly bool FlipX = flipX;

        /// <summary>查表获取该建筑指定方向、指定类型的端口在世界像素中的位置，自动考虑FlipX</summary>
        public Microsoft.Xna.Framework.Vector2 GetPortWorld(PortKind kind, PortSide side) {
            var ports = ArchitecturePorts.GetEffective(Type, WidthPx, FlipX);
            for (int i = 0; i < ports.Length; i++) {
                if (ports[i].Kind == kind && ports[i].Side == side) {
                    return ArchitecturePorts.ToWorldPixel(ports[i], PixelX, PixelY);
                }
            }
            return new Microsoft.Xna.Framework.Vector2(PixelX, PixelY);
        }
    }
}
