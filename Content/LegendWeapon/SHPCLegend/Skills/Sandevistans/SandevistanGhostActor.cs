using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Skills.Sandevistans
{
    /// <summary>
    /// 斯安威斯坦残影实体，每个实例代表一个玩家在某一帧的视觉快照。
    /// 使用 <see cref="ActorDrawLayer.BeforePlayers"/> 层级绘制，确保残影出现在玩家身后。
    /// </summary>
    internal class SandevistanGhostActor : Actor
    {
        private static Player ghostRenderPlayer;

        /// <summary>
        /// 玩家位置快照
        /// </summary>
        public Vector2 SnapshotPosition;
        /// <summary>
        /// 玩家速度快照
        /// </summary>
        public Vector2 SnapshotVelocity;
        /// <summary>
        /// 玩家朝向快照
        /// </summary>
        public int SnapshotDirection;
        /// <summary>
        /// 身体帧快照
        /// </summary>
        public Rectangle SnapshotBodyFrame;
        /// <summary>
        /// 腿部帧快照
        /// </summary>
        public Rectangle SnapshotLegFrame;
        /// <summary>
        /// 完整旋转角度快照
        /// </summary>
        public float SnapshotFullRotation;
        /// <summary>
        /// 旋转原点快照
        /// </summary>
        public Vector2 SnapshotFullRotationOrigin;
        /// <summary>
        /// 拥有者玩家索引
        /// </summary>
        public int OwnerIndex;
        /// <summary>
        /// 当前剩余生命帧数
        /// </summary>
        public int Lifetime;
        /// <summary>
        /// 最大生命帧数
        /// </summary>
        public int MaxLifetime;
        /// <summary>
        /// 当前透明度，基于生命周期自动计算（1=完全不透明，0=完全透明）
        /// </summary>
        public float Alpha => Math.Clamp((float)Lifetime / MaxLifetime, 0f, 1f);

        public override void OnSpawn(params object[] args) {
            Width = 4;
            Height = 4;
            DrawLayer = ActorDrawLayer.BeforePlayers;
            DrawExtendMode = 600;
            MaxLifetime = 122;
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
            if (Alpha <= 0.01f) {
                return false;
            }

            Player source = Main.player[OwnerIndex];
            if (source == null || !source.active) {
                return false;
            }

            //结束 ActorLoader 启动的批次，准备切换到适合 PlayerRenderer 的批次设置
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, null, Main.Rasterizer, null, Main.GameViewMatrix.ZoomMatrix);

            //构建残影玩家 —— 复制完整视觉外观（装备、头发、皮肤变体等）
            ghostRenderPlayer ??= new Player();
            Player gp = ghostRenderPlayer;

            gp.CopyVisuals(source);
            gp.ResetEffects();
            gp.position = SnapshotPosition;
            gp.velocity = SnapshotVelocity;
            gp.direction = SnapshotDirection;
            gp.bodyFrame = SnapshotBodyFrame;
            gp.legFrame = SnapshotLegFrame;
            gp.fullRotation = SnapshotFullRotation;
            gp.fullRotationOrigin = SnapshotFullRotationOrigin;
            gp.skinVariant = source.skinVariant;
            gp.heldProj = -1;

            //青色调着色，透明度随生命周期淡出
            float fade = Alpha;
            Color tint = new Color(0, 220, 255) * fade;
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

            //DrawPlayer 内部会管理自己的批次，绘制完毕后恢复 ActorLoader 所需的原始批次设置
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
