using CalamityMod;
using CalamityMod.Balancing;
using CalamityMod.CalPlayer;
using CalamityMod.CustomRecipes;
using CalamityMod.DataStructures;
using CalamityMod.Events;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Particles;
using CalamityMod.UI;
using CalamityMod.World;
using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV;
using CalamityOverhaul.Content.LegendWeapon.MurasamaLegend.UI;
using InnoVault.GameSystem;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;
using static CalamityOverhaul.Common.ModGanged;

namespace CalamityOverhaul
{
    /// <summary>
    /// 一个用于访问Calamity Mod内部内容的静态类
    /// </summary>
    internal static class CWRRef
    {
        /// <summary>
        /// Calamity Mod的目标版本，只有当安装了这个版本的Calamity Mod时才会启用相关功能
        /// </summary>
        public static Version TargetCalamityVersion => new(2, 1, 2);
        /// <summary>
        /// 是否安装了指定版本的Calamity Mod
        /// </summary>
        public static bool Has {
            get {
                _has ??= ModLoader.TryGetMod("CalamityMod", out Mod mod) && mod.Version == TargetCalamityVersion;
                return _has.Value;
            }
        }
        private static bool? _has = null;

        private static float dummyFloat;
        private static Type DownedBossSystemType;

        internal static void Load() {
            if (ModLoader.TryGetMod("CalamityMod", out Mod mod)) {
                DownedBossSystemType = mod.Code.GetType("CalamityMod.DownedBossSystem");
            }

            if (DownedBossSystemType is not null) {
                const BindingFlags bf = BindingFlags.Public | BindingFlags.Static;
                downedDesertScourgeProp = DownedBossSystemType.GetProperty("downedDesertScourge", bf);
                downedCLAMProp = DownedBossSystemType.GetProperty("downedCLAM", bf);
                downedCrabulonProp = DownedBossSystemType.GetProperty("downedCrabulon", bf);
                downedHiveMindProp = DownedBossSystemType.GetProperty("downedHiveMind", bf);
                downedPerforatorProp = DownedBossSystemType.GetProperty("downedPerforator", bf);
                downedSlimeGodProp = DownedBossSystemType.GetProperty("downedSlimeGod", bf);
                downedCryogenProp = DownedBossSystemType.GetProperty("downedCryogen", bf);
                downedBrimstoneElementalProp = DownedBossSystemType.GetProperty("downedBrimstoneElemental", bf);
                downedAquaticScourgeProp = DownedBossSystemType.GetProperty("downedAquaticScourge", bf);
                downedCragmawMireProp = DownedBossSystemType.GetProperty("downedCragmawMire", bf);
                downedCalamitasCloneProp = DownedBossSystemType.GetProperty("downedCalamitasClone", bf);
                downedGSSProp = DownedBossSystemType.GetProperty("downedGSS", bf);
                downedLeviathanProp = DownedBossSystemType.GetProperty("downedLeviathan", bf);
                downedAstrumAureusProp = DownedBossSystemType.GetProperty("downedAstrumAureus", bf);
                downedPlaguebringerProp = DownedBossSystemType.GetProperty("downedPlaguebringer", bf);
                downedRavagerProp = DownedBossSystemType.GetProperty("downedRavager", bf);
                downedAstrumDeusProp = DownedBossSystemType.GetProperty("downedAstrumDeus", bf);
                downedGuardiansProp = DownedBossSystemType.GetProperty("downedGuardians", bf);
                downedDragonfollyProp = DownedBossSystemType.GetProperty("downedDragonfolly", bf);
                downedProvidenceProp = DownedBossSystemType.GetProperty("downedProvidence", bf);
                downedCeaselessVoidProp = DownedBossSystemType.GetProperty("downedCeaselessVoid", bf);
                downedStormWeaverProp = DownedBossSystemType.GetProperty("downedStormWeaver", bf);
                downedSignusProp = DownedBossSystemType.GetProperty("downedSignus", bf);
                downedPolterghastProp = DownedBossSystemType.GetProperty("downedPolterghast", bf);
                downedMaulerProp = DownedBossSystemType.GetProperty("downedMauler", bf);
                downedNuclearTerrorProp = DownedBossSystemType.GetProperty("downedNuclearTerror", bf);
                downedBoomerDukeProp = DownedBossSystemType.GetProperty("downedBoomerDuke", bf);
                downedDoGProp = DownedBossSystemType.GetProperty("downedDoG", bf);
                downedYharonProp = DownedBossSystemType.GetProperty("downedYharon", bf);
                downedExoMechsProp = DownedBossSystemType.GetProperty("downedExoMechs", bf);
                downedCalamitasProp = DownedBossSystemType.GetProperty("downedCalamitas", bf);
                downedPrimordialWyrmProp = DownedBossSystemType.GetProperty("downedPrimordialWyrm", bf);
                downedBossRushProp = DownedBossSystemType.GetProperty("downedBossRush", bf);
                downedThanatosProp = DownedBossSystemType.GetProperty("downedThanatos", bf);
            }
        }
        internal static void UnLoad() {
            _has = null;
            DownedBossSystemType = null;
            downedDesertScourgeProp = null;
            downedCLAMProp = null;
            downedCrabulonProp = null;
            downedHiveMindProp = null;
            downedPerforatorProp = null;
            downedSlimeGodProp = null;
            downedCryogenProp = null;
            downedBrimstoneElementalProp = null;
            downedAquaticScourgeProp = null;
            downedCragmawMireProp = null;
            downedCalamitasCloneProp = null;
            downedGSSProp = null;
            downedLeviathanProp = null;
            downedAstrumAureusProp = null;
            downedPlaguebringerProp = null;
            downedRavagerProp = null;
            downedAstrumDeusProp = null;
            downedGuardiansProp = null;
            downedDragonfollyProp = null;
            downedProvidenceProp = null;
            downedCeaselessVoidProp = null;
            downedStormWeaverProp = null;
            downedSignusProp = null;
            downedPolterghastProp = null;
            downedMaulerProp = null;
            downedNuclearTerrorProp = null;
            downedBoomerDukeProp = null;
            downedDoGProp = null;
            downedYharonProp = null;
            downedExoMechsProp = null;
            downedCalamitasProp = null;
            downedPrimordialWyrmProp = null;
            downedBossRushProp = null;
            downedThanatosProp = null;
        }

