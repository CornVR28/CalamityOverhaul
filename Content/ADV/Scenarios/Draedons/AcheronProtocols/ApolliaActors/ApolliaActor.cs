using CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines;
using InnoVault.Actors;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors
{
    /// <summary>
    /// 阿波利娅角色Actor——可扩展状态机架构
    /// 当前实现：着陆演出行为（从天而降 → 走向玩家 → 到达）
    /// </summary>
    internal class ApolliaActor : Actor
    {
        #region 状态定义

        /// <summary>
        /// 阿波利娅的顶层行为状态
        /// </summary>
        internal enum ApolliaState
        {
            /// <summary>未激活，等待外部触发</summary>
            Inactive,
            /// <summary>从天空降落到地面</summary>
            Descending,
            /// <summary>着地后走向玩家</summary>
            Walking,
            /// <summary>到达玩家面前</summary>
            Arrived,
        }

        #endregion

        #region 常量

        private const int TotalFrames = 11;
        private const int DescendDuration = 50;
        private const float WalkSpeed = 1.8f;
        private const float ArrivalDistance = 60f;
        private const int WalkFrameInterval = 6;

        #endregion

        #region 字段

        internal ApolliaState CurrentState;
        private int stateTimer;

        //动画
        private int frameIndex;
        private int frameCounter;
        private int frameWidth;
        private int frameHeight;

        //降落
        private Vector2 descendStart;
        private Vector2 descendTarget;
        private float descendAlpha;

        //行走
        private int walkDirection;
        private Vector2 targetPlayerPos;

        //运镜
        private bool cameraActive;
        private Vector2 cameraTarget;
        private float cameraLerpSpeed;
        private float targetZoom;
        private float currentZoom;
        private float zoomLerpSpeed;
        private Vector2 smoothedScreenPos;
        private bool smoothedScreenPosInitialized;

        //视觉效果
        private float glowIntensity;

        #endregion

        public override void OnSpawn(params object[] args) {
            Width = 30;
            Height = 50;
            DrawExtendMode = 600;
            DrawLayer = ActorDrawLayer.Default;

            CurrentState = ApolliaState.Inactive;
            stateTimer = 0;
            frameIndex = 0;
            frameCounter = 0;
            descendAlpha = 0f;
            cameraActive = false;
            currentZoom = 1f;
            targetZoom = 1f;
            zoomLerpSpeed = 0.02f;
            smoothedScreenPos = Vector2.Zero;
            smoothedScreenPosInitialized = false;
            glowIntensity = 0f;

            //预计算帧尺寸
            if (ADVAsset.ApolliaActor != null) {
                frameWidth = ADVAsset.ApolliaActor.Width;
                frameHeight = ADVAsset.ApolliaActor.Height / TotalFrames;
            }
        }

        /// <summary>
        /// 外部触发：开始着陆演出行为
        /// </summary>
        /// <param name="landingPodCenter">空降仓中心位置，用于计算降落点</param>
        internal void StartLandingCutscene(Vector2 landingPodCenter) {
            if (CurrentState != ApolliaState.Inactive) return;

            //在空降仓右侧约200像素处降落
            float offsetX = 200f;
            Vector2 rawTarget = new Vector2(landingPodCenter.X + offsetX, landingPodCenter.Y);
            descendTarget = FindGroundPosition(rawTarget);
            descendStart = descendTarget - new Vector2(0, 800);

            Position = descendStart;
            CurrentState = ApolliaState.Descending;
            stateTimer = 0;
            descendAlpha = 0f;
            frameIndex = 0;

            //启动运镜
            cameraActive = true;
            cameraTarget = descendTarget;
            cameraLerpSpeed = 0.03f;
            targetZoom = 1f;
            zoomLerpSpeed = 0.02f;
        }

        public override void AI() {
            if (!MachineWorld.Active) {
                ActorLoader.KillActor(WhoAmI);
                return;
            }

            stateTimer++;

            switch (CurrentState) {
                case ApolliaState.Inactive:
                    break;
                case ApolliaState.Descending:
                    UpdateDescending();
                    break;
                case ApolliaState.Walking:
                    UpdateWalking();
                    break;
                case ApolliaState.Arrived:
                    UpdateArrived();
                    break;
            }

            //运镜更新
            UpdateCamera();

            //辉光效果衰减
            if (glowIntensity > 0.01f) {
                glowIntensity *= 0.96f;
            }
        }

        #region 状态更新

        private void UpdateDescending() {
            float progress = MathHelper.Clamp((float)stateTimer / DescendDuration, 0f, 1f);
            float easedProgress = EaseOutQuad(progress);

            //位置插值：从天空到地面
            Position = Vector2.Lerp(descendStart, descendTarget, easedProgress);

            //淡入
            descendAlpha = MathHelper.Clamp(progress * 3f, 0f, 1f);

            //站立帧
            frameIndex = 0;

            //降落过程中产生粒子
            if (stateTimer % 3 == 0 && !VaultUtils.isServer) {
                Vector2 dustPos = Center + Main.rand.NextVector2Circular(15, 8);
                Dust dust = Dust.NewDustDirect(dustPos, 1, 1, DustID.Electric, 0, 2, 150, default, 0.6f);
                dust.noGravity = true;
                dust.velocity *= 0.4f;
            }

            //运镜：跟踪阿波利娅下降
            cameraTarget = Center;

            if (progress >= 1f) {
                OnLanded();
                TransitionTo(ApolliaState.Walking);
            }
        }

        private void OnLanded() {
            //着陆音效
            SoundEngine.PlaySound(SoundID.Item74 with {
                Volume = 0.6f,
                Pitch = 0.2f
            }, Center);

            //着陆粒子
            if (!VaultUtils.isServer) {
                for (int i = 0; i < 12; i++) {
                    Vector2 vel = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-4f, -1f));
                    Dust dust = Dust.NewDustDirect(Center + new Vector2(Main.rand.NextFloat(-15, 15), 20), 1, 1,
                        DustID.Smoke, vel.X, vel.Y, 150, default, Main.rand.NextFloat(1f, 1.8f));
                    dust.noGravity = true;
                }
            }

            glowIntensity = 0.8f;

            //确定行走方向
            Player player = Main.LocalPlayer;
            if (player != null && player.active) {
                targetPlayerPos = player.Center;
                walkDirection = Math.Sign(targetPlayerPos.X - Center.X);
                if (walkDirection == 0) walkDirection = -1;
            }

            cameraLerpSpeed = 0.025f;
        }

        private void UpdateWalking() {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return;

            targetPlayerPos = player.Center;
            float distToPlayer = Math.Abs(Center.X - targetPlayerPos.X);
            walkDirection = Math.Sign(targetPlayerPos.X - Center.X);
            if (walkDirection == 0) walkDirection = -1;

            //行走动画帧(帧1~10为行走动画)
            frameCounter++;
            if (frameCounter >= WalkFrameInterval) {
                frameCounter = 0;
                frameIndex++;
                if (frameIndex >= TotalFrames) {
                    frameIndex = 1;
                }
                if (frameIndex < 1) {
                    frameIndex = 1;
                }
            }

            //移动
            Position.X += WalkSpeed * walkDirection;

            //地面吸附
            ApplyGravityAndGroundSnap();

            //运镜：缓慢跟踪两者中点，随着距离缩小逐渐放大
            Vector2 midPoint = (Center + targetPlayerPos) * 0.5f;
            cameraTarget = midPoint;

            float zoomFactor = MathHelper.Clamp(1f - (distToPlayer - ArrivalDistance) / 400f, 0f, 1f);
            targetZoom = MathHelper.Lerp(1f, 1.5f, EaseInOutQuad(zoomFactor));
            zoomLerpSpeed = 0.015f;

            //到达
            if (distToPlayer <= ArrivalDistance) {
                TransitionTo(ApolliaState.Arrived);
            }

            //脚步声
            if (stateTimer % 20 == 0) {
                SoundEngine.PlaySound(SoundID.Run with {
                    Volume = 0.3f,
                    Pitch = 0.4f
                }, Center);
            }
        }

        private void UpdateArrived() {
            //站立帧
            frameIndex = 0;

            //面向玩家
            Player player = Main.LocalPlayer;
            if (player != null && player.active) {
                walkDirection = Math.Sign(player.Center.X - Center.X);
                if (walkDirection == 0) walkDirection = -1;
            }

            //运镜：锁定在阿波利娅面部，放大画面
            cameraTarget = Center + new Vector2(0, -30);
            targetZoom = 1.6f;
            zoomLerpSpeed = 0.02f;
            cameraLerpSpeed = 0.04f;

            //到达后逐渐释放运镜控制
            if (stateTimer > 120) {
                targetZoom = MathHelper.Lerp(targetZoom, 1f, 0.01f);
                cameraLerpSpeed = 0.015f;
            }

            if (stateTimer > 240) {
                cameraActive = false;
                targetZoom = 1f;
            }
        }

        #endregion

        #region 运镜系统

        private void UpdateCamera() {
            //缩放平滑插值
            currentZoom = MathHelper.Lerp(currentZoom, targetZoom, zoomLerpSpeed);
            Main.GameZoomTarget = currentZoom;

            if (!cameraActive) {
                //运镜结束后平滑恢复缩放
                currentZoom = MathHelper.Lerp(currentZoom, 1f, 0.02f);
                Main.GameZoomTarget = currentZoom;
                return;
            }

            //计算期望的屏幕位置，让cameraTarget位于屏幕中心
            Vector2 desiredScreenPos = cameraTarget - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f / currentZoom;

            //初始化平滑位置
            if (!smoothedScreenPosInitialized) {
                smoothedScreenPos = Main.screenPosition;
                smoothedScreenPosInitialized = true;
            }

            //平滑过渡到目标位置
            smoothedScreenPos = Vector2.Lerp(smoothedScreenPos, desiredScreenPos, cameraLerpSpeed);
            Main.screenPosition = smoothedScreenPos;

            //运镜期间锁定玩家控制
            Player player = Main.LocalPlayer;
            if (player != null && player.active) {
                player.controlLeft = false;
                player.controlRight = false;
                player.controlUp = false;
                player.controlDown = false;
                player.controlJump = false;
                player.controlUseItem = false;
            }
        }

        #endregion

        #region 物理辅助

        private void ApplyGravityAndGroundSnap() {
            int tileX = (int)(Center.X / 16f);
            int tileY = (int)((Position.Y + Height) / 16f);

            for (int y = tileY; y < tileY + 10; y++) {
                if (!WorldGen.InWorld(tileX, y)) break;
                Tile tile = Framing.GetTileSafely(tileX, y);
                if (tile.HasTile && Main.tileSolid[tile.TileType]) {
                    float groundY = y * 16f - Height;
                    if (Position.Y < groundY) {
                        Position.Y = MathHelper.Lerp(Position.Y, groundY, 0.3f);
                    }
                    else {
                        Position.Y = groundY;
                    }
                    return;
                }
            }

            //没找到地面则施加重力
            Position.Y += 4f;
        }

        private static Vector2 FindGroundPosition(Vector2 startPos) {
            int tileX = (int)(startPos.X / 16f);
            int startTileY = (int)(startPos.Y / 16f);

            for (int y = startTileY - 50; y < startTileY + 100; y++) {
                if (!WorldGen.InWorld(tileX, y)) continue;
                Tile tile = Framing.GetTileSafely(tileX, y);
                if (tile.HasTile && Main.tileSolid[tile.TileType]) {
                    return new Vector2(startPos.X, y * 16f - 50f);
                }
            }

            return startPos;
        }

        #endregion

        #region 状态转换

        private void TransitionTo(ApolliaState newState) {
            CurrentState = newState;
            stateTimer = 0;
        }

        #endregion

        #region 绘制

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            if (CurrentState == ApolliaState.Inactive) return false;
            if (ADVAsset.ApolliaActor == null) return false;

            Texture2D tex = ADVAsset.ApolliaActor;
            if (frameWidth <= 0 || frameHeight <= 0) return false;

            int clampedFrame = Math.Clamp(frameIndex, 0, TotalFrames - 1);
            Rectangle sourceRect = new Rectangle(0, clampedFrame * frameHeight, frameWidth, frameHeight);
            Vector2 origin = new Vector2(frameWidth * 0.5f, frameHeight);
            Vector2 drawPos = new Vector2(Center.X, Position.Y + Height) - Main.screenPosition;

            //朝向翻转
            SpriteEffects fx = walkDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            //透明度
            float alpha = CurrentState == ApolliaState.Descending ? descendAlpha : 1f;
            Color bodyColor = Lighting.GetColor((int)(Center.X / 16), (int)(Center.Y / 16)) * alpha;

            //辉光效果（着陆闪光）
            if (glowIntensity > 0.02f && CWRAsset.SoftGlow != null && !CWRAsset.SoftGlow.IsDisposed) {
                Texture2D glow = CWRAsset.SoftGlow.Value;
                Color glowColor = new Color(80, 160, 255) * (glowIntensity * alpha);
                float glowScale = 80f / (glow.Width * 0.5f);
                spriteBatch.Draw(glow, drawPos - new Vector2(0, frameHeight * 0.4f), null, glowColor with { A = 0 },
                    0f, glow.Size() * 0.5f, glowScale, SpriteEffects.None, 0f);
            }

            //降落阶段的电弧边缘光
            if (CurrentState == ApolliaState.Descending && descendAlpha > 0.3f) {
                Color edgeColor = new Color(100, 200, 255) * (descendAlpha * 0.4f);
                float edgeOffset = 2f;
                for (int i = 0; i < 4; i++) {
                    float angle = MathHelper.PiOver2 * i;
                    Vector2 off = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * edgeOffset;
                    spriteBatch.Draw(tex, drawPos + off, sourceRect, edgeColor, 0f, origin, 1f, fx, 0f);
                }
            }

            //主体绘制
            spriteBatch.Draw(tex, drawPos, sourceRect, bodyColor, 0f, origin, 0.7f, fx, 0f);//70%缩放，因为这个角色比较大了，缩放一下才不显得大只

            return false;
        }

        #endregion

        #region 缓动函数

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f;

        #endregion
    }
}
