using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.CollisionMask;
using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.SignalTowers
{
    /// <summary>
    /// 信号塔Actor
    /// 作为策划上承担复杂独立功能的核心建筑，从通用<see cref="ArchitectureActor"/>中剥离
    /// 基础逻辑（时空可见度、碰撞、扭曲着色）与通用建筑保持一致，专属机制（过电滤镜、后续的扫描/广播等）直接挂在自身
    /// </summary>
    internal class SignalTowerActor : Actor
    {
        /// <summary>被雷击后过电滤镜的剩余帧数</summary>
        [SyncVar]
        public int ElectrifyTimer;
        /// <summary>本次过电的总帧长，便于shader按进度归一化</summary>
        [SyncVar]
        public int ElectrifyMax;
        /// <summary>本次过电的随机种子，让每次电弧形态不同</summary>
        [SyncVar]
        public float ElectrifySeed;

        private bool initialized;
        //时空可见度，过去时逼近1，现在时逼近0
        private float visibility;

        //隐形碰撞tile记录，与ArchitectureActor共用tile放置器
        private readonly List<Point16> placedCollisionTiles = [];
        private bool collisionPlaced;
        private const float CollisionThreshold = 0.5f;

        /// <summary>当前过电滤镜是否仍在持续</summary>
        public bool IsElectrified => ElectrifyTimer > 0 && ElectrifyMax > 0;

        /// <summary>在塔上触发一次过电滤镜，<paramref name="frames"/>为总帧长</summary>
        public void BeginElectrify(int frames, float seed) {
            if (frames <= 0) return;
            ElectrifyTimer = frames;
            ElectrifyMax = frames;
            ElectrifySeed = seed;
            NetUpdate = true;
        }

        public override void OnSpawn(params object[] args) {
            DrawLayer = ActorDrawLayer.AfterTiles;
            ApplyTextureSize();
        }

        public override void AI() {
            if (!initialized) ApplyTextureSize();
            if (!VoidColony.Active) {
                ArchitectureTilePlacer.Clear(placedCollisionTiles);
                collisionPlaced = false;
                ActorLoader.KillActor(WhoAmI);
                return;
            }
            ArchitectureWarpDraw.TickVisibility(ref visibility);
            UpdateCollision();
            if (ElectrifyTimer > 0) ElectrifyTimer--;
            Velocity = Vector2.Zero;
        }

        private void ApplyTextureSize() {
            Texture2D tex = ArchitectureAsset.Get(ArchitectureType.SignalTower);
            if (tex == null) return;
            Width = tex.Width;
            Height = tex.Height;
            DrawExtendMode = Math.Max(Width, Height);
            initialized = true;
        }

        private void UpdateCollision() {
            bool shouldPlace = visibility >= CollisionThreshold;
            if (shouldPlace == collisionPlaced) return;

            if (shouldPlace) {
                string[] mask = ArchitectureCollisionMask.Get(ArchitectureType.SignalTower);
                if (mask == null) { collisionPlaced = true; return; }
                int tileX = (int)Math.Round(Position.X / 16f);
                int tileY = (int)Math.Round(Position.Y / 16f);
                ArchitectureTilePlacer.Place(mask, tileX, tileY, placedCollisionTiles);
                collisionPlaced = true;
            }
            else {
                ArchitectureTilePlacer.Clear(placedCollisionTiles);
                collisionPlaced = false;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            Texture2D tex = ArchitectureAsset.Get(ArchitectureType.SignalTower);
            if (tex == null) return false;
            if (!ArchitectureWarpDraw.ShouldDraw(visibility)) return false;

            float warp = ArchitectureWarpDraw.ComputeWarp();
            Vector2 drawPos = Position - Main.screenPosition;

            ArchitectureWarpDraw.DrawWithShader(spriteBatch, tex, drawPos, visibility, warp, flipX: false);

            //过电滤镜：Additive叠在本体之上，用原图alpha做遮罩避免矩形外溢
            if (IsElectrified) {
                DrawElectrifiedOverlay(spriteBatch, tex, drawPos);
            }
            return false;
        }

        private void DrawElectrifiedOverlay(SpriteBatch spriteBatch, Texture2D tex, Vector2 drawPos) {
            Effect shader = EffectLoader.SignalTowerElectrified?.Value;
            if (shader == null) return;

            float progress = 1f - ElectrifyTimer / (float)ElectrifyMax;
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["electrifyProgress"]?.SetValue(MathHelper.Clamp(progress, 0f, 1f));
            shader.Parameters["seed"]?.SetValue(ElectrifySeed);
            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));
            shader.Parameters["intensity"]?.SetValue(MathHelper.Clamp(visibility, 0f, 1f));

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(tex, drawPos, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
