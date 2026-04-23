using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.CollisionMask;
using CalamityOverhaul.Content.HackTimes;
using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.SignalTowers
{
    /// <summary>
    /// 信号塔Actor
    /// 作为策划上承担复杂独立功能的核心建筑，从通用<see cref="ArchitectureActor"/>中剥离
    /// 基础逻辑（时空可见度、碰撞、扭曲着色）与通用建筑保持一致，专属机制（过电滤镜、骇入广播等）直接挂在自身
    /// 实现<see cref="IHackableSignalTower"/>后可被骇客时间选中、并触发病毒广播协议
    /// </summary>
    internal class SignalTowerActor : Actor, IHackableSignalTower
    {
        /// <summary>被雷击后过电滤镜的剩余帧数</summary>
        [SyncVar]
        public int ElectrifyTimer;
        /// <summary>本次过电的总帧长，便于shader按进度归一化</summary>
        [SyncVar]
        public int ElectrifyMax;
        /// <summary>本次过电的随机种子，让每次电弧形态不同</summary>
        [SyncVar]
        public float ElectrifySeed;

        /// <summary>广播冷却剩余帧数：防止短时间内被连续发射病毒广播</summary>
        [SyncVar]
        public int BroadcastCooldown;

        private bool initialized;
        //时空可见度，过去时逼近1，现在时逼近0
        private float visibility;

        //隐形碰撞tile记录，与ArchitectureActor共用tile放置器
        private readonly List<Point16> placedCollisionTiles = [];
        private bool collisionPlaced;
        private const float CollisionThreshold = 0.5f;

        /// <summary>当前过电滤镜是否仍在持续</summary>
        public bool IsElectrified => ElectrifyTimer > 0 && ElectrifyMax > 0;

        /// <summary>在塔上触发一次过电滤镜，<paramref name="frames"/>为总帧长</summary>
        public void BeginElectrify(int frames, float seed) {
            if (frames <= 0) return;
            ElectrifyTimer = frames;
            ElectrifyMax = frames;
            ElectrifySeed = seed;
            NetUpdate = true;
        }

        public override void OnSpawn(params object[] args) {
            DrawLayer = ActorDrawLayer.AfterTiles;
            ApplyTextureSize();
        }

        public override void AI() {
            if (!initialized) ApplyTextureSize();
            if (!VoidColony.Active) {
                ArchitectureTilePlacer.Clear(placedCollisionTiles);
                collisionPlaced = false;
                ActorLoader.KillActor(WhoAmI);
                return;
            }
            ArchitectureWarpDraw.TickVisibility(ref visibility);
            UpdateCollision();
            if (ElectrifyTimer > 0) ElectrifyTimer--;
            if (BroadcastCooldown > 0) BroadcastCooldown--;
            Velocity = Vector2.Zero;
        }

        private void ApplyTextureSize() {
            Texture2D tex = ArchitectureAsset.Get(ArchitectureType.SignalTower);
            if (tex == null) return;
            Width = tex.Width;
            Height = tex.Height;
            DrawExtendMode = Math.Max(Width, Height);
            initialized = true;
        }

        private void UpdateCollision() {
            bool shouldPlace = visibility >= CollisionThreshold;
            if (shouldPlace == collisionPlaced) return;

            if (shouldPlace) {
                string[] mask = ArchitectureCollisionMask.Get(ArchitectureType.SignalTower);
                if (mask == null) { collisionPlaced = true; return; }
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
            Texture2D tex = ArchitectureAsset.Get(ArchitectureType.SignalTower);
            if (tex == null) return false;
            if (!ArchitectureWarpDraw.ShouldDraw(visibility)) return false;

            float warp = ArchitectureWarpDraw.ComputeWarp();
            Vector2 drawPos = Position - Main.screenPosition;

            ArchitectureWarpDraw.DrawWithShader(spriteBatch, tex, drawPos, visibility, warp, flipX: false);

            //骇客时间中被悬停/选中时套一层NPC高亮shader，让玩家知晓信号塔可被骇入
            float hackMark = ComputeHackHighlight();
            if (hackMark > 0.01f) {
                DrawHackHighlightOverlay(spriteBatch, tex, drawPos, hackMark);
            }

            //过电滤镜：Additive叠在本体之上，用原图alpha做遮罩避免矩形外溢
            if (IsElectrified) {
                DrawElectrifiedOverlay(spriteBatch, tex, drawPos);
            }
            return false;
        }

        /// <summary>计算骇客时间高亮强度，选中=1、悬停=0.55，乘全局Intensity</summary>
        private float ComputeHackHighlight() {
            bool selected = ReferenceEquals(HackTime.CurrentScanTarget, this);
            bool hovered = ReferenceEquals(HackTimeTargeting.HoveredSignalTower, this);
            float baseMark = selected ? 1f : hovered ? 0.55f : 0f;
            return baseMark * HackTime.Intensity;
        }

        /// <summary>Additive叠一层NPC骇入高亮，与炮台选中风格一致</summary>
        private static void DrawHackHighlightOverlay(SpriteBatch sb, Texture2D tex, Vector2 drawPos, float strength) {
            Effect shader = HackTimeAssets.HackTimeNPCHighlight;
            if (shader == null) return;

            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));
            shader.Parameters["intensity"]?.SetValue(strength);
            shader.Parameters["isSelected"]?.SetValue(strength > 0.9f ? 1f : 0f);
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            sb.Draw(tex, drawPos, null, Color.White * strength, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        private void DrawElectrifiedOverlay(SpriteBatch spriteBatch, Texture2D tex, Vector2 drawPos) {
            Effect shader = EffectLoader.SignalTowerElectrified?.Value;
            if (shader == null) return;

            float progress = 1f - ElectrifyTimer / (float)ElectrifyMax;
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["electrifyProgress"]?.SetValue(MathHelper.Clamp(progress, 0f, 1f));
            shader.Parameters["seed"]?.SetValue(ElectrifySeed);
            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));
            shader.Parameters["intensity"]?.SetValue(MathHelper.Clamp(visibility, 0f, 1f));

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(tex, drawPos, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        #region IHackableSignalTower 实现

        Actor IHackableSignalTower.AsActor => this;

        Vector2 IScannable.WorldCenter => Position + new Vector2(Width * 0.5f, Height * 0.25f);

        bool IScannable.IsValid => Active && VoidColony.Active && visibility > 0.05f;

        bool IScannable.IsHackable => true;

        int IScannable.ScanRowCount => 4;

        void IScannable.BuildScanData(string[] labels, string[] values, Color[] colors) {
            labels[0] = HackTime.TurretScanName.Value;
            values[0] = HackTime.SignalTowerScanName.Value;
            colors[0] = HackTheme.Accent;

            labels[1] = HackTime.TypeLabel.Value;
            values[1] = HackTime.SignalTowerScanType.Value;
            colors[1] = HackTheme.AccentAlt;

            labels[2] = HackTime.ThreatLabel.Value;
            values[2] = HackTime.SignalTowerScanThreat.Value;
            colors[2] = HackTheme.Uploading;

            labels[3] = HackTime.SignalTowerScanStatus.Value;
            if (BroadcastCooldown > 0) {
                values[3] = HackTime.SignalTowerScanStatusBroadcasting.Value;
                colors[3] = HackTheme.Contagion;
            }
            else if (IsElectrified) {
                values[3] = HackTime.SignalTowerScanStatusElectrified.Value;
                colors[3] = HackTheme.Uploading;
            }
            else {
                values[3] = HackTime.SignalTowerScanStatusOnline.Value;
                colors[3] = HackTheme.Accent;
            }
        }

        public void BeginVirusBroadcast(float radiusPixels, int disableFrames, Player caster) {
            //广播冷却期间忽略后续触发，避免垃圾信号叠加
            if (BroadcastCooldown > 0) return;

            //自身也走一遍过电滤镜，强化塔内脉冲注入的表现
            int electrifyFrames = Math.Max(ElectrifyTimer, 180);
            BeginElectrify(electrifyFrames, Main.rand.NextFloat() * 100f);

            //广播持续帧长：与扩散Actor的InitLifeFrames对齐
            const int lifeFrames = 150;
            BroadcastCooldown = lifeFrames + 120;
            NetUpdate = true;

            //主控端Spawn扩散Actor，信号塔顶部中心作为波源
            Vector2 source = Position + new Vector2(Width * 0.5f, Height * 0.18f);
            SignalTowerVirusBroadcastActor.Spawn(source, radiusPixels, lifeFrames, disableFrames);
        }

        #endregion
    }
}
