using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Skills.Sandevistans
{
    /// <summary>
    /// 独立的GlobalProjectile，专门处理斯安威斯坦时缓对弹幕的影响。
    /// 不侵入CWRProjectile，由tModLoader自动加载和调度。
    /// </summary>
    internal class SandevistanProjectile : GlobalProjectile
    {
        public override bool PreAI(Projectile projectile) {
            if (!SandevistanTimeSlow.IsActive) {
                return true;
            }
            if (!SandevistanTimeSlow.ShouldAffectProjectile(projectile)) {
                return true;
            }

            int idx = projectile.whoAmI;
            //新弹幕首次进入时缓范围时记录速度
            if (!SandevistanTimeSlow.ProjHasCache[idx]) {
                SandevistanTimeSlow.ProjCachedVelocities[idx] = projectile.velocity;
                SandevistanTimeSlow.ProjHasCache[idx] = true;
            }

            Vector2 slowVel = SandevistanTimeSlow.ProjCachedVelocities[idx] * SandevistanTimeSlow.SlowFactor;

            //回滚位置然后按缩放速度缓慢前进
            projectile.position = projectile.oldPosition + slowVel;
            projectile.velocity = slowVel;
            projectile.timeLeft++;

            return false;
        }
    }
}
