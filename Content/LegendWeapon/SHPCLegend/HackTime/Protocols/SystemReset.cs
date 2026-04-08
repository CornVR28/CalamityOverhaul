using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Terraria;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime.Protocols
{
    /// <summary>
    /// 系统重启：强制目标系统重启导致长时间晕眩
    /// </summary>
    internal class SystemReset : QuickHackDef
    {
        public override void SetDefaults() {
            UploadTime = 120;
            RamCost = 4;
            Category = QuickHackCategory.Control;
        }

        public override int GetDuration() => 60 * 6; //6秒晕眩

        public override bool OnApply(NPC target, Player caster) {
            //系统关机蓝屏粒子
            for (int i = 0; i < 12; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(3f, 3f);
                PRTLoader.AddParticle(new PRT_Spark(target.Center, vel,
                    false, 25, 1.0f, new Color(40, 120, 255)));
            }
            CombatText.NewText(target.Hitbox, new Color(40, 150, 255), "REBOOTING...", true);
            return true;
        }

        public override bool OnTick(NPC target, int elapsed) {
            //向上飘散的数据流粒子
            if (elapsed % 8 == 0) {
                Vector2 pos = new(
                    target.Center.X + Main.rand.NextFloat(-target.width * 0.4f, target.width * 0.4f),
                    target.position.Y + target.height);
                Vector2 vel = new(0, Main.rand.NextFloat(-2f, -0.5f));
                PRTLoader.AddParticle(new PRT_Spark(pos, vel,
                    false, 30, 0.5f, new Color(40, 100, 255)));
            }
            //偶尔闪烁蓝色光点
            if (elapsed % 30 == 0) {
                PRTLoader.AddParticle(new PRT_Spark(
                    target.Center + Main.rand.NextVector2Circular(target.width * 0.3f, target.height * 0.3f),
                    Vector2.Zero, false, 10, 1.2f, new Color(80, 160, 255)));
            }
            return true;
        }

        public override void OnRemove(NPC target) {
            //重启完成闪光
            for (int i = 0; i < 6; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(2.5f, 2.5f);
                PRTLoader.AddParticle(new PRT_Spark(target.Center, vel,
                    false, 12, 0.8f, new Color(100, 200, 255)));
            }
            CombatText.NewText(target.Hitbox, new Color(100, 200, 255), "SYSTEM ONLINE", false);
        }
    }
}
