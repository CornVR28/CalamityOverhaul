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

        public PortKind Kind => (PortKind)KindByte;

        public override void OnSpawn(params object[] args) {
            DrawLayer = ActorDrawLayer.AfterTiles;
            ApplyBoundingBox();
        }

        public override void AI() {
            if (!VoidColony.Active) {
                ActorLoader.KillActor(WhoAmI);
            }
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

            int leftX = Math.Min(StartX, EndX);
            int rightX = Math.Max(StartX, EndX);
            int span = rightX - leftX;
            if (span < 4) return false;

            int segWidth = tex.Width;
            int segHeight = tex.Height;
            //贴图锚点位于其竖直中线，这样端点Y对应桥面/管腔中心
            Vector2 origin = new(0f, segHeight * 0.5f);

            int segCount = Math.Max(1, (int)Math.Ceiling(span / (float)segWidth));
            for (int i = 0; i < segCount; i++) {
                //最后一段回退对齐到右端，避免超出或缝隙
                int segLeft = i == segCount - 1 ? rightX - segWidth : leftX + i * segWidth;
                int sampleTileX = Math.Clamp((segLeft + segWidth / 2) / 16, 0, Main.maxTilesX - 1);
                int sampleTileY = Math.Clamp(StartY / 16, 0, Main.maxTilesY - 1);
                Color light = Lighting.GetColor(sampleTileX, sampleTileY);

                Vector2 drawPos = new Vector2(segLeft, StartY) - Main.screenPosition;
                spriteBatch.Draw(tex, drawPos, null, light, 0f, origin, 1f, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