        private static bool GetDownedProp(PropertyInfo prop) => prop != null && (bool)prop.GetValue(null);
        private static void SetDownedProp(PropertyInfo prop, bool value) => prop?.SetValue(null, value);
        private static PropertyInfo downedDesertScourgeProp;
        private static PropertyInfo downedCLAMProp;
        private static PropertyInfo downedCrabulonProp;
        private static PropertyInfo downedHiveMindProp;
        private static PropertyInfo downedPerforatorProp;
        private static PropertyInfo downedSlimeGodProp;
        private static PropertyInfo downedCryogenProp;
        private static PropertyInfo downedBrimstoneElementalProp;
        private static PropertyInfo downedAquaticScourgeProp;
        private static PropertyInfo downedCragmawMireProp;
        private static PropertyInfo downedCalamitasCloneProp;
        private static PropertyInfo downedGSSProp;
        private static PropertyInfo downedLeviathanProp;
        private static PropertyInfo downedAstrumAureusProp;
        private static PropertyInfo downedPlaguebringerProp;
        private static PropertyInfo downedRavagerProp;
        private static PropertyInfo downedAstrumDeusProp;
        private static PropertyInfo downedGuardiansProp;
        private static PropertyInfo downedDragonfollyProp;
        private static PropertyInfo downedProvidenceProp;
        private static PropertyInfo downedCeaselessVoidProp;
        private static PropertyInfo downedStormWeaverProp;
        private static PropertyInfo downedSignusProp;
        private static PropertyInfo downedPolterghastProp;
        private static PropertyInfo downedMaulerProp;
        private static PropertyInfo downedNuclearTerrorProp;
        private static PropertyInfo downedBoomerDukeProp;
        private static PropertyInfo downedDoGProp;
        private static PropertyInfo downedYharonProp;
        private static PropertyInfo downedExoMechsProp;
        private static PropertyInfo downedCalamitasProp;
        private static PropertyInfo downedPrimordialWyrmProp;
        private static PropertyInfo downedBossRushProp;
        private static PropertyInfo downedThanatosProp;

        /// <summary>
        /// 荒漠灾虫
        /// </summary>
        public static bool GetDownedDesertScourge() => GetDownedProp(downedDesertScourgeProp);

        /// <summary>
        /// 巨像蛤
        /// </summary>
        public static bool GetDownedCLAM() => GetDownedProp(downedCLAMProp);

        /// <summary>
        /// 蘑菇蟹
        /// </summary>
        public static bool GetDownedCrabulon() => GetDownedProp(downedCrabulonProp);

        /// <summary>
        /// 腐巢意志
        /// </summary>
        public static bool GetDownedHiveMind() => GetDownedProp(downedHiveMindProp);

        /// <summary>
        /// 血肉宿主
        /// </summary>
        public static bool GetDownedPerforator() => GetDownedProp(downedPerforatorProp);

        /// <summary>
        /// 史莱姆之神
        /// </summary>
        public static bool GetDownedSlimeGod() => GetDownedProp(downedSlimeGodProp);

        /// <summary>
        /// 极地冰灵
        /// </summary>
        public static bool GetDownedCryogen() => GetDownedProp(downedCryogenProp);

        /// <summary>
        /// 硫磺火元素
        /// </summary>
        public static bool GetDownedBrimstoneElemental() => GetDownedProp(downedBrimstoneElementalProp);

        /// <summary>
        /// 渊海灾虫
        /// </summary>
        public static bool GetDownedAquaticScourge() => GetDownedProp(downedAquaticScourgeProp);

        /// <summary>
        /// 辐射之主
        /// </summary>
        public static bool GetDownedCragmawMire() => GetDownedProp(downedCragmawMireProp);

        /// <summary>
        /// 灾厄之影
        /// </summary>
        public static bool GetDownedCalamitasClone() => GetDownedProp(downedCalamitasCloneProp);

        /// <summary>
        /// 沙漠巨鲨
        /// </summary>
        public static bool GetDownedGSS() => GetDownedProp(downedGSSProp);

        /// <summary>
        /// 利维坦
        /// </summary>
        public static bool GetDownedLeviathan() => GetDownedProp(downedLeviathanProp);

        /// <summary>
        /// 白金星舰
        /// </summary>
        public static bool GetDownedAstrumAureus() => GetDownedProp(downedAstrumAureusProp);

        /// <summary>
        /// 瘟疫使者
        /// </summary>
        public static bool GetDownedPlaguebringer() => GetDownedProp(downedPlaguebringerProp);

        /// <summary>
        /// 毁灭魔像
        /// </summary>
        public static bool GetDownedRavager() => GetDownedProp(downedRavagerProp);

        /// <summary>
        /// 星神游龙
        /// </summary>
        public static bool GetDownedAstrumDeus() => GetDownedProp(downedAstrumDeusProp);

        /// <summary>
        /// 亵渎使徒
        /// </summary>
        public static bool GetDownedGuardians() => GetDownedProp(downedGuardiansProp);

        /// <summary>
        /// 痴愚金龙
        /// </summary>
        public static bool GetDownedDragonfolly() => GetDownedProp(downedDragonfollyProp);

        /// <summary>
        /// 亵渎天神
        /// </summary>
        public static bool GetDownedProvidence() => GetDownedProp(downedProvidenceProp);

        /// <summary>
        /// 无尽虚空
        /// </summary>
        public static bool GetDownedCeaselessVoid() => GetDownedProp(downedCeaselessVoidProp);

        /// <summary>
        /// 风暴编织者
        /// </summary>
        public static bool GetDownedStormWeaver() => GetDownedProp(downedStormWeaverProp);

        /// <summary>
        /// 西格纳斯
        /// </summary>
        public static bool GetDownedSignus() => GetDownedProp(downedSignusProp);

