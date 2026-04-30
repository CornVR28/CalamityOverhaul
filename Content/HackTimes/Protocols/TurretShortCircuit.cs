using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Terraria;

namespace CalamityOverhaul.Content.HackTimes.Protocols
{
    /// <summary>
    /// 炮台电路短路：瞬间放电令炮台短暂停摆
    /// 适合扰乱炮台射击节奏，恢复期较短但消耗也低
    /// </summary>
    internal class TurretShortCircuit : QuickHackDef
    {
        //失效帧数（大约4秒）
        private const int DisableFrames = 60 * 4;

        public override void SetDefaults() {
            UploadTime = 90;
            RamCost = 3;
            Category = QuickHackCategory.TileManip;
            SupportedTargets = HackTargetKind.Turret;
        }

        public override bool OnApply(IHackTarget target, Player caster) {
            if (target is not IHackableTurret turret) return false;
            turret.ApplyShortCircuit(DisableFrames, caster);
            Vector2 center = turret.WorldCenter;

            if (!VaultUtils.isServer) {
                //电弧火花爆闪
                for (int i = 0; i < 22; i++) {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(6f, 6f);
                    Color c = Color.Lerp(new Color(120, 200, 255), new Color(220, 240, 255), Main.rand.NextFloat());
                    PRTLoader.AddParticle(new PRT_Spark(center, vel, false, 22, 1.0f, c));
                }
                //沿外圈再撒一圈冷色粒子
                for (int i = 0; i < 10; i++) {
                    float angle = MathHelper.TwoPi * i / 10f;
                    Vector2 dir = angle.ToRotationVector2();
                    PRTLoader.AddParticle(new PRT_Spark(center + dir * 24f, dir * 3.5f,
                        false, 22, 0.55f, new Color(90, 180, 255, 120)));
                }
            }
            return true;
        }
    }
}
