#if DEBUG
using CalamityOverhaul.Content.ADV.Scenarios;
using CalamityOverhaul.Content.ADV.Scenarios.SupCal.End.EternalBlazingNows;
using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static System.Net.Mime.MediaTypeNames;

namespace CalamityOverhaul
{
    internal class TestProj : ModProjectile
    {
        public override string Texture => "CalamityOverhaul/icon";
        public override LocalizedText DisplayName => ItemLoader.GetItem(ModContent.ItemType<TestItem>()).DisplayName;
        public override void SetDefaults() {
            Projectile.width = Projectile.height = 66;
            Projectile.timeLeft = 400;
        }

        public override void AI() {
            Projectile.ai[0]++;
            if (Projectile.ai[0] == 90) {
                ScenarioManager.Reset<EternalBlazingNow>();
                ScenarioManager.Start<EternalBlazingNow>();
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            return false;
        }
    }

    internal class TestItem : ModItem
    {
        public override string Texture => "CalamityOverhaul/icon";
        public override void SetDefaults() {
            Item.width = 80;
            Item.height = 80;
            Item.damage = 9999;
            Item.DamageType = DamageClass.Default;
            Item.useAnimation = Item.useTime = 13;
            Item.useTurn = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 2.25f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.shootSpeed = 8f;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.value = 7;
            Item.rare = ItemRarityID.Yellow;
        }

        public override void UpdateInventory(Player player) {
            player.GetDamage(DamageClass.Generic) += 100f;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
            return false;
        }

        public override bool AltFunctionUse(Player player) {
            return true;
        }

        public override void HoldItem(Player player) {
        }

        public override bool? UseItem(Player player) {
            //ScenarioManager.Reset<GalacticCrisis>();
            //ScenarioManager.Start<GalacticCrisis>();
            //MachineWorld.Enter();
            //DropPodWorld.Enter();
            //ActorLoader.NewActor<DropPodActor>(player.Center, Vector2.Zero);
            //if (player.altFunctionUse == 0) {
            //    MachineWorld.Enter();
            //}
            //else {
            //    GargoyleSwarmPlayer.StartCutscene();
            //}
            //Sandevistan.IsActive = !Sandevistan.IsActive;
            //CyberwareUI.Instance.Toggle();

            //if (player.altFunctionUse == 0) {
            //    Cyberspace.Activate(player);   // 展开领域
            //    Cyberspace.SetLayer(3, player);
            //}
            //else {
            //    Cyberspace.Deactivate(); // 收缩关闭
            //}

            // 生成光束，ai[0] = 颜色主题 (0=蓝, 1=黄, 2=青)
            Projectile.NewProjectile(player.FromObjectGetParent(), player.Center, player.Center.To(Main.MouseWorld).UnitVector() * 12,
                ModContent.ProjectileType<CyberTraceBeamProj>(),
                20, 1, player.whoAmI, ai0: Main.rand.Next(3));

            //Cyberspace.SetLayer(3, player);
            //Cyberspace.Deactivate(); // 收缩关闭
            //ScenarioManager.Reset<FirstMetShepel>();
            //ScenarioManager.Start<FirstMetShepel>();
            return true;
        }
    }
}
#endif
