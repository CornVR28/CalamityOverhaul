using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.Gargoyles
{
    /// <summary>
    /// 流线型鸟群算法——使用空间网格加速的 Boids 变体。
    /// <para>
    /// 核心设计：<br/>
    /// · 去除凝聚力(Cohesion)，只保留分离+对齐，避免虫群挤成一团<br/>
    /// · 引入"流道(Stream)"概念——每个个体沿指定正弦波路径飞行，形成游龙般的蜿蜒队列<br/>
    /// · 使用空间哈希网格将 O(n²) 邻域搜索降至近 O(n)，支撑 3000+ 个体<br/>
    /// </para>
    /// </summary>
    internal static class GargoyleBoids
    {
        #region 流道配置

        /// <summary>流道数量——虫群被分成这么多条蜿蜒路径</summary>
        internal const int NumStreams = 8;

        private static float[] streamAmplitude;
        private static float[] streamFrequency;
        private static float[] streamPhase;
        private static float[] streamBaseY;
        private static bool initialized;

        #endregion

        #region 空间网格

        private const int CellSize = 100;
        private static readonly Dictionary<long, List<int>> grid = new(512);
        private static readonly Stack<List<int>> listPool = new(256);

        #endregion

        #region Boids 参数（流线型，反聚团）

        /// <summary>保护距离——在此范围内产生排斥力</summary>
        private const float ProtectedRange = 24f;
        /// <summary>分离权重</summary>
        private const float SeparationWeight = 0.08f;

        /// <summary>视觉感知半径——在此范围内参与对齐计算</summary>
        private const float VisualRange = 120f;
        /// <summary>对齐权重——趋向邻居平均航向</summary>
        private const float AlignmentWeight = 0.08f;

        // ※ 不设凝聚权重(Cohesion) —— 这是避免"挤成一团"的关键 ※

        /// <summary>流道Y跟踪权重——将个体拉回所属流道的正弦波路径</summary>
        private const float StreamYWeight = 0.025f;
        /// <summary>随机噪声幅度——微扰动制造有机运动感</summary>
        private const float NoiseAmount = 0.08f;

        /// <summary>最低飞行速度</summary>
        private const float MinSpeed = 9f;
        /// <summary>最高飞行速度</summary>
        private const float MaxSpeed = 18f;

        #endregion

        private static Vector2[] newVelocities = Array.Empty<Vector2>();

        #region 初始化 / 重置

        /// <summary>
        /// 初始化流道参数——在过场开始时调用一次
        /// </summary>
        /// <param name="skyY">天空基准Y坐标（摄像机上摇目标位置）</param>
        /// <param name="spreadHeight">流道垂直分布总高度</param>
        internal static void InitializeStreams(float skyY, float spreadHeight) {
            streamAmplitude = new float[NumStreams];
            streamFrequency = new float[NumStreams];
            streamPhase = new float[NumStreams];
            streamBaseY = new float[NumStreams];

            for (int i = 0; i < NumStreams; i++) {
                streamAmplitude[i] = 35f + Main.rand.NextFloat(80f);
                streamFrequency[i] = 0.001f + Main.rand.NextFloat(0.0025f);
                streamPhase[i] = Main.rand.NextFloat(MathHelper.TwoPi);
                float norm = (i + 0.5f) / NumStreams;
                streamBaseY[i] = skyY - spreadHeight * 0.5f + spreadHeight * norm;
            }

            initialized = true;
        }

        /// <summary>
        /// 获取指定流道在给定X坐标处的目标Y值
        /// </summary>
        internal static float GetStreamTargetY(int streamIndex, float x) {
            if (!initialized) return 0f;
            int idx = streamIndex % NumStreams;
            return streamBaseY[idx]
                + streamAmplitude[idx] * MathF.Sin(streamFrequency[idx] * x + streamPhase[idx]);
        }

        /// <summary>重置所有状态</summary>
        internal static void Reset() {
            initialized = false;
            ReturnGridLists();
        }

        #endregion

        #region 每帧集群更新

        /// <summary>
        /// 每帧调用一次——为整个集群计算新的速度向量
        /// </summary>
        internal static void UpdateFlock(List<GargoyleActor> flock) {
            int count = flock.Count;
            if (count == 0) return;

            BuildGrid(flock, count);

            if (newVelocities.Length < count)
                newVelocities = new Vector2[count + 256];

            float protectedSq = ProtectedRange * ProtectedRange;
            float visualSq = VisualRange * VisualRange;

            for (int i = 0; i < count; i++) {
                GargoyleActor self = flock[i];
                Vector2 pos = self.Position;
                Vector2 vel = self.BoidVelocity;

                Vector2 separation = Vector2.Zero;
                Vector2 alignSum = Vector2.Zero;
                int neighbors = 0;

                //── 仅查询相邻9个网格——近O(1)邻域搜索 ──
                int cx = (int)MathF.Floor(pos.X / CellSize);
                int cy = (int)MathF.Floor(pos.Y / CellSize);

                for (int dx = -1; dx <= 1; dx++) {
                    for (int dy = -1; dy <= 1; dy++) {
                        long key = PackKey(cx + dx, cy + dy);
                        if (!grid.TryGetValue(key, out var cell)) continue;

                        for (int ci = 0; ci < cell.Count; ci++) {
                            int j = cell[ci];
                            if (i == j) continue;

                            GargoyleActor other = flock[j];
                            float ddx = other.Position.X - pos.X;
                            float ddy = other.Position.Y - pos.Y;
                            float distSq = ddx * ddx + ddy * ddy;

                            //分离：太近则排斥，归一化方向
                            if (distSq < protectedSq && distSq > 0.001f) {
                                float invDist = 1f / MathF.Sqrt(distSq);
                                separation.X -= ddx * invDist;
                                separation.Y -= ddy * invDist;
                            }

                            //对齐：视觉范围内累加邻居速度
                            if (distSq < visualSq) {
                                alignSum += other.BoidVelocity;
                                neighbors++;
                            }
                        }
                    }
                }

                //── 合成转向力 ──
                Vector2 steer = separation * SeparationWeight;

                if (neighbors > 0) {
                    steer += (alignSum / neighbors - vel) * AlignmentWeight;
                }

                //流道跟踪——将个体拉回所属正弦波路径
                if (initialized) {
                    float targetY = GetStreamTargetY(self.SwarmGroup, pos.X);
                    float yError = targetY - pos.Y;
                    float pull = StreamYWeight * MathHelper.Clamp(MathF.Abs(yError) / 100f, 0.3f, 2f);
                    steer.Y += yError * pull;
                }

                //微噪声——有机运动感
                steer.X += (Main.rand.NextFloat() - 0.5f) * NoiseAmount * 2f;
                steer.Y += (Main.rand.NextFloat() - 0.5f) * NoiseAmount * 2f;

                //── 应用转向，限制速度 ──
                Vector2 newVel = vel + steer;

                float depthMod = MathHelper.Lerp(0.75f, 1.15f,
                    MathHelper.Clamp((self.DepthScale - 0.35f) / 0.8f, 0f, 1f));

                float speed = newVel.Length();
                float cMin = MinSpeed * depthMod;
                float cMax = MaxSpeed * depthMod;

                if (speed > cMax) newVel *= cMax / speed;
                else if (speed > 0.01f && speed < cMin) newVel *= cMin / speed;

                newVelocities[i] = newVel;
            }

            //── 双缓冲写回 ──
            for (int i = 0; i < count; i++) {
                flock[i].BoidVelocity = newVelocities[i];
            }

            ReturnGridLists();
        }

        #endregion

        #region 空间网格内部实现

        private static void BuildGrid(List<GargoyleActor> flock, int count) {
            ReturnGridLists();

            for (int i = 0; i < count; i++) {
                long key = GridKey(flock[i].Position);
                if (!grid.TryGetValue(key, out var cell)) {
                    cell = listPool.Count > 0 ? listPool.Pop() : new List<int>(16);
                    grid[key] = cell;
                }
                cell.Add(i);
            }
        }

        private static void ReturnGridLists() {
            foreach (var kvp in grid) {
                kvp.Value.Clear();
                listPool.Push(kvp.Value);
            }
            grid.Clear();
        }

        private static long GridKey(Vector2 pos) {
            int cx = (int)MathF.Floor(pos.X / CellSize);
            int cy = (int)MathF.Floor(pos.Y / CellSize);
            return PackKey(cx, cy);
        }

        private static long PackKey(int cx, int cy) {
            return ((long)cx << 32) | (uint)cy;
        }

        #endregion
    }
}
