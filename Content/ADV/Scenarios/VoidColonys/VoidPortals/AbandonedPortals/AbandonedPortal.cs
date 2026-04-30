using CalamityOverhaul.Common;
using InnoVault.Actors;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.VoidPortals.AbandonedPortals
{
    /// <summary>
    /// 主世界出生点下方洞穴层中的废墟传送门。<br/>
    /// 用 Actor 承载贴图、悬停反馈与右键交互。修复完成后会启动
    /// <see cref="VoidTransportPlayer"/> 的传送演出并进入虚空聚落。
    /// </summary>
    internal class AbandonedPortal : Actor
    {
        [VaultLoaden("CalamityOverhaul/Assets/ADV/VoidColony/AbandonedPortal")]
        private Texture2D AbandonedPortalTex;
        internal const int TileWidth = 25;
        internal const int TileHeight = 19;
        private const float InteractRangePixels = 520f;

        [SyncVar]
        public byte RepairStateByte;

        [SyncVar]
        public int RepairTimer;

        private bool initialized;
        private float hoverStrength;
        private float hoverSeed;

        internal enum RepairState : byte
        {
            Broken,
            Repairing,
            Repaired,
        }

        internal RepairState State {
            get => (RepairState)RepairStateByte;
            private set => RepairStateByte = (byte)value;
        }

        internal bool CanTeleport => State == RepairState.Repaired;

        internal float RepairProgress => State switch {
            RepairState.Repairing => MathHelper.Clamp(RepairTimer / (float)AbandonedPortalSession.RepairDurationFrames, 0f, 1f),
            RepairState.Repaired => 1f,
            _ => 0f,
        };

        internal Vector2 WorldCenter => Position + new Vector2(Width * 0.5f, Height * 0.5f);
        internal Vector2 PortalMouthCenter => Position + new Vector2(Width * 0.55f, Height * 0.46f);

        public override void OnSpawn(params object[] args) {
            DrawLayer = ActorDrawLayer.AfterTiles;
            hoverSeed = Main.rand.NextFloat() * 100f;
            Width = 416;
            Height = 300;
            DrawExtendMode = Math.Max(Width, Height) + 80;
            RepairStateByte = AbandonedPortalSystem.SavedStateByte;
            RepairTimer = Math.Clamp(AbandonedPortalSystem.SavedRepairTimer, 0, AbandonedPortalSession.RepairDurationFrames);
            if (RepairTimer >= AbandonedPortalSession.RepairDurationFrames) {
                State = RepairState.Repaired;
            }
            AbandonedPortalSession.CurrentPortal ??= this;
        }

        public override void AI() {
            if (VoidColony.Active) {
                AbandonedPortalSession.Close();
                ActorLoader.KillActor(WhoAmI);
                return;
            }

            if (AbandonedPortalSession.CurrentPortal != this) {
                AbandonedPortalSession.CurrentPortal = this;
            }

            Velocity = Vector2.Zero;
            UpdateRepair();
            UpdateLocalHoverAndInteract();
            initialized = true;
        }

        internal void StartRepair() {
            if (State != RepairState.Broken) return;
            State = RepairState.Repairing;
            RepairTimer = 0;
            AbandonedPortalSystem.StorePortalState(this);
            NetUpdate = true;
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.65f, Pitch = -0.25f }, WorldCenter);
        }

        internal void StartTransport(Player player) {
            if (!CanTeleport || player == null || !player.active) return;
            AbandonedPortalSession.RequestClose();
            player.GetModPlayer<VoidTransportPlayer>().StartTransport(PortalMouthCenter, VoidColony.Enter);
        }

        private void UpdateRepair() {
            if (State != RepairState.Repairing) return;

            RepairTimer++;
            if (!Main.dedServ && RepairTimer % 12 == 0) {
                SpawnRepairSpark();
            }

            if (RepairTimer >= AbandonedPortalSession.RepairDurationFrames) {
                RepairTimer = AbandonedPortalSession.RepairDurationFrames;
                State = RepairState.Repaired;
                NetUpdate = true;
                if (!Main.dedServ) {
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.75f, Pitch = -0.4f }, WorldCenter);
                }
            }
            AbandonedPortalSystem.StorePortalState(this);
        }

        private void UpdateLocalHoverAndInteract() {
            if (Main.netMode == NetmodeID.Server) return;

            Player local = Main.LocalPlayer;
            if (local == null || !local.active || local.dead) {
                hoverStrength = 0f;
                return;
            }

            bool panelOpen = AbandonedPortalSession.IsOpen;
            bool canHover = !Main.gameMenu
                && !panelOpen
                && !local.mouseInterface
                && local.Center.DistanceSQ(WorldCenter) < InteractRangePixels * InteractRangePixels;

            Rectangle aabb = new((int)Position.X, (int)Position.Y, Width, Height);
            bool mouseOver = canHover && aabb.Contains((int)Main.MouseWorld.X, (int)Main.MouseWorld.Y);

            float target = mouseOver ? 1f : 0f;
            hoverStrength = MathHelper.Lerp(hoverStrength, target, 0.18f);
            if (Math.Abs(hoverStrength - target) < 0.005f) hoverStrength = target;

            if (!mouseOver) return;

            local.CWR().DontUseItemTime = 2;
            local.mouseInterface = true;
            local.cursorItemIconEnabled = false;
            local.cursorItemIconID = ItemID.None;

            if (Main.mouseRight && Main.mouseRightRelease) {
                AbandonedPortalSession.Open(this);
                Main.mouseRightRelease = false;
                SoundEngine.PlaySound(SoundID.MenuOpen);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            Texture2D tex = AbandonedPortalTex;
            if (tex == null) return false;

            Vector2 drawPos = Position - Main.screenPosition;
            drawPos.Y += 4;
            float repairGlow = RepairProgress;
            spriteBatch.Draw(tex, drawPos, drawColor);

            if (repairGlow > 0.02f) {
                Color glow = Color.Lerp(new Color(80, 180, 220, 0), new Color(255, 150, 80, 0), repairGlow);
                float pulse = 0.18f + MathF.Sin(Main.GlobalTimeWrappedHourly * 4f) * 0.06f;
                spriteBatch.Draw(tex, drawPos, glow * (pulse + repairGlow * 0.28f));
            }

            if (hoverStrength > 0.01f && !AbandonedPortalSession.IsOpen) {
                DrawHoverOutline(spriteBatch, tex, drawPos, hoverStrength);
            }

            return false;
        }

        private void DrawHoverOutline(SpriteBatch sb, Texture2D tex, Vector2 drawPos, float strength) {
            Effect shader = EffectLoader.SignalTowerHoverOutline?.Value;
            if (shader == null) return;

            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["intensity"]?.SetValue(MathHelper.Clamp(strength, 0f, 1f));
            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));
            shader.Parameters["seed"]?.SetValue(hoverSeed);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            sb.Draw(tex, drawPos, Color.White);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        private void SpawnRepairSpark() {
            Vector2 basePos = PortalMouthCenter + Main.rand.NextVector2Circular(90f, 70f);
            Dust d = Dust.NewDustPerfect(basePos, DustID.Electric, Main.rand.NextVector2Circular(1.6f, 1.6f), 80,
                Color.Lerp(Color.Cyan, Color.OrangeRed, Main.rand.NextFloat()), Main.rand.NextFloat(0.8f, 1.35f));
            d.noGravity = true;
        }
    }
}
