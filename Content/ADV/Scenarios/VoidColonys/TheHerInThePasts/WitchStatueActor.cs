using CalamityOverhaul.Common;
using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TheHerInThePasts
{
    /// <summary>
    /// 过去的她场景中女巫的留影雕像
    /// 召唤出现后即展开硫磺火鬼域，鬼域稳定后由对话场景接管推进对白
    /// 场景结束则像素剥落消散
    /// </summary>
    internal class WitchStatueActor : Actor
    {
        /// <summary>
        /// 演出阶段
        /// </summary>
        public enum PhaseKind
        {
            //鬼域正在从中心向外扩张
            Expanding,
            //鬼域完全展开，等待或处于对话阶段
            Active,
            //像素剥落消散
            Dissolve,
        }

        //每场地图仅会有一尊雕像，保留静态引用便于场景查询
        public static WitchStatueActor Current { get; private set; }

        //可见度淡入步进
        private const float FadeInStep = 0.04f;

        //鬼域扩张时长
        private const int ExpandDuration = 90;
        //像素剥落时长
        private const int DissolveDuration = 180;

        //鬼域展开最大半径，像素
        private const float DomainMaxRadius = 780f;

        /// <summary>可见度0到1</summary>
        private float visibility;

        /// <summary>当前演出阶段</summary>
        public PhaseKind Phase { get; private set; } = PhaseKind.Expanding;

        /// <summary>鬼域是否已完全展开，可供场景触发对话</summary>
        public bool IsDomainReady => Phase == PhaseKind.Active;

        /// <summary>鬼域扩张进度0到1</summary>
        private float domainT;

        /// <summary>像素剥落进度0到1</summary>
        private float dissolveT;

        /// <summary>演出阶段内部计时</summary>
        private int phaseTimer;

        /// <summary>鬼域总时间累计，用于shader的uTime</summary>
        private float domainTimeAccum;

        /// <summary>鬼域展开后启动对话前的等待计时</summary>
        private int triggerDelay;

        /// <summary>是否已经启动过对话，避免重复触发</summary>
        private bool scenarioStarted;

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

            //可见度直接淡入，出场即可见
            if (visibility < 1f) visibility = MathF.Min(1f, visibility + FadeInStep);

            Velocity = Vector2.Zero;
            phaseTimer++;
            domainTimeAccum += 0.016f;

            switch (Phase) {
                case PhaseKind.Expanding:
                    UpdateExpanding();
                    break;
                case PhaseKind.Active:
                    //保持鬼域稳定，短暂延迟后自行启动对话场景
                    domainT = 1f;
                    TryStartScenario();
                    break;
                case PhaseKind.Dissolve:
                    UpdateDissolve();
                    break;
            }
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

        /// <summary>
        /// 鬼域稳定后由Actor自行驱动对话启动
        /// </summary>
        private void TryStartScenario() {
            if (scenarioStarted) return;
            //已完成过的存档不再触发
            //if (Main.LocalPlayer.TryGetADVSave(out var save)
            //    && save.Get<VoidColonyADVData>().TheHerInThePast) {
            //    scenarioStarted = true;
            //    return;
            //}

            if (++triggerDelay < 30) return;

            ScenarioManager.Reset<TheHerInThePast>();
            if (ScenarioManager.Start<TheHerInThePast>()) {
                scenarioStarted = true;
            }
        }

        private void UpdateExpanding() {
            domainT = MathHelper.Clamp(phaseTimer / (float)ExpandDuration, 0f, 1f);
            if (phaseTimer >= ExpandDuration) {
                Phase = PhaseKind.Active;
                phaseTimer = 0;
                domainT = 1f;
            }
        }

        private void UpdateDissolve() {
            dissolveT = MathHelper.Clamp(phaseTimer / (float)DissolveDuration, 0f, 1f);
            //消散过程中鬼域逐步收缩
            domainT = 1f - dissolveT;

            if (phaseTimer >= DissolveDuration) {
                ActorLoader.KillActor(WhoAmI);
                if (Current == this) Current = null;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            if (visibility <= 0.01f) return false;

            //先绘制鬼域光晕，叠加在雕像下面
            if (domainT > 0.01f) {
                WitchGhostDomainDraw.Draw(spriteBatch, Center, domainT, domainTimeAccum, visibility, dissolveT);
            }

            //绘制雕像本体
            DrawStatue(spriteBatch);

            return false;
        }

        private void DrawStatue(SpriteBatch spriteBatch) {
            Texture2D tex = ADVAsset.WitchStatue;
            if (tex == null) return;

            Vector2 drawPos = Center - Main.screenPosition;
            Vector2 origin = new(tex.Width * 0.5f, tex.Height * 0.5f);

            Color baseColor = Color.White * visibility;
            //消散阶段整体再透明化
            if (Phase == PhaseKind.Dissolve) {
                baseColor *= 1f - dissolveT * 0.6f;
            }

            //直接绘制整张贴图
            spriteBatch.Draw(tex, drawPos, null, baseColor, 0f, origin, 1f, SpriteEffects.None, 0f);

            //鬼域展开期间的红光叠加，突出展开气势
            float glowT = Phase == PhaseKind.Dissolve ? 0f : domainT;
            if (glowT > 0.01f) {
                Color glow = new Color(220, 40, 40, 0) * glowT * 0.45f * visibility;
                spriteBatch.Draw(tex, drawPos, null, glow, 0f, origin, 1.02f, SpriteEffects.None, 0f);
            }

            //像素剥落
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
                    //被剥落格子，黑色覆盖
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
    }
}
