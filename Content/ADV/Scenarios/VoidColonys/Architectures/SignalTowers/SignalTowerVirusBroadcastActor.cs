using CalamityOverhaul.Common;
using CalamityOverhaul.Content.HackTimes;
using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.SignalTowers
{
    /// <summary>
    /// 信号塔病毒广播Actor
    /// 由信号塔在被骇入后Spawn，以自身为圆心向外扩散一道赛博电磁冲击波
    /// 画布以广播中心为锚点，波前到达位置时命中范围内的可骇入炮台会被长时间短路
    /// 纯程序化着色器绘制，不依赖外部贴图
    /// </summary>
    internal class SignalTowerVirusBroadcastActor : Actor
    {
        /// <summary>广播总帧长，Spawn时写入</summary>
        [SyncVar]
        public int InitLifeFrames;
        /// <summary>广播覆盖半径px，Spawn时写入</summary>
        [SyncVar]
        public float InitRadius;
        /// <summary>命中炮台的短路帧数，Spawn时写入</summary>
        [SyncVar]
        public int InitDisableFrames;
        /// <summary>随机种子，驱动程序化噪声</summary>
        [SyncVar]
        public float InitSeed;

        private int maxLife;
        private int life;
        //已在本次广播中命中过的炮台集合，防止一次广播内重复命中
        private readonly HashSet<IHackableTurret> hitSet = [];

        /// <summary>广播中心在世界中的像素坐标，由Position+画布一半算出</summary>
        private Vector2 BroadcastCenter => Position + new Vector2(InitRadius, InitRadius);

        public override void OnSpawn(params object[] args) {
            DrawLayer = ActorDrawLayer.Default;
            //画布为以广播中心为圆心、半径为InitRadius的正方形外接
            int size = (int)Math.Ceiling(InitRadius * 2f);
            Width = size;
            Height = size;
            DrawExtendMode = size;
            maxLife = InitLifeFrames > 0 ? InitLifeFrames : 90;
            life = maxLife;
        }

        public override void AI() {
            Velocity = Vector2.Zero;
            if (!VoidColony.Active) {
                ActorLoader.KillActor(WhoAmI);
                return;
            }
            life--;
            if (life <= 0) {
                ActorLoader.KillActor(WhoAmI);
                return;
            }

            //仅主控端触发命中，多人同步由ApplyShortCircuit内部逻辑处理即可
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            //当前波前半径
            float progress = 1f - life / (float)maxLife;
            float frontRadius = InitRadius * progress;

            var actors = ActorLoader.Actors;
            for (int i = 0; i < actors.Length; i++) {
                if (actors[i] is not IHackableTurret turret) continue;
                if (!turret.IsValid) continue;
                if (hitSet.Contains(turret)) continue;

                //以欧几里得距离判断波前是否扫过了该炮台
                float d = Vector2.Distance(turret.WorldCenter, BroadcastCenter);
                if (d <= frontRadius && d <= InitRadius) {
                    turret.ApplyShortCircuit(InitDisableFrames, null);
                    hitSet.Add(turret);
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            Effect shader = EffectLoader.SignalTowerVirusBroadcast?.Value;
            if (shader == null || maxLife <= 0 || InitRadius <= 1f) return false;

            float progress = 1f - life / (float)maxLife;
            //波前归一化半径：与AI中同步按线性扩张
            float waveProgress = MathHelper.Clamp(progress, 0f, 1f);
            //厚度随扩张稍微变薄，避免整个屏幕被涂满
            float thickness = MathHelper.Lerp(0.12f, 0.05f, waveProgress);
            //整体淡出：前段恒亮，后段线性衰减
            float fadeAlpha = 1f - MathHelper.Clamp((waveProgress - 0.55f) / 0.45f, 0f, 1f);

            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["waveProgress"]?.SetValue(waveProgress);
            shader.Parameters["waveThickness"]?.SetValue(thickness);
            shader.Parameters["fadeAlpha"]?.SetValue(fadeAlpha);
            shader.Parameters["seed"]?.SetValue(InitSeed);

            Vector2 drawPos = Position - Main.screenPosition;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);

            Texture2D px = TextureAssets.MagicPixel.Value;
            Rectangle dest = new((int)drawPos.X, (int)drawPos.Y, Width, Height);
            spriteBatch.Draw(px, dest, Color.White);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            //光源提示：波前经过时在中心抛出强烈的紫光
            if (waveProgress < 0.9f) {
                Lighting.AddLight(BroadcastCenter, 1.2f * fadeAlpha, 0.6f * fadeAlpha, 1.6f * fadeAlpha);
            }
            return false;
        }

        /// <summary>
        /// 由信号塔在BeginVirusBroadcast中调用，从塔顶中心Spawn一道广播Actor
        /// </summary>
        public static void Spawn(Vector2 center, float radiusPixels, int lifeFrames, int disableFrames) {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Vector2 topLeft = center - new Vector2(radiusPixels, radiusPixels);
            int idx = ActorLoader.NewActor<SignalTowerVirusBroadcastActor>(topLeft, Vector2.Zero);
            if (idx < 0 || idx >= ActorLoader.Actors.Length) return;
            if (ActorLoader.Actors[idx] is not SignalTowerVirusBroadcastActor wave) return;
            wave.InitLifeFrames = lifeFrames;
            wave.InitRadius = radiusPixels;
            wave.InitDisableFrames = disableFrames;
            wave.InitSeed = Main.rand.NextFloat() * 100f;
            wave.OnSpawn();
            wave.NetUpdate = true;

            //震撼低频的电磁脉冲音效
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.9f, Pitch = -0.3f }, center);
            SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.6f, Pitch = -0.2f }, center);
        }
    }
}
