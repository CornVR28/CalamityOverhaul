namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.CybCourses
{
    //超梦教程关卡的存档状态，由ADVSave自动发现
    internal class CybCourseTutorialData : ADVDataModule
    {
        //Shepel开场介绍是否已播放
        public bool IntroPlayed;
        //SHPC HUD教学当前步骤索引，-1表示已完成全部步骤
        public int SHPCTutorialStep;
    }
}
