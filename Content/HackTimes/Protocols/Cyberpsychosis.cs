using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Terraria;

namespace CalamityOverhaul.Content.HackTimes.Protocols
{
    /// <summary>
    /// 赛博精神病：使目标陷入狂暴攻击周围一切单位
    /// </summary>
    internal class Cyberpsychosis : QuickHackDef
    {
        public override void SetDefaults() {
            UploadTime = 150;
            RamCost = 5;
            Category = QuickHackCategory.Control;
        }

        public override int GetDuration() => 60 * 8; //8秒持续

        public override bool OnApply(NPC target, Player caster) {
            //红色精神崩溃爆发
            for (int i = 0; i < 12; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(3.5f, 3.5f);
                PRTLoader.AddParticle(new PRT_Spark(target.Center, vel,
                    false, 30, 1.0f, new Color(255, 30, 30)));
            }
            CombatText.NewText(target.Hitbox, new Color(255, 0, 50), HackTime.Cyberpsychosis.Value, true);
            return true;
        }

        public override bool OnTick(NPC target, int elapsed) {
            //周期性红色故障粒子
            if (elapsed % 10 == 0) {
                Vector2 pos = target.Center + Main.rand.NextVector2Circular(
                    target.width * 0.4f, target.height * 0.4f);
                Vector2 vel = Main.rand.NextVector2Circular(2f, 2f);
                PRTLoader.AddParticle(new PRT_Spark(pos, vel,
                    false, 15, 0.6f, new Color(255, 50, 50)));
            }
            return true;
        }

        public override void OnRemove(NPC target) {
            //精神恢复闪光
            for (int i = 0; i < 5; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(2f, 2f);
                PRTLoader.AddParticle(new PRT_Spark(target.Center, vel,
                    false, 15, 0.5f, new Color(180, 80, 80)));
            }
        }
    }
}
