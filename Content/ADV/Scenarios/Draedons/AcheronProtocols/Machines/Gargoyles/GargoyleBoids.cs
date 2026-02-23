using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.Gargoyles
{
    /// <summary>
    /// 鸟群算法(Boids)——集中计算所有石像鬼的转向力。
    /// 实现 Craig Reynolds 经典三规则 + 分组亲和 + 目标追踪 + 深度速度修正。
    /// 使用双缓冲避免更新顺序对结果的影响
    /// </summary>
    internal static class GargoyleBoids
    {
        //── 调参常量 ──

        /// <summary>视觉感知半径——在此范围内的邻居参与对齐和凝聚计算</summary>
        private const float VisualRange = 140f;
        /// <summary>保护距离——在此范围内的邻居触发分离力</summary>
        private const float ProtectedRange = 30f;

        /// <summary>分离权重——避免碰撞和重叠</summary>
        private const float SeparationWeight = 0.05f;
        /// <summary>对齐权重——趋向邻居的平均航向</summary>
        private const float AlignmentWeight = 0.04f;
        /// <summary>凝聚权重——趋向邻居的平均位置</summary>
        private const float CohesionWeight = 0.003f;
        /// <summary>同组亲和权重——同SwarmGroup的个体额外凝聚</summary>
        private const float GroupAffinityWeight = 0.001f;

        /// <summary>目标追踪权重——趋向全局航路点</summary>
        private const float TargetWeight = 0.012f;
        /// <summary>随机噪声幅度——制造有机感的微扰动</summary>
        private const float NoiseAmount = 0.12f;

        /// <summary>最低飞行速度</summary>
        private const float MinSpeed = 1.8f;
        /// <summary>最高飞行速度</summary>
        private const float MaxSpeed = 5.5f;

        //── 双缓冲——计算完毕后一次性写入，避免顺序依赖 ──
        private static Vector2[] newVelocities = Array.Empty<Vector2>();

        /// <summary>
        /// 每帧调用一次——为整个集群计算新的速度向量
        /// </summary>
        /// <param name="flock">所有活跃的石像鬼列表</param>
        /// <param name="waypoint">当前全局航路点（集群的大致飞行目标）</param>
        internal static void UpdateFlock(List<GargoyleActor> flock, Vector2 waypoint) {
            int count = flock.Count;
            if (count == 0) return;

            //按需扩容，避免每帧分配
            if (newVelocities.Length < count) {
                newVelocities = new Vector2[count + 64];
            }

            float visualSq = VisualRange * VisualRange;
            float protectedSq = ProtectedRange * ProtectedRange;

            for (int i = 0; i < count; i++) {
                GargoyleActor self = flock[i];
                Vector2 pos = self.Position;
                Vector2 vel = self.BoidVelocity;
                int group = self.SwarmGroup;

                Vector2 separation = Vector2.Zero;
                Vector2 alignSum = Vector2.Zero;
                Vector2 cohesionSum = Vector2.Zero;
                Vector2 groupCohesionSum = Vector2.Zero;
                int neighbors = 0;
                int groupNeighbors = 0;

                for (int j = 0; j < count; j++) {
                    if (i == j) continue;
                    GargoyleActor other = flock[j];

                    float dx = other.Position.X - pos.X;
                    float dy = other.Position.Y - pos.Y;
                    float distSq = dx * dx + dy * dy;

                    //——分离：太近的邻居产生排斥力（反向位移累加）
                    if (distSq < protectedSq && distSq > 0.001f) {
                        separation += pos - other.Position;
                    }

                    //——对齐 + 凝聚：视觉范围内的邻居
                    if (distSq < visualSq) {
                        alignSum += other.BoidVelocity;
                        cohesionSum += other.Position;
                        neighbors++;

                        //同组额外凝聚
                        if (other.SwarmGroup == group) {
                            groupCohesionSum += other.Position;
                            groupNeighbors++;
                        }
                    }
                }

                //── 合成转向力 ──
                Vector2 steer = separation * SeparationWeight;

                if (neighbors > 0) {
                    //对齐：趋向邻居的平均速度方向
                    steer += (alignSum / neighbors - vel) * AlignmentWeight;
                    //凝聚：趋向邻居的平均位置
                    steer += (cohesionSum / neighbors - pos) * CohesionWeight;
                }

                if (groupNeighbors > 0) {
                    //同组亲和
                    steer += (groupCohesionSum / groupNeighbors - pos) * GroupAffinityWeight;
                }

                //目标追踪——距离越远力越强，避免集群偏离航线太远
                Vector2 toTarget = waypoint - pos;
                float targetDist = toTarget.Length();
                if (targetDist > 1f) {
                    float targetStrength = TargetWeight * MathHelper.Clamp(targetDist / 600f, 0.2f, 1.5f);
                    steer += (toTarget / targetDist) * targetStrength;
                }

                //随机噪声——微扰动制造有机运动感
                steer.X += (Main.rand.NextFloat() - 0.5f) * NoiseAmount * 2f;
                steer.Y += (Main.rand.NextFloat() - 0.5f) * NoiseAmount * 2f;

                //── 应用转向，限制速度 ──
                Vector2 newVel = vel + steer;

                //深度速度修正——远处（小）的个体飞得稍慢，制造视差
                float depthSpeedMod = MathHelper.Lerp(0.7f, 1.1f,
                    MathHelper.Clamp((self.DepthScale - 0.35f) / 0.8f, 0f, 1f));

                float speed = newVel.Length();
                float clampedMin = MinSpeed * depthSpeedMod;
                float clampedMax = MaxSpeed * depthSpeedMod;

                if (speed > clampedMax) {
                    newVel *= clampedMax / speed;
                }
                else if (speed > 0.01f && speed < clampedMin) {
                    newVel *= clampedMin / speed;
                }

                newVelocities[i] = newVel;
            }

            //双缓冲写回——一次性更新所有个体
            for (int i = 0; i < count; i++) {
                flock[i].BoidVelocity = newVelocities[i];
            }
        }
    }
}
