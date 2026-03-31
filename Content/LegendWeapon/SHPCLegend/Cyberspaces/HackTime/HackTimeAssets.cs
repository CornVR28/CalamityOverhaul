using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.HackTime
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
    }
}
