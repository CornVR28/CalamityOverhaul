using Terraria;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants
{
    /// <summary>
    /// 纯粹的视觉效果工厂——粒子、震屏等，不含任何逻辑判定
    /// </summary>
    internal static class HierophantEffects
    {
        public static void CameraShake(Vector2 center, float strength) {
            float s = Utils.Remap(Main.LocalPlayer.Distance(center), 4000f, 800f, 0f, strength);
            if (s <= 0f) return;
            var shake = new PunchCameraModifier(center, Main.rand.NextVector2Unit(), s, 6f, 10);
            Main.instance.CameraModifiers.Add(shake);
        }

        public static void DeathTickDust(NPC npc) {
            for (int i = 0; i < 3; i++) {
                Dust d = Dust.NewDustPerfect(
                    npc.Center + HierophantUtils.RandomPointInCircle(40f * npc.scale),
                    DustID.Electric,
                    HierophantUtils.RandomPointInCircle(8f),
                    Scale: Main.rand.NextFloat(1f, 2f));
                d.noGravity = true;
            }
        }

        public static void SlashTrailDust(NPC npc, HierophantArm arm) {
            if (Main.dedServ || Main.GameUpdateCount % 2 != 0) return;
            Dust d = Dust.NewDustPerfect(
                arm.BladeEnd, DustID.Wraith,
                (arm.BladeEnd - npc.Center).SafeNormalize(Vector2.Zero).RotatedByRandom(0.5f) * 4f,
                Alpha: 120, Scale: 1.5f * npc.scale);
            d.noGravity = true;
        }

        public static void SlamImpactDust(NPC npc) {
            if (Main.dedServ) return;
            for (int i = 0; i < 30; i++) {
                Vector2 dustVel = new(Main.rand.NextFloat(-12f, 12f), Main.rand.NextFloat(-6f, -1f));
                Dust d = Dust.NewDustPerfect(
                    npc.Bottom + new Vector2(Main.rand.NextFloat(-80f, 80f) * npc.scale, 0f),
                    DustID.Smoke, dustVel, Alpha: 100,
                    Scale: Main.rand.NextFloat(2f, 4f) * npc.scale);
                d.noGravity = true;
            }
            for (int i = 0; i < 16; i++) {
                Dust d2 = Dust.NewDustPerfect(
                    npc.Bottom + new Vector2(Main.rand.NextFloat(-60f, 60f) * npc.scale, 0f),
                    DustID.Electric,
                    new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-8f, -2f)),
                    Scale: Main.rand.NextFloat(1f, 2.5f));
                d2.noGravity = true;
            }
        }

        public static void DeathExplosionDust(NPC npc) {
            if (Main.dedServ) return;
            for (int i = 0; i < 40; i++) {
                Dust dust = Dust.NewDustPerfect(
                    npc.Center + HierophantUtils.RandomPointInCircle(60f * npc.scale),
                    DustID.Torch, HierophantUtils.RandomPointInCircle(32f * npc.scale));
                dust.scale = Main.rand.NextFloat(1f, 4f) * npc.scale;
                dust.noGravity = true;
            }
            for (int i = 0; i < 10; i++) {
                Gore.NewGore(npc.GetSource_Death(),
                    npc.Center + HierophantUtils.RandomPointInCircle(46f),
                    HierophantUtils.RandomPointInCircle(16f),
                    Main.rand.Next(61, 64), npc.scale);
            }
        }

        public static void OnKillEffects(NPC npc) {
            if (Main.dedServ) return;
            for (int i = 0; i < 60; i++) {
                Dust d = Dust.NewDustPerfect(
                    npc.Center + HierophantUtils.RandomPointInCircle(80f * npc.scale),
                    DustID.Torch, HierophantUtils.RandomPointInCircle(20f),
                    Scale: Main.rand.NextFloat(1.5f, 4f));
                d.noGravity = true;
            }
            for (int i = 0; i < 20; i++) {
                Gore.NewGore(npc.GetSource_Death(),
                    npc.Center + HierophantUtils.RandomPointInCircle(40f),
                    HierophantUtils.RandomPointInCircle(8f),
                    Main.rand.Next(61, 64), npc.scale);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath14, npc.Center);
        }
    }
}
