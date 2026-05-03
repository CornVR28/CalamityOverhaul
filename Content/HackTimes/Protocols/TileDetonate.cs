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
        //协议自带的基础镐力，未持有任何镐时也能炸开常见物块
        private const int BasePickPower = 50;

        public override void SetDefaults() {
            UploadTime = 80;
            RamCost = 3;
            Category = QuickHackCategory.TileManip;
            SupportedTargets = HackTargetKind.Tile;
        }

        //取玩家背包中所有物品的最高镐力，再与协议默认镐力取大值
        //这样适配所有原版及模组工具，因为它们都会把镐力写入Item.pick
        private static int GetEffectivePickPower(Player player) {
            int max = BasePickPower;
            if (player == null) return max;
            //主背包
            for (int i = 0; i < player.inventory.Length; i++) {
                Item it = player.inventory[i];
                if (it != null && !it.IsAir && it.pick > max) {
                    max = it.pick;
                }
            }
            //虚空袋等扩展容器：tModLoader提供GetAllInventorySlots
            if (max < int.MaxValue) {
                foreach (Item it in player.GetAllInventorySlots()) {
                    if (it != null && !it.IsAir && it.pick > max) {
                        max = it.pick;
                    }
                }
            }
            return max;
        }

        //判断单格瓦砾是否能被当前镐力击碎
        //同时遵循模组的MinPick设定，因为tModLoader会把它写入Main.tileMinPick
        private static bool CanBreakTileWithPickPower(int x, int y, int pickPower) {
            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) return false;
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile) return false;
            ushort type = tile.TileType;
            //跳过原版强制屏蔽的核心方块
            if (type == TileID.LihzahrdBrick || type == TileID.LihzahrdAltar) return false;
            if (type == TileID.DemonAltar || type == TileID.LunarMonolith) return false;
            //锤系方块（如平台、灯笼链等）不算镐子可破，跳过避免误炸不该炸的设施
            if (Main.tileHammer[type]) return false;
            //部分基础设施保护：宝箱、出生点等
            if (Main.tileContainer[type] || Main.tileDungeon[type] && !NPC.downedBoss3) return false;
            //核心判断：镐力是否达到该物块要求
            return pickPower >= Main.tileMinPick[type] && WorldGen.CanKillTile(x, y);
        }

        public override bool CanApplyTo(IHackTarget target) {
            if (!base.CanApplyTo(target)) return false;
            if (target is not TileScannable s) return false;
            //不允许对神庙砖等极高硬度物块使用
            Tile tile = Main.tile[s.TileCoordX, s.TileCoordY];
            if (tile.TileType == TileID.LihzahrdBrick || tile.TileType == TileID.LihzahrdAltar) {
                return false;
            }
            //目标本体的镐力门槛也必须满足，否则没有意义
            int pickPower = GetEffectivePickPower(Main.LocalPlayer);
            return pickPower >= Main.tileMinPick[tile.TileType];
        }

        public override bool OnApply(IHackTarget target, Player caster) {
            if (target is not TileScannable s) return false;
            int tileX = s.TileCoordX;
            int tileY = s.TileCoordY;
            Vector2 center = new(tileX * 16f + 8f, tileY * 16f + 8f);

            int pickPower = GetEffectivePickPower(caster);

            //范围破坏
            for (int dx = -BlastRadius; dx <= BlastRadius; dx++) {
                for (int dy = -BlastRadius; dy <= BlastRadius; dy++) {
                    if (dx * dx + dy * dy > BlastRadius * BlastRadius) continue;
                    int tx = tileX + dx;
                    int ty = tileY + dy;
                    if (!CanBreakTileWithPickPower(tx, ty, pickPower)) continue;
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
