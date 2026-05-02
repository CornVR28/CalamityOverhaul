using Terraria;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    /// <summary>
    /// 改件聚合中心：根据 SHPC 物品上的 <see cref="SHPCData"/> 收集所有已装备改件，
    /// 调用其 <see cref="SHPCModuleItem.Apply"/> 得到最终射击上下文
    /// </summary>
    internal static class SHPCModificationSystem
    {
        /// <summary>
        /// 解析当前 SHPC 物品的射击上下文。物品/数据为空时返回中性默认值
        /// </summary>
        public static ShootContext Resolve(Item shpcItem) {
            ShootContext ctx = ShootContext.Default;
            if (shpcItem == null || shpcItem.IsAir) {
                return ctx;
            }
            CWRItem cwr = shpcItem.CWR();
            if (cwr == null || cwr.LegendData is not SHPCData data) {
                return ctx;
            }
            for (int i = 0; i < SHPCData.SlotCount; i++) {
                Item m = data.GetModule(i);
                if (m == null || m.ModItem is not SHPCModuleItem mod) {
                    continue;
                }
                mod.Apply(ref ctx);
            }
            return ctx;
        }

        /// <summary>
        /// 获取指定槽位上装备的改件 ModItem 实例（未装备返回 null）
        /// </summary>
        public static SHPCModuleItem GetEquippedAt(Item shpcItem, int slotIdx) {
            if (shpcItem == null || shpcItem.IsAir) {
                return null;
            }
            CWRItem cwr = shpcItem.CWR();
            if (cwr == null || cwr.LegendData is not SHPCData data) {
                return null;
            }
            Item m = data.GetModule(slotIdx);
            return m?.ModItem as SHPCModuleItem;
        }
    }
}
