using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors
{
    /// <summary>
    /// 阿波利娅全身立绘演出
    /// </summary>
    internal class ApolliaFullBodyPortrait : FullBodyPortraitBase
    {
        public override string PortraitKey => "ApolliaFullBody";

        protected override float FadeInDuration => 90f;
        protected override float FadeSpeed => 0.06f;

        protected override void OnInitialize() {
            scale = 1f;
        }

        protected override void OnUpdate() {
            scale = 1f;
            drawColor = Color.White;
        }

        protected override void OnDraw(SpriteBatch spriteBatch, float alpha) {
            Texture2D portrait = ADVAsset.Apollia;
            if (portrait == null || portrait.IsDisposed) {
                return;
            }

            position = ownerDialogue.GetPanelRect().Top()
                + new Vector2(0, -portrait.Height + 120) * scale;

            Color color = drawColor * alpha;
            spriteBatch.Draw(portrait, position, null, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
