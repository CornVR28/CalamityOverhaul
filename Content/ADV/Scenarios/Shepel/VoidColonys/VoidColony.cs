using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.VoidColonys
{
    /// <summary>
    /// 虚空聚落 - 嘉登虚空实验室集群所在的亚空间维度
    /// 到处是虚空，现实和亚空间的屏障非常薄弱
    /// 地形由漂浮在虚空中的岛屿群组成，核心岛屿上建有最大的实验室
    /// </summary>
    internal class VoidColony : Subworld
    {
        public static VoidColony Instance { get; private set; }

        /// <summary>
        /// 世界宽度 - 使用中等尺寸，足够容纳多个浮岛实验室
        /// </summary>
        public override int Width => 4200;

        /// <summary>
        /// 世界高度 - 较高的世界以容纳上下分布的浮岛
        /// </summary>
        public override int Height => 1800;

        public static bool Active => SubworldSystem.IsActive<VoidColony>();

        public override List<GenPass> Tasks => [new VoidColonyGen()];

        public static void Enter() {
            SubworldSystem.Enter<VoidColony>();
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

        }

        public override void OnLoad() {
            //虚空维度永远处于昏暗的"白昼"状态
            Main.dayTime = true;
            Main.time = Main.dayLength / 2;
            //将地表线推到底部，整个世界视为"天空"
            Main.worldSurface = Main.maxTilesY - 2;
            Main.rockLayer = Main.maxTilesY - 1;
        }

        public override void Update() {
            //保持时间静止
            Main.dayTime = true;
            Main.time = Main.dayLength / 2;

            //更新机械和实体
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

        public override float GetGravity(Entity entity) {
            //亚空间中重力略微降低，营造漂浮感
            return 0.85f;
        }
    }
}
