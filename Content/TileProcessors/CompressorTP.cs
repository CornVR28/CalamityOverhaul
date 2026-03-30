using CalamityOverhaul.Content.Tiles;
using InnoVault.TileProcessors;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.TileProcessors
{
    internal class CompressorTP : TileProcessor
    {
        public override int TargetTileID => ModContent.TileType<DarkMatterCompressor>();
        private const int maxleng = 120;
        private bool mouseOnTile;
        internal bool drawGlow;
        internal Color gloaColor;
        private int gloawTime;
        internal int frame;
        internal Vector2 Center => PosInWorld + new Vector2(DarkMatterCompressor.Width, DarkMatterCompressor.Height) * 8;
        public override void Update() {
            VaultUtils.ClockFrame(ref frame, 8, 3);
            if (frame != 2) {
                Lighting.AddLight(Center, Color.White.ToVector3() * (Main.GameUpdateCount % 40 / 40f));
            }

            Player player = Main.LocalPlayer;
            if (!player.active || Main.myPlayer != player.whoAmI) {
                return;
            }

            if (mouseOnTile) {
                Lighting.AddLight(Center, Color.White.ToVector3());
            }

            if (VaultUtils.isServer) {
                return;
            }

            Rectangle tileRec = new Rectangle(Position.X * 16, Position.Y * 16, BloodAltar.Width * 18, BloodAltar.Height * 18);
            mouseOnTile = tileRec.Intersects(new Rectangle((int)Main.MouseWorld.X, (int)Main.MouseWorld.Y, 1, 1));

            float leng = PosInWorld.Distance(player.Center);
            drawGlow = leng < maxleng && mouseOnTile;
            if (drawGlow) {
                gloawTime++;
                gloaColor = Color.AliceBlue * MathF.Abs(MathF.Sin(gloawTime * 0.04f));
            }
            else {
                gloawTime = 0;
            }
        }
    }
}
