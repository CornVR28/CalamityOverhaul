using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.Gargoyles
{
    /// <summary>
    /// 湍流鸟群算法——使用空间网格加速的 Boids 变体。
    /// <para>
    /// 核心设计：<br/>
    /// · 去除凝聚力(Cohesion)，只保留分离+对齐，避免虫群挤成一团<br/>
    /// · 流道使用3层叠加波+时间演化，路径复杂不可预测<br/>
    /// · 每个个体拥有独立的圆形游走力，制造个体间的差异化轨迹<br/>
    /// · 基于位置的湍流场，让虫群局部翻腾起伏<br/>
    /// · 使用空间哈希网格将 O(n²) 邻域搜索降至近 O(n)，支撑 3000+ 个体<br/>
    /// </para>
    /// </summary>
    internal static class GargoyleBoids
    {
        #region 流道配置

        /// <summary>流道数量</summary>
        internal const int NumStreams = 8;

        //每条流道3个叠加波层(octave)的参数
        private const int Octaves = 3;
        private static float[,] octaveAmplitude;  // [stream, octave]
        private static float[,] octaveFrequency;
        private static float[,] octavePhase;
        private static float[] streamBaseY;
        //时间演化——流道路径随时间缓慢漂移
        private static float[,] octaveTimeDrift;   // [stream, octave] 时间相位漂移速率
        private static bool initialized;
        private static int globalTick;

        #endregion

        #region 空间网格

        private const int CellSize = 100;
        private static readonly Dictionary<long, List<int>> grid = new(512);
        private static readonly Stack<List<int>> listPool = new(256);

        #endregion

        #region Boids 参数

        private const float ProtectedRange = 24f;
        private const float SeparationWeight = 0.08f;

        private const float VisualRange = 120f;
        private const float AlignmentWeight = 0.07f;

        /// <summary>流道Y跟踪权重——降低以允许更多自由翻腾</summary>
        private const float StreamYWeight = 0.015f;

        /// <summary>个体游走力权重——驱动每个个体独立的圆形游走</summary>
        private const float WanderWeight = 0.35f;

        /// <summary>湍流场权重——基于位置的伪卷曲噪声力</summary>
        private const float TurbulenceWeight = 0.2f;

        /// <summary>随机噪声幅度</summary>
        private const float NoiseAmount = 0.12f;

        private const float MinSpeed = 9f;
        private const float MaxSpeed = 18f;

        #endregion

        private static Vector2[] newVelocities = Array.Empty<Vector2>();

        #region 初始化 / 重置

        internal static void InitializeStreams(float skyY, float spreadHeight) {
            octaveAmplitude = new float[NumStreams, Octaves];
            octaveFrequency = new float[NumStreams, Octaves];
            octavePhase = new float[NumStreams, Octaves];
            octaveTimeDrift = new float[NumStreams, Octaves];
            streamBaseY = new float[NumStreams];

            for (int i = 0; i < NumStreams; i++) {
                float norm = (i + 0.5f) / NumStreams;
                streamBaseY[i] = skyY - spreadHeight * 0.5f + spreadHeight * norm;

                //3层叠加波：低频大振幅 → 高频小振幅（类似分形地形）
                for (int o = 0; o < Octaves; o++) {
                    float octaveScale = 1f / (1 + o);  // 1.0, 0.5, 0.33
                    octaveAmplitude[i, o] = (40f + Main.rand.NextFloat(60f)) * octaveScale;
                    octaveFrequency[i, o] = (0.001f + Main.rand.NextFloat(0.002f)) * (1 + o * 1.5f);
                    octavePhase[i, o] = Main.rand.NextFloat(MathHelper.TwoPi);
                    octaveTimeDrift[i, o] = Main.rand.NextFloat(0.005f, 0.02f)
                        * (Main.rand.NextBool() ? 1f : -1f);
                }
            }

            globalTick = 0;
            initialized = true;
        }

        /// <summary>
        /// 获取指定流道在给定X坐标处的目标Y值——3层叠加波+时间演化
        /// </summary>
        internal static float GetStreamTargetY(int streamIndex, float x) {
            if (!initialized) return 0f;
            int idx = streamIndex % NumStreams;

            float y = streamBaseY[idx];
            for (int o = 0; o < Octaves; o++) {
                float timeShift = globalTick * octaveTimeDrift[idx, o];
                y += octaveAmplitude[idx, o]
                    * MathF.Sin(octaveFrequency[idx, o] * x + octavePhase[idx, o] + timeShift);
            }
            return y;
        }

        internal static void Reset() {
            initialized = false;
            globalTick = 0;
            ReturnGridLists();
        }

        #endregion

        #region 湍流场

        /// <summary>
        /// 基于位置的伪卷曲噪声——产生无散度的湍流力场，
        /// 让虫群局部产生涡旋翻腾效果
        /// </summary>
        private static Vector2 GetTurbulence(float x, float y) {
            //用多频率正弦叠加模拟卷曲噪声的旋转特性
            float s1 = MathF.Sin(x * 0.0037f + y * 0.0051f);
            float s2 = MathF.Sin(x * 0.0071f - y * 0.0043f + 1.7f);
            float s3 = MathF.Cos(x * 0.0023f + y * 0.0067f - 0.9f);
            float s4 = MathF.Sin(y * 0.0089f + x * 0.0031f + 2.3f);

            //时间演化——湍流场缓慢变化
            float timeWave = MathF.Sin(globalTick * 0.008f) * 0.5f;

            //卷曲方向：∂/∂y 作为 X 分量，-∂/∂x 作为 Y 分量（近似旋度）
            return new Vector2(
                (s1 + s2 * 0.5f + timeWave) * TurbulenceWeight,
                (s3 + s4 * 0.5f - timeWave) * TurbulenceWeight
            );
        }

        #endregion

        #region 每帧集群更新

        internal static void UpdateFlock(List<GargoyleActor> flock) {
            int count = flock.Count;
            if (count == 0) return;

            globalTick++;
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

                //── 空间网格邻域搜索 ──
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

                            if (distSq < protectedSq && distSq > 0.001f) {
                                float invDist = 1f / MathF.Sqrt(distSq);
                                separation.X -= ddx * invDist;
                                separation.Y -= ddy * invDist;
                            }

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

                //流道跟踪——多层叠加波路径，柔性回拉
                if (initialized) {
                    float targetY = GetStreamTargetY(self.SwarmGroup, pos.X);
                    float yError = targetY - pos.Y;
                    float pull = StreamYWeight * MathHelper.Clamp(MathF.Abs(yError) / 120f, 0.2f, 1.5f);
                    steer.Y += yError * pull;
                }

                //个体独立游走力——每个石像鬼沿自己的圆形路径施加侧向力
                float wanderX = MathF.Cos(self.WanderPhase) * WanderWeight * self.WanderStrength;
                float wanderY = MathF.Sin(self.WanderPhase) * WanderWeight * self.WanderStrength;
                steer.X += wanderX;
                steer.Y += wanderY;

                //位置湍流场——局部涡旋翻腾
                Vector2 turb = GetTurbulence(pos.X, pos.Y);
                steer += turb;

                //随机噪声
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
