using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.Cyberwares.Implementation.Sandevistans
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
        /// 屏幕级后处理着色器（径向模糊、色差分离、去饱和、暗角、边缘辉光）
        /// </summary>
        [VaultLoaden(CWRConstant.Effects)]
        public static Effect SandevistanScreen { get; private set; }
    }
}
