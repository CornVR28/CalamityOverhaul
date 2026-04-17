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
                if (n.IsABoss())
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

        private static readonly Color PanelBg = new(6, 1, 1, 170);
        private static readonly Color PrimaryRed = new(210, 28, 28);
        private static readonly Color DarkRed = new(45, 6, 6);
        private static readonly Color TrailColor = new(130, 18, 18, 110);
        private static readonly Color GlowRed = new(255, 30, 30, 40);
        private static readonly Color TextShadow = new(12, 0, 0);
        private static readonly Color AccentDim = new(180, 22, 22, 180);

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

            //受击检测
            if (npc.life < PrevLife) {
                HitFlash = 1f;
            }
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

            //动画透明度
            float alpha = MathHelper.Clamp(OpenTimer / (float)OpenTime, 0f, 1f);
            if (CloseTimer > 0)
                alpha = 1f - MathHelper.Clamp(CloseTimer / (float)CloseTime, 0f, 1f);
            if (alpha <= 0f) return;

            //开场闪烁
            if (OpenTimer == 3 || OpenTimer == 7 || OpenTimer == 14)
                alpha *= Main.rand.NextFloat(0.4f, 0.6f);
            if (OpenTimer == 4 || OpenTimer == 8 || OpenTimer == 15)
                alpha *= Main.rand.NextFloat(0.7f, 0.85f);

            Texture2D px = TextureAssets.MagicPixel.Value;
            float left = cx - CyberBossBarStyle.BarWidth / 2f;

            //背景面板
            sb.Draw(px, new Rectangle(
                (int)(left - 14), (int)(y - 8),
                CyberBossBarStyle.BarWidth + 28, 80),
                PanelBg * alpha);

            //名称
            var titleFont = FontAssets.DeathText.Value;
            const float nameScale = 0.44f;
            string name = npc.FullName;
            Vector2 nameSize = titleFont.MeasureString(name) * nameScale;
            float nameX = cx - nameSize.X / 2f;
            Utils.DrawBorderStringFourWay(sb, titleFont, name,
                nameX, y, PrimaryRed * alpha, TextShadow * alpha, Vector2.Zero, nameScale);

            y += nameSize.Y + 3;

            //顶部装饰线
            sb.Draw(px, new Rectangle((int)left, (int)y, CyberBossBarStyle.BarWidth, 2), PrimaryRed * alpha);
            y += 5;

            //百分比文本
            var textFont = FontAssets.MouseText.Value;
            const float pctScale = 0.85f;
            string pctText = $"{(int)(LifeRatio * 100)}%";
            Vector2 pctSize = textFont.MeasureString(pctText) * pctScale;
            Utils.DrawBorderStringFourWay(sb, textFont, pctText,
                left, y, PrimaryRed * alpha, TextShadow * alpha, Vector2.Zero, pctScale);

            //血条区域
            float barLeft = left + pctSize.X + 10;
            float barWidth = CyberBossBarStyle.BarWidth - pctSize.X - 10;
            int barH = CyberBossBarStyle.BarHeight;

            //暗底
            sb.Draw(px, new Rectangle((int)barLeft, (int)y, (int)barWidth, barH), DarkRed * alpha);

            //拖尾
            int trailW = (int)(barWidth * TrailRatio);
            if (trailW > 0)
                sb.Draw(px, new Rectangle((int)barLeft, (int)y, trailW, barH), TrailColor * alpha);

            //着色器血条绘制
            DrawShaderBar(sb, barLeft, y, barWidth, barH, alpha);

            //底部装饰线
            float bottomY = y + barH + 3;
            sb.Draw(px, new Rectangle((int)left, (int)bottomY, CyberBossBarStyle.BarWidth, 1), AccentDim * alpha);
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
                //着色器不可用时的降级绘制
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

            //末端辉光叠加层
            int glowFillW = (int)(w * SmoothRatio);
            if (glowFillW > 2) {
                sb.Draw(px, new Rectangle(
                    (int)(x + glowFillW - 4), (int)(y - 2),
                    8, (int)h + 4), GlowRed * alpha);
            }
        }
    }
}
