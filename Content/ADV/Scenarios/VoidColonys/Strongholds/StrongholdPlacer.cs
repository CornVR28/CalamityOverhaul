using System.Collections.Generic;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Strongholds
{
    /// <summary>
    /// 基地放置调度器
    /// 在<see cref="Architectures.ArchitecturePlacer.BuildAll"/>核心簇与卫星岛安排完毕后被调用
    /// 遍历所有已登记的基地实现，让每个基地决定自己的锚点并向各注册表写入内容
    /// </summary>
    internal static class StrongholdPlacer
    {
        /// <summary>当前世界生成中所有需要放置的基地实例</summary>
        private static readonly List<Stronghold> Strongholds = [
            new ZeroSiteStronghold(),
        ];

        public static void BuildAll() {
            foreach (var stronghold in Strongholds) {
                if (!stronghold.TryPickAnchor(out int tileX, out int tileY)) continue;
                int pixelX = tileX * 16;
                int pixelY = tileY * 16;
                stronghold.Build(pixelX, pixelY);
            }
        }
    }
}
