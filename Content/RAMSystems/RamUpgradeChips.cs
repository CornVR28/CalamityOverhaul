using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.RAMSystems
{
    internal abstract class BaseRamUpgradeChip : ModItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";

        protected abstract bool CanApplyUpgrade { get; }

        protected abstract void ApplyUpgrade(Player player);

        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 28;
            Item.maxStack = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: 1);
            Item.UseSound = SoundID.Item4;
            Item.autoReuse = true;
        }

        public override bool CanUseItem(Player player) => CanApplyUpgrade;

        public override bool? UseItem(Player player) {
            ApplyUpgrade(player);
            SoundEngine.PlaySound(SoundID.ResearchComplete, player.Center);
            return true;
        }
    }

    internal class RamCapacityUpgradeChip : BaseRamUpgradeChip
    {
        private const int MaxRamBonus = 1;

        protected override bool CanApplyUpgrade => RamSystem.BaseMaxRam < RamSystem.SoftMaxBaseMaxRam;

        protected override void ApplyUpgrade(Player player) {
            RamSystem.IncreaseBaseMaxRamBy(MaxRamBonus);
            RamSystem.Restore(MaxRamBonus);
        }
    }

    internal class RamRecoveryUpgradeChip : BaseRamUpgradeChip
    {
        private const float RecoveryBonus = 0.05f;

        protected override bool CanApplyUpgrade => true;

        protected override void ApplyUpgrade(Player player) => RamSystem.IncreaseBaseRecoveryRateBy(RecoveryBonus);
    }

    internal class RamUpgradeChipLootSystem : ModSystem
    {
        private const string SaveKeyInjected = "RamUpgradeChipsInjected";
        private static bool injected;

        public override void OnWorldLoad() => injected = false;

        public override void LoadWorldData(TagCompound tag) {
            injected = tag != null && tag.TryGet(SaveKeyInjected, out bool value) && value;
        }

        public override void SaveWorldData(TagCompound tag) {
            tag[SaveKeyInjected] = injected;
        }

        public override void PostWorldGen() {
            InjectUpgradeChips();
        }

        public override void PostUpdateWorld() {
            if (injected || Main.netMode == NetmodeID.MultiplayerClient) {
                return;
            }

            InjectUpgradeChips();
        }

        private static void InjectUpgradeChips() {
            int capacityChipType = ModContent.ItemType<RamCapacityUpgradeChip>();
            int recoveryChipType = ModContent.ItemType<RamRecoveryUpgradeChip>();

            for (int i = 0; i < Main.maxChests; i++) {
                Chest chest = Main.chest[i];
                if (chest == null || !IsLaboratorySecurityChest(chest)) {
                    continue;
                }

                bool addedCapacityChip = WorldGen.genRand.NextBool(2);
                bool addedRecoveryChip = WorldGen.genRand.NextBool(2);

                if (!addedCapacityChip && !addedRecoveryChip) {
                    addedCapacityChip = WorldGen.genRand.NextBool();
                    addedRecoveryChip = !addedCapacityChip;
                }

                if (addedCapacityChip) {
                    AddChestItem(chest, capacityChipType);
                }
                if (addedRecoveryChip) {
                    AddChestItem(chest, recoveryChipType);
                }
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
