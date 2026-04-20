using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures
{
    /// <summary>
    /// 虚空聚落建筑之间的一条水平连接段Actor
    /// 只做水平方向的贴图平铺，不做任何旋转，避免拐角拼接造成的视觉割裂
    /// 由Spawner先生成连接段再生成建筑，同层内先生成者先绘制，确保桥/管不遮挡主建筑
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

        public PortKind Kind => (PortKind)KindByte;

        public override void OnSpawn(params object[] args) {
            DrawLayer = ActorDrawLayer.AfterTiles;
            ApplyBoundingBox();
        }

        public override void AI() {
            if (!VoidColony.Active) {
                ActorLoader.KillActor(WhoAmI);
                return;
            }
            ArchitectureWarpDraw.TickVisibility(ref visibility);
        }

        private void ApplyBoundingBox() {
            int minX = Math.Min(StartX, EndX);
            int maxX = Math.Max(StartX, EndX);
            Position = new Vector2(minX - 32, StartY - 200);
            Width = maxX - minX + 64;
            Height = 400;
            DrawExtendMode = Math.Max(Width, Height);
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
                ArchitectureWarpDraw.DrawWithShader(spriteBatch, tex, drawPos, visibility, warp);
            }
            return false;
        }
    }
}
