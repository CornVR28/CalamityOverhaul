using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.ADV.DialogueBoxs.Styles;
using System;
using Terraria;
using Terraria.Graphics.CameraModifiers;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.SupCal.End.EternalBlazingNows
{
    /// <summary>
    /// 永恒燃烧的如今，比目鱼缺席的差分版本
    /// 当玩家未持有比目鱼时使用，避免比目鱼以陌生人身份突然登场
    /// </summary>
    internal class EternalBlazingNowNoHelen : ADVScenarioBase, ILocalizedModType
    {
        //角色名称，沿用主场景的称呼以保持本地化对照
        public static LocalizedText Rolename1 { get; private set; }
        public static LocalizedText Rolename2 { get; private set; }

        //对话台词
        public static LocalizedText Line1 { get; private set; }
        public static LocalizedText Line2 { get; private set; }
        public static LocalizedText Line3 { get; private set; }
        public static LocalizedText Line4 { get; private set; }
        public static LocalizedText Line5 { get; private set; }
        public static LocalizedText Line6 { get; private set; }
        public static LocalizedText Line7 { get; private set; }
        public static LocalizedText Line8 { get; private set; }
        public static LocalizedText Line9 { get; private set; }
        public static LocalizedText Line10 { get; private set; }

        protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => BrimstoneDialogueBox.Instance;

        //至尊灾厄表情常量，与主场景保持一致
        private const string supCalDespise = " ";

        public override void SetStaticDefaults() {
            Rolename1 = this.GetLocalization(nameof(Rolename1), () => "???硫火女巫???");
            Rolename2 = this.GetLocalization(nameof(Rolename2), () => "硫火女巫");

            Line1 = this.GetLocalization(nameof(Line1), () => "......能走到这里的人，已经许久没有出现了");
            Line2 = this.GetLocalization(nameof(Line2), () => "我不会死......不过，也差不多了");
            Line3 = this.GetLocalization(nameof(Line3), () => "你做得很好......或许，你真的是他口中那个值得等待的“时代唯一”");
            Line4 = this.GetLocalization(nameof(Line4), () => "所以，我有最后一件事，想拜托你");
            Line5 = this.GetLocalization(nameof(Line5), () => "只要这世间的过去与现在，还存有一缕硫磺火，“我”就不会消亡");
            Line6 = this.GetLocalization(nameof(Line6), () => "可我的意识，却会在这无尽的火海中被逐渐磨灭");
            Line7 = this.GetLocalization(nameof(Line7), () => "如果没有遇到你，我最多还能撑三十年");
            Line8 = this.GetLocalization(nameof(Line8), () => "这是唯一的办法");
            Line9 = this.GetLocalization(nameof(Line9), () => "当我的意识彻底消散，整个世界都会被焚尽");
            Line10 = this.GetLocalization(nameof(Line10), () => "况且，如果你想终结这个时代，凡人的躯壳太过脆弱");
        }

        protected override void Build() {
            //仅注册至尊灾厄立绘，本场景没有比目鱼出场
            DialogueBoxBase.RegisterPortrait(Rolename1.Value, ADVAsset.SupCalsADV[4]);
            DialogueBoxBase.SetPortraitStyle(Rolename1.Value, silhouette: true);

            DialogueBoxBase.RegisterPortrait(Rolename1.Value + supCalDespise, ADVAsset.SupCalsADV[3]);
            DialogueBoxBase.SetPortraitStyle(Rolename1.Value + supCalDespise, silhouette: true);

            DialogueBoxBase.RegisterPortrait(Rolename2.Value, ADVAsset.SupCalsADV[0]);
            DialogueBoxBase.SetPortraitStyle(Rolename2.Value, silhouette: true);

            Add(Rolename1.Value, Line1.Value);
            //Add(Rolename1.Value, Line2.Value);
            Add(Rolename1.Value, Line3.Value);
            Add(Rolename1.Value, Line4.Value);
            Add(Rolename1.Value + supCalDespise, Line5.Value);
            Add(Rolename1.Value, Line6.Value);
            Add(Rolename1.Value, Line7.Value);
            Add(Rolename1.Value + supCalDespise, Line8.Value);
            Add(Rolename1.Value + supCalDespise, Line9.Value);
            Add(Rolename1.Value + supCalDespise, Line10.Value);
        }

        protected override void OnScenarioStart() {
            //开始生成粒子效果
            EbnEffect.IsActive = true;

            MusicToast.ShowMusic(
                title: "罪之楔",
                artist: "腐姬",
                albumCover: ADVAsset.FUJI,
                style: MusicToast.MusicStyle.RedNeon,
                displayDuration: 480//8秒
            );
        }

        protected override void OnScenarioComplete() {
            //无人阻止，独白结束直接进入女巫告别场景
            if (Main.LocalPlayer.TryGetADVSave(out var save)) {
                save.Get<SupCalADVData>().EternalBlazingNowChoice1 = true;
            }
            //不在这里关闭粒子效果，让它延续到告别场景
            WitchFarewell.Spwan = true;
        }
    }
}
