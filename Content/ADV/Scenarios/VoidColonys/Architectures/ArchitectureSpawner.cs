using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures
{
    /// <summary>
    /// 虚空聚落建筑Actor生成器
    /// 进入虚空聚落且注册表中有规划数据时，一次性按记录批量生成ArchitectureActor
    /// 多人模式下仅在服务器/单机上生成，客户端通过Actor网络同步收到实例
    /// </summary>
    internal class ArchitectureSpawner : ModSystem
    {
        /// <summary>本次会话是否已完成生成，防止重复刷</summary>
        private bool spawned;

        public override void PostUpdateEverything() {
            //客户端不主动刷Actor，避免重复生成
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (!VoidColony.Active) {
                spawned = false;
                return;
            }
            if (spawned) return;
            if (ArchitectureRegistry.Entries.Count == 0) return;

            //注册表已有规划数据时，按记录一次性生成所有建筑Actor
            foreach (var entry in ArchitectureRegistry.Entries) {
                Vector2 position = new(entry.PixelX, entry.PixelY);
                int idx = ActorLoader.NewActor<ArchitectureActor>(position, Vector2.Zero);
                if (idx < 0 || idx >= ActorLoader.Actors.Length) continue;
                if (ActorLoader.Actors[idx] is not ArchitectureActor actor) continue;
                //显式设置SyncVar字段并触发网络同步，确保客户端也能拿到正确的建筑类型
                actor.TypeByte = (byte)entry.Type;
                actor.OnSpawn(entry.Type);
                actor.NetUpdate = true;
            }
            spawned = true;
        }

        public override void OnWorldUnload() {
            //离开世界时重置标记，下次进入时重新刷
            spawned = false;
        }
    }
}
