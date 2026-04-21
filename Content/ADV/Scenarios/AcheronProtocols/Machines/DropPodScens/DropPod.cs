using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace CalamityOverhaul.Content.ADV.Scenarios.AcheronProtocols.Machines.DropPodScens
{
    internal class DropPod
    {
        [VaultLoaden(CWRConstant.ADV + "Draedon/DropPod")]
        public static Asset<Texture2D> DropPodAsset = null!;
    }
}
