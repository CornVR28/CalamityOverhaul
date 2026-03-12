using CalamityOverhaul.Content.RangedModify.Core;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Ranged
{
    internal class BarrenBow : ModItem
    {
        public override string Texture => CWRConstant.Item_Ranged + "BarrenBow";
        public override void SetDefaults() {
            Item.damage = 28;
            Item.width = 32;
            Item.height = 58;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 4f;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.Ranged;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shootSpeed = 12f;
            Item.UseSound = SoundID.Item5;
            Item.useAmmo = AmmoID.Arrow;
            Item.value = Item.buyPrice(0, 2, 15, 0);
            Item.rare = CWRID.Rarity_PureGreen;
            Item.SetHeldProj<BarrenBowHeld>();
        }

        public override void AddRecipes() {
            if (!CWRRef.Has) {
                CreateRecipe()
                .AddIngredient(ItemID.LightShard, 2)
                .AddIngredient(ItemID.AntlionMandible, 5)
                .AddIngredient(ItemID.HellwingBow)
                .AddTile(TileID.Anvils)
                .Register();
                return;
            }
            CreateRecipe()
                .AddIngredient(ItemID.LightShard, 2)
                .AddIngredient(ItemID.AntlionMandible, 5)
                .AddIngredient(ItemID.HellwingBow)
                .AddIngredient(CWRID.Item_LunarianBow)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    internal class BarrenBowHeld : BaseBow//TODO:等待重做，目前失去了特殊效果
    {
        public override string Texture => CWRConstant.Item_Ranged + "BarrenBow";
        public override int TargetID => ModContent.ItemType<BarrenBow>();
        public override void SetRangedProperty() {
            DrawArrowMode = -18;
            //InOwner_HandState_AlwaysSetInFireRoding = true;
            BowstringData.DeductRectangle = new Rectangle(18, 22, 2, 38);
            BowstringData.TopBowOffset = new Vector2(18, 20);
            BowstringData.BottomBowOffset = new Vector2(18, 12);
        }
    }
}
