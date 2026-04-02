using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.DomainFreeze
{
    /// <summary>
    /// 赛博领域冻结资源加载器
    /// </summary>
    internal class CyberDomainFreezeAssets
    {
        /// <summary>
        /// 黑墙能量波六角网格着色器
        /// </summary>
        [VaultLoaden(CWRConstant.Effects)]
        public static Effect CyberFreezeWave { get; private set; }

        /// <summary>
        /// 冻结NPC/弹幕故障+六角覆盖着色器
        /// </summary>
        [VaultLoaden(CWRConstant.Effects)]
        public static Effect CyberFreezeEntity { get; private set; }
    }
}
