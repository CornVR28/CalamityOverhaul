using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.GlitchWraith;
using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime.Protocols
{
    /// <summary>
    /// 自我肢解：令灵异目标肢解自己的灵异
    /// </summary>
    internal class SelfDismember : QuickHackDef
    {
        public override void SetDefaults() {
            UploadTime = 360;
            RamCost = 99;
            Category = QuickHackCategory.Paranormal;
            SupportedTargets = HackTargetKind.Wraith;
        }

        public override bool OnApplyToWraith(GlitchWraithActor wraith, Player caster) {
            if (wraith == null) return false;
            wraith.ApplySelfDismember();
            SoundEngine.PlaySound(SoundID.NPCDeath10 with { Volume = 1f, Pitch = -0.9f }, wraith.Center);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 1f, Pitch = -1f }, wraith.Center);
            for (int i = 0; i < 40; i++) {
                Vector2 vel = Main.rand.NextVector2Circular(6f, 6f);
                PRTLoader.AddParticle(new PRT_Spark(wraith.Center, vel,
                    false, 60, 1.2f, new Color(255, 40, 80)));
            }
            return true;
        }
    }
}
