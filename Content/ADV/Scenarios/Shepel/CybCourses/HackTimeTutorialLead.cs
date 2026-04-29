using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.EntrustManager;
using CalamityOverhaul.Content.ADV.Scenarios;
using CalamityOverhaul.Content.HackTimes;
using InnoVault;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.CybCourses
{
    //骇客时间教学引导层
    //管理5步骤教学：激活骇客模式→锁定测试目标→协议面板介绍→上传协议→完成
    //包含SantaNK1测试目标的生成与位置钉固
    internal class HackTimeTutorialLead : ModSystem, ILocalizedModType
    {
        private enum Phase { Inactive, Running, FadeOut, Done }

        public string LocalizationCategory => "ADV.Shepel";

        //步骤元数据：是否自动推进
        private static readonly bool[] StepIsAuto = { false, true, false, true, true, false, true, true };

        private static LocalizedText[] _stepTitles;
        private static LocalizedText[] _stepBodies;
        private static LocalizedText _textWaiting;
        private static LocalizedText _textCalibrating;
        private static LocalizedText _textNextBtn;

        public override void SetStaticDefaults() {
            _stepTitles = new[] {
                this.GetLocalization("HT_Step0_Title", () => "激活骇客模式"),
                this.GetLocalization("HT_Step1_Title", () => "锁定目标"),
                this.GetLocalization("HT_Step2_Title", () => "骇入协议面板"),
                this.GetLocalization("HT_Step3_Title", () => "上传协议"),
                this.GetLocalization("HT_Step4_Title", () => "NPC骇入完成"),
                this.GetLocalization("HT_Step5_Title", () => "物块扫描"),
                this.GetLocalization("HT_Step6_Title", () => "锁定物块目标"),
                this.GetLocalization("HT_Step7_Title", () => "TILE SCAN COMPLETE"),
            };
            _stepBodies = new[] {
                this.GetLocalization("HT_Step0_Body", () => "按下 [N] 键进入骇客时间模式。\n时间将冻结，赛博滤镜叠加于画面。"),
                this.GetLocalization("HT_Step1_Body", () => "将光标悬停到高亮的圣诞坦克上，\n点击左键将其锁定为骇入目标。"),
                this.GetLocalization("HT_Step2_Body", () => "右侧面板展示目标的可用骇入协议。\n不同协议消耗不同RAM并产生不同效果。"),
                this.GetLocalization("HT_Step3_Body", () => "点击协议将其加入左侧上传队列，\n队列将依次执行骇入操作。"),
                this.GetLocalization("HT_Step4_Body", () => "NPC骇入序列完成。下一阶段：物块扫描接口训练。"),
                this.GetLocalization("HT_Step5_Body", () => "前方有一台热能发电机MK2。\n重新激活骇客时间，将光标移至发电机并点击选中。"),
                this.GetLocalization("HT_Step6_Body", () => "将光标悬停在发电机上，\n点击左键将其锁定为扫描目标。"),
                this.GetLocalization("HT_Step7_Body", () => "物块扫描接口已解析。右侧面板展示当前物块的可用骇入协议。"),
            };
            _textWaiting    = this.GetLocalization("HT_Waiting",    () => "AWAITING INPUT...");
            _textCalibrating = this.GetLocalization("HT_Calibrating", () => "DISCONNECTING...");
            _textNextBtn    = this.GetLocalization("HT_NextBtn",    () => "NEXT  >");
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
        private static bool _hackIntroAttempted = false;
        private static bool _prevMouseLeft = false;
        private static Rectangle _nextBtnRect = Rectangle.Empty;
        private static Rectangle _cardRect = Rectangle.Empty;

        //被固定在原地的SantaNK1的NPC索引和世界坐标
        private static int _npcIndex = -1;
        private static Vector2 _npcSpawnPos;

        public override void OnWorldUnload() {
            _phase = Phase.Inactive;
            _currentStep = 0;
            _cardAnim = 0f;
            _shaderTimer = 0f;
            _highlightPulse = 0f;
            _stepTimer = 0f;
            _hackIntroAttempted = false;
            _prevMouseLeft = false;
            _nextBtnRect = Rectangle.Empty;
            _cardRect = Rectangle.Empty;
            _npcIndex = -1;
        }

        //由CybCourseHackIntroDialogue.OnScenarioComplete在对话结束后调用
        public static void BeginHackTimeTutorial() {
            _phase = Phase.Running;
            _currentStep = 0;
            _cardAnim = 0f;
            _stepTimer = 0f;
            SpawnOrFindTank();
        }

        //生成或复用已存在的SantaNK1，并记录其固定位置
        private static void SpawnOrFindTank() {
            if (Main.dedServ) return;
            for (int i = 0; i < Main.maxNPCs; i++) {
                if (Main.npc[i].active && Main.npc[i].type == NPCID.SantaNK1) {
                    _npcIndex = i;
                    _npcSpawnPos = Main.npc[i].position;
                    return;
                }
            }
            //在走廊前段生成，Y取通道中间位置确保不落在实体块内，生成后再修正至地板上方
            int spawnX = (int)Main.LocalPlayer.Center.X + 350;
            int spawnY = (CybCourseGen.FloorY - 8) * 16;
            int idx = NPC.NewNPC(new EntitySource_WorldEvent(), spawnX, spawnY, NPCID.SantaNK1);
            if (idx < Main.maxNPCs) {
                float correctY = CybCourseGen.SurfaceY * 16f - Main.npc[idx].height;
                _npcIndex = idx;
                _npcSpawnPos = new Vector2(Main.npc[idx].position.X, correctY);
                Main.npc[idx].position = _npcSpawnPos;
                Main.npc[idx].dontTakeDamage = true;
            }
        }

        //每帧将测试目标钉固在原地，防止AI移动或重力下落
        public override void PostUpdateNPCs() {
            if (!CybCourseWorld.Active) return;
            if (_npcIndex < 0 || _npcIndex >= Main.maxNPCs) return;
            if (_phase == Phase.Inactive || _phase == Phase.Done) return;

            NPC npc = Main.npc[_npcIndex];
            if (!npc.active || npc.type != NPCID.SantaNK1) {
                _npcIndex = -1;
                return;
            }
            npc.velocity = Vector2.Zero;
            npc.position = _npcSpawnPos;
            npc.dontTakeDamage = true;
        }

        public override void UpdateUI(GameTime gameTime) {
            if (Main.dedServ || Main.gameMenu) return;
            if (!CybCourseWorld.Active) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _shaderTimer += dt * 0.8f;
            if (_shaderTimer > 100f) _shaderTimer -= 100f;

            AutoTriggerHackIntro();
            ResumeFromSave();

            bool mouseDown = Main.mouseLeft;
            bool mouseClicked = mouseDown && !_prevMouseLeft;
            _prevMouseLeft = mouseDown;

            if (_cardRect != Rectangle.Empty && _cardRect.Contains(Main.mouseX, Main.mouseY))
                Main.LocalPlayer.mouseInterface = true;

            switch (_phase) {
                case Phase.Running:
                    _highlightPulse += dt;
                    _cardAnim = MathHelper.Lerp(_cardAnim, 1f, 0.16f);
                    _stepTimer += dt;

                    bool isAuto = StepIsAuto[_currentStep];
                    if (isAuto) {
                        if (CheckAutoAdvance()) AdvanceStep();
                    } else {
                        //step 0特殊处理：玩家自行激活骇客模式时自动推进
                        if (_currentStep == 0 && HackTime.Active) {
                            AdvanceStep();
                        } else if (mouseClicked && _nextBtnRect != Rectangle.Empty
                                && _nextBtnRect.Contains(Main.mouseX, Main.mouseY)) {
                            Main.mouseLeft = false;
                            //玩家点NEXT跳过，强制激活骇客模式
                            if (_currentStep == 0 && !HackTime.Active)
                                HackTime.Activate();
                            //玩家点NEXT跳过物块扫描引导，强制激活骇客时间确保step6可推进
                            if (_currentStep == 5 && !HackTime.Active)
                                HackTime.Activate();
                            AdvanceStep();
                        }
                    }
                    break;

                case Phase.FadeOut:
                    _cardAnim = MathHelper.Lerp(_cardAnim, 0f, 0.18f);
                    if (_cardAnim < 0.02f) {
                        _cardAnim = 0f;
                        _phase = Phase.Done;
                        CleanupTank();
                    }
                    break;
            }
        }

        //SHPC教学完成且骇客时间介绍未播放时自动触发对话
        private static void AutoTriggerHackIntro() {
            if (_hackIntroAttempted) return;
            if (_phase != Phase.Inactive) return;
            if (!Main.LocalPlayer.TryGetADVSave(out var save)) return;
            var data = save.Get<CybCourseTutorialData>();
            if (data.SHPCTutorialStep != -1) return;
            if (data.HackIntroPlayed) return;
            if (ScenarioManager.IsActive()) return;
            ScenarioManager.Start<CybCourseHackIntroDialogue>();
            _hackIntroAttempted = true;
        }

        //重新进入世界时从存档恢复骇客时间教程进度
        private static void ResumeFromSave() {
            if (_phase != Phase.Inactive) return;
            if (!Main.LocalPlayer.TryGetADVSave(out var save)) return;
            var data = save.Get<CybCourseTutorialData>();
            if (!data.HackIntroPlayed) return;
            int step = data.HackTutorialStep;
            if (step < 0) {
                _phase = Phase.Done;
                return;
            }
            if (step < StepIsAuto.Length) {
                _phase = Phase.Running;
                _currentStep = step;
                _cardAnim = 0f;
                _stepTimer = 0f;
                SpawnOrFindTank();
            }
        }

        //各自动推进步骤的完成判定
        private static bool CheckAutoAdvance() {
            int step = _currentStep;
            if (step == 1) {
                //玩家点击选中了SantaNK1
                if (HackTime.SelectedTargetIndex < 0) return false;
                NPC target = Main.npc[HackTime.SelectedTargetIndex];
                return target.active && target.type == NPCID.SantaNK1;
            }
            if (step == 3)
                return (HackTimeUI.Instance?.Queue?.Entries.Count ?? 0) > 0;
            if (step == 4)
                return _stepTimer >= 3.5f;
            if (step == 6)
                return HackTime.Active && HackTime.CurrentScanTarget is TileScannable;
            if (step == StepIsAuto.Length - 1)
                return _stepTimer >= 3.5f;
            return false;
        }

        //将测试目标NPC从世界中移除
        private static void CleanupTank() {
            if (_npcIndex >= 0 && _npcIndex < Main.maxNPCs) {
                NPC npc = Main.npc[_npcIndex];
                if (npc.active && npc.type == NPCID.SantaNK1)
                    npc.active = false;
                _npcIndex = -1;
            }
        }

        private static void AdvanceStep() {
            _currentStep++;
            _stepTimer = 0f;
            _cardAnim = 0f;
            //NPC教学阶段结束，清除坦克避免占用后续物块扫描的收标
            if (_currentStep == 5)
                CleanupTank();
            if (_currentStep >= StepIsAuto.Length) {
                _phase = Phase.FadeOut;
                if (Main.LocalPlayer.TryGetADVSave(out var save))
                    save.Get<CybCourseTutorialData>().HackTutorialStep = -1;
                return;
            }
            if (Main.LocalPlayer.TryGetADVSave(out var sv))
                sv.Get<CybCourseTutorialData>().HackTutorialStep = _currentStep;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
            if (_phase != Phase.Running && _phase != Phase.FadeOut) return;
            if (_cardAnim < 0.02f) return;

            int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
            if (idx == -1) return;

            layers.Insert(idx, new LegacyGameInterfaceLayer("CWRMod: CybCourse HackTime Tutorial",
                delegate {
                    DrawOverlay(Main.spriteBatch);
                    return true;
                }, InterfaceScaleType.UI));
        }

        private static void DrawOverlay(SpriteBatch sb) {
            Texture2D px = VaultAsset.placeholder2?.Value;
            if (px == null) return;

            float alpha = MathHelper.Clamp(_cardAnim, 0f, 1f);
            //卡片固定在屏幕左上角，从上方滑入
            int cx = 20;
            int cy = 20;
            float slideY = (1f - alpha) * 20f;
            var card = new Rectangle(cx, cy + (int)slideY, CardW, CardH);

            _cardRect = card;
            DrawCardBg(sb, card, alpha);
            DrawCardContent(sb, px, card, alpha);
            DrawHighlightForStep(sb, px, alpha);
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
            float bodySc  = 0.70f;
            float subSc   = 0.58f;
            float lineT = font.MeasureString("A").Y * titleSc + 2f;
            float lineB = font.MeasureString("A").Y * bodySc + 1f;

            string title = _stepTitles[_currentStep].Value;
            string body  = _stepBodies[_currentStep].Value;
            bool isAuto  = StepIsAuto[_currentStep];
            float px2 = card.X + 14f;
            float py  = card.Y + 12f;

            //步骤计数
            string counter = $"{_currentStep + 1:D2} / {StepIsAuto.Length:D2}";
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
                string[] wrapped = Utils.WordwrapString(line, font, bodyWrapW, 99, out _);
                foreach (string wl in wrapped) {
                    if (string.IsNullOrEmpty(wl)) continue;
                    Utils.DrawBorderString(sb, wl.TrimEnd('-', ' '), new Vector2(px2, py),
                        new Color(175, 215, 225, (int)(215 * alpha)), bodySc);
                    py += lineB;
                }
            }

            //底部：自动步骤显示状态文字，手动步骤显示NEXT按钮
            if (isAuto) {
                float blink = 0.72f + 0.28f * MathF.Sin(_shaderTimer * 22f);
                bool isCompletionStep = _currentStep == 4 || _currentStep == StepIsAuto.Length - 1;
                string standby = isCompletionStep
                    ? _textCalibrating.Value
                    : _textWaiting.Value;
                float sbW = font.MeasureString(standby).X * subSc;
                Utils.DrawBorderString(sb, standby,
                    new Vector2(card.Right - 14f - sbW, card.Bottom - 16f),
                    new Color(60, 190, 200, (int)(200 * alpha * blink)), subSc);
            } else {
                DrawNextButton(sb, card, alpha);
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

        private static void DrawHighlightForStep(SpriteBatch sb, Texture2D px, float alpha) {
            if (_currentStep == 6) {
                DrawGeneratorHighlight(sb, px, alpha);
                return;
            }
            if (_currentStep != 1) return;
            if (_npcIndex < 0 || _npcIndex >= Main.maxNPCs) return;

            NPC npc = Main.npc[_npcIndex];
            if (!npc.active || npc.type != NPCID.SantaNK1) return;

            float pulse = 0.6f + 0.4f * MathF.Sin(_highlightPulse * 3.2f);
            Color bracketColor = new Color(80, 220, 245, (int)(200 * alpha));
            Color outlineColor = new Color(
                (int)(70 * pulse), (int)(215 * pulse), (int)(245 * pulse),
                (int)(120 * pulse * alpha));

            //切换到游戏视图矩阵，使世界坐标直接可用
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            var npcRect = new Rectangle(
                (int)npc.position.X - 8, (int)npc.position.Y - 8,
                npc.width + 16, npc.height + 16);
            sb.Draw(px, npcRect, outlineColor);
            DrawLBrackets(sb, px, npcRect, bracketColor);

            //切回UI矩阵
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }

        //绘制走廊内热能发电机MK2的高亮标注框（step 6等待玩家选中时）
        private static void DrawGeneratorHighlight(SpriteBatch sb, Texture2D px, float alpha) {
            float pulse = 0.6f + 0.4f * MathF.Sin(_highlightPulse * 3.2f);
            Color bracketColor = new Color(80, 220, 245, (int)(200 * alpha));
            Color outlineColor = new Color(
                (int)(70 * pulse), (int)(215 * pulse), (int)(245 * pulse),
                (int)(120 * pulse * alpha));

            //切换到游戏视图矩阵，直接使用世界坐标
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            var rect = new Rectangle(
                CybCourseGen.GenMK2TileLeft * 16 - 4,
                CybCourseGen.GenMK2TileTop  * 16 - 4,
                CybCourseGen.GenMK2TileW * 16 + 8,
                CybCourseGen.GenMK2TileH * 16 + 8);
            sb.Draw(px, rect, outlineColor);
            DrawLBrackets(sb, px, rect, bracketColor);

            //切回UI矩阵
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }

        private static void DrawLBrackets(SpriteBatch sb, Texture2D px, Rectangle r, Color c) {
            const int len = 14;
            const int thick = 2;
            sb.Draw(px, new Rectangle(r.Left,          r.Top,            len,   thick), c);
            sb.Draw(px, new Rectangle(r.Left,          r.Top,            thick, len),   c);
            sb.Draw(px, new Rectangle(r.Right - len,   r.Top,            len,   thick), c);
            sb.Draw(px, new Rectangle(r.Right - thick, r.Top,            thick, len),   c);
            sb.Draw(px, new Rectangle(r.Left,          r.Bottom - thick, len,   thick), c);
            sb.Draw(px, new Rectangle(r.Left,          r.Bottom - len,   thick, len),   c);
            sb.Draw(px, new Rectangle(r.Right - len,   r.Bottom - thick, len,   thick), c);
            sb.Draw(px, new Rectangle(r.Right - thick, r.Bottom - len,   thick, len),   c);
        }
    }
}
