using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace CalamityOverhaul.Content.RangedModify.UI
{
    internal class ReloadingProgressUI : UIHandle
    {
        public override LayersModeEnum LayersMode => LayersModeEnum.Vanilla_Interface_Logic_1;
        public override bool Active => false;
        [VaultLoaden(CWRConstant.UI + "ReloadingProgress")]
        internal static Asset<Texture2D> Glow { get; private set; }
        [VaultLoaden(CWRConstant.UI + "ReloadingProgressFull")]
        internal static Asset<Texture2D> Full { get; private set; }
        public override void Update() { }
        public override void Draw(SpriteBatch spriteBatch) { }
    }
}
