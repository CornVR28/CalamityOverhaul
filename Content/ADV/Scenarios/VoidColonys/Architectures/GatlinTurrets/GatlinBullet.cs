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
    /// 拖尾采用自研 GatlinTracer 着色器，三层结构实现白炽核心+等离子鞘+飞散微丝的 HDR 观感
    /// 命中物块/玩家时生成 GatlinImpactBurst 弹幕播放自研 GatlinImpactBurst 爆点着色器
    /// </summary>
    internal class GatlinBullet : ModProjectile, IPrimitiveDrawable
    {
        //占位贴图，本类所有绘制都走 PreDraw/DrawPrimitives 自绘
        public override string Texture => CWRConstant.Placeholder;

        private Trail trail;
        //初始速度缓存，超时消亡时仍有方向用来生成爆点
        private Vector2 initialVelocity;
        //是否已触发过爆点，避免 OnKill 重复
        private bool impactResolved;

        public override void SetStaticDefaults() {
            //高 extraUpdate 下 TrailingMode=2 保证每次子更新都采样
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
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
            //高 extraUpdate 下 TrailingMode=2 保证每次子更新都采样
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void OnSpawn(IEntitySource source) {
            initialVelocity = Projectile.velocity;
        }

        public override void AI() {
            //ai[0] 用作拖尾点填满的 tick 计数（含子更新），用于拖尾绘制时按实际填充长度截断数组
            Projectile.ai[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (initialVelocity == Vector2.Zero) {
                initialVelocity = Projectile.velocity;
            }

            //沿途暖黄光，让过去时代昏暗色调下更易辨识
            Lighting.AddLight(Projectile.Center, 0.85f, 0.55f, 0.18f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            SpawnImpactEffects(Projectile.Center, Projectile.velocity, hitTile: false);
            impactResolved = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            SpawnImpactEffects(Projectile.Center, oldVelocity, hitTile: true);
            impactResolved = true;
            return true;
        }

        public override void OnKill(int timeLeft) {
            if (!impactResolved) {
                SpawnImpactEffects(Projectile.Center, initialVelocity, hitTile: false);
            }
        }

        /// <summary>
        /// 命中反馈：着色器爆点弹幕、少量烟雾 Gore、命中音效
        /// 不再喷 DustID 粒子，爆点观感完全交给 GatlinImpactBurst 处理
        /// </summary>
        private static void SpawnImpactEffects(Vector2 pos, Vector2 hitVelocity, bool hitTile) {
            if (Main.dedServ) return;

            Vector2 normal = hitVelocity.SafeNormalize(Vector2.UnitX);

            //主爆点弹幕：只在本机生成，负责渲染整段爆炸
            Projectile.NewProjectile(new EntitySource_WorldEvent("GatlinBullet_Impact"), pos,
                Vector2.Zero, ModContent.ProjectileType<GatlinImpactBurst>(), 0, 0, Main.myPlayer,
                ai0: normal.X, ai1: normal.Y);

            //少量烟雾 Gore 让爆点在几帧后仍保留余韵，与着色器快速衰减形成层次
            if (hitTile) {
                int smokeCount = Main.rand.Next(2, 4);
                for (int i = 0; i < smokeCount; i++) {
                    Vector2 goreVel = -normal * Main.rand.NextFloat(0.6f, 1.8f)
                        + Main.rand.NextVector2Circular(1.2f, 1.2f) - Vector2.UnitY * 0.4f;
                    int gore = Gore.NewGore(new EntitySource_WorldEvent("GatlinBullet_Impact"),
                        pos, goreVel, GoreID.Smoke1 + Main.rand.Next(3), Main.rand.NextFloat(0.65f, 0.95f));
                    if (gore >= 0 && gore < Main.maxGore) {
                        Main.gore[gore].alpha = 90;
                    }
                }
            }

            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.38f, Pitch = 0.35f, PitchVariance = 0.2f }, pos);
        }

        public float GetTrailWidth(float completionRatio) {
            //头端较粗、尾端快速收束，着色器里 along=1 是尾端
            float headFactor = 1f - completionRatio;
            float profile = headFactor * headFactor * 0.85f + headFactor * 0.15f;
            return 16f * profile + 1f;
        }

        public Color GetTrailColor(Vector2 completionRatio) {
            //着色器内部自行配色，这里的 Color 主要用作顶点 alpha 调制
            float fade = 1f - completionRatio.X;
            return Color.White * fade;
        }

        void IPrimitiveDrawable.DrawPrimitives() {
            if (Projectile.oldPos == null || Projectile.oldPos.Length == 0) return;

            //按实际 tick 数判定已被引擎写入的条目数；未写入的尾部槽位此刻仍是 (0,0)，
            //若强行塞成 Center 会在"已写入段"与"伪填充段"交界处形成折线拐点（鬼畜链接）
            int ticks = (int)Projectile.ai[0];
            int validCount = System.Math.Min(Projectile.oldPos.Length, ticks);
            //至少两个真实点才画，避免首帧从 velocity 方向尚未稳定时的抖动
            if (validCount < 2) return;

            //保持数组长度稳定，便于 Trail 内部顶点缓冲复用；
            //尾部未填充槽位全部塞成"最老的真实点"——这些重合点在带状网格里会退化成零面积段不可见
            int length = Projectile.oldPos.Length;
            Vector2[] positions = new Vector2[length];
            Vector2 oldestReal = Projectile.oldPos[validCount - 1];
            if (oldestReal == Vector2.Zero) {
                oldestReal = Projectile.position;
            }
            for (int i = 0; i < length; i++) {
                Vector2 raw = i < validCount ? Projectile.oldPos[i] : oldestReal;
                if (raw == Vector2.Zero) {
                    raw = oldestReal;
                }
                positions[i] = raw + Projectile.Size * 0.5f;
            }

            trail ??= new Trail(positions, GetTrailWidth, GetTrailColor);
            trail.TrailPositions = positions;

            Effect effect = EffectLoader.GatlinTracer?.Value;
            if (effect == null) return;
            effect.Parameters["transformMatrix"]?.SetValue(VaultUtils.GetTransfromMatrix());
            effect.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly + Projectile.identity * 0.17f);
            effect.Parameters["fadeAlpha"]?.SetValue(1f);
            effect.Parameters["coreBoost"]?.SetValue(1f);
            effect.Parameters["uNoiseTex"]?.SetValue(CWRAsset.Extra_193.Value);

            GraphicsDevice device = Main.graphics.GraphicsDevice;
            device.BlendState = BlendState.Additive;
            trail.DrawTrail(effect);
            device.BlendState = BlendState.AlphaBlend;
        }

        public override bool PreDraw(ref Color lightColor) {
            //不再做任何 SpriteBatch 额外绘制，视觉完全由 DrawPrimitives 中的着色器拖尾承担
            return false;
        }
    }
}

