namespace CalamityOverhaul.Content.ADV.Scenarios.Helen
{
    /// <summary>
    /// 比目鱼/海伦基础剧情线的存档数据
    /// </summary>
    public class HalibutADVData : ADVDataModule
    {
        public override string SaveKey => "Halibut";

        public bool HasCaughtHalibut;
        public bool FirstMet;
        public bool DyeProtest;
        public bool FishoilQuestDeclined;
        public bool FishoilQuestAccepted;
        public bool FishoilQuestCompleted;
        public bool FishoilQuestSuspended;
        public bool FirstResurrectionWarning;
    }
}
