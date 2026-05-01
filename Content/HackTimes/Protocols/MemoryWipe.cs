using CalamityOverhaul.Content.HackTimes.Scannables;
using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Terraria;

namespace CalamityOverhaul.Content.HackTimes.Protocols
{
    /// <summary>
    /// 记忆清除：抹除目标短期记忆使其失去仇恨
    /// </summary>
    internal class MemoryWipe : QuickHackDef
    {
        public override void SetDefaults() {
            UploadTime = 80;
            RamCost = 3;
            Category = QuickHackCategory.Covert;
        }

        public override int GetDuration() => 60 * 5; //5秒失忆

        public override bool OnApply(IHackTarget target, Player caster) {
            if (target is not NpcScannable s) return false;
            NPC npc = Main.npc[s.NpcIndex];
            //数据溶解粒子——向上飘散的数据碎片
            for (int i = 0; i < 10; i++) {
                Vector2 pos = npc.Center + Main.rand.NextVector2Circular(
                    npc.width * 0.3f, npc.height * 0.3f);
                Vector2 vel = new(0, Main.rand.NextFloat(-1.5f, -0.3f));
                PRTLoader.AddParticle(new PRT_Spark(pos, vel,
                    false, 30, 0.7f, new Color(50, 255, 180)));
            }
            CombatText.NewText(npc.Hitbox, new Color(80, 255, 200), HackTime.MemoryWiped.Value, true);
            return true;
        }

        public override bool OnTick(IHackTarget target, int elapsed) {
            if (target is not NpcScannable s) return true;
            NPC npc = Main.npc[s.NpcIndex];
            //持续数据碎片升腾
            if (elapsed % 12 == 0) {
                Vector2 pos = npc.Center + Main.rand.NextVector2Circular(
                    npc.width * 0.4f, npc.height * 0.4f);
                Vector2 vel = new(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1f, -0.2f));
                PRTLoader.AddParticle(new PRT_Spark(pos, vel,
                    false, 20, 0.4f, new Color(0, 220, 150)));
            }
            return true;
        }

        public override void OnRemove(IHackTarget target) {
            if (target is not NpcScannable s) return;
            NPC npc = Main.npc[s.NpcIndex];
            //记忆恢复粒子
            for (int i = 0; i < 4; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1.5f, 1.5f);
                PRTLoader.AddParticle(new PRT_Spark(npc.Center, vel,
                    false, 15, 0.5f, new Color(0, 180, 120)));
            }
        }
    }
}
