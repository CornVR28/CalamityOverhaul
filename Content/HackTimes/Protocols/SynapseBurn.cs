using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Terraria;

namespace CalamityOverhaul.Content.HackTimes.Protocols
{
    /// <summary>
    /// 突触焚毁：对目标神经系统造成持续热伤害
    /// </summary>
    internal class SynapseBurn : QuickHackDef
    {
        public override void SetDefaults() {
            UploadTime = 90;
            RamCost = 3;
            Category = QuickHackCategory.Lethal;
        }

        public override int GetDuration() => 60 * 5; //5秒持续

        public override bool OnApply(NPC target, Player caster) {
            //初始神经脉冲爆发
            for (int i = 0; i < 8; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(4f, 4f);
                PRTLoader.AddParticle(new PRT_Spark(target.Center, vel, false, 25, 1.2f,
                    new Color(255, 120, 20)));
            }
            return true;
        }

        public override bool OnTick(NPC target, int elapsed) {
            //每15帧造成一次伤害（4次/秒，每次25，5秒共500）
            if (elapsed % 15 == 0) {
                target.SimpleStrikeNPC(25, 0, false, 0f, null, false, 0f, true);
            }
            //持续焚烧粒子
            if (elapsed % 3 == 0) {
                Vector2 pos = target.Center + Main.rand.NextVector2Circular(
                    target.width * 0.3f, target.height * 0.3f);
                Vector2 vel = new(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -0.5f));
                Color c = Color.Lerp(new Color(255, 80, 0), new Color(255, 200, 50), Main.rand.NextFloat());
                PRTLoader.AddParticle(new PRT_Spark(pos, vel, false, 20, 0.8f, c));
            }
            return true;
        }
    }
}
