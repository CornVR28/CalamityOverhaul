using CalamityOverhaul.Content.Industrials;
using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using InnoVault.TileProcessors;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.HackTimes.Protocols
{
    /// <summary>
    /// 机械短路协议：瞬间清空目标机械的全部电能，并释放电弧伤害周围实体
    /// </summary>
    internal class MachineShortCircuit : QuickHackDef
    {
        //电弧伤害半径（像素）
        private const float ArcRadius = 160f;
        //电弧伤害值
        private const int ArcDamage = 40;

        public override void SetDefaults() {
            UploadTime = 80;
            RamCost = 3;
            Category = QuickHackCategory.TileManip;
            SupportedTargets = HackTargetKind.Tile;
        }

        public override bool CanApplyTo(IHackTarget target) {
            if (!base.CanApplyTo(target)) return false;
            if (target is not TileScannable s) return false;
            return TryGetMachine(s.TileCoordX, s.TileCoordY, out _);
        }

        public override bool OnApply(IHackTarget target, Player caster) {
            if (target is not TileScannable s) return false;
            if (!TryGetMachine(s.TileCoordX, s.TileCoordY, out MachineTP machine)) return false;

            //清空电能
            float drainedUE = machine.MachineData.UEvalue;
            machine.MachineData.UEvalue = 0;

            Vector2 center = machine.CenterInWorld;

            //电弧范围伤害
            if (!VaultUtils.isClient) {
                for (int i = 0; i < Main.maxNPCs; i++) {
                    NPC npc = Main.npc[i];
                    if (!npc.active) continue;
                    if (Vector2.Distance(npc.Center, center) > ArcRadius) continue;
                    npc.StrikeNPC(new NPC.HitInfo {
                        Damage = ArcDamage,
                        Knockback = 4f,
                        HitDirection = npc.Center.X > center.X ? 1 : -1,
                        Crit = false,
                    });
                }
            }

            //视觉效果：电火花粒子
            if (!VaultUtils.isServer) {
                for (int i = 0; i < 20; i++) {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(7f, 7f);
                    Color c = Color.Lerp(new Color(100, 180, 255), new Color(200, 220, 255), Main.rand.NextFloat());
                    PRTLoader.AddParticle(new PRT_Spark(center, vel, false, 25, 1.2f, c));
                }
                //外圈扩散粒子
                for (int i = 0; i < 12; i++) {
                    float angle = MathHelper.TwoPi * i / 12f;
                    Vector2 dir = angle.ToRotationVector2();
                    PRTLoader.AddParticle(new PRT_Spark(center + dir * 20f, dir * 4f, false, 20, 0.6f,
                        new Color(80, 200, 255, 120)));
                }

                SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.7f, Pitch = -0.3f }, center);
            }

            //网络同步物块状态
            if (Main.netMode != NetmodeID.SinglePlayer) {
                int tileW = machine.Width / 16;
                int tileH = machine.Height / 16;
                NetMessage.SendTileSquare(-1, machine.Position.X, machine.Position.Y, tileW, tileH);
            }

            return true;
        }

        /// <summary>
        /// 尝试从物块坐标获取对应的MachineTP
        /// </summary>
        private static bool TryGetMachine(int tileX, int tileY, out MachineTP machine) {
            machine = null;
            if (!VaultUtils.SafeGetTopLeft(tileX, tileY, out var topLeft)) return false;
            if (!TileProcessorLoader.TP_Point_To_Instance.TryGetValue(topLeft, out var tp)) return false;
            if (tp is not MachineTP m || !tp.Active) return false;
            machine = m;
            return true;
        }
    }
}
