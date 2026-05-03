using InnoVault.Actors;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.Cyberwares.Implementation.Sandevistans
{
    /// <summary>
    /// 斯安威斯坦残影实体，每个实例代表一个玩家在某一帧的视觉快照。
    /// 使用 <see cref="ActorDrawLayer.BeforePlayers"/> 层级绘制，确保残影出现在玩家身后。
    /// 绘制采用 RT 架构：所有残影先绘制到独立 RenderTarget，再通过着色器统一处理后合成到屏幕。
    /// </summary>
    internal class SandevistanGhostActor : Actor
    {
        private static RenderTarget2D ghostRT;
        private static uint lastBatchDrawFrame;

        public Vector2 SnapshotPosition;
        public Vector2 SnapshotVelocity;
        public int SnapshotDirection;
        public Rectangle SnapshotBodyFrame;
        public Rectangle SnapshotLegFrame;
        public float SnapshotFullRotation;
        public Vector2 SnapshotFullRotationOrigin;
        public int OwnerIndex;
        public int Lifetime;
        public int MaxLifetime;
        public float Alpha => Math.Clamp((float)Lifetime / MaxLifetime, 0f, 1f);

        public override void OnSpawn(params object[] args) {
            Width = 4;
            Height = 4;
            DrawLayer = ActorDrawLayer.BeforePlayers;
            DrawExtendMode = 600;
            MaxLifetime = 120;
            Lifetime = MaxLifetime;

            if (args is not null && args.Length >= 1 && args[0] is Player owner) {
                CapturePlayerState(owner);
            }
        }

        private void CapturePlayerState(Player owner) {
            OwnerIndex = owner.whoAmI;
            SnapshotPosition = owner.position;
            SnapshotVelocity = owner.velocity;
            SnapshotDirection = owner.direction;
            SnapshotBodyFrame = owner.bodyFrame;
            SnapshotLegFrame = owner.legFrame;
            SnapshotFullRotation = owner.fullRotation;
            SnapshotFullRotationOrigin = owner.fullRotationOrigin;
            Position = owner.Center;
        }

        public override void AI() {
            Lifetime--;
            if (Lifetime <= 0) {
                ActorLoader.KillActor(WhoAmI);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            //每帧只由第一个被绘制的残影触发一次批量RT渲染，后续残影直接跳过
            if (Main.GameUpdateCount == lastBatchDrawFrame) {
                return false;
            }
            lastBatchDrawFrame = Main.GameUpdateCount;

            List<SandevistanGhostActor> ghosts = ActorLoader.GetActiveActors<SandevistanGhostActor>();
            if (ghosts.Count == 0) {
                return false;
            }

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            EnsureRT(gd);

            //保存当前渲染目标
            RenderTargetBinding[] previousTargets = gd.GetRenderTargets();

            //结束 ActorLoader 的批次
            spriteBatch.End();

            //标记 SpriteBatch 的状态，确保异常路径下也能恢复 ActorLoader 期望的批次
            bool batchActive = false;

            try {
                // === Phase 1: 将所有残影绘制到独立RT ===
                gd.SetRenderTarget(ghostRT);
                gd.Clear(Color.Transparent);

                //PlayerRenderer.DrawPlayer 内部管理自己的 Begin/End
                //需要一个活跃批次让它的首次 End 不崩溃
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.PointClamp, null, Main.Rasterizer, null, Main.GameViewMatrix.ZoomMatrix);
                batchActive = true;

                foreach (SandevistanGhostActor ghost in ghosts) {
                    if (!ghost.Active || ghost.Alpha <= 0.01f) {
                        continue;
                    }

                    if (ghost.OwnerIndex < 0 || ghost.OwnerIndex >= Main.maxPlayers) {
                        continue;
                    }

                    Player source = Main.player[ghost.OwnerIndex];
                    if (source == null || !source.active || source.dead) {
                        continue;
                    }

                    DrawGhostFromSource(source, ghost);
                }

                //DrawPlayer 结束后会留一个活跃批次，关掉它
                spriteBatch.End();
                batchActive = false;

                // === Phase 2: 切换回原始RT，用着色器处理ghost RT后绘制到屏幕 ===
                RestorePreviousTargets(gd, previousTargets);

                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                batchActive = true;

                spriteBatch.Draw(ghostRT, Vector2.Zero, Color.White);

                spriteBatch.End();
                batchActive = false;
            } catch (Exception ex) {
                CWRMod.Instance?.Logger.Warn("[Sandevistan] Ghost render failed: " + ex);
                if (batchActive) {
                    try { spriteBatch.End(); } catch { }
                }
                RestorePreviousTargets(gd, previousTargets);
            }

            //恢复 ActorLoader 所需的原始批次（无论成功失败都需要）
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        //直接借用源玩家做位置快照绘制，避免使用未初始化的 new Player() 触发 BoringSetup_End 除零
        private static void DrawGhostFromSource(Player source, SandevistanGhostActor ghost) {
            //保存原始状态
            Vector2 origPosition = source.position;
            Vector2 origVelocity = source.velocity;
            int origDirection = source.direction;
            Rectangle origBodyFrame = source.bodyFrame;
            Rectangle origLegFrame = source.legFrame;
            float origFullRotation = source.fullRotation;
            Vector2 origFullRotationOrigin = source.fullRotationOrigin;
            Color origSkin = source.skinColor;
            Color origShirt = source.shirtColor;
            Color origUnderShirt = source.underShirtColor;
            Color origPants = source.pantsColor;
            Color origShoe = source.shoeColor;
            Color origHair = source.hairColor;
            Color origEye = source.eyeColor;

            try {
                source.position = ghost.SnapshotPosition;
                source.velocity = ghost.SnapshotVelocity;
                source.direction = ghost.SnapshotDirection;
                source.bodyFrame = ghost.SnapshotBodyFrame;
                source.legFrame = ghost.SnapshotLegFrame;
                source.fullRotation = ghost.SnapshotFullRotation;
                source.fullRotationOrigin = ghost.SnapshotFullRotationOrigin;

                //颜色渐变：蓝 → 青绿，仅用 A 通道控制淡出
                float fadeProgress = 1f - ghost.Alpha;
                Color startColor = new Color(0, 180, 255, 255);
                Color endColor = new Color(0, 255, 200, 255);
                Color tint = Color.Lerp(startColor, endColor, fadeProgress);
                tint.A = (byte)(255 * ghost.Alpha);

                source.skinColor = tint;
                source.shirtColor = tint;
                source.underShirtColor = tint;
                source.pantsColor = tint;
                source.shoeColor = tint;
                source.hairColor = tint;
                source.eyeColor = tint;

                Main.PlayerRenderer.DrawPlayer(
                    Main.Camera, source, source.position,
                    source.fullRotation, source.fullRotationOrigin
                );
            } finally {
                //恢复原始状态
                source.position = origPosition;
                source.velocity = origVelocity;
                source.direction = origDirection;
                source.bodyFrame = origBodyFrame;
                source.legFrame = origLegFrame;
                source.fullRotation = origFullRotation;
                source.fullRotationOrigin = origFullRotationOrigin;
                source.skinColor = origSkin;
                source.shirtColor = origShirt;
                source.underShirtColor = origUnderShirt;
                source.pantsColor = origPants;
                source.shoeColor = origShoe;
                source.hairColor = origHair;
                source.eyeColor = origEye;
            }
        }

        private static void RestorePreviousTargets(GraphicsDevice gd, RenderTargetBinding[] previousTargets) {
            if (previousTargets != null && previousTargets.Length > 0
                && previousTargets[0].RenderTarget != null) {
                gd.SetRenderTargets(previousTargets);
            }
            else {
                gd.SetRenderTarget(null);
            }
        }

        private static void EnsureRT(GraphicsDevice gd) {
            if (ghostRT == null || ghostRT.IsDisposed
                || ghostRT.Width != Main.screenWidth || ghostRT.Height != Main.screenHeight) {
                ghostRT?.Dispose();
                ghostRT = new RenderTarget2D(gd, Main.screenWidth, Main.screenHeight,
                    false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
        }
    }
}
