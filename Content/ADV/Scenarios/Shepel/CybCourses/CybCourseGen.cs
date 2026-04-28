using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.CybCourses
{
    //超梦教程关卡的世界地形生成
    //布局：一条主通道从左延伸到右，中间有若干高低平台和隔断墙，整体走向横向
    internal class CybCourseGen : GenPass
    {
        //主通道地板Y坐标（从顶部算）
        private const int FloorY = 170;
        //通道净高（空气格数）
        private const int RoomHeight = 20;
        //地面厚度
        private const int FloorThick = 8;
        //墙面厚度（左右边界）
        private const int WallThick = 6;

        public CybCourseGen() : base("Cyb Course Generation", 1f) { }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration) {
            progress.Message = "构建超梦空间...";

            int width = Main.maxTilesX;
            int height = Main.maxTilesY;

            //第一步：用虚空填充整个世界（所有格子置为空气+深渊墙）
            FillVoid(width, height);

            //第二步：铺设封闭边界（上下左右厚板）
            FillBorders(width, height);

            //第三步：生成主通道地板和顶板
            BuildMainCorridor(width);

            //第四步：在主通道内放置若干高低平台
            PlacePlatforms(width);
        }

        //用黑暗虚空填充整个世界，背景墙使用深黑色砖墙
        private static void FillVoid(int width, int height) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    Tile tile = Main.tile[x, y];
                    tile.HasTile = false;
                    tile.WallType = WallID.EbonstoneUnsafe;
                }
            }
        }

        //填充外边界（上下左右），使用灰砖构成硬边框
        private static void FillBorders(int width, int height) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < WallThick; y++) {
                    PlaceSolid(x, y, TileID.GrayBrick);
                }
                for (int y = height - WallThick; y < height; y++) {
                    PlaceSolid(x, y, TileID.GrayBrick);
                }
            }
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < WallThick; x++) {
                    PlaceSolid(x, y, TileID.GrayBrick);
                }
                for (int x = width - WallThick; x < width; x++) {
                    PlaceSolid(x, y, TileID.GrayBrick);
                }
            }
        }

        //生成主通道：地板+顶板+背景墙换成铁砖墙以区分内外
        private static void BuildMainCorridor(int width) {
            int ceilY = FloorY - RoomHeight;

            for (int x = WallThick; x < width - WallThick; x++) {
                //地板
                for (int y = FloorY; y < FloorY + FloorThick; y++) {
                    PlaceSolid(x, y, TileID.GrayBrick);
                }
                //顶板（薄一些，2格）
                for (int y = ceilY - 2; y < ceilY; y++) {
                    PlaceSolid(x, y, TileID.GrayBrick);
                }
                //通道内部换为铁砖背景墙，视觉上更有金属感
                for (int y = ceilY; y < FloorY; y++) {
                    Main.tile[x, y].WallType = WallID.IronBrick;
                }
            }
        }

        //在主通道中铺设若干平台，制造高低起伏的教程路线
        //平台使用铁砖方块，高度交错，宽度固定为20格，间距20格
        private static void PlacePlatforms(int width) {
            //平台参数：[起始X偏移, 高于地板的格数, 宽度]
            (int offsetX, int riseY, int w)[] platformDefs = [
                (60,  6,  24),
                (110, 10, 20),
                (155, 6,  20),
                (200, 12, 22),
                (250, 6,  20),
                (300, 10, 18),
            ];

            foreach (var (offsetX, riseY, w) in platformDefs) {
                int platY = FloorY - riseY;
                for (int x = offsetX; x < offsetX + w && x < width - WallThick; x++) {
                    //平台主体（2格厚）
                    PlaceSolid(x, platY, TileID.IronBrick);
                    PlaceSolid(x, platY + 1, TileID.IronBrick);
                }
            }
        }

        private static void PlaceSolid(int x, int y, ushort tileType) {
            if (!WorldGen.InWorld(x, y)) {
                return;
            }
            Tile tile = Main.tile[x, y];
            tile.HasTile = true;
            tile.TileType = tileType;
        }
    }
}
