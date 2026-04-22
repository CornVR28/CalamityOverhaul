using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.GatlinTurrets
{
    /// <summary>
    /// 加特林炮台发射的敌意子弹
    /// 使用原版Deadeye子弹贴图，拥有短暂的曳光尾迹与重力抵消，保持高速直线飞行
    /// </summary>
    internal class GatlinBullet : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BulletDeadeye;

        public override void SetStaticDefaults() {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 2;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI() {
            //原版Deadeye子弹贴图朝上，+π/2让视觉方向与速度方向一致
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            //沿途少量发光，在过去时代的黑暗色调下更易辨识
            Lighting.AddLight(Projectile.Center, 0.6f, 0.45f, 0.15f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            //拖尾效果基于旧位置复绘半透明子弹
            for (int i = 0; i < Projectile.oldPos.Length; i++) {
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Color trailColor = new Color(255, 200, 120, 0) * (fade * 0.4f);
                Main.spriteBatch.Draw(tex, trailPos, null, trailColor, Projectile.rotation,
                    origin, Projectile.scale * fade, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(tex, drawPos, null, new Color(255, 220, 150),
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
