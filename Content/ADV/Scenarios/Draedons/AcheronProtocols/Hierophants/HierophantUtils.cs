using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants
{
    /// <summary>
    /// 本地工具类，提供 Hierophant 系列 NPC 所需的通用数学与绘制辅助方法
    /// </summary>
    internal static class HierophantUtils
    {
        private static readonly Dictionary<string, Texture2D> _texCache = [];

        internal static void ClearCache() => _texCache.Clear();

        #region Math

        public static float Distance(Vector2 a, Vector2 b) => Vector2.Distance(a, b);

        public static Vector2 SafeNormalize(this Vector2 v) => v.SafeNormalize(Vector2.Zero);

        public static float RotateTowardsAngle(float current, float target, float speed, bool fixedSpeed = true) {
            current = MathHelper.WrapAngle(current);
            target = MathHelper.WrapAngle(target);

            float diff = MathHelper.WrapAngle(target - current);
            if (fixedSpeed) {
                diff = MathHelper.Clamp(diff, -speed, speed);
            }
            else {
                diff *= MathHelper.Clamp(speed, 0f, 1f);
            }
            return current + diff;
        }

        public static float GetAngleBetweenVectors(Vector2 a, Vector2 b) {
            float dot = Vector2.Dot(a, b);
            float magA = a.Length();
            float magB = b.Length();
            if (magA == 0 || magB == 0) return 0;
            return MathF.Acos(MathHelper.Clamp(dot / (magA * magB), -1f, 1f));
        }

        public static Vector2 GetCircleIntersection(Vector2 c1, float r1, Vector2 c2, float r2, bool flag = false, bool flag2 = false) {
            float d = Vector2.Distance(c1, c2);
            if (d > r1 + r2 || d < MathF.Abs(r1 - r2)) {
                Vector2 dir = Vector2.Normalize(c2 - c1);
                return c1 + dir * r1;
            }

            float l = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
            float h = MathF.Sqrt(r1 * r1 - l * l);

            Vector2 p0 = c1 + (l / d) * (c2 - c1);
            Vector2 i1 = new(p0.X + (h / d) * (c2.Y - c1.Y), p0.Y - (h / d) * (c2.X - c1.X));
            Vector2 i2 = new(p0.X - (h / d) * (c2.Y - c1.Y), p0.Y + (h / d) * (c2.X - c1.X));

            if (flag2) return flag ? i1 : i2;
            return i1.Y < i2.Y ? i1 : i2;
        }

        public static Vector2 RandomPointInCircle(float radius) {
            return Main.rand.NextVector2Unit() * Main.rand.NextFloat(-radius, radius);
        }

        public static float RandomRotation() => Main.rand.NextFloat(MathHelper.TwoPi);

        public static Rectangle GetRectCentered(this Vector2 center, float w, float h) {
            return new Rectangle((int)(center.X - w / 2f), (int)(center.Y - h / 2f), (int)w, (int)h);
        }

        #endregion

        #region Tile

        public static bool InWorld(int i, int j) {
            return i >= 0 && j >= 0 && i < Main.tile.Width && j < Main.tile.Height;
        }

        public static bool IsAir(Vector2 worldPos, bool includePlatforms = false) {
            if (worldPos.X < 0 || worldPos.Y < 0) return true;
            int tx = (int)(worldPos.X / 16);
            int ty = (int)(worldPos.Y / 16);
            if (tx >= Main.tile.Width || ty >= Main.tile.Height) return false;

            Tile tile = Main.tile[tx, ty];
            if (tile == null || !tile.HasTile) return true;

            if (includePlatforms) {
                return !(Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
            }
            return !(Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType]);
        }

        public static bool CheckSolidTile(Rectangle rect) {
            if (rect.Y + rect.Height > Main.maxTilesY * 16) return true;
            return Collision.SolidCollision(rect.TopLeft(), rect.Width, rect.Height);
        }

        #endregion

        #region Texture

        public static Texture2D RequestTex(string path) {
            if (!_texCache.TryGetValue(path, out var tex)) {
                tex = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
                _texCache[path] = tex;
            }
            return tex;
        }

        public static Texture2D GetNpcTexture(this NPC npc) => TextureAssets.Npc[npc.type].Value;

        #endregion

        #region Drawing

        public static void DrawChain(Vector2 startPos, Vector2 endPos, int spacing, Texture2D tex, Color color) {
            float dist = Vector2.Distance(startPos, endPos);
            float rot = (endPos - startPos).ToRotation();
            int num = (int)(dist / spacing);
            if (num <= 0) return;
            Vector2 step = (endPos - startPos) / num;
            step.Normalize();
            Vector2 drawPos = startPos;

            for (int i = 0; i <= num; i++) {
                Color lightColor = Lighting.GetColor((drawPos / 16f).ToPoint());
                Main.EntitySpriteDraw(tex, drawPos - Main.screenPosition, null, lightColor,
                    rot, new Vector2(tex.Width / 2f, tex.Height / 2f), Vector2.One, SpriteEffects.None, 0);
                drawPos += step * spacing;
            }
        }

        public static void DrawChain(Vector2 startPos, Vector2 endPos, int spacing, string texturePath) {
            DrawChain(startPos, endPos, spacing, RequestTex(texturePath), Color.White);
        }

        #endregion

        #region NPC Helpers

        public static NPC ToNPC(this int index) {
            if (index < 0 || index >= Main.npc.Length || !Main.npc[index].active) return null;
            return Main.npc[index];
        }

        public static Player ToPlayer(this int index) {
            if (index < 0 || index >= Main.player.Length || !Main.player[index].active) return Main.LocalPlayer;
            return Main.player[index];
        }

        #endregion
    }
}
