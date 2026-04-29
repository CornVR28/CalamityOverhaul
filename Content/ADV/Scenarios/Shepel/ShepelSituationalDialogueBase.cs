using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.ADV.DialogueBoxs.Styles;
using System;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel
{
    //首次进入特定地点或情境时触发的一次性对话基类，触发后对应标记永久设置
    //子类无需重复声明LocalizationCategory，也无需手动注册
    internal abstract class ShepelSituationalDialogueBase : SHPCDialogueScenarioBase, ILocalizedModType
    {
        public new string LocalizationCategory => "ADV.Shepel";
        public override int DialoguePriority => 45;
        protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => SHPCDialogueBox.Instance;

        protected static void ShowPortraitWithFace(ShepelFullBodyPortrait.Face face) {
            SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
            if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                portrait.SkipFadeIn();
                portrait.currentFace = face;
            }
        }

        protected static void SetPortraitFace(ShepelFullBodyPortrait.Face face) {
            if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait)
                portrait.currentFace = face;
        }
    }
}
