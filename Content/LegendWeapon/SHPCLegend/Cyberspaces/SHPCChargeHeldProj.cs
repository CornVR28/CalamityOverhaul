using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces
{
    /// <summary>
    /// SHPC 右键蓄力时的手持弹幕
    /// <br/>负责绘制武器贴图、控制手臂动画、提供枪口位置
    /// <br/>由 <see cref="SHPCOverride"/> 的 On_Shoot 生成，
    /// 同时生成 <see cref="CyberChargeOrbProj"/> 挂在枪口
    /// </summary>
    internal class SHPCChargeHeldProj : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        private Player Owner => Main.player[Projectile.owner];

        /// <summary>
        /// 武器贴图偏移量
        /// </summary>
        private static readonly Vector2 GunOffset = new(27f, -10f);

        /// <summary>
        /// 枪口距离武器中心的前方距离（像素）
        /// </summary>
        private const float TipDistance = 90f;

        /// <summary>
        /// 枪口世界坐标，供 CyberChargeOrbProj 查询
        /// </summary>
        public Vector2 TipPosition => Projectile.Center
            + Vector2.UnitX.RotatedBy(Projectile.rotation) * TipDistance
            + Vector2.UnitY * GunOffset.Y;

        public override void SetDefaults() {
            Projectile.width = 70;
            Projectile.height = 70;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
        }

        public override bool? CanDamage() => false;

        public override void AI() {
            if (!Owner.active || Owner.dead) {
                Projectile.Kill();
                return;
            }

            // 右键持续按住时保持存活
            if (Owner.PressKey(false)) {
                Projectile.timeLeft = 60;
            }

            // 瞄准方向
            Vector2 aimDir = (Main.MouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX);
            float rotation = aimDir.ToRotation();

            // 更新弹幕状态
            Projectile.rotation = rotation;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = Owner.Center;

            // 玩家手臂与朝向
            Owner.ChangeDir(Math.Sign(aimDir.X));
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
                (Owner.Center - Main.MouseWorld).ToRotation() * Owner.gravDir + MathHelper.PiOver2);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
        }

        public override bool PreDraw(ref Color lightColor) {
            // 手动绘制 SHPC 武器贴图
            Texture2D weaponTex = TextureAssets.Item[CWRID.Item_SHPC].Value;
            if (weaponTex == null) return false;

            float rotation = Projectile.rotation;
            Vector2 position = Owner.Center - Main.screenPosition
                + Vector2.UnitX.RotatedBy(rotation) * GunOffset.X
                + Vector2.UnitY * GunOffset.Y;

            SpriteEffects sp = Main.MouseWorld.X < Owner.Center.X
                ? SpriteEffects.FlipVertically
                : SpriteEffects.None;

            Main.EntitySpriteDraw(weaponTex, position, null, lightColor, rotation,
                weaponTex.Size() / 2f, 1f, sp);

            return false;
        }
    }
}
