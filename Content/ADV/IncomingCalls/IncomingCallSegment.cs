using System;

namespace CalamityOverhaul.Content.ADV.IncomingCalls
{
    /// <summary>
    /// 来电台词段数据
    /// </summary>
    public class IncomingCallSegment
    {
        /// <summary>
        /// 说话者名称
        /// </summary>
        public string Speaker;

        /// <summary>
        /// 台词内容
        /// </summary>
        public string Content;

        /// <summary>
        /// 立绘键（为null时使用Speaker）
        /// </summary>
        public string PortraitKey;

        /// <summary>
        /// 本段台词开始时回调
        /// </summary>
        public Action OnStart;

        /// <summary>
        /// 本段台词结束时回调
        /// </summary>
        public Action OnFinish;

        /// <summary>
        /// 自动推进延迟帧数（0 表示需要手动点击推进，>0 表示打字完成后自动等待N帧后推进）
        /// </summary>
        public int AutoAdvanceDelay;
    }
}
