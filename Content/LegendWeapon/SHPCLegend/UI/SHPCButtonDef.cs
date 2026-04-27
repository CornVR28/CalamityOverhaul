using System;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.UI
{
    /// <summary>
    /// 扇形HUD单个按钮的配置数据
    /// 所有可视化字段都通过委托而非缓存值，保证每帧拿到最新状态
    /// </summary>
    internal class SHPCButtonDef
    {
        /// <summary>
        /// 短标题，绘制于二级信息面板顶部
        /// </summary>
        public string Title;
        /// <summary>
        /// 副标题或类别标签
        /// </summary>
        public string Subtitle;
        /// <summary>
        /// 信息面板下方的简短说明，可包含一处换行
        /// </summary>
        public string Description;
        /// <summary>
        /// 程序化图标符号，单字符或两字符，用于按钮中心
        /// </summary>
        public string Glyph;
        /// <summary>
        /// 是否启用，禁用按钮仍可见但不可点击
        /// </summary>
        public Func<bool> Enabled;
        /// <summary>
        /// 当前状态值，范围0到1，用于按钮内部的弧形进度条
        /// 返回负值或null委托表示不绘制状态条
        /// </summary>
        public Func<float> StatusValue;
        /// <summary>
        /// 状态值文本，例如"3/5"或"READY"，绘制于信息面板状态行
        /// </summary>
        public Func<string> StatusText;
        /// <summary>
        /// 点击回调，可空
        /// </summary>
        public Action OnClick;
        /// <summary>
        /// 是否在按下后弹出固定二级面板，由UI层负责面板的渲染与命中
        /// 设为true时，<see cref="OnClick"/>仅作为面板开关切换的附加回调
        /// </summary>
        public bool UsesFixedPanel;
    }
}
