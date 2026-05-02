using CalamityOverhaul.Common;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.RAMSystems
{
    internal abstract class BaseRamUpgradeChip : ModItem
    {
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
            Item.UseSound = CWRSound.ChipSet;
            Item.autoReuse = true;
        }

        public override bool CanUseItem(Player player) => CanApplyUpgrade;

        public override bool? UseItem(Player player) {
            ApplyUpgrade(player);
            SoundEngine.PlaySound(SoundID.ResearchComplete, player.Center);
            return true;
        }
    }
}
