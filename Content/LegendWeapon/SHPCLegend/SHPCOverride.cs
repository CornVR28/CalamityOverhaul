using CalamityOverhaul.Common;
using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces;
using InnoVault.GameSystem;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityOverhaul.Content.InWorldBossPhase;
using static InnoVault.GameSystem.ItemRebuildLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend
{
    internal class SHPCOverride : ItemOverride, ICWRLoader
    {
        /// <summary>
        /// 每个时期阶段对应的伤害，这个成员一般不需要直接访问，而是使用<see cref="GetOnDamage"/>
        /// </summary>
        private static Dictionary<int, int> DamageDictionary = new Dictionary<int, int>();
        /// <summary>
        /// 获取开局的伤害
        /// </summary>
        public static int GetStartDamage => DamageDictionary[0];
        /// <summary>
        /// 当前选中的魂魄类型，UI选择后会更新这个值
        /// </summary>
        public static int SelectedSoulType = ItemID.SoulofLight;
        /// <summary>
        /// 左键连发间隔帧数
        /// </summary>
        private const int LeftClickUseTime = 20;
        /// <summary>
        /// 左键每次发射的光束数量
        /// </summary>
        private const int BeamCount = 3;
        /// <summary>
        /// 左键散射角度（弧度）
        /// </summary>
        private const float BeamSpreadAngle = 0.08f;

        public override int TargetID => CWRID.Item_SHPC;

        #region 原版方法屏蔽

        private static void OnSHPCToolFunc(On_ModItem_ModifyTooltips_Delegate orig, object obj, List<TooltipLine> list) { }

        private static bool OnSHPCCanUseItemFunc(Func<object, Player, bool> orig, object self, Player player) => true;

        private static bool? OnSHPCUseItemFunc(Func<object, Player, bool?> orig, object self, Player player) => null;

        private static bool OnSHPCShootFunc(
            Func<object, Player, EntitySource_ItemUse_WithAmmo, Vector2, Vector2, int, int, float, bool> orig,
            object self, Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback) => false;

        private static float OnSHPCUseSpeedMultiplierFunc(Func<object, Player, float> orig, object self, Player player) => 1f;

        private delegate void OnSHPC_ModifyManaCost_Delegate(object self, Player player, ref float reduce, ref float mult);
        private static void OnSHPCModifyManaCostFunc(
            OnSHPC_ModifyManaCost_Delegate orig,
            object self, Player player, ref float reduce, ref float mult) { }

        private delegate void OnSHPC_PostDrawInInventory_Delegate(object self, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale);

        private static void OnPostDrawInInventoryFunc(OnSHPC_PostDrawInInventory_Delegate orig, object self, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) { }

        #endregion

        void ICWRLoader.LoadData() {
            var type = CWRRef.GetItem_SHPC_Type();
            if (type != null) {
                // 屏蔽原版 ModifyTooltips
                MethodInfo methodInfo = type.GetMethod("ModifyTooltips", BindingFlags.Public | BindingFlags.Instance);
                if (methodInfo != null) {
                    VaultHook.Add(methodInfo, OnSHPCToolFunc);
                }
                // 屏蔽原版 FindSoulForAmmo
                methodInfo = type.GetMethod("FindSoulForAmmo", BindingFlags.Public | BindingFlags.Static);
                if (methodInfo != null) {
                    VaultHook.Add(methodInfo, OnFindSoulForAmmoFunc);
                }
                // 屏蔽原版 Shoot —— 阻止原始弹幕生成
                methodInfo = type.GetMethod("Shoot", BindingFlags.Public | BindingFlags.Instance);
                if (methodInfo != null) {
                    VaultHook.Add(methodInfo, OnSHPCShootFunc);
                }
                // 屏蔽原版 CanUseItem —— 移除灵魂弹药检测
                methodInfo = type.GetMethod("CanUseItem", BindingFlags.Public | BindingFlags.Instance);
                if (methodInfo != null) {
                    VaultHook.Add(methodInfo, OnSHPCCanUseItemFunc);
                }
                // 屏蔽原版 UseItem —— 移除灵魂消耗逻辑
                methodInfo = type.GetMethod("UseItem", BindingFlags.Public | BindingFlags.Instance);
                if (methodInfo != null) {
                    VaultHook.Add(methodInfo, OnSHPCUseItemFunc);
                }
                // 屏蔽原版 UseSpeedMultiplier
                methodInfo = type.GetMethod("UseSpeedMultiplier", BindingFlags.Public | BindingFlags.Instance);
                if (methodInfo != null) {
                    VaultHook.Add(methodInfo, OnSHPCUseSpeedMultiplierFunc);
                }
                // 屏蔽原版 ModifyManaCost
                methodInfo = type.GetMethod("ModifyManaCost", BindingFlags.Public | BindingFlags.Instance);
                if (methodInfo != null) {
                    VaultHook.Add(methodInfo, OnSHPCModifyManaCostFunc);
                }

                methodInfo = type.GetMethod("PostDrawInInventory", BindingFlags.Public | BindingFlags.Instance);
                if (methodInfo != null) {
                    VaultHook.Add(methodInfo, OnPostDrawInInventoryFunc);
                }
            }
        }

        private static int OnFindSoulForAmmoFunc(Func<Player, int> orig, Player player) {
            return SelectedSoulType;
        }

        /// <summary>
        /// 获得成长等级
        /// </summary>
        public static int GetLevel(Item item) {
            if (item.type != CWRID.Item_SHPC) {
                return 0;
            }
            CWRItem cwrItem = item.CWR();
            if (cwrItem == null) {
                return 0;
            }
            if (cwrItem.LegendData == null) {
                return 0;
            }

            return cwrItem.LegendData.Level;
        }

        /// <summary>
        /// 获取时期对应的伤害
        /// </summary>
        public static int GetOnDamage(Item item) => DamageDictionary[GetLevel(item)];

        public static void LoadWeaponData() {
            DamageDictionary = new Dictionary<int, int>(){
                {0, 8 },
                {1, 10 },
                {2, 13 },
                {3, 17 },
                {4, 24 },
                {5, 48 },
                {6, 55 },
                {7, 66 },
                {8, 88 },
                {9, 110 },
                {10, 140 },
                {11, 260 },
                {12, 380 },
                {13, 500 },
                {14, 600 },
                {15, 700 },
                {16, 800 },
                {17, 900 },
                {18, 1000 },
                {19, 1100 },
                {20, 1200 },
                {21, 6666 }
            };
        }

        public override void SetStaticDefaults() => ItemID.Sets.ShimmerTransformToItem[TargetID] = CWRID.Item_PlasmaDriveCore;

        public override void SetDefaults(Item item) => SetDefaultsFunc(item);

        public override bool On_ModifyWeaponDamage(Item item, Player player, ref StatModifier damage) => SHPCDamage(item, ref damage);

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) => SetTooltip(item, ref tooltips);

        /// <summary>
        /// 允许右键使用（蓄力能量球）
        /// </summary>
        public override bool? On_AltFunctionUse(Item item, Player player) => true;

        /// <summary>
        /// 拦截原版 CanUseItem，移除灵魂弹药需求
        /// <br/>右键时：场上不能有已存在的蓄力球
        /// <br/>左键时：正常使用
        /// </summary>
        public override bool? On_CanUseItem(Item item, Player player) {
            if (player.altFunctionUse == 2) {
                // 右键蓄力模式：channel + noUseGraphic，且场上没有同类蓄力弹幕
                item.channel = true;
                item.noUseGraphic = true;
                item.UseSound = null;
                item.useAnimation = item.useTime = 10;
                return player.ownedProjectileCounts[ModContent.ProjectileType<SHPCChargeHeldProj>()] <= 0;
            }
            else {
                // 左键射击模式
                item.channel = false;
                item.noUseGraphic = false;
                item.UseSound = null;
                item.useAnimation = item.useTime = LeftClickUseTime;
                return true;
            }
        }

        /// <summary>
        /// 拦截原版 UseItem，阻止灵魂消耗
        /// </summary>
        public override bool? On_UseItem(Item item, Player player) => true;

        /// <summary>
        /// 右键蓄力不消耗法力
        /// </summary>
        public override void ModifyManaCost(Item item, Player player, ref float reduce, ref float mult) {
            if (player.altFunctionUse == 2) {
                mult *= 0f;
            }
        }

        /// <summary>
        /// 拦截原版射击，实现自定义左右键弹幕
        /// <br/>左键：发射三发 CyberTraceBeamProj
        /// <br/>右键：发射一发 CyberChargeOrbProj（蓄力能量球）
        /// </summary>
        public override bool? On_Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            if (player.altFunctionUse == 2) {
                // 右键：先生成手持弹幕（绘制武器 + 控制手臂动画）
                int heldIdx = Projectile.NewProjectile(source, player.Center, Vector2.Zero,
                    ModContent.ProjectileType<SHPCChargeHeldProj>(),
                    0, 0f, player.whoAmI);

                // 再生成蓄力能量球，ai[1] 传递手持弹幕索引以定位枪口
                Vector2 spawnPos = player.Center + velocity.SafeNormalize(Vector2.UnitX) * 70f;
                Projectile.NewProjectile(source, spawnPos, Vector2.Zero,
                    ModContent.ProjectileType<CyberChargeOrbProj>(),
                    damage * 3, knockback, player.whoAmI,
                    ai1: heldIdx);
            }
            else {
                // 左键：发射三发追踪光束，带小幅散射
                SoundEngine.PlaySound(SoundID.Item92, player.Center);
                Vector2 baseVel = velocity.SafeNormalize(Vector2.UnitX) * 14f;
                Vector2 dir = velocity.UnitVector();
                position += new Vector2(dir.X * 20, -8);
                for (int i = 0; i < BeamCount; i++) {
                    // 均匀散射 + 少量随机偏移
                    float spreadOffset = (i - (BeamCount - 1) / 2f) * BeamSpreadAngle;
                    float randomOffset = Main.rand.NextFloat(-0.03f, 0.03f);
                    Vector2 shotVel = baseVel.RotatedBy(spreadOffset + randomOffset);

                    // ai[0] 传递颜色主题索引（0=蓝, 1=黄, 2=青）
                    Projectile.NewProjectile(source, position + shotVel * 2f, shotVel,
                        ModContent.ProjectileType<CyberTraceBeamProj>(),
                        damage, knockback, player.whoAmI,
                        ai0: Main.rand.Next(3));
                }
            }

            return false; // 阻止原版射击行为
        }

        public static void SetDefaultsFunc(Item Item) {
            LoadWeaponData();
            Item.damage = GetStartDamage;
            Item.useAnimation = Item.useTime = LeftClickUseTime;
            Item.autoReuse = true;
            Item.mana = 8;
            Item.CWR().LegendData = new SHPCData();
        }

        public static bool SHPCDamage(Item Item, ref StatModifier damage) {
            CWRUtils.ModifyLegendWeaponDamageFunc(Item, GetOnDamage(Item), GetStartDamage, ref damage);
            return false;
        }

        public static void SetTooltip(Item item, ref List<TooltipLine> tooltips) {
            tooltips.ReplacePlaceholder("legend_Text", CWRLocText.GetTextValue("SHPC_No_legend_Content_3"), "");
            int index = SHPC_Level();
            string num = (index + 1).ToString();
            if (index == 22) {
                num = CWRLocText.GetTextValue("Murasama_Text_Lang_End");
            }
            string text = LegendData.GetLevelTrialPreText(item.CWR(), "Murasama_Text_Lang_0", num);
            tooltips.ReplacePlaceholder("[Lang4]", text, "");
        }
    }
}
