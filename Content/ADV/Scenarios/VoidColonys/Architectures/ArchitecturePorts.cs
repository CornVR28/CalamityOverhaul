using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures
{
    /// <summary>连接端口类型，桥段对接口对应地面通行，管段对接口对应悬空能量输送</summary>
    internal enum PortKind : byte
    {
        Bridge,
        Tunnel,
    }

    /// <summary>连接端口所在的侧面，只支持水平左右两侧以保证直线对接</summary>
    internal enum PortSide : byte
    {
        Left,
        Right,
    }

    /// <summary>
    /// 建筑表面的一个对接锚点，坐标以贴图左上角为原点
    /// </summary>
    internal readonly struct ArchitecturePort(PortKind kind, PortSide side, int localX, int localY)
    {
        public readonly PortKind Kind = kind;
        public readonly PortSide Side = side;
        public readonly int LocalX = localX;
        public readonly int LocalY = localY;
    }

    /// <summary>
    /// 每种建筑的对接端口表，仅保留左右两侧端口以强制直线连接
    /// 任何需要拐角的布线需求由端口Y对齐+布局端调整解决，不靠旋转贴图实现
    /// </summary>
    internal static class ArchitecturePorts
    {
        private static readonly Dictionary<ArchitectureType, ArchitecturePort[]> Table = new() {
            //核心虚空实验室：两侧底部外挑桁架上下双层桥梁口，两侧中部一对管道口
            [ArchitectureType.CoreVoidLab] = [
                new(PortKind.Bridge, PortSide.Left, 0, 580),
                new(PortKind.Bridge, PortSide.Left, 60, 660),
                new(PortKind.Bridge, PortSide.Right, 1162, 580),
                new(PortKind.Bridge, PortSide.Right, 1102, 660),
                new(PortKind.Tunnel, PortSide.Left, 8, 360),
                new(PortKind.Tunnel, PortSide.Right, 1154, 360),
            ],

            //能源控制站：平直基座两侧桥梁口+高位两侧管道口
            [ArchitectureType.EnergyControlStation] = [
                new(PortKind.Bridge, PortSide.Left, 0, 250),
                new(PortKind.Bridge, PortSide.Right, 482, 250),
                new(PortKind.Tunnel, PortSide.Left, 8, 120),
                new(PortKind.Tunnel, PortSide.Right, 474, 120),
            ],

            //中型物料分析实验室：基座两侧桥梁口+侧壁两侧管道口
            [ArchitectureType.MidSizeMaterialAnalysisLab] = [
                new(PortKind.Bridge, PortSide.Left, 0, 348),
                new(PortKind.Bridge, PortSide.Right, 766, 348),
                new(PortKind.Tunnel, PortSide.Left, 8, 140),
                new(PortKind.Tunnel, PortSide.Right, 758, 140),
            ],

            //观测哨：塔身较窄，仅提供基座两侧桥梁口
            [ArchitectureType.ObservationPostTelescope] = [
                new(PortKind.Bridge, PortSide.Left, 0, 285),
                new(PortKind.Bridge, PortSide.Right, 262, 285),
            ],
        };

        /// <summary>获取指定建筑的全部端口，类型未注册时返回空数组</summary>
        public static ArchitecturePort[] Get(ArchitectureType type)
            => Table.TryGetValue(type, out var ports) ? ports : [];

        /// <summary>端口世界像素坐标，输入贴图左上角的世界像素坐标</summary>
        public static Vector2 ToWorldPixel(in ArchitecturePort port, int buildingPixelX, int buildingPixelY)
            => new(buildingPixelX + port.LocalX, buildingPixelY + port.LocalY);
    }
}
