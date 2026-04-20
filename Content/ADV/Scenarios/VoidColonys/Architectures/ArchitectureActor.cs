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

        /// <summary>
        /// 当前可见度，过去时逼近1，现在时逼近0
        /// 初始为0以便玩家首次进入过去时也能播一次扭曲浮现
        /// </summary>
        private float visibility;

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
            ArchitectureWarpDraw.TickVisibility(ref visibility);
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
            if (!ArchitectureWarpDraw.ShouldDraw(visibility)) return false;

            float warp = ArchitectureWarpDraw.ComputeWarp();
            Vector2 drawPos = Position - Main.screenPosition;
            ArchitectureWarpDraw.DrawWithShader(spriteBatch, tex, drawPos, visibility, warp);
            return false;
        }
    }
}