        /// <summary>
        /// 噬魂幽花
        /// </summary>
        public static bool GetDownedPolterghast() => GetDownedProp(downedPolterghastProp);

        /// <summary>
        /// 酸雨二
        /// </summary>
        public static bool GetDownedMauler() => GetDownedProp(downedMaulerProp);

        /// <summary>
        /// 生化恐惧
        /// </summary>
        public static bool GetDownedNuclearTerror() => GetDownedProp(downedNuclearTerrorProp);

        /// <summary>
        /// 老核弹
        /// </summary>
        public static bool GetDownedBoomerDuke() => GetDownedProp(downedBoomerDukeProp);

        /// <summary>
        /// 神明吞噬者
        /// </summary>
        public static bool GetDownedDoG() => GetDownedProp(downedDoGProp);

        /// <summary>
        /// 丛林龙
        /// </summary>
        public static bool GetDownedYharon() => GetDownedProp(downedYharonProp);

        /// <summary>
        /// 星流巨械
        /// </summary>
        public static bool GetDownedExoMechs() => GetDownedProp(downedExoMechsProp);

        /// <summary>
        /// 至尊灾厄
        /// </summary>
        public static bool GetDownedCalamitas() => GetDownedProp(downedCalamitasProp);

        /// <summary>
        /// 始源妖龙
        /// </summary>
        public static bool GetDownedPrimordialWyrm() => GetDownedProp(downedPrimordialWyrmProp);

        /// <summary>
        /// 终焉之战
        /// </summary>
        public static bool GetDownedBossRush() => GetDownedProp(downedBossRushProp);

        public static void SetDownedPrimordialWyrm(bool value) => SetDownedProp(downedPrimordialWyrmProp, value);

        public static bool GetDeathMode() => Has && GetDeathModeInner();
        [CWRJITEnabled]
        private static bool GetDeathModeInner() => CalamityWorld.death;

        public static bool GetRevengeMode() => Has && GetRevengeModeInner();
        [CWRJITEnabled]
        private static bool GetRevengeModeInner() => CalamityWorld.revenge;

        public static bool GetBossRushActive() => Has && GetBossRushActiveInner();
        [CWRJITEnabled]
        private static bool GetBossRushActiveInner() => BossRushEvent.BossRushActive;

        public static void SetBossRushActive(bool value) {
            if (!Has) return;
            SetBossRushActiveInner(value);
        }
        [CWRJITEnabled]
        private static void SetBossRushActiveInner(bool value) => BossRushEvent.BossRushActive = value;

        public static bool GetAcidRainEventIsOngoing() => Has && GetAcidRainEventIsOngoingInner();
        [CWRJITEnabled]
        private static bool GetAcidRainEventIsOngoingInner() => AcidRainEvent.AcidRainEventIsOngoing;

        public static DamageClass GetTrueMeleeDamageClass() => Has ? GetTrueMeleeDamageClassInner() : DamageClass.Default;
        [CWRJITEnabled]
        private static DamageClass GetTrueMeleeDamageClassInner() => ModContent.GetInstance<TrueMeleeDamageClass>();

        public static DamageClass GetTrueMeleeNoSpeedDamageClass() => Has ? GetTrueMeleeNoSpeedDamageClassInner() : DamageClass.Default;
        [CWRJITEnabled]
        private static DamageClass GetTrueMeleeNoSpeedDamageClassInner() => ModContent.GetInstance<TrueMeleeNoSpeedDamageClass>();

        public static DamageClass GetMeleeRangedHybridDamageClass() => Has ? GetMeleeRangedHybridDamageClassInner() : DamageClass.Default;
        [CWRJITEnabled]
        private static DamageClass GetMeleeRangedHybridDamageClassInner() => ModContent.GetInstance<MeleeRangedHybridDamageClass>();

        public static float ChargeRatio(Item item) => Has ? ChargeRatioInner(item) : 0f;
        [CWRJITEnabled]
        private static float ChargeRatioInner(Item item) => item.Calamity().ChargeRatio;

        public static bool GetPlayerBladeArmEnchant(this Player player) => Has && GetPlayerBladeArmEnchantInner(player);
        [CWRJITEnabled]
        private static bool GetPlayerBladeArmEnchantInner(Player player) => player.Calamity().bladeArmEnchant;

        public static bool GetPlayerAdrenalineMode(this Player player) => Has && GetPlayerAdrenalineModeInner(player);
        [CWRJITEnabled]
        private static bool GetPlayerAdrenalineModeInner(Player player) => player.Calamity().adrenalineModeActive;

        public static void LargeFieryExplosion(Projectile projectile) {
            if (!Has) return;
            LargeFieryExplosionInner(projectile);
        }
        [CWRJITEnabled]
        private static void LargeFieryExplosionInner(Projectile projectile) => projectile.LargeFieryExplosion();

        public static void UpdateRogueStealth(Player player) {
            if (!Has) return;
            UpdateRogueStealthInner(player);
        }
        [CWRJITEnabled]
        private static void UpdateRogueStealthInner(Player player) {
            bool noAvailable = false;
            CalamityPlayer calPlayer = player.Calamity();
            if (CWRMod.Instance.narakuEye != null) {
                noAvailable = (bool)CWRMod.Instance.narakuEye.Call(player);
                if (calPlayer.StealthStrikeAvailable()) {
                    noAvailable = false;
                }
            }
            if (!noAvailable) {
                calPlayer.rogueStealth = 0;
                if (calPlayer.stealthUIAlpha > 0.02f) {
                    calPlayer.stealthUIAlpha -= 0.02f;
                }
            }
        }

        public static void SummonSupCal(Vector2 spawnPos) {
            SoundEngine.PlaySound("CalamityMod/Sounds/Custom/SCalAltarSummon".GetSound(), spawnPos);
            Projectile.NewProjectile(new EntitySource_WorldEvent(), spawnPos, Vector2.Zero
                , CWRID.Proj_SCalRitualDrama, 0, 0f, Main.myPlayer, 0, 0);
        }

