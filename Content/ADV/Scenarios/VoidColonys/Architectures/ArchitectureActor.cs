using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures
{
    /// <summary>
    /// 虚空聚落建筑Actor
    /// 用于承载整张建筑贴图的静态装饰实体，不参与碰撞，只负责绘制
    /// 通过<see cref="TypeByte"/>决定贴图，在首次AI时根据贴图尺寸补齐Width和Height
    /// </summary>
    internal class ArchitectureActor : Actor
    {
        /// <summary>建筑类型，使用byte存储以便于SyncVar传输</summary>
        [SyncVar]
        public byte TypeByte;

        /// <summary>是否已根据贴图尺寸完成初始化</summary>
        private bool initialized;

        public ArchitectureType Type => (ArchitectureType)TypeByte;

        public override void OnSpawn(params object[] args) {
            DrawLayer = ActorDrawLayer.AfterTiles;
            ApplyTextureSize();
        }

        public override void AI() {
            //首次拿到贴图后锁定尺寸，确保客户端通过SyncVar同步到TypeByte后也能正确设置
            if (!initialized) {
                ApplyTextureSize();
            }
            if (!VoidColony.Active) {
                ActorLoader.KillActor(WhoAmI);
                return;
            }
            Velocity = Vector2.Zero;
        }

        private void ApplyTextureSize() {
            Texture2D tex = ArchitectureAsset.Get(Type);
            if (tex == null) return;
            Width = tex.Width;
            Height = tex.Height;
            //扩展绘制范围到贴图的最长边，避免建筑中心离开屏幕时被裁掉
            DrawExtendMode = Math.Max(Width, Height);
            initialized = true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            Texture2D tex = ArchitectureAsset.Get(Type);
            if (tex == null) return false;

            //按贴图中心采样光照，保证整座建筑拿到统一但合理的亮度
            int lx = (int)((Position.X + tex.Width * 0.5f) / 16f);
            int ly = (int)((Position.Y + tex.Height * 0.5f) / 16f);
            lx = Math.Clamp(lx, 0, Main.maxTilesX - 1);
            ly = Math.Clamp(ly, 0, Main.maxTilesY - 1);
            Color lightColor = Lighting.GetColor(lx, ly);

            Vector2 drawPos = Position - Main.screenPosition;
            spriteBatch.Draw(tex, drawPos, null, lightColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
