using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.GatlinTurrets
{
    /// <summary>
    /// 加特林炮台贴图加载
    /// 共用Architectures目录下的两张原始素材Gatlin与GatlinPedestal
    /// 通过ModContent.Request按需加载，避免子命名空间下VaultLoaden扫描遗漏导致贴图为null
    /// </summary>
    [VaultLoaden("CalamityOverhaul/Content/ADV/Scenarios/VoidColonys/Architectures")]
    internal static class GatlinTurretAsset
    {
        /// <summary>加特林炮台底座，贴图中心偏上为枪架凸台</summary>
        public static Texture2D GatlinPedestal;
        /// <summary>加特林枪身本体，后部为枪管束所在</summary>
        public static Texture2D Gatlin;
    }
}
