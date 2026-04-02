using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel
{
    /// <summary>
    /// SHPC（Shepel）全身立绘演出
    /// </summary>
    internal class ShepelFullBodyPortrait : FullBodyPortraitBase
    {
        public override string PortraitKey => "ShepelFullBody";

        protected override float FadeInDuration => 20f;

        internal Face currentFace = Face.None;
        internal enum Face
        {
            None,
            Blank,
            Happy,
            Pain,
            Sad,
            Serious,
            Shocked,
            Sleep,
            Smirk,
        }

        protected override void OnInitialize() {
            scale = 1f;
        }

        protected override void OnUpdate() {
            scale = 1f;
            drawColor = Color.White;
        }

        protected override void OnDraw(SpriteBatch spriteBatch, float alpha) {
            Texture2D portrait = ADVAsset.Shepel;
            if (portrait == null || portrait.IsDisposed) {
                return;
            }

            Rectangle rectangle = new Rectangle(0, 0, portrait.Width, portrait.Height);
            position = OwnerDialogue.GetPanelRect().Top() + new Vector2(-160, -portrait.Height + 100) * scale;

            Color color = drawColor * alpha;
            spriteBatch.Draw(portrait, position, rectangle, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
            position.X += 18;

            if (currentFace is Face.None) {
                return;
            }

            Texture2D faceTexture = currentFace switch {
                Face.Blank => ADVAsset.Shepel_Blank,
                Face.Happy => ADVAsset.Shepel_Happy,
                Face.Pain => ADVAsset.Shepel_Pain,
                Face.Sad => ADVAsset.Shepel_Sad,
                Face.Serious => ADVAsset.Shepel_Serious,
                Face.Shocked => ADVAsset.Shepel_Shocked,
                Face.Sleep => ADVAsset.Shepel_Sleep,
                Face.Smirk => ADVAsset.Shepel_Smirk,
                _ => null
            };

            if (faceTexture is null) {
                return;
            }

            spriteBatch.Draw(faceTexture, position, null, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
