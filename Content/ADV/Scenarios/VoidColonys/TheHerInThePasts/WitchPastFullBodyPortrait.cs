using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TheHerInThePasts
{
    /// <summary>
    /// 过去的她——硫火女巫留影的全身立绘演出
    /// 淡入呈现为冷灰色雕像，逐步着色，最终像素剥落消散
    /// </summary>
    internal class WitchPastFullBodyPortrait : FullBodyPortraitBase
    {
        public override string PortraitKey => "WitchPast";

        protected override float FadeInDuration => 45f;
        protected override float FadeOutDuration => 60f;

        //着色进度0到1，越接近1越鲜活
        private float colorationT;
        //像素剥落进度
        private float dissolveT;
        //是否进入像素剥落阶段
        private bool dissolving;
        //内部计时
        private int localTimer;

        /// <summary>
        /// 当前着色进度
        /// </summary>
        public float Coloration => colorationT;

        /// <summary>
        /// 开始像素剥落消散
        /// </summary>
        public void StartPixelDissolve() {
            if (dissolving) return;
            dissolving = true;
            dissolveT = 0f;
            BlockDialogueClose = true;
            EnterCustomPhase();
        }

        /// <summary>
        /// 当前是否正在消散
        /// </summary>
        public bool IsDissolving => dissolving;

        /// <summary>
        /// 直接把着色推进到指定进度，用于随剧情推进由灰白复苏为鲜活
        /// </summary>
        public void SetColoration(float value) {
            colorationT = MathHelper.Clamp(value, 0f, 1f);
        }

        protected override void OnInitialize() {
            scale = 1.2f;
            colorationT = 0f;
            dissolveT = 0f;
            dissolving = false;
            localTimer = 0;
        }

        protected override void OnUpdate() {
            localTimer++;
            //淡入阶段随时间自然提升着色，但上限不超过外部设定值
            if (!dissolving && currentPhase != PerformancePhase.FadeOut) {
                //若外部尚未显式设置coloration，保持当前值
            }
        }

        protected override void OnCustomPhaseUpdate() {
            //剥落阶段：CurrentFade由本方法驱动，逐步归零
            localTimer++;
            dissolveT = MathF.Min(1f, dissolveT + 1f / 180f);
            CurrentFade = MathHelper.Clamp(1f - dissolveT, 0f, 1f);
            //剥落完成：退出自定义阶段并强制停用
            if (dissolveT >= 1f) {
                BlockDialogueClose = false;
                ForceDeactivate();
            }
        }

        protected override void OnDraw(SpriteBatch spriteBatch, float alpha) {
            Texture2D portrait = ADVAsset.WitchStatue;
            if (portrait == null || portrait.IsDisposed) return;

            Rectangle rectangle = new Rectangle(0, 0, portrait.Width, portrait.Height);
            position = OwnerDialogue.GetPanelRect().Top() + new Vector2(-160, -portrait.Height + 100) * scale;

            //灰白雕像底色，着色进度由外部驱动
            Color stone = new(180, 175, 170);
            Color vivid = Color.White;
            Color blended = Color.Lerp(stone, vivid, colorationT) * alpha;

            spriteBatch.Draw(portrait, position, rectangle, blended, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);

            //着色度越高越伴随一层微弱硫火内辉
            if (colorationT > 0.05f) {
                Color glow = new Color(220, 60, 40, 0) * alpha * colorationT * 0.4f;
                spriteBatch.Draw(portrait, position, rectangle, glow, rotation, Vector2.Zero, scale * 1.015f, SpriteEffects.None, 0f);
            }

            //剥落阶段覆盖像素方格
            if (dissolving && dissolveT > 0.01f) {
                DrawPixelDissolve(spriteBatch, portrait, position, alpha);
            }
        }

        private void DrawPixelDissolve(SpriteBatch spriteBatch, Texture2D portrait, Vector2 drawPos, float alpha) {
            Texture2D pixel = CWRAsset.Placeholder_White?.Value ?? TextureAssets.MagicPixel.Value;
            if (pixel == null) return;

            int blockSize = 6;
            int cols = portrait.Width / blockSize;
            int rows = portrait.Height / blockSize;
            float threshold = dissolveT;
            int seed = 9173;

            for (int y = 0; y < rows; y++) {
                float rowProgress = y / (float)rows;
                //下方像素更晚脱落
                float localT = MathHelper.Clamp((threshold - rowProgress) * 2f, 0f, 1f);
                if (localT <= 0f) continue;
                for (int x = 0; x < cols; x++) {
                    int hash = unchecked(seed + x * 73856093 + y * 19349663);
                    float rnd = (hash & 0xFFFF) / 65535f;
                    if (rnd > localT) continue;

                    Vector2 pos = drawPos + new Vector2(x * blockSize, y * blockSize) * scale;
                    Color cover = Color.Black * alpha * MathHelper.Clamp(localT * 1.3f, 0f, 1f);
                    spriteBatch.Draw(pixel, pos, null, cover, 0f, Vector2.Zero, blockSize * scale, SpriteEffects.None, 0f);

                    //向上飞散的像素残片
                    if (rnd < localT * 0.4f) {
                        float driftY = -MathF.Sin(localTimer * 0.03f + x * 0.4f) * 20f * localT;
                        float driftX = MathF.Cos(localTimer * 0.02f + y * 0.3f) * 5f * localT;
                        Vector2 shardPos = pos + new Vector2(driftX, driftY - localT * 36f);
                        Color shardColor = Color.Lerp(new Color(180, 175, 170), new Color(100, 30, 30), localT) * alpha * (1f - localT);
                        spriteBatch.Draw(pixel, shardPos, null, shardColor, 0f, Vector2.Zero, blockSize * scale * 0.8f, SpriteEffects.None, 0f);
                    }
                }
            }
        }
    }
}
