using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    /// <summary>
    /// SHPC 改件物品基类，提供槽位类别声明与对 <see cref="ShootContext"/> 的修改入口
    /// 子类只需覆写 <see cref="SlotCategory"/>、<see cref="Apply"/>、<see cref="GetStatLines"/>
    /// </summary>
    internal abstract class SHPCModuleItem : ModItem
    {
        /// <summary>
        /// 该改件能装入的槽位类别
        /// </summary>
        public abstract SHPCSlotCategory SlotCategory { get; }

        /// <summary>
        /// 改件作用：修改传入的 <see cref="ShootContext"/>，按字段进行乘加叠加
        /// </summary>
        public abstract void Apply(ref ShootContext ctx);

        /// <summary>
        /// 改件属性差值文字列表（用于自定义悬浮描述框）
        /// 每行格式形如 "+50% ATK SPD" / "-30% DMG"，颜色由 UI 层根据正负选择
        /// </summary>
        public virtual IEnumerable<string> GetStatLines() => System.Array.Empty<string>();

        /// <summary>
        /// 赛博朋克滤镜识别色，用于<see cref="SHPCModuleRender"/>对图标做双调色与边缘霓虹描边
        /// 缺省为青色，子类按风味自由覆写以做区分
        /// </summary>
        public virtual Color TintColor => new(0, 220, 255);

        /// <summary>
        /// 滤镜强度，缺省1.0，可在子类中调低以保留更多原贴图特征
        /// </summary>
        public virtual float TintIntensity => 1f;

        public override void SetDefaults() {
            Item.maxStack = 1;
            Item.width = 32;
            Item.height = 32;
            Item.rare = Terraria.ID.ItemRarityID.Yellow;
            Item.value = Item.sellPrice(0, 2, 0, 0);
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position
            , Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
            //背包绘制走UI变换矩阵，按识别色做双调色重映射
            Texture2D tex = TextureAssets.Item[Item.type]?.Value;
            if (tex == null) {
                return true;
            }
            Vector2 texSize = new(tex.Width, tex.Height);
            if (!SHPCModuleRender.Begin(spriteBatch, TintColor, texSize, Main.UIScaleMatrix, TintIntensity)) {
                return true;
            }
            spriteBatch.Draw(tex, position, frame, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
            SHPCModuleRender.End(spriteBatch);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor
            , ref float rotation, ref float scale, int whoAmI) {
            //世界掉落物使用游戏视角矩阵保持滤镜随屏幕缩放
            Texture2D tex = TextureAssets.Item[Item.type]?.Value;
            if (tex == null) {
                return true;
            }
            Rectangle frame = Main.itemAnimations[Item.type] != null
                ? Main.itemAnimations[Item.type].GetFrame(tex)
                : tex.Bounds;
            Vector2 texSize = new(tex.Width, tex.Height);
            Vector2 drawPos = Item.Center - Main.screenPosition;
            Vector2 origin = new(frame.Width * 0.5f, frame.Height * 0.5f);
            Matrix transform = Main.GameViewMatrix.TransformationMatrix;
            if (!SHPCModuleRender.Begin(spriteBatch, TintColor, texSize, transform, TintIntensity)) {
                return true;
            }
            spriteBatch.Draw(tex, drawPos, frame, lightColor, rotation, origin, scale, SpriteEffects.None, 0f);
            SHPCModuleRender.End(spriteBatch);
            return false;
        }
    }
}
