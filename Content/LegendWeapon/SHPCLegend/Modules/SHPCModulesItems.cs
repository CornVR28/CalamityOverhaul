using System.Collections.Generic;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    /// <summary>
    /// SHPC 改件实物集合，所有改件统一复用 Mewtwo 工具贴图作为占位
    /// 命名严格按槽位类别分组，文件内汇总便于属性参数对比微调
    /// </summary>
    internal sealed class RapidBarrelModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Barrel;

        public override void Apply(ref ShootContext ctx) {
            ctx.AttackSpeedMul *= 1.5f;
            ctx.DamageMul *= 0.7f;
            ctx.SpreadMul *= 1.4f;
        }

        public override IEnumerable<string> GetStatLines() {
            yield return "+50% ATK SPEED";
            yield return "-30% DAMAGE";
            yield return "+40% SPREAD";
        }
    }

    internal sealed class FocusBarrelModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Barrel;

        public override void Apply(ref ShootContext ctx) {
            ctx.MergeBeams = true;
            ctx.BeamSpeedMul *= 1.6f;
            ctx.HomingMul *= 2.5f;
            ctx.MergedDamageBonus *= 3f;
        }

        public override IEnumerable<string> GetStatLines() {
            yield return "+MERGE 3 BEAMS INTO 1";
            yield return "+60% BEAM VELOCITY";
            yield return "+150% HOMING";
            yield return "+200% MERGED DMG";
        }
    }

    internal sealed class PrecisionOpticModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Optic;

        public override void Apply(ref ShootContext ctx) {
            ctx.SpreadMul *= 0f;
            ctx.CritAdd += 10;
        }

        public override IEnumerable<string> GetStatLines() {
            yield return "-100% SPREAD";
            yield return "+10% CRIT";
        }
    }

    internal sealed class OverloadCoreModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Power;

        public override void Apply(ref ShootContext ctx) {
            ctx.OrbSpeedMul *= 1.4f;
            ctx.ChargeTimeMul *= 0.8f;
        }

        public override IEnumerable<string> GetStatLines() {
            yield return "+40% ORB SPEED";
            yield return "-20% CHARGE TIME";
        }
    }

    internal sealed class SteadyStockModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Stock;

        public override void Apply(ref ShootContext ctx) {
            ctx.AttackSpeedMul *= 0.85f;
            ctx.DamageMul *= 1.25f;
        }

        public override IEnumerable<string> GetStatLines() {
            yield return "-15% ATK SPEED";
            yield return "+25% DAMAGE";
        }
    }

    internal sealed class HarmonyGripModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Grip;

        public override void Apply(ref ShootContext ctx) {
            ctx.ManaCostMul *= 0.5f;
        }

        public override IEnumerable<string> GetStatLines() {
            yield return "-50% MANA COST";
        }
    }

    internal sealed class ResonanceFrameModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Frame;

        public override void Apply(ref ShootContext ctx) {
            ctx.BeamCountAdd += 1;
        }

        public override IEnumerable<string> GetStatLines() {
            yield return "+1 BEAM PER VOLLEY";
        }
    }
}
