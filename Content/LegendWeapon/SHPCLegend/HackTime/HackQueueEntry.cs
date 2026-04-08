namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.HackTime
{
    //队列条目状态
    internal enum HackQueueState
    {
        //等待上传
        Waiting,
        //正在上传
        Uploading,
        //上传完成（短暂闪烁后移除）
        Completed,
    }

    //用于在右侧面板查询某个hack在队列中的状态
    internal enum QueueSlotState
    {
        None,
        Queued,
        Uploading,
        Completed,
    }

    //左侧待执行队列的单条记录
    internal class HackQueueEntry
    {
        //对应的协议定义
        public QuickHackDef Hack;
        //在QuickHackRegistry.All中的索引
        public int SlotIndex;
        //当前队列状态
        public HackQueueState State;
        //上传进度0~1
        public float UploadProgress;
        //飞入动画进度0~1
        public float FlyIn;
        //完成后闪烁计时
        public float CompletedTimer;
        //故障种子
        public float GlitchSeed;

        public HackQueueEntry(QuickHackDef hack, int slotIndex) {
            Hack = hack;
            SlotIndex = slotIndex;
            State = HackQueueState.Waiting;
            UploadProgress = 0f;
            FlyIn = 0f;
            CompletedTimer = 0f;
            GlitchSeed = Terraria.Main.rand?.Next(10000) / 100f ?? 0f;
        }
    }
}
