using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures
{
    /// <summary>
    /// 建筑类型枚举，与Architectures目录下的PNG素材一一对应
    /// </summary>
    internal enum ArchitectureType : byte
    {
        /// <summary>核心虚空实验室，地图中心唯一地标</summary>
        CoreVoidLab,
        /// <summary>能源控制站，中小型供能节点</summary>
        EnergyControlStation,
        /// <summary>中型物料分析实验室，分析与熔炼车间</summary>
        MidSizeMaterialAnalysisLab,
        /// <summary>观测哨/望远镜台，边界地标</summary>
        ObservationPostTelescope,
        /// <summary>连接桥梁段，可平铺的桥段</summary>
        ConnectionBridgeSegment,
        /// <summary>管状连接通道，可平铺的输送管段</summary>
        TubularConnectorTunnel,
    }

    /// <summary>
    /// 虚空聚落建筑纹理加载与查询
    /// </summary>
    internal static class ArchitectureAsset
    {
        private const string Root = "CalamityOverhaul/Content/ADV/Scenarios/VoidColonys/Architectures/";

        [VaultLoaden(Root + "CoreVoidLab")]
        public static Texture2D CoreVoidLab = null;
        [VaultLoaden(Root + "EnergyControlStation")]
        public static Texture2D EnergyControlStation = null;
        [VaultLoaden(Root + "MidSizeMaterialAnalysisLab")]
        public static Texture2D MidSizeMaterialAnalysisLab = null;
        [VaultLoaden(Root + "ObservationPostTelescope")]
        public static Texture2D ObservationPostTelescope = null;
        [VaultLoaden(Root + "ConnectionBridgeSegment")]
        public static Texture2D ConnectionBridgeSegment = null;
        [VaultLoaden(Root + "TubularConnectorTunnel")]
        public static Texture2D TubularConnectorTunnel = null;

        /// <summary>根据类型查表得到对应纹理</summary>
        public static Texture2D Get(ArchitectureType type) => type switch {
            ArchitectureType.CoreVoidLab => CoreVoidLab,
            ArchitectureType.EnergyControlStation => EnergyControlStation,
            ArchitectureType.MidSizeMaterialAnalysisLab => MidSizeMaterialAnalysisLab,
            ArchitectureType.ObservationPostTelescope => ObservationPostTelescope,
            ArchitectureType.ConnectionBridgeSegment => ConnectionBridgeSegment,
            ArchitectureType.TubularConnectorTunnel => TubularConnectorTunnel,
            _ => null,
        };
    }
}
