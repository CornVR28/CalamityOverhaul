using CalamityOverhaul.Common;
using CalamityOverhaul.Content.UIs.SupertableUIs;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    /// <summary>
    /// 天国极乐，与万魔殿相对的教皇权杖
    /// </summary>
    internal class Elysium : ModItem
    {
        #region 本地化
        //门徒名称
        public static LocalizedText[] DiscipleNameTexts { get; private set; }
        //骑士名称
        public static LocalizedText[] HorsemanNameTexts { get; private set; }
        //combat text
        public static LocalizedText DiscipleFullText { get; private set; }
        public static LocalizedText NoConvertTargetText { get; private set; }
        public static LocalizedText DiscipleFallbackText { get; private set; }
        public static LocalizedText RevelationText { get; private set; }
        public static LocalizedText RevelationEndText { get; private set; }
        public static LocalizedText CelestialMeteorText { get; private set; }
        public static LocalizedText SealStarsText { get; private set; }
        public static LocalizedText HorsemenFullText { get; private set; }
        public static LocalizedText HorsemanArrivalText { get; private set; }
        public static LocalizedText DiscipleJoinedText { get; private set; }
        public static LocalizedText JudasBetrayalText { get; private set; }
        public static LocalizedText JudasDeathReasonText { get; private set; }
        public static LocalizedText DiscipleMartyrText { get; private set; }
        public static LocalizedText MartyrdomPowerText { get; private set; }
        //tooltip
        public static LocalizedText Tooltip_DiscipleCountText { get; private set; }
        public static LocalizedText Tooltip_NoDisciplesText { get; private set; }
        public static LocalizedText Tooltip_MartyrdomPowerText { get; private set; }
        public static LocalizedText Tooltip_RevelationReadyText { get; private set; }
        public static LocalizedText Tooltip_JudgmentActiveText { get; private set; }
        public static LocalizedText Tooltip_RevelationActiveText { get; private set; }
        public static LocalizedText Tooltip_JudasWarningText { get; private set; }
        #endregion

        //合成配方材料，与万魔殿同级
        public readonly static string[] FullItems = ["0", "0", "0", "0", "CalamityMod/AshesofAnnihilation", "0", "0", "0", "0",
            "0", "0", "0", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "0", "0", "0",
            "0", "0", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "0", "0",
            "0", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/DivineGeode", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "0",
            "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/Rock", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation",
            "0", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/Apotheosis", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "0",
            "0", "0", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "0", "0",
            "0", "0", "0", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "CalamityMod/AshesofAnnihilation", "0", "0", "0",
            "0", "0", "0", "0", "CalamityMod/AshesofAnnihilation", "0", "0", "0", "0",
            "CalamityOverhaul/Elysium"
        ];

        public override bool IsLoadingEnabled(Mod mod) => true;

        public override void SetStaticDefaults() {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
            SupertableUI.ModCall_OtherRpsData_StringList.Add(FullItems);

            DiscipleNameTexts = new LocalizedText[12];
            string[] defaultNames = ["西门彼得", "圣安德鲁", "雅各布", "圣约翰", "腓力", "巴多罗买", "多马", "圣马修", "雅各", "达泰", "西门", "犹大"];
            for (int i = 0; i < 12; i++) {
                string name = defaultNames[i];
                DiscipleNameTexts[i] = this.GetLocalization($"DiscipleName_{i}", () => name);
            }

            HorsemanNameTexts = new LocalizedText[4];
            string[] defaultHorsemen = ["瘟疫", "战争", "饥荒", "死亡"];
            for (int i = 0; i < 4; i++) {
                string hname = defaultHorsemen[i];
                HorsemanNameTexts[i] = this.GetLocalization($"HorsemanName_{i}", () => hname);
            }

            DiscipleFullText = this.GetLocalization(nameof(DiscipleFullText), () => "门徒已满");
            NoConvertTargetText = this.GetLocalization(nameof(NoConvertTargetText), () => "附近没有可转化的居民");
            DiscipleFallbackText = this.GetLocalization(nameof(DiscipleFallbackText), () => "门徒");
            RevelationText = this.GetLocalization(nameof(RevelationText), () => "启示录");
            RevelationEndText = this.GetLocalization(nameof(RevelationEndText), () => "启示录终止");
            CelestialMeteorText = this.GetLocalization(nameof(CelestialMeteorText), () => "天体陨石");
            SealStarsText = this.GetLocalization(nameof(SealStarsText), () => "第五星 第六星 第七星");
            HorsemenFullText = this.GetLocalization(nameof(HorsemenFullText), () => "四骑士已齐聚");
            HorsemanArrivalText = this.GetLocalization(nameof(HorsemanArrivalText), () => "{0}骑士降临");
            DiscipleJoinedText = this.GetLocalization(nameof(DiscipleJoinedText), () => "{0} 已加入");
            JudasBetrayalText = this.GetLocalization(nameof(JudasBetrayalText), () => "犹大的背叛!");
            JudasDeathReasonText = this.GetLocalization(nameof(JudasDeathReasonText), () => "{0} 被犹大背叛了");
            DiscipleMartyrText = this.GetLocalization(nameof(DiscipleMartyrText), () => "{0} 殉道了");
            MartyrdomPowerText = this.GetLocalization(nameof(MartyrdomPowerText), () => "殉道之力 {0}/11");

            Tooltip_DiscipleCountText = this.GetLocalization(nameof(Tooltip_DiscipleCountText), () => "当前门徒: {0}/12");
            Tooltip_NoDisciplesText = this.GetLocalization(nameof(Tooltip_NoDisciplesText), () => "尚无门徒追随");
            Tooltip_MartyrdomPowerText = this.GetLocalization(nameof(Tooltip_MartyrdomPowerText), () => "殉道之力: {0} {1}/11");
            Tooltip_RevelationReadyText = this.GetLocalization(nameof(Tooltip_RevelationReadyText), () => "按Q键 — 约翰将殉道，启示录将降临");
            Tooltip_JudgmentActiveText = this.GetLocalization(nameof(Tooltip_JudgmentActiveText), () => "后三印审判进行中，等待终结收束");
            Tooltip_RevelationActiveText = this.GetLocalization(nameof(Tooltip_RevelationActiveText), () => "启示录已降临 Q降天体陨石 R发动后三印审判 右键召唤四骑士 {0}/4");
            Tooltip_JudasWarningText = this.GetLocalization(nameof(Tooltip_JudasWarningText), () => "警告: 犹大的背叛已潜伏于你的身边");
        }

        public override void SetDefaults() {
            Item.damage = 320;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 20;
            Item.width = 50;
            Item.height = 50;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 6;
            Item.value = Item.sellPrice(platinum: 10);
            Item.rare = CWRID.Rarity_BurnishedAuric;
            Item.UseSound = SoundID.Item117;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ElysiumHeld>();
            Item.shootSpeed = 12f;
            Item.channel = true;
            Item.CWR().OmigaSnyContent = FullItems;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override void HoldItem(Player player) {
            if (!player.TryGetModPlayer<ElysiumPlayer>(out var ep) || !ep.HasElysiumInInventory()) {
                return;
            }

            if (ep.IsRevelationActive && CWRKeySystem.WeponSkill_R.JustPressed) {
                ep.TriggerSealJudgment(player);
                return;
            }

            if (!CWRKeySystem.WeponSkill_Q.JustPressed) {
                return;
            }

            if (ep.IsRevelationActive) {
                ep.CastRevelationMeteor(player);
                return;
            }

            //满足殉道条件时按Q进入启示录
            if (ep.GetMartyrdomEnergy() >= 11
                && !ep.Martyred[3]
                && ep.HasDiscipleOfType(ModContent.ProjectileType<John>())
                && player.CountProjectilesOfID<RevelationDomain>() == 0) {
                ep.ActivateRevelation(player);
            }
            else {
                //未进入启示录时按Q召唤天雷
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<DivineThunderStrike>(),
                    (int)(Item.damage * 1.5f),
                    Item.knockBack,
                    player.whoAmI
                );
            }
        }

        public override bool CanUseItem(Player player) {
            player.TryGetModPlayer<ElysiumPlayer>(out var ep);

            if (player.altFunctionUse == 2) {
                if (ep != null && ep.IsRevelationActive) {
                    //右键：启示录期间召唤四骑士
                    Item.mana = 15;
                    Item.useTime = Item.useAnimation = 18;
                    Item.channel = false;
                    return !ep.IsSealJudgmentActive && ep.GetHorsemanCount() < 4;
                }

                //右键：转化NPC为门徒
                Item.mana = 50;
                Item.useTime = Item.useAnimation = 30;
                Item.channel = false;
                return true;
            }
            else {
                //左键：化蛇术
                Item.mana = 20;
                Item.useTime = Item.useAnimation = 25;
                Item.channel = true;
                return player.ownedProjectileCounts[ModContent.ProjectileType<ElysiumHeld>()] == 0;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            tooltips.InsertHotkeyBinding(CWRKeySystem.WeponSkill_Q, "ElysiumQKey", CWRLocText.Instance.Notbound.Value);
            tooltips.InsertHotkeyBinding(CWRKeySystem.WeponSkill_R, "ElysiumRKey", CWRLocText.Instance.Notbound.Value);
            //显示当前门徒数量和殉道能量
            if (Main.LocalPlayer.TryGetModPlayer<ElysiumPlayer>(out var ep)) {
                int count = ep.GetDiscipleCount();
                string discipleInfo = count > 0
                    ? Tooltip_DiscipleCountText.Format(count)
                    : Tooltip_NoDisciplesText.Value;
                tooltips.Add(new TooltipLine(Mod, "DiscipleCount", discipleInfo));

                int energy = ep.GetMartyrdomEnergy();
                if (energy > 0) {
                    string energyBar = "";
                    for (int i = 0; i < 11; i++) {
                        energyBar += i < energy ? "█" : "░";
                    }
                    tooltips.Add(new TooltipLine(Mod, "MartyrdomEnergy",
                        $"[c/FFD700:{Tooltip_MartyrdomPowerText.Format(energyBar, energy)}]"));
                }

                if (energy >= 11 && !ep.IsRevelationActive) {
                    tooltips.Add(new TooltipLine(Mod, "RevelationReady",
                        $"[c/FFFFFF:{Tooltip_RevelationReadyText.Value}]"));
                }
                else if (ep.IsRevelationActive) {
                    int horsemen = ep.GetHorsemanCount();
                    if (ep.IsSealJudgmentActive) {
                        tooltips.Add(new TooltipLine(Mod, "RevelationJudgment",
                            $"[c/FFAA55:{Tooltip_JudgmentActiveText.Value}]"));
                    }
                    else {
                        tooltips.Add(new TooltipLine(Mod, "RevelationActive",
                            $"[c/FFD700:{Tooltip_RevelationActiveText.Format(horsemen)}]"));
                    }
                }

                if (count == 12) {
                    tooltips.Add(new TooltipLine(Mod, "JudasWarning",
                        $"[c/FF4444:{Tooltip_JudasWarningText.Value}]"));
                }
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            if (player.altFunctionUse == 2) {
                if (player.TryGetModPlayer<ElysiumPlayer>(out var ep)) {
                    if (ep.IsRevelationActive) {
                        ep.SummonNextHorseman(player);
                    }
                    else {
                        //右键：尝试转化最近的NPC为门徒
                        ep.TryConvertNearestNPC(player);
                    }
                }
                return false;
            }
            else {
                //左键：生成手持权杖弹幕
                Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
                return false;
            }
        }
    }
}
