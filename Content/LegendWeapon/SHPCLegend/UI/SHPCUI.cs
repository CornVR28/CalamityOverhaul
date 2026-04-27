using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.EntrustManager;
using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces;
using CalamityOverhaul.Content.QuestLogs;
using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.UI
{
    /// <summary>
    /// SHPC启动HUD主入口
    /// 屏幕左下角能量核心，点击后向右上方扇形展开多个交互按钮
    /// 仅在玩家手持SHPC时显示，避让全屏UI
    /// 全部使用程序化绘制，不依赖纹理资产
    /// </summary>
    internal class SHPCUI : UIHandle
    {
        public static SHPCUI Instance => UIHandleLoader.GetUIHandleOfType<SHPCUI>();

        #region 显隐与生命周期

        public override bool Active {
            get {
                Player p = Main.LocalPlayer;
                if (p == null || !p.active || p.dead) {
                    return false;
                }
                //仅在手持SHPC时显示
                Item held = p.HeldItem;
                if (held == null || held.IsAir || held.type != CWRID.Item_SHPC) {
                    return false;
                }
                //避让全屏UI
                if (QuestLog.Instance?.visible == true) {
                    return false;
                }
                if (QuestManagerUI.Instance?.IsOpen == true) {
                    return false;
                }
                if (Main.playerInventory || Main.inFancyUI) {
                    return false;
                }
                return true;
            }
        }

        #endregion

        #region 状态

        //展开进度，0为收起仅显示核心，1为完全展开
        private float expandProgress;
        //目标展开状态
        private bool expanded;
        //核心悬停强度，平滑跟随
        private float coreHoverAmt;
        //核心点击瞬闪强度，逐帧衰减
        private float clickFlash;
        //核心呼吸脉冲，点击时短暂提升
        private float corePulse;

        //当前悬停的扇区索引，-1表示无
        private int hoveredSector = -1;
        //当前选中的扇区索引，-1表示无
        private int selectedSector = -1;
        //每个扇区的悬停平滑强度
        private float[] hoverAmts;
        //每个扇区的选中平滑强度
        private float[] selectAmts;
        //二级信息面板的不透明度，平滑跟随
        private float infoPanelProgress;
        //当前用于展示的信息面板按钮索引，-1表示无
        private int infoButtonIdx = -1;
        //当前固定二级面板锁定的按钮索引，-1表示无
        private int pinnedSector = -1;
        //固定二级面板的不透明度，平滑跟随
        private float pinnedPanelProgress;
        //缓存上一帧赛博面板悬停的子控件
        private SHPCCyberPanel.HitKind cyberHover;

        //全局时间，单位秒
        private float time;

        //按钮配置列表
        private List<SHPCButtonDef> buttons;

        #endregion

        #region 按钮初始化

        /// <summary>
        /// 首次进入或按需重建按钮配置
        /// 当前为占位实现，等待具体功能接入后再细化
        /// </summary>
        private void EnsureButtons() {
            if (buttons != null) {
                return;
            }
            buttons = new List<SHPCButtonDef>(4) {
                new SHPCButtonDef {
                    Title = "CYBER DOMAIN",
                    Subtitle = "赛博领域",
                    Description = "展开/管理多层赛博空间",
                    Glyph = "D",
                    Enabled = () => true,
                    StatusValue = () => Cyberspace.Active
                        ? Cyberspace.CurrentLayer / (float)Cyberspace.MaxLayerCount
                        : -1f,
                    StatusText = () => Cyberspace.Active
                        ? "L" + Cyberspace.CurrentLayer
                        : "OFF",
                    OnClick = null,
                    UsesFixedPanel = true,
                },
                new SHPCButtonDef {
                    Title = "FIRE MODE",
                    Subtitle = "射击模式",
                    Description = "切换主武器的攻击模式",
                    Glyph = "F",
                    Enabled = () => true,
                    StatusValue = () => 0f,
                    StatusText = () => "STD",
                    OnClick = null,
                },
                new SHPCButtonDef {
                    Title = "STATUS",
                    Subtitle = "状态信息",
                    Description = "查看当前过载与冷却信息",
                    Glyph = "I",
                    Enabled = () => true,
                    StatusValue = () => 0f,
                    StatusText = () => "OK",
                    OnClick = null,
                },
                new SHPCButtonDef {
                    Title = "CONFIG",
                    Subtitle = "系统设置",
                    Description = "调整辅助选项与显示参数",
                    Glyph = "C",
                    Enabled = () => true,
                    StatusValue = () => -1f,
                    StatusText = () => "",
                    OnClick = null,
                },
            };
            hoverAmts = new float[buttons.Count];
            selectAmts = new float[buttons.Count];
        }

        #endregion

        #region 工具

        //取得核心位置
        private static Vector2 GetCorePosition() => new(96f, Main.screenHeight - 96f);

        //单按钮的角度区间
        private static void GetSectorAngles(int idx, int count, out float aStart, out float aEnd) {
            float total = SHPCTheme.FanEnd - SHPCTheme.FanStart;
            float gap = SHPCTheme.ButtonGap;
            float perAngle = (total - gap * (count - 1)) / count;
            aStart = SHPCTheme.FanStart + idx * (perAngle + gap);
            aEnd = aStart + perAngle;
        }

        //极坐标命中检测，给定鼠标到核心的偏移
        //返回-1表示未命中扇区，0..count-1表示命中索引
        //isCore通过out返回是否命中核心
        private int HitTest(Vector2 mouseOffset, out bool isCore) {
            isCore = false;
            float dist = mouseOffset.Length();
            //核心点击容差略大于实际外环
            if (dist <= SHPCTheme.CoreRingR + 6f) {
                isCore = true;
                return -1;
            }
            //仅在展开进度达到一定阈值后才允许点击扇区
            if (expandProgress < 0.6f) {
                return -1;
            }
            if (dist < SHPCTheme.ButtonInnerR - 4f || dist > SHPCTheme.ButtonOuterR + 4f) {
                return -1;
            }
            float ang = MathF.Atan2(mouseOffset.Y, mouseOffset.X);
            //角度容差，避免缝隙难点
            const float edgeTol = 0.025f;
            int count = buttons.Count;
            for (int i = 0; i < count; i++) {
                GetSectorAngles(i, count, out float a0, out float a1);
                if (ang >= a0 - edgeTol && ang <= a1 + edgeTol) {
                    return i;
                }
            }
            return -1;
        }

        //计算指定按钮固定二级面板的锚点与中线方向
        private void GetFixedPanelAnchor(int idx, out Vector2 anchor, out float midA) {
            int count = buttons.Count;
            GetSectorAngles(idx, count, out float a0, out float a1);
            midA = (a0 + a1) * 0.5f;
            anchor = GetCorePosition() + SHPCRenderer.AngleDir(midA) * SHPCTheme.ButtonOuterR;
        }

        #endregion

        #region 更新

        public override void Update() {
            EnsureButtons();
            time += 1f / 60f;

            //展开进度推进
            float targetExpand = expanded ? 1f : 0f;
            expandProgress = MathHelper.Lerp(expandProgress, targetExpand, expanded ? 0.18f : 0.22f);
            if (MathF.Abs(expandProgress - targetExpand) < 0.002f) {
                expandProgress = targetExpand;
            }

            //衰减瞬态值
            clickFlash *= 0.88f;
            if (clickFlash < 0.01f) {
                clickFlash = 0f;
            }
            corePulse *= 0.92f;
            if (corePulse < 0.01f) {
                corePulse = 0f;
            }

            //命中检测
            Vector2 corePos = GetCorePosition();
            Vector2 offset = MousePosition - corePos;
            int hit = HitTest(offset, out bool isCore);
            hoveredSector = hit;
            coreHoverAmt = MathHelper.Lerp(coreHoverAmt, isCore ? 1f : 0f, 0.2f);

            //平滑悬停与选中强度
            for (int i = 0; i < hoverAmts.Length; i++) {
                float ht = i == hoveredSector ? 1f : 0f;
                hoverAmts[i] = MathHelper.Lerp(hoverAmts[i], ht, 0.25f);
                float st = i == selectedSector ? 1f : 0f;
                selectAmts[i] = MathHelper.Lerp(selectAmts[i], st, 0.18f);
            }

            //固定二级面板进度与子控件命中
            float pinnedTarget = pinnedSector >= 0 && expandProgress > 0.7f ? 1f : 0f;
            pinnedPanelProgress = MathHelper.Lerp(pinnedPanelProgress, pinnedTarget, 0.22f);
            if (pinnedSector < 0 && pinnedPanelProgress < 0.02f) {
                pinnedPanelProgress = 0f;
            }
            cyberHover = SHPCCyberPanel.HitKind.None;
            SHPCCyberPanel.Layout cyberLayout = default;
            bool cyberPanelHit = false;
            if (pinnedSector >= 0 && pinnedSector < buttons.Count
                && buttons[pinnedSector].UsesFixedPanel && pinnedPanelProgress > 0.4f) {
                GetFixedPanelAnchor(pinnedSector, out Vector2 panelAnchor, out float panelMidA);
                cyberLayout = SHPCCyberPanel.Compute(panelAnchor, panelMidA, pinnedPanelProgress);
                cyberHover = SHPCCyberPanel.HitTest(cyberLayout, MousePosition);
                cyberPanelHit = cyberLayout.Panel.Contains((int)MousePosition.X, (int)MousePosition.Y);
            }

            //信息面板逻辑：优先展示悬停项，其次选中项；锁定面板期间隐藏例行信息面板以免重叠
            int targetInfo = hoveredSector >= 0 ? hoveredSector : selectedSector;
            float targetInfoAlpha = 0f;
            if (targetInfo >= 0 && expandProgress > 0.7f && pinnedSector < 0) {
                targetInfoAlpha = 1f;
                infoButtonIdx = targetInfo;
            }
            infoPanelProgress = MathHelper.Lerp(infoPanelProgress, targetInfoAlpha, 0.2f);
            if (infoPanelProgress < 0.02f) {
                infoButtonIdx = -1;
            }

            //领域快捷键：在手持SHPC且HUD可见时热键切换领域开关
            if (CWRKeySystem.Legend_Domain != null && CWRKeySystem.Legend_Domain.JustPressed) {
                if (Cyberspace.Active) {
                    Cyberspace.Deactivate();
                }
                else {
                    Cyberspace.Activate(player);
                }
            }

            //鼠标交互占用
            bool inHotArea = isCore || hit >= 0 ||
                (offset.Length() < SHPCTheme.ButtonOuterR + 8f && expandProgress > 0.4f) ||
                cyberPanelHit;
            if (inHotArea) {
                player.mouseInterface = true;
                player.CWR().DontSwitchWeaponTime = 2;
            }

            //左键处理
            if (keyLeftPressState == KeyPressState.Pressed) {
                if (cyberHover != SHPCCyberPanel.HitKind.None) {
                    SHPCCyberPanel.HandleClick(cyberHover, player);
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
                else if (isCore) {
                    expanded = !expanded;
                    clickFlash = 1f;
                    corePulse = 1f;
                    if (!expanded) {
                        pinnedSector = -1;
                    }
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
                else if (hit >= 0 && expandProgress > 0.6f) {
                    SHPCButtonDef def = buttons[hit];
                    bool enabled = def.Enabled?.Invoke() ?? true;
                    if (enabled) {
                        selectedSector = hit;
                        if (def.UsesFixedPanel) {
                            //切换锁定状态：同一按钮再点击则收起
                            pinnedSector = pinnedSector == hit ? -1 : hit;
                        }
                        else if (pinnedSector >= 0 && pinnedSector != hit) {
                            pinnedSector = -1;
                        }
                        def.OnClick?.Invoke();
                        SoundEngine.PlaySound(SoundID.MenuTick);
                    }
                    else {
                        SoundEngine.PlaySound(SoundID.MenuClose);
                    }
                }
                else if (!cyberPanelHit) {
                    //面板外点击不取消锁定，仅需保证收起总HUD时同步清除
                }
            }

            //总HUD收起同步收起锁定面板
            if (!expanded) {
                pinnedSector = -1;
            }

            //更新命中盒，便于上层屏蔽世界点击
            int hitR = (int)(SHPCTheme.ButtonOuterR + 12f);
            UIHitBox = new Rectangle(
                (int)(corePos.X - hitR), (int)(corePos.Y - hitR),
                hitR * 2, hitR * 2);
        }

        #endregion

        #region 绘制

        public override void Draw(SpriteBatch sb) {
            if (buttons == null) {
                return;
            }
            Texture2D px = CWRAsset.Placeholder_White?.Value ?? TextureAssets.MagicPixel.Value;
            if (px == null) {
                return;
            }

            Vector2 corePos = GetCorePosition();
            const float globalAlpha = 1f;

            //先绘制连接线，置于扇区下方
            int count = buttons.Count;
            for (int i = 0; i < count; i++) {
                GetSectorAngles(i, count, out float a0, out float a1);
                float midA = (a0 + a1) * 0.5f;
                SHPCRenderer.DrawConnector(sb, px, corePos, midA, expandProgress,
                    hoverAmts[i], globalAlpha);
            }

            //扇形按钮
            for (int i = 0; i < count; i++) {
                GetSectorAngles(i, count, out float a0, out float a1);
                SHPCButtonDef def = buttons[i];
                bool enabled = def.Enabled?.Invoke() ?? true;
                float status = def.StatusValue?.Invoke() ?? -1f;
                SHPCRenderer.DrawSector(sb, px, corePos, a0, a1, expandProgress,
                    hoverAmts[i], selectAmts[i], enabled,
                    MathHelper.Clamp(status, 0f, 1f),
                    def.Glyph, time, globalAlpha);
            }

            //核心
            SHPCRenderer.DrawCore(sb, px, corePos, expandProgress,
                coreHoverAmt, corePulse, clickFlash, time, globalAlpha);

            //二级信息面板，最后绘制保证置顶
            if (infoButtonIdx >= 0 && infoButtonIdx < buttons.Count && infoPanelProgress > 0.02f) {
                GetSectorAngles(infoButtonIdx, count, out float a0, out float a1);
                float midA = (a0 + a1) * 0.5f;
                Vector2 anchor = corePos + SHPCRenderer.AngleDir(midA) * SHPCTheme.ButtonOuterR;
                SHPCButtonDef def = buttons[infoButtonIdx];
                SHPCRenderer.DrawInfoPanel(sb, px, anchor, midA,
                    infoPanelProgress, globalAlpha,
                    def.Title, def.Subtitle, def.Description,
                    def.StatusText?.Invoke());
            }

            //固定二级面板（赛博领域）
            if (pinnedSector >= 0 && pinnedSector < buttons.Count && pinnedPanelProgress > 0.02f) {
                GetFixedPanelAnchor(pinnedSector, out Vector2 fAnchor, out float fMidA);
                SHPCCyberPanel.Layout layout = SHPCCyberPanel.Compute(fAnchor, fMidA, pinnedPanelProgress);
                SHPCCyberPanel.Draw(sb, px, layout, pinnedPanelProgress, globalAlpha, cyberHover);
            }
        }

        #endregion
    }
}
