using Microsoft.Xna.Framework;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime
{
    /// <summary>
    /// 可扫描目标的抽象接口
    /// <br/>将扫描数据的生成从具体类型（NPC、物块等）中解耦
    /// </summary>
    internal interface IScannable
    {
        /// <summary>
        /// 目标在世界中的中心坐标
        /// </summary>
        Vector2 WorldCenter { get; }

        /// <summary>
        /// 目标是否仍然有效（例如NPC是否存活、物块是否还存在）
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// 目标是否可以被骇入（NPC可以，物块不行）
        /// </summary>
        bool IsHackable { get; }

        /// <summary>
        /// 扫描数据的总行数
        /// </summary>
        int ScanRowCount { get; }

        /// <summary>
        /// 构建扫描面板中要显示的数据行
        /// </summary>
        void BuildScanData(string[] labels, string[] values, Color[] colors);
    }
}
