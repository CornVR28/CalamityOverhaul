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
            ctx.AttackSpeedMul += 0.4f;
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
            ctx.HomingMul += -0.5f;
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
            ctx.DamageMul += -0.3f;
        }
    }

    internal sealed class HypersonicBarrelModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Barrel;
        //超音速主题黄色
        public override Color TintColor => new(255, 235, 60);

        public override void Apply(ref ShootContext ctx) {
            ctx.BeamSpeedMul += 1f;
            ctx.AttackSpeedMul += 0.2f;
            ctx.DamageMul += -0.1f;
            ctx.HomingMul += -0.7f;
        }
    }

    internal sealed class HeavyBarrelModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Barrel;
        //重型炮管赤红
        public override Color TintColor => new(220, 40, 60);

        public override void Apply(ref ShootContext ctx) {
            ctx.DamageMul += 0.6f;
            ctx.AttackSpeedMul += -0.35f;
            ctx.SpreadMul += -0.5f;
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
            ctx.HomingMul += 0.5f;
            ctx.AttackSpeedMul += 0.05f;
            ctx.CritAdd += 5;
        }
    }

    internal sealed class ThermalOpticModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Optic;
        //热成像火粉
        public override Color TintColor => new(255, 90, 110);

        public override void Apply(ref ShootContext ctx) {
            ctx.HomingMul += 1f;
            ctx.CritAdd += 6;
            ctx.SpreadMul += -0.25f;
        }
    }

    internal sealed class HoloOpticModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Optic;
        //全息投影湖蓝
        public override Color TintColor => new(50, 200, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.SpreadMul += -0.55f;
            ctx.AttackSpeedMul += 0.15f;
            ctx.ManaCostMul += 0.15f;
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
            ctx.OrbSpeedMul += 0.45f;
            ctx.ChargeTimeMul += -0.25f;
        }
    }

    internal sealed class HighVoltageCoreModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Power;
        //高压电蓝
        public override Color TintColor => new(80, 180, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.DamageMul += 0.15f;
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
            ctx.ChargeTimeMul += -0.4f;
            ctx.OrbSpeedMul += -0.1f;
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
            ctx.OrbSpeedMul += 0.75f;
            ctx.MergedDamageBonus += 0.5f;
            ctx.ChargeTimeMul += 0.3f;
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
            ctx.DamageMul += 0.15f;
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
            ctx.AttackSpeedMul += 0.35f;
            ctx.DamageMul += -0.15f;
            ctx.SpreadMul += 0.2f;
        }
    }

    internal sealed class AssaultStockModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Stock;
        //突击橙红
        public override Color TintColor => new(255, 100, 60);

        public override void Apply(ref ShootContext ctx) {
            ctx.DamageMul += 0.10f;
            ctx.AttackSpeedMul += 0.10f;
            ctx.ManaCostMul += 0.2f;
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
            ctx.ManaCostMul += -0.35f;
        }
    }

    internal sealed class EfficientGripModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Grip;
        //高效翠绿
        public override Color TintColor => new(60, 220, 120);

        public override void Apply(ref ShootContext ctx) {
            ctx.ManaCostMul += -0.15f;
            ctx.AttackSpeedMul += 0.10f;
        }
    }

    internal sealed class CrystalGripModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Grip;
        //水晶幻紫
        public override Color TintColor => new(200, 130, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.ManaCostMul += -0.2f;
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
            ctx.DamageMul += -0.15f;
            ctx.SpreadMul += 0.3f;
        }
    }

    internal sealed class QuantumFrameModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Frame;
        //量子超紫
        public override Color TintColor => new(140, 80, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.HomingMul += 0.4f;
            ctx.OrbSpeedMul += 0.4f;
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

    //═════════════════════════ 行为型改件（依赖弹幕钩子触发的特殊效果） ═════════════════════════

    /// <summary>枪管：光束命中时引爆微型脉冲爆炸</summary>
    internal sealed class NovaBarrelModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Barrel;
        //新星橘红
        public override Color TintColor => new(255, 110, 50);

        public override void Apply(ref ShootContext ctx) {
            ctx.BeamExplodeOnHit = true;
            ctx.BeamExplodeRadius = 90f;
            //爆裂枪管自带较高的散布与法力开销
            ctx.SpreadMul += 0.25f;
            ctx.DamageMul += -0.20f;
            ctx.ManaCostMul += 0.20f;
        }
    }

    /// <summary>瞄具：光束消亡时分裂出 2 道副光束</summary>
    internal sealed class PrismOpticModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Optic;
        //棱镜彩光
        public override Color TintColor => new(190, 110, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.BeamSplitOnDeath += 2;
            //分光透镜会让原始光束略微短命，但暴击爬升
            ctx.BeamLifeMul += -0.15f;
            ctx.CritAdd += 4;
        }
    }

    /// <summary>能源：光束命中后向最近的敌人弹跳两次</summary>
    internal sealed class TeslaCoreModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Power;
        //特斯拉电弧蓝白
        public override Color TintColor => new(120, 220, 255);

        public override void Apply(ref ShootContext ctx) {
            ctx.BeamChainCount += 2;
            ctx.BeamChainRange = 280f;
            ctx.BeamExtraPierce += 1;
            ctx.ManaCostMul += 0.15f;
        }
    }

    /// <summary>枪托：能量球爆炸时反推玩家弹射</summary>
    internal sealed class RecoilStockModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Stock;
        //火药钢蓝灰
        public override Color TintColor => new(180, 180, 220);

        public override void Apply(ref ShootContext ctx) {
            ctx.OrbExplosionPropels = true;
            ctx.BeamLifeMul += 0.30f;
            ctx.AttackSpeedMul += -0.10f;
        }
    }

    /// <summary>握把：能量球爆炸时撒出迷你追踪光束</summary>
    internal sealed class SwarmGripModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Grip;
        //蜂群霓虹粉
        public override Color TintColor => new(255, 80, 180);

        public override void Apply(ref ShootContext ctx) {
            ctx.OrbDetonationMinions += 4;
            ctx.ManaCostMul += 0.20f;
            ctx.OrbSpeedMul += -0.10f;
        }
    }

    /// <summary>机匣：能量球蓄力时持续吸引附近敌人，爆炸范围扩大</summary>
    internal sealed class GravityFrameModule : SHPCModuleItem
    {
        public override string Texture => CWRConstant.Item_Tools + "Mewtwo";
        public override SHPCSlotCategory SlotCategory => SHPCSlotCategory.Frame;
        //引力深紫
        public override Color TintColor => new(90, 60, 200);

        public override void Apply(ref ShootContext ctx) {
            ctx.OrbDrainAura = true;
            ctx.OrbExplosionRadiusMul += 0.40f;
            ctx.ChargeTimeMul += 0.15f;
        }
    }
}
