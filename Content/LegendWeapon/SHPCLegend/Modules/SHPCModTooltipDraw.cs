using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    internal class SHPCModTooltipDraw : GlobalItem, ILocalizedModType
    {
        public string LocalizationCategory => "Items";

        private const int IconSize = 16;
        private const int IconGap = 3;

        public static LocalizedText InstalledHeader { get; private set; }
        public static LocalizedText NoModules { get; private set; }
        public static LocalizedText BonusHeader { get; private set; }

        public override void SetStaticDefaults() {
            InstalledHeader = this.GetLocalization(nameof(InstalledHeader));
            NoModules = this.GetLocalization(nameof(NoModules));
            BonusHeader = this.GetLocalization(nameof(BonusHeader));
        }

        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset) {
            if (item.type != CWRID.Item_SHPC) return true;
            if (line.Mod != CWRMod.Instance.Name) return true;
            if (line.Name != "SHPCModuleRow") return true;

            Player player = Main.LocalPlayer;
            SHPCPlayer sp = player.GetModPlayer<SHPCPlayer>();
            SpriteBatch sb = Main.spriteBatch;

            Color headerColor = line.OverrideColor ?? Color.White;
            ChatManager.DrawColorCodedStringWithShadow(sb, line.Font, line.Text, new Vector2(line.X, line.Y),
                headerColor, line.Rotation, line.Origin, line.BaseScale, line.MaxWidth, line.Spread);

            Vector2 textSize = line.Font.MeasureString(line.Text) * line.BaseScale;
            float iconX = line.X + textSize.X + IconGap * 2;
            float iconCenterY = line.Y + textSize.Y * 0.5f;

            for (int i = 0; i < SHPCData.SlotCount; i++) {
                Item m = sp.GetModule(i);
                Vector2 center = new(iconX + IconSize * 0.5f, iconCenterY);
                if (m != null && !m.IsAir && m.ModItem is SHPCModuleItem mod) {
                    SHPCModuleRender.DrawIcon(sb, m, center, IconSize, mod.TintColor, 1f, Main.UIScaleMatrix, mod.TintIntensity);
                }
                else {
                    sb.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)(center.X - IconSize * 0.5f), (int)(center.Y - IconSize * 0.5f), IconSize, IconSize),
                        Color.Gray * 0.2f);
                }
                iconX += IconSize + IconGap;
            }

            return false;
        }
    }
}
