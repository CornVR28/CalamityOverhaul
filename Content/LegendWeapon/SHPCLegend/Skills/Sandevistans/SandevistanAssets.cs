using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Skills.Sandevistans
{
    /// <summary>
    /// 斯安威斯坦残影效果的资源加载器
    /// </summary>
    internal class SandevistanAssets
    {
        /// <summary>
        /// 残影颜色偏移着色器
        /// </summary>
        [VaultLoaden(CWRConstant.Effects)]
        public static Effect SandevistanGhost { get; private set; }
    }
}
