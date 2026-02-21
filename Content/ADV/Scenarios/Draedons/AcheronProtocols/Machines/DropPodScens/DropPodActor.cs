using CalamityOverhaul.Common;
using InnoVault.Actors;
using InnoVault.Trails;
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

        /// <summary>
        /// 尾焰Trail路径点，从空降仓底部向下延伸
        /// </summary>
        private const int FlameTrailPointCount = 24;
        private readonly Vector2[] flameTrailPoints = new Vector2[FlameTrailPointCount];
        private Trail flameTrail;

        /// <summary>
        /// 冲击波环列表——环绕仓头的大气压缩波
        /// </summary>
        private readonly List<ShockwaveRing> shockwaveRings = [];

        //常量
        private const int MaxTrailParticles = 60;
        private const int MaxShockwaveRings = 6;
        private const float ShakeIntensityBase = 1.5f;
        private const float ShakeIntensityMax = 4f;

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
            Rotation = 0;

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

            //更新尾焰Trail路径点
            UpdateFlameTrailPoints();

            //生成冲击波环（再入灼烧达到一定强度后开始产生）
            if (reentryHeat > 0.15f && dropTimer % Math.Max(8, (int)(30 * (1f - reentryHeat * 0.7f))) == 0
                && shockwaveRings.Count < MaxShockwaveRings) {
                SpawnShockwaveRing();
            }

            //更新冲击波环
            for (int i = shockwaveRings.Count - 1; i >= 0; i--) {
                shockwaveRings[i].Update();
                if (shockwaveRings[i].IsDead) {
                    shockwaveRings.RemoveAt(i);
                }
            }

            //同步dropTimer给DropPodDrawSystem的屏幕特效使用
            DropPodDrawSystem.SyncDropTimer(dropTimer, reentryHeat);
        }

        /// <summary>
        /// 更新尾焰路径点，从火焰末端到仓底喷口，笔直向下喷射
        /// Trail约定：[0]=末尾(火焰远端)，[Length-1]=起点(喷口)
        /// </summary>
        private void UpdateFlameTrailPoints() {
            Vector2 podBottom = Center - new Vector2(0, 860) + shakeOffset;

            //火焰向下延伸的总长度，随时间逐渐增长
            float flameLength = MathHelper.Clamp(dropTimer / 60f, 0.3f, 1f) * 880f;

            for (int i = 0; i < FlameTrailPointCount; i++) {
                //反转：i=0对应火焰最远端，i=max对应喷口
                float t = 1f - i / (float)(FlameTrailPointCount - 1);

                //基础纵向位置——笔直向下
                float y = podBottom.Y + t * flameLength;

                //末端极轻微的热扰动（仅在远离喷口的位置），模拟热气扩散而非蛇形扭动
                float disturbance = t * t * 2f;
                float jitterX = MathF.Sin(dropTimer * 0.2f + t * 20f) * disturbance;

                flameTrailPoints[i] = new Vector2(podBottom.X + jitterX, y);
            }
        }

        private void SpawnShockwaveRing() {
            Texture2D podTex = DropPod.DropPodAsset?.Value;
            float podHalfHeight = podTex != null ? podTex.Height * 0.5f : 40f;

            //环生成在仓头前方，略有随机偏移
            Vector2 ringCenter = Center + new Vector2(0, podHalfHeight + Main.rand.NextFloat(0, 15f));

            shockwaveRings.Add(new ShockwaveRing {
                WorldCenter = ringCenter,
                Life = 0,
                MaxLife = Main.rand.Next(25, 45),
                MaxRadius = 50f + reentryHeat * 60f + Main.rand.NextFloat(-10f, 10f),
                Thickness = 0.08f + Main.rand.NextFloat(0, 0.04f),
                Intensity = 0.4f + reentryHeat * 0.6f
            });
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

            //绘制冲击波环（在仓体后面，营造大气压缩效果）
            DrawShockwaveRings(spriteBatch);

            //绘制尾焰Trail（在粒子和仓体之前，作为主火焰效果）
            DrawFlameTrail(spriteBatch);

            //绘制尾焰粒子
            DrawTrailParticles(spriteBatch);

            //绘制空降仓主体
            DrawDropPod(spriteBatch, drawCenter);

            return false;
        }

        /// <summary>
        /// 使用Trail和自定义着色器绘制尾焰效果
        /// </summary>
        private void DrawFlameTrail(SpriteBatch spriteBatch) {
            if (dropTimer < 10) return;
            if (EffectLoader.DropPodFlame == null || !EffectLoader.DropPodFlame.IsLoaded) return;

            //初始化或更新Trail
            flameTrail ??= new Trail(flameTrailPoints, GetFlameTrailWidth, GetFlameTrailColor);
            flameTrail.TrailPositions = flameTrailPoints;

            //切换到顶点绘制模式——与BaseSwing.DrawSlashTrail、DestroyerRenderHelper一致的模式
            spriteBatch.End();

            Effect effect = EffectLoader.DropPodFlame.Value;
            effect.Parameters["transformMatrix"].SetValue(VaultUtils.GetTransfromMatrix());
            effect.Parameters["globalTime"].SetValue((float)Main.timeForVisualEffects * 0.02f);
            effect.Parameters["heatIntensity"].SetValue(reentryHeat);
            effect.Parameters["uNoise"].SetValue(CWRAsset.Extra_193.Value);

            Main.graphics.GraphicsDevice.BlendState = BlendState.Additive;
            flameTrail.DrawTrail(effect);
            flameTrail.DrawTrail(effect);
            Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;

            //恢复SpriteBatch——与项目中其他End/Begin对保持一致
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// 尾焰Trail宽度函数：根部宽、末端窄，呈火焰锥形
        /// </summary>
        private float GetFlameTrailWidth(float progress) {
            //progress: 0=起点(仓底), 1=末端
            float intensityFactor = MathHelper.Clamp(dropTimer / 120f, 0.3f, 1f);

            //火焰根部宽，末端快速收窄
            float baseWidth = 28f + reentryHeat * 12f;
            float taper = 1f - MathF.Pow(progress, 0.6f);

            //添加脉动感
            float pulse = 1f + MathF.Sin(dropTimer * 0.15f + progress * 4f) * 0.08f;

            return baseWidth * taper * intensityFactor * pulse;
        }

        /// <summary>
        /// 尾焰Trail颜色函数：根部白黄，中段橙色，末端暗红渐隐
        /// </summary>
        private Color GetFlameTrailColor(Vector2 texCoords) {
            float progress = texCoords.X; //沿火焰长度方向
            float intensityFactor = MathHelper.Clamp(dropTimer / 120f, 0.3f, 1f);

            //三段颜色渐变
            Color coreColor = new Color(255, 240, 200);   //白黄核心
            Color midColor = new Color(255, 140, 40);     //橙色中段
            Color tipColor = new Color(180, 40, 10);      //暗红末端

            Color result;
            if (progress < 0.3f) {
                result = Color.Lerp(coreColor, midColor, progress / 0.3f);
            }
            else {
                result = Color.Lerp(midColor, tipColor, (progress - 0.3f) / 0.7f);
            }

            //末端透明度衰减
            float alpha = (1f - MathF.Pow(progress, 1.5f)) * intensityFactor;

            //再入灼烧时整体更亮
            float heatBoost = 1f + reentryHeat * 0.5f;

            return result * alpha * heatBoost;
        }

        /// <summary>
        /// 绘制大气再入冲击波环——从仓头向外扩散的压缩气流环
        /// 使用着色器渲染，带噪声扰动的环形效果
        /// </summary>
        private void DrawShockwaveRings(SpriteBatch spriteBatch) {
            if (shockwaveRings.Count == 0) return;
            if (CWRAsset.Placeholder_White == null || CWRAsset.Placeholder_White.IsDisposed) return;
            if (EffectLoader.DropPodShockwave == null || !EffectLoader.DropPodShockwave.IsLoaded) return;

            Texture2D canvas = CWRAsset.Placeholder_White.Value;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (var ring in shockwaveRings) {
                float progress = (float)ring.Life / ring.MaxLife;
                float currentRadius = ring.MaxRadius * MathHelper.SmoothStep(0.2f, 1f, progress);
                float alpha = MathF.Sin(progress * MathHelper.Pi) * ring.Intensity;

                Vector2 drawPos = ring.WorldCenter - Main.screenPosition + shakeOffset;

                //透视压缩比——横向宽、纵向窄，模拟从正上方俯视的水平环
                const float perspectiveSquish = 0.45f;

                Effect effect = EffectLoader.DropPodShockwave.Value;
                effect.Parameters["globalTime"].SetValue((float)Main.timeForVisualEffects * 0.015f);
                effect.Parameters["shockwaveIntensity"].SetValue(alpha);
                effect.Parameters["ringRadius"].SetValue(MathHelper.Lerp(0.3f, 0.85f, progress));
                effect.Parameters["ringThickness"].SetValue(ring.Thickness * (1f + (1f - progress) * 0.5f));
                effect.Parameters["squishY"].SetValue(perspectiveSquish);
                effect.Parameters["uNoise"].SetValue(CWRAsset.Extra_193.Value);
                effect.CurrentTechnique.Passes[0].Apply();

                //非等比缩放：X方向完整半径，Y方向压缩产生透视椭圆
                float drawSize = currentRadius * 2f;
                Vector2 drawScale = new Vector2(drawSize, drawSize * perspectiveSquish);
                Color ringColor = Color.Lerp(
                    new Color(180, 210, 255),
                    new Color(255, 200, 150),
                    reentryHeat * 0.6f) * alpha;
                spriteBatch.Draw(canvas, drawPos, null, ringColor,
                    0f, canvas.Size() * 0.5f, drawScale, SpriteEffects.None, 0f);
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
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

        /// <summary>
        /// 冲击波环数据——模拟大气再入时仓头前方的弓形激波
        /// </summary>
        private class ShockwaveRing
        {
            /// <summary>环中心的世界坐标</summary>
            public Vector2 WorldCenter;
            /// <summary>当前生命帧</summary>
            public int Life;
            /// <summary>最大生命帧</summary>
            public int MaxLife;
            /// <summary>完全展开时的半径（像素）</summary>
            public float MaxRadius;
            /// <summary>环的厚度比例 0~1</summary>
            public float Thickness;
            /// <summary>亮度强度</summary>
            public float Intensity;
            public bool IsDead => Life >= MaxLife;

            public void Update() {
                Life++;
            }
        }
    }
}
