using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.LaserCannons
{
    /// <summary>
    /// 巨型激光炮台贴图加载
    /// 共用Architectures目录下的底座与炮身两张原始素材
    /// </summary>
    [VaultLoaden("CalamityOverhaul/Content/ADV/Scenarios/VoidColonys/Architectures")]
    internal static class LaserCannonAsset
    {
        /// <summary>激光炮底座，下半为基座固定结构，上方凹槽承载炮身枢轴</summary>
        public static Texture2D LaserCannonPedestal;
        /// <summary>激光炮炮身，后部为能量舱，前部为长枪管与发射嘴</summary>
        public static Texture2D LaserCannon;
    }
}
