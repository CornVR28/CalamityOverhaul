using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols
{
    /// <summary>
    /// 虫群绘制：阴影团块、蠕动触须、粒子、边缘光晕
    /// 使用SoftGlow纹理替代像素方块实现有机感的黑暗虫群
    /// </summary>
    internal partial class GalacticCrisisRender
    {
        #region 虫群参数与数据

        private const int SwarmTendrilCount = 12;
        private const int SwarmParticleCount = 200;
        private static readonly List<SwarmTendril> swarmTendrils = [];
        private static readonly List<SwarmParticle> swarmParticles = [];
        private static float swarmApproachProgress;
        private static float swarmPulseTimer;

        //虫群入侵方向角度
        private const float SwarmCenterAngle = -MathHelper.PiOver4;

        private class SwarmTendril
        {
            public float BaseAngle;
            public float Length;
            public float MaxLength;
            public float Width;
            public float WavePhase;
            public float WaveSpeed;
            public float WaveAmplitude;
            public int SegmentCount;
        }

        private class SwarmParticle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
            public float Size;
            public float Alpha;
        }

        #endregion

        #region 初始化与清理

        private static void InitSwarm() {
            swarmApproachProgress = 0f;
            swarmPulseTimer = 0f;
            GenerateSwarmTendrils();
            swarmParticles.Clear();
        }

        private static void CleanupSwarm() {
            swarmTendrils.Clear();
            swarmParticles.Clear();
        }

        private static void GenerateSwarmTendrils() {
            swarmTendrils.Clear();
            for (int i = 0; i < SwarmTendrilCount; i++) {
                float angleSpread = MathHelper.ToRadians(60f);
                float angle = SwarmCenterAngle + Main.rand.NextFloat(-angleSpread, angleSpread);

                swarmTendrils.Add(new SwarmTendril {
                    BaseAngle = angle,
                    Length = 0f,
                    MaxLength = Main.rand.NextFloat(0.25f, 0.55f),
                    Width = Main.rand.NextFloat(6f, 18f),
                    WavePhase = Main.rand.NextFloat(MathHelper.TwoPi),
                    WaveSpeed = Main.rand.NextFloat(1.5f, 3f),
                    WaveAmplitude = Main.rand.NextFloat(5f, 15f),
                    SegmentCount = Main.rand.Next(12, 24)
                });
            }
        }

        #endregion

        #region 逻辑更新

        private static void UpdateSwarmLogic() {
            for (int i = swarmParticles.Count - 1; i >= 0; i--) {
                var p = swarmParticles[i];
                p.Life++;
                p.Position += p.Velocity;
                if (p.Life >= p.MaxLife) {
                    swarmParticles.RemoveAt(i);
                }
            }
        }

        private static void UpdateSwarmApproachPhase() {
            swarmApproachProgress = MathF.Min(swarmApproachProgress + 0.004f, 1f);
            swarmPulseTimer += 0.04f;
            phaseProgress = swarmApproachProgress;

            foreach (var tendril in swarmTendrils) {
                tendril.Length = MathF.Min(tendril.Length + 0.006f * Main.rand.NextFloat(0.5f, 1.5f), tendril.MaxLength * swarmApproachProgress);
                tendril.WavePhase += tendril.WaveSpeed * 0.016f;
            }

            glitchIntensity = MathHelper.Lerp(0.02f, 0.15f, swarmApproachProgress);
            SpawnSwarmParticles();
        }

        private static void UpdateIdlePhase() {
            swarmPulseTimer += 0.03f;
        }

        private static void SpawnSwarmParticles() {
            if (swarmParticles.Count >= SwarmParticleCount) return;
            if (!Main.rand.NextBool(3)) return;

            float spawnAngle = SwarmCenterAngle + Main.rand.NextFloat(-0.8f, 0.8f);
            float spawnDist = GalaxyRadius * (1.4f + Main.rand.NextFloat(0.4f));
            Vector2 spawnPos = new Vector2(MathF.Cos(spawnAngle), MathF.Sin(spawnAngle)) * spawnDist;

            Vector2 toCenter = -spawnPos;
            if (toCenter != Vector2.Zero) toCenter.Normalize();
            float speed = Main.rand.NextFloat(0.3f, 1.0f);
            Vector2 velocity = toCenter * speed + new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextFloat(-0.2f, 0.2f));

            swarmParticles.Add(new SwarmParticle {
                Position = spawnPos,
                Velocity = velocity,
                Life = 0f,
                MaxLife = Main.rand.NextFloat(80f, 200f),
                Size = Main.rand.NextFloat(1f, 3f),
                Alpha = Main.rand.NextFloat(0.3f, 0.8f)
            });
        }

        #endregion

        #region 绘制

        private static void DrawSwarm(SpriteBatch sb, Vector2 center, float alpha) {
            float swarmAlpha = alpha * MathF.Min(swarmApproachProgress * 2f, 1f);

            DrawSwarmShadowMass(sb, center, swarmAlpha);

            foreach (var tendril in swarmTendrils) {
                DrawSwarmTendril(sb, center, tendril, swarmAlpha);
            }

            DrawSwarmParticles(sb, center, swarmAlpha);
            DrawSwarmEdgeGlow(sb, center, swarmAlpha);
        }

        /// <summary>
        /// 获取虫群中心位置（供触须和边缘光晕共用）
        /// </summary>
        private static void GetSwarmCenterAndRadius(Vector2 center, out Vector2 swarmCenter, out float massRadius) {
            //虫群从远处逼近，但核心始终在面板内可见
            float swarmDistance = GalaxyRadius * MathHelper.Lerp(1.8f, 1.2f, swarmApproachProgress);
            swarmCenter = center + new Vector2(MathF.Cos(SwarmCenterAngle), MathF.Sin(SwarmCenterAngle)) * swarmDistance;
            massRadius = GalaxyRadius * MathHelper.Lerp(0.5f, 0.85f, swarmApproachProgress);
        }

        /// <summary>
        /// 绘制虫群阴影团块：用较少、较小、半透明的SoftGlow层
        /// 保持暗色核心但不遮挡红色光效
        /// </summary>
        private static void DrawSwarmShadowMass(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D softGlow = CWRAsset.SoftGlow?.Value;
            Texture2D pixel = VaultAsset.placeholder2.Value;

            GetSwarmCenterAndRadius(center, out Vector2 swarmCenter, out float massRadius);

            if (softGlow != null) {
                Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);

                //暗色核心团块：7层大面积SoftGlow叠加，半透明以保留红色可见
                //使用暗紫红色而非纯黑，让暗色本身也带有威胁感
                Color shadowCore = new Color(10, 3, 8);
                shadowCore.A = 0;
                for (int i = 0; i < 7; i++) {
                    float t = i / 7f;
                    float layerScale = massRadius * (0.016f - t * 0.004f);
                    //每层透明度较低，但7层叠加后中心区域足够黑暗，边缘自然渐隐
                    float layerAlpha = alpha * (0.2f - t * 0.015f);

                    float wobbleX = MathF.Sin(swarmPulseTimer * 1.5f + t * 4f) * 8f * (1f - t * 0.5f);
                    float wobbleY = MathF.Cos(swarmPulseTimer * 1.8f + t * 3f) * 6f * (1f - t * 0.5f);
                    Vector2 layerCenter = swarmCenter + new Vector2(wobbleX, wobbleY);

                    sb.Draw(softGlow, layerCenter, null, shadowCore * layerAlpha, 0f,
                        glowOrigin, layerScale, SpriteEffects.None, 0f);
                }

                //大面积暗红色光晕覆盖在暗核之上，视觉威胁感的主要来源
                float redPulse = MathF.Sin(swarmPulseTimer * 2f) * 0.15f + 0.85f;

                Color redMass = new Color(120, 18, 30);
                redMass.A = 0;
                sb.Draw(softGlow, swarmCenter, null, redMass * (alpha * 0.45f * redPulse), 0f,
                    glowOrigin, massRadius * 0.012f, SpriteEffects.None, 0f);

                //外层更大的暗红色弥散光晕
                Color outerRed = new Color(70, 10, 18);
                outerRed.A = 0;
                sb.Draw(softGlow, swarmCenter, null, outerRed * (alpha * 0.3f * redPulse), 0f,
                    glowOrigin, massRadius * 0.018f, SpriteEffects.None, 0f);

                //边缘不规则蠕动光斑，增加体积感和有机感
                int edgeBlobs = 12;
                for (int i = 0; i < edgeBlobs; i++) {
                    float angle = MathHelper.TwoPi * i / edgeBlobs + globalTimer * 0.12f;
                    float wobble = MathF.Sin(swarmPulseTimer * 2.5f + i * 1.3f) * 0.25f + 0.75f;
                    float dist = massRadius * (0.3f + wobble * 0.15f);
                    Vector2 blobPos = swarmCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist;
                    float blobScale = massRadius * (0.005f + MathF.Sin(swarmPulseTimer + i) * 0.002f);

                    //交替暗色和暗红色光斑，丰富层次
                    Color blobColor = i % 2 == 0
                        ? new Color(12, 4, 8) { A = 0 }
                        : new Color(90, 12, 20) { A = 0 };
                    float blobAlpha = alpha * (0.25f + wobble * 0.1f);
                    sb.Draw(softGlow, blobPos, null, blobColor * blobAlpha, 0f,
                        glowOrigin, blobScale, SpriteEffects.None, 0f);
                }
            } else {
                if (pixel == null) return;
                GetSwarmCenterAndRadius(center, out _, out _);
                Color shadowColor = new Color(15, 5, 10);
                int pointCount = 50;
                for (int i = 0; i < pointCount; i++) {
                    float angle = MathHelper.TwoPi * i / pointCount;
                    float wobble = MathF.Sin(swarmPulseTimer * 2f + i * 0.5f) * 0.15f + 0.85f;
                    float dist = massRadius * 0.4f * wobble * Main.rand.NextFloat(0.3f, 1f);
                    Vector2 pos = swarmCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist;
                    float ptSize = Main.rand.NextFloat(2f, 6f);
                    sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1),
                        shadowColor * (alpha * 0.4f), angle, new Vector2(0.5f),
                        new Vector2(ptSize), SpriteEffects.None, 0f);
                }
            }
        }

        private static void DrawSwarmTendril(SpriteBatch sb, Vector2 center, SwarmTendril tendril, float alpha) {
            Texture2D softGlow = CWRAsset.SoftGlow?.Value;
            Texture2D pixel = VaultAsset.placeholder2.Value;
            if (tendril.Length <= 0.01f) return;

            //触须从更远处开始，penetration更浅
            float startDist = GalaxyRadius * 1.6f;
            float endDist = GalaxyRadius * (1.6f - tendril.Length * 1.2f);
            //确保触须末端不会深入银河系核心区域
            endDist = MathF.Max(endDist, GalaxyRadius * 0.7f);

            Vector2 startPos = center + new Vector2(MathF.Cos(tendril.BaseAngle), MathF.Sin(tendril.BaseAngle)) * startDist;
            Vector2 endPos = center + new Vector2(MathF.Cos(tendril.BaseAngle), MathF.Sin(tendril.BaseAngle)) * endDist;

            Color tendrilColor = new Color(20, 6, 14);
            Color edgeColor = new Color(140, 25, 35);

            for (int seg = 0; seg < tendril.SegmentCount; seg++) {
                float t = seg / (float)tendril.SegmentCount;
                if (t > tendril.Length / tendril.MaxLength) break;

                Vector2 segPos = Vector2.Lerp(startPos, endPos, t);

                float perpAngle = tendril.BaseAngle + MathHelper.PiOver2;
                float waveOffset = MathF.Sin(tendril.WavePhase + t * 8f) * tendril.WaveAmplitude * t;
                segPos += new Vector2(MathF.Cos(perpAngle), MathF.Sin(perpAngle)) * waveOffset;

                float segWidth = tendril.Width * (1f - t * 0.7f);
                float segAlpha = alpha * (0.9f - t * 0.3f);

                if (softGlow != null) {
                    Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);
                    float segScale = segWidth * 0.03f;

                    //暗色触须核心
                    sb.Draw(softGlow, segPos, null, tendrilColor * (segAlpha * 0.6f), 0f,
                        glowOrigin, segScale, SpriteEffects.None, 0f);

                    //红色边缘发光（更亮更明显）
                    Color glowC = edgeColor;
                    glowC.A = 0;
                    float glowPulse = MathF.Sin(swarmPulseTimer * 3f + t * 5f) * 0.3f + 0.7f;
                    sb.Draw(softGlow, segPos, null,
                        glowC * (segAlpha * 0.45f * glowPulse), 0f,
                        glowOrigin, segScale * 1.8f, SpriteEffects.None, 0f);
                } else if (pixel != null) {
                    float segAngle = tendril.BaseAngle + MathHelper.Pi;
                    sb.Draw(pixel, segPos, new Rectangle(0, 0, 1, 1),
                        tendrilColor * segAlpha, segAngle, new Vector2(0.5f),
                        new Vector2(segWidth * 2f, segWidth * 0.5f), SpriteEffects.None, 0f);
                }
            }
        }

        private static void DrawSwarmParticles(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D softGlow = CWRAsset.SoftGlow?.Value;
            Texture2D pixel = VaultAsset.placeholder2.Value;

            foreach (var p in swarmParticles) {
                float lifeRatio = p.Life / p.MaxLife;
                float fade = MathF.Sin(lifeRatio * MathHelper.Pi);
                float particleAlpha = p.Alpha * fade * alpha;
                Vector2 screenPos = center + p.Position;

                if (softGlow != null) {
                    Color particleColor = new Color(50, 8, 15);
                    particleColor.A = 0;
                    Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);
                    sb.Draw(softGlow, screenPos, null, particleColor * particleAlpha, 0f,
                        glowOrigin, p.Size * 0.04f, SpriteEffects.None, 0f);
                } else if (pixel != null) {
                    Color particleColor = new Color(60, 10, 20) * particleAlpha;
                    sb.Draw(pixel, screenPos, new Rectangle(0, 0, 1, 1),
                        particleColor, globalTimer + p.Life * 0.1f, new Vector2(0.5f),
                        new Vector2(p.Size * 1.5f, p.Size * 0.5f), SpriteEffects.None, 0f);
                }
            }
        }

        private static void DrawSwarmEdgeGlow(SpriteBatch sb, Vector2 center, float alpha) {
            GetSwarmCenterAndRadius(center, out Vector2 swarmCenter, out float massRadius);
            float swarmDistance = (swarmCenter - center).Length();
            float pulse = MathF.Sin(swarmPulseTimer * 2f) * 0.3f + 0.7f;

            Texture2D softGlow = CWRAsset.SoftGlow?.Value;

            if (softGlow != null) {
                int glowCount = 14;
                float arcSpread = MathHelper.ToRadians(100f);
                Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);

                for (int i = 0; i < glowCount; i++) {
                    float t = i / (float)glowCount;
                    float angle = SwarmCenterAngle - arcSpread / 2f + arcSpread * t;
                    //光晕位于虫群主体和银河系之间的前沿
                    float glowDist = swarmDistance - massRadius * 0.3f;
                    Vector2 glowPos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * glowDist;

                    Color redGlow = new Color(200, 30, 40);
                    redGlow.A = 0;
                    float glowAlpha = alpha * 0.5f * pulse * MathF.Sin(t * MathHelper.Pi);
                    float glowScale = 0.45f + MathF.Sin(swarmPulseTimer * 3f + t * 5f) * 0.1f;

                    sb.Draw(softGlow, glowPos, null, redGlow * glowAlpha, 0f,
                        glowOrigin, glowScale, SpriteEffects.None, 0f);
                }
            } else {
                Texture2D pixel = VaultAsset.placeholder2.Value;
                if (pixel == null) return;
                int arcSegments = 20;
                float arcSpread = MathHelper.ToRadians(90f);
                for (int i = 0; i < arcSegments; i++) {
                    float t = i / (float)arcSegments;
                    float angle = SwarmCenterAngle - arcSpread / 2f + arcSpread * t;
                    Vector2 arcPos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (swarmDistance - 10f);
                    Color glowColor = new Color(150, 25, 35);
                    glowColor.A = 0;
                    float arcAlpha = alpha * 0.6f * pulse * MathF.Sin(t * MathHelper.Pi);
                    sb.Draw(pixel, arcPos, new Rectangle(0, 0, 1, 1),
                        glowColor * arcAlpha, angle, new Vector2(0.5f),
                        new Vector2(18f, 5f), SpriteEffects.None, 0f);
                }
            }
        }

        #endregion
    }
}
