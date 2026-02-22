using System.IO;
using Terraria;
using static CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants.HierophantUtils;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants
{
    /// <summary>
    /// 双节臂结构——第一段(上臂)连接身体，第二段(前臂/刃)连接武器末端。
    /// 提供指向、挥砍两种驱动模式
    /// </summary>
    public class HierophantArm
    {
        public float Seg1RotVelocity;
        public float Seg1Length;
        public float Seg1Rot;
        public float Seg2Rot;
        public float Seg1MaxRadians = MathHelper.ToRadians(50f);
        public Vector2 Offset;
        public NPC Npc;

        /// <summary>
        /// 镰刀刃尖的世界坐标
        /// </summary>
        public Vector2 BladeEnd => Seg1End + Seg2Rot.ToRotationVector2() * 120f * Npc.scale;

        /// <summary>
        /// 第一段(上臂)末端的世界坐标
        /// </summary>
        public Vector2 Seg1End {
            get {
                var boss = (Hierophant)Npc.ModNPC;
                float rot = boss.Direction > 0 ? Npc.rotation : (Npc.rotation + MathHelper.Pi);
                return Npc.Center + (Offset * new Vector2(boss.Direction, 1f) * Npc.scale).RotatedBy(rot)
                       + Seg1Rot.ToRotationVector2() * Seg1Length;
            }
        }

        public HierophantArm(NPC npc, Vector2 offset, float seg1Length, float seg1Rot, float seg2Rot) {
            Npc = npc;
            Offset = offset;
            Seg1Length = seg1Length;
            Seg1Rot = seg1Rot;
            Seg2Rot = seg2Rot;
        }

        #region Drive Modes

        /// <summary>
        /// 缓慢追踪：双段同时朝目标转动
        /// </summary>
        public void PointAt(Vector2 pos) {
            var boss = (Hierophant)Npc.ModNPC;
            float rot = boss.Direction > 0 ? Npc.rotation : (Npc.rotation + MathHelper.Pi);
            Vector2 basePos = Npc.Center + (Offset * new Vector2(boss.Direction, 1f)).RotatedBy(rot);

            Seg1Rot = RotateTowardsAngle(Seg1Rot, (pos - basePos).ToRotation(), 0.06f, false);

            if (GetAngleBetweenVectors(Seg1Rot.ToRotationVector2(), -Vector2.UnitY) > Seg1MaxRadians * 2f) {
                float upper = MathHelper.PiOver2 + Seg1MaxRadians;
                float lower = MathHelper.PiOver2 - Seg1MaxRadians;
                if (Seg1Rot > upper) Seg1Rot = upper;
                if (Seg1Rot < lower) Seg1Rot = lower;
            }

            Seg2Rot = RotateTowardsAngle(Seg2Rot, (pos - Seg1End).ToRotation(), 0.06f, false);
        }

        /// <summary>
        /// 快速挥动：以高速旋转角速度驱动 Seg2 朝目标方向横扫
        /// </summary>
        public void SwingTowards(Vector2 target, float angularSpeed) {
            var boss = (Hierophant)Npc.ModNPC;
            float rot = boss.Direction > 0 ? Npc.rotation : (Npc.rotation + MathHelper.Pi);
            Vector2 basePos = Npc.Center + (Offset * new Vector2(boss.Direction, 1f)).RotatedBy(rot);

            Seg1Rot = RotateTowardsAngle(Seg1Rot, (target - basePos).ToRotation(), 0.12f, false);
            Seg2Rot = RotateTowardsAngle(Seg2Rot, (target - Seg1End).ToRotation(), angularSpeed);
        }

        #endregion

        #region Update

        public void Update() {
            Seg1Rot += Seg1RotVelocity;
            Seg1RotVelocity *= 0.96f;
        }

        #endregion

        #region Networking

        public void NetSend(BinaryWriter writer) {
            writer.Write(Seg1Rot);
            writer.Write(Seg2Rot);
            writer.Write(Seg1RotVelocity);
        }

        public void NetReceive(BinaryReader reader) {
            Seg1Rot = reader.ReadSingle();
            Seg2Rot = reader.ReadSingle();
            Seg1RotVelocity = reader.ReadSingle();
        }

        #endregion
    }
}
