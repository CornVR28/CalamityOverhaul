using CalamityOverhaul.Common;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    /// <summary>
    /// 启示录瘟疫状态
    /// 被瘟疫骑士赐福的攻击会给敌人挂上瘟疫侵蚀和专属滤镜
    /// </summary>
    internal class RevelationPlagueNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public int PlagueTime;
        public int PlagueMaxTime;
        public float PlagueIntensity;
        public int PlagueOwner = -1;

        public static void Apply(NPC npc, int time, float intensity, int owner) {
            if (npc == null || !npc.active) {
                return;
            }

            RevelationPlagueNPC plague = npc.GetGlobalNPC<RevelationPlagueNPC>();
            plague.PlagueTime = System.Math.Max(plague.PlagueTime, time);
            plague.PlagueMaxTime = System.Math.Max(plague.PlagueMaxTime, time);
            plague.PlagueIntensity = System.Math.Max(plague.PlagueIntensity, intensity);
            plague.PlagueOwner = owner;
        }

        public static bool IsPlagued(NPC npc) {
            return npc != null && npc.active && npc.GetGlobalNPC<RevelationPlagueNPC>().PlagueTime > 0;
        }

        public override void AI(NPC npc) {
            if (PlagueTime > 0) {
                PlagueTime--;
                if (PlagueTime <= 0) {
                    PlagueTime = 0;
                    PlagueMaxTime = 0;
                    PlagueIntensity = 0f;
                    PlagueOwner = -1;
                }
            }
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage) {
            if (PlagueTime <= 0) {
                return;
            }

            if (npc.lifeRegen > 0) {
                npc.lifeRegen = 0;
            }

            int plagueDps = (int)(18 * PlagueIntensity);
            npc.lifeRegen -= plagueDps;
            if (damage < plagueDps / 4) {
                damage = plagueDps / 4;
            }
        }

        public override void DrawEffects(NPC npc, ref Color drawColor) {
            if (PlagueTime <= 0) {
                return;
            }

            float pulse = 0.7f + (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 8f + npc.whoAmI) * 0.3f;
            drawColor = Color.Lerp(drawColor, new Color(120, 220, 90), 0.18f * pulse);

            if (Main.rand.NextBool(10)) {
                Vector2 velocity = Main.rand.NextVector2Circular(1.5f, 1.5f);
                Dust dust = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(npc.width * 0.4f, npc.height * 0.4f), DustID.CorruptGibs, velocity, 120, new Color(130, 220, 90), 1f);
                dust.noGravity = true;
            }
        }
    }

    internal class RevelationPlagueNPCDraw : GlobalNPC
    {
        private static bool shaderActive;

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            RevelationPlagueNPC plague = npc.GetGlobalNPC<RevelationPlagueNPC>();
            if (plague.PlagueTime <= 0) {
                return true;
            }

            Effect shader = EffectLoader.RevelationPlague?.Value;
            if (shader == null) {
                return true;
            }

            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            float progress = plague.PlagueMaxTime <= 0 ? 1f : plague.PlagueTime / (float)plague.PlagueMaxTime;
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));
            shader.Parameters["intensity"]?.SetValue(plague.PlagueIntensity);
            shader.Parameters["progress"]?.SetValue(progress);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);
            shader.CurrentTechnique.Passes[0].Apply();

            shaderActive = true;
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (!shaderActive) {
                return;
            }

            shaderActive = false;
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
