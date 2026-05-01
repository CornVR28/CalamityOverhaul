using CalamityOverhaul.Content.HackTimes.Scannables;
using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Terraria;

namespace CalamityOverhaul.Content.HackTimes.Protocols
{
    /// <summary>
    /// 视觉过载：过载目标光学设备使其致盲
    /// </summary>
    internal class OpticOverload : QuickHackDef
    {
        public override void SetDefaults() {
            UploadTime = 75;
            RamCost = 2;
            Category = QuickHackCategory.Covert;
        }

        public override int GetDuration() => 60 * 4; //4秒致盲

        public override bool OnApply(IHackTarget target, Player caster) {
            if (target is not NpcScannable s) return false;
            NPC npc = Main.npc[s.NpcIndex];
            //明亮白色闪光爆发
            for (int i = 0; i < 12; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(5f, 5f);
                PRTLoader.AddParticle(new PRT_Spark(npc.Center, vel,
                    false, 15, 1.5f, Color.White));
            }
            //核心强光
            for (int i = 0; i < 4; i++) {
                PRTLoader.AddParticle(new PRT_Spark(npc.Center,
                    Main.rand.NextVector2Circular(1f, 1f),
                    false, 10, 2.5f, new Color(255, 255, 220)));
            }
            return true;
        }

        public override bool OnTick(IHackTarget target, int elapsed) {
            if (target is not NpcScannable s) return true;
            NPC npc = Main.npc[s.NpcIndex];
            //周期性闪烁表示仍在致盲
            if (elapsed % 20 == 0) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(3f, 3f);
                PRTLoader.AddParticle(new PRT_Spark(npc.Center, vel,
                    false, 10, 1.0f, new Color(255, 255, 220)));
            }
            return true;
        }

        public override void OnRemove(IHackTarget target) {
            if (target is not NpcScannable s) return;
            NPC npc = Main.npc[s.NpcIndex];
            //视觉恢复衰减光
            for (int i = 0; i < 4; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1.5f, 1.5f);
                PRTLoader.AddParticle(new PRT_Spark(npc.Center, vel,
                    false, 12, 0.6f, new Color(200, 200, 160)));
            }
        }
    }
}
