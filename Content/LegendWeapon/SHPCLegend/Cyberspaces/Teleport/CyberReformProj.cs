using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Teleport
{
    /// <summary>
    /// 赛博瞬移终点演出弹幕（重做版）
    /// <br/>抛弃旧版的同心圆/楔形数据块聚拢，改为锋锐的"数据残片向心冲撞 → 维度撕裂十字闪 → 故障像素柱"三段式
    /// <br/>整体演出基调：直线、锐角、十字撕裂；不再出现任何同心圆形闪光
    /// <br/>仅依赖单像素纹理 + 加法混合，不需要新的 shader
    /// </summary>
    internal class CyberReformProj : ModProjectile
    {
        public override string Texture => CWRConstant.Placeholder;

        //生命：与 CyberTeleport.HideDuration(22) 对应；snap 闪光与玩家重现帧对齐
        private const int Lifetime = 32;
        //撕裂闪光命中点(对应玩家重现，t≈22/32=0.69)
        private const float SnapPeakT = 0.65f;
        //残片冲入完成时刻——略早于 snap，留出一点"咬合"前置
        private const float ConvergeCompleteT = 0.62f;

        //向心残片数量
        private const int ShardCount = 6;
        //后段故障柱数量
        private const int PillarCount = 5;

        //残片来袭角度
        private float[] shardAngles;
        //残片入场延迟相位(让冲入有先后差)
        private float[] shardLag;
        //故障柱 X 偏移(像素)
        private float[] pillarX;
        //故障柱基础高度
        private float[] pillarH;
        //故障柱独立闪烁种子
        private float[] pillarSeed;
        //十字撕裂的整体倾角(每次瞬移随机一份)
        private float crossRot;

        public override void SetDefaults() {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI() {
            if (Projectile.localAI[0] == 0f) {
                Init();
                Projectile.localAI[0] = 1f;
            }
        }

        private void Init() {
            shardAngles = new float[ShardCount];
            shardLag = new float[ShardCount];
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < ShardCount; i++) {
                //均匀环带 + 角度抖动，避免六边形对称
                shardAngles[i] = baseAngle + i * MathHelper.TwoPi / ShardCount
                    + Main.rand.NextFloat(-0.32f, 0.32f);
                shardLag[i] = Main.rand.NextFloat(0f, 0.18f);
            }

            pillarX = new float[PillarCount];
            pillarH = new float[PillarCount];
            pillarSeed = new float[PillarCount];
            for (int i = 0; i < PillarCount; i++) {
                pillarX[i] = Main.rand.NextFloat(-44f, 44f);
                pillarH[i] = Main.rand.NextFloat(120f, 220f);
                pillarSeed[i] = Main.rand.NextFloat();
            }

            crossRot = Main.rand.NextFloat(-0.45f, 0.45f);
        }

        public override bool PreDraw(ref Color lightColor) {
            Texture2D pixel = CWRAsset.Placeholder_White?.Value;
            if (pixel == null || shardAngles == null) {
                return false;
            }

            float t = 1f - (float)Projectile.timeLeft / Lifetime;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            DrawShards(pixel, drawPos, t);
            DrawSnapCross(pixel, drawPos, t);
            DrawGlitchPillars(pixel, drawPos, t);
            DrawCenterSpark(pixel, drawPos, t);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        //数据残片向心收束：呈彗星状直线冲入，到 ConvergeCompleteT 时刚好咬合在中心
        private void DrawShards(Texture2D pixel, Vector2 drawPos, float t) {
            const float StartDist = 230f;
            const float ShardLength = 86f;
            const float ShardWidth = 6f;

            Vector2 origin = pixel.Size() * 0.5f;

            for (int i = 0; i < ShardCount; i++) {
                float lag = shardLag[i];
                float span = ConvergeCompleteT - lag;
                if (span <= 0f) continue;
                float local = MathHelper.Clamp((t - lag) / span, 0f, 1f);
                if (local <= 0f) continue;

                //缓出：起步极快，收尾减速"咬"进中心
                float ease = 1f - MathF.Pow(1f - local, 2.6f);
                float dist = MathHelper.Lerp(StartDist, 0f, ease);

                float ang = shardAngles[i];
                Vector2 dir = new(MathF.Cos(ang), MathF.Sin(ang));
                Vector2 shardCenter = drawPos + dir * dist;

                //临近撞中心时再加一记亮闪
                float alpha = MathHelper.Clamp(0.55f + local * 0.55f, 0f, 1f);
                if (local > 0.88f) {
                    alpha *= 1f + (local - 0.88f) * 6f;
                }

                Color core = new Color(1f, 0.85f, 0.55f) * alpha * 0.85f;
                Color outer = new Color(1f, 0.45f, 0.18f) * alpha * 0.55f;

                //旋转：让残片"长轴"指向飞行方向，尾巴自然在外侧
                float rot = ang;

                Main.spriteBatch.Draw(pixel, shardCenter, null, outer, rot, origin,
                    new Vector2(ShardLength * 1.15f, ShardWidth * 2.2f),
                    SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(pixel, shardCenter, null, core, rot, origin,
                    new Vector2(ShardLength, ShardWidth),
                    SpriteEffects.None, 0f);
            }
        }

        //维度撕裂十字闪：两条交叉光带，在 SnapPeakT 一拍达峰
        private void DrawSnapCross(Texture2D pixel, Vector2 drawPos, float t) {
            const float Window = 0.18f;
            float delta = MathF.Abs(t - SnapPeakT);
            if (delta > Window) return;

            //命中前快速达峰，命中后稍微拖尾让重现帧带"残光"
            float u = 1f - delta / Window;
            float pulse = MathF.Pow(u, 1.5f);
            if (t > SnapPeakT) {
                float decay = (t - SnapPeakT) / Window;
                pulse *= 1f - decay * 0.55f;
            }
            pulse = MathHelper.Clamp(pulse, 0f, 1f);

            float length = MathHelper.Lerp(180f, 360f, pulse);
            float widthMain = MathHelper.Lerp(2f, 18f, pulse);
            float widthThin = MathHelper.Lerp(1f, 6f, pulse);

            Color hot = new Color(1f, 0.95f, 0.78f) * pulse;
            Color warm = new Color(1f, 0.55f, 0.22f) * pulse * 0.8f;

            Vector2 origin = pixel.Size() * 0.5f;
            //两条带子互相不严格垂直，避免完美十字带来的"图标感"
            float r1 = crossRot;
            float r2 = crossRot + MathHelper.PiOver2 + 0.18f;

            //外焰
            Main.spriteBatch.Draw(pixel, drawPos, null, warm, r1, origin,
                new Vector2(length, widthMain * 1.6f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, drawPos, null, warm, r2, origin,
                new Vector2(length * 0.85f, widthMain * 1.3f), SpriteEffects.None, 0f);
            //白热细芯
            Main.spriteBatch.Draw(pixel, drawPos, null, hot, r1, origin,
                new Vector2(length, widthThin), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, drawPos, null, hot, r2, origin,
                new Vector2(length * 0.85f, widthThin * 0.85f), SpriteEffects.None, 0f);
        }

        //故障像素柱：snap 之后从中心向上"窜"出的竖向数据条带 + 少量横向撕裂线
        private void DrawGlitchPillars(Texture2D pixel, Vector2 drawPos, float t) {
            const float StartT = 0.55f;
            if (t < StartT) return;

            float local = MathHelper.Clamp((t - StartT) / (1f - StartT), 0f, 1f);
            //升起阶段(0~0.55)→ 消散阶段(0.55~1)
            float rise = local < 0.55f ? local / 0.55f : 1f;
            float fade = local < 0.55f ? 1f : 1f - (local - 0.55f) / 0.45f;
            fade = MathHelper.Clamp(fade, 0f, 1f);

            float time = (float)Main.timeForVisualEffects * 0.18f;
            Vector2 origin = pixel.Size() * 0.5f;

            for (int i = 0; i < PillarCount; i++) {
                float seed = pillarSeed[i];
                //节拍式闪烁；某些柱体瞬间被剪断，避免观感工整
                float flick = 0.4f + 0.6f * MathF.Sin(time * 11f + seed * 17f);
                if (flick < 0.3f) continue;

                float h = pillarH[i] * rise;
                float w = MathHelper.Lerp(2f, 5f, seed);
                float x = pillarX[i];
                Vector2 pos = drawPos + new Vector2(x, -h * 0.5f - 6f);

                Color core = new Color(1f, 0.75f, 0.45f) * fade * 0.55f * flick;
                Color edge = new Color(1f, 0.35f, 0.18f) * fade * 0.5f * flick;

                Main.spriteBatch.Draw(pixel, pos, null, edge, 0f, origin,
                    new Vector2(w * 2.2f, h), SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(pixel, pos, null, core, 0f, origin,
                    new Vector2(w, h), SpriteEffects.None, 0f);
            }

            //横向撕裂线：补全画面平衡，避免后段只剩竖向
            const int TearCount = 4;
            for (int i = 0; i < TearCount; i++) {
                float seed = (i + 1) * 0.137f + pillarSeed[i % PillarCount] * 0.5f;
                float life = MathF.Sin(time * 9f + seed * 13f);
                if (life < 0.2f) continue;

                float yOff = MathHelper.Lerp(-50f, 50f, (seed * 7f) % 1f);
                float len = MathHelper.Lerp(40f, 90f, (seed * 3.7f) % 1f) * (0.5f + life * 0.5f);
                Vector2 pos = drawPos + new Vector2(0f, yOff);
                Color tearC = new Color(1f, 0.85f, 0.55f) * fade * 0.5f * life;

                Main.spriteBatch.Draw(pixel, pos, null, tearC, 0f, origin,
                    new Vector2(len, 1.5f), SpriteEffects.None, 0f);
            }
        }

        //中心残留亮点：snap 后短促闪烁芯，强化"玩家于此重现"
        private void DrawCenterSpark(Texture2D pixel, Vector2 drawPos, float t) {
            if (t < 0.55f) return;

            float local = (t - 0.55f) / 0.45f;
            float fade = MathHelper.Clamp(1f - local * 1.4f, 0f, 1f);
            if (fade <= 0f) return;

            float size = MathHelper.Lerp(14f, 4f, local);
            Color c = new Color(1f, 0.9f, 0.6f) * fade * 0.7f;
            Vector2 origin = pixel.Size() * 0.5f;

            Main.spriteBatch.Draw(pixel, drawPos, null, c, 0f, origin,
                new Vector2(size, size), SpriteEffects.None, 0f);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}

