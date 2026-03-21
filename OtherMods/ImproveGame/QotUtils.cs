using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.OtherMods.ImproveGame;

public static class QotUtils
{
    public static bool QotLoaded => ModLoader.HasMod("ImproveGame");
    public static Mod QotInstance => ModLoader.GetMod("ImproveGame");

    public static List<Item> GetBigBagItems(this Player player) {
        return !QotLoaded ? null : (List<Item>)QotInstance.Call("GetBigBagItems", player);
    }
}