using Terraria;

namespace CalamityOverhaul.Content.HackTimes
{
    /// <summary>
    /// 可被骇入目标的统一抽象
    /// <br/>在 <see cref="IScannable"/> 的扫描数据基础上，补齐被锁定/被骇入时所需的全部行为接口
    /// <br/>所有目标种类（NPC、物块、灵异 Actor、炮台、信号塔等）都应实现此接口
    /// <br/>新加目标种类只需新增对应的 <see cref="HackTargetType"/> 工厂类，不再需要修改任何分发器
    /// </summary>
    internal interface IHackTarget : IScannable
    {
        /// <summary>
        /// 该目标所属的注册类型工厂
        /// </summary>
        HackTargetType TargetType { get; }

        /// <summary>
        /// 目标锁定框的半宽与半高（屏幕像素，已含外扩 padding）
        /// <br/>由 HackTargetFrame 直接使用
        /// </summary>
        Vector2 LockFrameHalfSize { get; }

        /// <summary>
        /// 目标锁定框下方显示的目标名（一般是本地化好的 DisplayName）
        /// </summary>
        string LockFrameTitle { get; }

        /// <summary>
        /// 目标锁定框下方的副状态（如 NPC 血量百分比、炮台电路状态等）
        /// <br/>没有副状态时返回 false
        /// </summary>
        bool TryGetLockFrameStatus(out string text, out Color color);

        /// <summary>
        /// 上传完成后将协议作用到该目标上
        /// <br/>具体目标决定是否经过 <see cref="HackEffectTracker"/>（持续时间）或直接调用即时效果
        /// </summary>
        /// <returns>true 表示效果成功生效或已注册到追踪器</returns>
        bool ApplyHack(QuickHackDef hack, Player caster);

        /// <summary>
        /// 判断两个目标引用是否指向同一实体
        /// <br/>用于"点击同一目标不要重复选中"等判定，避免依赖 ReferenceEquals 与坐标比较的散乱实现
        /// </summary>
        bool TargetEquals(IHackTarget other);
    }
}
