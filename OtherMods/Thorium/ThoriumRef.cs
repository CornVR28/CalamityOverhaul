using InnoVault.GameSystem;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.OtherMods.Thorium
{
    internal static class ThoriumRef
    {
        private static int sheathBuffType = -1;
        private static MethodInfo getModPlayerMethod;
        private static FieldInfo sheathTrackerField;
        private static MethodInfo strikeMethod;

        public static void Load() {
            if (!ModLoader.TryGetMod("ThoriumMod", out Mod mod)) {
                return;
            }

            if (mod.TryFind<ModBuff>("SheathBuff", out var sheathBuff)) {
                sheathBuffType = sheathBuff.Type;
            }

            Type[] modTypes = CWRUtils.GetModTypes(mod);

            var sheathDataType = CWRUtils.GetTargetTypeInStringKey(modTypes, "SheathData");
            if (sheathDataType != null) {
                var meth = sheathDataType.GetMethod("MeleeButNotValidItem", BindingFlags.Static | BindingFlags.Public);
                VaultHook.Add(meth, On_MeleeButNotValidItem);
                meth = sheathDataType.GetMethod("ValidItem", BindingFlags.Static | BindingFlags.Public);
                VaultHook.Add(meth, On_ValidItem);
            }

            var thoriumPlayerType = CWRUtils.GetTargetTypeInStringKey(modTypes, "ThoriumPlayer");
            if (thoriumPlayerType != null) {
                foreach (var m in typeof(Player).GetMethods()) {
                    if (m.Name == "GetModPlayer" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0) {
                        getModPlayerMethod = m.MakeGenericMethod(thoriumPlayerType);
                        break;
                    }
                }
                sheathTrackerField = thoriumPlayerType.GetField("sheathTracker",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (sheathTrackerField != null) {
                    strikeMethod = sheathTrackerField.FieldType.GetMethod("Strike",
                        BindingFlags.Instance | BindingFlags.Public);
                }
            }
        }

        public static bool On_ValidItem(Func<Item, bool> orig, Item item) {
            return item.damage > 0 && item.CountsAsClass(DamageClass.Melee);
        }

        public static bool On_MeleeButNotValidItem(Func<Item, bool> orig, Item item) {
            return item.damage == 0 || !item.CountsAsClass(DamageClass.Melee);
        }

        public static void ModifyProjBySheath(Player player, ref NPC.HitModifiers modifiers) {
            if (sheathBuffType > 0 && player.HasBuff(sheathBuffType)) {
                modifiers.SourceDamage *= 2f;
            }
        }

        public static void OnHitNPCWithProj(Player player, NPC target, NPC.HitInfo hit, int damageDone) {
            if (getModPlayerMethod == null || sheathTrackerField == null || strikeMethod == null) return;
            var thoriumPlayer = getModPlayerMethod.Invoke(player, null);
            if (thoriumPlayer == null) return;
            var sheathTracker = sheathTrackerField.GetValue(thoriumPlayer);
            if (sheathTracker == null) return;
            strikeMethod.Invoke(sheathTracker, [target, hit, damageDone]);
        }
    }

    internal class ThoriumRefLoader : ModPlayer
    {
        public override void Load() {
            if (ModLoader.HasMod("ThoriumMod")) {
                try {
                    ThoriumRef.Load();
                } catch (Exception ex) { CWRMod.Instance.Logger.Error($"ThoriumRefLoader.Load An Error Has Cccurred: {ex.Message}"); }
            }
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
            if (ModLoader.HasMod("ThoriumMod")) {
                ThoriumRef.ModifyProjBySheath(Player, ref modifiers);
            }
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
            if (ModLoader.HasMod("ThoriumMod")) {
                ThoriumRef.OnHitNPCWithProj(Player, target, hit, damageDone);
            }
        }
    }
}
