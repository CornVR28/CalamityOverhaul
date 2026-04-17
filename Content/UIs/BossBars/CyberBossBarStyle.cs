using CalamityOverhaul.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        public const int BarWidth = 520;
        public const int BarHeight = 14;
        public const int TopMargin = 36;
        public const int VerticalSpacing = 72;

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
                if (n.boss)
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
        public const int HitFlashFrames = 20;

        public int NPCIndex;
        public int IntendedType;
        public int OpenTimer;
        public int CloseTimer;
        public long PrevLife;
        public long InitMaxLife;
        public float SmoothRatio = 1f;
        public float TrailRatio = 1f;
        public float HitFlash;

        private static readonly Color PrimaryRed = new(210, 28, 28);
        private static readonly Color DimRed = new(90, 12, 12);
        private static readonly Color TrailColor = new(140, 20, 20, 120);
        private static readonly Color GlowCore = new(255, 60, 40, 55);
        private static readonly Color GlowSoft = new(200, 25, 15, 22);

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

            //名称（左对齐，实心黑边）
            var titleFont = FontAssets.DeathText.Value;
            const float nameScale = 0.44f;
            string name = npc.FullName;
            Vector2 nameSize = titleFont.MeasureString(name) * nameScale;
            Utils.DrawBorderStringFourWay(sb, titleFont, name,
                left, y, PrimaryRed * alpha, Color.Black * alpha, Vector2.Zero, nameScale);

            y += nameSize.Y + 2;

            //分隔线
            sb.Draw(px, new Rectangle((int)left, (int)y, (int)barW, 1), PrimaryRed * (alpha * 0.65f));
            y += 4;

            //百分比（左对齐）
            var textFont = FontAssets.MouseText.Value;
            const float pctScale = 0.8f;
            string pctText = $"{(int)(LifeRatio * 100)}%";
            Vector2 pctSize = textFont.MeasureString(pctText) * pctScale;
            Utils.DrawBorderStringFourWay(sb, textFont, pctText,
                left, y, PrimaryRed * alpha, Color.Black * alpha, Vector2.Zero, pctScale);

            //血条区域
            float bLeft = left + pctSize.X + 8;
            float bWidth = barW - pctSize.X - 8;
            int bH = CyberBossBarStyle.BarHeight;

            //拖尾
            int trailW = (int)(bWidth * TrailRatio);
            if (trailW > 0)
                sb.Draw(px, new Rectangle((int)bLeft, (int)y + 1, trailW, bH - 2), TrailColor * alpha);

            //Additive辉光层
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            int glowW = (int)(bWidth * SmoothRatio);
            if (glowW > 0) {
                sb.Draw(px, new Rectangle((int)bLeft, (int)y - 3, glowW, bH + 6), GlowSoft * alpha);
                sb.Draw(px, new Rectangle((int)bLeft, (int)y - 1, glowW, bH + 2), GlowSoft * (alpha * 0.5f));
            }
            if (glowW > 6)
                sb.Draw(px, new Rectangle((int)(bLeft + glowW - 5), (int)(y - 4), 10, bH + 8), GlowCore * alpha);
            if (HitFlash > 0.01f)
                sb.Draw(px, new Rectangle((int)bLeft, (int)y - 2, glowW, bH + 4), GlowCore * (alpha * HitFlash * 0.5f));

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            //着色器血条（加高矩形适配弧度）
            int shaderH = bH + 12;
            float shaderY = y - 6;
            DrawShaderBar(sb, bLeft, shaderY, bWidth, shaderH, alpha);

            //底部细线
            //float bottomY = y + bH + 3;
            //sb.Draw(px, new Rectangle((int)left, (int)bottomY, (int)barW, 1), DimRed * (alpha * 0.4f));
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
                        (int)(end - segStart), (int)h), PrimaryRed * alpha);
                }
            }
        }
    }
}
