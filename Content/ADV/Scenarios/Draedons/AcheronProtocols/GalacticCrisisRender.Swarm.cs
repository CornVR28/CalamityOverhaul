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
                    MaxLength = Main.rand.NextFloat(0.4f, 0.85f),
                    Width = Main.rand.NextFloat(8f, 25f),
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
            float spawnDist = GalaxyRadius * (1.1f + Main.rand.NextFloat(0.3f));
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
        /// 使用多层SoftGlow绘制虫群阴影团块，替代旋转矩形方块
        /// </summary>
        private static void DrawSwarmShadowMass(SpriteBatch sb, Vector2 center, float alpha) {
            Texture2D softGlow = CWRAsset.SoftGlow?.Value;
            Texture2D pixel = VaultAsset.placeholder2.Value;

            float swarmDistance = GalaxyRadius * MathHelper.Lerp(1.6f, 0.9f, swarmApproachProgress);
            Vector2 swarmCenter = center + new Vector2(MathF.Cos(SwarmCenterAngle), MathF.Sin(SwarmCenterAngle)) * swarmDistance;
            float massRadius = GalaxyRadius * MathHelper.Lerp(0.6f, 1.2f, swarmApproachProgress);

            if (softGlow != null) {
                Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);
                Color shadowColor = new Color(3, 1, 4);

                //多层SoftGlow叠加形成不规则的有机暗色团块
                int layers = 6;
                for (int i = 0; i < layers; i++) {
                    float t = i / (float)layers;
                    float layerScale = massRadius * (0.018f - t * 0.005f);
                    float layerAlpha = alpha * (0.55f - t * 0.05f);

                    //蠕动偏移：每层略微偏移位置
                    float wobbleX = MathF.Sin(swarmPulseTimer * 1.5f + t * 4f) * 8f * (1f - t * 0.5f);
                    float wobbleY = MathF.Cos(swarmPulseTimer * 1.8f + t * 3f) * 6f * (1f - t * 0.5f);
                    Vector2 layerCenter = swarmCenter + new Vector2(wobbleX, wobbleY);

                    sb.Draw(softGlow, layerCenter, null, shadowColor * layerAlpha, 0f,
                        glowOrigin, layerScale, SpriteEffects.None, 0f);
                }

                //在暗色团块边缘分布较小的SoftGlow模拟不规则触手状边缘
                int edgeBlobs = 10;
                for (int i = 0; i < edgeBlobs; i++) {
                    float angle = MathHelper.TwoPi * i / edgeBlobs + globalTimer * 0.15f;
                    float wobble = MathF.Sin(swarmPulseTimer * 2.5f + i * 1.3f) * 0.2f + 0.8f;
                    float dist = massRadius * 0.4f * wobble;
                    Vector2 blobPos = swarmCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist;
                    float blobScale = massRadius * (0.006f + MathF.Sin(swarmPulseTimer + i) * 0.002f);
                    float blobAlpha = alpha * 0.4f * wobble;

                    sb.Draw(softGlow, blobPos, null, shadowColor * blobAlpha, 0f,
                        glowOrigin, blobScale, SpriteEffects.None, 0f);
                }

                //中心暗红色微光
                Color innerGlow = new Color(40, 5, 10);
                innerGlow.A = 0;
                float innerPulse = MathF.Sin(swarmPulseTimer * 2f) * 0.2f + 0.8f;
                sb.Draw(softGlow, swarmCenter, null, innerGlow * (alpha * 0.2f * innerPulse), 0f,
                    glowOrigin, massRadius * 0.008f, SpriteEffects.None, 0f);
            } else {
                //后备方案：使用小像素点阵模拟
                if (pixel == null) return;
                Color shadowColor = new Color(5, 2, 5);
                int pointCount = 80;
                for (int i = 0; i < pointCount; i++) {
                    float angle = MathHelper.TwoPi * i / pointCount;
                    float wobble = MathF.Sin(swarmPulseTimer * 2f + i * 0.5f) * 0.15f + 0.85f;
                    float dist = massRadius * 0.5f * wobble * Main.rand.NextFloat(0.3f, 1f);
                    Vector2 pos = swarmCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist;
                    float ptSize = Main.rand.NextFloat(3f, 10f);
                    sb.Draw(pixel, pos, new Rectangle(0, 0, 1, 1),
                        shadowColor * (alpha * 0.5f), angle, new Vector2(0.5f),
                        new Vector2(ptSize), SpriteEffects.None, 0f);
                }
            }
        }

        private static void DrawSwarmTendril(SpriteBatch sb, Vector2 center, SwarmTendril tendril, float alpha) {
            Texture2D pixel = VaultAsset.placeholder2.Value;
            Texture2D softGlow = CWRAsset.SoftGlow?.Value;
            if (pixel == null) return;
            if (tendril.Length <= 0.01f) return;

            float startDist = GalaxyRadius * 1.3f;
            float endDist = GalaxyRadius * (1.3f - tendril.Length * 1.5f);

            Vector2 startPos = center + new Vector2(MathF.Cos(tendril.BaseAngle), MathF.Sin(tendril.BaseAngle)) * startDist;
            Vector2 endPos = center + new Vector2(MathF.Cos(tendril.BaseAngle), MathF.Sin(tendril.BaseAngle)) * endDist;

            Color tendrilColor = new Color(12, 4, 10);
            Color edgeColor = new Color(80, 15, 20);

            for (int seg = 0; seg < tendril.SegmentCount; seg++) {
                float t = seg / (float)tendril.SegmentCount;
                if (t > tendril.Length / tendril.MaxLength) break;

                Vector2 segPos = Vector2.Lerp(startPos, endPos, t);

                float perpAngle = tendril.BaseAngle + MathHelper.PiOver2;
                float waveOffset = MathF.Sin(tendril.WavePhase + t * 8f) * tendril.WaveAmplitude * t;
                segPos += new Vector2(MathF.Cos(perpAngle), MathF.Sin(perpAngle)) * waveOffset;

                float segWidth = tendril.Width * (1f - t * 0.7f);
                float segAlpha = alpha * (1f - t * 0.1f);

                //使用SoftGlow绘制触须段，更柔和
                Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);
                float segScale = segWidth * 0.04f;
                sb.Draw(softGlow, segPos, null, tendrilColor * segAlpha, 0f,
                    glowOrigin, segScale, SpriteEffects.None, 0f);

                //暗红色边缘发光
                Color glowC = edgeColor;
                glowC.A = 0;
                float glowPulse = MathF.Sin(swarmPulseTimer * 3f + t * 5f) * 0.3f + 0.5f;
                sb.Draw(softGlow, segPos, null,
                    glowC * (segAlpha * 0.25f * glowPulse), 0f,
                    glowOrigin, segScale * 1.5f, SpriteEffects.None, 0f);
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
            float swarmDistance = GalaxyRadius * MathHelper.Lerp(1.6f, 0.9f, swarmApproachProgress);
            float pulse = MathF.Sin(swarmPulseTimer * 2f) * 0.3f + 0.7f;

            Texture2D softGlow = CWRAsset.SoftGlow?.Value;

            if (softGlow != null) {
                int glowCount = 10;
                float arcSpread = MathHelper.ToRadians(90f);
                Vector2 glowOrigin = new(softGlow.Width * 0.5f, softGlow.Height * 0.5f);

                for (int i = 0; i < glowCount; i++) {
                    float t = i / (float)glowCount;
                    float angle = SwarmCenterAngle - arcSpread / 2f + arcSpread * t;
                    Vector2 glowPos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (swarmDistance - 5f);

                    Color redGlow = new Color(180, 25, 35);
                    redGlow.A = 0;
                    float glowAlpha = alpha * 0.35f * pulse * MathF.Sin(t * MathHelper.Pi);
                    float glowScale = 0.4f + MathF.Sin(swarmPulseTimer * 3f + t * 5f) * 0.08f;

                    sb.Draw(softGlow, glowPos, null, redGlow * glowAlpha, 0f,
                        glowOrigin, glowScale, SpriteEffects.None, 0f);
                }
            } else {
                Texture2D pixel = VaultAsset.placeholder2.Value;
                if (pixel == null) return;
                int arcSegments = 20;
                float arcSpread = MathHelper.ToRadians(80f);
                for (int i = 0; i < arcSegments; i++) {
                    float t = i / (float)arcSegments;
                    float angle = SwarmCenterAngle - arcSpread / 2f + arcSpread * t;
                    Vector2 arcPos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (swarmDistance - 10f);
                    Color glowColor = new Color(120, 20, 30);
                    glowColor.A = 0;
                    float arcAlpha = alpha * 0.5f * pulse * MathF.Sin(t * MathHelper.Pi);
                    sb.Draw(pixel, arcPos, new Rectangle(0, 0, 1, 1),
                        glowColor * arcAlpha, angle, new Vector2(0.5f),
                        new Vector2(15f, 4f), SpriteEffects.None, 0f);
                }
            }
        }

        #endregion
    }
}
