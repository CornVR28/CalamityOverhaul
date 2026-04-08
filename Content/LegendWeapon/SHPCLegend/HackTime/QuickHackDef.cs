using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime
{
    /// <summary>
    /// 骇入协议类别
    /// </summary>
    internal enum QuickHackCategory
    {
        /// <summary>
        /// 致命型：直接造成伤害
        /// </summary>
        Lethal,
        /// <summary>
        /// 控制型：限制目标行动
        /// </summary>
        Control,
        /// <summary>
        /// 隐匿型：干扰目标感知
        /// </summary>
        Covert,
        /// <summary>
        /// 传播型：扩散至附近目标
        /// </summary>
        Contagion
    }

    /// <summary>
    /// 快速骇入协议基类
    /// <br/>所有骇入协议必须继承此类，通过VaultType自动注册
    /// <br/>子类通过<see cref="SetDefaults"/>设置属性，通过<see cref="OnApply"/>实现效果
    /// </summary>
    internal abstract class QuickHackDef : VaultType<QuickHackDef>, ILocalizedModType
    {
        #region 静态注册表

        /// <summary>
        /// 类型 → 注册ID的映射
        /// </summary>
        public static readonly Dictionary<Type, int> TypeToID = [];
        /// <summary>
        /// 注册ID → 实例的映射
        /// </summary>
        public static readonly Dictionary<int, QuickHackDef> IDToInstance = [];

        /// <summary>
        /// 已注册的协议总数
        /// </summary>
        public static int Count => Instances.Count;

        /// <summary>
        /// 获取指定类型的协议实例
        /// </summary>
        public static T Get<T>() where T : QuickHackDef {
            if (TypeToID.TryGetValue(typeof(T), out int id)
                && IDToInstance.TryGetValue(id, out var inst)
                && inst is T t) {
                return t;
            }
            return null;
        }

        /// <summary>
        /// 通过索引获取协议（等同于旧的QuickHackRegistry.All[i]）
        /// </summary>
        public static QuickHackDef GetByIndex(int index) {
            if (index >= 0 && index < Instances.Count)
                return Instances[index];
            return null;
        }

        #endregion

        #region 本地化

        public string LocalizationCategory => "QuickHack";
        /// <summary>
        /// 协议显示名称（本地化）
        /// </summary>
        public LocalizedText DisplayName => this.GetLocalization(nameof(DisplayName), PrettyPrintName);
        /// <summary>
        /// 协议效果描述（本地化）
        /// </summary>
        public LocalizedText Description => this.GetLocalization(nameof(Description), () => "");

        #endregion

        #region 实例属性

        /// <summary>
        /// 注册序号（0开始，等同于在Instances列表中的索引）
        /// </summary>
        public int SlotIndex { get; private set; } = -1;
        /// <summary>
        /// 上传所需时间（帧数，60帧为1秒）
        /// </summary>
        public int UploadTime { get; set; } = 60;
        /// <summary>
        /// 执行该协议所需消耗的RAM单元数
        /// </summary>
        public int RamCost { get; set; } = 2;
        /// <summary>
        /// 协议类别
        /// </summary>
        public QuickHackCategory Category { get; set; } = QuickHackCategory.Lethal;

        #endregion

        #region VaultType生命周期

        protected sealed override void VaultRegister() {
            Instances.Add(this);
            SlotIndex = Instances.Count - 1;
            TypeToID[GetType()] = SlotIndex;
            IDToInstance[SlotIndex] = this;
        }

        public override void VaultSetup() {
            //触发本地化加载
            _ = DisplayName;
            _ = Description;
            SetDefaults();
        }

        public override void Unload() {
            TypeToID.Clear();
            IDToInstance.Clear();
        }

        #endregion

        #region 子类重写接口

        /// <summary>
        /// 设置协议默认属性（UploadTime、RamCost、Category等）
        /// </summary>
        public virtual void SetDefaults() { }

        /// <summary>
        /// 当协议上传完成时对目标NPC施加效果
        /// </summary>
        /// <param name="target">骇入的目标NPC</param>
        /// <param name="caster">发起骇入的玩家</param>
        /// <returns>返回true表示效果成功施加</returns>
        public virtual bool OnApply(NPC target, Player caster) => false;

        /// <summary>
        /// 驱动协议效果的帧更新（由效果管理器调用）
        /// <br/>自动处理持续时间检查，到期后返回false并触发<see cref="OnRemove"/>
        /// <br/>子类无需手动检查GetDuration
        /// </summary>
        /// <returns>返回false表示效果应被移除</returns>
        public bool TickEffect(NPC target, int elapsed) {
            int dur = GetDuration();
            if (dur > 0 && elapsed >= dur) return false;
            return OnTick(target, elapsed);
        }

        /// <summary>
        /// 协议效果的持续帧逻辑（用于持续性效果如灼烧等）
        /// <br/>仅在持续时间内被<see cref="TickEffect"/>调用，不需要手动检查时长
        /// </summary>
        /// <param name="target">受影响的目标NPC</param>
        /// <param name="elapsed">效果已持续的帧数</param>
        /// <returns>返回false表示效果提前结束</returns>
        public virtual bool OnTick(NPC target, int elapsed) => true;

        /// <summary>
        /// 协议效果被移除或到期时调用（清理工作）
        /// </summary>
        /// <param name="target">受影响的目标NPC</param>
        public virtual void OnRemove(NPC target) { }

        /// <summary>
        /// 判断该协议是否可对指定目标使用
        /// <br/>默认返回true，子类可按目标类型、状态等做限制
        /// </summary>
        public virtual bool CanApplyTo(NPC target) => target != null && target.active;

        /// <summary>
        /// 获取该协议的效果持续时间（帧），0表示即时效果无持续
        /// </summary>
        public virtual int GetDuration() => 0;

        #endregion
    }
}
