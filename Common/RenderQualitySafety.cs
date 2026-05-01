using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Light;

namespace CalamityOverhaul.Common
{
    internal static class RenderQualitySafety
    {
        /// <summary>
        /// 检查 <see cref="Main.screenTarget"/> 当前是否为 GraphicsDevice 的活动渲染目标
        /// <br/>切换水波质量等场景下，调用方挂的钩子时机可能并没有把 screenTarget 设为当前 RT，
        /// 此时再用 <c>SetRenderTarget(Main.screenTarget); Clear</c> 强行写回会把本该进入 backbuffer 的画面顶替掉，
        /// 导致整个屏幕和 UI 看起来消失。RT 管线类的特效在动手前应先用本方法兜底
        /// </summary>
        public static bool IsScreenTargetActive(GraphicsDevice graphicsDevice) {
            if (graphicsDevice == null) return false;
            if (Main.screenTarget == null || Main.screenTarget.IsDisposed) return false;

            RenderTargetBinding[] bindings = graphicsDevice.GetRenderTargets();
            if (bindings == null || bindings.Length == 0) return false;
            return bindings[0].RenderTarget == Main.screenTarget;
        }

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