        public static void SummonExo(int exoType, Player player) {
            if (!Has) {
                return;
            }
            SummonExoInner(exoType, player);
        }
        [CWRJITEnabled]
        public static void SummonExoInner(int exoType, Player player) {
            CalamityWorld.DraedonMechToSummon = (ExoMech)exoType;
            if (VaultUtils.isClient) {//客户端发送网络数据到服务器
                //通过反射直接调用 ExoMechSelectionPacket.Send()
                var calMod = ModLoader.GetMod("CalamityMod");
                var packetType = calMod.Code.GetType("CalamityMod.Packets.ExoMechSelectionPacket");
                var sendMethod = packetType.GetMethod("Send", BindingFlags.Public | BindingFlags.Static);
                sendMethod.Invoke(null, [/* toClient */ -1, /* ignoreClient */ -1]);
                return;
            }
            switch (CalamityWorld.DraedonMechToSummon) {
                case ExoMech.Destroyer:
                    Vector2 thanatosSpawnPosition = player.Center + Vector2.UnitY * 2100f;
                    NPC thanatos = CalamityUtils.SpawnBossBetter(thanatosSpawnPosition, CWRID.NPC_ThanatosHead);
                    if (thanatos != null)
                        thanatos.velocity = thanatos.SafeDirectionTo(player.Center) * 40f;
                    break;

                case ExoMech.Prime:
                    Vector2 aresSpawnPosition = player.Center - Vector2.UnitY * 1400f;
                    CalamityUtils.SpawnBossBetter(aresSpawnPosition, CWRID.NPC_AresBody);
                    break;

                case ExoMech.Twins:
                    Vector2 artemisSpawnPosition = player.Center + new Vector2(-1100f, -1600f);
                    Vector2 apolloSpawnPosition = player.Center + new Vector2(1100f, -1600f);
                    CalamityUtils.SpawnBossBetter(artemisSpawnPosition, CWRID.NPC_Artemis);
                    CalamityUtils.SpawnBossBetter(apolloSpawnPosition, CWRID.NPC_Apollo);
                    break;
            }
        }

        public static void DrawAfterimagesCentered(Projectile proj, int mode, Color lightColor, int typeOneIncrement = 1, Texture2D texture = null, bool drawCentered = true) {
            if (!Has) {
                Main.spriteBatch.Draw(TextureAssets.Projectile[proj.type].Value, proj.Center - Main.screenPosition
                    , null, lightColor, proj.rotation, TextureAssets.Projectile[proj.type].Value.Size() / 2, proj.scale, SpriteEffects.None, 0);
                return;
            }
            DrawAfterimagesCenteredInner(proj, mode, lightColor, typeOneIncrement, texture, drawCentered);
        }
        [CWRJITEnabled]
        private static void DrawAfterimagesCenteredInner(Projectile proj, int mode, Color lightColor, int typeOneIncrement, Texture2D texture, bool drawCentered) => CalamityUtils.DrawAfterimagesCentered(proj, mode, lightColor, typeOneIncrement, texture, drawCentered);

        public static void HomeInOnNPC(Projectile projectile, bool ignoreTiles, float distanceRequired, float homingVelocity, float inertia) {
            if (!Has) return;
            HomeInOnNPCInner(projectile, ignoreTiles, distanceRequired, homingVelocity, inertia);
        }
        [CWRJITEnabled]
        private static void HomeInOnNPCInner(Projectile projectile, bool ignoreTiles, float distanceRequired, float homingVelocity, float inertia) => CalamityUtils.HomeInOnNPC(projectile, ignoreTiles, distanceRequired, homingVelocity, inertia);

        public static void SetDraedonDefeatTimer(NPC npc, float value) {
            if (!Has) return;
            SetDraedonDefeatTimerInner(npc, value);
        }
        [CWRJITEnabled]
        private static void SetDraedonDefeatTimerInner(NPC npc, float value) {
            if (npc.ModNPC is Draedon draedon) {
                draedon.DefeatTimer = value;
            }
        }

        public static float GetDraedonDefeatTimer(NPC npc) => Has ? GetDraedonDefeatTimerInner(npc) : 0f;
        [CWRJITEnabled]
        private static float GetDraedonDefeatTimerInner(NPC npc) {
            if (npc.ModNPC is Draedon draedon) {
                return draedon.DefeatTimer;
            }
            return 0f;
        }

        public static bool HasExo() => Has && HasExoInner();
        [CWRJITEnabled]
        private static bool HasExoInner() => Draedon.ExoMechIsPresent;

        public static void SetAbleToSelectExoMech(Player player, bool value) {
            if (!Has) return;
            SetAbleToSelectExoMechInner(player, value);
        }
        [CWRJITEnabled]
        private static void SetAbleToSelectExoMechInner(Player player, bool value) {
            player.Calamity().AbleToSelectExoMech = value;
        }

        public static void SetProjtimesPierced(this Projectile projectile, int value) {
            if (!Has) return;
            SetProjtimesPiercedInner(projectile, value);
        }
        [CWRJITEnabled]
        private static void SetProjtimesPiercedInner(Projectile projectile, int value) => projectile.Calamity().timesPierced = value;

        public static void SetAllProjectilesHome(this Projectile projectile, bool value) {
            if (!Has) return;
            SetAllProjectilesHomeInner(projectile, value);
        }
        [CWRJITEnabled]
        private static void SetAllProjectilesHomeInner(Projectile projectile, bool value) => projectile.Calamity().conditionalHomingRange = (value ? 450 : 0);

        public static void SetDownedCalamitas(bool value) => SetDownedProp(downedCalamitasProp, value);

        public static SoundStyle GetSound(this string path) {
            if (ModContent.HasAsset(path)) {
                return new SoundStyle(path);
            }
            return CWRSound.None;
        }

        public static bool GetDownedThanatos() => GetDownedProp(downedThanatosProp);

        public static int GetSupCalGiveUpCounter(NPC npc) => Has ? GetSupCalGiveUpCounterInner(npc) : 0;
        [CWRJITEnabled]
        private static int GetSupCalGiveUpCounterInner(NPC npc) {
            if (npc.ModNPC is SupremeCalamitas supCal) {
                return supCal.giveUpCounter;
            }
            return 0;
        }

