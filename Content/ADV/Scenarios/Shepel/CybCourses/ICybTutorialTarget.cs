using Microsoft.Xna.Framework;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.CybCourses
{
    //教程高亮目标接口，外部系统通过实现此接口向教程引导层暴露UI元素的屏幕位置
    //不需要入侵任何内部UI代码，只需在独立的注册类中计算位置并注册
    internal interface ICybTutorialTarget
    {
        string Key { get; }
        //返回目标元素当前帧的屏幕矩形，用于绘制高亮框和箭头
        Rectangle GetScreenRect();
        //目标当前是否可见可操作
        bool IsAvailable { get; }
    }
}
