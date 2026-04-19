using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TimeShift;
using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.GlitchWraith
{
    /// <summary>
    /// 鬼乱码：仅在过去视角下可见的致命威胁
    /// 遵循123木头人规则，玩家在过去视角下注视时绝对静止，其余时间缓慢移动并偶发乱码瞬移
    /// 完全无视地形碰撞
    /// </summary>
    internal class GlitchWraithActor : Actor
    {
        //缓慢的基础爬行速度，像素每帧
        private const float CreepSpeed = 1.4f;
        //乱码瞬移参数，随机间隔触发一次短距离闪移
        private const int TeleportIntervalMin = 150;
        private const int TeleportIntervalMax = 300;
        private const float TeleportDistanceMin = 40f;
        private const float TeleportDistanceMax = 120f;
        //瞬移后的乱码闪烁余辉帧数
        private const int TeleportFlashFrames = 16;

        //玩家注视判定所需的屏幕内安全边距
        private const int ScreenMargin = 64;
        //触碰判定可见度阈值
        private const float ContactVisibility = 0.6f;
        //过去视角淡入速度与现在视角淡出速度
        private const float FadeInStep = 0.025f;
        private const float FadeOutStep = 0.08f;

        //失真影响的距离范围，越近失真越强
        private const float DistortionMaxDistance = 600f;
        private const float DistortionMinDistance = 100f;

        /// <summary>当前可见度0到1，过去视角下逼近1</summary>
        private float visibility;
        /// <summary>过去视角下是否被玩家注视，冻结中</summary>
        private bool isWatched;
        /// <summary>瞬移冷却帧数</summary>
        private int teleportTimer;
        /// <summary>瞬移后余辉闪烁帧数</summary>
        private int teleportFlash;
        /// <summary>头部乱码每帧滚动的随机种子</summary>
        private int glitchSeed;
        /// <summary>目标玩家whoAmI</summary>
        private int targetPlayer = -1;
        /// <summary>是否已触发处决演出，避免重复</summary>
        private bool hasExecuted;

        public new Vector2 Center {
            get => Position + new Vector2(Width * 0.5f, Height * 0.5f);
            set => Position = value - new Vector2(Width * 0.5f, Height * 0.5f);
        }

        public override void OnSpawn(params object[] args) {
            Width = 204;
            Height = 504;
            DrawLayer = ActorDrawLayer.AfterTiles;
            DrawExtendMode = 600;
            visibility = 0f;
            teleportTimer = Main.rand.Next(TeleportIntervalMin, TeleportIntervalMax);
            glitchSeed = Main.rand.Next(int.MaxValue);
        }

        public override void AI() {
            if (!VoidColony.Active || hasExecuted) {
                Velocity = Vector2.Zero;
                return;
            }

            Player target = AcquireTarget();
            if (target == null) {
                visibility = MathF.Max(0f, visibility - FadeOutStep);
                Velocity = Vector2.Zero;
                return;
            }

            bool inPast = VoidTimeShiftSystem.InPast;
            //现在视角下鬼乱码完全不可见，因此不参与注视判定
            isWatched = inPast && IsBeingWatched(target);

            //可见度根据时代平滑变化
            float targetVis = inPast ? 1f : 0f;
            if (visibility < targetVis) {
                visibility = MathF.Min(targetVis, visibility + FadeInStep);
            }
            else if (visibility > targetVis) {
                visibility = MathF.Max(targetVis, visibility - FadeOutStep);
            }

            if (isWatched) {
                //绝对静止
                Velocity = Vector2.Zero;
            }
            else {
                Vector2 toTarget = target.Center - Center;
                float dist = toTarget.Length();
                if (dist <= 1f) {
                    Velocity = Vector2.Zero;
                }
                else {
                    Vector2 dir = toTarget / dist;
                    //始终缓慢蠕动，速度不随时代改变
                    Velocity = dir * MathF.Min(CreepSpeed, dist);

                    //乱码瞬移倒计时推进，触发时一次性跳跃一小段距离
                    teleportTimer--;
                    if (teleportTimer <= 0) {
                        teleportTimer = Main.rand.Next(TeleportIntervalMin, TeleportIntervalMax);
                        float teleDist = Main.rand.NextFloat(TeleportDistanceMin, TeleportDistanceMax);
                        teleDist = MathF.Min(teleDist, MathF.Max(0f, dist - 80f));
                        if (teleDist > 0f) {
                            Position += dir * teleDist;
                            teleportFlash = TeleportFlashFrames;
                            //瞬移同帧重置种子，让余辉闪烁更离散
                            glitchSeed = Main.rand.Next(int.MaxValue);
                        }
                    }
                }
            }

            if (teleportFlash > 0) teleportFlash--;

            glitchSeed = unchecked(glitchSeed * 1664525 + 1013904223);

            //过去视角下与玩家碰撞触发处决
            if (inPast && visibility > ContactVisibility && !target.dead && !target.immune) {
                Rectangle myBox = new((int)Position.X, (int)Position.Y, Width, Height);
                if (myBox.Intersects(target.Hitbox)) {
                    ExecutePlayer(target);
                }
            }
        }

        /// <summary>
        /// 选择最近的活着玩家作为目标
        /// </summary>
        private Player AcquireTarget() {
            if (targetPlayer >= 0 && targetPlayer < Main.maxPlayers) {
                Player p = Main.player[targetPlayer];
                if (p.active && !p.dead) return p;
            }
            Player best = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++) {
                Player p = Main.player[i];
                if (!p.active || p.dead) continue;
                float d = (p.Center - Center).LengthSquared();
                if (d < bestDistSq) {
                    bestDistSq = d;
                    best = p;
                    targetPlayer = i;
                }
            }
            return best;
        }

        /// <summary>
        /// 判定本地玩家是否正在注视鬼乱码
        /// 必要条件过去视角鬼在屏幕内玩家朝向指向鬼一侧
        /// </summary>
        private bool IsBeingWatched(Player player) {
            if (player.whoAmI != Main.myPlayer) return false;

            Vector2 screenCenter = Center - Main.screenPosition;
            if (screenCenter.X < -ScreenMargin || screenCenter.X > Main.screenWidth + ScreenMargin) return false;
            if (screenCenter.Y < -ScreenMargin || screenCenter.Y > Main.screenHeight + ScreenMargin) return false;

            int dx = MathF.Sign(Center.X - player.Center.X);
            if (dx != 0 && player.direction != dx) return false;

            return true;
        }

        /// <summary>
        /// 触发处决演出
        /// </summary>
        private void ExecutePlayer(Player player) {
            if (hasExecuted) return;
            hasExecuted = true;
            GlitchWraithDeathSequence.Trigger(player);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            if (visibility <= 0.01f) return false;
            Texture2D tex = ADVAsset.Glitchwraith;
            if (tex == null) return false;

            Vector2 drawPos = Center - Main.screenPosition;
            Vector2 origin = new(tex.Width * 0.5f, tex.Height * 0.5f);

            //瞬移后余辉闪烁，分两层红紫残影左右抖动位移
            if (teleportFlash > 0) {
                float flashT = teleportFlash / (float)TeleportFlashFrames;
                float off = flashT * 10f;
                Color redGhost = new Color(255, 40, 80) * (visibility * 0.55f * flashT);
                Color purpleGhost = new Color(180, 80, 255) * (visibility * 0.55f * flashT);
                spriteBatch.Draw(tex, drawPos + new Vector2(-off, 0f), null, redGhost, 0f, origin, 1f, SpriteEffects.None, 0f);
                spriteBatch.Draw(tex, drawPos + new Vector2(off, 0f), null, purpleGhost, 0f, origin, 1f, SpriteEffects.None, 0f);
            }

            //本体纹理
            Color baseColor = Color.White * visibility;
            spriteBatch.Draw(tex, drawPos, null, baseColor, 0f, origin, 1f, SpriteEffects.None, 0f);

            //头部球形乱码：使用着色器生成团状有机乱码斑块
            DrawGlitchHead(spriteBatch, drawPos, origin, tex.Width);

            return false;
        }

        /// <summary>
        /// 使用GlitchHead着色器绘制头顶球形乱码区域
        /// 中心位于纹理头部附近，覆盖区域为边长约纹理宽度1.6倍的正方形
        /// </summary>
        private void DrawGlitchHead(SpriteBatch sb, Vector2 drawPos, Vector2 origin, int texW) {
            Effect shader = EffectLoader.GlitchHead?.Value;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            if (shader == null || pixel == null) return;

            //头部乱码正方形尺寸与锚点：覆盖纹理顶部区域并向上溢出
            int size = (int)(texW * 1.6f);
            Vector2 topCenter = new(drawPos.X, drawPos.Y - origin.Y + texW * 0.08f);
            Vector2 quadTopLeft = topCenter - new Vector2(size * 0.5f, size * 0.6f);
            Rectangle dest = new((int)quadTopLeft.X, (int)quadTopLeft.Y, size, size);

            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["intensity"]?.SetValue(visibility);
            //块尺寸0.025相当于每个乱码块占40分之一UV，颗粒感明显
            shader.Parameters["pixelSize"]?.SetValue(0.025f);

            //结束当前批次，切换到Immediate以便绑定着色器
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);

            sb.Draw(pixel, dest, Color.White);

            //恢复默认批次配置，保证后续Actor正常绘制
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// 以本地玩家视角返回当前场景中最近鬼乱码带来的失真强度0到1
        /// 距离越近值越大，与时代视角无关，因为鬼即使在现在不可见也会持续靠近
        /// 过去视角下会做少量加成以强化氛围
        /// </summary>
        public static float GetLocalDistortionStrength() {
            List<GlitchWraithActor> list = ActorLoader.GetActiveActors<GlitchWraithActor>();
            if (list == null || list.Count == 0) return 0f;
            Player p = Main.LocalPlayer;
            if (p == null || !p.active) return 0f;

            float bestDistSq = float.MaxValue;
            float bestVis = 0f;
            foreach (GlitchWraithActor w in list) {
                float d = (w.Center - p.Center).LengthSquared();
                if (d < bestDistSq) {
                    bestDistSq = d;
                    bestVis = w.visibility;
                }
            }

            float dist = MathF.Sqrt(bestDistSq);
            float range = DistortionMaxDistance - DistortionMinDistance;
            //基础强度只取决于距离，现在视角下依然生效
            float t = 1f - MathHelper.Clamp((dist - DistortionMinDistance) / range, 0f, 1f);
            //过去视角下额外叠加最多30%强度，作为视觉氛围加成
            float eraBoost = 1f + 0.3f * bestVis;
            return MathHelper.Clamp(t * eraBoost, 0f, 1f);
        }
    }
}
