using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Terraria;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime.Protocols
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

        public override bool OnApply(NPC target, Player caster) {
            //数据溶解粒子——向上飘散的数据碎片
            for (int i = 0; i < 10; i++) {
                Vector2 pos = target.Center + Main.rand.NextVector2Circular(
                    target.width * 0.3f, target.height * 0.3f);
                Vector2 vel = new(0, Main.rand.NextFloat(-1.5f, -0.3f));
                PRTLoader.AddParticle(new PRT_Spark(pos, vel,
                    false, 30, 0.7f, new Color(50, 255, 180)));
            }
            CombatText.NewText(target.Hitbox, new Color(80, 255, 200), HackTheme.Text("MemoryWiped"), true);
            return true;
        }

        public override bool OnTick(NPC target, int elapsed) {
            //持续数据碎片升腾
            if (elapsed % 12 == 0) {
                Vector2 pos = target.Center + Main.rand.NextVector2Circular(
                    target.width * 0.4f, target.height * 0.4f);
                Vector2 vel = new(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1f, -0.2f));
                PRTLoader.AddParticle(new PRT_Spark(pos, vel,
                    false, 20, 0.4f, new Color(0, 220, 150)));
            }
            return true;
        }

        public override void OnRemove(NPC target) {
            //记忆恢复粒子
            for (int i = 0; i < 4; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1.5f, 1.5f);
                PRTLoader.AddParticle(new PRT_Spark(target.Center, vel,
                    false, 15, 0.5f, new Color(0, 180, 120)));
            }
        }
    }
}
