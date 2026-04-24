using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.GatlinTurrets;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.LaserCannons;
using InnoVault.Actors;
using System;
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
            if (ArchitectureRegistry.Entries.Count == 0
                && ArchitectureRegistry.Connectors.Count == 0
                && GatlinTurretRegistry.Entries.Count == 0
                && LaserCannonRegistry.Entries.Count == 0) return;

            //先刷连接段（与建筑Actor同层AfterTiles），再刷建筑
            //同层内先生成的WhoAmI较小者更早绘制，因而桥/管位于建筑之后绘制不会遮挡主建筑
            foreach (var link in ArchitectureRegistry.Connectors) {
                Vector2 position = new(Math.Min(link.StartX, link.EndX), link.StartY);
                int idx = ActorLoader.NewActor<ArchitectureConnectorActor>(position, Vector2.Zero);
                if (idx < 0 || idx >= ActorLoader.Actors.Length) continue;
                if (ActorLoader.Actors[idx] is not ArchitectureConnectorActor connector) continue;
                connector.KindByte = (byte)link.Kind;
                connector.StartX = link.StartX;
                connector.StartY = link.StartY;
                connector.EndX = link.EndX;
                connector.OnSpawn();
                connector.NetUpdate = true;
            }

            foreach (var entry in ArchitectureRegistry.Entries) {
                Vector2 position = new(entry.PixelX, entry.PixelY);
                //信号塔是独立的复杂建筑，从通用ArchitectureActor管线中分叉出来
                if (entry.Type == ArchitectureType.SignalTower) {
                    int towerIdx = ActorLoader.NewActor<SignalTowers.SignalTowerActor>(position, Vector2.Zero);
                    if (towerIdx < 0 || towerIdx >= ActorLoader.Actors.Length) continue;
                    if (ActorLoader.Actors[towerIdx] is not SignalTowers.SignalTowerActor tower) continue;
                    tower.OnSpawn();
                    tower.NetUpdate = true;
                    continue;
                }

                int idx = ActorLoader.NewActor<ArchitectureActor>(position, Vector2.Zero);
                if (idx < 0 || idx >= ActorLoader.Actors.Length) continue;
                if (ActorLoader.Actors[idx] is not ArchitectureActor actor) continue;
                //显式设置SyncVar字段并触发网络同步，确保客户端也能拿到正确的建筑类型
                actor.TypeByte = (byte)entry.Type;
                actor.FlipX = entry.FlipX;
                actor.OnSpawn();
                actor.NetUpdate = true;
            }

            //加特林炮台：沿核心桥刷出，与建筑共用同样的扭曲可见度管线
            foreach (var turret in GatlinTurretRegistry.Entries) {
                Vector2 position = new(turret.PedestalPixelX, turret.PedestalPixelY);
                int idx = ActorLoader.NewActor<GatlinTurretActor>(position, Vector2.Zero);
                if (idx < 0 || idx >= ActorLoader.Actors.Length) continue;
                if (ActorLoader.Actors[idx] is not GatlinTurretActor actor) continue;
                actor.InitialFaceLeft = turret.InitialFaceLeft;
                actor.OnSpawn();
                actor.NetUpdate = true;
            }

            //巨型激光炮台：悬浮在两侧浮岛外缘上空
            foreach (var cannon in LaserCannonRegistry.Entries) {
                Vector2 position = new(cannon.PedestalPixelX, cannon.PedestalPixelY);
                int idx = ActorLoader.NewActor<LaserCannonTurretActor>(position, Vector2.Zero);
                if (idx < 0 || idx >= ActorLoader.Actors.Length) continue;
                if (ActorLoader.Actors[idx] is not LaserCannonTurretActor actor) continue;
                actor.InitialFaceLeft = cannon.InitialFaceLeft;
                actor.OnSpawn();
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
