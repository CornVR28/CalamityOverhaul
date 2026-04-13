using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    /// <summary>
    /// 第六门徒：巴多罗买（真言揭示）
    /// 能力：降低敌人的防御
    /// 象征物：刀
    /// </summary>
    internal class Bartholomew : BaseDisciple
    {
        public static LocalizedText TruthRevealText { get; private set; }

        public override int DiscipleIndex => 5;
        public override Color DiscipleColor => new(200, 100, 255); //真言紫
        public override int AbilityCooldownTime => 150;

        //巴多罗买揭示真相，运动沉稳
        protected override float OrbitSpeedMultiplier => 0.95f;
        protected override float VerticalWaveAmplitude => 14f;
        protected override float HorizontalWaveAmplitude => 8f;
        protected override float MovementSmoothness => 0.14f;

        //3D轨道：巴多罗买在中下层
        protected override float OrbitTiltAngle => 0.2f;
        protected override float OrbitTiltDirection => MathHelper.Pi * 0.9f;
        protected override float OrbitHeightLayer => -0.3f;

        public override void SetStaticDefaults() {
            TruthRevealText = this.GetLocalization(nameof(TruthRevealText), () => "真言揭示");
        }

        protected override void ExecuteAbility() {
            NPC target = FindNearestEnemy(300f);
            if (target != null) {
                target.defense = Math.Max(0, target.defense - 10);
                CombatText.NewText(target.Hitbox, Color.Purple, TruthRevealText.Value);
                //紫色光芒效果
                for (int i = 0; i < 15; i++) {
                    Dust d = Dust.NewDustPerfect(target.Center, DustID.PurpleTorch, Main.rand.NextVector2Circular(5f, 5f), 100, default, 1.5f);
                    d.noGravity = true;
                }
                SetCooldown(120);
            }
        }
    }
}
