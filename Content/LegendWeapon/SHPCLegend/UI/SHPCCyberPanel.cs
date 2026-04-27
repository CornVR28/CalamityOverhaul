using CalamityOverhaul.Common;
using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.UI
{
    /// <summary>
    /// 赛博空间领域管理面板
    /// 黑墙AI风格 深红色系 + 三段式领域指示环 + 故障层叠环 + 自定义着色器背景
    /// 由 <see cref="SHPCUI"/> 在固定二级面板模式下调用
    /// </summary>
    internal static class SHPCCyberPanel
    {
        public const float PanelW = 248f;
        public const float PanelH = 184f;
        private const float EdgePad = 12f;

        //三色环主区域参数
        private const float RingOuterR = 50f;
        private const float RingInnerR = 32f;

        //段间缝隙
        private const float SegmentGap = 0.10f;
        //12点方向起始
        private const float StartAngle = -MathHelper.PiOver2;

        /// <summary>
        /// 面板内可点击控件枚举
        /// </summary>
        public enum HitKind
        {
            None,
            Toggle,
            Layer1,
            Layer2,
            Layer3,
        }

        public ref struct Layout
        {
            public Rectangle Panel;
            public Vector2 RingCenter;
            public Rectangle Toggle;
        }

        /// <summary>
        /// 根据扇区锚点与中线方向计算面板位置
        /// </summary>
        public static Layout Compute(Vector2 anchor, float midAngle, float panelAlpha) {
            Vector2 outDir = SHPCRenderer.AngleDir(midAngle);
            float slide = (1f - panelAlpha) * 14f;
            Vector2 panelPos = anchor + outDir * (SHPCTheme.InfoPanelGap + slide);
            panelPos.Y -= PanelH * 0.5f;

            Rectangle panel = new((int)panelPos.X, (int)panelPos.Y, (int)PanelW, (int)PanelH);
            Vector2 ringCenter = new(panel.X + panel.Width * 0.32f, panel.Y + panel.Height * 0.50f);
            int toggleW = panel.Width - 24;
            Rectangle toggle = new(panel.X + 12, panel.Bottom - 36, toggleW, 24);

            return new Layout {
                Panel = panel,
                RingCenter = ringCenter,
                Toggle = toggle,
            };
        }

        /// <summary>
        /// 给定层数(1..3)取该段的角度区间
        /// </summary>
        private static void GetSegmentAngles(int layer, out float a0, out float a1) {
            float per = MathHelper.TwoPi / 3f;
            float center = StartAngle + (layer - 1) * per + per * 0.5f;
            a0 = center - per * 0.5f + SegmentGap * 0.5f;
            a1 = center + per * 0.5f - SegmentGap * 0.5f;
        }

        /// <summary>
        /// 命中测试
        /// </summary>
        public static HitKind HitTest(in Layout layout, Vector2 mouse) {
            if (layout.Toggle.Contains((int)mouse.X, (int)mouse.Y)) {
                return HitKind.Toggle;
            }
            Vector2 d = mouse - layout.RingCenter;
            float dist = d.Length();
            if (dist >= RingInnerR - 4f && dist <= RingOuterR + 6f) {
                float ang = MathF.Atan2(d.Y, d.X);
                float rel = MathHelper.WrapAngle(ang - StartAngle);
                if (rel < 0f) rel += MathHelper.TwoPi;
                int seg = (int)(rel / (MathHelper.TwoPi / 3f));
                seg = Math.Clamp(seg, 0, 2);
                return seg == 0 ? HitKind.Layer1 : seg == 1 ? HitKind.Layer2 : HitKind.Layer3;
            }
            return HitKind.None;
        }

        /// <summary>
        /// 整体面板绘制
        /// </summary>
        public static void Draw(SpriteBatch sb, Texture2D px, in Layout layout,
            float panelAlpha, float globalAlpha, HitKind hover) {
            if (panelAlpha < 0.02f) {
                return;
            }
            float a = panelAlpha * globalAlpha;
            Rectangle rect = layout.Panel;
            float time = (float)Main.GameUpdateCount / 60f;

            //投影
            SHPCRenderer.DrawFilledRect(sb, px,
                new Rectangle(rect.X + 3, rect.Y + 4, rect.Width, rect.Height),
                new Color(0, 0, 0) * (0.55f * a));

            //着色器背景
            DrawShaderBackground(sb, px, rect, panelAlpha, globalAlpha);

            //外框 + 四角L形装饰
            SHPCRenderer.DrawCornerBrackets(sb, px, rect, 9f, 1.5f,
                new Color(255, 90, 80) * (0.95f * a));
            SHPCRenderer.DrawRectStroke(sb, px, rect, 1.2f, new Color(140, 30, 30) * (0.9f * a));

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            CWRLocText loc = CWRLocText.Instance;

            //标题
            Utils.DrawBorderString(sb, loc.SHPC_HUD_Cyber_PanelTitle.Value,
                new Vector2(rect.X + 10, rect.Y + 7), new Color(255, 230, 230) * a, 0.62f);
            Utils.DrawBorderString(sb, loc.SHPC_HUD_Cyber_PanelSubtitle.Value,
                new Vector2(rect.X + 10, rect.Y + 24), new Color(180, 60, 60) * a, 0.40f);

            //右上ID码，每秒滚动
            string idCode = $"#{(int)(time * 11f) % 999:D3}";
            Vector2 idSize = font.MeasureString(idCode) * 0.45f;
            Utils.DrawBorderString(sb, idCode,
                new Vector2(rect.Right - 10 - idSize.X, rect.Y + 9),
                new Color(255, 120, 100) * (0.85f * a), 0.45f);

            //三色环主体
            DrawTriRing(sb, px, layout.RingCenter, time, hover, a);

            //右侧状态栏
            DrawStatusColumn(sb, font, rect, layout, a);

            //开关按钮
            DrawToggleButton(sb, px, font, layout.Toggle, hover == HitKind.Toggle, a);

            //快捷键提示
            string keyName = CWRKeySystem.Legend_Domain != null
                && CWRKeySystem.Legend_Domain.GetAssignedKeys().Count > 0
                ? CWRKeySystem.Legend_Domain.GetAssignedKeys()[0]
                : "?";
            string hint = string.Format(loc.SHPC_HUD_Cyber_Hint.Value, keyName);
            Vector2 hintSize = font.MeasureString(hint) * 0.36f;
            Utils.DrawBorderString(sb, hint,
                new Vector2(rect.X + (rect.Width - hintSize.X) * 0.5f, rect.Bottom - 12),
                new Color(220, 130, 120) * (0.85f * a), 0.36f);
        }

        /// <summary>
        /// 使用CyberDomainPanel着色器绘制背景，失败则降级为纯色
        /// </summary>
        private static void DrawShaderBackground(SpriteBatch sb, Texture2D px,
            Rectangle rect, float panelAlpha, float globalAlpha) {
            float a = panelAlpha * globalAlpha;
            if (EffectLoader.CyberDomainPanel?.Value == null) {
                SHPCRenderer.DrawFilledRect(sb, px, rect, new Color(28, 6, 10) * (0.96f * a));
                return;
            }

            Effect effect = EffectLoader.CyberDomainPanel.Value;
            float time = (float)Main.GameUpdateCount / 60f;

            //收缩动画期间用层展开进度求得带平滑过渡的浮点层数
            float floatLayer = 0f;
            for (int i = 0; i < Cyberspace.MaxLayerCount; i++) {
                floatLayer += Cyberspace.GetLayerExpand(i);
            }
            float layerParam = Cyberspace.Active
                ? MathF.Max(Cyberspace.CurrentLayer * 0.4f + floatLayer * 0.6f, floatLayer)
                : floatLayer;

            effect.Parameters["uTime"]?.SetValue(time);
            effect.Parameters["uAlpha"]?.SetValue(a * 0.97f);
            effect.Parameters["uResolution"]?.SetValue(new Vector2(rect.Width, rect.Height));
            effect.Parameters["uEdgePad"]?.SetValue(EdgePad);
            effect.Parameters["uLayer"]?.SetValue(layerParam);
            effect.Parameters["uIntensity"]?.SetValue(MathHelper.Clamp(Cyberspace.Intensity, 0f, 1f));

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.None,
                RasterizerState.CullNone, effect, Main.UIScaleMatrix);

            sb.Draw(px, rect, Color.White);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.AnisotropicClamp, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.UIScaleMatrix);
        }

        /// <summary>
        /// 绘制三段式领域指示环 + 中央心跳核心
        /// </summary>
        private static void DrawTriRing(SpriteBatch sb, Texture2D px,
            Vector2 center, float time, HitKind hover, float a) {
            //中央深色底盘
            SHPCRenderer.DrawDisc(sb, px, center, RingInnerR - 4f, 4f,
                new Color(10, 4, 6) * a);

            //核心心跳
            float beat = 0.5f + 0.5f * MathF.Sin(time * 3f);
            float beatScale = Cyberspace.Active ? 0.6f + 0.4f * Cyberspace.Intensity : 0.25f;
            SHPCRenderer.DrawDisc(sb, px, center,
                (RingInnerR - 12f) * (0.55f + 0.10f * beat), 3f,
                new Color(255, 80, 60) * (beatScale * a));

            //三段
            for (int i = 0; i < 3; i++) {
                int layer = i + 1;
                bool lit = Cyberspace.Active && Cyberspace.CurrentLayer >= layer;
                float litAmt = Cyberspace.GetLayerExpand(i);
                bool isHover = (i == 0 && hover == HitKind.Layer1)
                    || (i == 1 && hover == HitKind.Layer2)
                    || (i == 2 && hover == HitKind.Layer3);

                GetSegmentAngles(layer, out float a0, out float a1);
                DrawRingSegment(sb, px, center, a0, a1, layer, litAmt, lit, isHover, time, a);
            }

            //中央层数文本
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string txt = Cyberspace.Active ? Cyberspace.CurrentLayer.ToString() : "0";
            Vector2 size = font.MeasureString(txt) * 0.95f;
            Color textCol = Cyberspace.Active ? new Color(255, 220, 215) : new Color(120, 80, 80);
            Utils.DrawBorderString(sb, txt,
                center - size * 0.5f + new Vector2(0f, -2f), textCol * a, 0.95f);

            //中央上方LAYER小字
            string label = CWRLocText.Instance.SHPC_HUD_Cyber_LayerLabel.Value;
            Vector2 lblSize = font.MeasureString(label) * 0.35f;
            Utils.DrawBorderString(sb, label,
                center + new Vector2(-lblSize.X * 0.5f, -RingInnerR + 6f),
                new Color(180, 70, 70) * (0.85f * a), 0.35f);
        }

        /// <summary>
        /// 绘制单段环
        /// </summary>
        private static void DrawRingSegment(SpriteBatch sb, Texture2D px,
            Vector2 center, float a0, float a1, int layer,
            float litAmt, bool litFlag, bool hover, float time, float a) {
            //底环
            SHPCRenderer.DrawArc(sb, px, center, RingInnerR, RingOuterR, a0, a1,
                new Color(60, 14, 18) * (0.95f * a));

            //点亮填充，按litAmt插值半径
            if (litAmt > 0.01f) {
                Color hot = layer == 1 ? new Color(255, 70, 50)
                    : layer == 2 ? new Color(255, 130, 60)
                    : new Color(255, 200, 80);
                float fillR = MathHelper.Lerp(RingInnerR, RingOuterR, MathHelper.Clamp(litAmt, 0f, 1f));
                SHPCRenderer.DrawArc(sb, px, center, RingInnerR, fillR, a0, a1, hot * (0.55f * a));
                SHPCRenderer.DrawArcStroke(sb, px, center, fillR - 1f, a0, a1, 1.6f,
                    hot * (0.95f * a));

                //段内扫光，沿角度推进
                float scanT = (time * (0.55f + 0.20f * layer)) % 1f;
                float scanA = MathHelper.Lerp(a0, a1, scanT);
                float scanW = (a1 - a0) * 0.18f;
                float sa0 = MathF.Max(a0, scanA - scanW * 0.5f);
                float sa1 = MathF.Min(a1, scanA + scanW * 0.5f);
                SHPCRenderer.DrawArc(sb, px, center, RingInnerR + 2f, RingOuterR - 2f, sa0, sa1,
                    new Color(255, 240, 220) * (0.45f * litAmt * a));
            }

            //悬停柔光
            if (hover) {
                SHPCRenderer.DrawArc(sb, px, center, RingInnerR, RingOuterR, a0, a1,
                    new Color(255, 200, 180) * (0.18f * a));
            }

            //段描边
            Color border = litFlag
                ? new Color(255, 220, 200)
                : hover ? new Color(255, 150, 130) : new Color(160, 50, 50);
            SHPCRenderer.DrawArcStroke(sb, px, center, RingOuterR - 0.5f, a0, a1, 1.4f, border * a);
            SHPCRenderer.DrawArcStroke(sb, px, center, RingInnerR + 0.5f, a0, a1, 1.1f, border * (0.6f * a));

            //径向封口
            Vector2 dirS = SHPCRenderer.AngleDir(a0);
            Vector2 dirE = SHPCRenderer.AngleDir(a1);
            SHPCRenderer.DrawLine(sb, px,
                center + dirS * RingInnerR, center + dirS * RingOuterR,
                1.4f, border * (0.7f * a));
            SHPCRenderer.DrawLine(sb, px,
                center + dirE * RingInnerR, center + dirE * RingOuterR,
                1.4f, border * (0.7f * a));

            //段内层数文字
            float midA = (a0 + a1) * 0.5f;
            Vector2 textPos = center + SHPCRenderer.AngleDir(midA) * (RingInnerR + RingOuterR) * 0.5f;
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string s = layer.ToString();
            Vector2 sz = font.MeasureString(s) * 0.55f;
            Color tc = litFlag ? new Color(255, 250, 240) : new Color(180, 100, 90);
            Utils.DrawBorderString(sb, s, textPos - sz * 0.5f, tc * a, 0.55f);
        }

        /// <summary>
        /// 右侧状态文字栏，列出每层激活状态
        /// </summary>
        private static void DrawStatusColumn(SpriteBatch sb, DynamicSpriteFont font,
            Rectangle panel, in Layout layout, float a) {
            float colX = layout.RingCenter.X + RingOuterR + 18f;
            float colY = panel.Y + 44f;
            float lineH = 18f;

            string state = Cyberspace.Active
                ? CWRLocText.Instance.SHPC_HUD_Cyber_StateOnline.Value
                : CWRLocText.Instance.SHPC_HUD_Cyber_StateOffline.Value;
            Color stateCol = Cyberspace.Active ? new Color(255, 200, 180) : new Color(140, 80, 80);
            Utils.DrawBorderString(sb, state, new Vector2(colX, colY), stateCol * a, 0.46f);
            colY += lineH;

            for (int i = 0; i < 3; i++) {
                int layer = i + 1;
                bool lit = Cyberspace.Active && Cyberspace.CurrentLayer >= layer;
                string mark = lit ? "[#]" : "[ ]";
                Color tc = lit ? new Color(255, 200, 170) : new Color(120, 60, 60);
                Utils.DrawBorderString(sb, $"{mark} L{layer}", new Vector2(colX, colY), tc * a, 0.42f);
                colY += lineH;
            }
        }

        /// <summary>
        /// 底部开关按钮
        /// </summary>
        private static void DrawToggleButton(SpriteBatch sb, Texture2D px,
            DynamicSpriteFont font, Rectangle r, bool hover, float a) {
            bool active = Cyberspace.Active;
            string txt = active
                ? CWRLocText.Instance.SHPC_HUD_Cyber_BtnDeactivate.Value
                : CWRLocText.Instance.SHPC_HUD_Cyber_BtnActivate.Value;

            Color bg = active ? new Color(140, 30, 30) : new Color(60, 12, 18);
            if (hover) bg = active ? new Color(200, 60, 50) : new Color(110, 30, 30);
            SHPCRenderer.DrawFilledRect(sb, px, r, bg * (0.85f * a));

            Color border = hover ? new Color(255, 220, 200)
                : active ? new Color(255, 130, 110) : new Color(180, 50, 50);
            SHPCRenderer.DrawRectStroke(sb, px, r, 1.4f, border * a);

            //方块端帽
            int capSize = 4;
            SHPCRenderer.DrawFilledRect(sb, px,
                new Rectangle(r.X - 2, r.Y + r.Height / 2 - capSize, capSize, capSize * 2), border * a);
            SHPCRenderer.DrawFilledRect(sb, px,
                new Rectangle(r.Right - 2, r.Y + r.Height / 2 - capSize, capSize, capSize * 2), border * a);

            Vector2 size = font.MeasureString(txt) * 0.50f;
            Color textCol = active ? new Color(255, 240, 230) : new Color(255, 200, 180);
            Utils.DrawBorderString(sb, txt,
                new Vector2(r.X + (r.Width - size.X) * 0.5f, r.Y + (r.Height - size.Y) * 0.5f - 1f),
                textCol * a, 0.50f);
        }

        /// <summary>
        /// 处理面板控件点击效果
        /// </summary>
        public static void HandleClick(HitKind hit, Player owner) {
            switch (hit) {
                case HitKind.Toggle:
                    if (Cyberspace.Active) {
                        Cyberspace.Deactivate();
                    }
                    else {
                        Cyberspace.Activate(owner);
                    }
                    break;
                case HitKind.Layer1:
                case HitKind.Layer2:
                case HitKind.Layer3:
                    int target = hit == HitKind.Layer1 ? 1 : hit == HitKind.Layer2 ? 2 : 3;
                    if (!Cyberspace.Active) {
                        Cyberspace.Activate(owner);
                    }
                    Cyberspace.SetLayer(target, owner);
                    break;
            }
        }
    }
}
