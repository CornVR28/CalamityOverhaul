using InnoVault.Actors;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.DropPodScens
{
    /// <summary>
    /// 空降仓实体，在世界坐标中管理空降仓的逻辑、物理和绘制
    /// </summary>
    internal class DropPodActor : Actor
    {
        /// <summary>
        /// 坠落累计计时（帧）
        /// </summary>
        private int dropTimer;

        /// <summary>
        /// 当前震动偏移（世界坐标像素）
        /// </summary>
        private Vector2 shakeOffset;

        /// <summary>
        /// 再入灼烧强度 0~1
        /// </summary>
        private float reentryHeat;

        /// <summary>
        /// 尾焰粒子列表
        /// </summary>
        private readonly List<TrailParticle> trailParticles = [];

        //常量
        private const int MaxTrailParticles = 60;
        private const float ShakeIntensityBase = 1.5f;
        private const float ShakeIntensityMax = 4f;
        private const float RotationSwaySpeed = 0.015f;
        private const float RotationSwayMax = 0.06f;

        public override void OnSpawn(params object[] args) {
            Width = 40;
            Height = 80;
            DrawExtendMode = 600;
            DrawLayer = ActorDrawLayer.Default;
            dropTimer = 0;
            reentryHeat = 0f;
            shakeOffset = Vector2.Zero;
        }

        public override void AI() {
            if (!DropPodWorld.Active) {
                ActorLoader.KillActor(WhoAmI);
                return;
            }

            dropTimer++;

            //震动效果——随时间加剧（模拟大气再入的颠簸）
            float shakeProgress = MathHelper.Clamp(dropTimer / 600f, 0f, 1f);
            float shakeIntensity = MathHelper.Lerp(ShakeIntensityBase, ShakeIntensityMax, shakeProgress);
            shakeOffset = new Vector2(
                Main.rand.NextFloat(-shakeIntensity, shakeIntensity),
                Main.rand.NextFloat(-shakeIntensity, shakeIntensity));

            //微幅摇摆旋转
            Rotation = MathF.Sin(dropTimer * RotationSwaySpeed) * RotationSwayMax;

            //再入灼烧强度随时间增加
            reentryHeat = MathHelper.Clamp(dropTimer / 480f, 0f, 1f);

            //锁定在玩家位置（世界坐标中心）
            Player player = Main.LocalPlayer;
            if (player != null && player.active) {
                Position = player.Center - Size / 2f;
            }

            //生成尾焰粒子（世界坐标）
            if (dropTimer > 30 && Main.rand.NextBool(2)) {
                SpawnTrailParticle();
            }

            //更新尾焰粒子
            for (int i = trailParticles.Count - 1; i >= 0; i--) {
                trailParticles[i].Update();
                if (trailParticles[i].IsDead) {
                    trailParticles.RemoveAt(i);
                }
            }

            //同步dropTimer给DropPodDrawSystem的屏幕特效使用
            DropPodDrawSystem.SyncDropTimer(dropTimer, reentryHeat);
        }

        private void SpawnTrailParticle() {
            if (trailParticles.Count >= MaxTrailParticles) return;

            //从空降仓底部向下喷射（世界坐标）
            Vector2 spawnPos = Center + new Vector2(
                Main.rand.NextFloat(-20, 20),
                Height * 0.5f + Main.rand.NextFloat(0, 15));

            Vector2 velocity = new Vector2(
                Main.rand.NextFloat(-1.5f, 1.5f),
                Main.rand.NextFloat(3f, 8f));

            Color baseColor = Color.Lerp(
                new Color(255, 200, 100),
                new Color(255, 120, 40),
                Main.rand.NextFloat());

            trailParticles.Add(new TrailParticle {
                Position = spawnPos,
                Velocity = velocity,
                Color = baseColor,
                Alpha = Main.rand.NextFloat(0.6f, 1f),
                Scale = Main.rand.NextFloat(2f, 5f),
                Life = 0,
                MaxLife = Main.rand.Next(20, 50)
            });
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            if (DropPod.DropPodAsset == null || !DropPod.DropPodAsset.IsLoaded) return false;

            Vector2 drawCenter = Center - Main.screenPosition + shakeOffset;

            //绘制再入灼烧光效（在空降仓后面）
            DrawReentryHeatEffect(spriteBatch, drawCenter);

            //绘制尾焰粒子
            DrawTrailParticles(spriteBatch);

            //绘制空降仓主体
            DrawDropPod(spriteBatch, drawCenter);

            return false;
        }

        private void DrawDropPod(SpriteBatch sb, Vector2 drawCenter) {
            Texture2D podTex = DropPod.DropPodAsset.Value;
            Vector2 origin = podTex.Size() * 0.5f;

            //外层光晕
            float glowPulse = MathF.Sin(dropTimer * 0.05f) * 0.15f + 0.85f;
            DrawSoftGlow(sb, drawCenter, new Color(80, 120, 200) * (0.3f * glowPulse), 100f);

            //主体绘制
            sb.Draw(podTex, drawCenter, null, Color.White, Rotation, origin, 1f, SpriteEffects.None, 0f);

            //再入灼烧时在空降仓顶部叠加橙红辉光
            if (reentryHeat > 0.1f) {
                Color heatColor = new Color(255, 150, 50) * (reentryHeat * 0.4f * glowPulse);
                Vector2 heatPos = drawCenter - new Vector2(0, podTex.Height * 0.35f);
                DrawSoftGlow(sb, heatPos, heatColor, 60f + reentryHeat * 30f);
            }
        }

        private void DrawReentryHeatEffect(SpriteBatch sb, Vector2 drawCenter) {
            if (reentryHeat < 0.05f) return;

            float pulse = MathF.Sin(dropTimer * 0.08f) * 0.2f + 0.8f;

            //橙红灼烧光圈
            DrawSoftGlow(sb, drawCenter - new Vector2(0, 20),
                new Color(200, 100, 30) * (reentryHeat * 0.25f * pulse), 120f);

            //外层蓝紫等离子体光圈
            DrawSoftGlow(sb, drawCenter,
                new Color(60, 80, 200) * (reentryHeat * 0.15f * pulse), 160f);
        }

        private void DrawTrailParticles(SpriteBatch sb) {
            if (CWRAsset.SoftGlow == null || CWRAsset.SoftGlow.IsDisposed) return;
            Texture2D glow = CWRAsset.SoftGlow.Value;

            foreach (var p in trailParticles) {
                float lifeRatio = (float)p.Life / p.MaxLife;
                float alpha = p.Alpha * (1f - lifeRatio * lifeRatio);
                Color c = p.Color * alpha;
                float scale = p.Scale * (1f + lifeRatio * 0.5f) * 0.04f;
                Vector2 drawPos = p.Position - Main.screenPosition;
                sb.Draw(glow, drawPos, null, c with { A = 0 }, 0f, glow.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }

        private static void DrawSoftGlow(SpriteBatch sb, Vector2 center, Color color, float radius) {
            if (CWRAsset.SoftGlow == null || CWRAsset.SoftGlow.IsDisposed) return;
            Texture2D glow = CWRAsset.SoftGlow.Value;
            float scale = radius / (glow.Width * 0.5f);
            sb.Draw(glow, center, null, color with { A = 0 }, 0f, glow.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        private class TrailParticle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public Color Color;
            public float Alpha;
            public float Scale;
            public int Life;
            public int MaxLife;
            public bool IsDead => Life >= MaxLife;

            public void Update() {
                Life++;
                Position += Velocity;
                Velocity *= 0.96f;
                Velocity.X += Main.rand.NextFloat(-0.3f, 0.3f);
            }
        }
    }
}
