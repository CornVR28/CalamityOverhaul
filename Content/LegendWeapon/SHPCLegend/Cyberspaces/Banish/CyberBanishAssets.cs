using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Banish
{
    /// <summary>
    /// 赛博放逐资源加载器
    /// </summary>
    internal class CyberBanishAssets
    {
        /// <summary>
        /// 赛博放逐NPC故障着色器
        /// </summary>
        [VaultLoaden(CWRConstant.Effects)]
        public static Effect CyberBanishNPC { get; private set; }
    }
}
