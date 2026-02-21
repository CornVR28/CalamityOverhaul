using InnoVault.Actors;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.DropPodScens
{
    /// <summary>
    /// 空降仓坠落演出中的残骸障碍物Actor
    /// 流程：预警线阶段 → 残骸从屏幕底部向上高速飞过
    /// 如果与空降仓碰撞则产生火花粒子效果
    /// </summary>
    internal class DropPodDebrisActor : Actor
    {
        private const string ExoGorePath = "CalamityOverhaul/Content/ADV/Scenarios/Draedons/AcheronProtocols/Machines/ExoGores/";

        private static readonly string[] ExoGoreNames = [
            "Apollo1", "Apollo2", "Apollo3", "Apollo4", "Apollo5",
            "AresArm_Gore1", "AresArm_Gore2", "AresArm_Gore3",
            "AresBody1", "AresBody2", "AresBody3", "AresBody4", "AresBody5", "AresBody6", "AresBody7",
            "AresGaussNuke1", "AresGaussNuke2", "AresGaussNuke3",
            "AresHandBase1", "AresHandBase2", "AresHandBase3",
            "AresLaserCannon1", "AresLaserCannon2",
            "AresPlasmaFlamethrower1", "AresPlasmaFlamethrower2",
            "AresTeslaCannon1", "AresTeslaCannon2",
            "Artemis1", "Artemis2", "Artemis3", "Artemis4", "Artemis5",
            "ThanatosBody1", "ThanatosBody1_2", "ThanatosBody1_3",
            "ThanatosBody2", "ThanatosBody2_2", "ThanatosBody2_3",
            "ThanatosHead", "ThanatosHead2", "ThanatosHead3",
            "ThanatosTail", "ThanatosTail2", "ThanatosTail3", "ThanatosTail4"
        ];

        private static Asset<Texture2D>[] exoGoreAssets;

        internal static void LoadAssets() {
            if (VaultUtils.isServer) return;
            exoGoreAssets = new Asset<Texture2D>[ExoGoreNames.Length];
            for (int i = 0; i < ExoGoreNames.Length; i++) {
                exoGoreAssets[i] = CWRMod.Instance.Assets.Request<Texture2D>(
                    ExoGorePath.Replace("CalamityOverhaul/", "") + ExoGoreNames[i],
                    AssetRequestMode.AsyncLoad);
            }
        }

        internal static void UnloadAssets() {
            exoGoreAssets = null;
        }

        //阶段
        private enum DebrisPhase { Warning, Flying, Done }
        private DebrisPhase phase;

        //预警线
        private int warningTimer;
        private const int WarningDuration = 35;
        private float warningLineWidth;
        private float warningLineAlpha;

        //残骸飞行
        private float flySpeed;
        private float debrisRotation;
        private float debrisRotationSpeed;
        private float debrisScale;
        private int textureIndex;
        private bool flipH;
        private Color debrisTint;

        //碰撞检测
        private bool hasHit;
        private const float HitRadius = 60f;

        //火花粒子
        private readonly List<SparkParticle> sparks = [];
        private const int MaxSparks = 40;

        public override void OnSpawn(params object[] args) {
            Width = 20;
            Height = 20;
            DrawExtendMode = 800;
            DrawLayer = ActorDrawLayer.Default;

            phase = DebrisPhase.Warning;
            warningTimer = 0;
            warningLineWidth = 0f;
            warningLineAlpha = 0f;
            hasHit = false;

            flySpeed = Main.rand.NextFloat(14f, 22f);
            debrisRotation = Main.rand.NextFloat(MathHelper.TwoPi);
            debrisRotationSpeed = Main.rand.NextFloat(-0.03f, 0.03f);
            debrisScale = Main.rand.NextFloat(0.5f, 0.9f);
            textureIndex = exoGoreAssets != null ? Main.rand.Next(exoGoreAssets.Length) : 0;
            flipH = Main.rand.NextBool();

            float brightness = Main.rand.NextFloat(0.4f, 0.7f);
            debrisTint = new Color(brightness * 0.9f, brightness * 0.95f, brightness * 1.1f);
        }

        public override void AI() {
            if (!DropPodWorld.Active) {
                ActorLoader.KillActor(WhoAmI);
                return;
            }

            switch (phase) {
                case DebrisPhase.Warning:
                    UpdateWarning();
                    break;
                case DebrisPhase.Flying:
                    UpdateFlying();
                    break;
                case DebrisPhase.Done:
                    UpdateDone();
                    break;
            }

            //更新火花粒子
            for (int i = sparks.Count - 1; i >= 0; i--) {
                sparks[i].Update();
                if (sparks[i].IsDead) {
                    sparks.RemoveAt(i);
                }
            }
        }

        private void UpdateWarning() {
            warningTimer++;

            //预警线快速扩展变宽然后收缩
            float progress = (float)warningTimer / WarningDuration;

            if (progress < 0.5f) {
                //快速扩展
                float expandProgress = progress / 0.5f;
                warningLineWidth = MathHelper.Lerp(0f, 8f, MathF.Pow(expandProgress, 0.5f));
                warningLineAlpha = MathHelper.Lerp(0f, 0.9f, expandProgress);
            }
            else {
                //保持并闪烁
                float flickerProgress = (progress - 0.5f) / 0.5f;
                warningLineWidth = 8f - flickerProgress * 4f;
                warningLineAlpha = 0.9f * (1f - flickerProgress * 0.3f);
                //闪烁效果
                warningLineAlpha *= 0.7f + MathF.Sin(warningTimer * 0.8f) * 0.3f;
            }

            if (warningTimer >= WarningDuration) {
                phase = DebrisPhase.Flying;
            }
        }

        private void UpdateFlying() {
            //向上飞行（Y减小）
            Position.Y -= flySpeed;
            debrisRotation += debrisRotationSpeed;

            //预警线淡出
            warningLineAlpha *= 0.85f;
            warningLineWidth *= 0.9f;

            //碰撞检测——检查是否与空降仓碰撞
            if (!hasHit) {
                var podActors = ActorLoader.GetActiveActors<DropPodActor>();
                foreach (var podActor in podActors) {
                    Vector2 podCenter = podActor.Center;
                    float dist = Vector2.Distance(Center, podCenter);
                    if (dist < HitRadius + 30f) {
                        OnHitPod(podCenter);
                        hasHit = true;
                        break;
                    }
                }
            }

            //飞出屏幕上方后销毁
            Vector2 screenPos = Center - Main.screenPosition;
            if (screenPos.Y < -400) {
                if (sparks.Count == 0) {
                    phase = DebrisPhase.Done;
                }
            }
        }

        private void UpdateDone() {
            //等待所有火花消失后销毁
            if (sparks.Count == 0) {
                ActorLoader.KillActor(WhoAmI);
            }
        }

        /// <summary>
        /// 残骸击中空降仓时的效果
        /// </summary>
        private void OnHitPod(Vector2 podCenter) {
            //在碰撞点生成大量火花粒子
            Vector2 hitPoint = (Center + podCenter) * 0.5f;
            int sparkCount = Main.rand.Next(20, MaxSparks);

            for (int i = 0; i < sparkCount; i++) {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float speed = Main.rand.NextFloat(3f, 12f);
                Vector2 sparkVel = angle.ToRotationVector2() * speed;

                sparkVel.Y -= Main.rand.NextFloat(2f, 6f);

                Color sparkColor = Color.Lerp(
                    new Color(255, 220, 100),
                    new Color(255, 140, 40),
                    Main.rand.NextFloat());

                sparks.Add(new SparkParticle {
                    Position = hitPoint + Main.rand.NextVector2Circular(10f, 10f),
                    Velocity = sparkVel,
                    Color = sparkColor,
                    Alpha = Main.rand.NextFloat(0.7f, 1f),
                    Scale = Main.rand.NextFloat(1.5f, 4f),
                    Life = 0,
                    MaxLife = Main.rand.Next(15, 40)
                });
            }

            //给空降仓增加额外的震动
            Player player = Main.LocalPlayer;
            if (player != null && player.active) {
                player.CWR().GetScreenShake(6f);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            //绘制预警线
            if (phase == DebrisPhase.Warning || (phase == DebrisPhase.Flying && warningLineAlpha > 0.01f)) {
                DrawWarningLine(spriteBatch);
            }

            //绘制残骸
            if (phase == DebrisPhase.Flying) {
                DrawDebris(spriteBatch);
            }

            //绘制火花粒子
            DrawSparks(spriteBatch);

            return false;
        }

        private void DrawWarningLine(SpriteBatch sb) {
            if (warningLineAlpha < 0.01f) return;
            Texture2D px = VaultAsset.placeholder2.Value;

            Vector2 lineScreenX = new Vector2(Center.X - Main.screenPosition.X, 0);
            float lineHeight = Main.screenHeight;

            //主预警线——红色竖线
            Color warningColor = new Color(255, 60, 60) * warningLineAlpha;
            sb.Draw(px, lineScreenX, new Rectangle(0, 0, 1, 1), warningColor,
                0f, new Vector2(0.5f, 0f), new Vector2(warningLineWidth, lineHeight), SpriteEffects.None, 0f);

            //外层辉光
            Color glowColor = new Color(255, 100, 60) * (warningLineAlpha * 0.4f);
            sb.Draw(px, lineScreenX, new Rectangle(0, 0, 1, 1), glowColor,
                0f, new Vector2(0.5f, 0f), new Vector2(warningLineWidth * 3f, lineHeight), SpriteEffects.None, 0f);

            //中心亮线
            Color coreColor = new Color(255, 200, 150) * (warningLineAlpha * 0.8f);
            sb.Draw(px, lineScreenX, new Rectangle(0, 0, 1, 1), coreColor,
                0f, new Vector2(0.5f, 0f), new Vector2(MathF.Max(1f, warningLineWidth * 0.3f), lineHeight), SpriteEffects.None, 0f);
        }

        private void DrawDebris(SpriteBatch sb) {
            if (exoGoreAssets == null || textureIndex < 0 || textureIndex >= exoGoreAssets.Length) return;

            var asset = exoGoreAssets[textureIndex];
            if (asset == null || !asset.IsLoaded) return;

            Texture2D tex = asset.Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 drawPos = Center - Main.screenPosition;
            SpriteEffects fx = flipH ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            //冷色边缘光（rim light）
            Color rimColor = new Color(60, 120, 200) * 0.5f;
            float rimOffset = 1.5f;
            for (int i = 0; i < 4; i++) {
                float angle = MathHelper.PiOver2 * i;
                Vector2 off = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * rimOffset;
                sb.Draw(tex, drawPos + off, null, rimColor, debrisRotation, origin, debrisScale, fx, 0f);
            }

            //主体
            sb.Draw(tex, drawPos, null, Color.White, debrisRotation, origin, debrisScale, fx, 0f);

            //运动模糊拖影（向下方向，因为残骸向上飞，拖影在下方）
            for (int t = 1; t <= 3; t++) {
                Vector2 trailPos = drawPos + new Vector2(0, flySpeed * t * 2f);
                float trailAlpha = 1f - t / 4f;
                sb.Draw(tex, trailPos, null, debrisTint * (trailAlpha * 0.2f), debrisRotation, origin, debrisScale, fx, 0f);
            }
        }

        private void DrawSparks(SpriteBatch sb) {
            if (sparks.Count == 0) return;
            if (CWRAsset.SoftGlow == null || CWRAsset.SoftGlow.IsDisposed) return;
            Texture2D glow = CWRAsset.SoftGlow.Value;

            foreach (var spark in sparks) {
                float lifeRatio = (float)spark.Life / spark.MaxLife;
                float alpha = spark.Alpha * (1f - lifeRatio * lifeRatio);
                Color c = spark.Color * alpha;
                float scale = spark.Scale * (1f - lifeRatio * 0.5f) * 0.06f;
                Vector2 drawPos = spark.Position - Main.screenPosition;

                //火花拉伸——沿速度方向
                float sparkRot = spark.Velocity.ToRotation() + MathHelper.PiOver2;
                float stretch = MathF.Min(spark.Velocity.Length() * 0.15f, 2f);
                Vector2 sparkScale = new Vector2(scale, scale * (1f + stretch));

                sb.Draw(glow, drawPos, null, c with { A = 0 }, sparkRot, glow.Size() * 0.5f, sparkScale, SpriteEffects.None, 0f);
            }
        }

        private class SparkParticle
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
                Velocity *= 0.94f;
                Velocity.Y -= 0.65f;
                Velocity.X += Main.rand.NextFloat(-0.2f, 0.2f);
            }
        }
    }

    /// <summary>
    /// 残骸Actor资源加载器
    /// </summary>
    internal class DropPodDebrisLoader : ICWRLoader
    {
        void ICWRLoader.LoadData() => DropPodDebrisActor.LoadAssets();
        void ICWRLoader.UnLoadData() => DropPodDebrisActor.UnloadAssets();
    }
}
