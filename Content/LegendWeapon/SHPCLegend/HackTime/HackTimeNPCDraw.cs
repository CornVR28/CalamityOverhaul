using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime
{
    //骇客时间NPC绘制拦截器
    //在PreDraw中切换SpriteBatch到Immediate模式并应用高亮着色器
    //让原版绘制逻辑在着色器生效的状态下执行，PostDraw中恢复
    internal class HackTimeNPCDraw : GlobalNPC
    {
        //标记当前帧是否已激活着色器（用于PostDraw恢复判断）
        private static bool _shaderActive;

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (!ShouldApplyEffect(npc)) return true;

            Effect shader = HackTimeAssets.HackTimeNPCHighlight;
            if (shader == null) return true;

            bool isSelected = npc.whoAmI == HackTime.SelectedTargetIndex;
            float effectStr = HackTime.Intensity;

            //获取NPC纹理用于计算texelSize
            Texture2D tex = TextureAssets.Npc[npc.type].Value;

            //设置着色器参数
            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));
            shader.Parameters["intensity"]?.SetValue(effectStr);
            shader.Parameters["isSelected"]?.SetValue(isSelected ? 1f : 0f);
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);

            //切换SpriteBatch到Immediate模式以激活像素着色器
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();

            _shaderActive = true;
            return true; //让原版继续绘制NPC（此时着色器已生效）
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (!_shaderActive) return;
            _shaderActive = false;

            //恢复SpriteBatch到Deferred模式
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static bool ShouldApplyEffect(NPC npc) {
            if (!HackTime.Active && HackTime.Intensity < 0.01f) return false;
            // 正在被放逐的NPC由放逐着色器处理，不叠加HackTime高亮
            if (Cyberspaces.Banish.CyberBanish.IsBanishing(npc.whoAmI)) return false;
            // 正在被领域冻结的NPC由冻结着色器处理
            if (Cyberspaces.DomainFreeze.CyberDomainFreeze.IsNPCFrozen(npc.whoAmI)) return false;
            return npc.whoAmI == HackTime.SelectedTargetIndex
                || npc.whoAmI == HackTime.HoveredTargetIndex;
        }
    }
}
