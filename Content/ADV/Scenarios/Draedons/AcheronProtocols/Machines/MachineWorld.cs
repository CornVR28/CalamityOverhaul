using CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.ApolliaActors;
using CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.LandingScens;
using InnoVault.Actors;
using InnoVault.GameSystem;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines
{
    internal class MachineWorld : Subworld
    {
        public static MachineWorld Instance { get; private set; }

        //使用原版小世界标准尺寸，确保WorldGen.GenerateWorld能正常运行
        public override int Width => 4200;

        public override int Height => 1200;

        public static bool Active => SubworldSystem.IsActive<MachineWorld>();

        /// <summary>
        /// 着陆演出是否已完成（玩家已从空降仓弹出）
        /// 用于避免每次Update都重复生成着陆Actor
        /// </summary>
        private static bool landingCompleted;

        /// <summary>
        /// 着陆演出初始化是否已执行
        /// </summary>
        private static bool landingInitialized;

        /// <summary>
        /// 阿波利娅出场延迟计时器（着陆完成后开始倒计时）
        /// </summary>
        private static int apolliaSpawnDelay;

        /// <summary>
        /// 阿波利娅是否已经生成
        /// </summary>
        private static bool apolliaSpawned;

        /// <summary>
        /// 着陆完成时玩家的位置，用于确定阿波利娅降落点
        /// </summary>
        private static Vector2 landingPodCenter;

        public override List<GenPass> Tasks => [new MachineGen()];

        public static void Enter() {
            SubworldSystem.Enter<MachineWorld>();
        }

        public static void Exit() {
            SubworldSystem.Exit();
        }

        public override void Load() {
            Instance = this;
        }

        public override void Unload() {
            Instance = null;
        }

        public override void OnEnter() {
            //重置着陆演出状态
            landingCompleted = false;
            landingInitialized = false;
            apolliaSpawnDelay = 0;
            apolliaSpawned = false;
            landingPodCenter = Vector2.Zero;
        }

        public override void OnExit() {
            base.OnExit();
        }

        public override void OnLoad() {
            Main.dayTime = true;
            Main.time = Main.dayLength / 2;
            //将地表线和岩石层推到世界底部，防止地下背景墙显示
            Main.worldSurface = Main.maxTilesY - 2;
            Main.rockLayer = Main.maxTilesY - 1;
        }

        public override void Update() {
            Main.dayTime = true;
            Main.time = Main.dayLength / 2;

            Wiring.UpdateMech();

            TileEntity.UpdateStart();
            foreach (TileEntity te in TileEntity.ByID.Values) {
                te.Update();
            }
            TileEntity.UpdateEnd();
            for (int i = 0; i < 10; i++) {
                Liquid.UpdateLiquid();
            }

            //初始化着陆演出
            InitializeLandingScene();

            //着陆完成后触发阿波利娅出场
            if (landingCompleted && !apolliaSpawned) {
                apolliaSpawnDelay++;
                //约2秒后（120帧）生成阿波利娅
                if (apolliaSpawnDelay >= 120) {
                    SpawnApollia();
                    apolliaSpawned = true;
                }
            }

            //着陆完成后才开始特斯拉效果（避免着陆阶段干扰）
            if (landingCompleted && apolliaSpawned) {
                SpawnTeslaEffect();
            }
        }

        /// <summary>
        /// 初始化着陆演出——在玩家首次进入世界时生成坠毁的空降仓
        /// </summary>
        private static void InitializeLandingScene() {
            if (landingInitialized) {
                //检查着陆是否已完成（玩家已弹出）
                if (!landingCompleted) {
                    Player player = Main.LocalPlayer;
                    if (player != null && player.active
                        && player.TryGetOverride<MachineWorldLandingPlayer>(out var landingPlayer)) {
                        if (!landingPlayer.LandingActive) {
                            landingCompleted = true;
                            landingPodCenter = player.Center;
                        }
                    }
                }
                return;
            }

            Player localPlayer = Main.LocalPlayer;
            if (localPlayer == null || !localPlayer.active) return;

            landingInitialized = true;

            //设置玩家着陆状态
            if (localPlayer.TryGetOverride<MachineWorldLandingPlayer>(out var lp)) {
                lp.LandingActive = true;
            }

            //在玩家出生点生成坠毁的空降仓Actor
            Vector2 spawnPos = localPlayer.Center;
            ActorLoader.NewActor<MachineWorldLandingActor>(spawnPos, Vector2.Zero);

            //激活屏幕特效
            MachineWorldLandingDrawSystem.Activate();
        }

        /// <summary>
        /// 生成阿波利娅Actor并触发着陆演出
        /// </summary>
        private static void SpawnApollia() {
            int index = ActorLoader.NewActor<ApolliaActor>(Vector2.Zero, Vector2.Zero);
            if (index >= 0 && index < ActorLoader.MaxActorCount
                && ActorLoader.Actors[index] is ApolliaActor apollia) {
                apollia.StartLandingCutscene(landingPodCenter);
            }
        }

        private static void SpawnTeslaEffect() {
            if (!Main.rand.NextBool(68)) {
                return;
            }

            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead) {
                return;
            }

            Vector2 spawnPos = player.Center + new Vector2(
                Main.rand.Next(-900, 900),
                Main.rand.Next(-1200, -800));

            Vector2 velocity = new Vector2(0, 8);

            int projType = ModContent.ProjectileType<MachineTesla>();
            int proj = Projectile.NewProjectile(
                new EntitySource_WorldEvent(),
                spawnPos,
                velocity,
                projType,
                80,
                2,
                Main.myPlayer);

            if (proj >= 0 && proj < Main.maxProjectiles
                && Main.projectile[proj].ModProjectile is MachineTesla tesla) {
                tesla.TargetPosition = MachineTesla.FindHighestSolidInAreaBelow(spawnPos, 50, 800);
            }
        }

        public override bool ChangeAudio() {
            return base.ChangeAudio();
        }

        public override void DrawMenu(GameTime gameTime) {
            base.DrawMenu(gameTime);
        }

        public override void DrawSetup(GameTime gameTime) {
            base.DrawSetup(gameTime);
        }

        public override float GetGravity(Entity entity) {
            return base.GetGravity(entity);
        }
    }
}
