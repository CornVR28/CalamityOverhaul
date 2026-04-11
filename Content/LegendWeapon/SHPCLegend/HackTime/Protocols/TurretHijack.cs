using CalamityOverhaul.Content.Industrials;
using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using InnoVault.TileProcessors;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime.Protocols
{
    /// <summary>
    /// 炮塔劫持协议：篡改炮塔IFF识别模块，使其在持续时间内翻转敌我判定
    /// <br/>敌对炮塔将转为攻击敌方NPC，友方炮塔不受影响
    /// </summary>
    internal class TurretHijack : QuickHackDef
    {
        //劫持持续时间（帧，12秒）
        private const int HijackDuration = 60 * 12;

        public override void SetDefaults() {
            UploadTime = 120;
            RamCost = 5;
            Category = QuickHackCategory.TileManip;
            SupportedTargets = HackTargetKind.Tile;
        }

        public override int GetDuration() => HijackDuration;

        public override bool CanApplyToTile(int tileX, int tileY) {
            if (!base.CanApplyToTile(tileX, tileY)) return false;
            if (!TryGetTurret(tileX, tileY, out var turret)) return false;
            //仅可对敌对炮塔使用，且未被劫持
            return !turret.Friend && !turret.IsHijacked;
        }

        public override bool OnApplyToTile(int tileX, int tileY, Player caster) {
            if (!TryGetTurret(tileX, tileY, out var turret)) return false;

            turret.IsHijacked = true;

            Vector2 center = turret.CenterInWorld;

            //接管粒子效果
            if (!VaultUtils.isServer) {
                for (int i = 0; i < 16; i++) {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(5f, 5f);
                    Color c = Color.Lerp(new Color(0, 255, 120), new Color(80, 255, 200), Main.rand.NextFloat());
                    PRTLoader.AddParticle(new PRT_Spark(center, vel, false, 30, 1.0f, c));
                }

                SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.5f, Pitch = 0.4f }, center);
            }

            return true;
        }

        public override bool OnTickTile(int tileX, int tileY, int elapsed) {
            if (!TryGetTurret(tileX, tileY, out var turret)) return false;

            //维持劫持状态
            turret.IsHijacked = true;

            //周期性粒子提示
            if (elapsed % 40 == 0 && !VaultUtils.isServer) {
                Vector2 center = turret.CenterInWorld;
                Vector2 vel = new(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1.5f, 0f));
                PRTLoader.AddParticle(new PRT_Spark(center, vel, false, 20, 0.5f,
                    new Color(0, 255, 120, 80)));
            }

            return true;
        }

        public override void OnRemoveTile(int tileX, int tileY) {
            if (!TryGetTurret(tileX, tileY, out var turret)) return;

            turret.IsHijacked = false;

            //解除劫持粒子
            if (!VaultUtils.isServer) {
                Vector2 center = turret.CenterInWorld;
                for (int i = 0; i < 8; i++) {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(3f, 3f);
                    PRTLoader.AddParticle(new PRT_Spark(center, vel, false, 20, 0.7f,
                        new Color(255, 60, 60)));
                }
            }
        }

        private static bool TryGetTurret(int tileX, int tileY, out BaseTurretTP turret) {
            turret = null;
            if (!VaultUtils.SafeGetTopLeft(tileX, tileY, out var topLeft)) return false;
            if (!TileProcessorLoader.TP_Point_To_Instance.TryGetValue(topLeft, out var tp)) return false;
            if (tp is not BaseTurretTP t || !tp.Active) return false;
            turret = t;
            return true;
        }
    }
}
