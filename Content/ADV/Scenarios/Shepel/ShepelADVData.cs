namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel
{
    /// <summary>
    /// Shepel对话线的持久化存档模块，由ADVSave自动发现，无需手动注册
    /// </summary>
    internal class ShepelADVData : ADVDataModule
    {
        //空闲对话变体的轮换种子，每次触发后递增
        public int IdleVariantSeed;
        //故事阶段，框架预留，当前不主动推进（0=初遇前 1=初遇后 以此类推）
        public int StoryPhase;
        //待播响应式事件的bit位集合，各位含义见ShepelReactiveEvent枚举
        public int ReactiveEventFlags;
        //上一次触发BossDefeated事件的NPC类型ID，-1表示尚未记录
        public int LastDefeatedBossNpcType = -1;
        //是否已触发首次获得SHPC场景
        public bool FirstSHPCObtained;
    }
}
