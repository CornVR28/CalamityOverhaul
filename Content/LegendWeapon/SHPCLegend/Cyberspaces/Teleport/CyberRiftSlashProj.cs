using CalamityOverhaul.Common;
using InnoVault.Trails;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Teleport
{
    /// <summary>
    /// 赛博瞬移维度数据裂缝弹幕
    /// <br/>从起点劈向目标点的高能裂缝条带——快速延伸 → 命中冲击 → 急速尾收
    /// <br/>使用 <see cref="Trail"/> + CyberRiftSlash.fx 渲染，颜色主调橙红高亮
    /// <br/>路径采用"主轴 + 锯齿微偏移 + 垂向弧度"组合，区别于 CyberGlitchBolt 的散射形态
    /// </summary>
    internal class CyberRiftSlashProj : ModProjectile, IPrimitiveDrawable
    {
        public override string Texture => CWRConstant.Placeholder;

        //总寿命（约 0.4 秒），调短了，强化"快"
        private const int MaxLife = 26;
        //命中目标的归一化时间点：在此帧前完成"延伸"，到达后触发冲击脉冲
        private const float ImpactT = 0.30f;

        private Vector2 startPos;
        private Vector2 endPos;
        private Vector2[] points;
        private int pointCount;
        private bool pathReady;
        private float glitchSeed;
        private Trail trail;

        private float visibleStart;
        private float visibleEnd;
        private float fadeAlpha;
        private float impactPulse;

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
            if (!pathReady) {
                startPos = Projectile.Center;
                endPos = new Vector2(Projectile.ai[0], Projectile.ai[1]);
                glitchSeed = Main.rand.NextFloat();
                GeneratePath();
                pathReady = true;
            }

            float t = 1f - (float)Projectile.timeLeft / MaxLife;
            ComputeAnimation(t);
        }

        private void ComputeAnimation(float t) {
            //三段式：极速延伸（0~0.30）→ 命中全亮闪烁（0.30~0.45）→ 急速尾收（0.45~1）
            if (t < ImpactT) {
                //缓出：起步极快，临近命中再"砸"到目标
                float ext = t / ImpactT;
                visibleEnd = 1f - MathF.Pow(1f - ext, 2.4f);
                visibleStart = 0f;
                fadeAlpha = MathHelper.SmoothStep(0.5f, 1f, ext);
                impactPulse = 0f;
            }
            else if (t < 0.45f) {
                //命中段：整段全亮，并叠加冲击脉冲
                visibleEnd = 1f;
                visibleStart = 0f;
                float phase = (t - ImpactT) / (0.45f - ImpactT);
                fadeAlpha = 1f + (1f - phase) * 0.3f;
                //命中脉冲在阶段中段达峰，给"砸进目标"的冲击瞬感
                impactPulse = MathF.Sin(phase * MathF.PI);
            }
            else {
                //尾收段：从起点端开始消散（visibleStart 上升）
                float retract = (t - 0.45f) / 0.55f;
                visibleEnd = 1f;
                visibleStart = retract;
                fadeAlpha = 1f - retract;
                impactPulse = MathF.Max(0f, 0.4f - retract * 0.6f);
            }
            fadeAlpha = MathHelper.Clamp(fadeAlpha, 0f, 1.4f);
            impactPulse = MathHelper.Clamp(impactPulse, 0f, 1f);
        }

        private void GeneratePath() {
            //从 startPos 到 endPos 的笔直主轴 + 锯齿微偏 + 单次大弧形偏移
            Vector2 axis = endPos - startPos;
            float length = axis.Length();
            if (length < 1f) {
                points = new Vector2[] { startPos, endPos };
                pointCount = 2;
                return;
            }
            Vector2 dir = axis / length;
            Vector2 perp = new(-dir.Y, dir.X);

            //段数随距离自适应（近距离也保留足够细节让 shader 出效果）
            int segs = (int)MathHelper.Clamp(length / 36f, 12f, 28f);
            pointCount = segs + 1;
            points = new Vector2[pointCount];

            //主弧度方向（左右随机）+ 弧高比例（让劈砍轨迹有"挥砍弧线"感而不是死直）
            float arcSign = Main.rand.NextBool() ? 1f : -1f;
            float arcMag = MathHelper.Clamp(length * 0.06f, 8f, 56f);

            for (int i = 0; i < pointCount; i++) {
                float k = i / (float)(pointCount - 1);
                Vector2 basePos = startPos + axis * k;

                //大弧度：sin 半周
                float arc = MathF.Sin(k * MathF.PI) * arcMag * arcSign;

                //锯齿小偏移（端点压平，中段最大）
                float jagBand = MathF.Sin(k * MathF.PI);
                float jagAmp = jagBand * MathHelper.Clamp(length * 0.012f, 4f, 14f);
                float jag = Main.rand.NextFloat(-jagAmp, jagAmp);

                //靠近端点时减小偏移，避免起/终位置歪
                float endpointDamp = MathHelper.SmoothStep(0f, 1f, MathF.Min(k, 1f - k) * 4f);
                float offset = (arc + jag) * endpointDamp;

                points[i] = basePos + perp * offset;
            }
        }

        private float WidthFunction(float progress) {
            //梭形——两端尖、中段宽，给"剑气"感
            float taper = MathF.Sin(progress * MathF.PI);
            taper = MathF.Pow(MathF.Max(taper, 0.05f), 0.85f);
            //命中时整体加粗一帧，强化冲击感
            float boost = 1f + impactPulse * 0.35f;
            return 44f * taper * boost;
        }

        private Color ColorFunction(Vector2 _) => Color.White;

        public override bool PreDraw(ref Color lightColor) => false;

        void IPrimitiveDrawable.DrawPrimitives() {
            if (!pathReady || points == null || fadeAlpha < 0.01f) {
                return;
            }

            Effect shader = EffectLoader.CyberRiftSlash?.Value;
            if (shader == null) return;
            Texture2D noise = CWRAsset.Extra_193?.Value;
            if (noise == null) return;

            trail ??= new Trail(points, WidthFunction, ColorFunction);
            trail.TrailPositions = points;

            shader.Parameters["transformMatrix"]?.SetValue(VaultUtils.GetTransfromMatrix());
            shader.Parameters["uTime"]?.SetValue(Cyberspace.EffectTime);
            shader.Parameters["fadeAlpha"]?.SetValue(MathHelper.Clamp(fadeAlpha, 0f, 1f));
            shader.Parameters["visibleStart"]?.SetValue(visibleStart);
            shader.Parameters["visibleEnd"]?.SetValue(visibleEnd);
            shader.Parameters["glitchSeed"]?.SetValue(glitchSeed);
            shader.Parameters["impactPulse"]?.SetValue(impactPulse);
            shader.Parameters["uNoiseTex"]?.SetValue(noise);

            GraphicsDevice device = Main.graphics.GraphicsDevice;
            device.BlendState = BlendState.Additive;
            trail.DrawTrail(shader);
            device.BlendState = BlendState.AlphaBlend;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
