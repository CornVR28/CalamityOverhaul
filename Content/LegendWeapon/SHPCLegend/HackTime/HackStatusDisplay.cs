using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.HackTime
{
    //NPC头顶骇入状态悬浮窗口
    //对每个受骇入影响的NPC，在其头顶纵向排列绘制协议状态卡片
    //包括：已生效的协议（显示剩余时间进度条）和正在上传的协议（显示上传进度条）
    internal static class HackStatusDisplay
    {
        //卡片尺寸
        private const float CardWidth = 140f;
        private const float CardHeight = 22f;
        private const float CardGap = 3f;
        private const float BarHeight = 3f;
        private const float BarMargin = 4f;
        private const float FontScale = 0.28f;
        //距NPC头顶的距离
        private const float TopOffset = 18f;

        //复用缓冲
        private static readonly List<ActiveHackEffect> effectBuf = [];
        private static readonly List<HackQueueEntry> queueBuf = [];
        //收集需要绘制的NPC索引集合（避免重复）
        private static readonly HashSet<int> npcSet = [];

        public static void Draw(SpriteBatch sb) {
            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            //收集所有需要绘制的NPC
            npcSet.Clear();
            var allEffects = HackEffectTracker.AllActiveEffects;
            for (int i = 0; i < allEffects.Count; i++) {
                if (allEffects[i].Active)
                    npcSet.Add(allEffects[i].TargetIndex);
            }

            //也收集队列中正在上传的目标
            var queue = HackTimeUI.Instance?.Queue;
            if (queue != null) {
                var entries = queue.Entries;
                for (int i = 0; i < entries.Count; i++) {
                    npcSet.Add(entries[i].TargetIndex);
                }
            }

            if (npcSet.Count == 0) return;

            //逐NPC绘制
            foreach (int npcIndex in npcSet) {
                if (npcIndex < 0 || npcIndex >= Main.maxNPCs) continue;
                NPC npc = Main.npc[npcIndex];
                if (!npc.active) continue;

                DrawNPCStatus(sb, px, npc, npcIndex, queue);
            }
        }

        private static void DrawNPCStatus(SpriteBatch sb, Texture2D px, NPC npc, int npcIndex, HackQueueRenderer queue) {
            //收集该NPC的活跃效果和上传条目
            HackEffectTracker.GetEffects(npcIndex, effectBuf);
            if (queue != null)
                queue.GetEntriesForNPC(npcIndex, queueBuf);
            else
                queueBuf.Clear();

            int totalCards = effectBuf.Count + queueBuf.Count;
            if (totalCards == 0) return;

            //计算NPC屏幕坐标，卡片从头顶向上排列
            Vector2 npcScreen = npc.Top - Main.screenPosition;
            float totalHeight = totalCards * (CardHeight + CardGap) - CardGap;
            float startY = npcScreen.Y - TopOffset - totalHeight;
            float baseX = npcScreen.X - CardWidth * 0.5f;

            int cardIndex = 0;

            //先绘制上传中的条目（最上方）
            for (int i = 0; i < queueBuf.Count; i++) {
                var entry = queueBuf[i];
                float y = startY + cardIndex * (CardHeight + CardGap);
                DrawUploadCard(sb, px, baseX, y, entry);
                cardIndex++;
            }

            //再绘制已生效的协议
            for (int i = 0; i < effectBuf.Count; i++) {
                var eff = effectBuf[i];
                float y = startY + cardIndex * (CardHeight + CardGap);
                DrawActiveCard(sb, px, baseX, y, eff);
                cardIndex++;
            }
        }

        //绘制正在上传的协议卡片
        private static void DrawUploadCard(SpriteBatch sb, Texture2D px, float x, float y, HackQueueEntry entry) {
            //卡片暗底
            Color bgColor = HackTheme.BgPanel * 0.85f;
            sb.Draw(px, new Rectangle((int)x, (int)y, (int)CardWidth, (int)CardHeight), bgColor);

            //左侧竖条（琥珀色表示上传中）
            Color barColor = entry.State == HackQueueState.Uploading
                ? HackTheme.Uploading
                : HackTheme.TextDim;
            sb.Draw(px, new Rectangle((int)x, (int)y, 2, (int)CardHeight), barColor);

            //协议名称
            string name = entry.Hack.DisplayName.Value;
            float nameX = x + 6f;
            float nameY = y + 2f;
            Utils.DrawBorderString(sb, name, new Vector2(nameX, nameY), HackTheme.Uploading, FontScale);

            //状态文字
            string status = entry.State switch {
                HackQueueState.Waiting => HackTime.Queued.Value,
                HackQueueState.Uploading => HackTime.UploadingPct.Format((int)(entry.UploadProgress * 100)),
                HackQueueState.Completed => HackTime.Complete.Value,
                _ => ""
            };
            float statusWidth = FontAssets.MouseText.Value.MeasureString(status).X * (FontScale * 0.85f);
            Utils.DrawBorderString(sb, status, new Vector2(x + CardWidth - statusWidth - 4f, nameY),
                HackTheme.TextDim, FontScale * 0.85f);

            //上传进度条
            float barY = y + CardHeight - BarHeight - 2f;
            float barW = CardWidth - BarMargin * 2;
            //进度条背景
            sb.Draw(px, new Rectangle((int)(x + BarMargin), (int)barY, (int)barW, (int)BarHeight),
                HackTheme.ProgressBg * 0.9f);
            //进度条填充
            float fill = Math.Clamp(entry.UploadProgress, 0f, 1f);
            if (fill > 0) {
                Color fillColor = entry.State == HackQueueState.Completed
                    ? HackTheme.Accent
                    : HackTheme.Uploading;
                sb.Draw(px, new Rectangle((int)(x + BarMargin), (int)barY, (int)(barW * fill), (int)BarHeight),
                    fillColor);
            }

            //边框
            DrawCardBorder(sb, px, x, y, HackTheme.Border * 0.6f);
        }

        //绘制已生效的协议卡片
        private static void DrawActiveCard(SpriteBatch sb, Texture2D px, float x, float y, ActiveHackEffect eff) {
            //卡片暗底
            Color bgColor = HackTheme.BgSection * 0.85f;
            sb.Draw(px, new Rectangle((int)x, (int)y, (int)CardWidth, (int)CardHeight), bgColor);

            //左侧竖条（青色表示生效中）
            sb.Draw(px, new Rectangle((int)x, (int)y, 2, (int)CardHeight), HackTheme.Accent);

            //协议名称
            string name = eff.Hack.DisplayName.Value;
            float nameX = x + 6f;
            float nameY = y + 2f;
            Utils.DrawBorderString(sb, name, new Vector2(nameX, nameY), HackTheme.Accent, FontScale);

            //剩余时间进度条
            int duration = (int)(eff.Hack.GetDuration() * eff.EffectMult);
            float progress = duration > 0 ? Math.Clamp(1f - (float)eff.Elapsed / duration, 0f, 1f) : 0f;

            //状态文字
            string status = duration > 0 ? HackTime.ActivePct.Format((int)(progress * 100)) : HackTime.ActiveText.Value;
            float statusWidth = FontAssets.MouseText.Value.MeasureString(status).X * (FontScale * 0.85f);
            Utils.DrawBorderString(sb, status, new Vector2(x + CardWidth - statusWidth - 4f, nameY),
                HackTheme.TextDim, FontScale * 0.85f);

            if (duration > 0) {
                float barY = y + CardHeight - BarHeight - 2f;
                float barW = CardWidth - BarMargin * 2;
                //进度条背景
                sb.Draw(px, new Rectangle((int)(x + BarMargin), (int)barY, (int)barW, (int)BarHeight),
                    HackTheme.ProgressBg * 0.9f);
                //剩余时间填充（青色，随时间递减）
                if (progress > 0) {
                    sb.Draw(px, new Rectangle((int)(x + BarMargin), (int)barY,
                        (int)(barW * progress), (int)BarHeight), HackTheme.ProgressFill);
                }
            }

            //边框
            DrawCardBorder(sb, px, x, y, HackTheme.Border * 0.4f);
        }

        //绘制卡片细边框
        private static void DrawCardBorder(SpriteBatch sb, Texture2D px, float x, float y, Color color) {
            int w = (int)CardWidth;
            int h = (int)CardHeight;
            sb.Draw(px, new Rectangle((int)x, (int)y, w, 1), color);          //上
            sb.Draw(px, new Rectangle((int)x, (int)y + h - 1, w, 1), color);  //下
            sb.Draw(px, new Rectangle((int)x, (int)y, 1, h), color);          //左
            sb.Draw(px, new Rectangle((int)x + w - 1, (int)y, 1, h), color);  //右
        }
    }
}
