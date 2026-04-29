using InnoVault.GameSystem;
using System;
using System.Reflection;
using Terraria;

namespace CalamityOverhaul.Content.Structures
{
    //修复CalamityMod在子世界中HandleTileGrowth抛出ArgumentOutOfRangeException的问题
    //原因是子世界中Main.worldSurface可能被设置到接近世界底部，导致genRand.Next的minValue>=maxValue
    internal class CalamityWorldBugFix : ICWRLoader
    {
        private static void On_HandleTileGrowth(Action orig) {
            int surfaceLevel = (int)Main.worldSurface - 1;
            //地表层低于坐标10或高于接近底部时，随机范围会非法，直接跳过整个函数
            if (surfaceLevel <= 10 || surfaceLevel >= Main.maxTilesY - 20) {
                return;
            }
            orig();
        }

        void ICWRLoader.LoadData() {
            var type = CWRMod.Instance.calamity?.Code.GetType("CalamityMod.Systems.WorldMiscUpdateSystem");
            if (type is null) {
                return;
            }
            var method = type.GetMethod("HandleTileGrowth", BindingFlags.Static | BindingFlags.Public);
            if (method is null) {
                return;
            }
            VaultHook.Add(method, On_HandleTileGrowth);
        }
    }
}
