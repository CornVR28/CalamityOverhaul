using Terraria;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CalamityOverhaul.Content.HackTimes
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

        //公开坐标供协议面板使用
        public int TileCoordX => tileX;
        public int TileCoordY => tileY;

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

        public bool IsHackable => true;

        public int ScanRowCount => 5;

        public void BuildScanData(string[] labels, string[] values, Color[] colors) {
            if (!IsValid) return;
            Tile tile = Main.tile[tileX, tileY];
            int type = tile.TileType;

            //NAME
            labels[0] = HackTime.TileScanName.Value;
            values[0] = GetTileName(tileX, tileY, type);
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
        /// <br/>通过MapHelper将物块坐标转换为地图条目，再取本地化名称
        /// </summary>
        internal static string GetTileName(int x, int y, int type) {
            //MapHelper.CreateMapTile内部处理了帧到样式的映射
            //返回的MapTile.Type就是Lang.GetMapObjectName所需的查表索引
            MapTile mapTile = MapHelper.CreateMapTile(x, y, 255);
            if (mapTile.Type > 0) {
                string mapName = Lang.GetMapObjectName(mapTile.Type);
                //若名称非空且不是纯数字则直接使用
                if (!string.IsNullOrEmpty(mapName) && !IsNumericOnly(mapName)) return mapName;
            }

            //Mod物块回退到ModTile名称
            if (type >= TileID.Count) {
                ModTile modTile = TileLoader.GetTile(type);
                if (modTile != null) return modTile.Name;
            }

            //无法解析出正常名称时，通过掉落物获取物品名作为兜底
            Tile tile = Main.tile[x, y];
            int dropId = tile.GetTileDrop(x, y);
            if (dropId > 0) {
                string itemName = VaultUtils.GetLocalizedItemName(dropId).Value;
                if (!string.IsNullOrEmpty(itemName)) return itemName;
            }

            return $"TILE#{type:X3}";
        }

        private static bool IsNumericOnly(string s) {
            foreach (char c in s) {
                if (c < '0' || c > '9') return false;
            }
            return s.Length > 0;
        }

        /// <summary>
        /// 获取物块的分类文本
        /// </summary>
        internal static string GetTileClass(int type) {
            if (IsCraftingStation(type)) return HackTime.TileScanCrafting.Value;
            if (IsContainer(type)) return HackTime.TileScanContainer.Value;
            if (IsLightSource(type)) return HackTime.TileScanLight.Value;
            if (IsFurniture(type)) return HackTime.TileScanFurniture.Value;
            return HackTime.TileScanBlock.Value;
        }

        /// <summary>
        /// 获取物块分类对应的颜色
        /// </summary>
        internal static Color GetTileClassColor(int type) {
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
        /// <br/>任何存在实体物块的格子均可扫描
        /// </summary>
        public static bool TryGetScannableTile(Vector2 worldPos, out int outX, out int outY) {
            outX = (int)(worldPos.X / 16f);
            outY = (int)(worldPos.Y / 16f);

            if (outX < 0 || outX >= Main.maxTilesX || outY < 0 || outY >= Main.maxTilesY) {
                return false;
            }

            Tile tile = Main.tile[outX, outY];
            return tile.HasTile;
        }

        /// <summary>
        /// 判断是否为树木 trunk 物块
        /// <br/>树木由单列 trunk tile 组成，树冠和分枝由 TileDrawing 独立绘制
        /// </summary>
        public static bool IsTreeTile(int type) {
            return type == TileID.Trees
                || type == TileID.PalmTree
                || type == TileID.VanityTreeSakura
                || type == TileID.VanityTreeYellowWillow
                || type == TileID.TreeAsh
                || type == TileID.MushroomTrees;
        }

        /// <summary>
        /// 获取整棵树木的视觉包围盒
        /// <br/>沿 trunk 向上/向下搜索完整 trunk 范围，再扩展树冠高度和分枝宽度
        /// </summary>
        public static Rectangle GetTreeFullBounds(int x, int y, int type) {
            //向上找 trunk 顶（连续同类 HasTile）
            int topY = y;
            while (topY - 1 >= 0) {
                Tile t = Main.tile[x, topY - 1];
                if (!t.HasTile || t.TileType != type) break;
                topY--;
            }
            //向下找 trunk 底
            int botY = y;
            while (botY + 1 < Main.maxTilesY) {
                Tile t = Main.tile[x, botY + 1];
                if (!t.HasTile || t.TileType != type) break;
                botY++;
            }
            //树冠向上扩展 5 格约 80px，分枝左右扩展 ~2.5 格约 40px
            const int canopyUp = 80;
            const int branchSide = 40;
            int px = x * 16 - branchSide;
            int py = topY * 16 - canopyUp;
            int w = 16 + branchSide * 2;
            int h = (botY - topY + 1) * 16 + canopyUp;
            return new Rectangle(px, py, w, h);
        }

        /// <summary>
        /// 获取物块对象在世界空间中的包围盒（像素坐标）
        /// <br/>对于多格物块会尝试找到其左上角并计算完整尺寸
        /// <br/>对于树木返回整棵树的视觉范围
        /// </summary>
        public static Rectangle GetTileWorldBounds(int x, int y) {
            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
                return new Rectangle(x * 16, y * 16, 16, 16);

            Tile tile = Main.tile[x, y];
            if (!tile.HasTile) return new Rectangle(x * 16, y * 16, 16, 16);

            int type = tile.TileType;
            //树木特殊处理
            if (IsTreeTile(type)) {
                return GetTreeFullBounds(x, y, type);
            }

            TileObjectData data = TileObjectData.GetTileData(type, 0);
            if (data == null) return new Rectangle(x * 16, y * 16, 16, 16);

            //通过帧坐标反推当前格子在多格物块中的偏移
            int frameWidth = data.CoordinateWidth + data.CoordinatePadding;
            int frameHeight = data.CoordinateHeights[0] + data.CoordinatePadding;
            int offsetX = tile.TileFrameX % (data.Width * frameWidth) / frameWidth;
            int offsetY = tile.TileFrameY % (data.Height * frameHeight) / frameHeight;

            //左上角格子坐标
            int topLeftX = x - offsetX;
            int topLeftY = y - offsetY;

            return new Rectangle(
                topLeftX * 16,
                topLeftY * 16,
                data.Width * 16,
                data.Height * 16);
        }
    }
}
