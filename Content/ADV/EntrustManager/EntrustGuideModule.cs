namespace CalamityOverhaul.Content.ADV.EntrustManager
{
    /// <summary>
    /// 委托系统首次引导的存档数据，由ADVSave自动发现和管理
    /// </summary>
    internal class EntrustGuideModule : ADVDataModule
    {
        /// <summary>引导是否已经完成过（true则不再触发）</summary>
        public bool GuideSeen = false;
    }
}
