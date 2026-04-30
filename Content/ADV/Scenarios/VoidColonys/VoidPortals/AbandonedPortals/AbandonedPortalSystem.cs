using CalamityOverhaul.OtherMods.SubWorld;
using InnoVault.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.VoidPortals.AbandonedPortals
{
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

            if (Main.gameMenu || VoidColony.Active || VaultUtils.isClient || SubWorldRef.AnyActiveSubWorld()) {
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
}