        public static void SetSupCalGiveUpCounter(NPC npc, int value) {
            if (!Has) return;
            SetSupCalGiveUpCounterInner(npc, value);
        }
        [CWRJITEnabled]
        private static void SetSupCalGiveUpCounterInner(NPC npc, int value) {
            if (npc.ModNPC is SupremeCalamitas supCal) {
                supCal.giveUpCounter = value;
            }
        }

        public static Type FindCalamityType(string key) {
            if (CWRMod.Instance.calamity != null) {
                return CWRMod.Instance.calamity.Code.GetType(key);
            }
            return null;
        }

        public static Type GetItem_SHPC_Type() => FindCalamityType("CalamityMod.Items.Weapons.Magic.SHPC");
        public static Type GetNPC_WITCH_Type() => FindCalamityType("CalamityMod.NPCs.TownNPCs.BrimstoneWitch");
        public static Type GetNPC_SupCal_Type() => FindCalamityType("CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas");
        public static Type GetTEBaseTurret_Type() => FindCalamityType("CalamityMod.TileEntities.TEBaseTurret");

        /// <summary>
        /// 设置SHPC的装填魂魄类型
        /// </summary>
        public static void SetSHPCStoredSoulType(Item item, int soulType) {
            if (!Has) return;
            SetSHPCStoredSoulTypeInner(item, soulType);
        }
        [CWRJITEnabled]
        private static void SetSHPCStoredSoulTypeInner(Item item, int soulType) {
            if (item.ModItem is SHPC shpc) {
                shpc.storedSoulType = soulType;
            }
        }

        /// <summary>
        /// 获取SHPC的装填魂魄类型
        /// </summary>
        public static int GetSHPCStoredSoulType(Item item) => Has ? GetSHPCStoredSoulTypeInner(item) : ItemID.SoulofLight;
        [CWRJITEnabled]
        private static int GetSHPCStoredSoulTypeInner(Item item) {
            if (item.ModItem is SHPC shpc) {
                return shpc.storedSoulType;
            }
            return ItemID.SoulofLight;
        }

        public static bool GetEarlyHardmodeProgressionReworkBool() => Has && GetEarlyHardmodeProgressionReworkBoolInner();
        [CWRJITEnabled]
        private static bool GetEarlyHardmodeProgressionReworkBoolInner() => CalamityServerConfig.Instance.EarlyHardmodeProgressionRework;

        public static int GetProjectileDamage(NPC npc, int projType) {
            int num = npc.defDamage / 2;//暂时使用这个，原来的方法在某些情况下会返回1或者0
            if (Main.expertMode) {
                num = (int)(num * 0.75f);
            }
            if (Main.masterMode) {
                num = (int)(num * 0.75f);
            }
            return num;
        }

        public static void SetPlayerInfiniteFlight(this Player player, bool value) {
            if (!Has) return;
            SetPlayerInfiniteFlightInner(player, value);
        }
        [CWRJITEnabled]
        private static void SetPlayerInfiniteFlightInner(Player player, bool value) => player.Calamity().infiniteFlight = value;

        public static bool GetPlayerStealthStrikeAvailable(this Player player) => Has && GetPlayerStealthStrikeAvailableInner(player);
        [CWRJITEnabled]
        private static bool GetPlayerStealthStrikeAvailableInner(Player player) => player.Calamity().StealthStrikeAvailable();

        public static void SetProjStealthStrike(this Projectile projectile, bool value) {
            if (!Has) return;
            SetProjStealthStrikeInner(projectile, value);
        }
        [CWRJITEnabled]
        private static void SetProjStealthStrikeInner(Projectile projectile, bool value) => projectile.Calamity().stealthStrike = value;

        public static bool GetProjStealthStrike(this Projectile projectile) => Has && GetProjStealthStrikeInner(projectile);
        [CWRJITEnabled]
        private static bool GetProjStealthStrikeInner(Projectile projectile) => projectile.Calamity().stealthStrike;

        public static void OldDukeOnKill(NPC npc) {
            if (!Has) return;
            OldDukeOnKillInner(npc);
        }
        [CWRJITEnabled]
        private static void OldDukeOnKillInner(NPC npc) {
            StopAcidRainInner();
            CalamityGlobalNPC.SetNewBossJustDowned(npc);
            SetDownedProp(downedBoomerDukeProp, true);
            AcidRainEvent.OldDukeHasBeenEncountered = true;
            NPCLoader.OnKill(npc);
        }

        public static void StopAcidRain() {
            if (!Has) return;
            StopAcidRainInner();
        }
        [CWRJITEnabled]
        private static void StopAcidRainInner() {
            AcidRainEvent.AccumulatedKillPoints = 0;
            AcidRainEvent.UpdateInvasion(win: true);
        }

        public static void StarRT(Projectile projectile, Entity target) {
            if (!Has) return;
            StarRTInner(projectile, target);
        }
        [CWRJITEnabled]
        private static void StarRTInner(Projectile projectile, Entity target) {
            if (!VaultUtils.isServer) {
                Color color = Color.Lerp(Color.Cyan, Color.White, Main.rand.NextFloat(0.3f, 0.64f));
                GeneralParticleHandler.SpawnParticle(new ImpactParticle(Vector2.Lerp(projectile.Center, target.Center, 0.65f), 0.1f, 20, Main.rand.NextFloat(0.4f, 0.5f), color));
                for (int i = 0; i < 20; i++) {
                    Vector2 spawnPosition = target.Center + Main.rand.NextVector2Circular(30f, 30f);
                    StreamGougeMetaball.SpawnParticle(spawnPosition, Main.rand.NextVector2Circular(3f, 3f), 60f);

                    float scale = MathHelper.Lerp(24f, 64f, CalamityUtils.Convert01To010(i / 19f));
                    spawnPosition = target.Center + projectile.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(-40f, 90f, i / 19f);
                    Vector2 particleVelocity = projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.23f) * Main.rand.NextFloat(2.5f, 9f);
                    StreamGougeMetaball.SpawnParticle(spawnPosition, particleVelocity, scale);
                }
            }
        }

