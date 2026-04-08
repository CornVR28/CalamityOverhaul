namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime
{
    /// <summary>
    /// 骇客时间RAM资源管理器
    /// <br/>管理玩家的RAM池：总量、当前值、消耗与自动恢复
    /// <br/>类似赛博朋克2077的RAM机制，每个骇入协议消耗固定RAM，RAM随时间缓慢恢复
    /// </summary>
    internal class HackTimeRAM : ICWRLoader
    {
        void ICWRLoader.UnLoadData() => Reset();

        /// <summary>
        /// RAM最大容量（单元数）
        /// </summary>
        public static int MaxRam { get; set; } = DefaultMaxRam;
        /// <summary>
        /// 当前可用RAM（精确浮点值，显示时取整）
        /// </summary>
        public static float CurrentRam { get; set; }
        /// <summary>
        /// 每秒恢复的RAM量
        /// </summary>
        public static float RecoveryRate { get; set; } = DefaultRecoveryRate;

        //默认最大RAM
        private const int DefaultMaxRam = 8;
        //默认每秒恢复量（约5秒恢复1格）
        private const float DefaultRecoveryRate = 0.2f;
        //恢复延迟：消耗RAM后过多少秒才开始恢复
        private const float RecoveryDelay = 1.5f;
        //上次消耗RAM后的冷却计时
        private static float recoveryCooldown;

        /// <summary>
        /// 当前可用RAM的整数显示值
        /// </summary>
        public static int DisplayCurrent => (int)CurrentRam;

        /// <summary>
        /// 当前RAM占最大值的比例(0~1)
        /// </summary>
        public static float Ratio => MaxRam > 0 ? CurrentRam / MaxRam : 0f;

        /// <summary>
        /// 检查是否有足够RAM执行指定消耗
        /// </summary>
        public static bool CanAfford(int cost) {
            if (HackTime.InfiniteHack) return true;
            return CurrentRam >= cost;
        }

        /// <summary>
        /// 消耗RAM，返回是否成功
        /// </summary>
        public static bool TryConsume(int cost) {
            if (HackTime.InfiniteHack) return true;
            if (CurrentRam < cost) return false;
            CurrentRam -= cost;
            if (CurrentRam < 0f) CurrentRam = 0f;
            recoveryCooldown = RecoveryDelay;
            return true;
        }

        /// <summary>
        /// 每帧更新，处理RAM自动恢复
        /// </summary>
        //tModLoader固定每秒60tick
        private const float TickSeconds = 1f / 60f;

        public static void Update() {
            //恢复冷却
            if (recoveryCooldown > 0f) {
                recoveryCooldown -= TickSeconds;
                return;
            }

            //缓慢恢复
            if (CurrentRam < MaxRam) {
                CurrentRam += RecoveryRate * TickSeconds;
                if (CurrentRam > MaxRam) CurrentRam = MaxRam;
            }
        }

        /// <summary>
        /// 将RAM充满（进入骇客时间或重置时调用）
        /// </summary>
        public static void Refill() {
            CurrentRam = MaxRam;
            recoveryCooldown = 0f;
        }

        /// <summary>
        /// 重置所有RAM状态到默认值
        /// </summary>
        public static void Reset() {
            MaxRam = DefaultMaxRam;
            CurrentRam = DefaultMaxRam;
            RecoveryRate = DefaultRecoveryRate;
            recoveryCooldown = 0f;
        }
    }
}
