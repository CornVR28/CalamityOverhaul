using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Banish;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.DomainFreeze
{
    /// <summary>
    /// 赛博领域冻结NPC绘制拦截器
    /// <br/>在PreDraw中切换SpriteBatch到Immediate模式并应用冻结故障+六角覆盖着色器
    /// <br/>让原版绘制逻辑在着色器生效状态下执行，PostDraw中恢复
    /// </summary>
    internal class CyberDomainFreezeNPCDraw : GlobalNPC
    {
        private static bool _shaderActive;

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (!ShouldApplyEffect(npc)) return true;

            Effect shader = CyberDomainFreezeAssets.CyberFreezeEntity;
            if (shader == null) return true;

            float progress = CyberDomainFreeze.GetNPCFreezeProgress(npc.whoAmI);
            if (progress < 0f) return true;

            Texture2D tex = TextureAssets.Npc[npc.type].Value;

            float seed = CyberDomainFreeze.GetNPCSeed(npc.whoAmI);

            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["progress"]?.SetValue(progress);
            shader.Parameters["intensity"]?.SetValue(Cyberspace.Intensity);
            shader.Parameters["seed"]?.SetValue(seed);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();

            _shaderActive = true;
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (!_shaderActive) return;
            _shaderActive = false;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        public override bool PreAI(NPC npc) {
            if (!CyberDomainFreeze.IsNPCFrozen(npc.whoAmI)) return true;

            // 获取冻结位置快照
            for (int i = 0; i < CyberDomainFreeze.FrozenNPCs.Count; i++) {
                if (CyberDomainFreeze.FrozenNPCs[i].EntityIndex == npc.whoAmI) {
                    npc.Center = CyberDomainFreeze.FrozenNPCs[i].FreezePosition;
                    break;
                }
            }

            npc.velocity = Vector2.Zero;
            npc.frameCounter = 0;
            npc.timeLeft++;
            return false;
        }

        private static bool ShouldApplyEffect(NPC npc) {
            if (!CyberDomainFreeze.IsNPCFrozen(npc.whoAmI)) return false;
            // 正在被放逐的NPC由放逐着色器处理，不叠加冻结着色器
            if (CyberBanish.IsBanishing(npc.whoAmI)) return false;
            return true;
        }
    }
}
