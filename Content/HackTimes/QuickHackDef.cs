using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.HackTimes
{
    /// <summary>
    /// 骇入协议类别
    /// </summary>
    internal enum QuickHackCategory
    {
        /// <summary>致命型：直接造成伤害</summary>
        Lethal,
        /// <summary>控制型：限制目标行动</summary>
        Control,
        /// <summary>隐匿型：干扰目标感知</summary>
        Covert,
        /// <summary>传播型：扩散至附近目标</summary>
        Contagion,
        /// <summary>物块操控型：对物块施加效果</summary>
        TileManip,
        /// <summary>灵异型：对非实体灵异目标起效的特殊协议</summary>
        Paranormal,
    }

    /// <summary>
    /// 快速骇入协议基类
    /// <br/>所有骇入协议必须继承此类，通过 VaultType 自动注册
    /// <br/>子类通过<see cref="SetDefaults"/>设置属性，通过<see cref="OnApply"/>实现效果
    /// <br/>所有目标种类（NPC、物块、灵异 Actor、炮台、信号塔等）统一通过<see cref="IHackTarget"/>暴露给协议
    /// </summary>
    internal abstract class QuickHackDef : VaultType<QuickHackDef>, ILocalizedModType
    {
        #region 静态注册表

        public static readonly Dictionary<Type, int> TypeToID = [];
        public static readonly Dictionary<int, QuickHackDef> IDToInstance = [];

        public static int Count => Instances.Count;

        public static T Get<T>() where T : QuickHackDef {
            if (TypeToID.TryGetValue(typeof(T), out int id)
                && IDToInstance.TryGetValue(id, out var inst)
                && inst is T t) {
                return t;
            }
            return null;
        }

        public static QuickHackDef GetByIndex(int index) {
            if (index >= 0 && index < Instances.Count)
                return Instances[index];
            return null;
        }

        #endregion

        #region 本地化

        public string LocalizationCategory => "QuickHack";
        /// <summary>协议显示名称（本地化）</summary>
        public LocalizedText DisplayName => this.GetLocalization(nameof(DisplayName), PrettyPrintName);
        /// <summary>协议效果描述（本地化）</summary>
        public LocalizedText Description => this.GetLocalization(nameof(Description), () => "");

        #endregion

        #region 实例属性

        /// <summary>注册序号（0 开始，等同于在 <see cref="VaultType{T}.Instances"/> 列表中的索引）</summary>
        public int SlotIndex { get; private set; } = -1;
        /// <summary>上传所需时间（帧数，60 帧为 1 秒）</summary>
        public int UploadTime { get; set; } = 60;
        /// <summary>执行该协议所需消耗的 RAM 单元数</summary>
        public int RamCost { get; set; } = 2;
        /// <summary>协议类别</summary>
        public QuickHackCategory Category { get; set; } = QuickHackCategory.Lethal;
        /// <summary>
        /// 该协议支持的目标类型，默认仅 NPC
        /// <br/>子类可在 SetDefaults 中设置为 Tile、Wraith、Turret、SignalTower 等，可用按位或合并多个
        /// </summary>
        public HackTargetKind SupportedTargets { get; set; } = HackTargetKind.Npc;

        #endregion

        #region VaultType 生命周期

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

        #region 子类重写接口（统一目标抽象）

        /// <summary>
        /// 设置协议默认属性（UploadTime、RamCost、Category、SupportedTargets 等）
        /// </summary>
        public virtual void SetDefaults() { }

        /// <summary>
        /// 当协议上传完成时对目标施加效果
        /// <br/>所有目标种类（NPC、物块、灵异 Actor、炮台、信号塔等）通过<see cref="IHackTarget"/>暴露
        /// <br/>子类按需 pattern-match：<c>if (target is NpcScannable npc) ...</c>
        /// </summary>
        /// <param name="target">骇入的目标</param>
        /// <param name="caster">发起骇入的玩家</param>
        /// <returns>返回 true 表示效果成功施加</returns>
        public virtual bool OnApply(IHackTarget target, Player caster) => false;

        /// <summary>
        /// 协议效果的持续帧逻辑（用于持续性效果如灼烧、致盲等）
        /// <br/>仅在持续时间内被效果追踪器调用，不需要手动检查时长
        /// </summary>
        /// <param name="target">受影响的目标</param>
        /// <param name="elapsed">效果已持续的帧数</param>
        /// <returns>返回 false 表示效果提前结束</returns>
        public virtual bool OnTick(IHackTarget target, int elapsed) => true;

        /// <summary>
        /// 协议效果被移除或到期时调用（清理工作）
        /// </summary>
        /// <param name="target">受影响的目标</param>
        public virtual void OnRemove(IHackTarget target) { }

        /// <summary>
        /// 判断该协议是否可对指定目标使用
        /// <br/>默认要求目标存在且有效，子类可按目标类型、状态等做更细的限制
        /// </summary>
        public virtual bool CanApplyTo(IHackTarget target) => target != null && target.IsValid;

        /// <summary>
        /// 获取该协议的效果持续时间（帧），0 表示即时效果无持续
        /// </summary>
        public virtual int GetDuration() => 0;

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取与指定目标种类匹配的所有协议索引
        /// </summary>
        public static void GetFilteredIndices(HackTargetKind kind, List<int> result) {
            result.Clear();
            for (int i = 0; i < Instances.Count; i++) {
                if ((Instances[i].SupportedTargets & kind) != 0)
                    result.Add(i);
            }
        }

        #endregion
    }
}
