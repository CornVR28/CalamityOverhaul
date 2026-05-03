namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    /// <summary>
    /// SHPC改件实物集合，所有改件统一复用Mewtwo工具贴图作为占位
    /// 通过<see cref="SHPCModuleItem.TintColor"/>+赛博朋克滤镜在视觉上做区分
    /// 命名严格按槽位类别分组，文件内汇总便于属性参数对比微调
    /// </summary>

    //═════════════════════════ BARREL 枪管 ═════════════════════════
    internal sealed class RapidBarrelModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Barrel;
        //快速节奏的青色霓虹
        public override Color TintColor => new(0, 240, 220);

        public override void Apply(ref ShootContext ctx) {
            ctx.AttackSpeedMul += 0.5f;
            ctx.DamageMul += -0.3f;
            ctx.SpreadMul += 0.4f;
        }
    }

    internal sealed class FocusBarrelModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Barrel;
        //聚束高能调用电蓝
        public override Color TintColor => new(60, 130, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.MergeBeams = true;
            ctx.BeamSpeedMul += 0.6f;
            ctx.HomingMul += 1.5f;
            ctx.MergedDamageBonus += 2f;
        }
    }

    internal sealed class ScattershotBarrelModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Barrel;
        //霰射狂暴的橙色调
        public override Color TintColor => new(255, 130, 30);

        public override void Apply(ref ShootContext ctx) {
            ctx.BeamCountAdd += 2;
            ctx.SpreadMul += 1.2f;
            ctx.DamageMul += -0.45f;
            ctx.BeamSpeedMul += -0.1f;
        }
    }

    internal sealed class HypersonicBarrelModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Barrel;
        //超音速主题黄色
        public override Color TintColor => new(255, 235, 60);

        public override void Apply(ref ShootContext ctx) {
            ctx.BeamSpeedMul += 0.8f;
            ctx.AttackSpeedMul += 0.20f;
            ctx.DamageMul += -0.15f;
            ctx.HomingMul += -0.5f;
        }
    }

    internal sealed class HeavyBarrelModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Barrel;
        //重型炮管赤红
        public override Color TintColor => new(220, 40, 60);

        public override void Apply(ref ShootContext ctx) {
            ctx.DamageMul += 0.65f;
            ctx.AttackSpeedMul += -0.3f;
            ctx.SpreadMul += -0.7f;
            ctx.MergedDamageBonus += 0.4f;
        }
    }

    //═════════════════════════ OPTIC 瞄具 ═════════════════════════
    internal sealed class PrecisionOpticModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Optic;
        //精确蓝绿色
        public override Color TintColor => new(80, 255, 200);

        public override void Apply(ref ShootContext ctx) {
            ctx.SpreadMul += -1.0f;
            ctx.CritAdd += 10;
        }
    }

    internal sealed class AdaptiveOpticModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Optic;
        //智能跟踪洋红
        public override Color TintColor => new(255, 70, 200);

        public override void Apply(ref ShootContext ctx) {
            ctx.HomingMul += 0.8f;
            ctx.AttackSpeedMul += 0.15f;
            ctx.DamageMul += -0.05f;
        }
    }

    internal sealed class ThermalOpticModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Optic;
        //热成像火粉
        public override Color TintColor => new(255, 90, 110);

        public override void Apply(ref ShootContext ctx) {
            ctx.HomingMul += 1.5f;
            ctx.CritAdd += 6;
            ctx.SpreadMul += -0.6f;
        }
    }

    internal sealed class HoloOpticModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Optic;
        //全息投影湖蓝
        public override Color TintColor => new(50, 200, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.SpreadMul += -0.7f;
            ctx.AttackSpeedMul += 0.18f;
            ctx.ManaCostMul += 0.10f;
        }
    }

    //═════════════════════════ POWER 能源 ═════════════════════════
    internal sealed class OverloadCoreModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Power;
        //过载电浆紫
        public override Color TintColor => new(180, 80, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.OrbSpeedMul += 0.4f;
            ctx.ChargeTimeMul += -0.2f;
        }
    }

    internal sealed class HighVoltageCoreModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Power;
        //高压电蓝
        public override Color TintColor => new(80, 180, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.DamageMul += 0.30f;
            ctx.MergedDamageBonus += 0.5f;
            ctx.ManaCostMul += 0.30f;
        }
    }

    internal sealed class CapacitorBankModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Power;
        //储能黄绿
        public override Color TintColor => new(200, 255, 80);

        public override void Apply(ref ShootContext ctx) {
            ctx.ChargeTimeMul += -0.45f;
            ctx.OrbSpeedMul += -0.15f;
            ctx.AttackSpeedMul += -0.05f;
        }
    }

    internal sealed class PlasmaInjectorModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Power;
        //等离子注入粉紫
        public override Color TintColor => new(255, 100, 220);

        public override void Apply(ref ShootContext ctx) {
            ctx.OrbSpeedMul += 0.55f;
            ctx.MergedDamageBonus += 0.75f;
            ctx.ChargeTimeMul += 0.20f;
        }
    }

    //═════════════════════════ STOCK 枪托 ═════════════════════════
    internal sealed class SteadyStockModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Stock;
        //沉稳金属灰
        public override Color TintColor => new(180, 200, 220);

        public override void Apply(ref ShootContext ctx) {
            ctx.AttackSpeedMul += -0.15f;
            ctx.DamageMul += 0.25f;
        }
    }

    internal sealed class KineticDamperModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Stock;
        //减震橄榄绿
        public override Color TintColor => new(140, 180, 90);

        public override void Apply(ref ShootContext ctx) {
            ctx.SpreadMul += -0.5f;
            ctx.AttackSpeedMul += -0.08f;
            ctx.CritAdd += 3;
        }
    }

    internal sealed class LightStockModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Stock;
        //碳纤维浅青
        public override Color TintColor => new(160, 240, 240);

        public override void Apply(ref ShootContext ctx) {
            ctx.AttackSpeedMul += 0.30f;
            ctx.DamageMul += -0.10f;
            ctx.SpreadMul += 0.15f;
        }
    }

    internal sealed class AssaultStockModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Stock;
        //突击橙红
        public override Color TintColor => new(255, 100, 60);

        public override void Apply(ref ShootContext ctx) {
            ctx.DamageMul += 0.20f;
            ctx.AttackSpeedMul += 0.10f;
            ctx.ManaCostMul += 0.25f;
        }
    }

    //═════════════════════════ GRIP 握把 ═════════════════════════
    internal sealed class HarmonyGripModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Grip;
        //节能薄荷绿
        public override Color TintColor => new(120, 255, 180);

        public override void Apply(ref ShootContext ctx) {
            ctx.ManaCostMul += -0.5f;
        }
    }

    internal sealed class EfficientGripModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Grip;
        //高效翠绿
        public override Color TintColor => new(60, 220, 120);

        public override void Apply(ref ShootContext ctx) {
            ctx.ManaCostMul += -0.25f;
            ctx.AttackSpeedMul += 0.12f;
        }
    }

    internal sealed class CrystalGripModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Grip;
        //水晶幻紫
        public override Color TintColor => new(200, 130, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.ManaCostMul += -0.3f;
            ctx.CritAdd += 5;
            ctx.ChargeTimeMul += 0.10f;
        }
    }

    internal sealed class BalancedGripModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Grip;
        //平衡青铜
        public override Color TintColor => new(220, 180, 120);

        public override void Apply(ref ShootContext ctx) {
            ctx.SpreadMul += -0.2f;
            ctx.AttackSpeedMul += 0.08f;
            ctx.DamageMul += 0.06f;
        }
    }

    //═════════════════════════ FRAME 机匣 ═════════════════════════
    internal sealed class ResonanceFrameModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Frame;
        //共振翡翠
        public override Color TintColor => new(80, 255, 160);

        public override void Apply(ref ShootContext ctx) {
            ctx.BeamCountAdd += 1;
        }
    }

    internal sealed class MultiCellFrameModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Frame;
        //多重荧绿
        public override Color TintColor => new(100, 255, 80);

        public override void Apply(ref ShootContext ctx) {
            ctx.BeamCountAdd += 2;
            ctx.DamageMul += -0.22f;
            ctx.SpreadMul += 0.20f;
        }
    }

    internal sealed class QuantumFrameModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Frame;
        //量子超紫
        public override Color TintColor => new(140, 80, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.HomingMul += 0.6f;
            ctx.OrbSpeedMul += 0.25f;
            ctx.ManaCostMul += 0.15f;
        }
    }

    internal sealed class VolatileFrameModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Frame;
        //不稳定毒黄
        public override Color TintColor => new(220, 255, 40);

        public override void Apply(ref ShootContext ctx) {
            ctx.BeamCountAdd += 1;
            ctx.CritAdd += 8;
            ctx.ManaCostMul += 0.30f;
            ctx.SpreadMul += 0.10f;
        }
    }
}
