using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    /// <summary>
    /// 启示录天国领域：Q键终极技能生成的神圣领域
    /// 12门徒殉道后，圣约翰最终殉道触发
    /// </summary>
    internal class RevelationDomain : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        private Player Owner => Main.player[Projectile.owner];

        //ai[0]=计时器, ai[1]=未使用
        private ref float Timer => ref Projectile.ai[0];

        //领域参数
        private const int ExpandFrames = 60; //展开时间
        private const float MaxRadius = 1000f; //最大半径(像素)
        private const int DamageInterval = 15; //伤害间隔(帧)

        //视觉状态
        private float expandScale = 0f;
        private float fadeAlpha = 0f;
        private float shaderTime = 0f;
        private float revelationBuildUp = 0f;

        //天雷系统
        private int thunderCooldown = 0;
        private const int ThunderInterval = 40; //天雷间隔(帧)
        private readonly List<DivineLightning> activeLightnings = [];

        //天雷数据
        private struct DivineLightning
        {
            public Vector2 Start;
            public Vector2 End;
            public List<Vector2> Segments;
            public float Life;
            public float MaxLife;
            public float Intensity;
        }

        public override void SetDefaults() {
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = DamageInterval;
            Projectile.timeLeft = 2;
        }

        public override void AI() {
            if (!Owner.active || Owner.dead) {
                Projectile.Kill();
                return;
            }

            if (!Owner.TryGetModPlayer<ElysiumPlayer>(out var ep) || !ep.IsRevelationActive) {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            Timer++;
            shaderTime += 0.016f;

            //跟随玩家
            Projectile.Center = Owner.Center;

            //展开/持续/收缩阶段
            if (Timer <= ExpandFrames) {
                //展开阶段
                float t = Timer / ExpandFrames;
                expandScale = MathHelper.SmoothStep(0f, 1f, t);
                fadeAlpha = MathHelper.SmoothStep(0f, 1f, t);
            }
            else {
                expandScale = 1f;
                fadeAlpha = 1f;
            }

            //启示录强度随时间渐增
            revelationBuildUp = MathHelper.Clamp(Timer / 300f, 0f, 1f);

            //更新碰撞尺寸
            float currentRadius = MaxRadius * expandScale;
            int size = (int)(currentRadius * 2);
            Projectile.width = Projectile.height = Math.Max(size, 10);
            Projectile.Center = Owner.Center;

            //范围伤害
            if (Timer % DamageInterval == 0 && expandScale > 0.3f) {
                foreach (NPC npc in Main.npc) {
                    if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                    if (Vector2.Distance(npc.Center, Projectile.Center) < currentRadius) {
                        Owner.ApplyDamageToNPC(npc, Projectile.damage, Projectile.knockBack, 0, false);

                        //神圣伤害粒子
                        for (int i = 0; i < 3; i++) {
                            Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                            Dust d = Dust.NewDustPerfect(npc.Center, DustID.GoldFlame, vel, 100, default, 1.5f);
                            d.noGravity = true;
                        }
                    }
                }
            }

            //天雷系统：从天而降的神圣雷电
            if (thunderCooldown > 0) thunderCooldown--;
            if (thunderCooldown <= 0 && expandScale > 0.5f) {
                SpawnDivineThunder(currentRadius);
                thunderCooldown = ThunderInterval;
            }

            //更新活跃天雷
            for (int i = activeLightnings.Count - 1; i >= 0; i--) {
                var l = activeLightnings[i];
                l.Life++;
                activeLightnings[i] = l;
                if (l.Life >= l.MaxLife) {
                    activeLightnings.RemoveAt(i);
                }
            }

            //领域生命回复效果
            if (Timer % 30 == 0) {
                Owner.Heal((int)(5 * expandScale));
            }

            //领域增益
            Owner.GetDamage(DamageClass.Generic) += 0.35f * expandScale;
            Owner.statDefense += (int)(30 * expandScale);

            //动态照明
            float lightScale = 1.2f * expandScale * fadeAlpha;
            Lighting.AddLight(Projectile.Center, 1f * lightScale, 0.9f * lightScale, 0.6f * lightScale);

            //环境粒子
            if (Timer % 3 == 0 && expandScale > 0.2f) {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(currentRadius * 0.3f, currentRadius * 0.9f);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * dist;
                Vector2 vel = new Vector2(0, -Main.rand.NextFloat(1f, 3f)); //向上飘升
                int dustType = Main.rand.NextBool(2) ? DustID.GoldFlame : DustID.SilverFlame;
                Dust d = Dust.NewDustPerfect(pos, dustType, vel, 80, default, 1.2f);
                d.noGravity = true;
            }
        }

        public override bool? CanDamage() => false; //伤害通过AI手动应用

        public override bool PreDraw(ref Color lightColor) {
            SpriteBatch sb = Main.spriteBatch;
            Vector2 center = Projectile.Center - Main.screenPosition;

            DrawCelestialDomainShader(sb, center);
            DrawDivineLightnings(sb);
            return false;
        }

        /// <summary>
        /// 在域内随机位置或敌人位置生成天雷
        /// </summary>
        private void SpawnDivineThunder(float currentRadius) {
            //优先劈向域内的敌人
            NPC target = null;
            float closestDist = currentRadius;
            foreach (NPC npc in Main.npc) {
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;
                float d = Vector2.Distance(npc.Center, Projectile.Center);
                if (d < closestDist) {
                    closestDist = d;
                    target = npc;
                }
            }

            Vector2 strikePos;
            if (target != null) {
                strikePos = target.Center;
                //天雷对目标造成额外伤害
                Owner.ApplyDamageToNPC(target, Projectile.damage * 2, 8f, 0, true);
            }
            else {
                //没有敌人时随机劈在域内
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(currentRadius * 0.2f, currentRadius * 0.8f);
                strikePos = Projectile.Center + angle.ToRotationVector2() * dist;
            }

            //雷电从天空高处开始
            Vector2 skyPos = new Vector2(strikePos.X + Main.rand.NextFloat(-40f, 40f), strikePos.Y - 800f);

            //生成锯齿形闪电路径
            List<Vector2> segments = GenerateLightningPath(skyPos, strikePos);

            activeLightnings.Add(new DivineLightning {
                Start = skyPos,
                End = strikePos,
                Segments = segments,
                Life = 0,
                MaxLife = 20,
                Intensity = 1f
            });

            //同时生成1-2条分叉
            int branchCount = Main.rand.Next(1, 3);
            for (int b = 0; b < branchCount; b++) {
                int branchStart = Main.rand.Next(segments.Count / 3, segments.Count * 2 / 3);
                Vector2 branchEnd = strikePos + Main.rand.NextVector2Circular(80f, 50f);
                List<Vector2> branchSegs = GenerateLightningPath(segments[branchStart], branchEnd);
                activeLightnings.Add(new DivineLightning {
                    Start = segments[branchStart],
                    End = branchEnd,
                    Segments = branchSegs,
                    Life = 0,
                    MaxLife = 15,
                    Intensity = 0.6f
                });
            }

            //雷声
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 1.2f, Pitch = Main.rand.NextFloat(-0.4f, 0.1f) }, strikePos);

            //落点爆发粒子
            for (int i = 0; i < 15; i++) {
                Vector2 vel = Main.rand.NextVector2Circular(6f, 4f);
                vel.Y = -Math.Abs(vel.Y); //向上
                int dustType = Main.rand.NextBool(3) ? DustID.SilverFlame : DustID.GoldFlame;
                Dust dust = Dust.NewDustPerfect(strikePos, dustType, vel, 80, default, 2f);
                dust.noGravity = true;
            }

            //落点照明
            Lighting.AddLight(strikePos, 1.5f, 1.4f, 0.8f);
        }

        /// <summary>
        /// 生成锯齿闪电路径
        /// </summary>
        private static List<Vector2> GenerateLightningPath(Vector2 from, Vector2 to) {
            List<Vector2> points = [from];
            Vector2 dir = (to - from).SafeNormalize(Vector2.UnitY);
            Vector2 perp = dir.RotatedBy(MathHelper.PiOver2);
            float totalDist = Vector2.Distance(from, to);
            int segCount = Math.Max(8, (int)(totalDist / 30f));

            for (int i = 1; i < segCount; i++) {
                float t = i / (float)segCount;
                Vector2 basePos = Vector2.Lerp(from, to, t);
                //锯齿偏移：中间最大，两端收窄
                float zigzagRange = MathF.Sin(t * MathHelper.Pi) * 35f;
                float offset = (Main.rand.NextFloat() - 0.5f) * 2f * zigzagRange;
                points.Add(basePos + perp * offset);
            }

            points.Add(to);
            return points;
        }

        /// <summary>
        /// 绘制所有活跃天雷
        /// </summary>
        private void DrawDivineLightnings(SpriteBatch sb) {
            Texture2D pixel = CWRAsset.Placeholder_White.Value;
            if (pixel == null) return;

            foreach (var lightning in activeLightnings) {
                float alpha = 1f - lightning.Life / lightning.MaxLife;
                //闪烁效果：前几帧和随机帧更亮
                float flicker = lightning.Life < 3 ? 1.5f : (Main.rand.NextFloat() > 0.7f ? 1.2f : 0.8f);
                float finalAlpha = alpha * lightning.Intensity * flicker;

                if (lightning.Segments == null || lightning.Segments.Count < 2) continue;

                for (int i = 0; i < lightning.Segments.Count - 1; i++) {
                    Vector2 start = lightning.Segments[i] - Main.screenPosition;
                    Vector2 end = lightning.Segments[i + 1] - Main.screenPosition;

                    //内核层(白色)
                    Color coreColor = new Color(255, 255, 240, 0) * finalAlpha;
                    DrawLightningSegment(sb, pixel, start, end, 2.5f, coreColor);
                    //中间层(金色)
                    Color midColor = new Color(255, 215, 80, 0) * finalAlpha * 0.7f;
                    DrawLightningSegment(sb, pixel, start, end, 5f, midColor);
                    //外层光晕(蓝白)
                    Color glowColor = new Color(180, 200, 255, 0) * finalAlpha * 0.3f;
                    DrawLightningSegment(sb, pixel, start, end, 10f, glowColor);
                }
            }
        }

        private static void DrawLightningSegment(SpriteBatch sb, Texture2D pixel, Vector2 start, Vector2 end, float thickness, Color color) {
            Vector2 diff = end - start;
            float length = diff.Length();
            if (length < 1f) return;
            sb.Draw(pixel, start, new Rectangle(0, 0, 1, 1), color, diff.ToRotation(),
                Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        private void DrawCelestialDomainShader(SpriteBatch sb, Vector2 center) {
            Effect shader = EffectLoader.CelestialDomain?.Value;
            if (shader == null) return;

            Texture2D canvas = CWRAsset.Placeholder_White.Value;
            Texture2D noise = CWRAsset.Extra_193.Value;
            if (canvas == null || noise == null) return;

            float drawRadius = MaxRadius * expandScale * 1.3f;
            float drawDiameter = drawRadius * 2f;

            shader.Parameters["uTime"]?.SetValue(shaderTime);
            shader.Parameters["fadeAlpha"]?.SetValue(fadeAlpha);
            shader.Parameters["expandProgress"]?.SetValue(MathHelper.Clamp(expandScale, 0f, 1f));
            shader.Parameters["revelationIntensity"]?.SetValue(revelationBuildUp);

            //天国色调：白金核心、金色光环、天蓝神圣、柔紫荣光
            shader.Parameters["coreColor"]?.SetValue(new Vector3(1f, 0.96f, 0.88f));
            shader.Parameters["haloColor"]?.SetValue(new Vector3(1f, 0.84f, 0f));
            shader.Parameters["divineColor"]?.SetValue(new Vector3(0.53f, 0.81f, 0.92f));
            shader.Parameters["gloryColor"]?.SetValue(new Vector3(0.48f, 0.41f, 0.93f));
            shader.Parameters["uNoiseTex"]?.SetValue(noise);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            shader.CurrentTechnique.Passes[0].Apply();

            sb.Draw(canvas, center, null, Color.White,
                0f, canvas.Size() * 0.5f, new Vector2(drawDiameter, drawDiameter),
                SpriteEffects.None, 0f);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
