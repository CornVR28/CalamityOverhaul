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

        private bool _onTileFlag;
        private bool _wasMoving;
        private Vector2 _liftOrigin;

        public bool OnTile => !IsAir(StandPoint, true) && _onTileFlag;

        #endregion

        public HierophantLeg(NPC npc, Vector2 offset, float scale = 1f, float searchRadius = 1f, int group = 0) {
            NPC = npc;
            Offset = offset;
            Scale = scale;
            SearchRadius = searchRadius;
            Group = group;
            StandPoint = npc.Center + new Vector2(offset.X, 400f);
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
        /// 每帧更新落脚点，返回 <c>true</c> 表示本帧发起了一次新的迈步
        /// </summary>
        public bool Update() {
            var boss = (Hierophant)NPC.ModNPC;
            JustLanded = false;

            // 迈步动画：抬脚弧线
            if (_wasMoving && Distance(StandPoint, TargetPos) > 4f) {
                LiftProgress = MathHelper.Clamp(LiftProgress + 0.06f, 0f, 1f);
                float liftArc = MathF.Sin(LiftProgress * MathHelper.Pi);
                float liftHeight = 120f * NPC.scale * Scale * liftArc;

                float t = LiftProgress;
                float eased = t * t * (3f - 2f * t); // smoothstep
                Vector2 flatPos = Vector2.Lerp(_liftOrigin, TargetPos, eased);
                StandPoint = flatPos + new Vector2(0, -liftHeight);
            }
            else {
                // 到达目标
                if (_wasMoving) {
                    StandPoint = TargetPos;
                    JustLanded = true;
                    _wasMoving = false;
                    LiftProgress = 0f;
                }
                // 微调：缓慢滑向精确目标
                if (Distance(StandPoint, TargetPos) > 1f) {
                    StandPoint = Vector2.Lerp(StandPoint, TargetPos, 0.15f);
                }
            }

            NoMoveTime--;
            float distToMove = 160f * NPC.scale * Scale;

            if (boss.Jumping) {
                _onTileFlag = false;
                _wasMoving = false;
                LiftProgress = 0f;
                TargetPos = NPC.Center + new Vector2(Offset.X * 0.3f, 300f) * NPC.scale;
                StandPoint = Vector2.Lerp(StandPoint, TargetPos, 0.12f);
                return false;
            }

            float bodyRot = boss.Direction > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi);
            Vector2 idealPos = NPC.Center + NPC.velocity * 20f + (Offset * NPC.scale).RotatedBy(bodyRot);
            float dist = Distance(StandPoint, idealPos);

            bool needStep = !OnTile
                || (NoMoveTime <= 0 && dist > distToMove)
                || dist > distToMove * 1.5f;

            if (needStep && !_wasMoving) {
                Vector2 searchCenter = idealPos + new Vector2(
                    Math.Sign(NPC.velocity.X) == Math.Sign(Offset.X) ? Math.Sign(NPC.velocity.X) * 20f : 0f, 0f);
                Vector2 newTarget = FindStandPoint(searchCenter, 80f * SearchRadius * Scale * NPC.scale, 160);

                TargetPos = newTarget;
                MoveSpeed = Distance(TargetPos, StandPoint) * 0.15f;
                _liftOrigin = StandPoint;
                _wasMoving = true;
                LiftProgress = 0f;

                if (NoMoveTime < 10) NoMoveTime = 10;
                return true;
            }

            return false;
        }

        #endregion

        #region Stand-Point Search

        public Vector2 FindStandPoint(Vector2 center, float maxOffset, float maxTry = 64) {
            _onTileFlag = false;
            var boss = (Hierophant)NPC.ModNPC;

            for (int i = 0; i < (int)maxTry; i++) {
                Vector2 pos = RandomPointInCircle(maxOffset * 0.8f) + center;
                if (Distance(pos, center) <= maxOffset && Hierophant.CanStandOn(pos)) {
                    _onTileFlag = true;
                    Vector2 orgPos = pos;
                    int safety = 200;
                    while (Hierophant.CanStandOn(pos)) {
                        safety--;
                        pos.Y -= 2f * NPC.scale;
                        if (safety <= 0) return orgPos;
                    }
                    pos.Y += 2f * NPC.scale;
                    return pos;
                }
            }

            float rot = boss.Direction > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi);
            return NPC.Center + new Vector2(Offset.X, 400f).RotatedBy(rot);
        }

        #endregion
    }
}

