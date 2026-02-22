using System;
using System.IO;
using Terraria;
using static CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants.HierophantUtils;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants
{
    /// <summary>
    /// Hierophant 的单条腿——4 段 IK 骨骼（股节→膝盖高拱→胫节→跗节），
    /// 包含落脚点搜索、缓动追踪、落地震动与网络同步。
    /// <para>
    /// 骨骼拓扑：Hip → Knee(高拱) → Shin → Foot(落脚点)
    /// </para>
    /// </summary>
    public class HierophantLeg
    {
        #region Fields

        public Vector2 StandPoint;
        public float Scale = 1f;
        public NPC NPC;
        /// <summary>相对于身体中心的理想偏移（未旋转）</summary>
        public Vector2 Offset;
        /// <summary>落脚点搜索半径倍率</summary>
        public float SearchRadius = 1f;
        public int NoMoveTime;
        public Vector2 TargetPos;
        public float MoveSpeed;
        /// <summary>本帧是否刚刚落地（用于触发震动）</summary>
        public bool JustLanded;
        /// <summary>迈步期间的抬脚高度动画进度 [0,1]</summary>
        public float LiftProgress;
        /// <summary>腿部分组索引，同组腿不同时迈步</summary>
        public int Group;
        /// <summary>是否正在迈步中（外部可读，用于步态协调）</summary>
        public bool IsMoving => _wasMoving;

        private bool _onTileFlag;
        private bool _wasMoving;
        private Vector2 _liftOrigin;
        /// <summary>落地后的冷却帧，防止立刻再次迈步</summary>
        private int _landCooldown;
        /// <summary>连续搜索失败计数，超过阈值后使用确定性回退</summary>
        private int _searchFailCount;

        /// <summary>
        /// 脚是否稳定踩在地面上。
        /// 迈步动画期间始终返回 false，避免 IK 判定抖动。
        /// </summary>
        public bool OnTile => _onTileFlag && !_wasMoving;

        #endregion

        public HierophantLeg(NPC npc, Vector2 offset, float scale = 1f, float searchRadius = 1f, int group = 0) {
            NPC = npc;
            Offset = offset;
            Scale = scale;
            SearchRadius = searchRadius;
            Group = group;
            StandPoint = npc.Center + new Vector2(offset.X, 400f);
            TargetPos = StandPoint;
        }

        #region Networking

        public void NetSend(BinaryWriter writer) {
            writer.Write(NoMoveTime);
            writer.WriteVector2(TargetPos);
            writer.WriteVector2(StandPoint);
        }

        public void NetReceive(BinaryReader reader) {
            NoMoveTime = reader.ReadInt32();
            TargetPos = reader.ReadVector2();
            StandPoint = reader.ReadVector2();
        }

        #endregion

        #region Update

        /// <summary>
        /// 每帧更新落脚点，返回 <c>true</c> 表示本帧发起了一次新的迈步。
        /// </summary>
        public bool Update() {
            var boss = (Hierophant)NPC.ModNPC;
            JustLanded = false;
            NoMoveTime--;
            if (_landCooldown > 0) _landCooldown--;

            // ── 跳跃中：所有腿收拢到身体下方 ──
            if (boss.Jumping) {
                _onTileFlag = false;
                _wasMoving = false;
                LiftProgress = 0f;
                _landCooldown = 8;
                TargetPos = NPC.Center + new Vector2(Offset.X * 0.3f, 300f) * NPC.scale;
                StandPoint = Vector2.Lerp(StandPoint, TargetPos, 0.12f);
                return false;
            }

            // ── 迈步动画进行中 ──
            if (_wasMoving) {
                AdvanceLiftAnimation();
                return false; // 迈步中不发起新步
            }

            // ── 已着地：缓慢微调到精确目标 ──
            if (Distance(StandPoint, TargetPos) > 1f) {
                StandPoint = Vector2.Lerp(StandPoint, TargetPos, 0.15f);
            }

            // ── 更新地面接触标记（仅在非迈步时检测） ──
            _onTileFlag = !IsAir(StandPoint + new Vector2(0, 4f), true);

            // ── 评估是否需要迈步 ──
            if (_landCooldown > 0) return false; // 刚着地的冷却期

            float distToMove = 160f * NPC.scale * Scale;
            float bodyRot = boss.Direction > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi);
            Vector2 idealPos = NPC.Center + NPC.velocity * 20f + (Offset * NPC.scale).RotatedBy(bodyRot);
            float dist = Distance(StandPoint, idealPos);

            bool tooFar = dist > distToMove && NoMoveTime <= 0;
            bool wayTooFar = dist > distToMove * 1.8f; // 极端拉扯时忽略冷却
            bool lostGround = !_onTileFlag && NoMoveTime <= 0;

            if (tooFar || wayTooFar || lostGround) {
                return TryInitiateStep(idealPos);
            }

            return false;
        }

        /// <summary>
        /// 推进迈步弧线动画。当 LiftProgress 到达 1.0 时完成迈步。
        /// </summary>
        private void AdvanceLiftAnimation() {
            LiftProgress = MathHelper.Clamp(LiftProgress + 0.055f, 0f, 1f);

            if (LiftProgress >= 1f) {
                // 动画完成——落地
                StandPoint = TargetPos;
                JustLanded = true;
                _wasMoving = false;
                LiftProgress = 0f;
                _landCooldown = 6; // 落地后短暂冷却
                _onTileFlag = !IsAir(StandPoint + new Vector2(0, 4f), true);
                return;
            }

            // 弧线插值：smoothstep 水平 + sin 弧抬起
            float t = LiftProgress;
            float eased = t * t * (3f - 2f * t);
            float liftArc = MathF.Sin(t * MathHelper.Pi);
            float liftHeight = 100f * NPC.scale * Scale * liftArc;

            Vector2 flatPos = Vector2.Lerp(_liftOrigin, TargetPos, eased);
            StandPoint = flatPos + new Vector2(0, -liftHeight);
        }

        /// <summary>
        /// 尝试发起一次新的迈步。搜索落脚点，成功则进入迈步动画。
        /// </summary>
        private bool TryInitiateStep(Vector2 idealPos) {
            Vector2 searchCenter = idealPos + new Vector2(
                Math.Sign(NPC.velocity.X) == Math.Sign(Offset.X)
                    ? Math.Sign(NPC.velocity.X) * 20f : 0f, 0f);

            float searchRadius = 80f * SearchRadius * Scale * NPC.scale;
            Vector2 newTarget = FindStandPoint(searchCenter, searchRadius, 100);

            // 如果搜索找到的点和当前位置太近，不值得迈步
            if (Distance(newTarget, StandPoint) < 30f * NPC.scale) {
                _onTileFlag = !IsAir(StandPoint + new Vector2(0, 4f), true);
                return false;
            }

            TargetPos = newTarget;
            _liftOrigin = StandPoint;
            _wasMoving = true;
            LiftProgress = 0f;

            if (NoMoveTime < 12) NoMoveTime = 12;
            return true;
        }

        #endregion

        #region Stand-Point Search

        /// <summary>
        /// 搜索落脚点：从 center 附近随机采样实心方块表面。
        /// 失败时使用确定性射线回退，避免反复随机失败导致扑腾。
        /// </summary>
        public Vector2 FindStandPoint(Vector2 center, float maxOffset, float maxTry = 64) {
            var boss = (Hierophant)NPC.ModNPC;
            int tries = (int)maxTry;

            // 随机采样搜索
            for (int i = 0; i < tries; i++) {
                Vector2 pos = RandomPointInCircle(maxOffset * 0.7f) + center;
                if (Distance(pos, center) > maxOffset) continue;

                if (TrySnapToSurface(ref pos)) {
                    _onTileFlag = true;
                    _searchFailCount = 0;
                    return pos;
                }
            }

            // 随机搜索失败——确定性回退：从 center 向下射线
            Vector2 rayStart = center;
            Vector2 fallback = RaycastDown(rayStart, 600f * NPC.scale);
            if (TrySnapToSurface(ref fallback)) {
                _onTileFlag = true;
                _searchFailCount = 0;
                return fallback;
            }

            // 完全失败——使用保守的固定位置
            _searchFailCount++;
            _onTileFlag = false;

            // 如果连续多次失败，不要每帧都尝试
            if (_searchFailCount > 3) {
                NoMoveTime = Math.Max(NoMoveTime, 30);
            }

            float rot = boss.Direction > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi);
            return NPC.Center + (new Vector2(Offset.X, 350f) * NPC.scale).RotatedBy(rot);
        }

        /// <summary>
        /// 尝试将位置吸附到实心表面（向上搜索直到找到空气边界）。
        /// 返回 false 表示该位置不在实心区域内。
        /// </summary>
        private bool TrySnapToSurface(ref Vector2 pos) {
            if (!Hierophant.CanStandOn(pos)) return false;

            Vector2 orgPos = pos;
            float step = 2f * NPC.scale;
            int safety = 200;

            while (Hierophant.CanStandOn(pos)) {
                pos.Y -= step;
                safety--;
                if (safety <= 0) {
                    pos = orgPos;
                    return false; // 没找到表面，可能在实心内部
                }
            }

            // 回退一步到最后的实心位置上方
            pos.Y += step;
            return true;
        }

        /// <summary>
        /// 从起始点向下射线检测，找到第一个实心位置。
        /// </summary>
        private static Vector2 RaycastDown(Vector2 start, float maxDist) {
            float step = 16f;
            Vector2 pos = start;
            for (float d = 0; d < maxDist; d += step) {
                pos.Y = start.Y + d;
                if (Hierophant.CanStandOn(pos)) return pos;
            }
            return start + new Vector2(0, maxDist);
        }

        #endregion
    }
}

