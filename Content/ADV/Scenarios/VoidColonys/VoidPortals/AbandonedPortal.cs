using CalamityOverhaul.Common;
using InnoVault.Actors;
using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
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
    /// 主世界出生点下方洞穴层中的废墟传送门。<br/>
    /// 用 Actor 承载贴图、悬停反馈与右键交互。修复完成后会启动
    /// <see cref="VoidTransportPlayer"/> 的传送演出并进入虚空聚落。
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
            OpenProgress = MathHelper.Lerp(OpenProgress, target, 0.26f);
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
        public static LocalizedText StatusBroken { get; private set; }
        public static LocalizedText StatusRepairing { get; private set; }
        public static LocalizedText StatusRepaired { get; private set; }
        public static LocalizedText DiagnosticHeader { get; private set; }
        public static LocalizedText DiagnosticBroken { get; private set; }
        public static LocalizedText DiagnosticRepairing { get; private set; }
        public static LocalizedText DiagnosticRepaired { get; private set; }

        public override void SetStaticDefaults() {
            Title = this.GetLocalization(nameof(Title), () => "废墟传送门控制台");
            BrokenSubtitle = this.GetLocalization(nameof(BrokenSubtitle), () => "结构断裂 · 核心离线");
            RepairingSubtitle = this.GetLocalization(nameof(RepairingSubtitle), () => "自修复程序运行中");
            RepairedSubtitle = this.GetLocalization(nameof(RepairedSubtitle), () => "通道稳定 · 虚无坐标可用");
            BrokenBody = this.GetLocalization(nameof(BrokenBody), () => "残破门框中仍残留着微弱的亚空间回声。启动自修复后，门体会在数分钟内重建定位环。");
            RepairingBody = this.GetLocalization(nameof(RepairingBody), () => "纳米焊缝正在重构裂隙约束器。请保持附近区域稳定，等待校准完成。");
            RepairedBody = this.GetLocalization(nameof(RepairedBody), () => "门体已经完成自修复。传送序列会先展开裂隙演出，再将你送入虚无世界。");
            StartRepair = this.GetLocalization(nameof(StartRepair), () => "启 动 自 修 复");
            Teleport = this.GetLocalization(nameof(Teleport), () => "进 入 通 道");
            Close = this.GetLocalization(nameof(Close), () => "关 闭");
            ProgressFormat = this.GetLocalization(nameof(ProgressFormat), () => "校准进度  {0}%");
            StatusBroken = this.GetLocalization(nameof(StatusBroken), () => "STATUS  ▌ OFFLINE");
            StatusRepairing = this.GetLocalization(nameof(StatusRepairing), () => "STATUS  ▌ CALIBRATING");
            StatusRepaired = this.GetLocalization(nameof(StatusRepaired), () => "STATUS  ▌ ONLINE");
            DiagnosticHeader = this.GetLocalization(nameof(DiagnosticHeader), () => "[DIAG]");
            DiagnosticBroken = this.GetLocalization(nameof(DiagnosticBroken), () => "ERR-0xC4: 定位环裂隙 / 主能源回路断开");
            DiagnosticRepairing = this.GetLocalization(nameof(DiagnosticRepairing), () => "INFO: 纳米焊缝阵列同步中…请勿移动门基座");
            DiagnosticRepaired = this.GetLocalization(nameof(DiagnosticRepaired), () => "OK: 全部子系统在线 / 坐标解析就绪");
        }
    }

    internal class AbandonedPortalSystem : ModSystem
    {
        private const string SaveStateKey = "AbandonedPortalState";
        private const string SaveRepairTimerKey = "AbandonedPortalRepairTimer";
        private const string SavePosXKey = "AbandonedPortalPosX";
        private const string SavePosYKey = "AbandonedPortalPosY";
        private const string SaveResolvedKey = "AbandonedPortalResolved";

        //OnWorldLoad 设置为 true，表示已请求一次 spawn；首帧 PostUpdateWorld 时执行
        private bool spawnPending;

        internal static byte SavedStateByte { get; private set; }
        internal static int SavedRepairTimer { get; private set; }
        //缓存的传送门左上角 tile 坐标（一次世界 = 一次决策）
        internal static int SavedTileX { get; private set; }
        internal static int SavedTileY { get; private set; }
        internal static bool PositionResolved { get; private set; }

        internal static void StorePortalState(AbandonedPortal portal) {
            if (portal == null) return;
            SavedStateByte = portal.RepairStateByte;
            SavedRepairTimer = Math.Clamp(portal.RepairTimer, 0, AbandonedPortalSession.RepairDurationFrames);
        }

        public override void OnWorldLoad() {
            spawnPending = true;
        }

        public override void OnWorldUnload() {
            spawnPending = false;
            SavedStateByte = 0;
            SavedRepairTimer = 0;
            SavedTileX = 0;
            SavedTileY = 0;
            PositionResolved = false;
            AbandonedPortalSession.Close();
        }

        public override void PostUpdateWorld() {
            AbandonedPortalSession.Update();

            if (Main.gameMenu || VoidColony.Active || Main.netMode == NetmodeID.MultiplayerClient) {
                return;
            }

            if (!spawnPending) return;

            //世界已经开放绘制后才执行：避免在 worldgen 早期 / 数据未稳定时介入
            if (ActorLoader.GetActiveActors<AbandonedPortal>().Count > 0) {
                spawnPending = false;
                return;
            }

            //尚未持久化的世界，先决策位置并准备生成位（仅首次）
            bool firstTimeResolved = false;
            if (!PositionResolved) {
                Point spawnTile = AbandonedPortalSiteFinder.Resolve();
                SavedTileX = spawnTile.X;
                SavedTileY = spawnTile.Y;
                PositionResolved = true;
                firstTimeResolved = true;
            }

            Vector2 worldPos = new(SavedTileX * 16f, SavedTileY * 16f);
            //只在首次决策位置时执行地形整理，保留玩家后续对周边的改动
            if (firstTimeResolved) {
                AbandonedPortalSiteFinder.PreparePortalSite(SavedTileX, SavedTileY);
            }
            ActorLoader.NewActor<AbandonedPortal>(worldPos, Vector2.Zero);
            spawnPending = false;
        }

        public override void SaveWorldData(TagCompound tag) {
            AbandonedPortal portal = AbandonedPortalSession.CurrentPortal;
            if (portal != null && portal.Active) {
                StorePortalState(portal);
            }

            tag[SaveStateKey] = SavedStateByte;
            tag[SaveRepairTimerKey] = SavedRepairTimer;

            if (PositionResolved) {
                tag[SavePosXKey] = SavedTileX;
                tag[SavePosYKey] = SavedTileY;
                tag[SaveResolvedKey] = true;
            }
        }

        public override void LoadWorldData(TagCompound tag) {
            SavedStateByte = tag.GetByte(SaveStateKey);
            SavedRepairTimer = Math.Clamp(tag.GetInt(SaveRepairTimerKey), 0, AbandonedPortalSession.RepairDurationFrames);
            if (SavedRepairTimer >= AbandonedPortalSession.RepairDurationFrames) {
                SavedStateByte = (byte)AbandonedPortal.RepairState.Repaired;
            }

            if (tag.ContainsKey(SaveResolvedKey) && tag.GetBool(SaveResolvedKey)) {
                SavedTileX = tag.GetInt(SavePosXKey);
                SavedTileY = tag.GetInt(SavePosYKey);
                PositionResolved = SavedTileX > 0 && SavedTileY > 0;
            }
        }
    }

    /// <summary>
    /// 废墟传送门选址：在出生点正下方的洞穴层中，优先寻找天然的开阔空间，
    /// 找到后只填补缺失的地基；当无法找到合适空间时，最后才挖掘一个生成位。
    /// </summary>
    internal static class AbandonedPortalSiteFinder
    {
        //搜索水平半径（tile）
        private const int SearchRadiusX = 220;
        //搜索深度（tile）
        private const int SearchMaxDepth = 1100;
        //每列尝试步长，避免计算过密
        private const int ColumnStep = 3;
        //每列内 Y 扫描步长
        private const int RowStep = 2;
        //允许的最低开阔度门槛（找到完美空间时）
        private const int RequiredOpenWidth = AbandonedPortal.TileWidth + 4;
        private const int RequiredOpenHeight = AbandonedPortal.TileHeight + 3;
        //地基缺口允许的最大百分比，找到大致平整的地面就视为合格
        private const float MaxFloorGapRatio = 0.35f;
        //安全边距
        private const int WorldEdgeMargin = 40;
        //准备生成位时，门体外缘留出的清理缓冲（tile）
        private const int ClearMarginTiles = 1;

        /// <summary>
        /// 公开入口：返回门体左上角的 tile 坐标。<br/>
        /// 三阶段：①找完美天然腔体 → ②在合理腔体中填补地基 → ③如全失败则挖掘
        /// </summary>
        internal static Point Resolve() {
            int spawnX = Math.Clamp(Main.spawnTileX, WorldEdgeMargin, Main.maxTilesX - WorldEdgeMargin);
            int rockTop = (int)Main.rockLayer + 30; //至少进入洞穴层 30 tile
            int searchTop = Math.Max(rockTop, Main.spawnTileY + 80);
            int searchBottom = Math.Min(Main.maxTilesY - 220, searchTop + SearchMaxDepth);

            //阶段 1：寻找完美天然腔体（开阔且地基平整）
            if (TryFindNaturalCavity(spawnX, searchTop, searchBottom, allowFloorPatch: false, out Point perfectSpot)) {
                return perfectSpot;
            }

            //阶段 2：放宽——允许底部存在一定缺口，进行少量地基填补
            if (TryFindNaturalCavity(spawnX, searchTop, searchBottom, allowFloorPatch: true, out Point patchSpot)) {
                return patchSpot;
            }

            //阶段 3：实在找不到，就在期望的位置直接挖一个洞
            int forcedY = Math.Min(searchBottom, searchTop + 200);
            int forcedX = Math.Clamp(spawnX - AbandonedPortal.TileWidth / 2, WorldEdgeMargin,
                Main.maxTilesX - AbandonedPortal.TileWidth - WorldEdgeMargin);
            int forcedTopY = Math.Clamp(forcedY - AbandonedPortal.TileHeight, WorldEdgeMargin,
                Main.maxTilesY - AbandonedPortal.TileHeight - WorldEdgeMargin);
            return new Point(forcedX, forcedTopY);
        }

        /// <summary>
        /// 在 spawnX 周围以螺旋顺序搜索符合条件的洞穴空间。
        /// </summary>
        private static bool TryFindNaturalCavity(int spawnX, int searchTop, int searchBottom, bool allowFloorPatch, out Point result) {
            //螺旋扫描：自中心向两侧逐步外扩，深度自上而下
            for (int dx = 0; dx <= SearchRadiusX; dx += ColumnStep) {
                for (int sign = -1; sign <= 1; sign += 2) {
                    if (dx == 0 && sign == 1) continue; //中心列只评估一次

                    int x = spawnX + dx * sign;
                    int leftTile = x - AbandonedPortal.TileWidth / 2;
                    if (leftTile < WorldEdgeMargin || leftTile + AbandonedPortal.TileWidth >= Main.maxTilesX - WorldEdgeMargin) {
                        continue;
                    }

                    for (int y = searchTop; y < searchBottom; y += RowStep) {
                        if (EvaluateBox(leftTile, y, allowFloorPatch)) {
                            //y 是地基行，门体顶部在其上方 TileHeight 行
                            result = new Point(leftTile, y - AbandonedPortal.TileHeight);
                            return true;
                        }
                    }
                }
            }
            result = default;
            return false;
        }

        /// <summary>
        /// 评估候选位置：(leftTile, floorY) 表示候选地基行；<br/>
        /// 门体占据 floorY 上方 TileHeight 行，floorY 自身是要求的实心地基。
        /// </summary>
        private static bool EvaluateBox(int leftTile, int floorY, bool allowFloorPatch) {
            //门体顶部
            int topY = floorY - AbandonedPortal.TileHeight;
            if (topY <= WorldEdgeMargin) return false;
            if (floorY + 4 >= Main.maxTilesY - WorldEdgeMargin) return false;

            //1) 上方开阔：评估比 Portal 略大的范围（开阔度更友好）
            int openLeft = leftTile - 2;
            int openRight = leftTile + AbandonedPortal.TileWidth + 1;
            int openTop = topY - 1;
            int openBottom = floorY - 1;
            int openSampleCount = 0;
            int openClearCount = 0;
            for (int x = openLeft; x <= openRight; x++) {
                for (int y = openTop; y <= openBottom; y++) {
                    openSampleCount++;
                    Tile t = Framing.GetTileSafely(x, y);
                    if (!IsSolidObstruction(t) && t.LiquidAmount == 0) {
                        openClearCount++;
                    }
                }
            }
            float openRatio = openClearCount / (float)openSampleCount;
            //完美：>= 95% 空气；放宽：>= 80% 空气
            float openThreshold = allowFloorPatch ? 0.80f : 0.95f;
            if (openRatio < openThreshold) return false;

            //2) 地基：floorY 那一行至少要有大部分固体；下方再加一行做缓冲
            int floorSamples = 0;
            int floorSolids = 0;
            int floorLeft = leftTile - 1;
            int floorRight = leftTile + AbandonedPortal.TileWidth;
            for (int x = floorLeft; x <= floorRight; x++) {
                Tile t1 = Framing.GetTileSafely(x, floorY);
                Tile t2 = Framing.GetTileSafely(x, floorY + 1);
                floorSamples += 2;
                if (IsSolidGround(t1)) floorSolids++;
                if (IsSolidGround(t2)) floorSolids++;
            }
            float floorRatio = floorSolids / (float)floorSamples;
            //完美：>= 90% 地基；放宽：>= (1 - MaxFloorGapRatio*2) 也即 30%（最多挖一半再补）
            float floorThreshold = allowFloorPatch ? (1f - MaxFloorGapRatio) : 0.90f;
            if (floorRatio < floorThreshold) return false;

            //3) 不能有大液体堆积
            if (HasSignificantLiquid(leftTile, topY, AbandonedPortal.TileWidth, AbandonedPortal.TileHeight)) {
                return false;
            }

            //4) 必须达到最小开阔尺寸（防止位置卡在低矮缝里）
            if (!HasMinimumChamber(leftTile + AbandonedPortal.TileWidth / 2, topY + AbandonedPortal.TileHeight / 2,
                    RequiredOpenWidth, RequiredOpenHeight)) {
                if (!allowFloorPatch) return false;
            }

            return true;
        }

        private static bool IsSolidObstruction(Tile tile) {
            //只把"完整方块"视为阻挡，让平台、家具等可被忽略
            return tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
        }

        private static bool IsSolidGround(Tile tile) {
            //作为地基只接受完整石/泥/沙类方块，避免把灌木/草识别为支撑
            return tile.HasTile && Main.tileSolid[tile.TileType] && !TileID.Sets.Platforms[tile.TileType];
        }

        private static bool HasSignificantLiquid(int left, int top, int w, int h) {
            int liquid = 0;
            int total = 0;
            for (int x = left; x < left + w; x += 2) {
                for (int y = top; y < top + h; y += 2) {
                    total++;
                    if (Framing.GetTileSafely(x, y).LiquidAmount > 80) liquid++;
                }
            }
            return total > 0 && liquid * 5 > total; // > 20% 体积是液体则拒绝
        }

        /// <summary>
        /// 在中心点附近查找一个最少 reqW × reqH 的近似空腔。
        /// </summary>
        private static bool HasMinimumChamber(int cx, int cy, int reqW, int reqH) {
            int halfW = reqW / 2;
            int halfH = reqH / 2;
            int clearCount = 0;
            int total = 0;
            for (int x = cx - halfW; x <= cx + halfW; x++) {
                for (int y = cy - halfH; y <= cy + halfH; y++) {
                    total++;
                    if (!IsSolidObstruction(Framing.GetTileSafely(x, y))) clearCount++;
                }
            }
            return total > 0 && clearCount / (float)total >= 0.85f;
        }

        /// <summary>
        /// 准备生成位：①清空门体足迹 ②补齐地基 ③向外消除少量遮挡
        /// </summary>
        internal static void PreparePortalSite(int leftTile, int topTile) {
            int right = leftTile + AbandonedPortal.TileWidth - 1;
            int bottom = topTile + AbandonedPortal.TileHeight - 1;

            //1) 清空门体内部 + 一格缓冲，让 Actor 能完整可见
            ClearTiles(leftTile - ClearMarginTiles, topTile - ClearMarginTiles,
                right + ClearMarginTiles, bottom);

            //2) 补齐底部地基：把门下方两行内缺失的固体填上（仅限正下方与左右一格）
            FillFloor(leftTile - 1, right + 1, bottom + 1, bottom + 2);

            //3) 在底部边缘做轻量"风化清理"——把门两侧贴近门体的悬空块也敲掉，避免外观突兀
            CleanupLowerEdges(leftTile - ClearMarginTiles, right + ClearMarginTiles, bottom + 1);

            //广播一次大范围 TileSquare（多人同步用）
            if (Main.netMode == NetmodeID.Server) {
                int cx = (leftTile + right) / 2;
                int cy = (topTile + bottom) / 2;
                int size = Math.Max(right - leftTile + 4, bottom - topTile + 6);
                NetMessage.SendTileSquare(-1, cx, cy, size);
            }
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
        }

        private static void FillFloor(int left, int right, int top, int bottom) {
            left = Math.Clamp(left, 1, Main.maxTilesX - 2);
            right = Math.Clamp(right, 1, Main.maxTilesX - 2);
            top = Math.Clamp(top, 1, Main.maxTilesY - 2);
            bottom = Math.Clamp(bottom, 1, Main.maxTilesY - 2);

            //优先沿用周边主体物块作为填充类型，让填补尽量自然
            ushort fillType = ResolveSurroundingTileType(left, right, top);

            for (int x = left; x <= right; x++) {
                for (int y = top; y <= bottom; y++) {
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (tile.HasTile && Main.tileSolid[tile.TileType]) continue;
                    if (tile.LiquidAmount > 0) tile.LiquidAmount = 0;
                    WorldGen.PlaceTile(x, y, fillType, mute: true, forced: true);
                }
            }
        }

        private static void CleanupLowerEdges(int left, int right, int floorY) {
            //把门体足下两块外侧的"悬挂物"（草、藤蔓等）清掉，避免视觉穿模
            for (int x = left; x <= right; x++) {
                for (int dy = 0; dy <= 1; dy++) {
                    int y = floorY - dy;
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (!tile.HasTile) continue;
                    //只在不是稳定地基（非纯固体）的情况下处理，避免破坏地形
                    if (!Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]) {
                        WorldGen.KillTile(x, y, fail: false, effectOnly: false, noItem: true);
                    }
                }
            }
        }

        private static ushort ResolveSurroundingTileType(int left, int right, int floorY) {
            //在门体左右各扫几列，统计最常见的固体类型
            int[] counts = new int[TileLoader.TileCount];
            int sampleSpan = 6;
            int sampleX1 = Math.Max(1, left - sampleSpan);
            int sampleX2 = Math.Min(Main.maxTilesX - 2, right + sampleSpan);
            int sampleY1 = Math.Max(1, floorY);
            int sampleY2 = Math.Min(Main.maxTilesY - 2, floorY + 5);
            for (int x = sampleX1; x <= sampleX2; x++) {
                for (int y = sampleY1; y <= sampleY2; y++) {
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (!tile.HasTile) continue;
                    if (!Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]) continue;
                    if (tile.TileType >= counts.Length) continue;
                    counts[tile.TileType]++;
                }
            }

            int bestType = TileID.Stone;
            int bestCount = 0;
            for (int i = 0; i < counts.Length; i++) {
                if (counts[i] > bestCount) {
                    bestCount = counts[i];
                    bestType = i;
                }
            }
            return (ushort)bestType;
        }
    }

    internal class AbandonedPortalPanelUI : UIHandle
    {
        private const int EdgePad = 14;
        private static Rectangle panelRect;

        //轻量动画状态
        private float shaderTime;
        private float glitchTimer;
        private float lastRepairProgress;
        private float repairAccel;

        public override bool Active => !Main.gameMenu
            && (AbandonedPortalSession.IsOpen || AbandonedPortalSession.OpenProgress > 0.005f);

        public override void Update() {
            shaderTime += 1f / 60f;
            if (shaderTime > 100f) shaderTime -= 100f;

            //根据状态衰减/驱动故障强度
            AbandonedPortal portal = AbandonedPortalSession.CurrentPortal;
            float targetGlitch;
            if (portal == null) {
                targetGlitch = 0.4f;
            }
            else {
                targetGlitch = portal.State switch {
                    AbandonedPortal.RepairState.Broken => 0.85f,
                    AbandonedPortal.RepairState.Repairing => 0.45f - portal.RepairProgress * 0.30f,
                    _ => 0.10f,
                };
            }
            glitchTimer = MathHelper.Lerp(glitchTimer, targetGlitch, 0.08f);

            float curr = portal?.RepairProgress ?? 0f;
            repairAccel = MathHelper.Lerp(repairAccel, MathHelper.Clamp((curr - lastRepairProgress) * 60f, 0f, 1f), 0.15f);
            lastRepairProgress = curr;

            UIHitBox = AbandonedPortalSession.OpenProgress > 0.05f ? panelRect : Rectangle.Empty;
        }

        public override void Draw(SpriteBatch sb) {
            AbandonedPortal portal = AbandonedPortalSession.CurrentPortal;
            if (portal == null) return;

            float open = AbandonedPortalSession.OpenProgress;
            float eased = 1f - (float)Math.Pow(1f - MathHelper.Clamp(open, 0f, 1f), 3);
            int width = 760;
            int height = 410;
            float scale = 0.92f + eased * 0.08f;
            int drawW = (int)(width * scale);
            int drawH = (int)(height * scale);
            Rectangle rect = new(Main.screenWidth / 2 - drawW / 2, Main.screenHeight / 2 - drawH / 2, drawW, drawH);
            panelRect = rect;
            Texture2D px = TextureAssets.MagicPixel.Value;

            //背景压暗
            sb.Draw(px, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * (0.55f * eased));

            //着色器面板（含外发光范围）
            DrawShaderPanel(sb, rect, eased, portal);

            //内容
            DrawPanelContent(sb, rect, portal, eased);

            if (rect.Contains(Main.mouseX, Main.mouseY) && eased > 0.2f) {
                Main.LocalPlayer.mouseInterface = true;
                Main.LocalPlayer.cursorItemIconEnabled = false;
            }
        }

        //═══ 1. 使用 AbandonedPortalPanel 着色器渲染面板背景 ═══
        private void DrawShaderPanel(SpriteBatch sb, Rectangle rect, float alpha, AbandonedPortal portal) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            Asset<Effect> effectAsset = EffectLoader.AbandonedPortalPanel;

            if (effectAsset?.Value == null) {
                DrawFallbackPanel(sb, rect, alpha, portal);
                return;
            }

            Rectangle ext = rect;
            ext.Inflate(EdgePad, EdgePad);

            float repair = portal.RepairProgress;
            float state = portal.State switch {
                AbandonedPortal.RepairState.Repaired => 2f,
                AbandonedPortal.RepairState.Repairing => 1f,
                _ => 0f,
            };

            Effect effect = effectAsset.Value;
            effect.Parameters["uTime"]?.SetValue(shaderTime);
            effect.Parameters["uAlpha"]?.SetValue(alpha * 0.97f);
            effect.Parameters["uResolution"]?.SetValue(new Vector2(ext.Width, ext.Height));
            effect.Parameters["uEdgePad"]?.SetValue((float)EdgePad);
            effect.Parameters["uRepair"]?.SetValue(repair);
            effect.Parameters["uState"]?.SetValue(state);
            effect.Parameters["uGlitch"]?.SetValue(MathHelper.Clamp(glitchTimer, 0f, 1f));

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp,
                DepthStencilState.None, RasterizerState.CullNone, effect, Main.UIScaleMatrix);

            sb.Draw(px, ext, Color.White);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }

        //降级面板：着色器未加载时仍能显示
        private static void DrawFallbackPanel(SpriteBatch sb, Rectangle rect, float alpha, AbandonedPortal portal) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            Color bg = new Color(14, 12, 9) * (0.95f * alpha);
            Color edge = new Color(190, 110, 50) * alpha;
            Color rust = new Color(140, 70, 38) * (0.7f * alpha);

            sb.Draw(px, rect, bg);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 3), edge);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 3, rect.Width, 3), rust);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 2, rect.Height), edge * 0.85f);
            sb.Draw(px, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), rust * 0.85f);
        }

        //═══ 2. 面板内容：标题、状态、诊断、进度条、按钮 ═══
        private void DrawPanelContent(SpriteBatch sb, Rectangle rect, AbandonedPortal portal, float alpha) {
            DynamicSpriteFont font = FontAssets.MouseText.Value;

            //── 状态色调：损毁=橙红，修复中=蓝橙，已修复=青蓝 ──
            Color accent = portal.State switch {
                AbandonedPortal.RepairState.Repaired => new Color(140, 230, 250),
                AbandonedPortal.RepairState.Repairing => Color.Lerp(new Color(245, 130, 60), new Color(150, 220, 245), portal.RepairProgress),
                _ => new Color(245, 110, 50),
            };
            Color title = new Color(240, 235, 220) * alpha;
            Color body = new Color(196, 200, 196) * alpha;
            Color dim = new Color(120, 110, 96) * alpha;
            Color warn = new Color(255, 170, 90) * alpha;

            // ── 标题 + 状态徽章 ──
            int padX = 36;
            int padY = 30;
            Vector2 titlePos = new(rect.X + padX, rect.Y + padY);
            Utils.DrawBorderString(sb, AbandonedPortalStrings.Title.Value, titlePos, title, 0.95f);

            //右上角状态徽章
            string status = portal.State switch {
                AbandonedPortal.RepairState.Repaired => AbandonedPortalStrings.StatusRepaired.Value,
                AbandonedPortal.RepairState.Repairing => AbandonedPortalStrings.StatusRepairing.Value,
                _ => AbandonedPortalStrings.StatusBroken.Value,
            };
            float blink = (MathF.Sin(shaderTime * (portal.State == AbandonedPortal.RepairState.Broken ? 4.5f : 1.6f)) * 0.3f + 0.7f);
            DrawStatusBadge(sb, new Rectangle(rect.Right - 232, rect.Y + 26, 200, 26), status, accent * (alpha * blink));

            //── 副标题 ──
            string subtitle = portal.State switch {
                AbandonedPortal.RepairState.Repairing => AbandonedPortalStrings.RepairingSubtitle.Value,
                AbandonedPortal.RepairState.Repaired => AbandonedPortalStrings.RepairedSubtitle.Value,
                _ => AbandonedPortalStrings.BrokenSubtitle.Value,
            };
            Utils.DrawBorderString(sb, subtitle, new Vector2(rect.X + padX, rect.Y + padY + 36f), accent * alpha, 0.74f);

            //装饰：分隔线
            DrawDecoLine(sb, new Rectangle(rect.X + padX, rect.Y + padY + 64, rect.Width - padX * 2, 2), accent * (alpha * 0.6f), alpha);

            //── 正文（带终端字头） ──
            string body1 = portal.State switch {
                AbandonedPortal.RepairState.Repairing => AbandonedPortalStrings.RepairingBody.Value,
                AbandonedPortal.RepairState.Repaired => AbandonedPortalStrings.RepairedBody.Value,
                _ => AbandonedPortalStrings.BrokenBody.Value,
            };
            string[] wrapped = Utils.WordwrapString(body1, font, rect.Width - 90, 6, out _);
            float bodyY = rect.Y + padY + 78f;
            for (int i = 0; i < wrapped.Length; i++) {
                if (string.IsNullOrEmpty(wrapped[i])) continue;
                Utils.DrawBorderString(sb, wrapped[i], new Vector2(rect.X + padX, bodyY + i * 22f), body, 0.66f);
            }

            //── 诊断行（终端式 [DIAG] 标签） ──
            string diag = portal.State switch {
                AbandonedPortal.RepairState.Repairing => AbandonedPortalStrings.DiagnosticRepairing.Value,
                AbandonedPortal.RepairState.Repaired => AbandonedPortalStrings.DiagnosticRepaired.Value,
                _ => AbandonedPortalStrings.DiagnosticBroken.Value,
            };
            string diagFull = AbandonedPortalStrings.DiagnosticHeader.Value + " " + diag;
            Utils.DrawBorderString(sb, diagFull, new Vector2(rect.X + padX, rect.Bottom - 156f), dim, 0.62f);

            //── 校准进度条 ──
            Rectangle progressRect = new(rect.X + padX, rect.Bottom - 124, rect.Width - padX * 2, 24);
            DrawRepairBar(sb, progressRect, portal.RepairProgress, accent, alpha);

            int percent = (int)(portal.RepairProgress * 100f);
            string progressText = string.Format(AbandonedPortalStrings.ProgressFormat.Value, percent);
            Utils.DrawBorderString(sb, progressText,
                new Vector2(rect.X + padX, progressRect.Bottom + 4f),
                portal.CanTeleport ? accent * alpha : warn, 0.62f);

            //── 按钮区 ──
            int btnH = 38;
            int btnPadY = rect.Bottom - 60;
            Rectangle close = new(rect.X + padX, btnPadY, 130, btnH);
            Rectangle primary = new(rect.Right - 274, btnPadY, 240, btnH);

            DrawTechButton(sb, close, AbandonedPortalStrings.Close.Value, dim * 1.6f, alpha,
                AbandonedPortalSession.RequestClose, false);

            if (portal.State == AbandonedPortal.RepairState.Broken) {
                DrawTechButton(sb, primary, AbandonedPortalStrings.StartRepair.Value, accent, alpha,
                    () => portal.StartRepair(), true);
            }
            else if (portal.State == AbandonedPortal.RepairState.Repaired) {
                DrawTechButton(sb, primary, AbandonedPortalStrings.Teleport.Value, accent, alpha,
                    () => portal.StartTransport(Main.LocalPlayer), true);
            }
            else {
                //修复中：进度按钮（不可点击，呼吸动效）
                DrawProgressButton(sb, primary, accent, portal.RepairProgress, alpha);
            }
        }

        //═══ 装饰：分隔线，左端有起始符号 ═══
        private static void DrawDecoLine(SpriteBatch sb, Rectangle rect, Color c, float alpha) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            //起始三角符号 ▌
            sb.Draw(px, new Rectangle(rect.X, rect.Y - 3, 4, 8), c);
            sb.Draw(px, new Rectangle(rect.X + 6, rect.Y, rect.Width - 6, rect.Height), c * 0.85f);
            //短装饰
            sb.Draw(px, new Rectangle(rect.Right - 26, rect.Y - 3, 26, 1), c * 0.7f);
            sb.Draw(px, new Rectangle(rect.Right - 12, rect.Y - 5, 12, 1), c * 0.5f);
        }

        //═══ 状态徽章 ═══
        private static void DrawStatusBadge(SpriteBatch sb, Rectangle rect, string text, Color color) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            sb.Draw(px, rect, Color.Black * (color.A / 255f * 0.45f));
            //左端 6px 浓色条
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 4, rect.Height), color);
            sb.Draw(px, new Rectangle(rect.X + 4, rect.Y, rect.Width - 4, 1), color * 0.85f);
            sb.Draw(px, new Rectangle(rect.X + 4, rect.Bottom - 1, rect.Width - 4, 1), color * 0.6f);

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text) * 0.55f;
            Utils.DrawBorderString(sb, text,
                new Vector2(rect.X + 12, rect.Center.Y - size.Y * 0.5f),
                color, 0.55f);
        }

        //═══ 修复进度条（带流光） ═══
        private void DrawRepairBar(SpriteBatch sb, Rectangle rect, float progress, Color accent, float alpha) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            //凹槽
            sb.Draw(px, rect, Color.Black * (0.65f * alpha));
            //内边框
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1), accent * (alpha * 0.55f));
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), accent * (alpha * 0.4f));

            //填充
            int fillW = (int)(rect.Width * MathHelper.Clamp(progress, 0f, 1f));
            if (fillW > 0) {
                Rectangle fill = new(rect.X, rect.Y, fillW, rect.Height);
                Color fillColor = accent * (alpha * 0.85f);
                sb.Draw(px, fill, fillColor * 0.25f);
                sb.Draw(px, new Rectangle(fill.X, fill.Y, fill.Width, 2), fillColor);
                sb.Draw(px, new Rectangle(fill.X, fill.Bottom - 2, fill.Width, 2), fillColor * 0.7f);

                //流光
                float flow = (shaderTime * 0.55f) % 1f;
                int flowX = fill.X + (int)(flow * fill.Width);
                int beam = 28;
                for (int dx = -beam; dx <= beam; dx++) {
                    int x = flowX + dx;
                    if (x < fill.X || x >= fill.Right) continue;
                    float f = 1f - Math.Abs(dx) / (float)beam;
                    sb.Draw(px, new Rectangle(x, fill.Y, 1, fill.Height), Color.White * (alpha * 0.35f * f * f));
                }
            }

            //刻度（每 10%）
            for (int i = 1; i < 10; i++) {
                int x = rect.X + rect.Width * i / 10;
                int h = (i % 5 == 0) ? rect.Height : (rect.Height / 2);
                sb.Draw(px, new Rectangle(x, rect.Bottom - h, 1, h), accent * (alpha * 0.25f));
            }
        }

        //═══ 主按钮：带边框、悬停发光 ═══
        private void DrawTechButton(SpriteBatch sb, Rectangle rect, string text, Color color, float alpha,
            Action onClick, bool isPrimary) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            bool hover = rect.Contains(Main.mouseX, Main.mouseY);

            //背景
            Color bg = (hover ? color * 0.32f : Color.Black * 0.40f) * alpha;
            sb.Draw(px, rect, bg);

            //双层边框
            Color edge = color * (alpha * (hover ? 1f : 0.7f));
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 2), edge);
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), edge * 0.7f);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 1, rect.Height), edge * 0.85f);
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), edge * 0.55f);

            //四角小切角
            int cs = 5;
            sb.Draw(px, new Rectangle(rect.X, rect.Y, cs, 2), color * alpha);
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 2, cs), color * alpha);
            sb.Draw(px, new Rectangle(rect.Right - cs, rect.Bottom - 2, cs, 2), color * (alpha * 0.7f));
            sb.Draw(px, new Rectangle(rect.Right - 2, rect.Bottom - cs, 2, cs), color * (alpha * 0.7f));

            //主按钮悬停时左端添加流动条
            if (isPrimary && hover) {
                float w = ((shaderTime * 1.4f) % 1f) * rect.Width;
                int beam = 64;
                for (int dx = -beam; dx <= beam; dx++) {
                    int x = rect.X + (int)w + dx;
                    if (x < rect.X || x >= rect.Right) continue;
                    float f = 1f - Math.Abs(dx) / (float)beam;
                    sb.Draw(px, new Rectangle(x, rect.Y + 2, 1, rect.Height - 4), color * (alpha * 0.28f * f * f));
                }
            }

            //按钮标识（左端 ▌）
            sb.Draw(px, new Rectangle(rect.X + 10, rect.Center.Y - 7, 3, 14), color * alpha);

            //文本
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text) * 0.66f;
            Utils.DrawBorderString(sb, text,
                new Vector2(rect.X + 22 + (rect.Width - 22 - size.X) * 0.5f, rect.Center.Y - size.Y * 0.5f),
                Color.White * alpha, 0.66f);

            if (hover) {
                Main.LocalPlayer.mouseInterface = true;
                if (Main.mouseLeft && Main.mouseLeftRelease) {
                    Main.mouseLeftRelease = false;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    onClick?.Invoke();
                }
            }
        }

        //═══ 进度按钮：修复进行中显示，无法点击 ═══
        private void DrawProgressButton(SpriteBatch sb, Rectangle rect, Color color, float progress, float alpha) {
            Texture2D px = TextureAssets.MagicPixel.Value;
            //底色
            sb.Draw(px, rect, Color.Black * (alpha * 0.55f));
            int fillW = (int)(rect.Width * MathHelper.Clamp(progress, 0f, 1f));
            if (fillW > 0) {
                sb.Draw(px, new Rectangle(rect.X, rect.Y, fillW, rect.Height), color * (alpha * 0.30f));
                //顶/底高光
                sb.Draw(px, new Rectangle(rect.X, rect.Y, fillW, 2), color * (alpha * 0.85f));
                sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 2, fillW, 2), color * (alpha * 0.55f));
            }
            //外边框
            sb.Draw(px, new Rectangle(rect.X, rect.Y, rect.Width, 1), color * (alpha * 0.55f));
            sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color * (alpha * 0.4f));
            sb.Draw(px, new Rectangle(rect.X, rect.Y, 1, rect.Height), color * (alpha * 0.55f));
            sb.Draw(px, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color * (alpha * 0.4f));

            //文本：CALIBRATING xx%
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string txt = string.Format(AbandonedPortalStrings.ProgressFormat.Value, (int)(progress * 100f));
            Vector2 size = font.MeasureString(txt) * 0.66f;
            float pulse = MathF.Sin(shaderTime * 3.4f) * 0.18f + 0.82f;
            Utils.DrawBorderString(sb, txt,
                new Vector2(rect.Center.X - size.X * 0.5f, rect.Center.Y - size.Y * 0.5f),
                color * (alpha * pulse), 0.66f);
        }
    }

}
