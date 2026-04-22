using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.CollisionMask;
using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures
{
    /// <summary>
    /// 虚空聚落建筑Actor
    /// 用于承载整张建筑贴图的静态装饰实体，贴图绘制由shader负责
    /// 过去时代实体显像时通过<see cref="CollisionMask.ArchitectureTilePlacer"/>就地放置隐形碰撞tile，形成与原版完全兼容的物理
    /// </summary>
    internal class ArchitectureActor : Actor
    {
        /// <summary>建筑类型，使用byte存储以便于SyncVar传输</summary>
        [SyncVar]
        public byte TypeByte;

        /// <summary>是否已根据贴图尺寸完成初始化</summary>
        private bool initialized;

        /// <summary>
        /// 当前可见度，过去时逼近1，现在时逼近0
        /// 初始为0以便玩家首次进入过去时也能播一次扭曲浮现
        /// </summary>
        private float visibility;

        /// <summary>已放置的隐形碰撞tile坐标列表，供擦除时精准回收</summary>
        private readonly List<Point16> placedCollisionTiles = [];
        /// <summary>当前是否处于"碰撞tile已写入世界"的状态</summary>
        private bool collisionPlaced;
        //碰撞显现/消失的可见度阈值，与shader的凝实感同步
        private const float CollisionThreshold = 0.5f;

        public ArchitectureType Type => (ArchitectureType)TypeByte;

        public override void OnSpawn(params object[] args) {
            DrawLayer = ActorDrawLayer.AfterTiles;
            ApplyTextureSize();
        }

        public override void AI() {
            //首次拿到贴图后锁定尺寸，确保客户端通过SyncVar同步到TypeByte后也能正确设置
            if (!initialized) {
                ApplyTextureSize();
            }
            if (!VoidColony.Active) {
                //离开维度前先回收已放置的碰撞方块，防止残留
                ArchitectureTilePlacer.Clear(placedCollisionTiles);
                collisionPlaced = false;
                ActorLoader.KillActor(WhoAmI);
                return;
            }
            ArchitectureWarpDraw.TickVisibility(ref visibility);
            UpdateCollision();
            Velocity = Vector2.Zero;
        }

        private void ApplyTextureSize() {
            Texture2D tex = ArchitectureAsset.Get(Type);
            if (tex == null) return;
            Width = tex.Width;
            Height = tex.Height;
            //扩展绘制范围到贴图的最长边，避免建筑中心离开屏幕时被裁掉
            DrawExtendMode = Math.Max(Width, Height);
            initialized = true;
        }

        /// <summary>
        /// 根据可见度状态切换碰撞tile的存在与否
        /// 只在主机与单机上执行放置/清除，客户端会自动通过SendTileSquare接收同步
        /// </summary>
        private void UpdateCollision() {
            bool shouldPlace = visibility >= CollisionThreshold;
            if (shouldPlace == collisionPlaced) return;

            if (shouldPlace) {
                string[] mask = ArchitectureCollisionMask.Get(Type);
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
            Texture2D tex = ArchitectureAsset.Get(Type);
            if (tex == null) return false;
            if (!ArchitectureWarpDraw.ShouldDraw(visibility)) return false;

            float warp = ArchitectureWarpDraw.ComputeWarp();
            Vector2 drawPos = Position - Main.screenPosition;

            ArchitectureWarpDraw.DrawWithShader(spriteBatch, tex, drawPos, visibility, warp);
            return false;
        }
    }
}

