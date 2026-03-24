using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Skills.Sandevistans
{
    /// <summary>
    /// 斯安威斯坦屏幕级渲染器（预留）。
    /// 残影的逐帧绘制已移至 <see cref="SandevistanGhostActor.PreDraw"/>，
    /// 使用 <see cref="InnoVault.Actors.ActorDrawLayer.BeforePlayers"/> 层级确保在玩家身后绘制。
    /// 此 RenderHandle 保留用于未来的屏幕级后处理效果（如色差、径向模糊等全局效果）。
    /// </summary>
    internal class SandevistanRender : RenderHandle
    {
        public override void EndCaptureDraw(SpriteBatch sb, GraphicsDevice gd, RenderTarget2D screenSwap) {
            if (!Sandevistan.IsActive) {
                return;
            }
            //预留：激活状态下的屏幕级后处理效果（色差、时间扭曲等）
        }
    }
}
