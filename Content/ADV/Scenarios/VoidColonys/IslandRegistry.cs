using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys
{
    /// <summary>
    /// 岛屿分级：决定岛屿的尺寸范围和用途
    /// </summary>
    internal enum IslandTier
    {
        /// <summary>核心岛 - 最大的中心岛屿，承载主实验室</summary>
        Core,
        /// <summary>卫星岛 - 中型岛屿，承载各功能实验室</summary>
        Satellite,
        /// <summary>哨站岛 - 小型岛屿，观察哨/传送点/辅助设施</summary>
        Outpost,
        /// <summary>碎片岛 - 微型浮岩，纯地形装饰</summary>
        Fragment
    }

    /// <summary>
    /// 单个浮岛的完整数据，供后续建筑放置、桥梁连接等系统使用
    /// </summary>
    internal class IslandData
    {
        /// <summary>唯一标识符（生成顺序）</summary>
        public int Id;
        /// <summary>岛屿分级</summary>
        public IslandTier Tier;
        /// <summary>岛屿中心X（世界坐标，格为单位）</summary>
        public int CenterX;
        /// <summary>岛屿中心Y（上表面与下锥体的交界处）</summary>
        public int CenterY;
        /// <summary>岛屿半宽</summary>
        public int HalfWidth;
        /// <summary>上部厚度</summary>
        public int TopThickness;
        /// <summary>下部深度</summary>
        public int BottomDepth;
        /// <summary>噪声种子</summary>
        public int NoiseSeed;
        /// <summary>上表面最高点Y（生成后扫描得到）</summary>
        public int SurfaceY;
        /// <summary>可选的语义标签（如"能量控制站"），用于固定结构放置时识别</summary>
        public string Tag;

        /// <summary>
        /// 岛屿占据的大致矩形范围（用于碰撞检测，避免岛屿重叠）
        /// </summary>
        public int Left => CenterX - HalfWidth;
        public int Right => CenterX + HalfWidth;
        public int Top => CenterY - TopThickness;
        public int Bottom => CenterY + BottomDepth;

        /// <summary>
        /// 检查与另一个岛屿是否距离过近（含最小间距）
        /// </summary>
        public bool OverlapsWith(IslandData other, int minGap) {
            return Left - minGap < other.Right + minGap
                && Right + minGap > other.Left - minGap
                && Top - minGap < other.Bottom + minGap
                && Bottom + minGap > other.Top - minGap;
        }

        /// <summary>
        /// 获取到另一个岛屿中心的距离
        /// </summary>
        public float DistanceTo(IslandData other) {
            float dx = CenterX - other.CenterX;
            float dy = CenterY - other.CenterY;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 在生成后扫描实际的上表面Y坐标
        /// </summary>
        public void ScanSurface() {
            SurfaceY = CenterY;
            for (int y = CenterY - TopThickness - 10; y <= CenterY + 10; y++) {
                if (y >= 0 && y < Main.maxTilesY && CenterX >= 0 && CenterX < Main.maxTilesX) {
                    if (Main.tile[CenterX, y].HasTile) {
                        SurfaceY = y;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 获取指定X坐标处的地面Y（从上往下扫描第一个实心格）
        /// 返回-1表示该X不在岛屿范围内或无实心格
        /// </summary>
        public int GetSurfaceAt(int x) {
            if (x < Left - 5 || x > Right + 5) return -1;
            int scanTop = CenterY - TopThickness - 10;
            int scanBot = CenterY + 10;
            for (int y = scanTop; y <= scanBot; y++) {
                if (y >= 0 && y < Main.maxTilesY && x >= 0 && x < Main.maxTilesX) {
                    if (Main.tile[x, y].HasTile) {
                        return y;
                    }
                }
            }
            return -1;
        }
    }

    /// <summary>
    /// 虚空聚落的全局岛屿注册表
    /// 保存所有生成的岛屿信息，供建筑放置、桥梁连接、NPC寻路等系统查询
    /// </summary>
    internal static class IslandRegistry
    {
        /// <summary>所有已注册的岛屿</summary>
        public static List<IslandData> Islands { get; private set; } = [];

        /// <summary>下一个可用的ID</summary>
        private static int nextId;

        /// <summary>清空注册表（世界生成开始时调用）</summary>
        public static void Clear() {
            Islands.Clear();
            nextId = 0;
        }

        /// <summary>注册一个岛屿并返回其数据</summary>
        public static IslandData Register(IslandTier tier, int cx, int cy,
            int halfWidth, int topThickness, int bottomDepth, int noiseSeed, string tag = null) {
            var data = new IslandData {
                Id = nextId++,
                Tier = tier,
                CenterX = cx,
                CenterY = cy,
                HalfWidth = halfWidth,
                TopThickness = topThickness,
                BottomDepth = bottomDepth,
                NoiseSeed = noiseSeed,
                Tag = tag
            };
            Islands.Add(data);
            return data;
        }

        /// <summary>
        /// 检查候选位置是否与已有岛屿冲突
        /// </summary>
        public static bool HasOverlap(int cx, int cy, int halfWidth, int topThickness, int bottomDepth, int minGap) {
            var candidate = new IslandData {
                CenterX = cx, CenterY = cy,
                HalfWidth = halfWidth, TopThickness = topThickness, BottomDepth = bottomDepth
            };
            foreach (var island in Islands) {
                if (candidate.OverlapsWith(island, minGap)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>根据标签查找岛屿</summary>
        public static IslandData FindByTag(string tag) {
            foreach (var island in Islands) {
                if (island.Tag == tag) return island;
            }
            return null;
        }

        /// <summary>根据分级获取所有岛屿</summary>
        public static List<IslandData> GetByTier(IslandTier tier) {
            List<IslandData> result = [];
            foreach (var island in Islands) {
                if (island.Tier == tier) result.Add(island);
            }
            return result;
        }

        /// <summary>在所有岛屿生成完毕后，扫描每个岛屿的实际表面高度</summary>
        public static void ScanAllSurfaces() {
            foreach (var island in Islands) {
                island.ScanSurface();
            }
        }
    }
}
