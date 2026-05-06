using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime
{
    internal class KingSlimeEffect : ModSystem
    {
        public override void PostUpdateEverything() {
            if (NPC.AnyNPCs(NPCID.KingSlime)) {
                Main.newMusic = Main.musicBox2 = MusicLoader.GetMusicSlot("CalamityOverhaul/Assets/Sounds/Music/KingSlime");
            }
        }
    }
}
