using CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors.States;
using CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines;
using InnoVault.Actors;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors
{
    /// <summary>
    /// 阿波利娅角色Actor——通过 <see cref="IApolliaState"/> 驱动行为，
    /// 运镜由 <see cref="CutsceneCamera"/> 管理并在 <see cref="ApolliaPlayer"/> 中应用
    /// </summary>
    internal class ApolliaActor : Actor
    {
        private const int TotalFrames = 11;

        #region 状态机

        /// <summary>当前行为状态实例，null 表示未激活</summary>
        internal IApolliaState CurrentState { get; private set; }

        /// <summary>
        /// 切换到新状态——自动调用旧状态的Exit和新状态的Enter
        /// </summary>
        internal void TransitionTo(IApolliaState newState) {
            CurrentState?.Exit(this);
            CurrentState = newState;
            CurrentState?.Enter(this);
        }

        #endregion

        #region 公开属性——供状态类读写

        /// <summary>运镜系统实例——由 <see cref="ApolliaPlayer"/> 读取并在ModifyScreenPosition中应用</summary>
        internal CutsceneCamera Camera { get; } = new();

        /// <summary>当前动画帧索引 (0=站立, 1~10=行走)</summary>
        internal int FrameIndex;

        /// <summary>行走朝向 (-1=左, 1=右)</summary>
        internal int WalkDirection;

        /// <summary>降落阶段的透明度 (0~1)</summary>
        internal float DescendAlpha;

        /// <summary>着陆辉光强度 (0~1)</summary>
        internal float GlowIntensity;

        /// <summary>帧纹理的单帧宽度</summary>
        internal int FrameWidth { get; private set; }

        /// <summary>帧纹理的单帧高度</summary>
        internal int FrameHeight { get; private set; }

        /// <summary>速度向量——供状态类进行物理运动</summary>
        internal Vector2 Velocity;

        /// <summary>角色是否站在实心地面上</summary>
        internal bool OnGround;

        /// <summary>是否使用跳跃/飞行单帧纹理进行绘制</summary>
        internal bool UseJumpTexture;

        #endregion

        #region 指令访问

        /// <summary>读取英雄面板上的当前指令</summary>
        internal HeroCommand CurrentCommand =>
            ApolliaHeroPanelUI.Instance?.HeroData?.CurrentCommand ?? HeroCommand.Follow;

        #endregion

        public override void OnSpawn(params object[] args) {
            Width = 30;
            Height = 50;
            DrawExtendMode = 600;
            DrawLayer = ActorDrawLayer.Default;

            FrameIndex = 0;
            WalkDirection = 1;
            DescendAlpha = 0f;
            GlowIntensity = 0f;
            Velocity = Vector2.Zero;
            OnGround = false;
            UseJumpTexture = false;
            Camera.Reset();

            if (ADVAsset.ApolliaActor != null) {
                FrameWidth = ADVAsset.ApolliaActor.Width;
                FrameHeight = ADVAsset.ApolliaActor.Height / TotalFrames;
            }
        }

        /// <summary>
        /// 外部触发：开始着陆演出
        /// </summary>
        internal void StartLandingCutscene(Vector2 landingPodCenter) {
            if (CurrentState != null) return;

            float offsetX = 200f;
            Vector2 rawTarget = new Vector2(landingPodCenter.X + offsetX, landingPodCenter.Y);
            Vector2 groundPos = FindGroundPosition(rawTarget);

            TransitionTo(new ApolliaDescendingState(groundPos));
        }

        public override void AI() {
            if (!MachineWorld.Active) {
                Camera.Reset();
                ActorLoader.KillActor(WhoAmI);
                return;
            }

            //状态机驱动
            if (CurrentState != null) {
                IApolliaState next = CurrentState.Update(this);
                if (next != null) {
                    TransitionTo(next);
                }
            }

            //辉光衰减
            if (GlowIntensity > 0.01f) {
                GlowIntensity *= 0.96f;
            }
        }

        #region 物理辅助——供状态类调用

        /// <summary>
        /// 将角色吸附到脚下最近的实心地面上，并更新 <see cref="OnGround"/> 状态
        /// </summary>
        internal void SnapToGround() {
            int tileX = (int)(Center.X / 16f);
            int tileY = (int)((Position.Y + Height) / 16f);

            for (int y = tileY; y < tileY + 10; y++) {
                if (!WorldGen.InWorld(tileX, y)) break;
                Tile tile = Framing.GetTileSafely(tileX, y);
                if (tile.HasTile && Main.tileSolid[tile.TileType]) {
                    float groundY = y * 16f - Height;
                    if (Position.Y < groundY) {
                        Position.Y = MathHelper.Lerp(Position.Y, groundY, 0.3f);
                        OnGround = Math.Abs(Position.Y - groundY) < 2f;
                    }
                    else {
                        Position.Y = groundY;
                        OnGround = true;
                    }
                    return;
                }
            }

            OnGround = false;
            Position.Y += 4f;
        }

        /// <summary>
        /// 检测角色前方是否有实心墙壁阻挡 (检测碰撞体前方2格高度)
        /// </summary>
        internal bool IsWallAhead(int direction) {
            int checkX = (int)((Center.X + direction * (Width * 0.5f + 8f)) / 16f);
            int footY = (int)((Position.Y + Height - 4f) / 16f);

            for (int y = footY; y >= footY - 2; y--) {
                if (!WorldGen.InWorld(checkX, y)) continue;
                Tile tile = Framing.GetTileSafely(checkX, y);
                if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType]) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检测角色前方脚下是否存在深坑 (超过6格没有地面)
        /// </summary>
        internal bool IsGapAhead(int direction) {
            int checkX = (int)((Center.X + direction * (Width * 0.5f + 16f)) / 16f);
            int footY = (int)((Position.Y + Height) / 16f);

            for (int y = footY; y < footY + 6; y++) {
                if (!WorldGen.InWorld(checkX, y)) return true;
                Tile tile = Framing.GetTileSafely(checkX, y);
                if (tile.HasTile && Main.tileSolid[tile.TileType]) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 应用重力和速度到位置，用于非吸附式的物理运动
        /// </summary>
        internal void ApplyGravity(float gravity = 0.4f, float maxFallSpeed = 12f) {
            Velocity.Y = Math.Min(Velocity.Y + gravity, maxFallSpeed);
            Position += Velocity;
        }

        internal static Vector2 FindGroundPosition(Vector2 startPos) {
            int tileX = (int)(startPos.X / 16f);
            int startTileY = (int)(startPos.Y / 16f);

            for (int y = startTileY - 50; y < startTileY + 100; y++) {
                if (!WorldGen.InWorld(tileX, y)) continue;
                Tile tile = Framing.GetTileSafely(tileX, y);
                if (tile.HasTile && Main.tileSolid[tile.TileType]) {
                    return new Vector2(startPos.X, y * 16f - 50f);
                }
            }

            return startPos;
        }

        #endregion

        #region 绘制

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            if (CurrentState == null) return false;

            SpriteEffects fx = WalkDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float alpha = DescendAlpha < 1f && CurrentState is ApolliaDescendingState ? DescendAlpha : 1f;
            Color bodyColor = Lighting.GetColor((int)(Center.X / 16), (int)(Center.Y / 16)) * alpha;
            Vector2 drawPos = new Vector2(Center.X, Position.Y + Height) - Main.screenPosition;

            //选择纹理：飞行/跳跃时使用单帧Jump纹理，否则使用行走帧图
            if (UseJumpTexture && ADVAsset.ApolliaActor_Jump != null) {
                Texture2D jumpTex = ADVAsset.ApolliaActor_Jump;
                Vector2 jumpOrigin = new Vector2(jumpTex.Width * 0.5f, jumpTex.Height);

                //辉光
                DrawGlow(spriteBatch, drawPos, alpha);

                spriteBatch.Draw(jumpTex, drawPos, null, bodyColor, 0f, jumpOrigin, 0.7f, fx, 0f);
                return false;
            }

            if (ADVAsset.ApolliaActor == null) return false;

            Texture2D tex = ADVAsset.ApolliaActor;
            if (FrameWidth <= 0 || FrameHeight <= 0) return false;

            int clampedFrame = Math.Clamp(FrameIndex, 0, TotalFrames - 1);
            Rectangle sourceRect = new Rectangle(0, clampedFrame * FrameHeight, FrameWidth, FrameHeight);
            Vector2 origin = new Vector2(FrameWidth * 0.5f, FrameHeight);

            //辉光效果
            DrawGlow(spriteBatch, drawPos, alpha);

            //降落阶段电弧边缘光
            if (CurrentState is ApolliaDescendingState && DescendAlpha > 0.3f) {
                Color edgeColor = new Color(100, 200, 255) * (DescendAlpha * 0.4f);
                for (int i = 0; i < 4; i++) {
                    float angle = MathHelper.PiOver2 * i;
                    Vector2 off = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 2f;
                    spriteBatch.Draw(tex, drawPos + off, sourceRect, edgeColor, 0f, origin, 0.7f, fx, 0f);
                }
            }

            //主体
            spriteBatch.Draw(tex, drawPos, sourceRect, bodyColor, 0f, origin, 0.7f, fx, 0f);

            return false;
        }

        private void DrawGlow(SpriteBatch spriteBatch, Vector2 drawPos, float alpha) {
            if (GlowIntensity > 0.02f && CWRAsset.SoftGlow != null && !CWRAsset.SoftGlow.IsDisposed) {
                Texture2D glow = CWRAsset.SoftGlow.Value;
                Color glowColor = new Color(80, 160, 255) * (GlowIntensity * alpha);
                float glowScale = 80f / (glow.Width * 0.5f);
                spriteBatch.Draw(glow, drawPos - new Vector2(0, FrameHeight * 0.4f), null,
                    glowColor with { A = 0 }, 0f, glow.Size() * 0.5f, glowScale, SpriteEffects.None, 0f);
            }
        }

        #endregion
    }
}
