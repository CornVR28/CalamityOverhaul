using Microsoft.Xna.Framework.Graphics;

namespace CalamityOverhaul.Content.ADV.Extras
{
    public interface IExtraStyle
    {
        //更新样式动画
        void UpdateStyle();
        //绘制面板背景
        void DrawBackground(SpriteBatch spriteBatch, ExtraMain extra, Rectangle panelRect);
        //绘制标题区域（EXTRA + 完成度百分比）
        void DrawTitle(SpriteBatch spriteBatch, ExtraMain extra, Rectangle panelRect, float alpha);
        //绘制标签栏
        void DrawTabBar(SpriteBatch spriteBatch, ExtraMain extra, Rectangle panelRect, ExtraTab activeTab, float alpha);
        //绘制单个CG缩略图（已解锁状态）
        void DrawCGThumbnail(SpriteBatch spriteBatch, CGEntry entry, Rectangle thumbRect, bool isHovered, float alpha);
        //绘制未解锁CG占位（深色渐变+问号）
        void DrawLockedCG(SpriteBatch spriteBatch, Rectangle thumbRect, float alpha);
        //绘制右侧滚动条
        void DrawScrollbar(SpriteBatch spriteBatch, Rectangle trackRect, float scrollRatio, float viewRatio, float alpha);
        //绘制CG全屏查看器
        void DrawFullscreenViewer(SpriteBatch spriteBatch, CGEntry entry, float alpha);
        //绘制返回按钮
        void DrawBackButton(SpriteBatch spriteBatch, Rectangle panelRect, bool isHovered, float alpha);
        //绘制全屏查看器的左右切换箭头
        void DrawViewerArrow(SpriteBatch spriteBatch, Rectangle arrowRect, bool isLeft, bool isHovered, float alpha);
        //获取标签栏区域
        Rectangle GetTabBarRect(Rectangle panelRect);
        //获取返回按钮区域
        Rectangle GetBackButtonRect(Rectangle panelRect);
        //获取左箭头区域
        Rectangle GetLeftArrowRect();
        //获取右箭头区域
        Rectangle GetRightArrowRect();
        //获取面板内边距（左/上/右/下）
        Vector4 GetPadding();
    }
}
