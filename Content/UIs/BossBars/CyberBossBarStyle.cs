using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.UIs.BossBars
{
    /// <summary>
    /// 赛博朋克2077风格Boss血条
    /// </summary>
    public class CyberBossBarStyle : ModBossBarStyle
    {
        public const int MaxBars = 4;
        public const int BarWidth = 560;
        public const int BarHeight = 18;
        public const int TopMargin = 42;
        public const int VerticalSpacing = 88;

        public static List<CyberBossHPUI> Bars;
        public static List<int> ExclusionList;

        public override void Load() {
            Bars = [];
            ExclusionList = [];
        }

        public override void SetStaticDefaults() {
            Bars = [];
            ExclusionList = [];
        }

        public override void Unload() {
            Bars = null;
            ExclusionList = null;
        }

        public override void Update(IBigProgressBar currentBar, ref BigProgressBarInfo info) {
            foreach (NPC n in Main.ActiveNPCs) {
                if (ExclusionList.Contains(n.type))
                    continue;
                //realLife>=0表示该NPC是某段蠕虫子节点，跳过避免重复添加
                if (n.boss && n.realLife < 0)
                    TryAddBar(n.whoAmI);
            }

            for (int i = 0; i < Bars.Count; i++) {
                Bars[i].Update();
                if (Bars[i].CloseTimer >= CyberBossHPUI.CloseTime) {
                    Bars.RemoveAt(i);
                    i--;
                }
            }
        }

        private static void TryAddBar(int index) {
            if (Bars.Count >= MaxBars) return;
            NPC npc = Main.npc[index];
            if (!npc.active || npc.life <= 0) return;
            if (Bars.Any(b => b.NPCIndex == index)) return;
            Bars.Add(new CyberBossHPUI(index));
        }

        public override bool PreventDraw => true;

        public override void Draw(SpriteBatch sb, IBigProgressBar currentBar, BigProgressBarInfo info) {
            float cx = Main.screenWidth / 2f;
            float y = TopMargin;
            foreach (var ui in Bars) {
                ui.Draw(sb, cx, y);
                y += VerticalSpacing;
            }
        }
    }

    /// <summary>
    /// 单个Boss血条的状态与绘制
    /// </summary>
    public class CyberBossHPUI
    {
        public const int OpenTime = 50;
        public const int CloseTime = 80;
        public const int HitFlashFrames = 22;

        public int NPCIndex;
        public int IntendedType;
        public int OpenTimer;
        public int CloseTimer;
        public long PrevLife;
        public long InitMaxLife;
        public float SmoothRatio = 1f;
        public float TrailRatio = 1f;
        public float HitFlash;

        //赛博朋克2077 HUD标志色：琥珀黄与危险红
        private static readonly Color AmberHigh = new(255, 210, 30);
        private static readonly Color AmberMid = new(255, 138, 30);
        private static readonly Color AmberLow = new(255, 50, 55);
        private static readonly Color InkBlack = new(8, 6, 4);
        private static readonly Color ChromaCyan = new(0, 210, 255);
        private static readonly Color ChromaRed = new(255, 40, 60);

        public NPC Target => Main.npc.IndexInRange(NPCIndex) ? Main.npc[NPCIndex] : null;

        public float LifeRatio {
            get {
                NPC npc = Target;
                if (npc == null || !npc.active || InitMaxLife <= 0)
                    return 0f;
                return MathHelper.Clamp(npc.life / (float)InitMaxLife, 0f, 1f);
            }
        }

        public CyberBossHPUI(int index) {
            NPCIndex = index;
            NPC npc = Target;
            if (npc != null && npc.active) {
                IntendedType = npc.type;
                PrevLife = npc.life;
                InitMaxLife = npc.lifeMax;
            }
        }

        public void Update() {
            NPC npc = Target;
            bool dead = npc == null || !npc.active || npc.type != IntendedType;
            if (dead) {
                CloseTimer = Math.Min(CloseTimer + 1, CloseTime);
                return;
            }
            OpenTimer = Math.Min(OpenTimer + 1, OpenTime);
            if (npc.lifeMax > InitMaxLife)
                InitMaxLife = npc.lifeMax;

            if (npc.life < PrevLife)
                HitFlash = 1f;
            PrevLife = npc.life;

            float target = LifeRatio;
            SmoothRatio = MathHelper.Lerp(SmoothRatio, target, 0.12f);
            TrailRatio = MathHelper.Lerp(TrailRatio, target, 0.03f);
            if (Math.Abs(SmoothRatio - target) < 0.002f) SmoothRatio = target;
            if (Math.Abs(TrailRatio - target) < 0.002f) TrailRatio = target;

            if (HitFlash > 0f) HitFlash -= 1f / HitFlashFrames;
            if (HitFlash < 0f) HitFlash = 0f;
        }

        //威胁色插值(高=琥珀黄,中=橘,低=红)
        private static Color ThreatColor(float r) {
            if (r > 0.6f) return Color.Lerp(AmberMid, AmberHigh, (r - 0.6f) / 0.4f);
            if (r > 0.3f) return Color.Lerp(AmberLow, AmberMid, (r - 0.3f) / 0.3f);
            return AmberLow;
        }

        //lifeMax对数映射出威胁等级(仅视觉)
        private static int ComputeLevel(long maxLife) {
            if (maxLife <= 1) return 1;
            double lv = Math.Log10(maxLife) * 9.5;
            return Math.Clamp((int)lv, 1, 99);
        }

        private string StatusTag() {
            NPC npc = Target;
            if (npc == null || !npc.active) return "[ NEUTRALIZED ]";
            if (LifeRatio < 0.2f) return "[ CRITICAL ]";
            if (LifeRatio < 0.5f) return "[ WOUNDED ]";
            return "[ HOSTILE ]";
        }

        public void Draw(SpriteBatch sb, float cx, float y) {
            NPC npc = Target;
            if (npc == null) return;

            float alpha = MathHelper.Clamp(OpenTimer / (float)OpenTime, 0f, 1f);
            if (CloseTimer > 0)
                alpha = 1f - MathHelper.Clamp(CloseTimer / (float)CloseTime, 0f, 1f);
            if (alpha <= 0f) return;

            //开场赛博闪烁
            if (OpenTimer == 3 || OpenTimer == 7 || OpenTimer == 14)
                alpha *= Main.rand.NextFloat(0.35f, 0.55f);
            if (OpenTimer == 4 || OpenTimer == 8 || OpenTimer == 15)
                alpha *= Main.rand.NextFloat(0.65f, 0.8f);

            Texture2D px = TextureAssets.MagicPixel.Value;
            float barW = CyberBossBarStyle.BarWidth;
            float left = cx - barW / 2f;
            Color primary = ThreatColor(LifeRatio);
            Color primaryDim = primary * 0.35f;

            var nameFont = FontAssets.DeathText.Value;
            var sFont = FontAssets.MouseText.Value;
            const float nameScale = 0.48f;
            const float smallScale = 0.82f;
            const float tagScale = 0.72f;

            string name = npc.FullName.ToUpperInvariant();
            string lvText = $"LV.{ComputeLevel(InitMaxLife):00}";
            string hpText = $"{Math.Max(npc.life, 0):N0} / {InitMaxLife:N0}";

            Vector2 nameSize = nameFont.MeasureString(name) * nameScale;
            Vector2 lvSize = sFont.MeasureString(lvText) * smallScale;
            Vector2 hpSize = sFont.MeasureString(hpText) * smallScale;

            float topH = nameSize.Y;

            //———— 等级章(始终琥珀黄底黑字,带斜切) ————
            //LV底色固定使用高威胁琥珀黄,与威胁色解耦保证黄底黑字可读
            Color lvBg = AmberHigh;
            float lvBoxW = lvSize.X + 18;
            float lvBoxH = lvSize.Y + 4;
            float lvBoxY = y + (topH - lvBoxH) / 2f;
            sb.Draw(px, new Rectangle((int)left, (int)lvBoxY, (int)lvBoxW, (int)lvBoxH), lvBg * alpha);
            //右下切角(用透明扣除外形)
            sb.Draw(px, new Rectangle((int)(left + lvBoxW - 4), (int)(lvBoxY + lvBoxH - 3), 4, 3), Color.Transparent);
            sb.Draw(px, new Rectangle((int)(left + lvBoxW - 7), (int)(lvBoxY + lvBoxH - 2), 3, 2), Color.Transparent);
            //黑字无描边以避免覆盖字芯
            Utils.DrawBorderStringFourWay(sb, sFont, lvText,
                left + 9, lvBoxY + 1, Color.Black * alpha, Color.Transparent, Vector2.Zero, smallScale);

            //———— 名称(仅受击时色散) ————
            float nameX = left + lvBoxW + 10;
            float chroma = HitFlash * 4f;
            if (chroma > 0.5f) {
                Utils.DrawBorderStringFourWay(sb, nameFont, name,
                    nameX - chroma, y, ChromaCyan * (alpha * 0.55f), Color.Transparent, Vector2.Zero, nameScale);
                Utils.DrawBorderStringFourWay(sb, nameFont, name,
                    nameX + chroma, y, ChromaRed * (alpha * 0.55f), Color.Transparent, Vector2.Zero, nameScale);
            }
            Utils.DrawBorderStringFourWay(sb, nameFont, name,
                nameX, y, primary * alpha, InkBlack * alpha, Vector2.Zero, nameScale);

            //———— HP数值(右对齐) ————
            float hpY = y + (topH - hpSize.Y) / 2f;
            Utils.DrawBorderStringFourWay(sb, sFont, hpText,
                left + barW - hpSize.X, hpY, primary * alpha, InkBlack * alpha, Vector2.Zero, smallScale);

            y += topH + 6;

            //———— 血条区 ————
            float bLeft = left;
            float bWidth = barW;
            int bH = CyberBossBarStyle.BarHeight;

            //灰色拖尾
            int trailW = (int)(bWidth * TrailRatio);
            if (trailW > 0) {
                sb.Draw(px, new Rectangle((int)bLeft + 2, (int)y + 3, trailW - 4, bH - 6),
                    new Color(130, 120, 100) * (alpha * 0.45f));
            }

            //着色器主条(外扩以容纳斜边)
            DrawShaderBar(sb, bLeft - 4, y - 2, bWidth + 8, bH + 4, alpha);

            //———— Additive辉光层 ————
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            int glowW = (int)(bWidth * SmoothRatio);
            if (glowW > 0) {
                sb.Draw(px, new Rectangle((int)bLeft, (int)y - 5, glowW, bH + 10),
                    primary * (alpha * 0.18f));
                if (glowW > 8) {
                    sb.Draw(px, new Rectangle((int)(bLeft + glowW - 6), (int)y - 6, 12, bH + 12),
                        primary * (alpha * 0.55f));
                }
                if (HitFlash > 0.01f) {
                    sb.Draw(px, new Rectangle((int)bLeft, (int)y - 4, glowW, bH + 8),
                        new Color(255, 240, 180) * (alpha * HitFlash * 0.55f));
                }
            }

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            //———— 底部状态标签 ————
            float botY = y + bH + 6;

            //左下ID(小字直接用主色,不加描边)
            string idTag = $"TYPE:{npc.type:0000}";
            sb.Draw(px, new Rectangle((int)left, (int)(botY + 4), 4, 4), primary * (alpha * 0.85f));
            sb.DrawString(sFont, idTag, new Vector2(left + 8, botY + 2), primary * (alpha * 0.85f), 0f, Vector2.Zero, tagScale, SpriteEffects.None, 0f);

            //右下状态标签(危急闪烁)
            string tag = StatusTag();
            float tagAlpha = alpha;
            if (LifeRatio < 0.2f) {
                tagAlpha *= 0.6f + 0.4f * (float)Math.Sin(Main.GameUpdateCount * 0.25f);
            }
            Vector2 tagSize = sFont.MeasureString(tag) * tagScale;
            sb.DrawString(sFont, tag, new Vector2(left + barW - tagSize.X, botY + 2), primary * tagAlpha, 0f, Vector2.Zero, tagScale, SpriteEffects.None, 0f);
        }

        private void DrawShaderBar(SpriteBatch sb, float x, float y, float w, float h, float alpha) {
            Effect effect = EffectLoader.CyberBossBar?.Value;
            Texture2D px = TextureAssets.MagicPixel.Value;

            if (effect != null) {
                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                    DepthStencilState.None, RasterizerState.CullNone, effect, Main.UIScaleMatrix);

                effect.Parameters["uTime"]?.SetValue((float)Main.gameTimeCache.TotalGameTime.TotalSeconds);
                effect.Parameters["uLifeRatio"]?.SetValue(SmoothRatio);
                effect.Parameters["uHitFlash"]?.SetValue(HitFlash);
                effect.Parameters["uBarSize"]?.SetValue(new Vector2(w, h));
                effect.CurrentTechnique.Passes[0].Apply();

                sb.Draw(px, new Rectangle((int)x, (int)y, (int)w, (int)h),
                    null, Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, 0f);

                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            }
            else {
                //降级绘制
                Color fallback = ThreatColor(LifeRatio);
                int fillW = (int)(w * SmoothRatio);
                float segW = w / 20f;
                for (int i = 0; i < 20; i++) {
                    float segStart = i * segW;
                    float segEnd = (i + 1) * segW - 2;
                    if (segStart >= fillW) break;
                    float end = Math.Min(segEnd, fillW);
                    if (end <= segStart) continue;
                    sb.Draw(px, new Rectangle(
                        (int)(x + segStart), (int)y,
                        (int)(end - segStart), (int)h), fallback * alpha);
                }
            }
        }
    }
}
