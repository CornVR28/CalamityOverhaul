using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.UIs.NotificationPopup
{
    /// <summary>
    /// 弹窗通知条目基类，子类通过重写 <see cref="DrawContent"/> 自定义外观，
    /// 通过重写 <see cref="OnClick"/> 自定义点击行为
    /// </summary>
    internal abstract class NotificationEntry
    {
        /// <summary>弹窗宽度</summary>
        public virtual float Width => 260f;

        /// <summary>弹窗高度</summary>
        public virtual float Height => 60f;

        /// <summary>滑入/滑出动画帧数</summary>
        public virtual int SlideTime => 20;

        /// <summary>完全展示停留帧数</summary>
        public virtual int DisplayTime => 180;

        /// <summary>与下一条弹窗的间距</summary>
        public virtual float Gap => 5f;

        /// <summary>弹出时的音效，null则使用系统默认音效</summary>
        public virtual SoundStyle? AppearSound => null;

        /// <summary>
        /// 绘制弹窗内容，由系统在正确的动画位置调用
        /// </summary>
        /// <param name="sb">SpriteBatch</param>
        /// <param name="panelRect">弹窗屏幕区域（已根据动画偏移）</param>
        /// <param name="alpha">当前透明度（0~1）</param>
        public abstract void DrawContent(SpriteBatch sb, Rectangle panelRect, float alpha);

        /// <summary>
        /// 弹窗被点击时调用，返回 true 则提前收起弹窗，返回 false 则不收起
        /// </summary>
        public virtual bool OnClick() => true;

        #region 辅助绘制

        /// <summary>
        /// 绘制标准面板背景（半透明黑底 + 上下左边框线）
        /// </summary>
        protected static void DrawPanelBackground(SpriteBatch sb, Rectangle rect,
            Color bgColor, Color borderColor, float alpha) {
            sb.Draw(TextureAssets.MagicPixel.Value, rect, bgColor * alpha);
            //上边框
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(rect.X, rect.Y, rect.Width, 2), borderColor * alpha);
            //下边框
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), borderColor * (alpha * 0.5f));
            //左边框
            sb.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle(rect.X, rect.Y, 2, rect.Height), borderColor * alpha);
        }

        #endregion
    }
}
