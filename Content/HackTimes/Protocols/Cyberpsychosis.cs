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

        public override bool OnApply(IHackTarget target, Player caster) {
            if (target is not NpcScannable s) return false;
            NPC npc = Main.npc[s.NpcIndex];
            //红色精神崩溃爆发
            for (int i = 0; i < 12; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(3.5f, 3.5f);
                PRTLoader.AddParticle(new PRT_Spark(npc.Center, vel,
                    false, 30, 1.0f, new Color(255, 30, 30)));
            }
            CombatText.NewText(npc.Hitbox, new Color(255, 0, 50), HackTime.Cyberpsychosis.Value, true);
            return true;
        }

        public override bool OnTick(IHackTarget target, int elapsed) {
            if (target is not NpcScannable s) return true;
            NPC npc = Main.npc[s.NpcIndex];
            //周期性红色故障粒子
            if (elapsed % 10 == 0) {
                Vector2 pos = npc.Center + Main.rand.NextVector2Circular(
                    npc.width * 0.4f, npc.height * 0.4f);
                Vector2 vel = Main.rand.NextVector2Circular(2f, 2f);
                PRTLoader.AddParticle(new PRT_Spark(pos, vel,
                    false, 15, 0.6f, new Color(255, 50, 50)));
            }
            return true;
        }

        public override void OnRemove(IHackTarget target) {
            if (target is not NpcScannable s) return;
            NPC npc = Main.npc[s.NpcIndex];
            //精神恢复闪光
            for (int i = 0; i < 5; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(2f, 2f);
                PRTLoader.AddParticle(new PRT_Spark(npc.Center, vel,
                    false, 15, 0.5f, new Color(180, 80, 80)));
            }
        }
    }
}