        public static void SpanFire(Entity entity) {
            if (!Has) return;
            SpanFireInner(entity);
        }
        [CWRJITEnabled]
        private static void SpanFireInner(Entity entity) {
            bool LowVel = Main.rand.NextBool() ? false : true;
            FlameParticle ballFire = new FlameParticle(entity.Center + VaultUtils.RandVr(entity.width / 2)
                , Main.rand.Next(13, 22), Main.rand.NextFloat(0.1f, 0.22f), Main.rand.NextFloat(0.02f, 0.07f), Color.Gold, Color.DarkRed) {
                Velocity = new Vector2(entity.velocity.X * 0.8f, -10).RotatedByRandom(0.005f)
                * (LowVel ? Main.rand.NextFloat(0.4f, 0.65f) : Main.rand.NextFloat(0.8f, 1f))
            };
            GeneralParticleHandler.SpawnParticle(ballFire);
        }

        public static ref float RefItemCharge(this Item item) {
            if (!Has) {
                return ref dummyFloat;
            }
            return ref RefItemChargeInner(item);
        }
        [CWRJITEnabled]
        private static ref float RefItemChargeInner(Item item) => ref item.Calamity().Charge;

        public static float GetItemMaxCharge(this Item item) => Has ? GetItemMaxChargeInner(item) : 0f;
        [CWRJITEnabled]
        private static float GetItemMaxChargeInner(Item item) => item.Calamity().MaxCharge;

        public static ref float RefItemMaxCharge(this Item item) {
            if (!Has) {
                return ref dummyFloat;
            }
            return ref RefItemMaxChargeInner(item);
        }
        [CWRJITEnabled]
        private static ref float RefItemMaxChargeInner(Item item) => ref item.Calamity().MaxCharge;

        public static bool GetItemUsesCharge(this Item item) => Has && GetItemUsesChargeInner(item);
        [CWRJITEnabled]
        private static bool GetItemUsesChargeInner(Item item) => item.Calamity().UsesCharge;

        public static bool SetItemUsesCharge(this Item item, bool value) => Has && SetItemUsesChargeInner(item, value);
        [CWRJITEnabled]
        private static bool SetItemUsesChargeInner(Item item, bool value) => item.Calamity().UsesCharge = value;

        public static DamageClass GetRogueDamageClass() {
            if (!Has) {
                return DamageClass.Default;
            }
            return GetRogueDamageClassInner();
        }
        [CWRJITEnabled]
        private static DamageClass GetRogueDamageClassInner() => ModContent.GetInstance<RogueDamageClass>();

        public static float GetPlayerRogueStealth(this Player player) => Has ? GetPlayerRogueStealthInner(player) : 0f;
        [CWRJITEnabled]
        private static float GetPlayerRogueStealthInner(Player player) => player.Calamity().rogueStealth;

        public static float SetPlayerRogueStealth(this Player player, float value) => Has ? SetPlayerRogueStealthInner(player, value) : 0f;
        [CWRJITEnabled]
        private static float SetPlayerRogueStealthInner(Player player, float value) => player.Calamity().rogueStealth = value;

        public static float GetPlayerRogueStealthMax(this Player player) => Has ? GetPlayerRogueStealthMaxInner(player) : 0f;
        [CWRJITEnabled]
        private static float GetPlayerRogueStealthMaxInner(Player player) => player.Calamity().rogueStealthMax;

        public static ref float RefPlayerRogueStealthMax(this Player player) {
            if (!Has) {
                return ref dummyFloat;
            }
            return ref RefPlayerRogueStealthMaxInner(player);
        }
        [CWRJITEnabled]
        private static ref float RefPlayerRogueStealthMaxInner(Player player) => ref player.Calamity().rogueStealthMax;

        public static bool GetPlayerZoneSulphur(this Player player) => Has && GetPlayerZoneSulphurInner(player);
        [CWRJITEnabled]
        private static bool GetPlayerZoneSulphurInner(Player player) => player.Calamity().ZoneSulphur;

        public static bool GetPlayerZoneAbyss(this Player player) => Has && GetPlayerZoneAbyssInner(player);
        [CWRJITEnabled]
        private static bool GetPlayerZoneAbyssInner(Player player) => player.Calamity().ZoneAbyss;

        public static bool GetPlayerProfanedCrystalBuffs(this Player player) => Has && GetPlayerProfanedCrystalBuffsInner(player);
        [CWRJITEnabled]
        private static bool GetPlayerProfanedCrystalBuffsInner(Player player) => player.Calamity().profanedCrystalBuffs;

        public static void SetPlayerDashID(this Player player, string value) {
            if (!Has) return;
            SetPlayerDashIDInner(player, value);
        }
        [CWRJITEnabled]
        private static void SetPlayerDashIDInner(Player player, string value) => player.Calamity().DashID = value;

        public static LocalizedText ConstructRecipeCondition(int tier, out Func<bool> condition) {
            condition = null;
            return Has ? ConstructRecipeConditionInner(tier, out condition) : null;
        }
        [CWRJITEnabled]
        private static LocalizedText ConstructRecipeConditionInner(int tier, out Func<bool> condition) => ArsenalTierGatedRecipe.ConstructRecipeCondition(tier, out condition);

        public static List<Vector2> BezierCurveGetPoints(int count, params Vector2[] pos) => Has ? BezierCurveGetPointsInner(count, pos) : new List<Vector2>();
        [CWRJITEnabled]
        private static List<Vector2> BezierCurveGetPointsInner(int count, Vector2[] pos) => new BezierCurve(pos).GetPoints(count);

        #region 炼铸系统包装器
        /// <summary>
        /// 附魔包装器结构体，用于安全地封装CalamityMod的Enchantment
        /// </summary>
        public struct EnchantmentWrapper
        {
            /// <summary>
            /// 附魔名称
            /// </summary>
            public LocalizedText Name { get; set; }

            /// <summary>
            /// 附魔描述
            /// </summary>
            public LocalizedText Description { get; set; }

