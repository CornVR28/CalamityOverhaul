using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces
{
    /// <summary>
    /// 赛博空间故障闪电弹幕
    /// <br/>领域展开时生成的闪电形黑墙故障线——从中心快速延伸再收缩消失
    /// <br/>锯齿状折线路径，多层加法辉光绘制，深红色系
    /// </summary>
    internal class CyberGlitchBoltProj : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        private const int MaxLife = 24;
        private const int MaxPoints = 14;
        private Vector2[] points;
        private float[] segBrightness;
        private int pointCount;
        private bool pathReady;

        private static Asset<Texture2D> softGlowAsset;

        public override void SetStaticDefaults() {
            softGlowAsset = ModContent.Request<Texture2D>(CWRConstant.Masking + "SoftGlow");
        }

        public override void SetDefaults() {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = MaxLife;
        }

        public override void AI() {
            //ai[0] = 主方向角度, ai[1] = 延迟帧数
            if (Projectile.ai[1] > 0) {
                Projectile.ai[1]--;
                Projectile.timeLeft = MaxLife;
                return;
            }

            if (!pathReady) {
                GeneratePath();
                pathReady = true;
            }
        }

        private void GeneratePath() {
            float angle = Projectile.ai[0];
            pointCount = Main.rand.Next(8, MaxPoints + 1);
            points = new Vector2[pointCount];
            segBrightness = new float[pointCount];

            Vector2 current = Projectile.Center;
            points[0] = current;
            segBrightness[0] = 1f;

            float domainRadius = Cyberspace.Radius;

            for (int i = 1; i < pointCount; i++) {
                //越远离中心，锯齿偏转越大
                float distFactor = (float)i / pointCount;
                float jag = Main.rand.NextFloat(-0.55f, 0.55f) * (0.6f + distFactor * 0.8f);

                //段长度：越远越长（加速感）
                float segLen = Main.rand.NextFloat(28f, 50f) * (0.8f + distFactor * 0.5f);

                //偶尔出现分叉式大偏转
                if (Main.rand.NextFloat() < 0.15f)
                    jag += (Main.rand.NextBool() ? 1f : -1f) * 0.8f;

                Vector2 step = (angle + jag).ToRotationVector2() * segLen;
                current += step;
                points[i] = current;
                segBrightness[i] = Main.rand.NextFloat(0.55f, 1.0f);
            }
        }

        public override bool PreDraw(ref Color lightColor) {
            if (!pathReady || points == null || Projectile.ai[1] > 0)
                return false;

            float t = 1f - (float)Projectile.timeLeft / MaxLife;

            //阶段：快速延伸(0→0.3) → 短暂全亮(0.3→0.42) → 收缩消失(0.42→1.0)
            float visibleFrac;
            float alpha;
            if (t < 0.30f) {
                //延伸：缓出曲线
                float extend = t / 0.30f;
                visibleFrac = 1f - MathF.Pow(1f - extend, 2.5f);
                alpha = MathHelper.SmoothStep(0.4f, 1f, extend);
            }
            else if (t < 0.42f) {
                visibleFrac = 1f;
                //全亮阶段闪烁
                float flash = MathF.Sin((t - 0.30f) / 0.12f * MathF.PI);
                alpha = 1f + flash * 0.5f;
            }
            else {
                float retract = (t - 0.42f) / 0.58f;
                visibleFrac = 1f - retract;
                alpha = 1f - retract;
            }

            visibleFrac = MathHelper.Clamp(visibleFrac, 0f, 1f);
            alpha = MathHelper.Clamp(alpha, 0f, 1.5f);
            if (alpha < 0.01f) return false;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            DrawBolt(visibleFrac, alpha);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void DrawBolt(float visibleFrac, float alpha) {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            if (pixel == null) return;

            float visibleCount = visibleFrac * (pointCount - 1);
            int fullSegs = (int)visibleCount;
            float partial = visibleCount - fullSegs;

            Vector2 pixelOrigin = new Vector2(0f, 0.5f);

            for (int i = 0; i < fullSegs && i < pointCount - 1; i++) {
                float segAlpha = alpha * segBrightness[i];
                DrawSegment(pixel, points[i], points[i + 1], segAlpha, pixelOrigin);
            }

            //局部最后一段
            if (fullSegs < pointCount - 1 && partial > 0.01f) {
                Vector2 partEnd = Vector2.Lerp(points[fullSegs], points[fullSegs + 1], partial);
                float segAlpha = alpha * segBrightness[fullSegs] * partial;
                DrawSegment(pixel, points[fullSegs], partEnd, segAlpha, pixelOrigin);
            }

            //尖端辉光点
            DrawTipGlow(visibleFrac, alpha);
        }

        private void DrawSegment(Texture2D pixel, Vector2 from, Vector2 to, float alpha, Vector2 origin) {
            Vector2 dir = to - from;
            float len = dir.Length();
            if (len < 0.5f) return;
            float rot = dir.ToRotation();

            Vector2 screenFrom = from - Main.screenPosition;

            //外层辉光（宽，暗）
            Color outerColor = new Color(0.45f, 0.025f, 0.03f) * (alpha * 0.55f);
            Main.spriteBatch.Draw(pixel, screenFrom, null, outerColor,
                rot, origin, new Vector2(len, 10f), SpriteEffects.None, 0f);

            //中层主体
            Color midColor = new Color(0.85f, 0.10f, 0.07f) * alpha;
            Main.spriteBatch.Draw(pixel, screenFrom, null, midColor,
                rot, origin, new Vector2(len, 4f), SpriteEffects.None, 0f);

            //核心高亮
            Color coreColor = new Color(1f, 0.50f, 0.35f) * (alpha * 0.75f);
            Main.spriteBatch.Draw(pixel, screenFrom, null, coreColor,
                rot, origin, new Vector2(len, 1.8f), SpriteEffects.None, 0f);
        }

        private void DrawTipGlow(float visibleFrac, float alpha) {
            Texture2D glow = softGlowAsset?.Value;
            if (glow == null) return;

            //计算当前尖端位置
            float idx = visibleFrac * (pointCount - 1);
            int segIdx = Math.Clamp((int)idx, 0, pointCount - 2);
            float segFrac = idx - segIdx;
            Vector2 tipWorld = Vector2.Lerp(points[segIdx], points[segIdx + 1], segFrac);
            Vector2 tipScreen = tipWorld - Main.screenPosition;

            Vector2 glowOrigin = glow.Size() * 0.5f;
            float glowSize = 40f / glow.Width;

            //暗红大辉光
            Color outerGlow = new Color(0.5f, 0.03f, 0.03f) * (alpha * 0.5f);
            Main.spriteBatch.Draw(glow, tipScreen, null, outerGlow,
                0f, glowOrigin, glowSize * 1.8f, SpriteEffects.None, 0f);

            //明亮核心
            Color coreGlow = new Color(1f, 0.35f, 0.2f) * (alpha * 0.7f);
            Main.spriteBatch.Draw(glow, tipScreen, null, coreGlow,
                0f, glowOrigin, glowSize * 0.7f, SpriteEffects.None, 0f);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
