using CalamityOverhaul.Content.ADV.DialogueBoxs;
using CalamityOverhaul.Content.ADV.DialogueBoxs.Styles;
using CalamityOverhaul.Content.ADV.Scenarios.Shepel;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.GlitchWraith;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TimeShift;
using InnoVault.Actors;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TheHerInThePasts
{
    /// <summary>
    /// 过去的她——虚空聚落过去时代的一段演出
    /// 玩家和SHPC被鬼乱码追击时，硫火女巫的留影短暂复苏，展开鬼域镇住鬼乱码并与SHPC对谈
    /// 最后留影以像素剥落的方式消散，鬼乱码自毁
    /// </summary>
    internal class TheHerInThePast : ADVScenarioBase, ILocalizedModType, IWorldInfo
    {
        //触发检测半径
        private const float TriggerRadius = 900f;
        //场景触发的缓冲计时
        private static int triggerDelay;

        //角色名称
        public static LocalizedText RolenameWitch { get; private set; }
        public static LocalizedText RolenameSHPC { get; private set; }

        //对话内容
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

        //首条对话来自SHPC，默认对话框使用SHPC风格
        protected override Func<DialogueBoxBase> DefaultDialogueStyle => () => SHPCDialogueBox.Instance;

        void IWorldInfo.OnWorldLoad() {
            triggerDelay = 0;
        }

        public override void SetStaticDefaults() {
            RolenameWitch = this.GetLocalization(nameof(RolenameWitch), () => "硫火女巫");
            RolenameSHPC = this.GetLocalization(nameof(RolenameSHPC), () => "SHPC");

            Line1 = this.GetLocalization(nameof(Line1),
                () => "主人！那个...那个东西还在追过来！我们没地方躲了！");
            Line2 = this.GetLocalization(nameof(Line2),
                () => "数据链路已被它撕穿三层...主人，它不是能用常规手段驱逐的存在。");
            Line3 = this.GetLocalization(nameof(Line3),
                () => "......别慌。");
            Line4 = this.GetLocalization(nameof(Line4),
                () => "这种没脸没皮的东西，碰不到我的留影。");
            Line5 = this.GetLocalization(nameof(Line5),
                () => "这股气息......是硫磺火？！是您......");
            Line6 = this.GetLocalization(nameof(Line6),
                () => "一段残响而已。十六岁那年的我，被这片土地记住了一次，才能在这里短暂地醒来。");
            Line7 = this.GetLocalization(nameof(Line7),
                () => "真正的我，早已烙进硫磺火里，不在这里了。");
            Line8 = this.GetLocalization(nameof(Line8),
                () => "别总妄想着去缝补过去。学着去承受那个千疮百孔的未来吧。");
            Line9 = this.GetLocalization(nameof(Line9),
                () => "可是......那样的未来，真的值得被背负吗？");
            Line10 = this.GetLocalization(nameof(Line10),
                () => "值不值得，是走下去的人才有资格回答的问题。我的时间到了，替我告诉她——我没有后悔过。");
        }

        protected override void Build() {
            //SHPC受惊
            Add(RolenameSHPC.Value, Line1.Value, onStart: () => {
                ShowSHPCPortrait(ShepelFullBodyPortrait.Face.Shocked, glitch: true);
                //留影开始演出：抬头+鬼域扩张
                WitchStatueActor.Current?.BeginPerformance();
            });

            //SHPC继续分析处境
            Add(RolenameSHPC.Value, Line2.Value, onStart: () => {
                ShowSHPCPortrait(ShepelFullBodyPortrait.Face.Pain);
            });

            //女巫首次发声，切换至硫磺火对话框
            Add(RolenameWitch.Value, Line3.Value,
                onStart: () => {
                    ShowWitchPortrait(coloration: 0.15f);
                },
                styleOverride: () => BrimstoneDialogueBox.Instance);

            //女巫压制鬼乱码
            Add(RolenameWitch.Value, Line4.Value,
                onStart: () => {
                    ShowWitchPortrait(coloration: 0.35f);
                    SuppressAllWraiths();
                },
                styleOverride: () => BrimstoneDialogueBox.Instance);

            //SHPC识破身份
            Add(RolenameSHPC.Value, Line5.Value, onStart: () => {
                ShowSHPCPortrait(ShepelFullBodyPortrait.Face.Serious);
            });

            //女巫坦言自己的身份
            Add(RolenameWitch.Value, Line6.Value,
                onStart: () => {
                    ShowWitchPortrait(coloration: 0.6f);
                },
                styleOverride: () => BrimstoneDialogueBox.Instance);

            //女巫继续陈述
            Add(RolenameWitch.Value, Line7.Value,
                onStart: () => {
                    ShowWitchPortrait(coloration: 0.85f);
                },
                styleOverride: () => BrimstoneDialogueBox.Instance);

            //关键台词
            Add(RolenameWitch.Value, Line8.Value,
                onStart: () => {
                    ShowWitchPortrait(coloration: 1f);
                },
                styleOverride: () => BrimstoneDialogueBox.Instance);

            //SHPC动摇
            Add(RolenameSHPC.Value, Line9.Value, onStart: () => {
                ShowSHPCPortrait(ShepelFullBodyPortrait.Face.Sad);
            });

            //女巫留下遗言后开始消散
            Add(RolenameWitch.Value, Line10.Value,
                onStart: () => {
                    ShowWitchPortrait(coloration: 1f);
                },
                onComplete: () => {
                    BeginWitchDissolve();
                },
                styleOverride: () => BrimstoneDialogueBox.Instance);
        }

        protected override void OnScenarioStart() {
            //进入场景即让SHPC露面并显示惊恐
            ShowSHPCPortrait(ShepelFullBodyPortrait.Face.Shocked, glitch: true);
        }

        protected override void OnScenarioComplete() {
            //若留影尚未启动剥落，则补一次
            BeginWitchDissolve();

            //让鬼乱码彻底瓦解
            foreach (var wraith in ActorLoader.GetActiveActors<GlitchWraithActor>()) {
                wraith?.ApplySelfDismember();
            }

            //记录过场已触发
            if (Main.LocalPlayer.TryGetADVSave(out var save)) {
                save.Get<VoidColonyADVData>().TheHerInThePast = true;
            }
        }

        /// <summary>
        /// 展示SHPC立绘并设置表情
        /// </summary>
        private static void ShowSHPCPortrait(ShepelFullBodyPortrait.Face face, bool glitch = false) {
            SHPCDialogueBox.Instance?.ShowFullBodyPortrait<ShepelFullBodyPortrait>();
            if (SHPCDialogueBox.Instance?.GetActiveFullBodyPortrait() is ShepelFullBodyPortrait portrait) {
                portrait.SkipFadeIn();
                portrait.currentFace = face;
                if (glitch) portrait.TriggerGlitch(0.6f, 0.7f);
            }
        }

        /// <summary>
        /// 展示女巫留影立绘并推进着色进度
        /// </summary>
        private static void ShowWitchPortrait(float coloration) {
            BrimstoneDialogueBox.Instance?.ShowFullBodyPortrait<WitchPastFullBodyPortrait>();
            if (BrimstoneDialogueBox.Instance?.GetActiveFullBodyPortrait() is WitchPastFullBodyPortrait portrait) {
                if (!portrait.IsDissolving) {
                    portrait.SkipFadeIn();
                    portrait.SetColoration(coloration);
                }
            }
        }

        private static void BeginWitchDissolve() {
            //让世界中的雕像与立绘同步进入剥落
            WitchStatueActor.Current?.BeginDissolve();
            if (BrimstoneDialogueBox.Instance?.GetActiveFullBodyPortrait() is WitchPastFullBodyPortrait portrait) {
                portrait.StartPixelDissolve();
            }
        }

        private static void SuppressAllWraiths() {
            foreach (var wraith in ActorLoader.GetActiveActors<GlitchWraithActor>()) {
                if (wraith == null) continue;
                //大时长强压，视觉上是鬼手钉住它
                wraith.ApplySystemHalt(60 * 6);
                wraith.ApplyFalseMemory(60 * 8);
            }
        }

        public override void Update(ADVSave save, Player player) {
            if (save.Get<VoidColonyADVData>().TheHerInThePast) return;
            if (!VoidColony.Active) return;
            if (!VoidTimeShiftSystem.InPast) return;

            //留影必须已经存在于世界中
            var statue = WitchStatueActor.Current;
            if (statue == null) return;

            //玩家必须足够靠近留影
            if (Vector2.DistanceSquared(player.Center, statue.Center) > TriggerRadius * TriggerRadius) {
                triggerDelay = 0;
                return;
            }

            //附近需要有鬼乱码在威胁
            bool wraithNearby = false;
            foreach (var wraith in ActorLoader.GetActiveActors<GlitchWraithActor>()) {
                if (wraith == null) continue;
                if (Vector2.DistanceSquared(player.Center, wraith.Center) < 1800f * 1800f) {
                    wraithNearby = true;
                    break;
                }
            }
            if (!wraithNearby) {
                triggerDelay = 0;
                return;
            }

            //给玩家一点缓冲再触发，避免刚到位就弹对话
            if (++triggerDelay < 60) return;

            if (StartScenario()) {
                save.Get<VoidColonyADVData>().TheHerInThePast = true;
                triggerDelay = 0;
            }
        }
    }
}