            /// <summary>
            /// 附魔图标路径
            /// </summary>
            public string IconTexturePath { get; set; }

            /// <summary>
            /// 内部标识符（用于比较）
            /// </summary>
            internal int InternalId { get; set; }

            /// <summary>
            /// 是否是清除附魔
            /// </summary>
            public bool IsClearEnchantment { get; set; }

            public override bool Equals(object obj) {
                if (obj is EnchantmentWrapper other)
                    return InternalId == other.InternalId;
                return false;
            }

            public override int GetHashCode() => InternalId;

            public static bool operator ==(EnchantmentWrapper left, EnchantmentWrapper right)
                => left.InternalId == right.InternalId;

            public static bool operator !=(EnchantmentWrapper left, EnchantmentWrapper right)
                => !(left == right);
        }

        /// <summary>
        /// 获取物品的有效附魔列表
        /// </summary>
        public static List<EnchantmentWrapper> GetValidEnchantmentsForItem(Item item) {
            if (!Has || item == null || item.IsAir)
                return new List<EnchantmentWrapper>();
            return GetValidEnchantmentsForItemInner(item);
        }
        [CWRJITEnabled]
        private static List<EnchantmentWrapper> GetValidEnchantmentsForItemInner(Item item) {
            var result = new List<EnchantmentWrapper>();
            var enchantments = CalamityMod.UI.CalamitasEnchants.EnchantmentManager.GetValidEnchantmentsForItem(item);

            int id = 0;
            foreach (var enchantment in enchantments) {
                result.Add(new EnchantmentWrapper {
                    Name = enchantment.Name,
                    Description = enchantment.Description,
                    IconTexturePath = enchantment.IconTexturePath,
                    InternalId = id++,
                    IsClearEnchantment = enchantment.Equals(CalamityMod.UI.CalamitasEnchants.EnchantmentManager.ClearEnchantment)
                });
            }

            return result;
        }

        /// <summary>
        /// 获取清除附魔的包装器
        /// </summary>
        public static EnchantmentWrapper GetClearEnchantment() {
            if (!Has)
                return default;
            return GetClearEnchantmentInner();
        }
        [CWRJITEnabled]
        private static EnchantmentWrapper GetClearEnchantmentInner() {
            var clearEnchant = CalamityMod.UI.CalamitasEnchants.EnchantmentManager.ClearEnchantment;
            return new EnchantmentWrapper {
                Name = clearEnchant.Name,
                Description = clearEnchant.Description,
                IconTexturePath = clearEnchant.IconTexturePath,
                InternalId = -1,
                IsClearEnchantment = true
            };
        }

        /// <summary>
        /// 应用附魔到物品
        /// </summary>
        public static void ApplyEnchantmentToItem(Item item, EnchantmentWrapper wrapper, Action<Item> creationEffect = null) {
            if (!Has || item == null || item.IsAir)
                return;
            ApplyEnchantmentToItemInner(item, wrapper, creationEffect);
        }
        [CWRJITEnabled]
        private static void ApplyEnchantmentToItemInner(Item item, EnchantmentWrapper wrapper, Action<Item> creationEffect) {
            int oldPrefix = item.prefix;
            item.SetDefaults(item.type);
            item.Prefix(oldPrefix);

            if (wrapper.IsClearEnchantment) {
                item.Calamity().AppliedEnchantment = null;
                item.Prefix(oldPrefix);
            }
            else {
                //通过Name和Description重新匹配Enchantment
                var allEnchantments = CalamityMod.UI.CalamitasEnchants.EnchantmentManager.GetValidEnchantmentsForItem(item);
                CalamityMod.UI.CalamitasEnchants.Enchantment? targetEnchant = null;

                foreach (var ench in allEnchantments) {
                    if (ench.Name.Value == wrapper.Name.Value && ench.Description.Value == wrapper.Description.Value) {
                        targetEnchant = ench;
                        break;
                    }
                }

                if (targetEnchant.HasValue) {
                    item.Calamity().AppliedEnchantment = targetEnchant.Value;
                    creationEffect?.Invoke(item);
                    targetEnchant.Value.CreationEffect?.Invoke(item);

                    if (CalamityMod.UI.CalamitasEnchants.EnchantmentManager.ItemUpgradeRelationship.TryGetValue(item.type, out var newID)) {
                        item.SetDefaults(newID);
                        item.Prefix(oldPrefix);
                    }
                }
            }
        }

        /// <summary>
        /// 获取物品当前的附魔
        /// </summary>
        public static EnchantmentWrapper? GetItemEnchantment(Item item) {
            if (!Has || item == null || item.IsAir)
                return null;
            return GetItemEnchantmentInner(item);
        }
        [CWRJITEnabled]
        private static EnchantmentWrapper? GetItemEnchantmentInner(Item item) {
            var appliedEnchant = item.Calamity().AppliedEnchantment;
            if (!appliedEnchant.HasValue)
                return null;

            var ench = appliedEnchant.Value;
            return new EnchantmentWrapper {
                Name = ench.Name,
                Description = ench.Description,
                IconTexturePath = ench.IconTexturePath,
                InternalId = 0,
                IsClearEnchantment = ench.Equals(CalamityMod.UI.CalamitasEnchants.EnchantmentManager.ClearEnchantment)
            };
        }

        /// <summary>
        /// 检查附魔是否可用于物品
        /// </summary>
        public static bool IsEnchantmentValidForItem(Item item, EnchantmentWrapper wrapper) {
            if (!Has || item == null || item.IsAir)
                return false;
            return IsEnchantmentValidForItemInner(item, wrapper);
        }
        [CWRJITEnabled]
        private static bool IsEnchantmentValidForItemInner(Item item, EnchantmentWrapper wrapper) {
            var validEnchantments = CalamityMod.UI.CalamitasEnchants.EnchantmentManager.GetValidEnchantmentsForItem(item);

            foreach (var ench in validEnchantments) {
                if (ench.Name.Value == wrapper.Name.Value && ench.Description.Value == wrapper.Description.Value)
                    return true;
            }

            return false;
        }
        #endregion

