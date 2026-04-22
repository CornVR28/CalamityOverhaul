namespace CalamityOverhaul.Content.HackTimes
{
    using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.GlitchWraith;

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
        //在QuickHackDef.Instances中的索引
        public int SlotIndex;
        //目标类型
        public HackTargetKind TargetKind;
        //骇入目标NPC索引（TargetKind为Npc时有效），入队时锁定
        public int TargetIndex;
        //骇入目标物块坐标（TargetKind为Tile时有效）
        public int TileX;
        public int TileY;
        //骇入目标灵异Actor引用（TargetKind为Wraith时有效）
        public GlitchWraithActor WraithTarget;
        //骇入目标炮台Actor引用（TargetKind为Turret时有效）
        public IHackableTurret TurretTarget;
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

        //NPC目标构造
        public HackQueueEntry(QuickHackDef hack, int slotIndex, int targetIndex) {
            Hack = hack;
            SlotIndex = slotIndex;
            TargetKind = HackTargetKind.Npc;
            TargetIndex = targetIndex;
            TileX = -1;
            TileY = -1;
            State = HackQueueState.Waiting;
            UploadProgress = 0f;
            FlyIn = 0f;
            CompletedTimer = 0f;
            GlitchSeed = Terraria.Main.rand?.Next(10000) / 100f ?? 0f;
        }

        //物块目标构造
        public HackQueueEntry(QuickHackDef hack, int slotIndex, int tileX, int tileY) {
            Hack = hack;
            SlotIndex = slotIndex;
            TargetKind = HackTargetKind.Tile;
            TargetIndex = -1;
            TileX = tileX;
            TileY = tileY;
            State = HackQueueState.Waiting;
            UploadProgress = 0f;
            FlyIn = 0f;
            CompletedTimer = 0f;
            GlitchSeed = Terraria.Main.rand?.Next(10000) / 100f ?? 0f;
        }

        //灵异目标构造
        public HackQueueEntry(QuickHackDef hack, int slotIndex, GlitchWraithActor wraith) {
            Hack = hack;
            SlotIndex = slotIndex;
            TargetKind = HackTargetKind.Wraith;
            TargetIndex = -1;
            TileX = -1;
            TileY = -1;
            WraithTarget = wraith;
            State = HackQueueState.Waiting;
            UploadProgress = 0f;
            FlyIn = 0f;
            CompletedTimer = 0f;
            GlitchSeed = Terraria.Main.rand?.Next(10000) / 100f ?? 0f;
        }

        //炮台目标构造
        public HackQueueEntry(QuickHackDef hack, int slotIndex, IHackableTurret turret) {
            Hack = hack;
            SlotIndex = slotIndex;
            TargetKind = HackTargetKind.Turret;
            TargetIndex = -1;
            TileX = -1;
            TileY = -1;
            TurretTarget = turret;
            State = HackQueueState.Waiting;
            UploadProgress = 0f;
            FlyIn = 0f;
            CompletedTimer = 0f;
            GlitchSeed = Terraria.Main.rand?.Next(10000) / 100f ?? 0f;
        }

        //目标是否仍然有效
        public bool IsTargetValid {
            get {
                if (TargetKind == HackTargetKind.Npc) {
                    return TargetIndex >= 0 && TargetIndex < Terraria.Main.maxNPCs
                        && Terraria.Main.npc[TargetIndex].active;
                }
                if (TargetKind == HackTargetKind.Tile) {
                    return TileX >= 0 && TileX < Terraria.Main.maxTilesX
                        && TileY >= 0 && TileY < Terraria.Main.maxTilesY
                        && Terraria.Main.tile[TileX, TileY].HasTile;
                }
                if (TargetKind == HackTargetKind.Wraith) {
                    return WraithTarget != null && WraithTarget.Active;
                }
                if (TargetKind == HackTargetKind.Turret) {
                    return TurretTarget != null && TurretTarget.AsActor != null
                        && TurretTarget.AsActor.Active && TurretTarget.IsValid;
                }
                return false;
            }
        }
    }
}
