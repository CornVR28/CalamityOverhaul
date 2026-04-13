using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    //R技能：七印后三印审判
    internal class RevelationSealJudgment : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        private Player Owner => Main.player[Projectile.owner];
        private ref float Timer => ref Projectile.ai[0];

        private const int Seal5Duration = 60;
        private const int Seal6Duration = 60;
        private const int Seal7Duration = 60;
        private const int FinaleDuration = 50;
        private bool finaleDamaged;

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = Seal5Duration + Seal6Duration + Seal7Duration + FinaleDuration + 20;
        }

        public override bool? CanDamage() => false;

        public override void AI() {
            if (!Owner.active || Owner.dead) {
                Projectile.Kill();
                return;
            }

            if (!Owner.TryGetModPlayer<ElysiumPlayer>(out var ep) || !ep.IsRevelationActive) {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Owner.Center;
            Timer++;

            int t = (int)Timer;
            int seal5End = Seal5Duration;
            int seal6End = seal5End + Seal6Duration;
            int seal7End = seal6End + Seal7Duration;
            int finaleEnd = seal7End + FinaleDuration;

            if (t == 1) {
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = 1.2f, Pitch = -0.3f }, Projectile.Center);
                CombatText.NewText(Owner.Hitbox, Color.Gold, "第五印", true);
            }
            else if (t == seal5End + 1) {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 1.2f, Pitch = -0.22f }, Projectile.Center);
                CombatText.NewText(Owner.Hitbox, Color.Orange, "第六印", true);
            }
            else if (t == seal6End + 1) {
                SoundEngine.PlaySound(SoundID.Item84 with { Volume = 1.25f, Pitch = -0.1f }, Projectile.Center);
                CombatText.NewText(Owner.Hitbox, Color.OrangeRed, "第七印", true);
            }
            else if (t == seal7End + 1) {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1.45f, Pitch = -0.45f }, Projectile.Center);
                CombatText.NewText(Owner.Hitbox, Color.White, "世界审判", true);
            }

            //阶段伤害脉冲
            if (t % 20 == 0 && t < seal7End) {
                float pulseRadius = t <= seal5End ? 520f : t <= seal6End ? 700f : 900f;
                int damage = (int)(Projectile.damage * (t <= seal5End ? 0.5f : t <= seal6End ? 0.7f : 1f));
                PulseDamage(pulseRadius, damage, false);
            }

            //终结伤害
            if (!finaleDamaged && t >= seal7End + 8) {
                finaleDamaged = true;
                PulseDamage(1300f, (int)(Projectile.damage * (ep.HasDeathAmplification() ? 3.2f : 2.6f)), true);
            }

            SpawnPhaseDust(t, seal5End, seal6End, seal7End);

            if (t >= finaleEnd) {
                ep.DeactivateRevelation(Owner);
                Projectile.Kill();
            }
        }

        private void PulseDamage(float radius, int damage, bool crit) {
            foreach (NPC npc in Main.npc) {
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                if (Vector2.Distance(npc.Center, Projectile.Center) <= radius) {
                    Owner.ApplyDamageToNPC(npc, damage, 12f, 0, crit);
                }
            }
        }

        private void SpawnPhaseDust(int t, int seal5End, int seal6End, int seal7End) {
            int dustType = t <= seal5End ? DustID.SilverFlame : t <= seal6End ? DustID.GoldFlame : t <= seal7End ? DustID.Torch : DustID.WhiteTorch;
            float ringR = t <= seal5End ? 220f : t <= seal6End ? 320f : t <= seal7End ? 430f : 560f;
            int count = t <= seal7End ? 10 : 18;

            for (int i = 0; i < count; i++) {
                float ang = MathHelper.TwoPi * i / count + Main.GlobalTimeWrappedHourly * (t <= seal7End ? 1.5f : 3f);
                Vector2 pos = Projectile.Center + ang.ToRotationVector2() * ringR;
                Vector2 vel = (Projectile.Center - pos).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.5f, 4f);
                Dust d = Dust.NewDustPerfect(pos, dustType, vel, 80, default, t <= seal7End ? 1.25f : 1.7f);
                d.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, t <= seal7End ? 1f : 1.4f, t <= seal7End ? 0.85f : 1.2f, t <= seal7End ? 0.55f : 0.9f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D glow = CWRAsset.SoftGlow.Value;
            if (glow == null) return false;

            SpriteBatch sb = Main.spriteBatch;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float p = MathHelper.Clamp(Timer / (Seal5Duration + Seal6Duration + Seal7Duration + FinaleDuration), 0f, 1f);

            float s1 = MathHelper.Lerp(0.5f, 3.2f, p);
            float s2 = MathHelper.Lerp(0.25f, 2.2f, p);
            float pulse = 0.65f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7f) * 0.22f;

            sb.Draw(glow, pos, null, new Color(255, 210, 130, 0) * 0.45f * pulse, 0f, glow.Size() * 0.5f, s1, SpriteEffects.None, 0f);
            sb.Draw(glow, pos, null, new Color(255, 255, 245, 0) * 0.35f * pulse, 0f, glow.Size() * 0.5f, s2, SpriteEffects.None, 0f);
            return false;
        }
    }
}
