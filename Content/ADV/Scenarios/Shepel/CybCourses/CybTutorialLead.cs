using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.EntrustManager;
using CalamityOverhaul.Content.ADV.Scenarios;
using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.UI;
using InnoVault;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.CybCourses
{
    //超梦教程引导层，负责自动触发开场对话、管理教学步骤状态，并绘制教学卡片和高亮标注
    //复用EntrustGuideCard着色器（variant=1青色版），不修改任何被教学的目标UI代码
    internal class CybTutorialLead : ModSystem, ILocalizedModType
    {
        private enum Phase { Inactive, Running, FadeOut, Done }

        public string LocalizationCategory => "ADV.Shepel";

        //各步骤的固定元数据（目标键和推进方式）
        private static readonly (string TargetKey, bool IsAuto)[] StepMeta =
        {
            (null,              false),
            ("SHPC.Core",       false),
            ("SHPC.Sector.0",   false),
            ("SHPC.Sector.1",   false),
            ("SHPC.Sector.2",   false),
            ("SHPC.Sector.3",   false),
            (null,              true),
        };

        //各步骤的本地化标题与正文
        private static LocalizedText[] _stepTitles;
        private static LocalizedText[] _stepBodies;
        private static LocalizedText _textAwaitingEquip;
        private static LocalizedText _textCalibrating;
        private static LocalizedText _textNextBtn;

        public override void SetStaticDefaults() {
            _stepTitles = new[] {
                this.GetLocalization("Step0_Title", () => "连接 SHPC"),
                this.GetLocalization("Step1_Title", () => "核心节点"),
                this.GetLocalization("Step2_Title", () => "CYBER DOMAIN"),
                this.GetLocalization("Step3_Title", () => "CYBERWARE"),
                this.GetLocalization("Step4_Title", () => "MODIFY"),
                this.GetLocalization("Step5_Title", () => "TALK"),
                this.GetLocalization("Step6_Title", () => "ENGRAM CALIBRATED"),
            };
            _stepBodies = new[] {
                this.GetLocalization("Step0_Body", () => "将SHPC装备至武器栏并持握，HUD核心节点即会出现在屏幕左下角。"),
                this.GetLocalization("Step1_Body", () => "点击左下角的核心节点可展开或收起操作面板。"),
                this.GetLocalization("Step2_Body", () => "网域 — 部署并管理多层赛博空间层叠结构。"),
                this.GetLocalization("Step3_Body", () => "赛博改装 — 查看并管理你的机体增强模块。"),
                this.GetLocalization("Step4_Body", () => "改造 — 为SHPC安装或拆卸改造零件。"),
                this.GetLocalization("Step5_Body", () => "神经链路 — 与SHPC建立直连通讯，开启对话。"),
                this.GetLocalization("Step6_Body", () => "所有接口已解析完毕。\n神经链路稳定，SHPC已就绪。"),
            };
            _textAwaitingEquip = this.GetLocalization("AwaitingEquip", () => "AWAITING EQUIP...");
            _textCalibrating   = this.GetLocalization("Calibrating",   () => "CALIBRATING...");
            _textNextBtn       = this.GetLocalization("NextBtn",       () => "NEXT  >");
        }

        private const int CardW = 310;
        private const int CardH = 118;
        private const int EdgePad = 8;

        private static Phase _phase = Phase.Inactive;
        private static int _currentStep = 0;
        private static float _cardAnim = 0f;
        private static float _shaderTimer = 0f;
        private static float _highlightPulse = 0f;
        private static float _stepTimer = 0f;
        private static bool _introAttempted = false;
        private static bool _prevMouseLeft = false;
        private static Rectangle _nextBtnRect = Rectangle.Empty;
        private static Rectangle _cardRect = Rectangle.Empty;

        public override void OnWorldUnload() {
            _phase = Phase.Inactive;
            _currentStep = 0;
            _cardAnim = 0f;
            _stepTimer = 0f;
            _introAttempted = false;
            _prevMouseLeft = false;
            _nextBtnRect = Rectangle.Empty;
            _cardRect = Rectangle.Empty;
        }

        //由CybCourseIntroDialogue.OnScenarioComplete()在对话结束后调用
        public static void BeginSHPCTutorial() {
            _phase = Phase.Running;
            _currentStep = 0;
            _cardAnim = 0f;
            _stepTimer = 0f;
        }

        public override void UpdateUI(GameTime gameTime) {
            if (Main.dedServ || Main.gameMenu) return;
            if (!CybCourseWorld.Active) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _shaderTimer += dt * 0.8f;
            if (_shaderTimer > 100f) _shaderTimer -= 100f;

            AutoTriggerIntro();
            ResumeFromSave();

            bool mouseDown = Main.mouseLeft;
            bool mouseClicked = mouseDown && !_prevMouseLeft;
            _prevMouseLeft = mouseDown;

            //卡片可见时屏蔽世界点击，防止玩家在操作引导界面时误触武器
            if (_cardRect != Rectangle.Empty && _cardRect.Contains(Main.mouseX, Main.mouseY)) {
                Main.LocalPlayer.mouseInterface = true;
            }

            switch (_phase) {
                case Phase.Running:
                    _highlightPulse += dt;
                    _cardAnim = MathHelper.Lerp(_cardAnim, 1f, 0.16f);

                    _stepTimer += dt;
                    bool isAuto = StepMeta[_currentStep].IsAuto;
                    if (isAuto) {
                        if (CheckAutoAdvance())
                            AdvanceStep();
                    } else {
                        //step 0：玩家自行持握SHPC时自动推进，无需点击NEXT
                        if (_currentStep == 0 && SHPCUI.Instance?.Active == true) {
                            AdvanceStep();
                        } else if (mouseClicked && _nextBtnRect != Rectangle.Empty
                            && _nextBtnRect.Contains(Main.mouseX, Main.mouseY)) {
                            Main.mouseLeft = false;
                            //玩家直接点NEXT跳过装备步骤，强制将SHPC移至持握槽
                            if (_currentStep == 0 && SHPCUI.Instance?.Active != true)
                                ForceEquipSHPC();
                            AdvanceStep();
                        }
                    }
                    break;

                case Phase.FadeOut:
                    _cardAnim = MathHelper.Lerp(_cardAnim, 0f, 0.18f);
                    if (_cardAnim < 0.02f) {
                        _cardAnim = 0f;
                        _phase = Phase.Done;
                    }
                    break;
            }
        }

        //在开场对话未播放时自动触发
        private static void AutoTriggerIntro() {
            if (_introAttempted) return;
            if (!Main.LocalPlayer.TryGetADVSave(out var save)) return;
            var data = save.Get<CybCourseTutorialData>();
            if (data.IntroPlayed) return;
            if (ScenarioManager.IsActive()) return;
            ScenarioManager.Start<CybCourseIntroDialogue>();
            _introAttempted = true;
        }

        //重新进入世界时从存档恢复教程进度
        private static void ResumeFromSave() {
            if (_phase != Phase.Inactive) return;
            if (!Main.LocalPlayer.TryGetADVSave(out var save)) return;
            var data = save.Get<CybCourseTutorialData>();
            if (!data.IntroPlayed) return;
            int step = data.SHPCTutorialStep;
            if (step < 0) {
                _phase = Phase.Done;
                return;
            }
            if (step < StepMeta.Length) {
                _phase = Phase.Running;
                _currentStep = step;
                _cardAnim = 0f;
                _stepTimer = 0f;
            }
        }

        //各自动推进步骤的完成条件（仅IsAuto=true的步骤会调用此方法）
        private static bool CheckAutoAdvance() {
            if (_currentStep == StepMeta.Length - 1)
                return _stepTimer >= 3.5f;
            return false;
        }

        //在热键栏或背包中找SHPC并装备到当前持握槽
        private static void ForceEquipSHPC() {
            Player p = Main.LocalPlayer;
            if (p == null || p.dead) return;
            for (int i = 0; i < 10; i++) {
                if (p.inventory[i].type == CWRID.Item_SHPC) {
                    p.selectedItem = i;
                    return;
                }
            }
            for (int i = 10; i < 58; i++) {
                if (p.inventory[i].type == CWRID.Item_SHPC) {
                    var tmp = p.inventory[p.selectedItem];
                    p.inventory[p.selectedItem] = p.inventory[i];
                    p.inventory[i] = tmp;
                    return;
                }
            }
        }

        private static void AdvanceStep() {
            _currentStep++;
            _stepTimer = 0f;
            _cardAnim = 0f;
            if (_currentStep >= StepMeta.Length) {
                _phase = Phase.FadeOut;
                if (Main.LocalPlayer.TryGetADVSave(out var save))
                    save.Get<CybCourseTutorialData>().SHPCTutorialStep = -1;
                return;
            }
            if (Main.LocalPlayer.TryGetADVSave(out var sv))
                sv.Get<CybCourseTutorialData>().SHPCTutorialStep = _currentStep;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
            if (_phase != Phase.Running && _phase != Phase.FadeOut) return;
            if (_cardAnim < 0.02f) return;

            int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (idx == -1) return;

            layers.Insert(idx, new LegacyGameInterfaceLayer("CWRMod: CybCourse Tutorial Lead",
                delegate {
                    DrawOverlay(Main.spriteBatch);
                    return true;
                }, InterfaceScaleType.UI));
        }

        private static void DrawOverlay(SpriteBatch sb) {
            Texture2D px = VaultAsset.placeholder2?.Value;
            if (px == null) return;

            float alpha = MathHelper.Clamp(_cardAnim, 0f, 1f);
            string targetKey = StepMeta[_currentStep].TargetKey;

            //卡片位置固定在SHPC HUD右侧，从下往上偏移
            Vector2 corePos = SHPCHUDTargets.CorePos;
            int cx = (int)(corePos.X + SHPCTheme.ButtonOuterR + 18f);
            int cy = (int)(corePos.Y) - CardH + 8;
            float slideX = (1f - alpha) * 30f;
            var card = new Rectangle(cx + (int)slideX, cy, CardW, CardH);

            _cardRect = card;
            DrawCardBg(sb, card, alpha);
            DrawCardContent(sb, px, card, alpha);
            DrawHighlightForStep(sb, px, targetKey, alpha);
        }

        private static void DrawCardBg(SpriteBatch sb, Rectangle card, float alpha) {
            Effect effect = EffectLoader.EntrustGuideCard?.Value;
            if (effect != null) {
                Rectangle ext = card;
                ext.Inflate(EdgePad, EdgePad);
                effect.Parameters["uTime"]?.SetValue(_shaderTimer);
                effect.Parameters["uAlpha"]?.SetValue(alpha * 0.96f);
                effect.Parameters["uResolution"]?.SetValue(new Vector2(ext.Width, ext.Height));
                effect.Parameters["uEdgePad"]?.SetValue((float)EdgePad);
                effect.Parameters["uVariant"]?.SetValue(1f);
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.AnisotropicClamp, DepthStencilState.None,
                    RasterizerState.CullNone, effect, Main.UIScaleMatrix);
                sb.Draw(VaultAsset.placeholder2.Value, ext, Color.White);
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.AnisotropicClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.UIScaleMatrix);
            } else {
                sb.Draw(VaultAsset.placeholder2.Value, card, new Color(0, 8, 18, (int)(200 * alpha)));
                BaseManagerStyle.StrokeRect(sb, card, 1, new Color(50, 160, 200, (int)(120 * alpha)));
            }
        }

        private static void DrawCardContent(SpriteBatch sb, Texture2D px, Rectangle card, float alpha) {
            var font = FontAssets.MouseText.Value;
            float titleSc = 0.84f;
            float bodySc = 0.70f;
            float subSc = 0.58f;
            float lineT = font.MeasureString("A").Y * titleSc + 2f;
            float lineB = font.MeasureString("A").Y * bodySc + 1f;

            string title  = _stepTitles[_currentStep].Value;
            string body   = _stepBodies[_currentStep].Value;
            bool isAuto   = StepMeta[_currentStep].IsAuto;
            float px2 = card.X + 14f;
            float py = card.Y + 12f;

            //步骤计数
            string counter = $"{_currentStep + 1:D2} / {StepMeta.Length:D2}";
            float counterW = font.MeasureString(counter).X * subSc;
            Utils.DrawBorderString(sb, counter,
                new Vector2(card.Right - 14f - counterW, py),
                new Color(70, 155, 175, (int)(150 * alpha)), subSc);

            //标题（青色）
            Utils.DrawBorderString(sb, title, new Vector2(px2, py),
                new Color(80, 220, 245, (int)(255 * alpha)), titleSc);
            py += lineT + 2f;

            //分割线
            BaseManagerStyle.FillRect(sb,
                new Rectangle((int)px2, (int)py, CardW - 28, 1),
                new Color(45, 130, 155, (int)(90 * alpha)));
            py += 6f;

            //正文（支持换行）
            int bodyWrapW = (int)((CardW - 28) / bodySc);
            foreach (string line in body.Split('\n')) {
                string[] bodyWrapped = Utils.WordwrapString(line, font, bodyWrapW, 99, out _);
                foreach (string wl in bodyWrapped) {
                    if (string.IsNullOrEmpty(wl)) continue;
                    Utils.DrawBorderString(sb, wl.TrimEnd('-', ' '), new Vector2(px2, py),
                        new Color(175, 215, 225, (int)(215 * alpha)), bodySc);
                    py += lineB;
                }
            }

            //底部按钮区
            if (!isAuto) {
                DrawNextButton(sb, card, alpha);
            } else {
                //自动推进步骤显示等待提示
                float blink = 0.72f + 0.28f * MathF.Sin(_shaderTimer * 22f);
                string standby = _currentStep == 0 ? _textAwaitingEquip.Value : _textCalibrating.Value;
                float sbW = font.MeasureString(standby).X * subSc;
                Utils.DrawBorderString(sb, standby,
                    new Vector2(card.Right - 14f - sbW, card.Bottom - 16f),
                    new Color(60, 190, 200, (int)(200 * alpha * blink)), subSc);
            }
        }

        private static void DrawNextButton(SpriteBatch sb, Rectangle card, float alpha) {
            const int btnW = 72, btnH = 20, margin = 10;
            var btn = new Rectangle(card.Right - btnW - margin, card.Bottom - btnH - margin, btnW, btnH);
            _nextBtnRect = btn;

            bool hovered = btn.Contains(Main.mouseX, Main.mouseY);
            Color bgColor = hovered
                ? new Color(40, 155, 180, (int)(210 * alpha))
                : new Color(18, 72, 92, (int)(150 * alpha));
            Color borderColor = hovered
                ? new Color(100, 220, 245, (int)(200 * alpha))
                : new Color(50, 150, 180, (int)(120 * alpha));
            Color textColor = hovered
                ? new Color(200, 250, 255, (int)(255 * alpha))
                : new Color(110, 205, 225, (int)(195 * alpha));

            BaseManagerStyle.FillRect(sb, btn, bgColor);
            BaseManagerStyle.StrokeRect(sb, btn, 1, borderColor);
            BaseManagerStyle.DrawCenteredText(sb, _textNextBtn.Value, btn.Center.ToVector2(), textColor, 0.60f);
        }

        private static void DrawHighlightForStep(SpriteBatch sb, Texture2D px, string targetKey, float alpha) {
            if (string.IsNullOrEmpty(targetKey)) return;
            if (!CybTutorialRegistry.TryGet(targetKey, out var target)) return;

            float pulse = 0.6f + 0.4f * MathF.Sin(_highlightPulse * 3.2f);
            Color hColor = new Color(
                (int)(70 * pulse), (int)(215 * pulse), (int)(245 * pulse),
                (int)(175 * pulse * alpha));
            Color bracketColor = new Color(80, 220, 245, (int)(200 * alpha));

            Vector2 corePos = SHPCHUDTargets.CorePos;

            if (targetKey == "SHPC.Core") {
                SHPCRenderer.DrawArc(sb, px, corePos,
                    SHPCTheme.CoreRingR + 4f, SHPCTheme.CoreRingR + 14f,
                    0f, MathHelper.TwoPi, hColor);
            } else if (targetKey.StartsWith("SHPC.Sector.")
                && int.TryParse(targetKey[12..], out int idx)) {
                SHPCHUDTargets.GetSectorAngles(idx, out float a0, out float a1);
                float expand = 3f + 4f * MathF.Sin(_highlightPulse * 3.2f);
                SHPCRenderer.DrawArc(sb, px, corePos,
                    SHPCTheme.ButtonInnerR - 5f,
                    SHPCTheme.ButtonOuterR + 8f + expand,
                    a0, a1, hColor);
            }

            //目标区域L角括号
            Rectangle rect = target.GetScreenRect();
            DrawLBrackets(sb, px, rect, bracketColor);
        }

        //绘制四角L形括号标注框
        private static void DrawLBrackets(SpriteBatch sb, Texture2D px, Rectangle r, Color c) {
            const int len = 12;
            const int thick = 2;
            sb.Draw(px, new Rectangle(r.Left,          r.Top,          len,   thick), c);
            sb.Draw(px, new Rectangle(r.Left,          r.Top,          thick, len),   c);
            sb.Draw(px, new Rectangle(r.Right - len,   r.Top,          len,   thick), c);
            sb.Draw(px, new Rectangle(r.Right - thick, r.Top,          thick, len),   c);
            sb.Draw(px, new Rectangle(r.Left,          r.Bottom - thick, len,   thick), c);
            sb.Draw(px, new Rectangle(r.Left,          r.Bottom - len,   thick, len),   c);
            sb.Draw(px, new Rectangle(r.Right - len,   r.Bottom - thick, len,   thick), c);
            sb.Draw(px, new Rectangle(r.Right - thick, r.Bottom - len,   thick, len),   c);
        }
    }
}
