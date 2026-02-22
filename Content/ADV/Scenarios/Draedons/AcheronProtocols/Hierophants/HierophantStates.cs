using Terraria;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants
{
    /// <summary>
    /// Hierophant 顶层状态接口——每个状态负责完整的一帧逻辑
    /// </summary>
    internal interface IHierophantState
    {
        /// <summary>当进入该状态时调用一次</summary>
        void Enter(Hierophant boss) { }

        /// <summary>每帧执行</summary>
        void Update(Hierophant boss);

        /// <summary>当离开该状态时调用一次</summary>
        void Exit(Hierophant boss) { }
    }

    /// <summary>
    /// 休眠态——Boss 未被激怒，站在原地受重力影响
    /// </summary>
    internal sealed class DormantState : IHierophantState
    {
        public static readonly DormantState Instance = new();

        public void Update(Hierophant boss) {
            NPC npc = boss.NPC;
            npc.boss = false;
            npc.noTileCollide = false;
            npc.velocity.Y += 0.4f;
            if (HierophantUtils.CheckSolidTile(npc.getRect())) {
                npc.velocity.Y = 0f;
            }
        }
    }

    /// <summary>
    /// 战斗态——AI 主循环、移动与攻击全部委托给 <see cref="HierophantCombatController"/>
    /// </summary>
    internal sealed class CombatState : IHierophantState
    {
        public static readonly CombatState Instance = new();

        public void Enter(Hierophant boss) {
            boss.NPC.boss = true;
            boss.NPC.noTileCollide = true;
        }

        public void Update(Hierophant boss) {
            NPC npc = boss.NPC;

            if (!npc.HasValidTarget) npc.TargetClosest();

            if (npc.HasValidTarget && npc.target.ToPlayer().Distance(npc.Center) < 3000f) {
                boss.CombatController.Run(boss, Main.player[npc.target]);
                if (Main.netMode == Terraria.ID.NetmodeID.Server) npc.netUpdate = true;
                boss.DespawnCounter = 0;
            }
            else {
                boss.DespawnCounter++;
                npc.velocity.X += 0.25f;
                npc.velocity.Y += HierophantUtils.CheckSolidTile(npc.getRect()) ? -0.4f : 0.7f;
                if (boss.DespawnCounter > 290) npc.active = false;
            }
        }
    }

    /// <summary>
    /// 死亡演出态——Boss 已被击败，播放震动粒子后自毁
    /// </summary>
    internal sealed class DeathState : IHierophantState
    {
        public static readonly DeathState Instance = new();

        public void Enter(Hierophant boss) {
            boss.NPC.dontTakeDamage = true;
            boss.NPC.damage = 0;
            boss.NPC.boss = true;
            boss.NPC.life = 1;
            boss.Jumping = false;
        }

        public void Update(Hierophant boss) {
            NPC npc = boss.NPC;
            npc.netUpdate = true;
            if (npc.netSpam >= 10) npc.netSpam = 9;

            boss.DeathCounter--;
            npc.velocity *= 0f;
            boss.CombatController.ResetSlash();

            if (!Main.dedServ && Main.GameUpdateCount % 2 == 0) {
                HierophantEffects.CameraShake(npc.Center, 5f);
                HierophantEffects.DeathTickDust(npc);
            }

            if (boss.DeathCounter < 0) {
                if (Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient) {
                    npc.dontTakeDamage = false;
                    npc.StrikeInstantKill();
                    npc.netUpdate = true;
                }
            }

            if (Main.netMode == Terraria.ID.NetmodeID.Server) {
                NetMessage.SendData(Terraria.ID.MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
            }
        }
    }
}
