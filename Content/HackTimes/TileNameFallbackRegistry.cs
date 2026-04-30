using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;

namespace CalamityOverhaul.Content.HackTimes
{
    /// <summary>
    /// 物块名称最终兜底表。
    /// <br/>测试时遇到异常名称，可在 <see cref="RegisterInitialData"/> 里继续追加 Register / RegisterStyle。
    /// </summary>
    internal static class TileNameFallbackRegistry
    {
        private static readonly Dictionary<int, string> namesByType = [];
        private static readonly Dictionary<TileStyleKey, string> namesByStyle = [];

        static TileNameFallbackRegistry() {
            RegisterInitialData();
        }

        public static void Register(int tileType, string name) {
            if (tileType < 0 || string.IsNullOrWhiteSpace(name)) return;
            namesByType[tileType] = name.Trim();
        }

        public static void RegisterStyle(int tileType, int style, string name) {
            if (tileType < 0 || style < 0 || string.IsNullOrWhiteSpace(name)) return;
            namesByStyle[new TileStyleKey(tileType, style)] = name.Trim();
        }

        public static bool TryGetName(Tile tile, int tileType, out string name) {
            int style = GetBestEffortStyle(tile, tileType);
            if (style >= 0 && namesByStyle.TryGetValue(new TileStyleKey(tileType, style), out name)) {
                return true;
            }

            return namesByType.TryGetValue(tileType, out name);
        }

        public static bool TryGetName(int tileType, out string name) {
            return namesByType.TryGetValue(tileType, out name);
        }

        private static int GetBestEffortStyle(Tile tile, int tileType) {
            TileObjectData data = TileObjectData.GetTileData(tileType, 0);
            if (data == null) return 0;

            int frameWidth = data.CoordinateWidth + data.CoordinatePadding;
            int frameHeight = data.CoordinateHeights.Length > 0
                ? data.CoordinateHeights[0] + data.CoordinatePadding
                : 0;
            if (frameWidth <= 0 || frameHeight <= 0) return 0;

            int styleWidth = Math.Max(frameWidth * data.Width, frameWidth);
            int styleHeight = Math.Max(frameHeight * data.Height, frameHeight);
            return data.StyleHorizontal
                ? tile.TileFrameX / styleWidth
                : tile.TileFrameY / styleHeight;
        }

