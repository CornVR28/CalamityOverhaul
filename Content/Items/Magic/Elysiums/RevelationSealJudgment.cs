using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    //R技能：七印后三印审判
    internal class RevelationSealJudgment : ModProjectile
    {
        public static LocalizedText Seal5Text { get; private set; }
        public static LocalizedText Seal6Text { get; private set; }
        public static LocalizedText Seal7Text { get; private set; }
        public static LocalizedText WorldJudgmentText { get; private set; }

        public override string Texture => CWRConstant.Placeholder;

        private Player Owner => Main.player[Projectile.owner];
        private ref float Timer => ref Projectile.ai[0];

        private const int Seal5Duration = 60;
        private const int Seal6Duration = 60;
        private const int Seal7Duration = 60;
        private const int FinaleDuration = 50;
        private bool finaleDamaged;

        //屏幕级视觉状态
        private float screenFlashAlpha;
        private float screenDarkenAlpha;
        private Color screenFlashColor = Color.White;

        public override void SetStaticDefaults() {
            Seal5Text = this.GetLocalization(nameof(Seal5Text), () => "第五印");
            Seal6Text = this.GetLocalization(nameof(Seal6Text), () => "第六印");
            Seal7Text = this.GetLocalization(nameof(Seal7Text), () => "第七印");
            WorldJudgmentText = this.GetLocalization(nameof(WorldJudgmentText), () => "世界审判");
        }

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

            //屏幕闪光衰减
            if (screenFlashAlpha > 0f) {
                screenFlashAlpha *= 0.88f;
                if (screenFlashAlpha < 0.01f) screenFlashAlpha = 0f;
            }

            //阶段性压暗：随审判推进越来越暗
            float targetDarken = t <= seal5End ? 0.12f : t <= seal6End ? 0.22f : t <= seal7End ? 0.35f : 0.08f;
            screenDarkenAlpha = MathHelper.Lerp(screenDarkenAlpha, targetDarken, 0.06f);

            if (t == 1) {
                SoundEngine.PlaySound(SoundID.Item62 with { Volume = 1.2f, Pitch = -0.3f }, Projectile.Center);
                CombatText.NewText(Owner.Hitbox, Color.Gold, Seal5Text.Value, true);
                TriggerScreenFlash(new Color(255, 230, 180), 0.5f, 3f);
            }
            else if (t == seal5End + 1) {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 1.2f, Pitch = -0.22f }, Projectile.Center);
                CombatText.NewText(Owner.Hitbox, Color.Orange, Seal6Text.Value, true);
                TriggerScreenFlash(new Color(255, 200, 120), 0.62f, 5f);
            }
            else if (t == seal6End + 1) {
                SoundEngine.PlaySound(SoundID.Item84 with { Volume = 1.25f, Pitch = -0.1f }, Projectile.Center);
                CombatText.NewText(Owner.Hitbox, Color.OrangeRed, Seal7Text.Value, true);
                TriggerScreenFlash(new Color(255, 160, 80), 0.78f, 8f);
            }
            else if (t == seal7End + 1) {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1.45f, Pitch = -0.45f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 1.6f, Pitch = -0.5f }, Projectile.Center);
                CombatText.NewText(Owner.Hitbox, Color.White, WorldJudgmentText.Value, true);
                TriggerScreenFlash(Color.White, 1f, 14f);
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

            //终结阶段闪白回收
            if (t > seal7End + FinaleDuration - 6) {
                screenDarkenAlpha = MathHelper.Lerp(screenDarkenAlpha, 0f, 0.15f);
            }

            if (t >= finaleEnd) {
                ep.DeactivateRevelation(Owner);
                Projectile.Kill();
            }
        }

        private void TriggerScreenFlash(Color color, float intensity, float shake) {
            screenFlashAlpha = intensity;
            screenFlashColor = color;
            if (Owner.TryGetModPlayer<CWRPlayer>(out var cwr)) {
                cwr.GetScreenShake(shake);
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

            //脉冲环额外外围大粒子
            if (t % 10 == 0) {
                int burstCount = t <= seal7End ? 4 : 8;
                float burstR = ringR * 1.15f;
                for (int i = 0; i < burstCount; i++) {
                    float ang = MathHelper.TwoPi * i / burstCount + Main.GlobalTimeWrappedHourly * 2.3f + t * 0.01f;
                    Vector2 pos = Projectile.Center + ang.ToRotationVector2() * burstR;
                    Vector2 radialVel = (pos - Projectile.Center).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2.5f, 6f);
                    Dust d = Dust.NewDustPerfect(pos, dustType, radialVel, 60, default, t <= seal7End ? 1.6f : 2.2f);
                    d.noGravity = true;
                }
            }

            Lighting.AddLight(Projectile.Center, t <= seal7End ? 1f : 1.4f, t <= seal7End ? 0.85f : 1.2f, t <= seal7End ? 0.55f : 0.9f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D glow = CWRAsset.SoftGlow.Value;
            Texture2D pixel = CWRAsset.Placeholder_White.Value;
            Texture2D star = CWRAsset.StarTexture_White.Value;
            if (glow == null || pixel == null) return false;

            SpriteBatch sb = Main.spriteBatch;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;
            int t = (int)Timer;
            int seal5End = Seal5Duration;
            int seal6End = seal5End + Seal6Duration;
            int seal7End = seal6End + Seal7Duration;
            float totalDur = Seal5Duration + Seal6Duration + Seal7Duration + FinaleDuration;
            float p = MathHelper.Clamp(Timer / totalDur, 0f, 1f);
            float pulse = 0.65f + (float)Math.Sin(time * 7f) * 0.22f;
            bool inFinale = t > seal7End;

            //全屏压暗层
            if (screenDarkenAlpha > 0.01f) {
                Color darkenColor = inFinale
                    ? Color.Lerp(new Color(10, 5, 18), new Color(50, 35, 15), MathHelper.Clamp((t - seal7End) / (float)FinaleDuration, 0f, 1f))
                    : new Color(10, 5, 18);
                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                    null, darkenColor * screenDarkenAlpha, 0f, Vector2.Zero, SpriteEffects.None, 0f);
            }

            //全屏边缘暗角（审判进行中的压迫感）
            if (p > 0.08f) {
                float vignetteAlpha = MathHelper.Lerp(0f, 0.3f, p) * pulse;
                Vector2 screenCenter = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
                float maxDim = Math.Max(Main.screenWidth, Main.screenHeight) * 0.55f;
                sb.Draw(glow, screenCenter, null, new Color(0, 0, 0, 220) * vignetteAlpha,
                    0f, glow.Size() * 0.5f, maxDim / (glow.Width * 0.5f), SpriteEffects.None, 0f);
                sb.Draw(glow, screenCenter, null, new Color(0, 0, 0, 0) * (vignetteAlpha * 0.6f),
                    0f, glow.Size() * 0.5f, maxDim * 0.7f / (glow.Width * 0.5f), SpriteEffects.None, 0f);
            }

            //中心审判光柱（随阶段增大）
            float pillarHeight = MathHelper.Lerp(80f, 600f, p) * pulse;
            float pillarWidth = MathHelper.Lerp(12f, 38f, p);
            Color pillarColor = t <= seal5End ? new Color(210, 200, 255, 0) :
                t <= seal6End ? new Color(255, 220, 140, 0) :
                t <= seal7End ? new Color(255, 160, 80, 0) :
                new Color(255, 255, 245, 0);

            sb.Draw(pixel, pos, null, pillarColor * 0.28f * pulse, 0f, new Vector2(0.5f, 0.5f),
                new Vector2(pillarWidth, pillarHeight), SpriteEffects.None, 0f);
            sb.Draw(pixel, pos, null, pillarColor * 0.16f * pulse, 0f, new Vector2(0.5f, 0.5f),
                new Vector2(pillarWidth * 2.2f, pillarHeight * 0.7f), SpriteEffects.None, 0f);

            //阶段性环效果
            float ringR = t <= seal5End ? 220f : t <= seal6End ? 320f : t <= seal7End ? 430f : 560f;
            float ringScale = ringR / (glow.Width * 0.5f);
            float ringPulse = 0.7f + (float)Math.Sin(time * 5.5f + p * 8f) * 0.2f;
            sb.Draw(glow, pos, null, pillarColor * 0.22f * ringPulse, time * 0.4f, glow.Size() * 0.5f,
                new Vector2(ringScale * 1.05f, ringScale * 0.95f), SpriteEffects.None, 0f);
            if (t > seal5End) {
                float innerRingR = ringR * 0.6f;
                float innerRS = innerRingR / (glow.Width * 0.5f);
                sb.Draw(glow, pos, null, pillarColor * 0.15f * ringPulse, -time * 0.7f, glow.Size() * 0.5f,
                    new Vector2(innerRS * 0.95f, innerRS * 1.05f), SpriteEffects.None, 0f);
            }

            //中心核心辉光（大幅放大）
            float coreS1 = MathHelper.Lerp(1.2f, 5.5f, p);
            float coreS2 = MathHelper.Lerp(0.6f, 3.6f, p);
            sb.Draw(glow, pos, null, new Color(255, 210, 130, 0) * 0.52f * pulse, 0f, glow.Size() * 0.5f, coreS1, SpriteEffects.None, 0f);
            sb.Draw(glow, pos, null, new Color(255, 255, 245, 0) * 0.4f * pulse, 0f, glow.Size() * 0.5f, coreS2, SpriteEffects.None, 0f);

            //星形装饰（审判阶段旋转）
            if (star != null && p > 0.15f) {
                float starAlpha = MathHelper.Clamp((p - 0.15f) * 2f, 0f, 1f) * pulse;
                float starScale = MathHelper.Lerp(0.3f, 1.5f, p);
                sb.Draw(star, pos, null, pillarColor * 0.35f * starAlpha, time * 1.8f,
                    star.Size() * 0.5f, starScale, SpriteEffects.None, 0f);
                sb.Draw(star, pos, null, Color.White with { A = 0 } * 0.18f * starAlpha, -time * 2.5f,
                    star.Size() * 0.5f, starScale * 0.65f, SpriteEffects.None, 0f);
            }

            //终结阶段放射线
            if (inFinale) {
                float finaleP = MathHelper.Clamp((t - seal7End) / (float)FinaleDuration, 0f, 1f);
                int rayCount = 12;
                float rayLen = MathHelper.Lerp(200f, 800f, finaleP);
                float rayWidth = MathHelper.Lerp(3f, 10f, finaleP);
                float rayAlpha = (1f - finaleP) * pulse;
                for (int i = 0; i < rayCount; i++) {
                    float ang = MathHelper.TwoPi * i / rayCount + time * 0.6f;
                    Vector2 rayDir = ang.ToRotationVector2();
                    Vector2 rayCenter = pos + rayDir * rayLen * 0.5f;
                    sb.Draw(pixel, rayCenter, null, new Color(255, 245, 220, 0) * 0.22f * rayAlpha,
                        ang, new Vector2(0.5f, 0.5f), new Vector2(rayLen, rayWidth), SpriteEffects.None, 0f);
                }
            }

            //全屏闪光层（在最后绘制以覆盖一切）
            if (screenFlashAlpha > 0.01f) {
                sb.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                    null, screenFlashColor with { A = 0 } * screenFlashAlpha, 0f, Vector2.Zero, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
