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
        //端口坐标以当前贴图左上角为原点，数值随贴图尺寸等比缩放
        //当前建筑贴图相较最初版本已统一缩小到一半（桥梁/管道同样）
        //右侧端口的X直接使用真实贴图宽度作为边界，避免四舍五入带来的1像素错位
        private static readonly Dictionary<ArchitectureType, ArchitecturePort[]> Table = new() {
            //核心虚空实验室 580x378：两侧底部外挑桁架上下双层桥梁口+两侧中部一对管道口
            [ArchitectureType.CoreVoidLab] = [
                new(PortKind.Bridge, PortSide.Left, 0, 290),
                new(PortKind.Bridge, PortSide.Left, 30, 330),
                new(PortKind.Bridge, PortSide.Right, 580, 290),
                new(PortKind.Bridge, PortSide.Right, 550, 330),
                new(PortKind.Tunnel, PortSide.Left, 4, 180),
                new(PortKind.Tunnel, PortSide.Right, 576, 180),
            ],

            //能源控制站 240x136：平直基座两侧桥梁口+高位两侧管道口
            [ArchitectureType.EnergyControlStation] = [
                new(PortKind.Bridge, PortSide.Left, 0, 125),
                new(PortKind.Bridge, PortSide.Right, 240, 125),
                new(PortKind.Tunnel, PortSide.Left, 4, 60),
                new(PortKind.Tunnel, PortSide.Right, 236, 60),
            ],

            //中型物料分析实验室 382x184：基座两侧桥梁口+侧壁两侧管道口
            [ArchitectureType.MidSizeMaterialAnalysisLab] = [
                new(PortKind.Bridge, PortSide.Left, 0, 174),
                new(PortKind.Bridge, PortSide.Right, 382, 174),
                new(PortKind.Tunnel, PortSide.Left, 4, 70),
                new(PortKind.Tunnel, PortSide.Right, 378, 70),
            ],

            //观测哨 130x154：塔身较窄，仅提供基座两侧桥梁口
            [ArchitectureType.ObservationPostTelescope] = [
                new(PortKind.Bridge, PortSide.Left, 0, 142),
                new(PortKind.Bridge, PortSide.Right, 130, 142),
            ],

            //信号塔 218x516：纵向塔，仅在底部两侧开桥梁口，桥面正好托住塔基
            [ArchitectureType.SignalTower] = [
                new(PortKind.Bridge, PortSide.Left, 0, 506),
                new(PortKind.Bridge, PortSide.Right, 218, 506),
            ],

            //X桁架斜桥 266x178：左下低端、右上高端，两端共同负责抬升垂直差
            //端口Y放在主梁中线附近，让与其相接的水平桥面平齐
            [ArchitectureType.ConnectionBridgeSlope] = [
                new(PortKind.Bridge, PortSide.Left, 0, 160),
                new(PortKind.Bridge, PortSide.Right, 266, 30),
            ],

            //铁锈加固阶梯 362x208：完整阶梯走廊，左下低端、右上高端
            //端口Y同样取阶梯两端的主体踏面中线
            [ArchitectureType.ReinforcedRustedPathway] = [
                new(PortKind.Bridge, PortSide.Left, 0, 188),
                new(PortKind.Bridge, PortSide.Right, 362, 42),
            ],
        };

        /// <summary>
        /// 获取水平镜像后的端口列表
        /// Side互换，LocalX反向，LocalY不变
        /// </summary>
        public static ArchitecturePort[] GetMirrored(ArchitectureType type, int widthPx) {
            var raw = Get(type);
            if (raw.Length == 0) return [];
            var mirrored = new ArchitecturePort[raw.Length];
            for (int i = 0; i < raw.Length; i++) {
                PortSide newSide = raw[i].Side == PortSide.Left ? PortSide.Right : PortSide.Left;
                mirrored[i] = new ArchitecturePort(raw[i].Kind, newSide, widthPx - raw[i].LocalX, raw[i].LocalY);
            }
            return mirrored;
        }

        /// <summary>
        /// 根据flipX返回应使用的端口数组，true时自动镜像
        /// </summary>
        public static ArchitecturePort[] GetEffective(ArchitectureType type, int widthPx, bool flipX)
            => flipX ? GetMirrored(type, widthPx) : Get(type);

        /// <summary>获取指定建筑的全部端口，类型未注册时返回空数组</summary>
        public static ArchitecturePort[] Get(ArchitectureType type)
            => Table.TryGetValue(type, out var ports) ? ports : [];

        /// <summary>端口世界像素坐标，输入贴图左上角的世界像素坐标</summary>
        public static Vector2 ToWorldPixel(in ArchitecturePort port, int buildingPixelX, int buildingPixelY)
            => new(buildingPixelX + port.LocalX, buildingPixelY + port.LocalY);
    }
}