        private static void RegisterInitialData() {
            // 基础数据来自 terraria.wiki.gg/zh/wiki/图格_ID，后续测试员可按异常样本继续追加。
            Register(TileID.Dirt, "土块");
            Register(TileID.Stone, "石块");
            Register(TileID.Grass, "草");
            Register(TileID.Plants, "野生植物");
            Register(TileID.Torches, "火把");
            Register(TileID.Trees, "树");
            Register(TileID.Iron, "铁矿");
            Register(TileID.Copper, "铜矿");
            Register(TileID.Gold, "金矿");
            Register(TileID.Silver, "银矿");
            Register(TileID.ClosedDoor, "门");
            Register(TileID.OpenDoor, "门");
            Register(TileID.Heart, "生命水晶");
            Register(TileID.Bottles, "玻璃瓶");
            Register(TileID.Tables, "桌");
            Register(TileID.Chairs, "椅");
            Register(TileID.Anvils, "砧");
            Register(TileID.Furnaces, "熔炉");
            Register(TileID.WorkBenches, "工作台");
            Register(TileID.Platforms, "平台");
            Register(TileID.Saplings, "树苗");
            Register(TileID.Containers, "宝箱");
            Register(TileID.Demonite, "魔矿");
            Register(TileID.CorruptGrass, "腐化草");
            Register(TileID.CorruptPlants, "野生腐化植物");
            Register(TileID.Ebonstone, "黑檀石块");
            Register(TileID.DemonAltar, "祭坛");
            Register(TileID.Sunflower, "向日葵");
            Register(TileID.Pots, "罐子");
            Register(TileID.PiggyBank, "猪猪存钱罐");
            Register(TileID.WoodBlock, "木材");
            Register(TileID.ShadowOrbs, "暗影珠");
            Register(TileID.CorruptThorns, "腐化荆棘");
            Register(TileID.Candles, "蜡烛");
            Register(TileID.Chandeliers, "吊灯");
            Register(TileID.Jackolanterns, "杰克南瓜灯");
            Register(TileID.Presents, "礼物");
            Register(TileID.Meteorite, "陨石");
            Register(TileID.GrayBrick, "灰砖");
            Register(TileID.RedBrick, "红砖");
            Register(TileID.ClayBlock, "黏土块");
            Register(TileID.BlueDungeonBrick, "蓝砖");
            Register(TileID.HangingLanterns, "吊灯笼");
            Register(TileID.GreenDungeonBrick, "绿砖");
            Register(TileID.PinkDungeonBrick, "粉砖");
            Register(TileID.GoldBrick, "金砖");
            Register(TileID.SilverBrick, "银砖");
            Register(TileID.CopperBrick, "铜砖");
            Register(TileID.Spikes, "尖刺");
            Register(TileID.WaterCandle, "水蜡烛");
            Register(TileID.Books, "书");
            Register(TileID.Cobweb, "蛛网");
            Register(TileID.Vines, "藤蔓");
            Register(TileID.Sand, "沙块");
            Register(TileID.Glass, "玻璃");
            Register(TileID.Signs, "标牌");
            Register(TileID.Obsidian, "黑曜石");
            Register(TileID.Ash, "灰烬块");
            Register(TileID.Hellstone, "狱石");
            Register(TileID.Mud, "泥块");
            Register(TileID.JungleGrass, "丛林草");
            Register(TileID.JunglePlants, "野生丛林植物");
            Register(TileID.JungleVines, "丛林藤蔓");
            Register(TileID.Sapphire, "蓝玉石块");
            Register(TileID.Ruby, "红玉石块");
            Register(TileID.Emerald, "翡翠石块");
            Register(TileID.Topaz, "黄玉石块");
            Register(TileID.Amethyst, "紫晶石块");
            Register(TileID.Diamond, "钻石石块");
            Register(TileID.JungleThorns, "丛林荆棘");
            Register(TileID.MushroomGrass, "蘑菇草");
            Register(TileID.MushroomPlants, "发光蘑菇");
            Register(TileID.MushroomTrees, "巨型发光蘑菇");
            Register(TileID.Plants2, "高茎草");
            Register(TileID.JunglePlants2, "高野生丛林植物");
            Register(TileID.ObsidianBrick, "黑曜石砖");
            Register(TileID.HellstoneBrick, "狱石砖");
            Register(TileID.Hellforge, "地狱熔炉");
            Register(TileID.ClayPot, "陶盆");
            Register(TileID.Beds, "床");
            Register(TileID.Cactus, "仙人掌");
            Register(TileID.Coral, "珊瑚");
            Register(TileID.ImmatureHerbs, "草药幼苗");
            Register(TileID.MatureHerbs, "成熟草药");
            Register(TileID.BloomingHerbs, "开花草药");
            Register(TileID.Tombstones, "墓碑");
            Register(TileID.Loom, "织布机");
            Register(TileID.Pianos, "钢琴");
            Register(TileID.Dressers, "梳妆台");
            Register(TileID.Benches, "长椅");
            Register(TileID.Bathtubs, "浴缸");

            RegisterStyle(TileID.Torches, 0, "火把");
            RegisterStyle(TileID.Torches, 1, "蓝火把");
            RegisterStyle(TileID.Torches, 2, "红火把");
            RegisterStyle(TileID.Torches, 3, "绿火把");
            RegisterStyle(TileID.Torches, 4, "紫火把");
            RegisterStyle(TileID.Torches, 5, "白火把");
            RegisterStyle(TileID.Torches, 6, "黄火把");
            RegisterStyle(TileID.Torches, 7, "恶魔火把");
            RegisterStyle(TileID.Torches, 8, "诅咒火把");
            RegisterStyle(TileID.Torches, 9, "冰雪火把");
            RegisterStyle(TileID.Torches, 10, "橙火把");
            RegisterStyle(TileID.Torches, 11, "灵液火把");
            RegisterStyle(TileID.Torches, 12, "超亮火把");
            RegisterStyle(TileID.Torches, 13, "骨头火把");
            RegisterStyle(TileID.Torches, 14, "彩虹火把");
            RegisterStyle(TileID.Torches, 15, "粉火把");
            RegisterStyle(TileID.Torches, 16, "沙漠火把");
            RegisterStyle(TileID.Torches, 17, "珊瑚火把");
            RegisterStyle(TileID.Torches, 18, "腐化火把");
            RegisterStyle(TileID.Torches, 19, "猩红火把");
            RegisterStyle(TileID.Torches, 20, "神圣火把");
            RegisterStyle(TileID.Torches, 21, "丛林火把");
            RegisterStyle(TileID.Torches, 22, "蘑菇火把");
            RegisterStyle(TileID.Torches, 23, "以太火把");

            RegisterStyle(TileID.Trees, 0, "森林树");
            RegisterStyle(TileID.Trees, 1, "乌木树");
            RegisterStyle(TileID.Trees, 2, "红木树");
            RegisterStyle(TileID.Trees, 3, "珍珠木树");
            RegisterStyle(TileID.Trees, 4, "针叶树");
            RegisterStyle(TileID.Trees, 5, "暗影木树");
            RegisterStyle(TileID.Trees, 7, "巨型发光蘑菇");

            RegisterStyle(TileID.Anvils, 0, "铁砧");
            RegisterStyle(TileID.Anvils, 1, "铅砧");
            RegisterStyle(TileID.DemonAltar, 0, "恶魔祭坛");
            RegisterStyle(TileID.DemonAltar, 1, "猩红祭坛");
        }

        private readonly record struct TileStyleKey(int TileType, int Style);
    }
}
