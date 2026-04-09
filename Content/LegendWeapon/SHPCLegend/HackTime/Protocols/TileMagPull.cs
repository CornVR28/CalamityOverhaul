using System;
using CalamityOverhaul.Common;
using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime.Protocols
{
    /// <summary>
    /// 磁力牵引协议：将物块及其附近掉落物吸引向玩家
    /// <br/>适合远程回收物品，对容器和家具类物块有效
    /// </summary>
    internal class TileMagPull : QuickHackDef
    {
        //牵引半径（像素）
        private const float PullRadius = 400f;
        //牵引持续时间（帧，5秒）
        private const int PullDuration = 60 * 5;

        public override void SetDefaults() {
            UploadTime = 60;
            RamCost = 2;
            Category = QuickHackCategory.TileManip;
            SupportedTargets = HackTargetKind.Tile;
        }

        public override int GetDuration() => PullDuration;

        public override bool OnApplyToTile(int tileX, int tileY, Player caster) {
            Vector2 center = new(tileX * 16f + 8f, tileY * 16f + 8f);

            //启动粒子
            for (int i = 0; i < 10; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(4f, 4f);
                PRTLoader.AddParticle(new PRT_Spark(center, vel, false, 25, 1.0f,
                    new Color(120, 80, 255)));
            }

            if (!VaultUtils.isServer) {
                SoundEngine.PlaySound(CWRSound.Hacker with { Volume = 0.35f, Pitch = 0.3f }, center);
            }

            return true;
        }

        public override bool OnTickTile(int tileX, int tileY, int elapsed) {
            Vector2 tileCenter = new(tileX * 16f + 8f, tileY * 16f + 8f);
            Player player = Main.LocalPlayer;
            Vector2 pullTarget = player.Center;

            //吸引周围掉落物向玩家
            for (int i = 0; i < Main.maxItems; i++) {
                Item item = Main.item[i];
                if (!item.active || item.noGrabDelay > 0) continue;
                float dist = Vector2.Distance(item.Center, tileCenter);
                if (dist > PullRadius) continue;

                //越近牵引力越强
                float strength = 1f - dist / PullRadius;
                Vector2 dir = (pullTarget - item.Center).SafeNormalize(Vector2.Zero);
                item.velocity += dir * strength * 0.8f;

                //限速
                if (item.velocity.Length() > 12f) {
                    item.velocity = Vector2.Normalize(item.velocity) * 12f;
                }
            }

            //脉冲粒子
            if (elapsed % 15 == 0) {
                float angle = elapsed * 0.1f;
                Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 20f;
                PRTLoader.AddParticle(new PRT_Spark(tileCenter + offset,
                    (pullTarget - tileCenter).SafeNormalize(Vector2.Zero) * 2f,
                    false, 20, 0.5f, new Color(120, 80, 255, 100)));
            }

            return true;
        }
    }
}
