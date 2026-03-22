using CalamityOverhaul.Content.UIs.StorageUIs;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.Abysses.OldDukes.OldDuchests.OldDuchestUIs
{
    /// <summary>
    /// 老箱子UI渲染器 - 木质主题
    /// </summary>
    internal class OldDuchestRenderer : BaseChestRenderer
    {
        protected override int PanelWidth => 760;
        protected override int PanelHeight => 520;
        protected override int StorageStartX => 20;
        protected override int StorageStartY => 90;

        public OldDuchestRenderer(Player player, IChestStorage storage,
            OldDuchestAnimation animation, ChestInteraction interaction)
            : base(player, storage, animation, interaction) {
        }

        protected override string GetTitleText() => OldDuchestUI.TitleText.Value;
        protected override string GetStorageText() => OldDuchestUI.StorageText.Value;
        protected override Color GetFooterColor() => new Color(200, 150, 100);
        protected override Color GetSlotBackgroundColor() => new Color(40, 30, 20);
        protected override Color GetSlotHoverBackgroundColor() => new Color(80, 60, 40);
        protected override Color GetSlotBorderColor() => new Color(139, 87, 42);
        protected override Color GetSlotHoverBorderColor() => new Color(200, 150, 100);
        protected override Color GetCloseButtonHoverColor() => new Color(180, 80, 60);
        protected override Color GetCloseButtonNormalColor() => new Color(120, 80, 40);

        protected override void DrawMainPanel(SpriteBatch spriteBatch, Vector2 panelPosition) {
            Rectangle panelRect = new((int)panelPosition.X, (int)panelPosition.Y, PanelWidth, PanelHeight + 30);
            Texture2D pixel = VaultAsset.placeholder2.Value;

            spriteBatch.Draw(pixel, panelRect, new Rectangle(0, 0, 1, 1),
                new Color(20, 15, 10) * (animation.UIAlpha * 0.95f));

            Color borderColor = new Color(139, 87, 42) * animation.UIAlpha;
            DrawPanelBorder(spriteBatch, panelRect, borderColor, 2);
        }

        protected override void DrawHeader(SpriteBatch spriteBatch, Vector2 panelPosition) {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string title = GetTitleText();
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = panelPosition + new Vector2(PanelWidth / 2 - titleSize.X / 2 * 1.1f, 15);

            Utils.DrawBorderString(spriteBatch, title, titlePos, Color.White * animation.UIAlpha, 1.1f);
        }

        protected override void DrawHeaderDivider(SpriteBatch spriteBatch, Vector2 panelPosition) {
            Vector2 divStart = panelPosition + new Vector2(20, 55);
            Vector2 divEnd = divStart + new Vector2(PanelWidth - 40, 0);
            DrawLine(spriteBatch, divStart, divEnd, new Color(139, 87, 42) * animation.UIAlpha, 1.5f);
        }
    }
}
