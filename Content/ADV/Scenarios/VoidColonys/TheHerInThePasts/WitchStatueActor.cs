using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.GlitchWraith;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TimeShift;
using CalamityOverhaul.Content.Items.Melee;
using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TheHerInThePasts
{
    /// <summary>
    /// 过去的她场景中女巫的留影雕像
    /// 平时仅过去时代可见，呈灰白色雕像状静止
    /// 演出激活后推动抬头、展开鬼域、召唤鬼手镇压鬼乱码、像素剥落等时间轴
    /// </summary>
    internal class WitchStatueActor : Actor
    {
        /// <summary>
        /// 演出阶段
        /// </summary>
        public enum PhaseKind
        {
            //完全静止的灰白雕像
            Idle,
            //抬头并展开鬼域
            Awaken,
            //维持鬼域镇压鬼乱码，推动对话
            Suppress,
            //像素剥落消散
            Dissolve,
        }

        //每场地图仅会有一尊雕像，保留静态引用便于场景查询
        public static WitchStatueActor Current { get; private set; }

        //可见度淡入淡出步进，与建筑Actor一致
        private const float FadeInStep = 0.025f;
        private const float FadeOutStep = 0.08f;

        //演出时长常量
        private const int AwakenDuration = 90;
        private const int DissolveDuration = 180;

        //鬼域展开最大半径，像素
        private const float DomainMaxRadius = 780f;

        //鬼手数量上限，避免过多
        private const int MaxHands = 6;

        /// <summary>过去时代下的可见度，0到1</summary>
        private float visibility;

        /// <summary>当前演出阶段</summary>
        public PhaseKind Phase { get; private set; } = PhaseKind.Idle;

        /// <summary>抬头进度0到1</summary>
        private float awakenT;

        /// <summary>鬼域扩张进度0到1</summary>
        private float domainT;

        /// <summary>像素剥落进度0到1</summary>
        private float dissolveT;

        /// <summary>演出阶段内部计时</summary>
        private int phaseTimer;

        /// <summary>鬼域总时间累计，用于shader的uTime</summary>
        private float domainTimeAccum;

        /// <summary>鬼手列表</summary>
        private readonly List<GhostHand> ghostHands = [];

        /// <summary>
        /// 从地面升起抓住鬼乱码的鬼手
        /// </summary>
        private class GhostHand
        {
            //目标鬼乱码的WhoAmI
            public int TargetIndex;
            //手掌位置
            public Vector2 HandPos;
            //从地面破出的起点
            public Vector2 AnchorPos;
            //手臂骨骼节点，HandPos在末端
            public Vector2[] Segments;
            //生命计时
            public int Life;
            //破土进度0到1
            public float RiseT;
            //朝向翻转，-1或1
            public int Direction;
            //随机相位，避免手掌运动同步
            public float Phase;
        }

        public new Vector2 Center {
            get => Position + new Vector2(Width * 0.5f, Height * 0.5f);
            set => Position = value - new Vector2(Width * 0.5f, Height * 0.5f);
        }

        public override void OnSpawn(params object[] args) {
            Texture2D tex = ADVAsset.WitchStatue;
            Width = tex?.Width ?? 120;
            Height = tex?.Height ?? 260;
            DrawLayer = ActorDrawLayer.AfterTiles;
            DrawExtendMode = (int)(DomainMaxRadius * 1.4f);
            visibility = 0f;
            Current = this;
        }

        public override void AI() {
            //维持单例引用，切地图或重建actor后新的实例接管
            if (Current != this) {
                Current = this;
            }

            if (!VoidColony.Active) {
                visibility = MathF.Max(0f, visibility - FadeOutStep);
                if (visibility <= 0f) {
                    ActorLoader.KillActor(WhoAmI);
                }
                Velocity = Vector2.Zero;
                return;
            }

            //可见度随时代切换
            float targetVis = VoidTimeShiftSystem.InPast ? 1f : 0f;
            if (visibility < targetVis) visibility = MathF.Min(targetVis, visibility + FadeInStep);
            else if (visibility > targetVis) visibility = MathF.Max(targetVis, visibility - FadeOutStep);

            Velocity = Vector2.Zero;
            phaseTimer++;
            domainTimeAccum += 0.016f;

            switch (Phase) {
                case PhaseKind.Idle:
                    //雕像完全静止
                    break;
                case PhaseKind.Awaken:
                    UpdateAwaken();
                    break;
                case PhaseKind.Suppress:
                    UpdateSuppress();
                    break;
                case PhaseKind.Dissolve:
                    UpdateDissolve();
                    break;
            }
        }

        /// <summary>
        /// 由场景调用，进入抬头+展开鬼域阶段
        /// </summary>
        public void BeginPerformance() {
            if (Phase != PhaseKind.Idle) return;
            Phase = PhaseKind.Awaken;
            phaseTimer = 0;
            awakenT = 0f;
            domainT = 0f;
            ghostHands.Clear();
        }

        /// <summary>
        /// 由场景调用，进入像素剥落消散阶段
        /// </summary>
        public void BeginDissolve() {
            if (Phase == PhaseKind.Dissolve) return;
            Phase = PhaseKind.Dissolve;
            phaseTimer = 0;
            dissolveT = 0f;
        }

        private void UpdateAwaken() {
            //抬头与鬼域扩张并行推进
            awakenT = MathHelper.Clamp(phaseTimer / (float)AwakenDuration, 0f, 1f);
            domainT = MathHelper.Clamp(phaseTimer / (float)(AwakenDuration * 1.2f), 0f, 1f);

            //鬼域扩张到一半时开始召唤鬼手
            if (domainT > 0.4f && ghostHands.Count < MaxHands) {
                TrySpawnHands();
            }

            if (phaseTimer >= AwakenDuration) {
                Phase = PhaseKind.Suppress;
                phaseTimer = 0;
                domainT = 1f;
                awakenT = 1f;
            }
        }

        private void UpdateSuppress() {
            //维持鬼域与鬼手，持续对所有乱码施加死机+虚假记忆
            var wraiths = ActorLoader.GetActiveActors<GlitchWraithActor>();
            foreach (var wraith in wraiths) {
                if (wraith == null) continue;
                //压制时长每帧续上一点，防止死机时间耗尽
                wraith.ApplySystemHalt(10);
                wraith.ApplyFalseMemory(20);
            }

            //鬼手不足且有可用目标，继续补刷
            if (ghostHands.Count < MaxHands) {
                TrySpawnHands();
            }

            UpdateGhostHands();
        }

        private void UpdateDissolve() {
            dissolveT = MathHelper.Clamp(phaseTimer / (float)DissolveDuration, 0f, 1f);
            //消散过程中鬼域逐步收缩
            domainT = 1f - dissolveT;
            awakenT = 1f - dissolveT * 0.5f;

            //消散期间让所有鬼手也同步收回
            for (int i = ghostHands.Count - 1; i >= 0; i--) {
                ghostHands[i].RiseT = MathF.Max(0f, ghostHands[i].RiseT - 0.02f);
                if (ghostHands[i].RiseT <= 0f) {
                    ghostHands.RemoveAt(i);
                }
            }

            if (phaseTimer >= DissolveDuration) {
                //演出终结，让乱码彻底自毁
                var wraiths = ActorLoader.GetActiveActors<GlitchWraithActor>();
                foreach (var wraith in wraiths) {
                    wraith?.ApplySelfDismember();
                }
                ActorLoader.KillActor(WhoAmI);
                if (Current == this) Current = null;
            }
        }

        /// <summary>
        /// 尝试为可见的鬼乱码生成鬼手
        /// </summary>
        private void TrySpawnHands() {
            var wraiths = ActorLoader.GetActiveActors<GlitchWraithActor>();
            foreach (var wraith in wraiths) {
                if (wraith == null) continue;
                //每只乱码最多绑两条鬼手
                int existing = 0;
                for (int i = 0; i < ghostHands.Count; i++) {
                    if (ghostHands[i].TargetIndex == wraith.WhoAmI) existing++;
                }
                if (existing >= 2) continue;
                if (ghostHands.Count >= MaxHands) break;
                ghostHands.Add(CreateHandFor(wraith));
            }
        }

        private GhostHand CreateHandFor(GlitchWraithActor wraith) {
            Vector2 target = wraith.Center;
            //从目标正下方60~160像素处破土，左右随机偏移
            float offsetX = Main.rand.NextFloat(-80f, 80f);
            float offsetY = Main.rand.NextFloat(40f, 120f);
            Vector2 anchor = target + new Vector2(offsetX, offsetY);
            int dir = offsetX >= 0 ? 1 : -1;

            //手臂由4节组成，rise完成后末端指向目标中心
            Vector2[] segs = new Vector2[5];
            for (int i = 0; i < segs.Length; i++) segs[i] = anchor;

            return new GhostHand {
                TargetIndex = wraith.WhoAmI,
                AnchorPos = anchor,
                HandPos = anchor,
                Segments = segs,
                Life = 0,
                RiseT = 0f,
                Direction = dir,
                Phase = Main.rand.NextFloat(MathHelper.TwoPi),
            };
        }

        private void UpdateGhostHands() {
            for (int i = ghostHands.Count - 1; i >= 0; i--) {
                var hand = ghostHands[i];
                hand.Life++;
                hand.RiseT = MathF.Min(1f, hand.RiseT + 0.04f);

                //找不到目标就回收
                if (hand.TargetIndex < 0 || hand.TargetIndex >= ActorLoader.Actors.Length) {
                    ghostHands.RemoveAt(i);
                    continue;
                }
                if (ActorLoader.Actors[hand.TargetIndex] is not GlitchWraithActor wraith
                    || !wraith.Active) {
                    ghostHands.RemoveAt(i);
                    continue;
                }

                //手掌目标为鬼乱码躯干中部稍上，带一点随机抖动
                float shake = MathF.Sin(hand.Life * 0.3f + hand.Phase) * 4f;
                Vector2 targetHand = wraith.Center + new Vector2(shake, -wraith.Height * 0.15f);
                //RiseT控制手从地面升起到抓住目标的插值
                hand.HandPos = Vector2.Lerp(hand.AnchorPos, targetHand, hand.RiseT);

                //手臂分节点简单线性分布，略带波浪
                for (int j = 0; j < hand.Segments.Length; j++) {
                    float t = j / (float)(hand.Segments.Length - 1);
                    Vector2 baseLine = Vector2.Lerp(hand.AnchorPos, hand.HandPos, t);
                    float wave = MathF.Sin(t * MathHelper.Pi * 1.2f + hand.Life * 0.12f + hand.Phase) * 8f * hand.RiseT;
                    Vector2 perp = new(-(hand.HandPos - hand.AnchorPos).Y, (hand.HandPos - hand.AnchorPos).X);
                    perp = perp.SafeNormalize(Vector2.UnitX);
                    hand.Segments[j] = baseLine + perp * wave * hand.Direction;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            if (visibility <= 0.01f) return false;

            //先绘制鬼域光晕，叠加在雕像下面
            if (domainT > 0.01f) {
                WitchGhostDomainDraw.Draw(spriteBatch, Center, domainT, domainTimeAccum, visibility);
            }

            //绘制雕像本体
            DrawStatue(spriteBatch);

            //绘制鬼手
            if (ghostHands.Count > 0) {
                DrawGhostHands(spriteBatch);
            }

            return false;
        }

        private void DrawStatue(SpriteBatch spriteBatch) {
            Texture2D tex = ADVAsset.WitchStatue;
            if (tex == null) return;

            Vector2 drawPos = Center - Main.screenPosition;
            Vector2 origin = new(tex.Width * 0.5f, tex.Height * 0.5f);

            //抬头之前呈灰白雕像，抬头进度越高越接近原色
            Color stone = new(180, 175, 170);
            Color vivid = Color.White;
            Color baseColor = Color.Lerp(stone, vivid, awakenT) * visibility;

            //消散阶段整体再透明化
            if (Phase == PhaseKind.Dissolve) {
                baseColor *= 1f - dissolveT * 0.6f;
            }

            //直接绘制整张贴图
            spriteBatch.Draw(tex, drawPos, null, baseColor, 0f, origin, 1f, SpriteEffects.None, 0f);

            //抬头过程中轻微上浮+红光叠加
            if (awakenT > 0.01f) {
                Color glow = new Color(220, 40, 40, 0) * awakenT * 0.45f * visibility;
                spriteBatch.Draw(tex, drawPos, null, glow, 0f, origin, 1.02f, SpriteEffects.None, 0f);
            }

            //像素剥落：从上至下随机格子被黑色覆盖+向上飞散的小方块
            if (Phase == PhaseKind.Dissolve && dissolveT > 0.02f) {
                DrawPixelDissolve(spriteBatch, drawPos, origin, tex);
            }
        }

        private void DrawPixelDissolve(SpriteBatch spriteBatch, Vector2 drawPos, Vector2 origin, Texture2D tex) {
            Texture2D pixel = CWRAsset.Placeholder_White?.Value ?? TextureAssets.MagicPixel.Value;
            if (pixel == null) return;

            int blockSize = 6;
            int cols = tex.Width / blockSize;
            int rows = tex.Height / blockSize;
            //当前该被剥落覆盖的百分比，从上往下推进
            float topThreshold = dissolveT;
            //随机种子固定以保证每帧一致
            int seed = WhoAmI * 7919;

            for (int y = 0; y < rows; y++) {
                float rowProgress = y / (float)rows;
                //下面的格子更晚剥落
                float localT = MathHelper.Clamp((topThreshold - rowProgress) * 2f, 0f, 1f);
                if (localT <= 0f) continue;
                for (int x = 0; x < cols; x++) {
                    //每个格子用确定性随机判定剥落
                    int hash = unchecked(seed + x * 73856093 + y * 19349663);
                    float rnd = (hash & 0xFFFF) / 65535f;
                    if (rnd > localT) continue;

                    Vector2 pos = drawPos - origin + new Vector2(x * blockSize, y * blockSize);
                    //被剥落格子：黑色覆盖
                    Color cover = Color.Black * visibility * MathHelper.Clamp(localT * 1.3f, 0f, 1f);
                    spriteBatch.Draw(pixel, pos, null, cover, 0f, Vector2.Zero, blockSize, SpriteEffects.None, 0f);

                    //向上飞散的像素残片
                    if (rnd < localT * 0.4f) {
                        float driftY = -MathF.Sin(phaseTimer * 0.03f + x * 0.4f) * 16f * localT;
                        float driftX = MathF.Cos(phaseTimer * 0.02f + y * 0.3f) * 4f * localT;
                        Vector2 shardPos = pos + new Vector2(driftX, driftY - localT * 30f);
                        Color shardColor = Color.Lerp(new Color(180, 175, 170), new Color(80, 30, 30), localT) * visibility * (1f - localT);
                        spriteBatch.Draw(pixel, shardPos, null, shardColor, 0f, Vector2.Zero, blockSize * 0.8f, SpriteEffects.None, 0f);
                    }
                }
            }
        }

        private void DrawGhostHands(SpriteBatch spriteBatch) {
            Texture2D arm = OniMachete.OniArm;
            Texture2D hand = OniMachete.OniHand;
            if (arm == null || hand == null) return;

            foreach (var ghostHand in ghostHands) {
                if (ghostHand.RiseT <= 0.01f) continue;
                Color baseColor = Color.Lerp(new Color(80, 10, 10), Color.White, 0.2f) * visibility * ghostHand.RiseT;

                //沿segments铺设arm贴图作为链
                for (int i = 0; i < ghostHand.Segments.Length - 1; i++) {
                    Vector2 start = ghostHand.Segments[i];
                    Vector2 end = ghostHand.Segments[i + 1];
                    Vector2 diff = end - start;
                    float length = diff.Length();
                    if (length < 1f) continue;
                    float rotation = diff.ToRotation() - MathHelper.ToRadians(80) * ghostHand.Direction;

                    int boneCount = Math.Max(1, (int)(length / arm.Height));
                    for (int j = 0; j < boneCount; j++) {
                        float progress = j / (float)boneCount;
                        Vector2 bonePos = Vector2.Lerp(start, end, progress);
                        float boneScale = MathHelper.Lerp(0.55f, 0.75f, MathF.Sin(progress * MathHelper.Pi));

                        spriteBatch.Draw(
                            arm,
                            bonePos - Main.screenPosition,
                            null,
                            baseColor,
                            rotation + MathF.Sin(domainTimeAccum * 2f + i + j + ghostHand.Phase) * 0.08f,
                            arm.Size() / 2f,
                            boneScale * 1.8f,
                            SpriteEffects.None,
                            0);

                        //硫磺火辉光层
                        Color glowColor = new Color(255, 80, 40, 0) * visibility * ghostHand.RiseT * 0.5f;
                        spriteBatch.Draw(
                            arm,
                            bonePos - Main.screenPosition,
                            null,
                            glowColor,
                            rotation + MathF.Sin(domainTimeAccum * 2f + i + j + ghostHand.Phase) * 0.08f,
                            arm.Size() / 2f,
                            boneScale * 1.95f,
                            SpriteEffects.None,
                            0);
                    }
                }

                //末端手掌
                Vector2 handDrawPos = ghostHand.HandPos - Main.screenPosition;
                Vector2 forward = ghostHand.Segments[^1] - ghostHand.Segments[^2];
                float handRot = forward.ToRotation() + MathHelper.Pi;

                spriteBatch.Draw(
                    hand,
                    handDrawPos,
                    null,
                    baseColor,
                    handRot,
                    hand.Size() / 2f,
                    1f,
                    ghostHand.Direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    0);

                //辉光层
                Color handGlow = new Color(255, 100, 50, 0) * visibility * ghostHand.RiseT * 0.6f;
                spriteBatch.Draw(
                    hand,
                    handDrawPos,
                    null,
                    handGlow,
                    handRot,
                    hand.Size() / 2f,
                    1.12f,
                    ghostHand.Direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    0);
            }
        }
    }
}
