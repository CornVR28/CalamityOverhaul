using System;
using System.IO;
using Terraria;
using static CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants.HierophantUtils;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants
{
    /// <summary>
    /// Hierophant 的单条腿——包含落脚点搜索、IK 目标追踪与网络同步
    /// </summary>
    public class HierophantLeg
    {
        public Vector2 StandPoint;
        public float Scale = 1f;
        public NPC NPC;
        public Vector2 Offset;
        public int NoMoveTime;
        public Vector2 TargetPos;
        public float MoveSpeed;
        private bool _onTileFlag;

        public bool OnTile => !IsAir(StandPoint, true) && _onTileFlag;

        public HierophantLeg(NPC npc, Vector2 offset, float scale = 1f) {
            NPC = npc;
            Offset = offset;
            Scale = scale;
            StandPoint = npc.Center;
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
            float speedMult = NPC.velocity.Y > 1f ? 3f : 1f;
            if (Distance(StandPoint, TargetPos) < MoveSpeed * speedMult) {
                StandPoint = TargetPos;
            }
            else {
                StandPoint += (TargetPos - StandPoint).SafeNormalize() * MoveSpeed * (NPC.velocity.Y > 0.5f ? 3f : 1f);
            }

            NoMoveTime--;
            float distToMove = 100f * NPC.scale;
            var boss = (Hierophant)NPC.ModNPC;

            if (boss.Jumping) {
                _onTileFlag = false;
                TargetPos = NPC.Center + new Vector2(Offset.X * 0.2f, 200f) * NPC.scale;
                MoveSpeed = Distance(TargetPos, StandPoint) * 0.2f;
                return false;
            }

            float bodyRot = boss.Direction > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi);
            Vector2 idealPos = NPC.Center + NPC.velocity * 16f + (Offset * NPC.scale).RotatedBy(bodyRot);
            float dist = Distance(StandPoint, idealPos);

            if (!OnTile
                || (NoMoveTime <= 0 && dist > distToMove)
                || dist > distToMove * 1.4f) {
                Vector2 searchCenter = idealPos + new Vector2(
                    Math.Sign(NPC.velocity.X) == Math.Sign(Offset.X) ? Math.Sign(NPC.velocity.X) * 12f : 0f, 0f);
                TargetPos = FindStandPoint(searchCenter, 60f * Scale * NPC.scale, 128);
                MoveSpeed = Distance(TargetPos, StandPoint) * 0.2f;
                if (NoMoveTime < 4) NoMoveTime = 4;
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
                Vector2 pos = RandomPointInCircle(maxTry) + center;
                if (Distance(pos, center) <= maxOffset * 0.9f && Hierophant.CanStandOn(pos)) {
                    _onTileFlag = true;
                    Vector2 orgPos = pos;
                    int safety = 128;
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
            return NPC.Center + new Vector2(Offset.X, 200f).RotatedBy(rot);
        }

        #endregion
    }
}
