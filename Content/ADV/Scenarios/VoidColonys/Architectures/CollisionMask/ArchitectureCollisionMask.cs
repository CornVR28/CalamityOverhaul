using System.Collections.Generic;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.CollisionMask
{
    /// <summary>
    /// 虚空聚落建筑的物块级碰撞蒙版
    /// 以字符网格<c>string[]</c>最终被消费：'#'=实心 '='=平台 '.'=空气
    /// 出于维护性用结构化描述填充出网格，每座建筑只声明实心矩形与可选的镂空/平台行
    /// 建筑贴图并非都能被16整除，这里的行列取<c>ceil(texture/16)</c>
    /// 第一版蒙版按贴图轮廓近似设计，后续可按手感替换为手写字符串阵列覆盖
    /// </summary>
    internal static class ArchitectureCollisionMask
    {
        public const char Empty = '.';
        public const char Solid = '#';
        public const char Platform = '=';

        //矩形块包围区间闭区间，坐标为蒙版网格的tile索引
        private readonly record struct Box(int X1, int Y1, int X2, int Y2);
        //平台行：在[x1,x2]范围（闭区间）内、Y行填入'='
        private readonly record struct PlatformRow(int Y, int X1, int X2);

        /// <summary>
        /// 单个建筑的结构化蒙版描述
        /// 先把所有<see cref="Solids"/>矩形覆盖为实心，再以<see cref="Hollows"/>矩形抠空，最后叠加<see cref="Platforms"/>平台行
        /// </summary>
        private sealed class Spec(int cols, int rows, Box[] solids, Box[] hollows = null, PlatformRow[] platforms = null)
        {
            public int Cols { get; } = cols;
            public int Rows { get; } = rows;
            public Box[] Solids { get; } = solids;
            public Box[] Hollows { get; } = hollows ?? [];
            public PlatformRow[] Platforms { get; } = platforms ?? [];
        }

        //贴图ceil尺寸参考：
        //CoreVoidLab 1162x756 -> 73x48
        //EnergyControlStation 482x272 -> 31x17
        //MidSizeMaterialAnalysisLab 766x368 -> 48x23
        //ObservationPostTelescope 262x310 -> 17x20
        //设计原则：只为建筑提供落脚用的地板/甲板平台，绝不覆盖装饰物（储罐、草木、管道）
        //墙体碰撞会让这些氛围型装饰变成真正的实心，反而妨碍玩家走动与战斗
        private static readonly Dictionary<ArchitectureType, Spec> _specs = new() {
            //观测塔：只在塔楼甲板上铺一层平台，玩家可踩上望远镜基座
            //入口区不加碰撞，方便从正面自由穿行
            [ArchitectureType.ObservationPostTelescope] = new Spec(
                cols: 17, rows: 20,
                solids: [],
                platforms: [new PlatformRow(7, 2, 13)]
            ),
            //能源控制站：只在底部金属地台表面铺一条地板平台
            //罐体、管道、控制台全部保持装饰性，不做实心
            [ArchitectureType.EnergyControlStation] = new Spec(
                cols: 31, rows: 17,
                solids: [],
                platforms: [new PlatformRow(15, 1, 29)]
            ),
            //中型分析实验室：只在底部金属基座表面铺地板
            //右下角的草丛区域故意留出来
            [ArchitectureType.MidSizeMaterialAnalysisLab] = new Spec(
                cols: 48, rows: 23,
                solids: [],
                platforms: [new PlatformRow(20, 1, 43)]
            ),
            //核心实验室：主屋楼板 + 两侧桁架露台 + 底部工作甲板，都用平台
            //玩家可自由在各层之间跳跃穿行，符合多层结构观感
            [ArchitectureType.CoreVoidLab] = new Spec(
                cols: 73, rows: 48,
                solids: [],
                platforms: [
                    //主屋内部楼板
                    new PlatformRow(25, 12, 60),
                    //左右两侧伸出的桁架露台
                    new PlatformRow(34, 2, 20),
                    new PlatformRow(34, 52, 70),
                    //底部工作甲板
                    new PlatformRow(43, 22, 50),
                ]
            ),
        };

        //字符网格缓存，避免每次放置都重新生成
        private static readonly Dictionary<ArchitectureType, string[]> _cache = [];

        /// <summary>
        /// 获取指定建筑的字符网格蒙版，未登记返回null
        /// </summary>
        public static string[] Get(ArchitectureType type) {
            if (_cache.TryGetValue(type, out string[] cached)) return cached;
            if (!_specs.TryGetValue(type, out Spec spec)) return null;
            string[] built = Build(spec);
            _cache[type] = built;
            return built;
        }

        private static string[] Build(Spec spec) {
            char[,] grid = new char[spec.Rows, spec.Cols];
            for (int r = 0; r < spec.Rows; r++) {
                for (int c = 0; c < spec.Cols; c++) {
                    grid[r, c] = Empty;
                }
            }

            foreach (Box b in spec.Solids) {
                Fill(grid, b, Solid, spec);
            }
            foreach (Box b in spec.Hollows) {
                Fill(grid, b, Empty, spec);
            }
            foreach (PlatformRow row in spec.Platforms) {
                if (row.Y < 0 || row.Y >= spec.Rows) continue;
                int x1 = Clamp(row.X1, 0, spec.Cols - 1);
                int x2 = Clamp(row.X2, 0, spec.Cols - 1);
                for (int x = x1; x <= x2; x++) {
                    grid[row.Y, x] = Platform;
                }
            }

            string[] output = new string[spec.Rows];
            char[] rowBuf = new char[spec.Cols];
            for (int r = 0; r < spec.Rows; r++) {
                for (int c = 0; c < spec.Cols; c++) {
                    rowBuf[c] = grid[r, c];
                }
                output[r] = new string(rowBuf);
            }
            return output;
        }

        private static void Fill(char[,] grid, Box b, char ch, Spec spec) {
            int x1 = Clamp(b.X1, 0, spec.Cols - 1);
            int x2 = Clamp(b.X2, 0, spec.Cols - 1);
            int y1 = Clamp(b.Y1, 0, spec.Rows - 1);
            int y2 = Clamp(b.Y2, 0, spec.Rows - 1);
            for (int y = y1; y <= y2; y++) {
                for (int x = x1; x <= x2; x++) {
                    grid[y, x] = ch;
                }
            }
        }

        private static int Clamp(int v, int lo, int hi) {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }
    }
}
