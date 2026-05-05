using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.Core;
using CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime.States;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace CalamityOverhaul.Content.NPCs.BrutalNPCs.BrutalKingSlime
{
    /// <summary>
    /// 史莱姆王 AI 重做（皇室霸主）
    /// <br/>使用状态机替换原版跳跃 + 召唤蓝史莱姆的循环；新增多种主动技能和皇室描边视觉。
    /// <br/>放大数值（HP / 伤害 / 防御）以匹配重做后的视觉冲击力。
    /// </summary>
    internal class KingSlimeAI : CWRNPCOverride
    {
        public override int TargetID => NPCID.KingSlime;

        private KingSlimeStateMachine stateMachine;
        private KingSlimeStateContext stateContext;
        private Player targetPlayer;

        //保存初始 HP/伤害以便基于其放大
        private int origLifeMax = -1;
        private int origDamage = -1;

        #region 初始化

        public override void SetProperty() {
            //放大基础数值——皇室霸主，更耐打更危险
            //放在 SetProperty 中，先于 AI 执行，多次调用也不会重复放大（已有标记）
            if (origLifeMax < 0) {
                origLifeMax = npc.lifeMax;
                origDamage = npc.damage;

                //HP 提升 65%（专家 / 大师上层会自动叠加），防御 +6
                int newMax = (int)(origLifeMax * 1.65f);
                npc.lifeMax = newMax;
                npc.life = newMax;
                npc.defDefense = npc.defense = npc.defDefense + 6;

                //伤害提升 25%
                npc.damage = (int)(origDamage * 1.25f);
                npc.defDamage = npc.damage;
            }

            InitializeStateContext();
        }

        public override bool? CanCWROverride() => null;

        private void InitializeStateContext() {
            stateContext = new KingSlimeStateContext {
                Npc = npc,
                IsDeathMode = CWRRef.GetDeathMode() || CWRRef.GetBossRushActive()
            };
            stateMachine = new KingSlimeStateMachine(stateContext);

            //客户端加入时根据 npc.ai[2] 还原服务端状态，避免 desync
            if (VaultUtils.isClient) {
                int serverStateIndex = (int)npc.ai[2];
                IKingSlimeState syncedState = KingSlimeStateMachine.CreateStateFromIndex(
                    (KingSlimeStateIndex)serverStateIndex);
                stateMachine.SetInitialState(syncedState ?? new KingSlimeIntroState());
            }
            else {
                stateMachine.SetInitialState(new KingSlimeIntroState());
            }
        }

        #endregion

        #region AI 主循环

        public override bool AI() {
            if (CWRWorld.CanTimeFrozen()) {
                CWRNpc.DoTimeFrozen(npc);
                return false;
            }

            //延迟初始化保护
            if (stateContext == null || stateMachine == null) {
                InitializeStateContext();
            }

            FindTarget();
            UpdateStateContext();
            UpdateScale();

            stateMachine?.Update();

            //每 10 帧强制网络同步
            if (!VaultUtils.isClient && Main.GameUpdateCount % 10 == 0) {
                npc.netUpdate = true;
            }

            //返回 false 跳过原版 AI；引擎仍然会基于 npc.velocity 做位置更新和地形碰撞
            return false;
        }

        private void FindTarget() {
            if (npc.target < 0 || npc.target >= 255 || !targetPlayer.Alives()) {
                npc.TargetClosest();
            }
            targetPlayer = Main.player[npc.target];

            if (!targetPlayer.Alives()) {
                if (!VaultUtils.isClient && stateMachine?.CurrentState is not KingSlimeDespawnState) {
                    stateMachine?.ForceChangeState(new KingSlimeDespawnState());
                }
            }
        }

        private void UpdateStateContext() {
            stateContext.Npc = npc;
            stateContext.Target = targetPlayer;
            stateContext.IsEnraged = npc.life < npc.lifeMax * 0.5f;
            stateContext.IsDeathMode = CWRRef.GetDeathMode() || CWRRef.GetBossRushActive();
        }

        /// <summary>
        /// 模拟原版"血量越低体型越小"的缩放，再叠加 SquishY 提供蹲伏 / 拉伸视觉。
        /// </summary>
        private void UpdateScale() {
            float lifePct = MathHelper.Clamp(npc.life / (float)MathHelper.Max(npc.lifeMax, 1), 0f, 1f);
            //从 1.6（满血）平滑插值到 0.85（残血）
            float baseScale = MathHelper.Lerp(0.85f, 1.6f, lifePct);
            //叠加 SquishY：正值（下蹲）整体偏小一些，负值（拉伸）整体偏大
            float squish = MathHelper.Clamp(stateContext.SquishY, -0.45f, 0.45f);
            //把纵向压扁简单转换为整体缩放变化（uniform scale）
            float visualScale = baseScale * (1f - squish * 0.30f);
            npc.scale = MathHelper.Clamp(visualScale, 0.45f, 2.0f);
        }

        #endregion

        #region 召唤蓝史莱姆——保留但减少频率

        /// <summary>
        /// 史莱姆王在战斗中会持续召唤蓝史莱姆，重做后改为低频召唤以避免数量爆炸。
        /// </summary>
        public override bool? On_PreKill() {
            //死亡时清除剩余的皇冠光柱与冲击波，避免视觉残留
            CleanUpProjectiles();
            return null;
        }

        private static void CleanUpProjectiles() {
            int beam = Terraria.ModLoader.ModContent.ProjectileType<Projectiles.KingSlimeCrownBeamProj>();
            int wave = Terraria.ModLoader.ModContent.ProjectileType<Projectiles.KingSlimeShockwaveProj>();
            foreach (var p in Main.ActiveProjectiles) {
                if (p.type == beam || p.type == wave) {
                    p.Kill();
                }
            }
        }

        #endregion

        #region 视觉描边状态推送

        /// <summary>
        /// 根据当前状态决定皇室光环的视觉模式，统一在 PostDraw 中读取。
        /// </summary>
        private (KingSlimeAuraMode mode, float intensity, float progress) GetAuraVisuals() {
            if (stateContext == null) return (KingSlimeAuraMode.Idle, 0.5f, 0f);

            //砸地下落 / 砸地恢复——白热爆闪
            if (stateMachine?.CurrentState is KingSlimeRoyalSlamFallingState) {
                return (KingSlimeAuraMode.Slamming, 1f, 1f);
            }

            //蓄力中——皇冠琥珀光
            if (stateContext.IsCharging) {
                return (KingSlimeAuraMode.Charging, 0.85f, stateContext.ChargeProgress);
            }

            //暴怒（二阶段）——深紫宝石光
            if (stateContext.IsEnraged) {
                return (KingSlimeAuraMode.Enraged, 0.70f, 0f);
            }

            //常态——温和宝石蓝紫
            return (KingSlimeAuraMode.Idle, 0.45f, 0f);
        }

        #endregion

        #region 绘制

        public override bool? Draw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            //蓄力 / 演出附加特效（蓄力光圈、瞄准线）画在身体之前
            if (stateContext != null) {
                KingSlimeRenderHelper.DrawChargeEffect(spriteBatch, stateContext);
            }

            //外圈描边光晕——确保夜晚远距离也能看清史莱姆王轮廓
            if (npc.alpha < 240) {
                Texture2D npcTex = TextureAssets.Npc[npc.type].Value;
                Rectangle frame = npc.frame;
                Vector2 origin = new Vector2(frame.Width / 2f, frame.Height / 2f);
                Vector2 drawPos = npc.Center - screenPos + new Vector2(0, npc.gfxOffY + 40);
                SpriteEffects effects = npc.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                var (mode, intensity, progress) = GetAuraVisuals();
                KingSlimeRenderHelper.DrawRoyalHalo(spriteBatch, npcTex, drawPos, frame,
                    npc.rotation, origin, new Vector2(npc.scale), effects, mode, intensity, progress);
            }

            //返回 null 让原版正常绘制本体（保留皇冠、眼睛、HP 缩放等原版细节）
            return false;
        }

        public override bool PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            //在原版本体绘制后，叠加一层应用了皇室光环着色器的"虚像"
            //着色器中的边描和内部光纹保持原本贴图细节不变，只增强视觉冲击。
            if (npc.alpha >= 240) return true;

            Texture2D npcTex = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = new Vector2(frame.Width / 2f, frame.Height / 2f);
            Vector2 drawPos = npc.Center - screenPos + new Vector2(0, npc.gfxOffY + 40);
            SpriteEffects effects = npc.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            var (mode, intensity, progress) = GetAuraVisuals();

            bool shaderApplied = KingSlimeRenderHelper.BeginRoyalAuraShader(
                spriteBatch, npcTex, frame, mode, intensity, progress, seed: 0f);

            //半透明叠加，避免破坏原版皇冠细节
            Color overlay = Color.White * 0.85f;
            spriteBatch.Draw(npcTex, drawPos, frame, overlay,
                npc.rotation, origin, npc.scale, effects, 0f);

            if (shaderApplied) {
                KingSlimeRenderHelper.EndRoyalAuraShader(spriteBatch);
            }

            return false;
        }

        #endregion

        #region 防御 / 命中

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers) {
            //避免被自己生成的弹幕误伤
            if (projectile.type == Terraria.ModLoader.ModContent.ProjectileType<Projectiles.KingSlimeShockwaveProj>()
                || projectile.type == Terraria.ModLoader.ModContent.ProjectileType<Projectiles.KingSlimeCrownBeamProj>()
                || projectile.type == Terraria.ModLoader.ModContent.ProjectileType<Projectiles.KingSlimeRoyalGelDropletProj>()) {
                modifiers.FinalDamage *= 0f;
            }
        }

        #endregion
    }
}
