using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants.HierophantUtils;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants
{
    [AutoloadBossHead]
    internal class Hierophant : ModNPC
    {
        #region Inner Types

        public class HierophantLeg
        {
            public Vector2 StandPoint;
            public float Scale = 1f;
            public NPC NPC;
            public Vector2 Offset;
            public int NoMoveTime;
            public Vector2 TargetPos;
            public float MoveSpeed;
            private bool _onTileFlag;

            public bool OnTile => !IsAir(StandPoint, true) && _onTileFlag;

            public HierophantLeg(NPC npc, Vector2 offset, float scale = 1f) {
                NPC = npc;
                Offset = offset;
                Scale = scale;
                StandPoint = npc.Center;
            }

            public void NetSend(BinaryWriter writer) {
                writer.Write(NoMoveTime);
                writer.WriteVector2(TargetPos);
                writer.WriteVector2(StandPoint);
            }

            public void NetReceive(BinaryReader reader) {
                NoMoveTime = reader.ReadInt32();
                TargetPos = reader.ReadVector2();
                StandPoint = reader.ReadVector2();
            }

            public bool Update() {
                float speedMult = NPC.velocity.Y > 1f ? 3f : 1f;
                if (Distance(StandPoint, TargetPos) < MoveSpeed * speedMult) {
                    StandPoint = TargetPos;
                }
                else {
                    StandPoint += (TargetPos - StandPoint).SafeNormalize() * MoveSpeed * (NPC.velocity.Y > 0.5f ? 3f : 1f);
                }

                NoMoveTime--;
                float distToMove = 100f * NPC.scale;
                var machine = (Hierophant)NPC.ModNPC;

                if (machine.Jumping) {
                    _onTileFlag = false;
                    TargetPos = NPC.Center + new Vector2(Offset.X * 0.2f, 200f) * NPC.scale;
                    MoveSpeed = Distance(TargetPos, StandPoint) * 0.2f;
                    return false;
                }

                float bodyRot = machine.Direction > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi);
                Vector2 idealPos = NPC.Center + NPC.velocity * 16f + (Offset * NPC.scale).RotatedBy(bodyRot);
                float dist = Distance(StandPoint, idealPos);

                if (!OnTile
                    || (NoMoveTime <= 0 && dist > distToMove)
                    || dist > distToMove * 1.4f) {
                    Vector2 searchCenter = idealPos + new Vector2(
                        Math.Sign(NPC.velocity.X) == Math.Sign(Offset.X) ? Math.Sign(NPC.velocity.X) * 12f : 0f, 0f);
                    TargetPos = FindStandPoint(searchCenter, 60f * Scale * NPC.scale, 128);
                    MoveSpeed = Distance(TargetPos, StandPoint) * 0.2f;
                    if (NoMoveTime < 4) NoMoveTime = 4;
                    return true;
                }

                return false;
            }

            public Vector2 FindStandPoint(Vector2 center, float maxOffset, float maxTry = 64) {
                _onTileFlag = false;
                var machine = (Hierophant)NPC.ModNPC;

                for (int i = 0; i < (int)maxTry; i++) {
                    Vector2 pos = RandomPointInCircle(maxTry) + center;
                    if (Distance(pos, center) <= maxOffset * 0.9f && CanStandOn(pos)) {
                        _onTileFlag = true;
                        Vector2 orgPos = pos;
                        int safety = 128;
                        while (CanStandOn(pos)) {
                            safety--;
                            pos.Y -= 2f * NPC.scale;
                            if (safety <= 0) return orgPos;
                        }
                        pos.Y += 2f * NPC.scale;
                        return pos;
                    }
                }

                float rot = machine.Direction > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi);
                return NPC.Center + new Vector2(Offset.X, 200f).RotatedBy(rot);
            }
        }

        public class HierophantArm
        {
            public float Seg1RotVelocity;
            public float Seg1Length;
            public float Seg1Rot;
            public float Seg2Rot;
            public float Seg1MaxRadians = MathHelper.ToRadians(50f);
            public Vector2 Offset;
            public NPC Npc;

            public Vector2 TopPos => Seg1End + Seg2Rot.ToRotationVector2() * 60f * Npc.scale;

            public Vector2 Seg1End {
                get {
                    var machine = (Hierophant)Npc.ModNPC;
                    float rot = machine.Direction > 0 ? Npc.rotation : (Npc.rotation + MathHelper.Pi);
                    return Npc.Center + (Offset * new Vector2(machine.Direction, 1f) * Npc.scale).RotatedBy(rot)
                           + Seg1Rot.ToRotationVector2() * Seg1Length;
                }
            }

            public HierophantArm(NPC npc, Vector2 offset, float seg1Length, float seg1Rot, float seg2Rot) {
                Npc = npc;
                Offset = offset;
                Seg1Length = seg1Length;
                Seg1Rot = seg1Rot;
                Seg2Rot = seg2Rot;
            }

            public void PointAt(Vector2 pos) {
                var machine = (Hierophant)Npc.ModNPC;
                float rot = machine.Direction > 0 ? Npc.rotation : (Npc.rotation + MathHelper.Pi);
                Vector2 basePos = Npc.Center + (Offset * new Vector2(machine.Direction, 1f)).RotatedBy(rot);

                Seg1Rot = RotateTowardsAngle(Seg1Rot, (pos - basePos).ToRotation(), 0.06f, false);

                if (GetAngleBetweenVectors(Seg1Rot.ToRotationVector2(), -Vector2.UnitY) > Seg1MaxRadians * 2f) {
                    float upper = MathHelper.PiOver2 + Seg1MaxRadians;
                    float lower = MathHelper.PiOver2 - Seg1MaxRadians;
                    if (Seg1Rot > upper) Seg1Rot = upper;
                    if (Seg1Rot < lower) Seg1Rot = lower;
                }

                Seg2Rot = RotateTowardsAngle(Seg2Rot, (pos - Seg1End).ToRotation(), 0.06f, false);
            }

            public void Update() {
                Seg1Rot += Seg1RotVelocity;
                Seg1RotVelocity *= 0.96f;
            }

            public void NetSend(BinaryWriter writer) {
                writer.Write(Seg1Rot);
                writer.Write(Seg2Rot);
                writer.Write(Seg1RotVelocity);
            }

            public void NetReceive(BinaryReader reader) {
                Seg1Rot = reader.ReadSingle();
                Seg2Rot = reader.ReadSingle();
                Seg1RotVelocity = reader.ReadSingle();
            }
        }

        #endregion

        #region Fields

        public List<HierophantLeg> Legs;
        public HierophantArm Cannon;
        public HierophantArm Harpoon;
        public int Direction = 1;
        public int DeathCounter = 240;
        public bool Defeated;
        public bool Jumping;
        public bool JumpFlag;
        public bool BossActivated = true;
        public int JumpCooldown;
        public int JumpAndShootTimer;
        public int CannonUpAttackTimer;
        public float TeslaCooldown = 120f;
        public float TeslaUpCooldown;
        public float HarpoonCharge;
        public float HarpoonCooldown = 120f;
        public int HarpoonNpcIndex = -1;
        private int _despawnCounter;

        #endregion

        #region Properties

        public bool IsHarpoonOnLauncher {
            get {
                NPC harpoonNpc = HarpoonNpcIndex.ToNPC();
                return harpoonNpc?.ModNPC is HierophantHarpoon h && h.OnLauncher;
            }
        }

        public Vector2 HarpoonTipPos =>
            Harpoon.Seg1End + Harpoon.Seg2Rot.ToRotationVector2() * 150f * NPC.scale
            + new Vector2(0f, 10f * Direction).RotatedBy(Harpoon.Seg2Rot) * NPC.scale;

        #endregion

        #region Static Defaults

        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers value = new() {
                Scale = 0.48f,
                PortraitScale = 0.56f,
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
            NPC.width = 142;
            NPC.height = 132;
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
        }

        #endregion

        #region Lifecycle

        public override bool CheckActive() => !NPC.boss;

        public override bool CheckDead() {
            if (DeathCounter <= 0) return true;

            Defeated = true;
            NPC.dontTakeDamage = true;
            NPC.active = true;
            NPC.netUpdate = true;
            NPC.damage = 0;
            NPC.boss = true;
            NPC.life = 1;
            if (Main.dedServ) {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
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

        #endregion

        #region Segment Initialization

        private void EnsureSegments() {
            if (Legs != null) return;

            Legs = [
                new HierophantLeg(NPC, new Vector2(-100, 120), 0.8f),
                new HierophantLeg(NPC, new Vector2(100, 120), 0.8f),
                new HierophantLeg(NPC, new Vector2(-140, 120), 1f),
                new HierophantLeg(NPC, new Vector2(140, 120), 1f),
            ];
            Cannon = new HierophantArm(NPC, new Vector2(-80, -32), 76f, MathHelper.PiOver2, MathHelper.PiOver2);
            Harpoon = new HierophantArm(NPC, new Vector2(60, -18), 66f, MathHelper.PiOver2, MathHelper.PiOver2);
        }

        public static bool CanStandOn(Vector2 pos) => !IsAir(pos, true);

        #endregion

        #region AI

        public override void AI() {
            NPC.chaseable = NPC.boss;
            JumpCooldown--;
            EnsureSegments();
            Cannon.Update();
            Harpoon.Update();

            if (HarpoonNpcIndex == -1 && Main.netMode != NetmodeID.MultiplayerClient) {
                HarpoonNpcIndex = NPC.NewNPC(NPC.GetSource_FromAI(), 0, 0, ModContent.NPCType<HierophantHarpoon>(), 0, NPC.whoAmI);
                NPC harpoonNpc = HarpoonNpcIndex.ToNPC();
                if (harpoonNpc != null) {
                    harpoonNpc.Center = HarpoonTipPos;
                    harpoonNpc.netSpam = 9;
                    harpoonNpc.netUpdate = true;
                }
            }

            foreach (var leg in Legs) {
                if (leg.Update()) {
                    foreach (var otherLeg in Legs) {
                        if (Math.Sign(otherLeg.Offset.X) == Math.Sign(leg.Offset.X) && otherLeg.NoMoveTime < 8) {
                            otherLeg.NoMoveTime = 8;
                        }
                    }
                }
            }

            if (NPC.life < 2) Defeated = true;

            if (Defeated) {
                RunDeathSequence();
                return;
            }

            if (!NPC.HasValidTarget) NPC.TargetClosest();

            if ((float)NPC.life / NPC.lifeMax < 0.98f) {
                if (BossActivated) {
                    BossActivated = false;
                    if (!Main.dedServ) {
                        Music = MusicID.OtherworldlyBoss1;
                    }
                    NPC.boss = true;
                }

                NPC.noTileCollide = true;

                if (NPC.HasValidTarget && NPC.target.ToPlayer().Distance(NPC.Center) < 3000f) {
                    AttackPlayer(Main.player[NPC.target]);
                    if (Main.netMode == NetmodeID.Server) NPC.netUpdate = true;
                    _despawnCounter = 0;
                }
                else {
                    _despawnCounter++;
                    NPC.velocity.X += 0.25f;
                    NPC.velocity.Y += CheckSolidTile(NPC.getRect()) ? -0.4f : 0.7f;
                    if (_despawnCounter > 290) NPC.active = false;
                }
            }
            else {
                NPC.boss = false;
                NPC.noTileCollide = false;
                NPC.velocity.Y += 0.4f;
                if (CheckSolidTile(NPC.getRect())) NPC.velocity.Y = 0f;
            }

            if (Jumping) {
                NPC.velocity.Y += 0.4f * NPC.scale;
            }
            else {
                NPC.velocity *= 0.97f;
            }

            UpdateBodyRotation();
        }

        private void RunDeathSequence() {
            NPC.netUpdate = true;
            if (NPC.netSpam >= 10) NPC.netSpam = 9;

            int decrement = 1;
            if (Main.zenithWorld) decrement = 1;
            DeathCounter -= decrement;

            NPC.velocity *= 0f;
            Jumping = false;
            CannonUpAttackTimer = -1;
            JumpAndShootTimer = -1;

            if (!Main.dedServ && Main.GameUpdateCount % 2 == 0) {
                AddCameraShake(NPC.Center, 5f);
                for (int i = 0; i < 3; i++) {
                    Dust d = Dust.NewDustPerfect(NPC.Center + RandomPointInCircle(40f * NPC.scale),
                        DustID.Electric, RandomPointInCircle(8f), Scale: Main.rand.NextFloat(1f, 2f));
                    d.noGravity = true;
                }
            }

            if (DeathCounter < 0) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    NPC.dontTakeDamage = false;
                    NPC.StrikeInstantKill();
                    NPC.netUpdate = true;
                }
            }

            if (Main.netMode == NetmodeID.Server) {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
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

        #region Attack Logic

        public void AttackPlayer(Player player) {
            float enrage = 1f + (1f - (float)NPC.life / NPC.lifeMax);
            if (Main.expertMode) enrage += 0.07f;
            if (Main.masterMode) enrage += 0.07f;
            if (Main.getGoodWorld) enrage *= 1.1f;
            if (Main.zenithWorld) enrage *= 1.15f;

            float distToPlayer = Distance(player.Center, NPC.Center);

            UpdateCannonAI(player, enrage, distToPlayer);
            UpdateHarpoonAI(player, enrage);
            UpdateTeslaAttacks(player, enrage);
            UpdateMovement(player, enrage);
            UpdateDirection();
            UpdatePullLogic();
        }

        private void UpdateCannonAI(Player player, float enrage, float distToPlayer) {
            if (CannonUpAttackTimer-- > 0) {
                Cannon.PointAt(player.Center + new Vector2(0f, -800f));
                if (CannonUpAttackTimer < 140) {
                    TeslaUpCooldown -= enrage;
                    if (TeslaUpCooldown <= 0f) {
                        TeslaUpCooldown = 12f;
                        ShootTeslaBall(Cannon.TopPos, Cannon.Seg2Rot.ToRotationVector2().RotatedByRandom(0.6f) * 5f);
                        SoundEngine.PlaySound(SoundID.Item93, Cannon.TopPos);
                        Cannon.Seg1RotVelocity = 0.06f * Direction;
                    }
                }
            }
            else if (JumpAndShootTimer-- > 0) {
                Cannon.PointAt(player.Center + new Vector2(0f, -30f));
                TeslaUpCooldown -= enrage;
                if (TeslaUpCooldown <= 0f) {
                    TeslaUpCooldown = 12f;
                    ShootTeslaBall(Cannon.TopPos, Cannon.Seg2Rot.ToRotationVector2().RotatedByRandom(0.12f) * 5f);
                    SoundEngine.PlaySound(SoundID.Item93, Cannon.TopPos);
                }
            }
            else {
                float yOffset = distToPlayer > 500f ? -(((distToPlayer - 500f) * 0.02f) * ((distToPlayer - 500f) * 0.02f)) : 0f;
                Cannon.PointAt(player.Center + new Vector2(0f, -14f + yOffset));
            }

            if (!Jumping) JumpAndShootTimer = -1;
        }

        private void UpdateHarpoonAI(Player player, float enrage) {
            if (HarpoonCharge <= 0.8f && IsHarpoonOnLauncher) {
                Harpoon.PointAt(player.Center);
            }
            else if (!IsHarpoonOnLauncher) {
                NPC harpoonNpc = HarpoonNpcIndex.ToNPC();
                if (harpoonNpc != null) {
                    Harpoon.PointAt(harpoonNpc.Center);
                }
            }

            if (IsHarpoonOnLauncher) HarpoonCooldown -= enrage;

            if (HarpoonCooldown <= 0f) {
                HarpoonCharge += 0.01f * enrage;
                if (HarpoonCharge >= 1f) {
                    HarpoonCharge = 0f;
                    HarpoonCooldown = 160f;
                    NPC harpoonNpc = HarpoonNpcIndex.ToNPC();
                    if (harpoonNpc?.ModNPC is HierophantHarpoon hp) {
                        hp.BackTimer = 40;
                        hp.OnLauncher = false;
                        harpoonNpc.velocity = Harpoon.Seg2Rot.ToRotationVector2() * 36f * NPC.scale;
                        Harpoon.Seg1RotVelocity = 0.3f * Direction;
                        SoundEngine.PlaySound(SoundID.Item10, NPC.Center);
                    }
                }
            }
        }

        private void UpdateTeslaAttacks(Player player, float enrage) {
            TeslaCooldown -= enrage;
            if (TeslaCooldown > 0f) return;

            if (Main.rand.NextBool(8)) {
                TeslaCooldown = 360f;
                CannonUpAttackTimer = 200;
            }
            else if (Main.rand.NextBool(8) && !Jumping && IsHarpoonOnLauncher) {
                Jumping = true;
                NPC.velocity = new Vector2(12f * Math.Sign(player.Center.X - NPC.Center.X) / NPC.scale, -24f) * NPC.scale;
                JumpCooldown = 200;
                JumpAndShootTimer = 200;
                TeslaCooldown = 360f;
            }
            else {
                TeslaCooldown = 160f;
                ShootTeslaBall(Cannon.TopPos, Cannon.Seg2Rot.ToRotationVector2().RotatedByRandom(0.1f) * 6f);
                SoundEngine.PlaySound(SoundID.Item93, Cannon.TopPos);
                Cannon.Seg1RotVelocity = 0.2f * Direction;
            }
        }

        private void UpdateMovement(Player player, float enrage) {
            if (!Jumping) {
                UpdateGroundMovement(player, enrage);
            }
            else {
                UpdateJumpMovement(player);
            }
        }

        private void UpdateGroundMovement(Player player, float enrage) {
            int onTileCount = 0;
            foreach (var leg in Legs) {
                if (leg.OnTile) onTileCount++;
            }

            bool grounded = onTileCount >= 3;
            if (grounded) {
                if (JumpFlag) {
                    JumpFlag = false;
                    if (NPC.velocity.Y > 0f) NPC.velocity.Y = 0f;
                }
            }

            if (grounded || CheckSolidTile(NPC.getRect())) {
                if (IsHarpoonOnLauncher && player.Center.Y + 200f * NPC.scale < NPC.Center.Y) {
                    if (JumpCooldown <= -260) {
                        Jumping = true;
                        NPC.velocity = new Vector2(
                            0.01f * (player.Center.X - NPC.Center.X) / NPC.scale,
                            MathF.Max((player.Center.Y - NPC.Center.Y) / NPC.scale * 0.08f, -30f)
                        ) * NPC.scale;
                        JumpCooldown = 160;
                    }
                }

                float yOffset = -90f * NPC.scale;
                if (NPC.Center.Y - yOffset + 90f * NPC.scale * NPC.scale > player.Center.Y) {
                    if (NPC.velocity.Y > 2f * NPC.scale) NPC.velocity.Y = 2f * NPC.scale;
                    if (NPC.velocity.Y > 0f) NPC.velocity.Y *= 0.84f;
                }

                float vFactor = 0.2f;
                float yDist = MathF.Abs(NPC.Center.Y + yOffset - player.Center.Y);
                if (yDist > 150f * NPC.scale) vFactor = 1f;
                if (yDist < 14f * NPC.scale) { vFactor = 0f; NPC.velocity.Y *= 0.8f; }
                vFactor *= NPC.scale;

                if (IsHarpoonOnLauncher && MathF.Abs(yOffset + player.Center.Y - NPC.Center.Y) > 20f * NPC.scale) {
                    if (player.Center.Y + yOffset > NPC.Center.Y) {
                        NPC.velocity.Y += 0.4f * enrage * vFactor;
                    }
                    else {
                        bool canGoUp = true, mustGoDown = false;
                        foreach (var leg in Legs) {
                            if (leg.OnTile && leg.StandPoint.Y > NPC.Center.Y + 110f * NPC.scale) canGoUp = false;
                            if (leg.OnTile && leg.StandPoint.Y > NPC.Center.Y + 130f * NPC.scale) mustGoDown = true;
                        }

                        if (canGoUp || CheckSolidTile(NPC.getRect()))
                            NPC.velocity.Y -= 0.6f * enrage * vFactor;
                        else if (mustGoDown)
                            NPC.velocity.Y += 2f * enrage * vFactor;
                    }
                }
            }
            else {
                NPC.velocity.Y += 0.42f;
                if (NPC.velocity.Y > 12f) NPC.velocity.Y = 12f;
            }

            if (IsHarpoonOnLauncher && Distance(NPC.Center, player.Center) > 100f * NPC.scale) {
                NPC.velocity.X += Math.Sign(player.Center.X - NPC.Center.X) * 0.1f * enrage * NPC.scale;
            }
        }

        private void UpdateJumpMovement(Player player) {
            int onTileCount = 0;
            foreach (var leg in Legs) {
                if (leg.OnTile) onTileCount++;
            }

            bool grounded = onTileCount > 2;

            if (JumpAndShootTimer <= 96 && NPC.ai[2] <= 0f) {
                if (((NPC.velocity.Y > 0f && JumpAndShootTimer <= 0) || NPC.Center.Y < player.Center.Y) && grounded) {
                    Jumping = false;
                    NPC.velocity *= 0f;
                }

                if (JumpCooldown < 20
                    || (NPC.velocity.Y > 0f && CheckSolidTile(NPC.getRect()) && JumpAndShootTimer <= 0)
                    || NPC.velocity.Y > 2f) {
                    Jumping = false;
                    NPC.velocity *= 0f;
                }
            }

            JumpFlag = true;
        }

        private void UpdateDirection() {
            if (NPC.velocity.X > 0f) {
                if (Direction == -1) NPC.rotation += MathHelper.Pi;
                Direction = 1;
            }
            if (NPC.velocity.X < 0f) {
                if (Direction == 1) NPC.rotation += MathHelper.Pi;
                Direction = -1;
            }
        }

        private void UpdatePullLogic() {
            if (NPC.ai[2]-- > 0f) {
                NPC harpoonNpc = HarpoonNpcIndex.ToNPC();
                if (harpoonNpc != null) {
                    NPC.velocity = (harpoonNpc.Center - NPC.Center).SafeNormalize() * 40f;
                }
            }
        }

        private void ShootTeslaBall(Vector2 pos, Vector2 velocity) {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int baseDamage = (int)(NPC.damage / 6.2f);
            Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, velocity,
                ProjectileID.Electrosphere, baseDamage, 4f, Main.myPlayer);
        }

        #endregion

        #region Networking

        public override void SendExtraAI(BinaryWriter writer) {
            EnsureSegments();
            writer.Write(NPC.boss);
            writer.Write(CannonUpAttackTimer);
            Cannon.NetSend(writer);
            Harpoon.NetSend(writer);
            writer.Write(HarpoonNpcIndex);
            writer.Write(Legs.Count > 0);
            if (Legs.Count > 0) {
                foreach (var leg in Legs) leg.NetSend(writer);
            }
            writer.Write(JumpCooldown);
            writer.Write(Jumping);
            writer.Write(TeslaCooldown);
            writer.Write(HarpoonCooldown);
            writer.Write(JumpAndShootTimer);
            writer.Write(CannonUpAttackTimer);
            writer.Write(Defeated);
            writer.Write(DeathCounter);
        }

        public override void ReceiveExtraAI(BinaryReader reader) {
            EnsureSegments();
            NPC.boss = reader.ReadBoolean();
            CannonUpAttackTimer = reader.ReadInt32();
            Cannon.NetReceive(reader);
            Harpoon.NetReceive(reader);
            HarpoonNpcIndex = reader.ReadInt32();

            if (reader.ReadBoolean()) {
                if (Legs.Count > 0) {
                    foreach (var leg in Legs) leg.NetReceive(reader);
                }
            }
            else {
                for (int i = 0; i < 4; i++) {
                    new HierophantLeg(NPC, Vector2.Zero).NetReceive(reader);
                }
            }

            JumpCooldown = reader.ReadInt32();
            Jumping = reader.ReadBoolean();
            TeslaCooldown = reader.ReadSingle();
            HarpoonCooldown = reader.ReadSingle();
            JumpAndShootTimer = reader.ReadInt32();
            CannonUpAttackTimer = reader.ReadInt32();
            Defeated = reader.ReadBoolean();
            DeathCounter = reader.ReadInt32();
        }

        #endregion

        #region Drawing

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (Legs == null) return false;

            if (Main.zenithWorld) drawColor = Main.DiscoColor;

            Texture2D body = NPC.GetNpcTexture();
            Texture2D legTex1 = RequestTex("CalamityOverhaul/Content/ADV/Scenarios/Draedons/AcheronProtocols/Hierophants/Leg1");
            Texture2D legTex2 = RequestTex("CalamityOverhaul/Content/ADV/Scenarios/Draedons/AcheronProtocols/Hierophants/Leg2");
            Texture2D footTex = RequestTex("CalamityOverhaul/Content/ADV/Scenarios/Draedons/AcheronProtocols/Hierophants/Foot");

            DrawLegs(drawColor, legTex1, legTex2, footTex);
            DrawArms(spriteBatch, screenPos, drawColor, body);

            return false;
        }

        private void DrawLegs(Color drawColor, Texture2D t1, Texture2D t2, Texture2D t3) {
            foreach (var leg in Legs) {
                float l1 = 46f * NPC.scale * leg.Scale;
                float l2 = 70f * NPC.scale * leg.Scale;
                float l3 = 72f * NPC.scale * leg.Scale;

                float bodyRot = Direction > 0 ? NPC.rotation : (-MathHelper.Pi + NPC.rotation);
                Vector2 start = NPC.Center + (new Vector2(Math.Sign(leg.Offset.X) * 20f, 60f) * NPC.scale).RotatedBy(bodyRot);

                CalculateLegJoints(start, leg.StandPoint, l1, l2, l3, out Vector2 p1, out _);
                Vector2 p2 = GetCircleIntersection(p1, l2, leg.StandPoint, l3);
                Vector2 p3 = p2 + (leg.StandPoint - p2).SafeNormalize() * l3;

                Main.EntitySpriteDraw(t1, start - Main.screenPosition, null, drawColor,
                    (p1 - start).ToRotation(), new Vector2(4, 13), NPC.scale * leg.Scale, SpriteEffects.None);
                Main.EntitySpriteDraw(t2, p1 - Main.screenPosition, null, drawColor,
                    (p2 - p1).ToRotation(), new Vector2(6, 9), NPC.scale * leg.Scale, SpriteEffects.None);

                float footDir = leg.Offset.X > 0 ? 1f : -1f;
                Main.EntitySpriteDraw(t3, p2 - Main.screenPosition, null, drawColor,
                    (p3 - p2).ToRotation() + footDir * MathHelper.ToRadians(24f),
                    new Vector2(27, t3.Height / 2f), NPC.scale * leg.Scale,
                    leg.Offset.X > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically);
            }
        }

        private void DrawArms(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, Texture2D body) {
            string basePath = "CalamityOverhaul/Content/ADV/Scenarios/Draedons/AcheronProtocols/Hierophants/";
            Texture2D cannonConnect = RequestTex(basePath + "CannonConnect");
            Texture2D cannonTex = RequestTex(basePath + "Cannon");
            Texture2D harpoonArm = RequestTex(basePath + "HarpoonArm");
            Texture2D harpoonLauncher = RequestTex(basePath + "HarpoonLauncher");
            Texture2D harpoonTex = RequestTex(basePath + "Harpoon");
            Texture2D harpoonOutline = RequestTex(basePath + "HarpoonOutline");
            Texture2D shoulder = RequestTex(basePath + "Shoulder");

            float bodyRot = Direction > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi);
            SpriteEffects dirFlip = Direction > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;

            // Harpoon arm
            Vector2 harpoonBase = (Harpoon.Offset * new Vector2(Direction, 1f) * NPC.scale).RotatedBy(bodyRot) + NPC.Center;
            Main.EntitySpriteDraw(harpoonArm, harpoonBase - Main.screenPosition, null, drawColor,
                Harpoon.Seg1Rot, new Vector2(6, harpoonArm.Height / 2f), NPC.scale, SpriteEffects.None);

            // Harpoon projectile on launcher
            if (HarpoonNpcIndex < 0 || IsHarpoonOnLauncher) {
                Vector2 harpoonDrawPos = Harpoon.Seg1End + Harpoon.Seg2Rot.ToRotationVector2() * 150f * NPC.scale
                    + new Vector2(0f, 10f * Direction).RotatedBy(Harpoon.Seg2Rot) * NPC.scale;

                for (float r = 0; r <= 360; r += 60) {
                    Main.EntitySpriteDraw(harpoonOutline,
                        MathHelper.ToRadians(r).ToRotationVector2() * 2f + harpoonDrawPos - Main.screenPosition,
                        null, Color.OrangeRed * HarpoonCharge, Harpoon.Seg2Rot,
                        new Vector2(70, harpoonTex.Height / 2f), NPC.scale, dirFlip);
                }

                Main.EntitySpriteDraw(harpoonTex, harpoonDrawPos - Main.screenPosition, null, drawColor,
                    Harpoon.Seg2Rot, new Vector2(70, harpoonTex.Height / 2f), NPC.scale, dirFlip);
            }

            Main.EntitySpriteDraw(harpoonLauncher, Harpoon.Seg1End - Main.screenPosition, null, drawColor,
                Harpoon.Seg2Rot, new Vector2(6, harpoonLauncher.Height / 2f), NPC.scale, dirFlip);

            // Body
            Main.EntitySpriteDraw(body, NPC.Center - screenPos, null, drawColor,
                NPC.rotation, body.Size() / 2f, NPC.scale,
                Direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);

            // Cannon
            Vector2 cannonBase = (Cannon.Offset * new Vector2(Direction, 1f) * NPC.scale).RotatedBy(bodyRot) + NPC.Center;
            Main.EntitySpriteDraw(cannonConnect, cannonBase - Main.screenPosition, null, drawColor,
                Cannon.Seg1Rot, new Vector2(6, cannonConnect.Height / 2f), NPC.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(cannonTex, Cannon.Seg1End - Main.screenPosition, null, drawColor,
                Cannon.Seg2Rot, new Vector2(6, cannonTex.Height / 2f), NPC.scale, dirFlip);

            // Shoulder
            Main.EntitySpriteDraw(shoulder, NPC.Center - screenPos, null, drawColor,
                NPC.rotation, shoulder.Size() / 2f, NPC.scale,
                Direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        }

        #endregion

        #region Hit & Death Effects

        public override void HitEffect(NPC.HitInfo hit) {
            if (DeathCounter > 0 || NPC.life > 0 || Main.dedServ) return;

            AddCameraShake(NPC.Center, 100f);

            for (int i = 0; i < 40; i++) {
                Dust dust = Dust.NewDustPerfect(NPC.Center + RandomPointInCircle(60f * NPC.scale),
                    DustID.Torch, RandomPointInCircle(32f * NPC.scale));
                dust.scale = Main.rand.NextFloat(1f, 4f) * NPC.scale;
                dust.noGravity = true;
            }

            for (int i = 0; i < 10; i++) {
                Gore.NewGore(NPC.GetSource_Death(), NPC.Center + RandomPointInCircle(46f),
                    RandomPointInCircle(16f), Main.rand.Next(61, 64), NPC.scale);
            }
        }

        public override void OnKill() {
            if (!Main.dedServ) {
                for (int i = 0; i < 60; i++) {
                    Dust d = Dust.NewDustPerfect(NPC.Center + RandomPointInCircle(80f * NPC.scale),
                        DustID.Torch, RandomPointInCircle(20f), Scale: Main.rand.NextFloat(1.5f, 4f));
                    d.noGravity = true;
                }
                for (int i = 0; i < 20; i++) {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.Center + RandomPointInCircle(40f),
                        RandomPointInCircle(8f), Main.rand.Next(61, 64), NPC.scale);
                }
                SoundEngine.PlaySound(SoundID.NPCDeath14, NPC.Center);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(ItemDropRule.Common(ItemID.HallowedBar, 1, 16, 24));
            npcLoot.Add(ItemDropRule.Common(ItemID.SoulofMight, 1, 10, 20));
        }

        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot,
            ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox) => false;

        #endregion

        #region Leg IK

        public Vector2 CalculateLegJoints(Vector2 center, Vector2 legStandPoint, float l1, float l2, float l3,
            out Vector2 p1, out Vector2 p2) {
            p1 = Vector2.Zero;
            p2 = Vector2.Zero;
            if (l1 <= 0 || l2 <= 0 || l3 <= 0) return center;

            Vector2 d = legStandPoint - center;
            float dist = d.Length();
            Vector2 target = legStandPoint;

            if (dist > l1 + l2 + l3) {
                target = center + Vector2.Normalize(d) * (l1 + l2 + l3);
            }

            Vector2 downDir = new(0, 1);
            Vector2 targetDir = d.Length() > 0 ? Vector2.Normalize(d) : downDir;

            float maxDeflection = MathHelper.ToRadians(68f);
            float angleToTarget = MathF.Atan2(targetDir.Y, targetDir.X) - MathF.PI / 2f;
            float deflection = MathHelper.Clamp(angleToTarget, -maxDeflection, maxDeflection);

            float cos = MathF.Cos(deflection);
            float sin = MathF.Sin(deflection);
            Vector2 firstDir = new(downDir.X * cos - downDir.Y * sin, downDir.X * sin + downDir.Y * cos);

            p1 = center + l1 * firstDir;

            float y2 = target.Y - l3;
            float deltaY = y2 - p1.Y;
            float deltaX;
            try {
                deltaX = MathF.Sqrt(l2 * l2 - deltaY * deltaY);
            }
            catch {
                deltaX = 0;
                y2 = p1.Y - l2;
            }

            float x2Pos = p1.X + deltaX;
            float x2Neg = p1.X - deltaX;
            float x2 = MathF.Abs(x2Pos - target.X) < MathF.Abs(x2Neg - target.X) ? x2Pos : x2Neg;

            p2 = new Vector2(x2, y2);

            float distP2 = Vector2.Distance(p2, target);
            if (MathF.Abs(distP2 - l3) > 0.001f) {
                p2 = new Vector2(p1.X, p1.Y - l2);
                target = new Vector2(p2.X, p2.Y + l3);
            }

            return target;
        }

        #endregion

        #region Helpers

        private static void AddCameraShake(Vector2 center, float strength) {
            float s = Utils.Remap(Main.LocalPlayer.Distance(center), 4000f, 800f, 0f, strength);
            if (s <= 0f) return;
            var shake = new PunchCameraModifier(center, Main.rand.NextVector2Unit(), s, 6f, 10);
            Main.instance.CameraModifiers.Add(shake);
        }

        #endregion
    }
}
