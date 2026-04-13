using InnoVault.UIHandles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    /// <summary>
    /// 殉道之力能量条HUD — 显示在屏幕下方
    /// 12个门徒槽位，殉道的显示为金色十字，约翰特殊标记
    /// </summary>
    internal class MartyrdomBarUI : UIHandle, ILocalizedModType
    {
        //布局参数
        private const float SlotSize = 22f;
        private const float SlotSpacing = 4f;
        private const float BarHeight = 30f;
        private const float BarPaddingX = 8f;
        private const float BarPaddingY = 6f;

        //动画
        private float uiFadeAlpha;
        private float pulseTimer;
        private float revelationFlash;

        public string LocalizationCategory => "UI";

        //门徒名称缩写本地化
        public static LocalizedText[] SlotLabelTexts { get; private set; }
        public static LocalizedText RevelationJudgingText { get; private set; }
        public static LocalizedText RevelationStatusText { get; private set; }
        public static LocalizedText RevelationReadyText { get; private set; }
        public static LocalizedText MartyrdomPowerText { get; private set; }

        public override void SetStaticDefaults() {
            SlotLabelTexts = new LocalizedText[12];
            string[] defaults = ["彼", "安", "雅", "约", "腓", "巴", "多", "马", "小", "达", "西", "犹"];
            for (int i = 0; i < 12; i++) {
                string label = defaults[i];
                SlotLabelTexts[i] = this.GetLocalization($"SlotLabel_{i}", () => label);
            }
            RevelationJudgingText = this.GetLocalization(nameof(RevelationJudgingText), () => "启示录 后三印审判进行中");
            RevelationStatusText = this.GetLocalization(nameof(RevelationStatusText), () => "启示录 四骑士 {0}/4 Q陨石 R审判");
            RevelationReadyText = this.GetLocalization(nameof(RevelationReadyText), () => "按 Q — 启示录就绪");
            MartyrdomPowerText = this.GetLocalization(nameof(MartyrdomPowerText), () => "殉道之力 {0}/11");
        }

        //门徒名称缩写（对应12槽位）
        private static string GetSlotLabel(int index) => SlotLabelTexts[index].Value;

        //门徒代表色
        private static readonly Color[] DiscipleColors = [
            new Color(180, 180, 220), //彼得-磐石灰蓝
            new Color(100, 180, 220), //安德鲁-渔网蓝
            new Color(255, 220, 80),  //雅各布-雷霆金
            new Color(200, 200, 255), //约翰-启示白蓝
            new Color(220, 200, 140), //腓力-引导暖白
            new Color(180, 220, 180), //巴多罗买-真言绿
            new Color(180, 160, 200), //多马-怀疑紫
            new Color(220, 200, 100), //马太-财富金
            new Color(200, 200, 200), //小雅各-银白
            new Color(180, 140, 200), //达泰-奇迹紫
            new Color(220, 120, 100), //西门-狂热红
            new Color(100, 60, 60),   //犹大-暗红
        ];

        public override bool Active {
            get {
                //启示录期间强制显示；非启示录时按原能量逻辑显示
                if (!Main.LocalPlayer.active || Main.LocalPlayer.dead) return false;
                if (!Main.LocalPlayer.TryGetModPlayer<ElysiumPlayer>(out var ep)) return false;
                bool hasElysium = ep.HasElysiumInInventory();
                bool hasEnergy = ep.GetMartyrdomEnergy() > 0 || ep.IsRevelationActive;
                return (hasElysium && hasEnergy) || uiFadeAlpha > 0.01f;
            }
        }

        public override void Update() {
            if (!Main.LocalPlayer.TryGetModPlayer<ElysiumPlayer>(out var ep)) return;

            bool heldElysium = Main.LocalPlayer.HeldItem?.type == ModContent.ItemType<Elysium>();
            bool shouldShow = ep.IsRevelationActive || (heldElysium && ep.GetMartyrdomEnergy() > 0);

            if (shouldShow) {
                uiFadeAlpha = Math.Min(1f, uiFadeAlpha + 0.06f);
            }
            else {
                uiFadeAlpha = Math.Max(0f, uiFadeAlpha - 0.04f);
            }

            pulseTimer += 0.04f;
            if (pulseTimer > MathHelper.TwoPi) pulseTimer -= MathHelper.TwoPi;

            //启示录闪烁
            if (ep.IsRevelationActive) {
                revelationFlash += 0.08f;
                if (revelationFlash > MathHelper.TwoPi) revelationFlash -= MathHelper.TwoPi;
            }
            else {
                revelationFlash = 0f;
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (uiFadeAlpha < 0.01f) return;
            if (!Main.LocalPlayer.TryGetModPlayer<ElysiumPlayer>(out var ep)) return;

            float alpha = uiFadeAlpha;

            //计算总宽度
            float totalWidth = 12 * SlotSize + 11 * SlotSpacing + BarPaddingX * 2;
            float totalHeight = BarHeight + BarPaddingY * 2;

            //位置：屏幕底部中央，生命条上方
            Vector2 barPos = new Vector2(
                Main.screenWidth / 2f - totalWidth / 2f,
                Main.screenHeight - 100f
            );

            //绘制背景面板
            DrawBarBackground(spriteBatch, barPos, totalWidth, totalHeight, alpha, ep);

            //绘制12个槽位
            for (int i = 0; i < 12; i++) {
                Vector2 slotPos = new Vector2(
                    barPos.X + BarPaddingX + i * (SlotSize + SlotSpacing),
                    barPos.Y + BarPaddingY
                );

                DrawSlot(spriteBatch, slotPos, i, ep, alpha);
            }

            //绘制能量文字
            int energy = ep.GetMartyrdomEnergy();
            string text;
            Color textColor;
            if (ep.IsRevelationActive) {
                int horsemanCount = ep.GetHorsemanCount();
                text = ep.IsSealJudgmentActive
                    ? RevelationJudgingText.Value
                    : RevelationStatusText.Format(horsemanCount);
                float flash = (float)Math.Sin(revelationFlash) * 0.3f + 0.7f;
                textColor = Color.Lerp(Color.Gold, Color.White, flash);
            }
            else if (energy >= 11) {
                text = RevelationReadyText.Value;
                float blink = (float)Math.Sin(pulseTimer * 2f) * 0.3f + 0.7f;
                textColor = Color.Gold * blink;
            }
            else {
                text = MartyrdomPowerText.Format(energy);
                textColor = Color.Lerp(Color.Gray, Color.Gold, energy / 11f);
            }

            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * 0.7f;
            Vector2 textPos = new Vector2(
                barPos.X + totalWidth / 2f - textSize.X / 2f,
                barPos.Y - 18f
            );

            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value,
                text, textPos, textColor * alpha, 0f, Vector2.Zero, Vector2.One * 0.7f);
        }

        private void DrawBarBackground(SpriteBatch sb, Vector2 pos, float width, float height, float alpha, ElysiumPlayer ep) {
            Texture2D pixel = CWRAsset.Placeholder_White.Value;
            if (pixel == null) return;

            //暗色半透明背景
            Color bgColor = new Color(10, 8, 15, 180) * alpha;
            sb.Draw(pixel, new Rectangle((int)pos.X - 2, (int)pos.Y - 2, (int)width + 4, (int)height + 4),
                null, bgColor, 0f, Vector2.Zero, SpriteEffects.None, 0f);

            //金色边框
            Color borderColor = ep.IsRevelationActive
                ? Color.Lerp(Color.Gold, Color.White, (float)Math.Sin(revelationFlash) * 0.3f + 0.3f)
                : Color.Lerp(new Color(80, 60, 20), Color.Gold, ep.GetMartyrdomEnergy() / 11f);
            borderColor *= alpha;

            int borderThick = 1;
            //上
            sb.Draw(pixel, new Rectangle((int)pos.X - 2, (int)pos.Y - 2, (int)width + 4, borderThick), borderColor);
            //下
            sb.Draw(pixel, new Rectangle((int)pos.X - 2, (int)(pos.Y + height + 1), (int)width + 4, borderThick), borderColor);
            //左
            sb.Draw(pixel, new Rectangle((int)pos.X - 2, (int)pos.Y - 2, borderThick, (int)height + 4), borderColor);
            //右
            sb.Draw(pixel, new Rectangle((int)(pos.X + width + 1), (int)pos.Y - 2, borderThick, (int)height + 4), borderColor);
        }

        private void DrawSlot(SpriteBatch sb, Vector2 pos, int index, ElysiumPlayer ep, float alpha) {
            Texture2D pixel = CWRAsset.Placeholder_White.Value;
            if (pixel == null) return;

            bool martyred = ep.Martyred != null && ep.Martyred[index];
            bool isJohn = index == 3;
            bool alive = ep.HasDiscipleOfType(ElysiumPlayer.DiscipleTypes[index]);

            Color slotBg;
            Color labelColor;

            if (ep.IsRevelationActive && martyred) {
                //启示录期间殉道者闪耀
                float flash = (float)Math.Sin(revelationFlash + index * 0.5f) * 0.2f + 0.8f;
                slotBg = Color.Lerp(Color.Gold, Color.White, flash * 0.3f) * 0.6f;
                labelColor = Color.White;
            }
            else if (martyred) {
                //殉道者：金色十字标记
                float pulse = (float)Math.Sin(pulseTimer + index * 0.3f) * 0.15f + 0.85f;
                slotBg = new Color(180, 150, 50) * 0.5f * pulse;
                labelColor = Color.Gold;
            }
            else if (alive) {
                //存活门徒：门徒代表色
                slotBg = DiscipleColors[index] * 0.25f;
                labelColor = DiscipleColors[index] * 0.8f;
            }
            else {
                //空位
                slotBg = new Color(30, 25, 35) * 0.5f;
                labelColor = new Color(60, 50, 70);
            }

            slotBg *= alpha;
            labelColor *= alpha;

            //槽位背景
            sb.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y, (int)SlotSize, (int)BarHeight),
                null, slotBg, 0f, Vector2.Zero, SpriteEffects.None, 0f);

            //约翰特殊框(蓝白边框)
            if (isJohn) {
                Color johnBorder = martyred ? Color.Gold : new Color(200, 200, 255);
                johnBorder *= alpha * 0.7f;
                int jb = 1;
                sb.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y, (int)SlotSize, jb), johnBorder);
                sb.Draw(pixel, new Rectangle((int)pos.X, (int)(pos.Y + BarHeight - jb), (int)SlotSize, jb), johnBorder);
                sb.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y, jb, (int)BarHeight), johnBorder);
                sb.Draw(pixel, new Rectangle((int)(pos.X + SlotSize - jb), (int)pos.Y, jb, (int)BarHeight), johnBorder);
            }

            //殉道十字标记
            if (martyred) {
                Color crossColor = Color.Gold * alpha * 0.9f;
                float cx = pos.X + SlotSize / 2f;
                float cy = pos.Y + BarHeight / 2f;
                float crossLen = SlotSize * 0.35f;
                float crossThick = 2f;
                //竖
                sb.Draw(pixel, new Rectangle((int)(cx - crossThick / 2f), (int)(cy - crossLen), (int)crossThick, (int)(crossLen * 2f)), crossColor);
                //横(偏上)
                sb.Draw(pixel, new Rectangle((int)(cx - crossLen * 0.7f), (int)(cy - crossLen * 0.4f), (int)(crossLen * 1.4f), (int)crossThick), crossColor);
            }

            //门徒名称缩写
            Vector2 labelSize = FontAssets.MouseText.Value.MeasureString(GetSlotLabel(index)) * 0.55f;
            Vector2 labelPos = new Vector2(
                pos.X + SlotSize / 2f - labelSize.X / 2f,
                pos.Y + BarHeight - labelSize.Y - 2f
            );

            ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value,
                GetSlotLabel(index), labelPos, labelColor, 0f, Vector2.Zero, Vector2.One * 0.55f);
        }
    }
}
