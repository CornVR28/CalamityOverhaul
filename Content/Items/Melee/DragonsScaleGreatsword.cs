using CalamityOverhaul.Content.MeleeModify.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Melee
{
    internal class DragonsScaleGreatsword : ModItem
    {
        public override string Texture => CWRConstant.Item_Melee + "DragonsScaleGreatsword";
        public override void SetDefaults() {
            Item.height = 54;
            Item.width = 54;
            Item.damage = 556;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = Item.useTime = 16;
            Item.useTurn = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 2.5f;
            Item.UseSound = SoundID.Item60;
            Item.channel = true;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 4, 75, 0);
            Item.rare = CWRID.Rarity_BurnishedAuric;
            Item.shoot = ModContent.ProjectileType<DragonsScaleGreatswordBeam>();
            Item.shootSpeed = 7f;
            Item.SetKnifeHeld<DragonsScaleGreatswordHeld>();
        }

        public override void ModifyWeaponCrit(Player player, ref float crit) => crit += 3;

        public override void AddRecipes() {
            if (!CWRRef.Has) {
                return;
            }
            CreateRecipe().
                AddIngredient(CWRID.Item_PerennialBar, 15).
                AddIngredient(CWRID.Item_UelibloomBar, 15).
                AddIngredient(ItemID.ChlorophyteBar, 15).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }

    internal class DragonsScaleGreatswordHeld : BaseKnife
    {
        public override int TargetID => ModContent.ItemType<DragonsScaleGreatsword>();
        public override string trailTexturePath => CWRConstant.Masking + "MotionTrail2";
        public override string gradientTexturePath => CWRConstant.ColorBar + "DragonsScaleGreatsword_Bar";
        public override void SetKnifeProperty() {
            Projectile.width = Projectile.height = 112;
            drawTrailHighlight = false;
            canDrawSlashTrail = true;
            drawTrailCount = 4;
            distanceToOwner = -20;
            drawTrailTopWidth = 86;
            ownerOrientationLock = true;
            SwingData.starArg = 48;
            SwingData.baseSwingSpeed = 5;
            Length = 124;
        }

        public override bool PreInOwner() {
            ExecuteAdaptiveSwing(initialMeleeSize: 1, phase0SwingSpeed: 0.6f
                , phase1SwingSpeed: 6.6f, phase2SwingSpeed: 6f
                , phase0MeleeSizeIncrement: 0, phase2MeleeSizeIncrement: 0);
            return base.PreInOwner();
        }

        public override void MeleeEffect() {
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.JungleSpore);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].scale = Main.rand.NextFloat(0.5f, 2.2f);
        }

        public override void Shoot() {
            int type = ModContent.ProjectileType<DragonsScaleGreatswordBeam>();
            Projectile.NewProjectile(Source, ShootSpanPos, ShootVelocity, type, Item.damage / 2, 0, Owner.whoAmI);
        }

        public override void KnifeHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (CWRLoad.WormBodys.Contains(target.type) && !Main.rand.NextBool(5)) {
                return;
            }
            int type = ModContent.ProjectileType<SporeCloud>();
            target.AddBuff(BuffID.Poisoned, 1200);
            if (Owner.ownedProjectileCounts[type] < 220) {
                for (int i = 0; i < 3; i++) {
                    Vector2 spanPos = target.Center + new Vector2(Main.rand.Next(-723, 724), Main.rand.Next(-553, 0));
                    int proj = Projectile.NewProjectile(Owner.GetSource_FromThis(), spanPos
                        , spanPos.To(target.Center).UnitVector() * Main.rand.Next(9, 13), type, Item.damage / 2, 0, Owner.whoAmI);
                    Main.projectile[proj].timeLeft = 120;
                    Main.projectile[proj].scale = 1.2f + Main.rand.NextFloat(0.3f);
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.Poisoned, 600);
    }

    internal class DragonsScaleGreatswordBeam : ModProjectile
    {
        public override string Texture => CWRConstant.Projectile_Melee + "SporeCloud";
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = Projectile.height = 24;
            Projectile.penetrate = 1;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.MaxUpdates = 3;
            Projectile.timeLeft = 120 * Projectile.MaxUpdates;
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            for (int i = 0; i < 3; i++) {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Main.rand.NextFloat(220 * CWRUtils.atoR, 320 * CWRUtils.atoR).ToRotationVector2() * Main.rand.Next(5, 11)
                    , ModContent.ProjectileType<SporeCloud>(), Projectile.damage / 3, Projectile.knockBack, Projectile.owner);
            }
            Projectile.Explode(32);
            return true;
        }

        public override void AI() {
            Projectile.scale += 0.01f;
            for (int i = 0; i < 3; i++) {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.JungleSpore, Projectile.velocity.X, Projectile.velocity.Y);
                Main.dust[dust].noGravity = true;
                CWRUtils.SpanCycleDust(Projectile, DustID.JungleTorch, DustID.JungleTorch);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            for (int i = 0; i < Main.rand.Next(3, 6); i++) {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Main.rand.NextVector2Unit() * Main.rand.Next(6, 9)
                    , ModContent.ProjectileType<SporeCloud>(), Projectile.damage / 3, Projectile.knockBack, Projectile.owner);
            }
            Projectile.Explode(42);
            target.AddBuff(BuffID.Poisoned, 1200);
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    internal class SporeCloud : ModProjectile
    {
        public override string Texture => CWRConstant.Projectile_Melee + "SporeCloud";
        private int startCanHitCooldown;//弹幕堆叠所会造成极高伤害的问题始终存在，所以使用这个控制开始造成伤害的时机来错开伤害阶段
        public override void SetDefaults() {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = Projectile.height = 24;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
            startCanHitCooldown = Main.rand.Next(Projectile.localNPCHitCooldown);
        }

        public override void AI() {
            Projectile.velocity *= 0.985f;
            Projectile.scale += 0.013f;
            float maxShaking = 20;
            Projectile.rotation += Math.Sign(Projectile.velocity.X) * 0.05f;
            if (Projectile.rotation > MathHelper.ToRadians(maxShaking))
                Projectile.rotation = MathHelper.ToRadians(maxShaking);
            if (Projectile.rotation < MathHelper.ToRadians(-maxShaking))
                Projectile.rotation = MathHelper.ToRadians(-maxShaking);
            VaultUtils.ClockFrame(ref Projectile.frame, 5, 3);
        }

        public override bool? CanHitNPC(NPC target) => Projectile.timeLeft >= 90 - startCanHitCooldown ? false : base.CanHitNPC(target);
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            target.AddBuff(BuffID.Poisoned, 1200);
            Projectile.timeLeft -= 15;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D value = TextureAssets.Projectile[Type].Value;
            Rectangle rectangle = value.GetRectangle(Projectile.frame, 4);
            Main.EntitySpriteDraw(value, Projectile.Center - Main.screenPosition, rectangle, lightColor * (Projectile.timeLeft / 30f)
                , Projectile.rotation, rectangle.Size() / 2, Projectile.scale * 0.8f, 0, 0);
            return false;
        }
    }
}