        #region 加载联动修改内容
        public static MethodBase BossHealthBarManager_Draw_Method;
        public static MethodBase calamityUtils_GetReworkedReforge_Method;
        internal delegate void On_DisplayLocalizedText_Dalegate(string key, Color? textColor = null);

        internal static void LoadComders() {
            if (!Has) {
                return;
            }
            try {
                LoadComdersInner();
            } catch { }
        }
        [CWRJITEnabled]
        internal static void LoadComdersInner() {
            //这一切不该发生，灾厄没有在这里留下任何可扩展的接口，如果想要那该死血条的为第三方事件靠边站，只能这么做，至少这是我目前能想到的方法
            BossHealthBarManager_Draw_Method = typeof(BossHealthBarManager)
                .GetMethod("Draw", BindingFlags.Instance | BindingFlags.Public);
            if (BossHealthBarManager_Draw_Method != null) {
                VaultHook.Add(BossHealthBarManager_Draw_Method, On_BossHealthBarManager_Draw_Hook);
            }
            else {
                CWRUtils.LogFailedLoad("BossHealthBarManager_Draw_Method", "CalamityMod.BossHealthBarManager");
            }

            calamityUtils_GetReworkedReforge_Method = typeof(CalamityUtils)
                .GetMethod("GetReworkedReforge", BindingFlags.Static | BindingFlags.NonPublic);
            if (calamityUtils_GetReworkedReforge_Method != null) {
                VaultHook.Add(calamityUtils_GetReworkedReforge_Method, OnGetReworkedReforgeHook);
            }
            else {
                CWRUtils.LogFailedLoad("calamityUtils_GetReworkedReforge_Method", "CalamityUtils.GetReworkedReforge");
            }

            MethodInfo methodInfo = typeof(CalamityUtils).GetMethod("BroadcastLocalizedText", BindingFlags.Static | BindingFlags.Public);
            if (methodInfo != null) {
                VaultHook.Add(methodInfo, OnDisplayLocalizedTextHook);
            }

            //我鸡巴的还能说什么？为什么这么多人喜欢改同一个东西？Fuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuck
            if (CWRMod.Instance.luminance != null) {
                var utType = CWRUtils.GetTargetTypeInStringKey(CWRUtils.GetModTypes(CWRMod.Instance.luminance), "Utilities");
                methodInfo = utType.GetMethod("BroadcastLocalizedText", BindingFlags.Static | BindingFlags.Public);
                if (methodInfo != null) {
                    VaultHook.Add(methodInfo, OnDisplayLocalizedTextHook);
                }
            }

            var math = typeof(CalamityPlayer).GetMethod("ProvideStealthStatBonuses", BindingFlags.Instance | BindingFlags.NonPublic);
            VaultHook.Add(math, OnProvideStealthStatBonusesHook);
        }

        [CWRJITEnabled]
        private static void On_BossHealthBarManager_Draw_Hook(On_BossHealthBarManager_Draw_Dalegate orig, object obj, SpriteBatch spriteBatch, IBigProgressBar currentBar, BigProgressBarInfo info) {
            int startHeight = 100;
            int x = Main.screenWidth - 420;
            int y = Main.screenHeight - startHeight;
            if (Main.playerInventory || VaultUtils.IsInvasion()) {
                x -= 250;
            }
            Vector2 modifyPos = MuraChargeUI.Instance.ModifyBossHealthBarManagerPositon(x, y);
            x = (int)modifyPos.X;
            y = (int)modifyPos.Y;
            //谢天谢地BossHealthBarManager.Bars和BossHealthBarManager.BossHPUI是公开的
            foreach (BossHealthBarManager.BossHPUI ui in BossHealthBarManager.Bars) {
                ui.Draw(spriteBatch, x, y);
                y -= BossHealthBarManager.BossHPUI.VerticalOffsetPerBar;
            }
        }

        [CWRJITEnabled]
        internal static int OnGetReworkedReforgeHook(On_GetReworkedReforge_Dalegate orig
            , Item item, UnifiedRandom rand, int currentPrefix) {
            int reset = orig.Invoke(item, rand, currentPrefix);
            reset = OnCalamityReforgeEvent.HandleCalamityReforgeModificationDueToMissingItemLoader(item, rand, currentPrefix);
            return reset;
        }

        [CWRJITEnabled]
        internal static void OnDisplayLocalizedTextHook(On_DisplayLocalizedText_Dalegate orig, string key, Color? textColor = null) {
            Color color = textColor ?? Color.White;
            if (VaultLoad.LoadenContent) {
                bool result = true;
                foreach (var d in ModifyDisplayText.Instances) {
                    if (!d.Alive(Main.LocalPlayer)) {
                        continue;
                    }
                    bool newResult = d.Handle(ref key, ref color);
                    if (!newResult) {
                        result = false;
                    }
                }
                if (!result) {
                    return;
                }
            }

            orig.Invoke(key, color);
        }

        [CWRJITEnabled]
        private static void OnProvideStealthStatBonusesHook(Action<CalamityPlayer> orig, CalamityPlayer calamityPlayer) {
            if (calamityPlayer.Player.CWR().IsUnsunghero) {
                if (!calamityPlayer.wearingRogueArmor || calamityPlayer.rogueStealthMax <= 0) {
                    return;
                }

                Item item = calamityPlayer.Player.GetItem();
                int realUseTime = Math.Max(item.useTime, item.useAnimation);
                double useTimeFactor = 0.75 + 0.75 * Math.Log(realUseTime + 2D, 4D);
                //直接使用固定的基础时间，固定为 4 秒
                double stealthGenFactor = Math.Max(Math.Pow(4f, 2D / 3D), 1.5);

                double stealthAddedDamage = calamityPlayer.rogueStealth * BalancingConstants.UniversalStealthStrikeDamageFactor * useTimeFactor * stealthGenFactor;
                calamityPlayer.stealthDamage += (float)stealthAddedDamage;

                calamityPlayer.Player.aggro -= (int)(calamityPlayer.rogueStealth * 300f);

                return;
            }

            orig.Invoke(calamityPlayer);
        }
        #endregion
    }
}