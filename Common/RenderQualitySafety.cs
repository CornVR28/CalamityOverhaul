using System;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Light;

namespace CalamityOverhaul.Common
{
    internal static class RenderQualitySafety
    {
        //tModLoader/Terraria 版本间该设置名可能不同，反射读取可避免绑定具体字段名。
        private static readonly string[] WaterQualityMemberNames = [
            "WaveQuality", "waveQuality", "WaterQuality", "waterQuality",
            "LiquidQuality", "liquidQuality"
        ];

        public static bool NeedsScreenTargetFallback() {
            if (Lighting.Mode == LightMode.Retro || Lighting.Mode == LightMode.Trippy) {
                return true;
            }

            return IsLowWaterQuality();
        }

        private static bool IsLowWaterQuality() {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            Type mainType = typeof(Main);

            foreach (string memberName in WaterQualityMemberNames) {
                FieldInfo field = mainType.GetField(memberName, flags);
                if (field != null && IsLowQualityValue(field.GetValue(null))) {
                    return true;
                }

                PropertyInfo property = mainType.GetProperty(memberName, flags);
                if (property != null && IsLowQualityValue(property.GetValue(null))) {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLowQualityValue(object value) {
            if (value == null) return false;

            string text = value.ToString();
            if (text.Equals("Off", StringComparison.OrdinalIgnoreCase)
                || text.Equals("Low", StringComparison.OrdinalIgnoreCase)
                || text.Equals("Disabled", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            if (value is bool enabled) return !enabled;

            if (value is IConvertible convertible) {
                try {
                    return convertible.ToDouble(null) <= 1d;
                }
                catch {
                    return false;
                }
            }

            return false;
        }
    }
}
