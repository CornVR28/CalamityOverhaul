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

        //贴图ceil尺寸参考（当前建筑贴图已缩至最初版本的一半）：
        //CoreVoidLab 580x378 -> 37x24
        //EnergyControlStation 240x136 -> 15x9
        //MidSizeMaterialAnalysisLab 382x184 -> 24x12
        //ObservationPostTelescope 130x154 -> 9x10
        //设计原则：只为建筑提供落脚用的地板/甲板平台，绝不覆盖装饰物（储罐、草木、管道）
        //墙体碰撞会让这些氛围型装饰变成真正的实心，反而妨碍玩家走动与战斗
        private static readonly Dictionary<ArchitectureType, Spec> _specs = new() {
            //观测塔：只在塔楼甲板上铺一层平台，玩家可踩上望远镜基座
            //入口区不加碰撞，方便从正面自由穿行
            [ArchitectureType.ObservationPostTelescope] = new Spec(
                cols: 9, rows: 10,
                solids: [],
                platforms: [new PlatformRow(3, 1, 6)]
            ),
            //能源控制站：只在底部金属地台表面铺一条地板平台
            //罐体、管道、控制台全部保持装饰性，不做实心
            [ArchitectureType.EnergyControlStation] = new Spec(
                cols: 15, rows: 9,
                solids: [],
                platforms: [new PlatformRow(7, 0, 14)]
            ),
            //中型分析实验室：只在底部金属基座表面铺地板
            //右下角的草丛区域故意留出来
            [ArchitectureType.MidSizeMaterialAnalysisLab] = new Spec(
                cols: 24, rows: 12,
                solids: [],
                platforms: [new PlatformRow(10, 0, 21)]
            ),
            //核心实验室：主屋楼板 + 两侧桁架露台 + 底部工作甲板，都用平台
            //玩家可自由在各层之间跳跃穿行，符合多层结构观感
            [ArchitectureType.CoreVoidLab] = new Spec(
                cols: 37, rows: 24,
                solids: [],
                platforms: [
                    //主屋内部楼板
                    new PlatformRow(12, 6, 30),
                    //左右两侧伸出的桁架露台
                    new PlatformRow(17, 1, 10),
                    new PlatformRow(17, 26, 35),
                    //底部工作甲板
                    new PlatformRow(21, 11, 25),
                ]
            ),
            //信号塔 218x516 -> 14x33：塔身高耸装饰，只在塔基处铺一条平台让玩家能站到塔脚下
            //塔顶天线/碟盘等纯装饰，不加碰撞免得玩家被卡在天线丛里
            [ArchitectureType.SignalTower] = new Spec(
                cols: 14, rows: 33,
                solids: [],
                platforms: [new PlatformRow(31, 0, 13)]
            ),
            //X桁架斜桥 266x178 -> 17x12：踏面位于对角线顶面
            //步进斜坡由<see cref="BuildStaircaseSpec"/>生成，左下→右上
            [ArchitectureType.ConnectionBridgeSlope] = BuildStaircaseSpec(cols: 17, rows: 12,
                startCol: 0, endCol: 16, startRow: 10, endRow: 2),

            //铁锈加固阶梯 362x208 -> 23x13：完整阶梯走廊，踏面覆盖整条对角线
            [ArchitectureType.ReinforcedRustedPathway] = BuildStaircaseSpec(cols: 23, rows: 13,
                startCol: 0, endCol: 22, startRow: 11, endRow: 3),
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

        /// <summary>
        /// 为斜坡类建筑生成按列阶梯分布的平台Spec
        /// 每一列都会放一格平台，其Y在<paramref name="startRow"/>到<paramref name="endRow"/>之间线性插值
        /// 相邻两列同Y时合并成宽平台行，减少<see cref="PlatformRow"/>数量
        /// </summary>
        private static Spec BuildStaircaseSpec(int cols, int rows, int startCol, int endCol, int startRow, int endRow) {
            List<PlatformRow> rowList = [];
            int spanCols = endCol - startCol;
            int spanRows = endRow - startRow;
            int currentRow = -1;
            int currentStartCol = 0;
            for (int c = startCol; c <= endCol; c++) {
                float t = spanCols == 0 ? 0 : (c - startCol) / (float)spanCols;
                int r = (int)System.Math.Round(startRow + spanRows * t);
                r = Clamp(r, 0, rows - 1);
                if (r != currentRow) {
                    //前一段结束，落笔
                    if (currentRow >= 0) {
                        rowList.Add(new PlatformRow(currentRow, currentStartCol, c - 1));
                    }
                    currentRow = r;
                    currentStartCol = c;
                }
            }
            if (currentRow >= 0) {
                rowList.Add(new PlatformRow(currentRow, currentStartCol, endCol));
            }
            return new Spec(cols, rows, solids: [], platforms: rowList.ToArray());
        }
    }
}
