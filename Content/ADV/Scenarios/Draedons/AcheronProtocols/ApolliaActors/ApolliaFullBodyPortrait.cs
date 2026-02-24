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

        protected override float FadeInDuration => 20f;

        internal Face currentFace = Face.None;
        internal enum Face
        {
            None,
            Calmnessl,
            Feel,
            Rage,
            Worry,
        }

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

            int offsetY = 100;
            Rectangle rectangle = new Rectangle(0, 0, portrait.Width, portrait.Height - offsetY);
            position = OwnerDialogue.GetPanelRect().Top()
                + new Vector2(0, -portrait.Height + 120 + offsetY) * scale;

            Color color = drawColor * alpha;
            spriteBatch.Draw(portrait, position, rectangle, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
            position.X += 148;

            if (currentFace is Face.None) {
                return;
            }

            Texture2D faceTexture = currentFace switch {
                Face.Calmnessl => ADVAsset.Apollia_Calmnessl,
                Face.Feel => ADVAsset.Apollia_Feel,
                Face.Rage => ADVAsset.Apollia_Rage,
                Face.Worry => ADVAsset.Apollia_Worry,
                _ => null
            };

            if (faceTexture is null) {
                return;
            }

            spriteBatch.Draw(faceTexture, position, null, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
