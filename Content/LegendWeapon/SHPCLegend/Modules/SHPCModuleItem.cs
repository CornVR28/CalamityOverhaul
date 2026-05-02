using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    /// <summary>
    /// SHPC 改件物品基类，提供槽位类别声明与对 <see cref="ShootContext"/> 的修改入口
    /// 子类只需覆写 <see cref="SlotCategory"/>、<see cref="Apply"/>、<see cref="GetStatLines"/>
    /// </summary>
    internal abstract class SHPCModuleItem : ModItem
    {
        /// <summary>
        /// 该改件能装入的槽位类别
        /// </summary>
        public abstract SHPCSlotCategory SlotCategory { get; }

        /// <summary>
        /// 改件作用：修改传入的 <see cref="ShootContext"/>，按字段进行乘加叠加
        /// </summary>
        public abstract void Apply(ref ShootContext ctx);

        /// <summary>
        /// 改件属性差值文字列表（用于自定义悬浮描述框）
        /// 每行格式形如 "+50% ATK SPD" / "-30% DMG"，颜色由 UI 层根据正负选择
        /// </summary>
        public virtual IEnumerable<string> GetStatLines() => System.Array.Empty<string>();

        public override void SetDefaults() {
            Item.maxStack = 1;
            Item.width = 32;
            Item.height = 32;
            Item.rare = Terraria.ID.ItemRarityID.Yellow;
            Item.value = Item.sellPrice(0, 2, 0, 0);
        }
    }
}
