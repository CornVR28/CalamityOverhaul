using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants.HierophantUtils;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants
{
    [AutoloadBossHead]
    internal class Hierophant : ModNPC
    {
        #region Fields — Components

        public List<HierophantLeg> Legs;
        public HierophantArm LeftScythe;
        public HierophantArm RightScythe;
        public HierophantCombatController CombatController = new();

        #endregion

        #region Fields — State

        public int Direction = 1;
        public int DeathCounter = 240;
        public bool Defeated;
        public bool Jumping;
        public bool JumpFlag;
        public bool BossActivated = true;
        public int JumpCooldown;
        public int DespawnCounter;
        private IHierophantState _state;

        #endregion

        #region State Machine

        public void TransitionTo(IHierophantState next) {
            _state?.Exit(this);
            _state = next;
            _state.Enter(this);
        }

        #endregion

        #region Static Defaults

        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new() {
                Scale = 0.24f,
                PortraitScale = 0.28f,
                PortraitPositionXOverride = 0,
                PortraitPositionYOverride = -4
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange([
                new FlavorTextBestiaryInfoElement("Mods.CalamityOverhaul.Bestiary.Hierophant")
            ]);
        }

        public override void SetDefaults() {
            NPC.width = 284;
            NPC.height = 264;
            NPC.damage = 28;
            NPC.defense = 8;
            NPC.lifeMax = 3000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = 1600f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.dontCountMe = true;
            NPC.timeLeft *= 12;
            NPC.lavaImmune = true;
            NPC.scale = 1f;

            if (Main.getGoodWorld) NPC.scale += 0.2f;
            if (Main.zenithWorld) NPC.scale += 0.8f;

            NPC.boss = false;
            _state = DormantState.Instance;
        }

        #endregion

        #region Lifecycle

        public override bool CheckActive() => !NPC.boss;

        public override bool CheckDead() {
            if (DeathCounter <= 0) return true;

            Defeated = true;
            NPC.active = true;
            NPC.netUpdate = true;
            NPC.life = 1;
            if (Main.dedServ) {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
            TransitionTo(DeathState.Instance);
            return false;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile) => Defeated ? false : null;
        public override bool? CanBeHitByItem(Player player, Item item) => Defeated ? false : null;
        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => NPC.boss;

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) {
            target.velocity = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * 8f;
            target.AddBuff(BuffID.Electrified, 180);
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) { }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

        public static bool CanStandOn(Vector2 pos) => !IsAir(pos, true);

        #endregion

        #region Segment Initialization

        private void EnsureSegments() {
            if (Legs != null) return;

            // 6 条腿，3 对，交替分组实现蛛形步态
            // 前腿向前伸展，中腿最宽，后腿向后
            Legs = [
                // 前腿（组 0）——略短，向前偏
                new HierophantLeg(NPC, new Vector2(-180, 160), 0.75f, 0.9f, group: 0),
                new HierophantLeg(NPC, new Vector2(180, 160),  0.75f, 0.9f, group: 0),
                // 中腿（组 1）——最大最宽
                new HierophantLeg(NPC, new Vector2(-300, 220), 1f, 1.1f, group: 1),
                new HierophantLeg(NPC, new Vector2(300, 220),  1f, 1.1f, group: 1),
                // 后腿（组 2）——向后偏
                new HierophantLeg(NPC, new Vector2(-240, 300), 0.85f, 1f, group: 2),
                new HierophantLeg(NPC, new Vector2(240, 300),  0.85f, 1f, group: 2),
            ];
            LeftScythe = new HierophantArm(NPC, new Vector2(-160, -64), 152f, MathHelper.PiOver2, MathHelper.PiOver2);
            RightScythe = new HierophantArm(NPC, new Vector2(120, -36), 132f, MathHelper.PiOver2, MathHelper.PiOver2);
        }

        #endregion

        #region AI

        public override void AI() {
            NPC.chaseable = NPC.boss;
            JumpCooldown--;
            EnsureSegments();

            LeftScythe.Update();
            RightScythe.Update();
            UpdateLegs();

            if (NPC.life < 2) Defeated = true;

            if (Defeated && _state is not DeathState) {
                TransitionTo(DeathState.Instance);
            }
            else if (!Defeated) {
                EvaluateTopState();
            }

            _state.Update(this);

            ApplyPhysics();
            UpdateBodyRotation();
        }

        private void EvaluateTopState() {
            bool shouldBeCombat = (float)NPC.life / NPC.lifeMax < 0.98f;

            if (shouldBeCombat && _state is DormantState) {
                if (BossActivated) {
                    BossActivated = false;
                    if (!Main.dedServ) Music = MusicID.OtherworldlyBoss1;
                }
                TransitionTo(CombatState.Instance);
            }
        }

        private void UpdateLegs() {
            // 检测哪些组正在迈步中
            int steppingGroup = -1;
            foreach (var leg in Legs) {
                if (leg.IsMoving) {
                    steppingGroup = leg.Group;
                    break;
                }
            }

            foreach (var leg in Legs) {
                // 如果有其他组正在迈步，阻止本组发起新步
                if (steppingGroup >= 0 && steppingGroup != leg.Group && !leg.IsMoving) {
                    if (leg.NoMoveTime < 6) leg.NoMoveTime = 6;
                }

                bool initiated = leg.Update();

                // 落地震动——巨物感的关键
                if (leg.JustLanded && !Main.dedServ) {
                    HierophantEffects.CameraShake(leg.StandPoint, 3.5f);
                    for (int i = 0; i < 5; i++) {
                        Dust d = Dust.NewDustPerfect(
                            leg.StandPoint + new Vector2(Main.rand.NextFloat(-16f, 16f), 0f),
                            Terraria.ID.DustID.Smoke,
                            new Vector2(Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(-3f, -1f)),
                            Alpha: 120, Scale: Main.rand.NextFloat(1.5f, 2.5f) * NPC.scale);
                        d.noGravity = true;
                    }
                }

                if (initiated) {
                    // 本组迈步时，强制延迟其他组
                    foreach (var otherLeg in Legs) {
                        if (otherLeg.Group != leg.Group && otherLeg.NoMoveTime < 16) {
                            otherLeg.NoMoveTime = 16;
                        }
                    }
                }
            }
        }

        private void ApplyPhysics() {
            if (Jumping) {
                NPC.velocity.Y += 0.35f * NPC.scale;
            }
            else if (_state is not DeathState) {
                NPC.velocity *= 0.96f;
            }
        }

        private void UpdateBodyRotation() {
            if (Jumping) {
                NPC.rotation = (MathF.Abs(NPC.velocity.X * 0.04f).ToRotationVector2() * new Vector2(Direction, 1f)).ToRotation();
                return;
            }

            Vector2 leftAvg = Vector2.Zero, rightAvg = Vector2.Zero;
            int leftCount = 0, rightCount = 0;
            foreach (var leg in Legs) {
                if (!leg.OnTile) continue;
                if (leg.Offset.X < 0) { leftAvg += leg.StandPoint; leftCount++; }
                else { rightAvg += leg.StandPoint; rightCount++; }
            }

            if (leftCount > 0 && rightCount > 0) {
                float r = ((rightAvg / rightCount) - (leftAvg / leftCount)).ToRotation();
                float maxR = MathHelper.ToRadians(60f);
                r = MathHelper.Clamp(r, -maxR, maxR);
                if (Direction < 0) r += MathHelper.Pi;
                NPC.rotation = RotateTowardsAngle(NPC.rotation, r, 0.1f, false);
            }
            else if (leftCount > rightCount) {
                NPC.rotation += 0.1f;
            }
            else if (leftCount < rightCount) {
                NPC.rotation -= 0.1f;
            }
            else {
                float r = Direction == 1 ? 0f : MathHelper.Pi;
                NPC.rotation = RotateTowardsAngle(NPC.rotation, r, 0.3f, false);
            }
        }

        #endregion

        #region Networking

        public override void SendExtraAI(BinaryWriter writer) {
            EnsureSegments();
            writer.Write(NPC.boss);
            LeftScythe.NetSend(writer);
            RightScythe.NetSend(writer);
            writer.Write(Legs.Count > 0);
            if (Legs.Count > 0) {
                foreach (var leg in Legs) leg.NetSend(writer);
            }
            writer.Write(JumpCooldown);
            writer.Write(Jumping);
            CombatController.NetSend(writer);
            writer.Write(Defeated);
            writer.Write(DeathCounter);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            EnsureSegments();
            NPC.boss = reader.ReadBoolean();
            LeftScythe.NetReceive(reader);
            RightScythe.NetReceive(reader);

            if (reader.ReadBoolean()) {
                foreach (var leg in Legs) leg.NetReceive(reader);
            }
            else {
                for (int i = 0; i < 6; i++) {
                    new HierophantLeg(NPC, Vector2.Zero).NetReceive(reader);
                }
            }

            JumpCooldown = reader.ReadInt32();
            Jumping = reader.ReadBoolean();
            CombatController.NetReceive(reader);
            Defeated = reader.ReadBoolean();
            DeathCounter = reader.ReadInt32();
        }

        #endregion

        #region Drawing

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            return HierophantRenderer.Draw(this, spriteBatch, screenPos, drawColor);
        }

        #endregion

        #region Hit & Death Effects

        public override void HitEffect(NPC.HitInfo hit) {
            if (DeathCounter > 0 || NPC.life > 0 || Main.dedServ) return;
            HierophantEffects.CameraShake(NPC.Center, 100f);
            HierophantEffects.DeathExplosionDust(NPC);
        }

        public override void OnKill() => HierophantEffects.OnKillEffects(NPC);

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.Common(ItemID.HallowedBar, 1, 16, 24));
            npcLoot.Add(ItemDropRule.Common(ItemID.SoulofMight, 1, 10, 20));
        }

        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot,
            ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox) => false;

        #endregion
    }
}
