using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TimeShift;
using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.SignalTowers
{
    /// <summary>
    /// 信号塔闪电调度系统
    /// 遍历当前所有信号塔Actor，按随机间隔在塔顶触发一道红色闪电
    /// 仅在过去时代播放，保持与建筑群"过去显像、现在虚化"的时空语义一致
    /// 服务器端/单机端负责Spawn，客户端经由Actor网络同步收到画面
    /// </summary>
    internal class SignalTowerLightningSystem : ModSystem
    {
        //相邻两次闪电的随机冷却，帧
        private const int CooldownMin = 90;
        private const int CooldownMax = 300;

        private int cooldown;

        public override void PostUpdateEverything() {
            //闪电的生成由Actor主动同步，客户端不重复生成
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            if (!VoidColony.Active || !VoidTimeShiftSystem.InPast) {
                cooldown = 0;
                return;
            }

            if (cooldown > 0) {
                cooldown--;
                return;
            }

            //遍历所有Actor挑出信号塔
            int towerCount = 0;
            int pickedIdx = -1;
            var actors = ActorLoader.Actors;
            for (int i = 0; i < actors.Length; i++) {
                if (actors[i] is SignalTowerActor tower && tower.Active) {
                    towerCount++;
                    //水库抽样：等概率随机挑一座
                    if (Main.rand.Next(towerCount) == 0) pickedIdx = i;
                }
            }

            //没有信号塔就再等一会儿
            if (pickedIdx < 0) {
                cooldown = CooldownMin;
                return;
            }

            if (actors[pickedIdx] is not SignalTowerActor target) {
                cooldown = CooldownMin;
                return;
            }

            //塔顶中心下沉120px，让画布底部被塔体遮住，矩形边界不再外露
            Vector2 strikePoint = target.Position + new Vector2(target.Width * 0.5f, 120f);
            TrySpawnBolt(strikePoint, target);

            cooldown = Main.rand.Next(CooldownMin, CooldownMax + 1);
        }

        private static void TrySpawnBolt(Vector2 strikePoint, SignalTowerActor tower) {
            int w = SignalTowerLightningActor.BoltWidthPx;
            int h = SignalTowerLightningActor.BoltHeightPx;
            //画布底部中点锚在打击点，因此画布左上角在打击点的(-W/2, -H)处
            Vector2 topLeft = strikePoint - new Vector2(w * 0.5f, h);

            int idx = ActorLoader.NewActor<SignalTowerLightningActor>(topLeft, Vector2.Zero);
            if (idx < 0 || idx >= ActorLoader.Actors.Length) return;
            if (ActorLoader.Actors[idx] is not SignalTowerLightningActor bolt) return;

            int lifeFrames = Main.rand.Next(36, 68);
            bolt.InitLifeFrames = lifeFrames;
            float seedValue = Main.rand.NextFloat() * 100f;
            bolt.InitSeed = seedValue;
            bolt.OnSpawn();
            bolt.NetUpdate = true;

            //同步触发信号塔自身的过电滤镜，过电持续时间比闪电长以体现余韵
            tower?.BeginElectrify(lifeFrames + 60, seedValue);

            //雷击音效
            SoundEngine.PlaySound(SoundID.Thunder, strikePoint);
        }
    }
}
