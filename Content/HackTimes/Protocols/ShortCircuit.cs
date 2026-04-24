using CalamityOverhaul.Common;
using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Terraria;
using Terraria.Audio;

namespace CalamityOverhaul.Content.HackTimes.Protocols
{
    /// <summary>
    /// 短路：释放电磁脉冲造成即时伤害并短暂麻痹
    /// </summary>
    internal class ShortCircuit : QuickHackDef
    {
        public override void SetDefaults() {
            UploadTime = 60;
            RamCost = 2;
            Category = QuickHackCategory.Lethal;
        }

        public override bool OnApply(NPC target, Player caster) {
            //即时重击
            target.SimpleStrikeNPC(300, 0, false, 0f, null, false, 0f, true);
            //电弧爆发粒子
            for (int i = 0; i < 15; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(6f, 6f);
                PRTLoader.AddParticle(new PRT_Spark(target.Center, vel,
                    false, 15, 1.5f, new Color(100, 180, 255)));
            }
            //内层白色核心闪光
            for (int i = 0; i < 6; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(2f, 2f);
                PRTLoader.AddParticle(new PRT_Spark(target.Center, vel,
                    false, 8, 2.0f, Color.White));
            }
            if (!VaultUtils.isServer) {
                SoundEngine.PlaySound(CWRSound.ShortCircuit, target.Center);
            }
            return true;
        }
    }
}
