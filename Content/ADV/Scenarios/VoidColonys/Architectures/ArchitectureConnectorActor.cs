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
    /// 虚空聚落建筑之间的一条水平连接段Actor
    /// 只做水平方向的贴图平铺，不做任何旋转，避免拐角拼接造成的视觉割裂
    /// 由Spawner先生成连接段再生成建筑，同层内先生成者先绘制，确保桥/管不遮挡主建筑
    /// 过去时代显现时沿端点Y高度铺一行隐形平台，玩家可直接走上走下
    /// </summary>
    internal class ArchitectureConnectorActor : Actor
    {
        [SyncVar]
        public byte KindByte;
        [SyncVar]
        public int StartX;
        [SyncVar]
        public int StartY;
        [SyncVar]
        public int EndX;

        /// <summary>当前可见度，机制与ArchitectureActor保持一致</summary>
        private float visibility;

        private readonly List<Point16> placedCollisionTiles = [];
        private bool collisionPlaced;
        private const float CollisionThreshold = 0.5f;

        public PortKind Kind => (PortKind)KindByte;

        public override void OnSpawn(params object[] args) {
            DrawLayer = ActorDrawLayer.AfterTiles;
            ApplyBoundingBox();
        }

        public override void AI() {
            if (!VoidColony.Active) {
                ArchitectureTilePlacer.Clear(placedCollisionTiles);
                collisionPlaced = false;
                ActorLoader.KillActor(WhoAmI);
                return;
            }
            ArchitectureWarpDraw.TickVisibility(ref visibility);
            UpdateCollision();
        }

        private void ApplyBoundingBox() {
            int minX = Math.Min(StartX, EndX);
            int maxX = Math.Max(StartX, EndX);
            Position = new Vector2(minX - 32, StartY - 200);
            Width = maxX - minX + 64;
            Height = 400;
            DrawExtendMode = Math.Max(Width, Height);
        }

        private void UpdateCollision() {
            bool shouldPlace = visibility >= CollisionThreshold;
            if (shouldPlace == collisionPlaced) return;

            if (shouldPlace) {
                int leftX = Math.Min(StartX, EndX);
                int rightX = Math.Max(StartX, EndX);
                int tileX1 = (int)Math.Round(leftX / 16f);
                int tileX2 = (int)Math.Round(rightX / 16f);
                //连接段贴图锚点位于竖直中线，贴图下半部分才是桥底/管底
                //往下偏移1格让平台正好位于"桥面"而不是"管道正中"
                int tileY = (int)Math.Round(StartY / 16f) + 1;
                ArchitectureTilePlacer.PlaceRow(tileX1, tileX2, tileY, placedCollisionTiles);
                collisionPlaced = true;
            }
            else {
                ArchitectureTilePlacer.Clear(placedCollisionTiles);
                collisionPlaced = false;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            Texture2D tex = Kind == PortKind.Bridge
                ? ArchitectureAsset.ConnectionBridgeSegment
                : ArchitectureAsset.TubularConnectorTunnel;
            if (tex == null) return false;
            if (!ArchitectureWarpDraw.ShouldDraw(visibility)) return false;

            int leftX = Math.Min(StartX, EndX);
            int rightX = Math.Max(StartX, EndX);
            int span = rightX - leftX;
            if (span < 4) return false;

            int segWidth = tex.Width;
            int segCount = Math.Max(1, (int)Math.Ceiling(span / (float)segWidth));
            float warp = ArchitectureWarpDraw.ComputeWarp();
            //贴图锚点位于竖直中线，因此左上角Y = 端点Y - 半高
            int topY = StartY - tex.Height / 2;

            for (int i = 0; i < segCount; i++) {
                int segLeft = i == segCount - 1 ? rightX - segWidth : leftX + i * segWidth;
                Vector2 drawPos = new Vector2(segLeft, topY) - Main.screenPosition;
                drawPos.Y -= 16;//一个魔法矫正值，这些桥梁往上16像素几乎刚刚好
                ArchitectureWarpDraw.DrawWithShader(spriteBatch, tex, drawPos, visibility, warp);
            }
            return false;
        }
    }
}

