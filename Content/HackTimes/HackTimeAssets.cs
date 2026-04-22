using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.HackTimes
{
    /// <summary>
    /// 骇客时间资源加载器
    /// </summary>
    internal class HackTimeAssets
    {
        /// <summary>
        /// 骇客时间屏幕后处理着色器
        /// </summary>
        [VaultLoaden(CWRConstant.Effects)]
        public static Effect HackTimeScreen { get; private set; }

        /// <summary>
        /// 骇客时间NPC高亮着色器（描边+赛博滤镜）
        /// </summary>
        [VaultLoaden(CWRConstant.Effects)]
        public static Effect HackTimeNPCHighlight { get; private set; }

        /// <summary>
        /// 骇客时间灵异目标高亮着色器（有机撕裂+血雾+紫红色差）
        /// </summary>
        [VaultLoaden(CWRConstant.Effects)]
        public static Effect HackWraithHighlight { get; private set; }

        /// <summary>
        /// 骇客炮台电路故障着色器（短路冷蓝/过载热红，强RGB色散+电弧闪烁）
        /// </summary>
        [VaultLoaden(CWRConstant.Effects)]
        public static Effect HackTurretCircuitFault { get; private set; }
    }
}
