namespace CalamityOverhaul.Content.ADV.IncomingCalls
{
    /// <summary>
    /// 来电系统生命周期状态
    /// </summary>
    public enum IncomingCallState
    {
        /// <summary>
        /// 空闲，未激活
        /// </summary>
        Idle,
        /// <summary>
        /// 来电滑入屏幕，待接听振铃中
        /// </summary>
        Ringing,
        /// <summary>
        /// 接听过渡动画（面板展开到通话模式）
        /// </summary>
        Connecting,
        /// <summary>
        /// 通话中，正在逐条播放台词
        /// </summary>
        Speaking,
        /// <summary>
        /// 挂断/结束，面板滑出
        /// </summary>
        Ending
    }
}
