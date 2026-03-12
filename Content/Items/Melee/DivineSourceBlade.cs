using CalamityOverhaul.Common;
using CalamityOverhaul.Content.MeleeModify.Core;
using InnoVault.Trails;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Melee
{
    internal class DivineSourceBlade : ModItem
    {
        public override string Texture => CWRConstant.Item_Melee + "DivineSourceBlade";
        public override void SetDefaults() {
            Item.height = 154;
            Item.width = 154;
            Item.damage = 560;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = Item.useTime = 15;
            Item.scale = 1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 5.5f;
            Item.UseSound = SoundID.Item60;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 33, 15, 0);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<DivineSourceBladeProjectile>();
            Item.shootSpeed = 18f;
            Item.SetKnifeHeld<DivineSourceBladeHeld>();
        }

        public override void ModifyWeaponCrit(Player player, ref float crit) => crit += 10;

        public override void AddRecipes() {
            if (!CWRRef.Has) {
                return;
            }
            CreateRecipe().
                AddIngredient(CWRID.Item_AuricBar, 5).
                AddIngredient(CWRID.Item_Terratomere).
                AddIngredient(CWRID.Item_Excelsus).
                AddTile(CWRID.Tile_CosmicAnvil).
                Register();
        }
    }

    internal class DivineSourceBladeHeld : BaseKnife
    {
        public override int TargetID => ModContent.ItemType<DivineSourceBlade>();
        public override string trailTexturePath => CWRConstant.Masking + "MotionTrail3";
        public override string gradientTexturePath => CWRConstant.ColorBar + "DragonRage_Bar";
        public override void SetKnifeProperty() {
            Projectile.width = Projectile.height = 112;
            canDrawSlashTrail = true;
            drawTrailCount = 34;
            distanceToOwner = -20;
            drawTrailTopWidth = 86;
            ownerOrientationLock = true;
            SwingData.starArg = 42;
            SwingData.baseSwingSpeed = 4.65f;
            unitOffsetDrawZkMode = 20;
            Length = 124;
            ShootSpeed = 18;
        }

        public override void UpdateCaches() {
            if (Time < 2) {
                return;
            }

            for (int i = drawTrailCount - 1; i > 0; i--) {
                oldRotate[i] = oldRotate[i - 1];
                oldDistanceToOwner[i] = oldDistanceToOwner[i - 1];
                oldLength[i] = oldLength[i - 1];
            }

            oldRotate[0] = safeInSwingUnit.RotatedBy(MathHelper.ToRadians(-8 * Projectile.spriteDirection)).ToRotation();
            oldDistanceToOwner[0] = distanceToOwner;
            oldLength[0] = Projectile.height * Projectile.scale;
        }

        public override bool PreInOwner() {
            ExecuteAdaptiveSwing(initialMeleeSize: 1, phase0SwingSpeed: 0.3f
                , phase1SwingSpeed: 8.2f, phase2SwingSpeed: 5f
                , phase0MeleeSizeIncrement: 0, phase2MeleeSizeIncrement: 0);
            if (Time % (6 * UpdateRate) == 0 && Projectile.IsOwnedByLocalPlayer()) {
                int types = ModContent.ProjectileType<DivineSourceBeam>();
                Vector2 vector2 = Owner.Center.To(Main.MouseWorld).UnitVector() * 3;
                Vector2 position = Owner.Center;
                Projectile.NewProjectile(
                    Source, position, vector2, types
                    , (int)(Item.damage * 1.25f)
                    , Item.knockBack
                    , Owner.whoAmI);
            }
            return base.PreInOwner();
        }

        public override void Shoot() {
            int type = ModContent.ProjectileType<DivineSourceBladeProjectile>();
            Projectile proj = Projectile.NewProjectileDirect(Source, ShootSpanPos, ShootVelocity, type, Projectile.damage, 0, Owner.whoAmI);
            proj.SetArrowRot();
        }

        public override void KnifeHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            if (Projectile.numHits == 0) {
                int proj = Projectile.NewProjectile(Source, Projectile.Center, Vector2.Zero
                    , CWRID.Proj_TerratomereSlashCreator,
                Projectile.damage / 3, 0, Projectile.owner, target.whoAmI, Main.rand.NextFloat(MathHelper.TwoPi));
                Main.projectile[proj].timeLeft = 30;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) {
            if (Projectile.numHits == 0) {
                int proj = Projectile.NewProjectile(Source, Projectile.Center, Vector2.Zero
                    , CWRID.Proj_TerratomereSlashCreator,
                Projectile.damage / 3, 0, Projectile.owner, target.whoAmI, Main.rand.NextFloat(MathHelper.TwoPi));
                Main.projectile[proj].timeLeft = 30;
            }
        }
    }

    internal class DivineSourceBeam : ModProjectile, IPrimitiveDrawable
    {
        public Vector2[] ControlPoints;
        private Player owner => CWRUtils.GetPlayerInstance(Projectile.owner);
        public const float EndRot = 60 * CWRUtils.atoR;
        public const float StarRot = -170 * CWRUtils.atoR;
        public const float LEndRot = -240 * CWRUtils.atoR;
        public const float LStarRot = -10 * CWRUtils.atoR;
        public bool Flipped => Projectile.ai[0] == 1f;
        public override string Texture => CWRConstant.Placeholder;
        private Trail Trail;

        public override void SetDefaults() {
            Projectile.width = 60;
            Projectile.height = 144;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = CWRRef.GetTrueMeleeDamageClass();
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
            => Projectile.rotation = Projectile.velocity.X > 0 ? -160 : 90;

        public IEnumerable<Vector2> GenerateSlashPoints(bool dir) {
            float starRot = StarRot;
            float endRot = EndRot;
            if (dir) {
                starRot = LStarRot - 120 * CWRUtils.atoR;
                endRot = LEndRot + 30 * CWRUtils.atoR - 120 * CWRUtils.atoR;
            }

            for (int i = 0; i < 30; i++) {
                float completion = MathHelper.Lerp(endRot + Projectile.rotation.AtoR(), starRot + Projectile.rotation.AtoR(), i / 30f);
                completion *= Math.Sign(Projectile.velocity.X) * -1;
                yield return completion.ToRotationVector2() * 84f;
            }
        }

        public override void AI() {
            if (owner != null) {
                Projectile.position += owner.CWR().PlayerPositionChange;
            }
            Projectile.Opacity = Utils.GetLerpValue(Projectile.localAI[0], 26f, Projectile.timeLeft, clamped: true);
            Projectile.velocity *= 0.91f;
            Projectile.scale *= 1.03f;
            Projectile.rotation -= 5f * Math.Sign(Projectile.velocity.X);
        }

        public float GetWidthFunc(float completionRatio) {
            return Projectile.scale * 50f;
        }

        public Color GetColorFunc(Vector2 completionRatio) {
            float sengs = MathF.Sin(completionRatio.X * MathF.PI);
            if (completionRatio.X < 0.4f) {
                sengs = MathF.Pow(completionRatio.X, 3) * 13;
            }
            return Color.Lime * sengs * Projectile.Opacity;
        }

        void IPrimitiveDrawable.DrawPrimitives() {
            if (ControlPoints == null || ControlPoints.Length == 0) {
                return;
            }

            //准备轨迹点 - 根据方向翻转处理
            Vector2[] positions = new Vector2[ControlPoints.Length];
            bool facingLeft = Projectile.velocity.X < 0;

            for (int i = 0; i < ControlPoints.Length; i++) {
                Vector2 offset = ControlPoints[i] + ControlPoints[i].SafeNormalize(Vector2.Zero) * (Projectile.scale - 1f) * 70f;

                // 如果朝左，需要水平翻转控制点
                if (facingLeft) {
                    offset.X = -offset.X;
                }

                positions[i] = offset + Projectile.Center + new Vector2(0, -60);
            }

            //创建或更新 Trail
            Trail ??= new Trail(positions, GetWidthFunc, GetColorFunc);
            Trail.TrailPositions = positions;

            //使用 InnoVault 的绘制方法
            Effect effect = EffectLoader.GradientTrail.Value;
            effect.Parameters["transformMatrix"].SetValue(VaultUtils.GetTransfromMatrix());
            effect.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects * 0.08f);
            effect.Parameters["uTimeG"].SetValue(Main.GlobalTimeWrappedHourly * 0.2f);
            effect.Parameters["udissolveS"].SetValue(1f);
            effect.Parameters["uBaseImage"].SetValue(CWRUtils.GetT2DValue(CWRConstant.Masking + "SlashFlatBlurHVMirror"));
            effect.Parameters["uFlow"].SetValue(CWRAsset.Airflow.Value);
            effect.Parameters["uGradient"].SetValue(CWRUtils.GetT2DValue(CWRConstant.ColorBar + "DragonRage_Bar"));
            effect.Parameters["uDissolve"].SetValue(CWRAsset.Placeholder_White.Value);

            Main.graphics.GraphicsDevice.BlendState = BlendState.Additive;
            for (int j = 0; j < 3; j++) {
                Trail?.DrawTrail(effect);
            }
            Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        }

        public override bool PreDraw(ref Color lightColor) {
            ControlPoints = GenerateSlashPoints(Projectile.velocity.X < 0).ToArray();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            bool collBool = false;
            float point = 0;
            Vector2 starPos = Projectile.Center;
            for (int i = 0; i < 20; i++) {
                Vector2 endPos = Projectile.Center + MathHelper.ToRadians(-160 + 11 * i).ToRotationVector2() * Projectile.scale * 120;
                if (Projectile.velocity.X < 0)
                    endPos = Projectile.Center + MathHelper.ToRadians(20 - 11 * i).ToRotationVector2() * Projectile.scale * 120;
                collBool = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), starPos, endPos, 32, ref point);
                if (collBool) {
                    break;
                }
            }

            return collBool;
        }
    }

    internal class DivineSourceBladeProjectile : ModProjectile, IPrimitiveDrawable
    {
        public override string Texture => CWRConstant.Projectile_Melee + "DivineSourceBeam";
        private Trail Trail;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 25;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults() {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.extraUpdates = 2;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            Player player = Main.player[Projectile.owner];
            Item item = player.GetItem();
            if (Projectile.numHits == 0 && item.type == ModContent.ItemType<DivineSourceBlade>()) {
                int proj = Projectile.NewProjectile(new EntitySource_ItemUse(player, item), Projectile.Center, Vector2.Zero
                    , CWRID.Proj_TerratomereSlashCreator,
                Projectile.damage, 0, Projectile.owner, target.whoAmI, Main.rand.NextFloat(MathHelper.TwoPi));
                Main.projectile[proj].timeLeft = 30;
            }
        }

        public float GetWidthFunc(float completionRatio) {
            float amount = (float)Math.Pow(1f - completionRatio, 3.0);
            return MathHelper.Lerp(0f, 22f * Projectile.scale * Projectile.Opacity, amount);
        }

        public Color GetColorFunc(Vector2 completionRatio) {
            float amount = MathHelper.Lerp(0.65f, 1f, (float)Math.Cos((0f - Main.GlobalTimeWrappedHourly) * 3f) * 0.5f + 0.5f);
            float num = Utils.GetLerpValue(1f, 0.64f, completionRatio.X, clamped: true) * Projectile.Opacity;

            Color value = Color.Lerp(new Color(255, 223, 186), new Color(255, 218, 185), (float)Math.Sin(completionRatio.X * MathF.PI * 1.6f - Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);

            return Color.Lerp(new Color(255, 248, 220), value, amount) * num;
        }

        void IPrimitiveDrawable.DrawPrimitives() {
            if (Projectile.oldPos == null || Projectile.oldPos.Length == 0) {
                return;
            }

            //准备轨迹点
            Vector2[] positions = new Vector2[Projectile.oldPos.Length];
            for (int i = 0; i < Projectile.oldPos.Length; i++) {
                if (Projectile.oldPos[i] == Vector2.Zero) {
                    Projectile.oldPos[i] = Projectile.Center;
                }
                positions[i] = Projectile.oldPos[i] + Projectile.Size * 0.5f;
            }

            //创建或更新 Trail
            Trail ??= new Trail(positions, GetWidthFunc, GetColorFunc);
            Trail.TrailPositions = positions;

            //使用 InnoVault 的绘制方法
            Effect effect = EffectLoader.GradientTrail.Value;
            effect.Parameters["transformMatrix"].SetValue(VaultUtils.GetTransfromMatrix());
            effect.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects * 0.08f);
            effect.Parameters["uTimeG"].SetValue(Main.GlobalTimeWrappedHourly * 0.2f);
            effect.Parameters["udissolveS"].SetValue(1f);
            effect.Parameters["uBaseImage"].SetValue(CWRUtils.GetT2DValue(CWRConstant.Masking + "SlashFlatBlurHVMirror"));
            effect.Parameters["uFlow"].SetValue(CWRAsset.Airflow.Value);
            effect.Parameters["uGradient"].SetValue(CWRUtils.GetT2DValue(CWRConstant.ColorBar + "DragonRage_Bar"));
            effect.Parameters["uDissolve"].SetValue(CWRAsset.Extra_193.Value);

            Main.graphics.GraphicsDevice.BlendState = BlendState.Additive;
            Trail?.DrawTrail(effect);
            Main.graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D mainValue = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(
                mainValue,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation + MathHelper.PiOver2,
                mainValue.GetOrig(),
                Projectile.scale,
                Projectile.velocity.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
                0
                );
            return false;
        }
    }
}
