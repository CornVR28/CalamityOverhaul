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

        /// <summary>
        /// 屏幕级后处理着色器（色差分离、青色调去饱和、暗角、扫描线、数字噪点）
        /// </summary>
        [VaultLoaden(CWRConstant.Effects)]
        public static Effect SandevistanScreen { get; private set; }
    }
}
