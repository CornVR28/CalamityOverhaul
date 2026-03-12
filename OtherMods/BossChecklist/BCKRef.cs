using System;
using System.Reflection;
using Terraria.ModLoader;

namespace CalamityOverhaul.OtherMods.BossChecklist
{
    internal class BCKRef
    {
        public static bool Has => ModLoader.HasMod("BossChecklist");

        private static FieldInfo _activeNPCEntryFlagsField;
        private static Type _worldAssistType;

        private static bool TryGetActiveNPCEntryFlags(out int[] flags) {
            flags = null;

            if (_worldAssistType == null) {
                if (!ModLoader.TryGetMod("BossChecklist", out var mod)) {
                    return false;
                }
                _worldAssistType = mod.Code.GetType("BossChecklist.WorldAssist");
                if (_worldAssistType == null) {
                    return false;
                }
            }

            _activeNPCEntryFlagsField ??= _worldAssistType.GetField("ActiveNPCEntryFlags",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

            if (_activeNPCEntryFlagsField == null) {
                return false;
            }

            flags = _activeNPCEntryFlagsField.GetValue(null) as int[];
            return flags != null;
        }

        public static void SetActiveNPCEntryFlags(int index, int value) {
            if (!Has) {
                return;
            }
            if (!TryGetActiveNPCEntryFlags(out var flags)) {
                return;
            }
            if (index < 0 || index >= flags.Length) {
                return;
            }
            flags[index] = value;
        }
    }
}
