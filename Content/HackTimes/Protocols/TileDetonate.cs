using CalamityOverhaul.Content.HackTimes.Scannables;
using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.HackTimes.Protocols
{
    /// <summary>
    /// 物块爆破协议：瓦解目标物块及周围区域的结构完整性
    /// </summary>
    internal class TileDetonate : QuickHackDef
    {
        //爆破半径（格子数）
        private const int BlastRadius = 3;

        public override void SetDefaults() {
            UploadTime = 80;
            RamCost = 3;
            Category = QuickHackCategory.TileManip;
            SupportedTargets = HackTargetKind.Tile;
        }

        public override bool CanApplyTo(IHackTarget target) {
            if (!base.CanApplyTo(target)) return false;
            if (target is not TileScannable s) return false;
            //不允许对神庙砖等极高硬度物块使用
            Tile tile = Main.tile[s.TileCoordX, s.TileCoordY];
            return tile.TileType != TileID.LihzahrdBrick
                && tile.TileType != TileID.LihzahrdAltar;
        }

        public override bool OnApply(IHackTarget target, Player caster) {
            if (target is not TileScannable s) return false;
            int tileX = s.TileCoordX;
            int tileY = s.TileCoordY;
            Vector2 center = new(tileX * 16f + 8f, tileY * 16f + 8f);

            //范围破坏
            for (int dx = -BlastRadius; dx <= BlastRadius; dx++) {
                for (int dy = -BlastRadius; dy <= BlastRadius; dy++) {
                    if (dx * dx + dy * dy > BlastRadius * BlastRadius) continue;
                    int tx = tileX + dx;
                    int ty = tileY + dy;
                    if (tx < 0 || tx >= Main.maxTilesX || ty < 0 || ty >= Main.maxTilesY) continue;
                    WorldGen.KillTile(tx, ty, false, false, false);
                }
            }

            //爆破粒子
            for (int i = 0; i < 16; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(6f, 6f);
                Color c = Color.Lerp(new Color(255, 150, 50), new Color(255, 80, 30), Main.rand.NextFloat());
                PRTLoader.AddParticle(new PRT_Spark(center, vel, false, 30, 1.5f, c));
            }

            //碎片粒子向外飞散
            for (int i = 0; i < 24; i++) {
                Vector2 pos = center + Main.rand.NextVector2Circular(BlastRadius * 10f, BlastRadius * 10f);
                Vector2 vel = (pos - center).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 5f);
                PRTLoader.AddParticle(new PRT_Spark(pos, vel, false, 20, 0.6f,
                    new Color(80, 200, 255)));
            }

            if (!VaultUtils.isServer) {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = -0.2f }, center);
            }

            //同步网络
            if (Main.netMode != NetmodeID.SinglePlayer) {
                NetMessage.SendTileSquare(-1, tileX - BlastRadius, tileY - BlastRadius,
                    BlastRadius * 2 + 1);
            }

            return true;
        }
    }
}
