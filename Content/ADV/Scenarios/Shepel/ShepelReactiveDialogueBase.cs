using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.ADV.DialogueBoxs.Styles;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel
{
    /// <summary>
    /// 所有响应式对话的抽象基类
    /// 新增响应式事件时只需新建一个继承此类的文件，无需修改任何现有代码
    /// 发现机制依赖ADVScenarioBase.Instances的反射扫描，无需手动注册
    /// </summary>
    internal abstract class ShepelReactiveDialogueBase : SHPCDialogueScenarioBase, ILocalizedModType
    {
        public new string LocalizationCategory => "ADV.Shepel";
        public override int DialoguePriority => 50;
        protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => SHPCDialogueBox.Instance;

        /// <summary>
        /// 子类声明自己负责处理的事件类型，一个子类对应一种事件
        /// </summary>
        protected abstract ShepelReactiveEvent HandledEvent { get; }

        /// <summary>
        /// Boss专属对话重写此属性，返回目标Boss的NPC类型ID
        /// 非Boss事件保持默认值-1，路由时不做类型过滤
        /// </summary>
        protected virtual int TargetBossNpcType => -1;

        protected override bool CheckConditions(Player player, ADVSave save) {
            ShepelADVData data = save.Get<ShepelADVData>();
            if (!ShepelReactiveEvents.HasFlag(data, HandledEvent)) return false;
            if (TargetBossNpcType != -1 && data.LastDefeatedBossNpcType != TargetBossNpcType) return false;
            return true;
        }

        /// <summary>
        /// 消费当前事件标记，在Build()开头调用
        /// </summary>
        protected void ConsumeEvent(ShepelADVData data)
            => ShepelReactiveEvents.ClearFlag(data, HandledEvent);

        /// <summary>
        /// 显示立绘并设置初始表情
        /// </summary>
        protected static void ShowPortraitWithFace(ShepelFullBodyPortrait.Face face) {
            SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
            if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                portrait.SkipFadeIn();
                portrait.currentFace = face;
            }
        }

        /// <summary>
        /// 更改已显示立绘的表情，用于对话中途切换
        /// </summary>
        protected static void SetPortraitFace(ShepelFullBodyPortrait.Face face) {
            if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait)
                portrait.currentFace = face;
        }
    }
}
