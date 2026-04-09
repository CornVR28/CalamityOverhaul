using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime
{
    /// <summary>
    /// 物块扫描数据实现
    /// <br/>扫描家具、工作站、容器等可交互物块的属性信息
    /// </summary>
    internal class TileScannable : IScannable
    {
        //物块的格子坐标
        private readonly int tileX;
        private readonly int tileY;

        public TileScannable(int tileX, int tileY) {
            this.tileX = tileX;
            this.tileY = tileY;
        }

        public Vector2 WorldCenter => new(tileX * 16f + 8f, tileY * 16f + 8f);

        public bool IsValid {
            get {
                if (tileX < 0 || tileX >= Main.maxTilesX
                    || tileY < 0 || tileY >= Main.maxTilesY) return false;
                Tile tile = Main.tile[tileX, tileY];
                return tile.HasTile;
            }
        }

        public bool IsHackable => false;

        public int ScanRowCount => 5;

        public void BuildScanData(string[] labels, string[] values, Color[] colors) {
            if (!IsValid) return;
            Tile tile = Main.tile[tileX, tileY];
            int type = tile.TileType;

            //NAME
            labels[0] = HackTime.TileScanName.Value;
            values[0] = GetTileName(type);
            colors[0] = HackTheme.TextBright;

            //CLASS（分类）
            labels[1] = HackTime.TileScanClass.Value;
            values[1] = GetTileClass(type);
            colors[1] = GetTileClassColor(type);

            //SIZE（多物块尺寸）
            labels[2] = HackTime.TileScanSize.Value;
            TileObjectData data = TileObjectData.GetTileData(type, 0);
            if (data != null) {
                values[2] = $"{data.Width} x {data.Height}";
                colors[2] = HackTheme.Accent;
            }
            else {
                values[2] = "1 x 1";
                colors[2] = HackTheme.TextDim;
            }

            //HARDNESS（硬度/破坏信息）
            labels[3] = HackTime.TileScanHardness.Value;
            values[3] = GetHardnessText(type);
            colors[3] = HackTheme.TextBright;

            //STATUS（物块状态）
            labels[4] = HackTime.TileScanStatus.Value;
            values[4] = GetStatusText(tile, type);
            colors[4] = GetStatusColor(tile, type);
        }

        /// <summary>
        /// 获取物块的显示名称
        /// </summary>
        private static string GetTileName(int type) {
            //优先尝试获取ModTile名称
            if (type >= TileID.Count) {
                ModTile modTile = TileLoader.GetTile(type);
                if (modTile != null) {
                    //tModLoader的GetMapOption需要传入坐标参数
                    string name = Lang.GetMapObjectName(type);
                    if (!string.IsNullOrEmpty(name)) return name;
                    return modTile.Name;
                }
            }

            //原版物块用地图名
            string mapName = Lang.GetMapObjectName(type);
            if (!string.IsNullOrEmpty(mapName)) return mapName;

            return $"TILE#{type:X3}";
        }

        /// <summary>
        /// 获取物块的分类文本
        /// </summary>
        private static string GetTileClass(int type) {
            if (IsCraftingStation(type)) return HackTime.TileScanCrafting.Value;
            if (IsContainer(type)) return HackTime.TileScanContainer.Value;
            if (IsLightSource(type)) return HackTime.TileScanLight.Value;
            if (IsFurniture(type)) return HackTime.TileScanFurniture.Value;
            return HackTime.TileScanBlock.Value;
        }

        /// <summary>
        /// 获取物块分类对应的颜色
        /// </summary>
        private static Color GetTileClassColor(int type) {
            if (IsCraftingStation(type)) return HackTheme.Uploading;
            if (IsContainer(type)) return HackTheme.AccentAlt;
            if (IsLightSource(type)) return new Color(200, 200, 80);
            if (IsFurniture(type)) return HackTheme.Accent;
            return HackTheme.TextDim;
        }

        /// <summary>
        /// 获取物块硬度文本
        /// </summary>
        private static string GetHardnessText(int type) {
            //地牢砖、丛林蜥蜴砖等需要特定工具
            if (Main.tileDungeon[type]) return HackTime.TileScanDungeon.Value;
            if (type == TileID.LihzahrdBrick || type == TileID.LihzahrdAltar)
                return HackTime.TileScanLihzahrd.Value;

            //根据镐力需求判断
            int minPick = GetMinPickPower(type);
            if (minPick >= 200) return HackTime.TileScanHardnessExtreme.Value;
            if (minPick >= 100) return HackTime.TileScanHardnessHigh.Value;
            if (minPick > 0) return HackTime.TileScanHardnessNormal.Value;
            return HackTime.TileScanHardnessLow.Value;
        }

        /// <summary>
        /// 获取物块状态文本
        /// </summary>
        private static string GetStatusText(Tile tile, int type) {
            //火把、蜡烛等光源检查开关状态
            if (IsLightSource(type)) {
                //物块帧X用于判断开关状态（不同物块可能不同，这里做通用处理）
                bool isOn = tile.TileFrameX < 66 || Main.tileFrameImportant[type] && tile.TileFrameX == 0;
                return isOn ? HackTime.TileScanActive.Value : HackTime.TileScanInactive.Value;
            }
            if (IsContainer(type)) return HackTime.TileScanSealed.Value;
            if (IsCraftingStation(type)) return HackTime.TileScanOnline.Value;
            return HackTime.TileScanIntact.Value;
        }

        /// <summary>
        /// 获取状态对应的颜色
        /// </summary>
        private static Color GetStatusColor(Tile tile, int type) {
            if (IsLightSource(type)) {
                bool isOn = tile.TileFrameX < 66 || Main.tileFrameImportant[type] && tile.TileFrameX == 0;
                return isOn ? HackTheme.Accent : HackTheme.Danger;
            }
            if (IsContainer(type)) return HackTheme.AccentAlt;
            if (IsCraftingStation(type)) return HackTheme.Accent;
            return HackTheme.TextBright;
        }

        #region 物块类型判定

        private static bool IsCraftingStation(int type) {
            return type == TileID.WorkBenches || type == TileID.Furnaces
                || type == TileID.Anvils || type == TileID.MythrilAnvil
                || type == TileID.AdamantiteForge || type == TileID.Hellforge
                || type == TileID.Bottles || type == TileID.AlchemyTable
                || type == TileID.TinkerersWorkbench || type == TileID.Loom
                || type == TileID.Kegs || type == TileID.CookingPots
                || type == TileID.Sawmill || type == TileID.HeavyWorkBench
                || type == TileID.DemonAltar || type == TileID.ImbuingStation
                || type == TileID.Solidifier || type == TileID.Blendomatic
                || type == TileID.MeatGrinder || type == TileID.Extractinator
                || type == TileID.LunarCraftingStation
                || type == TileID.LihzahrdAltar || type == TileID.DyeVat
                || type == TileID.GlassKiln || type == TileID.BoneWelder
                || type == TileID.SteampunkBoiler
                || type == TileID.HoneyDispenser || type == TileID.IceMachine
                || type == TileID.LivingLoom || type == TileID.SkyMill
                || type == TileID.Autohammer || type == TileID.CrystalBall;
        }

        private static bool IsContainer(int type) {
            return type == TileID.Containers || type == TileID.Containers2
                || type == TileID.FakeContainers || type == TileID.FakeContainers2
                || type == TileID.Dressers || type == TileID.Pigronata
                || type == TileID.Mannequin || type == TileID.Womannequin
                || type == TileID.DisplayDoll || type == TileID.HatRack;
        }

        private static bool IsLightSource(int type) {
            return type == TileID.Torches || type == TileID.Candles
                || type == TileID.Chandeliers || type == TileID.HangingLanterns
                || type == TileID.Lamps || type == TileID.Candelabras
                || type == TileID.Campfire || type == TileID.FireflyinaBottle
                || type == TileID.LightningBuginaBottle
                || type == TileID.ChineseLanterns
                || type == TileID.DiscoBall || type == TileID.WaterCandle
                || type == TileID.PeaceCandle;
        }

        private static bool IsFurniture(int type) {
            return Main.tileFrameImportant[type]
                && !IsCraftingStation(type) && !IsContainer(type) && !IsLightSource(type);
        }

        /// <summary>
        /// 获取破坏物块所需的最低镐力
        /// </summary>
        private static int GetMinPickPower(int type) {
            //利用原版的minPick检测数组
            if (type == TileID.Meteorite) return 50;
            if (type == TileID.Demonite || type == TileID.Crimtane) return 55;
            if (type == TileID.Ebonstone || type == TileID.Crimstone
                || type == TileID.Pearlstone || type == TileID.Hellstone) return 65;
            if (type == TileID.Cobalt || type == TileID.Palladium) return 100;
            if (type == TileID.Mythril || type == TileID.Orichalcum) return 110;
            if (type == TileID.Adamantite || type == TileID.Titanium) return 150;
            if (type == TileID.Chlorophyte) return 200;
            if (type == TileID.LihzahrdBrick) return 210;
            return 0;
        }

        #endregion

        /// <summary>
        /// 判断指定世界坐标处是否存在可扫描的物块
        /// <br/>只扫描frameImportant的物块（家具、工作站等）或特殊方块
        /// </summary>
        public static bool TryGetScannableTile(Vector2 worldPos, out int outX, out int outY) {
            outX = (int)(worldPos.X / 16f);
            outY = (int)(worldPos.Y / 16f);

            if (outX < 0 || outX >= Main.maxTilesX || outY < 0 || outY >= Main.maxTilesY) {
                return false;
            }

            Tile tile = Main.tile[outX, outY];
            if (!tile.HasTile) return false;

            //frameImportant通常是家具、工作站等可交互物块
            if (Main.tileFrameImportant[tile.TileType]) return true;

            //也允许扫描一些特殊方块（矿石、地牢砖等）
            if (Main.tileDungeon[tile.TileType]) return true;
            if (tile.TileType == TileID.LihzahrdBrick) return true;

            return false;
        }
    }
}
