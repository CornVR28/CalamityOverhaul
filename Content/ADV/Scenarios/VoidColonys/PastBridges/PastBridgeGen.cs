using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.PastBridges
{
    /// <summary>
    /// 过去桥梁生成器
    /// 在虚空地形完成后被调用，为Core↔每个Satellite以及相邻Satellite之间架设轻度下垂的拱桥
    /// 所有方块只写入注册表不直接放置，Apply/Restore由时空切换系统按需驱动
    /// </summary>
    internal static class PastBridgeGen
    {
        //桥梁厚度，桥面+承重层
        private const int BridgeThickness = 3;
        //桥梁下垂深度系数，跨度越大下垂越多
        private const float SagFactor = 0.05f;
        //距离超过该阈值则视为过长放弃，避免横穿地图
        private const int MaxBridgeSpan = 900;

        /// <summary>
        /// 建造所有桥梁并注册
        /// 调用时VoidFrameworkPast类型已加载可通过ModContent解析
        /// </summary>
        public static void BuildAll() {
            PastBridgeRegistry.Clear();

            ushort pastType = (ushort)ModContent.TileType<VoidFrameworkPast>();

            var core = IslandRegistry.FindByTag("核心实验室");
            if (core == null) return;

            var satellites = IslandRegistry.GetByTier(IslandTier.Satellite);

            //Core连到每个Satellite
            foreach (var sat in satellites) {
                BuildBridge(core, sat, pastType);
            }

            //Satellite之间按中心角度排序，相邻两两相连，形成外环
            if (satellites.Count >= 2) {
                satellites.Sort((a, b) => {
                    float angA = MathF.Atan2(a.CenterY - core.CenterY, a.CenterX - core.CenterX);
                    float angB = MathF.Atan2(b.CenterY - core.CenterY, b.CenterX - core.CenterX);
                    return angA.CompareTo(angB);
                });
                for (int i = 0; i < satellites.Count; i++) {
                    var a = satellites[i];
                    var b = satellites[(i + 1) % satellites.Count];
                    BuildBridge(a, b, pastType);
                }
            }
        }

        /// <summary>
        /// 在两座岛之间架设一条下垂拱桥
        /// </summary>
        private static void BuildBridge(IslandData a, IslandData b, ushort pastType) {
            bool rightward = b.CenterX > a.CenterX;
            //锚点定在岛屿朝向对方的边缘稍外侧，并使用岛屿扫描表面高度
            int startX = rightward ? a.Right + 1 : a.Left - 1;
            int endX = rightward ? b.Left - 1 : b.Right + 1;
            int startY = a.SurfaceY;
            int endY = b.SurfaceY;

            int span = Math.Abs(endX - startX);
            if (span <= 0 || span > MaxBridgeSpan) return;

            //下垂量按跨度比例，长桥更明显的悬链感
            float sagDepth = span * SagFactor;
            if (sagDepth > 40f) sagDepth = 40f;

            int step = rightward ? 1 : -1;
            int segments = span;

            for (int i = 0; i <= segments; i++) {
                float t = i / (float)segments;
                int x = startX + i * step;

                //线性插值Y再叠加抛物线下垂，中点最低
                float yLine = startY + (endY - startY) * t;
                float sag = 4f * t * (1f - t) * sagDepth;
                int y = (int)MathF.Round(yLine + sag);

                PlaceBridgeColumn(x, y, pastType);

                //每隔一段距离向桥面上方添加一个小立柱栏杆节点，增加轮廓辨识
                if (i > 2 && i < segments - 2 && i % 12 == 0) {
                    PastBridgeRegistry.Add(x, y - 1, pastType);
                }
            }
        }

        /// <summary>
        /// 在(x,y)位置垂直放置桥身厚度的一列方块
        /// 只添加记录，不立即写入世界
        /// </summary>
        private static void PlaceBridgeColumn(int x, int y, ushort pastType) {
            int w = Main.maxTilesX;
            int h = Main.maxTilesY;
            if ((uint)x >= (uint)w) return;

            for (int dy = 0; dy < BridgeThickness; dy++) {
                int py = y + dy;
                if ((uint)py >= (uint)h) continue;
                //跳过现有岛屿内部格，避免在锚点附近与岛体重叠
                if (Main.tile[x, py].HasTile) continue;
                PastBridgeRegistry.Add(x, py, pastType);
            }
        }
    }
}
