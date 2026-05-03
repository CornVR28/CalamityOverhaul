using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Modules
{
    /// <summary>
    /// SHPC 改件物品基类，提供槽位类别声明与对 <see cref="ShootContext"/> 的修改入口
    /// 子类只需覆写 <see cref="SlotCategory"/> 与 <see cref="Apply"/>
    /// </summary>
    internal abstract class SHPCModuleItem : ModItem
    {
        /// <summary>
        /// 该改件能装入的槽位类别
        /// </summary>
        public abstract SHPCSlotCategory SlotCategory { get; }

        /// <summary>
        /// 改件作用：修改传入的 <see cref="ShootContext"/>，对浮点倍率字段使用加算叠加（增量 += delta）
        /// </summary>
        public abstract void Apply(ref ShootContext ctx);

        /// <summary>
        /// 改件属性差值文字列表，自动对 <see cref="Apply"/> 前后的 <see cref="ShootContext"/> 做 diff
        /// 数值来自代码，本地化只提供字段名模板（含 {0} 占位符）
        /// 子类无需覆写此方法
        /// </summary>
        public virtual IEnumerable<string> GetStatLines() {
            ShootContext ctx = ShootContext.Default;
            Apply(ref ctx);
            return BuildStatLines(ctx);
        }

        private static IEnumerable<string> BuildStatLines(ShootContext ctx) {
            if (ctx.MergeBeams)
                yield return Language.GetTextValue("Mods.CalamityOverhaul.Legend.SHPCModuleStat.MergeBeams");
            foreach (string s in FloatStat("AttackSpeed", ctx.AttackSpeedMul)) yield return s;
            foreach (string s in FloatStat("Damage", ctx.DamageMul)) yield return s;
            foreach (string s in FloatStat("Spread", ctx.SpreadMul)) yield return s;
            foreach (string s in FloatStat("BeamSpeed", ctx.BeamSpeedMul)) yield return s;
            foreach (string s in FloatStat("Homing", ctx.HomingMul)) yield return s;
            foreach (string s in FloatStat("MergedDamage", ctx.MergedDamageBonus)) yield return s;
            foreach (string s in FloatStat("ManaCost", ctx.ManaCostMul)) yield return s;
            foreach (string s in FloatStat("ChargeTime", ctx.ChargeTimeMul)) yield return s;
            foreach (string s in FloatStat("OrbSpeed", ctx.OrbSpeedMul)) yield return s;
            if (ctx.BeamCountAdd != 0) yield return IntStat("BeamCount", ctx.BeamCountAdd);
            if (ctx.CritAdd != 0) yield return IntStat("Crit", ctx.CritAdd);
        }

        private static IEnumerable<string> FloatStat(string key, float mulValue) {
            float delta = mulValue - 1f;
            if (MathF.Abs(delta) < 0.001f) yield break;
            int pct = (int)MathF.Round(delta * 100f);
            //正数补 + 号，负数 pct 自带 - 号，sign 对负数为空串
            string sign = pct > 0 ? "+" : "";
            yield return Language.GetTextValue($"Mods.CalamityOverhaul.Legend.SHPCModuleStat.{key}", $"{sign}{pct}");
        }

        private static string IntStat(string key, int value) {
            string sign = value > 0 ? "+" : "";
            return Language.GetTextValue($"Mods.CalamityOverhaul.Legend.SHPCModuleStat.{key}", $"{sign}{value}");
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            int idx = 0;
            foreach (string line in GetStatLines()) {
                if (string.IsNullOrEmpty(line)) continue;
                bool isNeg = line.StartsWith("-");
                tooltips.Add(new TooltipLine(Mod, $"SHPCStat{idx++}", line) {
                    OverrideColor = isNeg ? new Color(255, 120, 110) : new Color(120, 255, 170)
                });
            }
        }

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
