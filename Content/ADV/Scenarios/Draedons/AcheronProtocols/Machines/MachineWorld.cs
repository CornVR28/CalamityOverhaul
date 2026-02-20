using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
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

        }

        public override void OnExit() {
            base.OnExit();
        }

        public override void OnLoad() {
            Main.dayTime = false;
            Main.time = Main.nightLength - 2000;
            //将地表线和岩石层推到世界底部，防止地下背景墙显示
            Main.worldSurface = Main.maxTilesY - 2;
            Main.rockLayer = Main.maxTilesY - 1;
        }

        public override void Update() {
            Wiring.UpdateMech();

            TileEntity.UpdateStart();
            foreach (TileEntity te in TileEntity.ByID.Values) {
                te.Update();
            }
            TileEntity.UpdateEnd();
            for (int i = 0; i < 10; i++) {
                Liquid.UpdateLiquid();
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
