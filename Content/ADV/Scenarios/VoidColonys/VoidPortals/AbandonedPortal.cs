using CalamityOverhaul.Common;
using InnoVault.Actors;
using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.VoidPortals
{
    /// <summary>
    /// 主世界出生点下方的废墟传送门，用 Actor 承载贴图、悬停反馈与右键交互。
    /// 修复完成后会启动 <see cref="VoidTransportPlayer"/> 的传送演出并进入虚空聚落。
    /// </summary>
    internal class AbandonedPortal : Actor
    {
        private const string TexturePath = "CalamityOverhaul/Content/ADV/Scenarios/VoidColonys/VoidPortals/AbandonedPortal";
        private const int FallbackWidth = 400;
        private const int FallbackHeight = 300;
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
            Width = 416;
            Height = 300;
            DrawLayer = ActorDrawLayer.AfterTiles;
            hoverSeed = Main.rand.NextFloat() * 100f;
            ApplyTextureSize();
            RepairStateByte = AbandonedPortalSystem.SavedStateByte;
            RepairTimer = Math.Clamp(AbandonedPortalSystem.SavedRepairTimer, 0, AbandonedPortalSession.RepairDurationFrames);
            if (RepairTimer >= AbandonedPortalSession.RepairDurationFrames) {
                State = RepairState.Repaired;
            }
            AbandonedPortalSession.CurrentPortal ??= this;
        }

        public override void AI() {
            if (!initialized) ApplyTextureSize();

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
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.8f, Pitch = -0.45f }, WorldCenter);
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

            local.mouseInterface = true;
            local.cursorItemIconEnabled = false;
            local.cursorItemIconID = ItemID.None;

            if (Main.mouseRight && Main.mouseRightRelease) {
                AbandonedPortalSession.Open(this);
                Main.mouseRightRelease = false;
                SoundEngine.PlaySound(SoundID.MenuOpen);
            }
        }

        private void ApplyTextureSize() {
            Texture2D tex = GetTexture();
            Width = tex?.Width ?? FallbackWidth;
            Height = tex?.Height ?? FallbackHeight;
            DrawExtendMode = Math.Max(Width, Height) + 80;
            initialized = true;
        }

        private static Texture2D GetTexture() {
            if (Main.dedServ) return null;
            return ModContent.Request<Texture2D>(TexturePath).Value;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            Texture2D tex = GetTexture();
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

    internal static class AbandonedPortalSession
    {
        public const int RepairDurationFrames = 60 * 60 * 3;

        internal static AbandonedPortal CurrentPortal { get; set; }
        internal static bool IsOpen => CurrentPortal != null && Phase != PanelPhase.Closed;
        internal static PanelPhase Phase { get; private set; }
        internal static int PhaseTimer { get; private set; }
        internal static float OpenProgress { get; private set; }
        internal static float SessionTime { get; private set; }

        internal enum PanelPhase
        {
            Closed,
            Broken,
            Repairing,
            Repaired,
            Closing,
        }

        internal static void Open(AbandonedPortal portal) {
            if (portal == null || !portal.Active) return;
            CurrentPortal = portal;
            Phase = portal.State switch {
                AbandonedPortal.RepairState.Repairing => PanelPhase.Repairing,
                AbandonedPortal.RepairState.Repaired => PanelPhase.Repaired,
                _ => PanelPhase.Broken,
            };
            PhaseTimer = 0;
            SessionTime = 0f;
        }

        internal static void RequestClose() {
            if (!IsOpen || Phase == PanelPhase.Closing) return;
            Phase = PanelPhase.Closing;
            PhaseTimer = 0;
        }

        internal static void Close() {
            CurrentPortal = null;
            Phase = PanelPhase.Closed;
            PhaseTimer = 0;
            OpenProgress = 0f;
            SessionTime = 0f;
        }

        internal static void Update() {
            if (Phase == PanelPhase.Closed) return;

            if (CurrentPortal == null || !CurrentPortal.Active || Main.gameMenu) {
                Close();
                return;
            }

            SessionTime += 1f / 60f;
            PhaseTimer++;

            if (Phase != PanelPhase.Closing) {
                Phase = CurrentPortal.State switch {
                    AbandonedPortal.RepairState.Repairing => PanelPhase.Repairing,
                    AbandonedPortal.RepairState.Repaired => PanelPhase.Repaired,
                    _ => PanelPhase.Broken,
                };
            }

            float target = Phase == PanelPhase.Closing ? 0f : 1f;
            OpenProgress = MathHelper.Lerp(OpenProgress, target, 0.16f);
            if (Math.Abs(OpenProgress - target) < 0.005f) OpenProgress = target;

            if (Phase == PanelPhase.Closing && OpenProgress <= 0.01f) {
                Close();
            }
        }
    }

    internal class AbandonedPortalStrings : ModSystem, ILocalizedModType
    {
        public string LocalizationCategory => "UI";

        public static LocalizedText Title { get; private set; }
        public static LocalizedText BrokenSubtitle { get; private set; }
        public static LocalizedText RepairingSubtitle { get; private set; }
        public static LocalizedText RepairedSubtitle { get; private set; }
        public static LocalizedText BrokenBody { get; private set; }
        public static LocalizedText RepairingBody { get; private set; }
        public static LocalizedText RepairedBody { get; private set; }
        public static LocalizedText StartRepair { get; private set; }
        public static LocalizedText Teleport { get; private set; }
        public static LocalizedText Close { get; private set; }
        public static LocalizedText ProgressFormat { get; private set; }

        public override void SetStaticDefaults() {
            Title = this.GetLocalization(nameof(Title), () => "废墟传送门控制台");
            BrokenSubtitle = this.GetLocalization(nameof(BrokenSubtitle), () => "结构断裂 · 核心离线");
            RepairingSubtitle = this.GetLocalization(nameof(RepairingSubtitle), () => "自修复程序运行中");
            RepairedSubtitle = this.GetLocalization(nameof(RepairedSubtitle), () => "通道稳定 · 虚无坐标可用");
            BrokenBody = this.GetLocalization(nameof(BrokenBody), () => "残破门框中仍残留着微弱的亚空间回声。启动自修复后，门体会在数分钟内重建定位环。");
            RepairingBody = this.GetLocalization(nameof(RepairingBody), () => "纳米焊缝正在重构裂隙约束器。请保持附近区域稳定，等待校准完成。");
            RepairedBody = this.GetLocalization(nameof(RepairedBody), () => "门体已经完成自修复。传送序列会先展开裂隙演出，再将你送入虚无世界。");
            StartRepair = this.GetLocalization(nameof(StartRepair), () => "▷ 启动自修复程序");
            Teleport = this.GetLocalization(nameof(Teleport), () => "▷ 启动传送");
            Close = this.GetLocalization(nameof(Close), () => "✕ 关闭");
            ProgressFormat = this.GetLocalization(nameof(ProgressFormat), () => "修复进度 {0}%");
        }
    }

    internal class AbandonedPortalSystem : ModSystem
    {
        private const int SpawnCheckDelay = 90;
        private const string SaveStateKey = "AbandonedPortalState";
        private const string SaveRepairTimerKey = "AbandonedPortalRepairTimer";
        private const int ClearMarginTiles = 2;
        private int spawnTimer;

        internal static byte SavedStateByte { get; private set; }
        internal static int SavedRepairTimer { get; private set; }

        internal static void StorePortalState(AbandonedPortal portal) {
            if (portal == null) return;
            SavedStateByte = portal.RepairStateByte;
            SavedRepairTimer = Math.Clamp(portal.RepairTimer, 0, AbandonedPortalSession.RepairDurationFrames);
        }

        public override void PostUpdateWorld() {
            AbandonedPortalSession.Update();

            if (Main.gameMenu || VoidColony.Active || Main.netMode == NetmodeID.MultiplayerClient) {
                return;
            }

            if (++spawnTimer < SpawnCheckDelay) {
                return;
            }
            spawnTimer = 0;

            if (ActorLoader.GetActiveActors<AbandonedPortal>().Count > 0) {
                return;
            }

            Vector2 position = ResolveSpawnPosition();
            PreparePortalSite(position);
            ActorLoader.NewActor<AbandonedPortal>(position, Vector2.Zero);
        }

        public override void OnWorldUnload() {
            spawnTimer = 0;
            SavedStateByte = 0;
            SavedRepairTimer = 0;
            AbandonedPortalSession.Close();
        }

        public override void SaveWorldData(TagCompound tag) {
            AbandonedPortal portal = AbandonedPortalSession.CurrentPortal;
            if (portal != null && portal.Active) {
                StorePortalState(portal);
            }

            tag[SaveStateKey] = SavedStateByte;
            tag[SaveRepairTimerKey] = SavedRepairTimer;
        }

        public override void LoadWorldData(TagCompound tag) {
            SavedStateByte = tag.GetByte(SaveStateKey);
            SavedRepairTimer = Math.Clamp(tag.GetInt(SaveRepairTimerKey), 0, AbandonedPortalSession.RepairDurationFrames);
            if (SavedRepairTimer >= AbandonedPortalSession.RepairDurationFrames) {
                SavedStateByte = (byte)AbandonedPortal.RepairState.Repaired;
            }
        }

        private static Vector2 ResolveSpawnPosition() {
            int x = Math.Clamp(Main.spawnTileX, 20, Main.maxTilesX - 20);
            int startY = Math.Clamp(Main.spawnTileY + 12, 20, Main.maxTilesY - 20);
            int groundY = Math.Clamp(Main.spawnTileY + 42, 20, Main.maxTilesY - 20);

            for (int y = startY; y < Math.Min(Main.maxTilesY - 20, startY + 260); y++) {
                Tile tile = Framing.GetTileSafely(x, y);
                if (tile.HasTile && Main.tileSolid[tile.TileType]) {
                    groundY = y;
                    break;
                }
            }

            int leftTile = x - AbandonedPortal.TileWidth / 2;
            int topTile = groundY - AbandonedPortal.TileHeight;

            leftTile = Math.Clamp(leftTile, 10, Main.maxTilesX - AbandonedPortal.TileWidth - 10);
            topTile = Math.Clamp(topTile, 10, Main.maxTilesY - AbandonedPortal.TileHeight - 10);

            return new Vector2(leftTile * 16f, topTile * 16f);
        }

        private static void PreparePortalSite(Vector2 topLeftWorld) {
            int left = (int)MathF.Floor(topLeftWorld.X / 16f);
            int top = (int)MathF.Floor(topLeftWorld.Y / 16f);
            int right = left + AbandonedPortal.TileWidth - 1;
            int bottom = top + AbandonedPortal.TileHeight - 1;

            // 清理门体足迹与边缘缓冲，但保留其下方地面，避免 Actor 基座看起来悬空。
            ClearTiles(left - ClearMarginTiles, top - ClearMarginTiles,
                right + ClearMarginTiles, bottom);
        }

        private static void ClearTiles(int left, int top, int right, int bottom) {
            left = Math.Clamp(left, 1, Main.maxTilesX - 2);
            right = Math.Clamp(right, 1, Main.maxTilesX - 2);
            top = Math.Clamp(top, 1, Main.maxTilesY - 2);
            bottom = Math.Clamp(bottom, 1, Main.maxTilesY - 2);

            for (int x = left; x <= right; x++) {
                for (int y = top; y <= bottom; y++) {
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (!tile.HasTile) continue;
                    WorldGen.KillTile(x, y, fail: false, effectOnly: false, noItem: true);
                }
            }

            if (Main.netMode == NetmodeID.Server) {
                NetMessage.SendTileSquare(-1, (left + right) / 2, (top + bottom) / 2,
                    Math.Max(right - left + 1, bottom - top + 1));
            }
        }
    }

    internal class AbandonedPortalPanelUI : UIHandle
    {
        private static Rectangle panelRect;

        public override bool Active => !Main.gameMenu
            && (AbandonedPortalSession.IsOpen || AbandonedPortalSession.OpenProgress > 0.005f);

        public override void Update() {
            if (AbandonedPortalSession.OpenProgress > 0.05f) {
                UIHitBox = panelRect;
            }
            else {
                UIHitBox = Rectangle.Empty;
            }
        }

        public override void Draw(SpriteBatch sb) {
            AbandonedPortal portal = AbandonedPortalSession.CurrentPortal;
            if (portal == null) return;

            float open = AbandonedPortalSession.OpenProgress;
            float eased = 1f - (float)Math.Pow(1f - MathHelper.Clamp(open, 0f, 1f), 3);
            int width = 700;
            int height = 360;
            float scale = 0.9f + eased * 0.1f;
            int drawW = (int)(width * scale);
            int drawH = (int)(height * scale);
            Rectangle rect = new(Main.screenWidth / 2 - drawW / 2, Main.screenHeight / 2 - drawH / 2, drawW, drawH);
            panelRect = rect;
            Texture2D px = TextureAssets.MagicPixel.Value;

            sb.Draw(px, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * (0.48f * eased));
            DrawPanelBox(sb, rect, eased);
            DrawPanelText(sb, rect, portal, eased);

            if (rect.Contains(Main.mouseX, Main.mouseY) && eased > 0.2f) {
                Main.LocalPlayer.mouseInterface = true;
                Main.LocalPlayer.cursorItemIconEnabled = false;
            }
        }

        private static void DrawPanelBox(SpriteBatch sb, Rectangle rect, float alpha) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            Color bg = new Color(10, 13, 16) * (0.94f * alpha);
            Color edge = new Color(86, 184, 210) * alpha;
            Color rust = new Color(210, 92, 48) * (0.75f * alpha);

            sb.Draw(px, rect, bg);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 2), edge);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), rust);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 2, rect.Height), edge * 0.75f);
            sb.Draw(px, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), rust * 0.75f);

            for (int i = 0; i < 8; i++) {
                float t = (AbandonedPortalSession.SessionTime * 40f + i * 59f) % rect.Width;
                sb.Draw(px, new Rectangle(rect.X + (int)t, rect.Y + 18 + i * 31, 54, 1), edge * 0.12f);
            }
        }

        private static void DrawPanelText(SpriteBatch sb, Rectangle rect, AbandonedPortal portal, float alpha) {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Color title = new Color(220, 245, 255) * alpha;
            Color accent = new Color(86, 220, 238) * alpha;
            Color bodyColor = new Color(190, 206, 210) * alpha;
            Color warn = new Color(245, 118, 72) * alpha;

            Vector2 pos = new(rect.X + 34, rect.Y + 26);
            Utils.DrawBorderString(sb, AbandonedPortalStrings.Title.Value, pos, title, 0.92f);

            string subtitle = AbandonedPortalSession.Phase switch {
                AbandonedPortalSession.PanelPhase.Repairing => AbandonedPortalStrings.RepairingSubtitle.Value,
                AbandonedPortalSession.PanelPhase.Repaired => AbandonedPortalStrings.RepairedSubtitle.Value,
                _ => AbandonedPortalStrings.BrokenSubtitle.Value,
            };
            Utils.DrawBorderString(sb, subtitle, pos + new Vector2(0f, 38f), accent, 0.7f);

            string body = AbandonedPortalSession.Phase switch {
                AbandonedPortalSession.PanelPhase.Repairing => AbandonedPortalStrings.RepairingBody.Value,
                AbandonedPortalSession.PanelPhase.Repaired => AbandonedPortalStrings.RepairedBody.Value,
                _ => AbandonedPortalStrings.BrokenBody.Value,
            };
            string[] wrapped = Utils.WordwrapString(body, font, rect.Width - 90, 8, out _);
            for (int i = 0; i < wrapped.Length; i++) {
                Utils.DrawBorderString(sb, wrapped[i], pos + new Vector2(0f, 98f + i * 24f), bodyColor, 0.65f);
            }

            DrawRepairBar(sb, new Rectangle(rect.X + 34, rect.Y + 226, rect.Width - 68, 18), portal.RepairProgress, alpha);

            int percent = (int)(portal.RepairProgress * 100f);
            Utils.DrawBorderString(sb, string.Format(AbandonedPortalStrings.ProgressFormat.Value, percent),
                new Vector2(rect.X + 34, rect.Y + 250), portal.CanTeleport ? accent : warn, 0.64f);

            Rectangle primary = new(rect.Right - 244, rect.Bottom - 66, 190, 34);
            Rectangle close = new(rect.X + 34, rect.Bottom - 66, 120, 34);
            if (portal.State == AbandonedPortal.RepairState.Broken) {
                DrawButton(sb, primary, AbandonedPortalStrings.StartRepair.Value, accent, alpha, () => portal.StartRepair());
            }
            else if (portal.State == AbandonedPortal.RepairState.Repaired) {
                DrawButton(sb, primary, AbandonedPortalStrings.Teleport.Value, warn, alpha, () => portal.StartTransport(Main.LocalPlayer));
            }
            DrawButton(sb, close, AbandonedPortalStrings.Close.Value, bodyColor, alpha, AbandonedPortalSession.RequestClose);
        }

        private static void DrawRepairBar(SpriteBatch sb, Rectangle rect, float progress, float alpha) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            sb.Draw(px, rect, Color.Black * (0.55f * alpha));
            Rectangle fill = rect;
            fill.Width = (int)(rect.Width * MathHelper.Clamp(progress, 0f, 1f));
            Color c = Color.Lerp(new Color(230, 75, 45), new Color(80, 230, 245), progress) * alpha;
            sb.Draw(px, fill, c * 0.75f);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1), c);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), c * 0.8f);
        }

        private static void DrawButton(SpriteBatch sb, Rectangle rect, string text, Color color, float alpha, Action onClick) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            bool hover = rect.Contains(Main.mouseX, Main.mouseY);
            Color bg = (hover ? color * 0.28f : Color.Black * 0.35f) * alpha;
            sb.Draw(px, rect, bg);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1), color * alpha);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color * (0.75f * alpha));

            Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * 0.62f;
            Utils.DrawBorderString(sb, text, new Vector2(rect.Center.X - size.X * 0.5f, rect.Center.Y - size.Y * 0.5f),
                Color.White * alpha, 0.62f);

            if (hover) {
                Main.LocalPlayer.mouseInterface = true;
                if (Main.mouseLeft && Main.mouseLeftRelease) {
                    Main.mouseLeftRelease = false;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    onClick?.Invoke();
                }
            }
        }
    }
}
