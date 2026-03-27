using InnoVault.Actors;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Skills.Sandevistans
{
    /// <summary>
    /// 斯安威斯坦残影实体，每个实例代表一个玩家在某一帧的视觉快照。
    /// 使用 <see cref="ActorDrawLayer.BeforePlayers"/> 层级绘制，确保残影出现在玩家身后。
    /// 绘制采用 RT 架构：所有残影先绘制到独立 RenderTarget，再通过着色器统一处理后合成到屏幕。
    /// </summary>
    internal class SandevistanGhostActor : Actor
    {
        private static Player ghostRenderPlayer;
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

            // === Phase 1: 将所有残影绘制到独立RT ===
            gd.SetRenderTarget(ghostRT);
            gd.Clear(Color.Transparent);

            //PlayerRenderer.DrawPlayer 内部管理自己的 Begin/End
            //需要一个活跃批次让它的首次 End 不崩溃
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, null, Main.Rasterizer, null, Main.GameViewMatrix.ZoomMatrix);

            ghostRenderPlayer ??= new Player();

            foreach (SandevistanGhostActor ghost in ghosts) {
                if (!ghost.Active || ghost.Alpha <= 0.01f) {
                    continue;
                }

                Player source = Main.player[ghost.OwnerIndex];
                if (source == null || !source.active) {
                    continue;
                }

                Player gp = ghostRenderPlayer;
                gp.CopyVisuals(source);
                gp.ResetEffects();
                gp.position = ghost.SnapshotPosition;
                gp.velocity = ghost.SnapshotVelocity;
                gp.direction = ghost.SnapshotDirection;
                gp.bodyFrame = ghost.SnapshotBodyFrame;
                gp.legFrame = ghost.SnapshotLegFrame;
                gp.fullRotation = ghost.SnapshotFullRotation;
                gp.fullRotationOrigin = ghost.SnapshotFullRotationOrigin;
                gp.skinVariant = source.skinVariant;
                gp.heldProj = -1;

                //颜色渐变：蓝 → 青绿，RGB保持明亮，仅用A通道控制淡出
                float fadeProgress = 1f - ghost.Alpha;
                Color startColor = new Color(0, 180, 255, 255);
                Color endColor = new Color(0, 255, 200, 255);
                Color tint = Color.Lerp(startColor, endColor, fadeProgress);
                tint.A = (byte)(255 * ghost.Alpha);

                gp.skinColor = tint;
                gp.shirtColor = tint;
                gp.underShirtColor = tint;
                gp.pantsColor = tint;
                gp.shoeColor = tint;
                gp.hairColor = tint;
                gp.eyeColor = tint;

                Main.PlayerRenderer.DrawPlayer(
                    Main.Camera, gp, gp.position,
                    gp.fullRotation, gp.fullRotationOrigin
                );
            }

            //DrawPlayer 结束后会留一个活跃批次，关掉它
            spriteBatch.End();

            // === Phase 2: 切换回原始RT，用着色器处理ghost RT后绘制到屏幕 ===
            if (previousTargets.Length > 0) {
                gd.SetRenderTarget((RenderTarget2D)previousTargets[0].RenderTarget);
            }
            else {
                gd.SetRenderTarget(null);
            }

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            //Effect shader = SandevistanAssets.SandevistanGhost;
            //if (shader != null) {
            //    shader.Parameters["glowIntensity"]?.SetValue(0.4f);
            //    shader.CurrentTechnique.Passes[0].Apply();
            //}

            spriteBatch.Draw(ghostRT, Vector2.Zero, Color.White);
            spriteBatch.End();

            //恢复 ActorLoader 所需的原始批次
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);

            return false;
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
