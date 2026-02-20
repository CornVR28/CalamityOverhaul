using InnoVault.GameSystem;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Graphics;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.DropPodScens
{
    /// <summary>
    /// 空降仓演出玩家覆写——在空降仓子世界中隐藏玩家绘制，管理坠落状态
    /// </summary>
    internal class DropPodPlayer : PlayerOverride
    {
        /// <summary>
        /// 坠落演出是否激活
        /// </summary>
        public bool DropPodActive;
        /// <summary>
        /// 坠落累计计时器
        /// </summary>
        public int DropTimer;

        public override void PostUpdate() {
            if (!DropPodWorld.Active) {
                if (DropPodActive) {
                    DropPodActive = false;
                    DropTimer = 0;
                }
                return;
            }

            DropPodActive = true;
            DropTimer++;

            //锁定玩家位置在世界中央，无重力
            Player.position = new Vector2(
                DropPodWorld.Instance.Width * 16 / 2f - Player.width / 2f,
                DropPodWorld.Instance.Height * 16 / 2f - Player.height / 2f);
            Player.velocity = Vector2.Zero;
            Player.fallStart = (int)(Player.position.Y / 16f);//防止摔落伤害
        }

        /// <summary>
        /// 隐藏玩家绘制——参考HalibutPlayer和CrabulonPlayer的模式
        /// </summary>
        public override bool PreDrawPlayers(ref Camera camera, ref IEnumerable<Player> players) {
            if (!DropPodActive) return true;

            //移除本玩家的绘制
            players = players.Where(p => p.whoAmI != Player.whoAmI);
            return true;
        }
    }

    /// <summary>
    /// 空降仓演出绘制系统——使用ModSystem.PostDrawInterface在UI层绘制空降仓坠落效果
    /// </summary>
    internal class DropPodDrawSystem : ModSystem
    {
        //尾焰粒子
        private static readonly List<TrailParticle> trailParticles = [];
        //状态数据
        private static Vector2 shakeOffset;
        private static float podRotation;
        private static float reentryHeat;
        private static int dropTimer;

        //常量
        private const int MaxTrailParticles = 60;
        private const float ShakeIntensityBase = 1.5f;
        private const float ShakeIntensityMax = 4f;
        private const float RotationSwaySpeed = 0.015f;
        private const float RotationSwayMax = 0.06f;

        public override void PostUpdateEverything() {
            if (!DropPodWorld.Active) {
                if (trailParticles.Count > 0) trailParticles.Clear();
                dropTimer = 0;
                return;
            }

            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return;

            if (!player.TryGetOverride<DropPodPlayer>(out var dpPlayer) || dpPlayer == null || !dpPlayer.DropPodActive) {
                return;
            }

            dropTimer = dpPlayer.DropTimer;

            //震动效果——随时间加剧（模拟大气再入的颠簸）
            float shakeProgress = MathHelper.Clamp(dropTimer / 600f, 0f, 1f);
            float shakeIntensity = MathHelper.Lerp(ShakeIntensityBase, ShakeIntensityMax, shakeProgress);
            shakeOffset = new Vector2(
                Main.rand.NextFloat(-shakeIntensity, shakeIntensity),
                Main.rand.NextFloat(-shakeIntensity, shakeIntensity));

            //微幅摇摆旋转
            podRotation = MathF.Sin(dropTimer * RotationSwaySpeed) * RotationSwayMax;

            //再入灼烧强度随时间增加
            reentryHeat = MathHelper.Clamp(dropTimer / 480f, 0f, 1f);

            //生成尾焰粒子
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
        }

        private static void SpawnTrailParticle() {
            if (trailParticles.Count >= MaxTrailParticles) return;

            //从空降仓底部向下喷射（屏幕空间）
            Vector2 spawnPos = new Vector2(
                Main.screenWidth / 2f + Main.rand.NextFloat(-20, 20),
                Main.screenHeight / 2f + 40 + Main.rand.NextFloat(0, 15));

            Vector2 velocity = new Vector2(
                Main.rand.NextFloat(-1.5f, 1.5f),
                Main.rand.NextFloat(3f, 8f));//向下喷射（尾焰拖出效果）

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

        public override void PostDrawInterface(SpriteBatch spriteBatch) {
            if (!DropPodWorld.Active) return;
            if (DropPod.DropPodAsset == null || !DropPod.DropPodAsset.IsLoaded) return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return;
            if (!player.TryGetOverride<DropPodPlayer>(out var dpPlayer) || dpPlayer == null || !dpPlayer.DropPodActive) return;

            //绘制再入灼烧光效（在空降仓后面）
            DrawReentryHeatEffect(spriteBatch);

            //绘制尾焰粒子
            DrawTrailParticles(spriteBatch);

            //绘制空降仓主体
            DrawDropPod(spriteBatch);

            //绘制前景速度线
            DrawSpeedLines(spriteBatch);
        }

        /// <summary>
        /// 绘制空降仓主体
        /// </summary>
        private static void DrawDropPod(SpriteBatch sb) {
            Texture2D podTex = DropPod.DropPodAsset.Value;
            Vector2 center = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f) + shakeOffset;
            Vector2 origin = podTex.Size() * 0.5f;

            //外层光晕
            float glowPulse = MathF.Sin(dropTimer * 0.05f) * 0.15f + 0.85f;
            DrawSoftGlow(sb, center, new Color(80, 120, 200) * (0.3f * glowPulse), 100f);

            //主体绘制
            sb.Draw(podTex, center, null, Color.White, podRotation, origin, 1f, SpriteEffects.None, 0f);

            //再入灼烧时在空降仓顶部叠加橙红辉光
            if (reentryHeat > 0.1f) {
                Color heatColor = new Color(255, 150, 50) * (reentryHeat * 0.4f * glowPulse);
                Vector2 heatPos = center - new Vector2(0, podTex.Height * 0.35f);
                DrawSoftGlow(sb, heatPos, heatColor, 60f + reentryHeat * 30f);
            }
        }

        /// <summary>
        /// 再入灼烧光效——空降仓周围的热辉光
        /// </summary>
        private static void DrawReentryHeatEffect(SpriteBatch sb) {
            if (reentryHeat < 0.05f) return;

            Vector2 center = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f) + shakeOffset;
            float pulse = MathF.Sin(dropTimer * 0.08f) * 0.2f + 0.8f;

            //橙红灼烧光圈
            DrawSoftGlow(sb, center - new Vector2(0, 20),
                new Color(200, 100, 30) * (reentryHeat * 0.25f * pulse), 120f);

            //外层蓝紫等离子体光圈
            DrawSoftGlow(sb, center,
                new Color(60, 80, 200) * (reentryHeat * 0.15f * pulse), 160f);
        }

        /// <summary>
        /// 绘制尾焰粒子
        /// </summary>
        private static void DrawTrailParticles(SpriteBatch sb) {
            if (CWRAsset.SoftGlow == null || CWRAsset.SoftGlow.IsDisposed) return;
            Texture2D glow = CWRAsset.SoftGlow.Value;

            foreach (var p in trailParticles) {
                float lifeRatio = (float)p.Life / p.MaxLife;
                float alpha = p.Alpha * (1f - lifeRatio * lifeRatio);
                Color c = p.Color * alpha;
                float scale = p.Scale * (1f + lifeRatio * 0.5f) * 0.04f;
                sb.Draw(glow, p.Position, null, c with { A = 0 }, 0f, glow.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>
        /// 速度线——模拟极速下坠时的速度感，从中心向外辐射
        /// </summary>
        private static void DrawSpeedLines(SpriteBatch sb) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float speedAlpha = MathHelper.Clamp(dropTimer / 120f, 0f, 0.6f);

            int lineCount = (int)(20 + reentryHeat * 30);
            int seed = dropTimer / 3;//每3帧更新一次种子，避免太频繁闪烁
            for (int i = 0; i < lineCount; i++) {
                //使用确定性随机避免闪烁
                int lineSeed = seed * 13 + i * 7919;
                float hash1 = MathF.Abs(MathF.Sin(lineSeed * 0.127f));
                float hash2 = MathF.Abs(MathF.Sin(lineSeed * 0.283f));
                float hash3 = MathF.Abs(MathF.Sin(lineSeed * 0.419f));

                float angle = hash1 * MathHelper.TwoPi;
                float dist = 80f + hash2 * (Main.screenWidth * 0.4f);
                float lineLength = 20f + hash3 * 60f;
                float lineWidth = 1f + hash3 * 1.5f;

                Vector2 center = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
                Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                Vector2 lineStart = center + dir * dist;
                Vector2 lineEnd = lineStart + dir * lineLength;

                float lineAlpha = speedAlpha * (0.3f + hash2 * 0.5f);
                Color lineColor = Color.Lerp(
                    new Color(200, 220, 255),
                    new Color(255, 180, 100),
                    reentryHeat * hash3) * lineAlpha;

                //绘制线段
                Vector2 diff = lineEnd - lineStart;
                float rot = diff.ToRotation();
                float len = diff.Length();
                sb.Draw(px, lineStart, new Rectangle(0, 0, 1, 1), lineColor, rot,
                    new Vector2(0, 0.5f), new Vector2(len, lineWidth), SpriteEffects.None, 0f);
            }
        }

        private static void DrawSoftGlow(SpriteBatch sb, Vector2 center, Color color, float radius) {
            if (CWRAsset.SoftGlow == null || CWRAsset.SoftGlow.IsDisposed) return;
            Texture2D glow = CWRAsset.SoftGlow.Value;
            float scale = radius / (glow.Width * 0.5f);
            sb.Draw(glow, center, null, color with { A = 0 }, 0f, glow.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }

        public override void Unload() {
            trailParticles.Clear();
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
