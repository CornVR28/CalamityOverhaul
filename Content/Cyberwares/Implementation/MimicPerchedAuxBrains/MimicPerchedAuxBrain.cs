using Terraria;
using Terraria.ID;

namespace CalamityOverhaul.Content.Cyberwares.Implementation.MimicPerchedAuxBrains
{
    /// <summary>
    /// 拟态栖置副脑义体物品
    /// 装备后将玩家拆解为四个幻象，遭受致命攻击时真身借幻象之乱免疫伤害，
    /// 幻象同时冲向袭击者引爆，伤害取决于敌怪原始攻击力
    /// </summary>
    internal class MimicPerchedAuxBrain : BaseCyberware
    {
        public override CyberwareSlotCategory SlotCategory => CyberwareSlotCategory.FrontalCortex;

        public override int CapacityCost => 4;

        /// <summary>
        /// 触发后的冷却时间，单位帧
        /// </summary>
        public virtual int TriggerCooldown => 600;

        /// <summary>
        /// 幻象自爆伤害对原始受击伤害的倍率
        /// </summary>
        public virtual float DamageScaling => 2.5f;

        /// <summary>
        /// 幻象绕主体旋转的半径
        /// </summary>
        public virtual float OrbitRadius => 64f;

        public override void SetDefaults() {
            base.SetDefaults();
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(0, 8, 0, 0);
        }

        public override void OnEquip(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                MimicPerchedAuxBrainPlayer.RequestRespawnPhantoms(player);
            }
        }

        public override void OnUnequip(Player player) {
            if (player.whoAmI == Main.myPlayer) {
                MimicPerchedAuxBrainPlayer.ClearPhantoms(player);
            }
        }
    }
}
