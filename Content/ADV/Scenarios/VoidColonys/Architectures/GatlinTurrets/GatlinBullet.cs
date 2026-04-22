using CalamityOverhaul.Common;
using InnoVault.Trails;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.GatlinTurrets
{
    /// <summary>
    /// 加特林炮台发射的敌意子弹
    /// 使用GradientTrail着色器驱动的顶点拖尾，配合SoftGlow/StarTexture/LightShot加性叠绘制造炽热穿甲曳光观感
    /// 撞击物块或命中玩家时生成烟雾Gore、火花Dust与石屑Dust，强化互动反馈
    /// </summary>
    internal class GatlinBullet : ModProjectile, IPrimitiveDrawable
    {
        //占位贴图，本类所有绘制都走PreDraw/DrawPrimitives自绘
        public override string Texture => CWRConstant.Placeholder;

        private Trail trail;
        //初始速度缓存，OnKill超时消亡时仍有方向可用来生成碎屑
        private Vector2 initialVelocity;
        //OnTileCollide已触发过爆点，避免OnKill再重复一次
        private bool impactResolved;

        public override void SetStaticDefaults() {
            //大量采样点覆盖高extraUpdate下的子弹间隔，TrailingMode=2保证每次子更新都记录
            ProjectileID.Sets.TrailCacheLength[Type] = 60;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 3;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void OnSpawn(IEntitySource source) {
            initialVelocity = Projectile.velocity;
        }

        public override void AI() {
            Projectile.ai[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (initialVelocity == Vector2.Zero) {
                initialVelocity = Projectile.velocity;
            }

            //沿途暖黄光照，配合过去时代昏暗色调增强可读性
            Lighting.AddLight(Projectile.Center, 0.85f, 0.55f, 0.18f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            SpawnImpactEffects(Projectile.Center, Projectile.velocity);
            impactResolved = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            SpawnImpactEffects(Projectile.Center, oldVelocity);
            impactResolved = true;
            return true;
        }

        public override void OnKill(int timeLeft) {
            if (!impactResolved) {
                SpawnImpactEffects(Projectile.Center, initialVelocity);
            }
        }

        /// <summary>
        /// 命中爆点：烟雾Gore、橙红火花Dust、灰色石屑Dust与命中音效
        /// </summary>
        private static void SpawnImpactEffects(Vector2 pos, Vector2 hitVelocity) {
            if (Main.dedServ) return;

            Vector2 normal = hitVelocity.SafeNormalize(Vector2.UnitX);

            //蘑菇云状烟雾Gore
            int smokeCount = Main.rand.Next(2, 4);
            for (int i = 0; i < smokeCount; i++) {
                Vector2 goreVel = -normal * Main.rand.NextFloat(0.6f, 1.8f)
                    + Main.rand.NextVector2Circular(1.6f, 1.6f) - Vector2.UnitY * 0.4f;
                int gore = Gore.NewGore(new EntitySource_WorldEvent("GatlinBullet_Impact"),
                    pos, goreVel, GoreID.Smoke1 + Main.rand.Next(3), Main.rand.NextFloat(0.7f, 1.05f));
                if (gore >= 0 && gore < Main.maxGore) {
                    Main.gore[gore].alpha = 60;
                }
            }

            //橙红火花碎屑Dust沿反射方向锥形迸发
            for (int i = 0; i < 14; i++) {
                Vector2 spread = (-normal).RotatedByRandom(0.9f) * Main.rand.NextFloat(2f, 6f);
                Dust d = Dust.NewDustPerfect(pos, DustID.Torch, spread, 0, default, Main.rand.NextFloat(1.1f, 1.6f));
                d.noGravity = Main.rand.NextBool(3);
            }
            //灰色石屑Dust模拟被击中物块的碎片
            for (int i = 0; i < 8; i++) {
                Vector2 spread = (-normal).RotatedByRandom(1.2f) * Main.rand.NextFloat(1f, 3.5f);
                Dust d = Dust.NewDustPerfect(pos, DustID.Stone, spread, 80, default, Main.rand.NextFloat(0.9f, 1.3f));
                d.velocity.Y -= 0.4f;
            }
            //核心闪光
            Dust flash = Dust.NewDustPerfect(pos, DustID.GoldFlame, Vector2.Zero, 0, Color.White, 1.8f);
            flash.noGravity = true;

            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.35f, Pitch = 0.35f, PitchVariance = 0.2f }, pos);
        }

        public float GetTrailWidth(float completionRatio) {
            //头端粗尾端收敛，平方衰减让能量集中在弹头附近
            float taper = 1f - completionRatio;
            return 27.5f * taper * taper + 1f;
        }

        public Color GetTrailColor(Vector2 completionRatio) {
            float t = completionRatio.X;
            //白炽→橙红→烟灰的曳光弹渐变
            Color hot = Color.Lerp(new Color(255, 245, 200), new Color(255, 140, 40), MathHelper.Clamp(t * 1.4f, 0f, 1f));
            Color cool = Color.Lerp(hot, new Color(70, 30, 15), MathHelper.Clamp((t - 0.4f) * 1.6f, 0f, 1f));
            float fade = 1f - t;
            return cool * fade;
        }

        void IPrimitiveDrawable.DrawPrimitives() {
            if (Projectile.oldPos == null || Projectile.oldPos.Length == 0 || Projectile.ai[0] < 16) return;

            //拼装拖尾路径：零向量位置用中心替代避免开局跳回(0,0)拉出横线
            Vector2[] positions = new Vector2[Projectile.oldPos.Length];
            for (int i = 0; i < Projectile.oldPos.Length; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    Projectile.oldPos[i] = Projectile.Center;
                }
                positions[i] = Projectile.oldPos[i] + Projectile.Size * 0.5f;
            }

            trail ??= new Trail(positions, GetTrailWidth, GetTrailColor);
            trail.TrailPositions = positions;

            Effect effect = EffectLoader.GradientTrail?.Value;
            if (effect == null) return;
            effect.Parameters["transformMatrix"]?.SetValue(VaultUtils.GetTransfromMatrix());
            effect.Parameters["uTime"]?.SetValue((float)Main.timeForVisualEffects * 0.12f);
            effect.Parameters["uTimeG"]?.SetValue(Main.GlobalTimeWrappedHourly * 0.35f);
            effect.Parameters["udissolveS"]?.SetValue(1f);
            effect.Parameters["uBaseImage"]?.SetValue(CWRAsset.Fire.Value);
            effect.Parameters["uFlow"]?.SetValue(CWRAsset.SoftGlow.Value);
            //橙黄到深红的炽热曳光色带
            effect.Parameters["uGradient"]?.SetValue(CWRUtils.GetT2DValue(CWRConstant.ColorBar + "BurntSienna_Bar"));
            effect.Parameters["uDissolve"]?.SetValue(CWRAsset.SoftGlow.Value);

            Main.graphics.GraphicsDevice.BlendState = BlendState.Additive;
            trail.DrawTrail(effect);
            Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        }

        public override bool PreDraw(ref Color lightColor) {
            if (Projectile.ai[0] == 0) {
                return false;
            }

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Texture2D glow = CWRAsset.SoftGlow.Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            //仅保留贴合弹头的小白炽亮点，不再叠加星芒与光束，避免淹没着色器拖尾
            Vector2 glowOrigin = glow.Size() * 0.5f;
            Main.spriteBatch.Draw(glow, drawPos, null, new Color(255, 230, 170, 0) * 0.8f,
                Projectile.rotation, glowOrigin, new Vector2(0.22f, 0.12f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glow, drawPos, null, new Color(255, 255, 230, 0) * 0.9f,
                0f, glowOrigin, 0.08f, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
