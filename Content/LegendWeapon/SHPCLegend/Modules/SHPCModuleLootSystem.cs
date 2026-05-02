using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    internal class SHPCModuleLootSystem : ModSystem
    {
        private const string SaveKeyInjected = "SHPCModulesInjected";
        private static bool injected;

        public override void OnWorldLoad() => injected = false;

        public override void LoadWorldData(TagCompound tag) {
            injected = tag != null && tag.TryGet(SaveKeyInjected, out bool value) && value;
        }

        public override void SaveWorldData(TagCompound tag) {
            tag[SaveKeyInjected] = injected;
        }

        public override void PostWorldGen() {
            InjectModules();
        }

        public override void PostUpdateWorld() {
            if (injected || Main.netMode == NetmodeID.MultiplayerClient) {
                return;
            }

            InjectModules();
        }

        private static void InjectModules() {
            //收集所有改件类型，方便后续按索引随机抽取
            int[] moduleTypes = [
                ModContent.ItemType<RapidBarrelModule>(),
                ModContent.ItemType<FocusBarrelModule>(),
                ModContent.ItemType<ScattershotBarrelModule>(),
                ModContent.ItemType<HypersonicBarrelModule>(),
                ModContent.ItemType<HeavyBarrelModule>(),
                ModContent.ItemType<PrecisionOpticModule>(),
                ModContent.ItemType<AdaptiveOpticModule>(),
                ModContent.ItemType<ThermalOpticModule>(),
                ModContent.ItemType<HoloOpticModule>(),
                ModContent.ItemType<OverloadCoreModule>(),
                ModContent.ItemType<HighVoltageCoreModule>(),
                ModContent.ItemType<CapacitorBankModule>(),
                ModContent.ItemType<PlasmaInjectorModule>(),
                ModContent.ItemType<SteadyStockModule>(),
                ModContent.ItemType<KineticDamperModule>(),
                ModContent.ItemType<LightStockModule>(),
                ModContent.ItemType<AssaultStockModule>(),
                ModContent.ItemType<HarmonyGripModule>(),
                ModContent.ItemType<EfficientGripModule>(),
                ModContent.ItemType<CrystalGripModule>(),
                ModContent.ItemType<BalancedGripModule>(),
                ModContent.ItemType<ResonanceFrameModule>(),
                ModContent.ItemType<MultiCellFrameModule>(),
                ModContent.ItemType<QuantumFrameModule>(),
                ModContent.ItemType<VolatileFrameModule>(),
            ];

            for (int i = 0; i < Main.maxChests; i++) {
                Chest chest = Main.chest[i];
                if (chest == null || !IsLaboratorySecurityChest(chest)) {
                    continue;
                }

                int moduleType = moduleTypes[WorldGen.genRand.Next(moduleTypes.Length)];
                AddChestItem(chest, moduleType);
            }

            injected = true;
        }

        private static bool IsLaboratorySecurityChest(Chest chest) {
            if (chest.x < 0 || chest.x >= Main.maxTilesX || chest.y < 0 || chest.y >= Main.maxTilesY) {
                return false;
            }

            Tile tile = Main.tile[chest.x, chest.y];
            return tile.HasTile
                && (tile.TileType == CWRID.Tile_SecurityChestTile
                    || tile.TileType == CWRID.Tile_AgedSecurityChestTile);
        }

        private static void AddChestItem(Chest chest, int itemType) {
            for (int i = 0; i < chest.item.Length; i++) {
                Item item = chest.item[i];
                if (item.type != ItemID.None) {
                    continue;
                }

                item.SetDefaults(itemType);
                item.stack = 1;
                return;
            }
        }
    }
}
