using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.CollisionMask;
using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures
{
    /// <summary>
    /// 虚空聚落建筑Actor
    /// 用于承载整张建筑贴图的静态装饰实体，贴图绘制由shader负责
    /// 过去时代实体显像时通过<see cref="CollisionMask.ArchitectureTilePlacer"/>就地放置隐形碰撞tile，形成与原版完全兼容的物理
    /// </summary>
    internal class ArchitectureActor : Actor
    {
        /// <summary>建筑类型，使用byte存储以便于SyncVar传输</summary>
        [SyncVar]
        public byte TypeByte;

        /// <summary>贴图是否水平翻转，斜桥/阶梯可用于实现右下→左上走向</summary>
        [SyncVar]
        public bool FlipX;

        /// <summary>被雷击后过电滤镜剩余帧数，服务端写入后同步到客户端</summary>
        [SyncVar]
        public int ElectrifyTimer;
        /// <summary>本次过电总时长，供进度归一化使用</summary>
        [SyncVar]
        public int ElectrifyMax;
        /// <summary>本次过电的随机种子，传给shader避免每次都一样</summary>
        [SyncVar]
        public float ElectrifySeed;

        /// <summary>是否已根据贴图尺寸完成初始化</summary>
        private bool initialized;

        /// <summary>
        /// 当前可见度，过去时逼近1，现在时逼近0
        /// 初始为0以便玩家首次进入过去时也能播一次扭曲浮现
        /// </summary>
        private float visibility;

        /// <summary>已放置的隐形碰撞tile坐标列表，供擦除时精准回收</summary>
        private readonly List<Point16> placedCollisionTiles = [];
        /// <summary>当前是否处于"碰撞tile已写入世界"的状态</summary>
        private bool collisionPlaced;
        //碰撞显现/消失的可见度阈值，与shader的凝实感同步
        private const float CollisionThreshold = 0.5f;

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
                //离开维度前先回收已放置的碰撞方块，防止残留
                ArchitectureTilePlacer.Clear(placedCollisionTiles);
                collisionPlaced = false;
                ActorLoader.KillActor(WhoAmI);
                return;
            }
            ArchitectureWarpDraw.TickVisibility(ref visibility);
            UpdateCollision();
            //过电计时逐帧递减，服务端与客户端各自消耗以减少同步频率
            if (ElectrifyTimer > 0) ElectrifyTimer--;
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

        /// <summary>
        /// 根据可见度状态切换碰撞tile的存在与否
        /// 只在主机与单机上执行放置/清除，客户端会自动通过SendTileSquare接收同步
        /// </summary>
        private void UpdateCollision() {
            bool shouldPlace = visibility >= CollisionThreshold;
            if (shouldPlace == collisionPlaced) return;

            if (shouldPlace) {
                string[] mask = ArchitectureCollisionMask.Get(Type);
                if (mask == null) { collisionPlaced = true; return; }
                //FlipX时对每一行做字符翻转，得到水平镜像后的碰撞蒙版
                if (FlipX) mask = MirrorMaskRows(mask);
                int tileX = (int)Math.Round(Position.X / 16f);
                int tileY = (int)Math.Round(Position.Y / 16f);
                ArchitectureTilePlacer.Place(mask, tileX, tileY, placedCollisionTiles);
                collisionPlaced = true;
            }
            else {
                ArchitectureTilePlacer.Clear(placedCollisionTiles);
                collisionPlaced = false;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            Texture2D tex = ArchitectureAsset.Get(Type);
            if (tex == null) return false;
            if (!ArchitectureWarpDraw.ShouldDraw(visibility)) return false;

            float warp = ArchitectureWarpDraw.ComputeWarp();
            Vector2 drawPos = Position - Main.screenPosition;

            ArchitectureWarpDraw.DrawWithShader(spriteBatch, tex, drawPos, visibility, warp, FlipX);

            //过电滤镜：在建筑之上再以Additive混合跑一遍过电shader，值随计时器衰减
            if (ElectrifyTimer > 0 && ElectrifyMax > 0) {
                DrawElectrifiedOverlay(spriteBatch, tex, drawPos);
            }
            return false;
        }

        /// <summary>
        /// 在建筑贴图上额外绘制一层过电辉光，Additive混合确保不抹黑背景
        /// </summary>
        private void DrawElectrifiedOverlay(SpriteBatch spriteBatch, Texture2D tex, Vector2 drawPos) {
            Effect shader = CalamityOverhaul.Common.EffectLoader.SignalTowerElectrified?.Value;
            if (shader == null) return;

            float progress = 1f - ElectrifyTimer / (float)ElectrifyMax;
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["electrifyProgress"]?.SetValue(MathHelper.Clamp(progress, 0f, 1f));
            shader.Parameters["seed"]?.SetValue(ElectrifySeed);
            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));
            shader.Parameters["intensity"]?.SetValue(MathHelper.Clamp(visibility, 0f, 1f));

            SpriteEffects fx = FlipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(tex, drawPos, null, Color.White, 0f, Vector2.Zero, 1f, fx, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// 按行字符反转水平镜像蒙版，配合FlipX贴图保持碰撞与画面一致
        /// </summary>
        private static string[] MirrorMaskRows(string[] mask) {
            string[] result = new string[mask.Length];
            for (int i = 0; i < mask.Length; i++) {
                string row = mask[i] ?? string.Empty;
                char[] buf = row.ToCharArray();
                System.Array.Reverse(buf);
                result[i] = new string(buf);
            }
            return result;
        }
    }
}

