using CalamityOverhaul.Common;
using CalamityOverhaul.Content.HackTimes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.SignalTowers.Decryption
{
    /// <summary>
    /// Breach Protocol风格破译小游戏
    /// 玩家需要在字节矩阵中按行列交替的选择规则，把缓冲区填入包含目标序列的连续子串
    /// 失败惩罚为冷却秒数，玩家可以无限重试但不能即刻重开
    /// </summary>
    internal class BreachMinigame
    {
        public const int MatrixSize = 6;
        public const int BufferCapacity = 7;
        public const float AttemptTimeSeconds = 30f;
        public const float FailCooldownSeconds = 4f;

        //字节池：保留2077风格的16进制码元
        private static readonly byte[] Pool = [0x1C, 0x55, 0xBD, 0xE9, 0x7A, 0xFF];

        public byte[,] Matrix { get; private set; } = new byte[MatrixSize, MatrixSize];
        public List<byte> Buffer { get; } = [];
        public List<Target> Targets { get; } = [];

        /// <summary>当前选择受限轴：0=行约束 1=列约束</summary>
        public int AxisLock { get; private set; }
        /// <summary>当前选择受限的行/列索引</summary>
        public int AxisIndex { get; private set; }
        /// <summary>已选中的矩阵单元（避免重复）</summary>
        public bool[,] Taken { get; private set; } = new bool[MatrixSize, MatrixSize];

        public float TimeLeft { get; private set; }
        public float Cooldown { get; private set; }
        public int AttemptCount { get; private set; }
        public bool HasSolved { get; private set; }
        public bool HasFailedAttempt { get; private set; }

        public (int row, int col)? HoverCell { get; private set; }

        /// <summary>新一轮破译：重新生成矩阵与目标</summary>
        public void NewPuzzle() {
            AttemptCount = 0;
            Cooldown = 0f;
            HasSolved = false;
            HasFailedAttempt = false;
            GenerateMatrix();
            GenerateTargets();
            ResetAttempt();
        }

        /// <summary>完全清空，用于会话关闭</summary>
        public void Clear() {
            AttemptCount = 0;
            Cooldown = 0f;
            HasSolved = false;
            HasFailedAttempt = false;
            Buffer.Clear();
            Targets.Clear();
            HoverCell = null;
            for (int r = 0; r < MatrixSize; r++) for (int c = 0; c < MatrixSize; c++) Taken[r, c] = false;
        }

        /// <summary>失败或手动重置后的新一次尝试，不重新生成矩阵</summary>
        public void ResetAttempt() {
            Buffer.Clear();
            for (int r = 0; r < MatrixSize; r++) for (int c = 0; c < MatrixSize; c++) Taken[r, c] = false;
            foreach (var t in Targets) t.Matched = false;
            AxisLock = 0;
            AxisIndex = 0;
            TimeLeft = AttemptTimeSeconds;
            HasFailedAttempt = false;
            AttemptCount++;
        }

        public void Update(float dt) {
            if (HasSolved) return;
            if (Cooldown > 0f) {
                Cooldown -= dt;
                if (Cooldown <= 0f) {
                    Cooldown = 0f;
                    ResetAttempt();
                }
                return;
            }
            TimeLeft -= dt;
            if (TimeLeft <= 0f) {
                TimeLeft = 0f;
                TriggerFail();
            }
        }

        public bool CanSelect(int r, int c) {
            if (Cooldown > 0f || HasSolved) return false;
            if (r < 0 || r >= MatrixSize || c < 0 || c >= MatrixSize) return false;
            if (Taken[r, c]) return false;
            if (Buffer.Count >= BufferCapacity) return false;
            return AxisLock == 0 ? r == AxisIndex : c == AxisIndex;
        }

        public bool TrySelect(int r, int c) {
            if (!CanSelect(r, c)) return false;
            Buffer.Add(Matrix[r, c]);
            Taken[r, c] = true;
            //轴翻转：行约束→下次只能在此列选择；反之亦然
            if (AxisLock == 0) { AxisLock = 1; AxisIndex = c; }
            else { AxisLock = 0; AxisIndex = r; }

            CheckTargetMatches();
            if (AllTargetsMatched()) {
                HasSolved = true;
                return true;
            }
            if (Buffer.Count >= BufferCapacity) {
                //缓冲区满仍未全部匹配即失败
                bool anyPending = false;
                foreach (var t in Targets) if (!t.Matched) { anyPending = true; break; }
                if (anyPending) TriggerFail();
            }
            return true;
        }

        /// <summary>手动取消所有已选：与时间强耦合的重试键</summary>
        public void TriggerFail() {
            HasFailedAttempt = true;
            Cooldown = FailCooldownSeconds;
        }

        private void CheckTargetMatches() {
            foreach (var t in Targets) {
                if (t.Matched) continue;
                t.Matched = ContainsSubsequence(Buffer, t.Sequence);
            }
        }

        private bool AllTargetsMatched() {
            foreach (var t in Targets) if (!t.Matched) return false;
            return Targets.Count > 0;
        }

        private static bool ContainsSubsequence(List<byte> buf, byte[] seq) {
            if (seq.Length == 0) return true;
            if (buf.Count < seq.Length) return false;
            for (int i = 0; i <= buf.Count - seq.Length; i++) {
                bool ok = true;
                for (int j = 0; j < seq.Length; j++) {
                    if (buf[i + j] != seq[j]) { ok = false; break; }
                }
                if (ok) return true;
            }
            return false;
        }

        //═══════════════════════════════════════════════════════════
        // 生成
        //═══════════════════════════════════════════════════════════
        private void GenerateMatrix() {
            var rng = new FastRandom(unchecked((long)((ulong)Main.GameUpdateCount * 9301 + 49297)));
            for (int r = 0; r < MatrixSize; r++) {
                for (int c = 0; c < MatrixSize; c++) {
                    Matrix[r, c] = Pool[rng.Next(Pool.Length)];
                }
            }
        }

        private void GenerateTargets() {
            Targets.Clear();
            var rng = new FastRandom(unchecked((long)((ulong)Main.GameUpdateCount * 48271 + 11)));
            //三条目标：2 / 3 / 4 长度，从矩阵沿合法路径采样以保证理论可解
            int[] lens = [2, 3, 4];
            string[] names = ["DATAMINE_V1", "DATAMINE_V2", "DATAMINE_V3"];
            Color[] cols = [new(160, 240, 255), new(255, 200, 120), new(200, 255, 160)];
            for (int i = 0; i < 3; i++) {
                byte[] seq = SamplePath(lens[i], ref rng);
                Targets.Add(new Target { Sequence = seq, Name = names[i], TintColor = cols[i], Matched = false });
            }
        }

        /// <summary>沿合法交替规则从矩阵采样一条长度为len的路径作为候选目标</summary>
        private byte[] SamplePath(int len, ref FastRandom rng) {
            int axis = 0;
            int idx = rng.Next(MatrixSize);
            bool[,] used = new bool[MatrixSize, MatrixSize];
            byte[] result = new byte[len];
            for (int k = 0; k < len; k++) {
                //在当前轴上找所有未使用的候选
                int pick = -1;
                int start = rng.Next(MatrixSize);
                for (int o = 0; o < MatrixSize; o++) {
                    int cand = (start + o) % MatrixSize;
                    int r = axis == 0 ? idx : cand;
                    int c = axis == 0 ? cand : idx;
                    if (!used[r, c]) { pick = cand; break; }
                }
                if (pick < 0) pick = 0;
                int rr = axis == 0 ? idx : pick;
                int cc = axis == 0 ? pick : idx;
                used[rr, cc] = true;
                result[k] = Matrix[rr, cc];
                //轴翻转
                if (axis == 0) { axis = 1; idx = cc; }
                else { axis = 0; idx = rr; }
            }
            return result;
        }

        /// <summary>处理鼠标悬停和左键点击（在给定矩阵绘制矩形里）</summary>
        public void HandleMatrixInput(Rectangle cellArea, int cellSize, int gap) {
            HoverCell = null;
            int mx = Main.mouseX;
            int my = Main.mouseY;
            for (int r = 0; r < MatrixSize; r++) {
                for (int c = 0; c < MatrixSize; c++) {
                    int x = cellArea.X + c * (cellSize + gap);
                    int y = cellArea.Y + r * (cellSize + gap);
                    var rc = new Rectangle(x, y, cellSize, cellSize);
                    if (rc.Contains(mx, my)) {
                        HoverCell = (r, c);
                        if (Main.mouseLeft && Main.mouseLeftRelease) {
                            if (TrySelect(r, c)) {
                                SoundEngine.PlaySound(SoundID.MenuTick);
                                Main.mouseLeftRelease = false;
                            }
                            else {
                                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.3f, Pitch = -0.4f });
                                Main.mouseLeftRelease = false;
                            }
                        }
                        return;
                    }
                }
            }
        }

        public class Target
        {
            public byte[] Sequence;
            public string Name;
            public Color TintColor;
            public bool Matched;
        }

        /// <summary>
        /// 本地化字符串——供UI显示
        /// </summary>
        public static LocalizedText LabelBuffer { get; private set; }
        public static LocalizedText LabelTargets { get; private set; }
        public static LocalizedText LabelTimer { get; private set; }
        public static LocalizedText LabelAxisRow { get; private set; }
        public static LocalizedText LabelAxisCol { get; private set; }
        public static LocalizedText LabelCooldown { get; private set; }
        public static LocalizedText LabelAttempt { get; private set; }
        public static LocalizedText LabelRestart { get; private set; }

        internal static void RegisterLocalization(ILocalizedModType host) {
            LabelBuffer = host.GetLocalization("BreachBuffer", () => "缓冲区 {0}/{1}");
            LabelTargets = host.GetLocalization("BreachTargets", () => "目标序列");
            LabelTimer = host.GetLocalization("BreachTimer", () => "剩余时间");
            LabelAxisRow = host.GetLocalization("BreachAxisRow", () => "行约束 ▸ 第{0}行");
            LabelAxisCol = host.GetLocalization("BreachAxisCol", () => "列约束 ▸ 第{0}列");
            LabelCooldown = host.GetLocalization("BreachCooldown", () => "协议重置中 {0:0.0}s");
            LabelAttempt = host.GetLocalization("BreachAttempt", () => "尝试 #{0}");
            LabelRestart = host.GetLocalization("BreachRestart", () => "⟳ 重试");
        }
    }

    /// <summary>
    /// 轻量XorShift随机数，避免对Main.rand的耦合
    /// </summary>
    internal struct FastRandom
    {
        private ulong state;
        public FastRandom(long seed) { state = (ulong)(seed == 0 ? 1 : seed); }
        public int Next(int max) {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            if (max <= 0) return 0;
            return (int)(state % (ulong)max);
        }
    }
}
