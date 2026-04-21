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
    }
}
