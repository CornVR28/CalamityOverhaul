using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
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
                var boss = (Hierophant)NPC.ModNPC;

                if (boss.Jumping) {
                    _onTileFlag = false;
                    TargetPos = NPC.Center + new Vector2(Offset.X * 0.2f, 200f) * NPC.scale;
                    MoveSpeed = Distance(TargetPos, StandPoint) * 0.2f;
                    return false;
                }

                float bodyRot = boss.Direction > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi);
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
                var boss = (Hierophant)NPC.ModNPC;

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

                float rot = boss.Direction > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi);
                return NPC.Center + new Vector2(Offset.X, 200f).RotatedBy(rot);
            }
        }

        /// <summary>
        /// 双节臂结构，第一段(上臂)连接身体，第二段(前臂/刃)连接武器末端
        /// </summary>
        public class HierophantArm
        {
            public float Seg1RotVelocity;
            public float Seg1Length;
            public float Seg1Rot;
            public float Seg2Rot;
            public float Seg1MaxRadians = MathHelper.ToRadians(50f);
            public Vector2 Offset;
            public NPC Npc;

            /// <summary>
            /// 镰刀刃尖的世界坐标
            /// </summary>
            public Vector2 BladeEnd => Seg1End + Seg2Rot.ToRotationVector2() * 60f * Npc.scale;

            public Vector2 Seg1End {
                get {
                    var boss = (Hierophant)Npc.ModNPC;
                    float rot = boss.Direction > 0 ? Npc.rotation : (Npc.rotation + MathHelper.Pi);
                    return Npc.Center + (Offset * new Vector2(boss.Direction, 1f) * Npc.scale).RotatedBy(rot)
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
                var boss = (Hierophant)Npc.ModNPC;
                float rot = boss.Direction > 0 ? Npc.rotation : (Npc.rotation + MathHelper.Pi);
                Vector2 basePos = Npc.Center + (Offset * new Vector2(boss.Direction, 1f)).RotatedBy(rot);

                Seg1Rot = RotateTowardsAngle(Seg1Rot, (pos - basePos).ToRotation(), 0.06f, false);

                if (GetAngleBetweenVectors(Seg1Rot.ToRotationVector2(), -Vector2.UnitY) > Seg1MaxRadians * 2f) {
                    float upper = MathHelper.PiOver2 + Seg1MaxRadians;
                    float lower = MathHelper.PiOver2 - Seg1MaxRadians;
                    if (Seg1Rot > upper) Seg1Rot = upper;
                    if (Seg1Rot < lower) Seg1Rot = lower;
                }

                Seg2Rot = RotateTowardsAngle(Seg2Rot, (pos - Seg1End).ToRotation(), 0.06f, false);
            }

            /// <summary>
            /// 快速挥动：以高速旋转角速度驱动 Seg2 朝目标方向横扫
            /// </summary>
            public void SwingTowards(Vector2 target, float angularSpeed) {
                var boss = (Hierophant)Npc.ModNPC;
                float rot = boss.Direction > 0 ? Npc.rotation : (Npc.rotation + MathHelper.Pi);
                Vector2 basePos = Npc.Center + (Offset * new Vector2(boss.Direction, 1f)).RotatedBy(rot);

                Seg1Rot = RotateTowardsAngle(Seg1Rot, (target - basePos).ToRotation(), 0.12f, false);
                Seg2Rot = RotateTowardsAngle(Seg2Rot, (target - Seg1End).ToRotation(), angularSpeed);
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

        #region Enums

        private enum SlashState : byte
        {
            Idle,
            LeftSlash,
            RightSlash,
            CrossSlash,
            SlamDown
        }

        #endregion

        #region Constants

        private const float SlashBladeRadius = 110f;
        private const int SlashDuration = 30;
        private const int CrossSlashDuration = 36;
        private const int SlamDuration = 24;

        #endregion

        #region Fields

        public List<HierophantLeg> Legs;
        public HierophantArm LeftScythe;
        public HierophantArm RightScythe;
        public int Direction = 1;
        public int DeathCounter = 240;
        public bool Defeated;
        public bool Jumping;
        public bool JumpFlag;
        public bool BossActivated = true;
        public int JumpCooldown;

        private int _despawnCounter;
        private SlashState _slashState = SlashState.Idle;
        private int _slashTimer;
        private float _slashCooldown = 60f;
        private bool _slamDamageDealt;

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
            LeftScythe = new HierophantArm(NPC, new Vector2(-80, -32), 76f, MathHelper.PiOver2, MathHelper.PiOver2);
            RightScythe = new HierophantArm(NPC, new Vector2(60, -18), 66f, MathHelper.PiOver2, MathHelper.PiOver2);
        }

        public static bool CanStandOn(Vector2 pos) => !IsAir(pos, true);

        #endregion

        #region AI

        public override void AI() {
            NPC.chaseable = NPC.boss;
            JumpCooldown--;
            EnsureSegments();
            LeftScythe.Update();
            RightScythe.Update();

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

            DeathCounter--;
            NPC.velocity *= 0f;
            Jumping = false;
            _slashState = SlashState.Idle;

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

            UpdateSlashAI(player, enrage);
            UpdateMovement(player, enrage);
            UpdateDirection();
        }

        private void UpdateSlashAI(Player player, float enrage) {
            float distToPlayer = Distance(player.Center, NPC.Center);

            if (_slashState != SlashState.Idle) {
                RunActiveSlash(player, enrage);
                return;
            }

            // 空闲时双臂缓慢追踪玩家
            LeftScythe.PointAt(player.Center + new Vector2(-40f, -20f));
            RightScythe.PointAt(player.Center + new Vector2(40f, -20f));

            _slashCooldown -= enrage;
            if (_slashCooldown > 0f) return;

            // 选择攻击模式
            if (distToPlayer < 300f * NPC.scale) {
                // 近距离：交叉斩
                _slashState = SlashState.CrossSlash;
                _slashTimer = CrossSlashDuration;
                _slashCooldown = 120f;
                SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
            }
            else if (distToPlayer < 500f * NPC.scale) {
                // 中距离：交替单刀斩
                _slashState = Main.rand.NextBool() ? SlashState.LeftSlash : SlashState.RightSlash;
                _slashTimer = SlashDuration;
                _slashCooldown = 80f;
                SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
            }
            else if (!Jumping && JumpCooldown <= -200) {
                // 远距离：跳跃 + 落地砸击
                Jumping = true;
                NPC.velocity = new Vector2(
                    12f * Math.Sign(player.Center.X - NPC.Center.X) / NPC.scale, -22f) * NPC.scale;
                JumpCooldown = 180;
                _slashState = SlashState.SlamDown;
                _slashTimer = SlamDuration;
                _slashCooldown = 140f;
                _slamDamageDealt = false;
                SoundEngine.PlaySound(SoundID.Item73, NPC.Center);
            }
            else {
                // 无法攻击时缩短冷却
                _slashCooldown = 20f;
            }
        }

        private void RunActiveSlash(Player player, float enrage) {
            _slashTimer--;
            float progress = 1f - (float)_slashTimer / GetSlashMaxDuration();

            switch (_slashState) {
                case SlashState.LeftSlash:
                    RunSingleSlash(LeftScythe, player, progress, enrage, -1);
                    RightScythe.PointAt(player.Center + new Vector2(40f, -20f));
                    break;

                case SlashState.RightSlash:
                    RunSingleSlash(RightScythe, player, progress, enrage, 1);
                    LeftScythe.PointAt(player.Center + new Vector2(-40f, -20f));
                    break;

                case SlashState.CrossSlash:
                    RunSingleSlash(LeftScythe, player, progress, enrage, -1);
                    RunSingleSlash(RightScythe, player, progress, enrage, 1);
                    break;

                case SlashState.SlamDown:
                    RunSlamSlash(player, progress, enrage);
                    break;
            }

            if (_slashTimer <= 0) {
                _slashState = SlashState.Idle;
                // 连招：交叉斩后快速追加一次单斩
                if (progress >= 1f && _slashState == SlashState.Idle
                    && Distance(player.Center, NPC.Center) < 400f * NPC.scale
                    && Main.rand.NextBool(3)) {
                    _slashState = Main.rand.NextBool() ? SlashState.LeftSlash : SlashState.RightSlash;
                    _slashTimer = (int)(SlashDuration * 0.7f);
                    _slashCooldown = 60f;
                    SoundEngine.PlaySound(SoundID.Item71 with { Pitch = 0.3f }, NPC.Center);
                }
            }
        }

        private void RunSingleSlash(HierophantArm arm, Player player, float progress, float enrage, int side) {
            // 挥刀弧线：先抬起(0~0.3)，然后猛烈劈下(0.3~0.8)，回收(0.8~1.0)
            float bladeRange = SlashBladeRadius * NPC.scale;

            if (progress < 0.3f) {
                // 蓄力举起
                float raise = progress / 0.3f;
                Vector2 raiseTarget = NPC.Center + new Vector2(side * 60f, -120f * raise) * NPC.scale;
                arm.SwingTowards(raiseTarget, 0.15f);
            }
            else if (progress < 0.8f) {
                // 劈砍弧线
                float swing = (progress - 0.3f) / 0.5f;
                float arcAngle = MathHelper.Lerp(-MathHelper.PiOver4 * side, MathHelper.PiOver2 * side, swing);
                Vector2 swingTarget = player.Center + new Vector2(MathF.Cos(arcAngle), MathF.Sin(arcAngle)) * bladeRange * 0.4f;
                arm.SwingTowards(swingTarget, 0.3f);

                // 伤害判定：刃端碰撞
                if (Distance(arm.BladeEnd, player.Center) < bladeRange) {
                    TrySlashDamage(player, arm.BladeEnd, enrage);
                }

                // 挥刀拖尾粒子
                if (!Main.dedServ && Main.GameUpdateCount % 2 == 0) {
                    Dust d = Dust.NewDustPerfect(arm.BladeEnd, DustID.Wraith,
                        (arm.BladeEnd - NPC.Center).SafeNormalize().RotatedByRandom(0.5f) * 4f,
                        Alpha: 120, Scale: 1.5f * NPC.scale);
                    d.noGravity = true;
                }
            }
            else {
                // 回收
                arm.PointAt(NPC.Center + new Vector2(side * 50f, 30f) * NPC.scale);
            }
        }

        private void RunSlamSlash(Player player, float progress, float enrage) {
            if (progress < 0.5f) {
                // 空中双臂张开
                float spread = progress / 0.5f;
                LeftScythe.SwingTowards(NPC.Center + new Vector2(-100f * spread, -80f) * NPC.scale, 0.15f);
                RightScythe.SwingTowards(NPC.Center + new Vector2(100f * spread, -80f) * NPC.scale, 0.15f);
            }
            else {
                // 落地后双臂向下猛砸
                float slam = (progress - 0.5f) / 0.5f;
                Vector2 groundPoint = NPC.Center + new Vector2(0f, 120f) * NPC.scale;
                LeftScythe.SwingTowards(groundPoint + new Vector2(-30f, 0f) * NPC.scale, 0.25f);
                RightScythe.SwingTowards(groundPoint + new Vector2(30f, 0f) * NPC.scale, 0.25f);

                // 落地砸击伤害 + 震波
                if (!_slamDamageDealt && !Jumping) {
                    _slamDamageDealt = true;
                    AddCameraShake(NPC.Center, 20f);
                    SoundEngine.PlaySound(SoundID.Item14, NPC.Center);

                    // 生成地面尘埃震波
                    if (!Main.dedServ) {
                        for (int i = 0; i < 30; i++) {
                            Vector2 dustVel = new Vector2(Main.rand.NextFloat(-12f, 12f), Main.rand.NextFloat(-6f, -1f));
                            Dust d = Dust.NewDustPerfect(NPC.Bottom + new Vector2(Main.rand.NextFloat(-80f, 80f) * NPC.scale, 0f),
                                DustID.Smoke, dustVel, Alpha: 100, Scale: Main.rand.NextFloat(2f, 4f) * NPC.scale);
                            d.noGravity = true;
                        }
                        for (int i = 0; i < 16; i++) {
                            Dust d2 = Dust.NewDustPerfect(NPC.Bottom + new Vector2(Main.rand.NextFloat(-60f, 60f) * NPC.scale, 0f),
                                DustID.Electric, new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-8f, -2f)),
                                Scale: Main.rand.NextFloat(1f, 2.5f));
                            d2.noGravity = true;
                        }
                    }

                    // 范围伤害判定
                    float slamRadius = 160f * NPC.scale;
                    foreach (Player p in Main.ActivePlayers) {
                        if (Distance(p.Center, NPC.Bottom) < slamRadius) {
                            TrySlashDamage(p, NPC.Bottom, enrage);
                        }
                    }
                }
            }
        }

        private void TrySlashDamage(Player player, Vector2 hitPos, float enrage) {
            if (player.immune || player.immuneTime > 0) return;

            int dmg = (int)(NPC.damage * 1.5f * enrage);
            player.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), dmg, Math.Sign(hitPos.X - player.Center.X));
        }

        private int GetSlashMaxDuration() {
            return _slashState switch {
                SlashState.CrossSlash => CrossSlashDuration,
                SlashState.SlamDown => SlamDuration,
                _ => SlashDuration,
            };
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
            if (grounded && JumpFlag) {
                JumpFlag = false;
                if (NPC.velocity.Y > 0f) NPC.velocity.Y = 0f;
            }

            if (grounded || CheckSolidTile(NPC.getRect())) {
                // 玩家在上方时跳跃
                if (player.Center.Y + 200f * NPC.scale < NPC.Center.Y && JumpCooldown <= -260) {
                    Jumping = true;
                    NPC.velocity = new Vector2(
                        0.01f * (player.Center.X - NPC.Center.X) / NPC.scale,
                        MathF.Max((player.Center.Y - NPC.Center.Y) / NPC.scale * 0.08f, -30f)
                    ) * NPC.scale;
                    JumpCooldown = 160;
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

                if (MathF.Abs(yOffset + player.Center.Y - NPC.Center.Y) > 20f * NPC.scale) {
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

            // 近战Boss更积极地追踪玩家
            if (Distance(NPC.Center, player.Center) > 80f * NPC.scale) {
                NPC.velocity.X += Math.Sign(player.Center.X - NPC.Center.X) * 0.15f * enrage * NPC.scale;
            }
        }

        private void UpdateJumpMovement(Player player) {
            int onTileCount = 0;
            foreach (var leg in Legs) {
                if (leg.OnTile) onTileCount++;
            }

            bool grounded = onTileCount > 2;

            if (_slashState != SlashState.SlamDown || _slashTimer <= 0) {
                if (((NPC.velocity.Y > 0f) || NPC.Center.Y < player.Center.Y) && grounded) {
                    Jumping = false;
                    NPC.velocity *= 0f;
                }

                if (JumpCooldown < 20
                    || (NPC.velocity.Y > 0f && CheckSolidTile(NPC.getRect()))
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
            writer.Write(_slashCooldown);
            writer.Write((byte)_slashState);
            writer.Write(_slashTimer);
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
                for (int i = 0; i < 4; i++) {
                    new HierophantLeg(NPC, Vector2.Zero).NetReceive(reader);
                }
            }

            JumpCooldown = reader.ReadInt32();
            Jumping = reader.ReadBoolean();
            _slashCooldown = reader.ReadSingle();
            _slashState = (SlashState)reader.ReadByte();
            _slashTimer = reader.ReadInt32();
            Defeated = reader.ReadBoolean();
            DeathCounter = reader.ReadInt32();
        }

        #endregion

        #region Drawing

        private const string TexBasePath = "CalamityOverhaul/Content/ADV/Scenarios/Draedons/AcheronProtocols/Hierophants/";

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (Legs == null) return false;

            if (Main.zenithWorld) drawColor = Main.DiscoColor;

            Texture2D body = NPC.GetNpcTexture();
            Texture2D legTex1 = RequestTex(TexBasePath + "Leg1");
            Texture2D legTex2 = RequestTex(TexBasePath + "Leg2");
            Texture2D footTex = RequestTex(TexBasePath + "Foot");

            DrawLegs(drawColor, legTex1, legTex2, footTex);
            DrawScytheArms(screenPos, drawColor, body);

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

        private void DrawScytheArms(Vector2 screenPos, Color drawColor, Texture2D body) {
            Texture2D armTex = RequestTex(TexBasePath + "CannonConnect");
            Texture2D bladeTex = RequestTex(TexBasePath + "Cannon");
            Texture2D shoulder = RequestTex(TexBasePath + "Shoulder");

            float bodyRot = Direction > 0 ? NPC.rotation : (NPC.rotation + MathHelper.Pi);
            SpriteEffects dirFlip = Direction > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
            SpriteEffects bodyFlip = Direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            // 层级 1（最后方）：右镰刀臂 — 绘制在身体后面
            DrawSingleScythe(RightScythe, armTex, bladeTex, drawColor, bodyRot, dirFlip, true);

            // 层级 2：身体
            Main.EntitySpriteDraw(body, NPC.Center - screenPos, null, drawColor,
                NPC.rotation, body.Size() / 2f, NPC.scale, bodyFlip);

            // 层级 3：肩甲（覆盖在身体之上）
            Main.EntitySpriteDraw(shoulder, NPC.Center - screenPos, null, drawColor,
                NPC.rotation, shoulder.Size() / 2f, NPC.scale, bodyFlip);

            // 层级 4（最前方）：左镰刀臂 — 绘制在身体前面
            DrawSingleScythe(LeftScythe, armTex, bladeTex, drawColor, bodyRot, dirFlip, false);
        }

        private void DrawSingleScythe(HierophantArm arm, Texture2D armTex, Texture2D bladeTex,
            Color drawColor, float bodyRot, SpriteEffects dirFlip, bool flipBlade) {
            // 上臂段
            Vector2 armBase = (arm.Offset * new Vector2(Direction, 1f) * NPC.scale).RotatedBy(bodyRot) + NPC.Center;
            Main.EntitySpriteDraw(armTex, armBase - Main.screenPosition, null, drawColor,
                arm.Seg1Rot, new Vector2(6, armTex.Height / 2f), NPC.scale, SpriteEffects.None);

            // 镰刀刃
            SpriteEffects bladeFlip = flipBlade
                ? (dirFlip == SpriteEffects.None ? SpriteEffects.FlipVertically : SpriteEffects.None)
                : dirFlip;
            Main.EntitySpriteDraw(bladeTex, arm.Seg1End - Main.screenPosition, null, drawColor,
                arm.Seg2Rot, new Vector2(6, bladeTex.Height / 2f), NPC.scale, bladeFlip);

            // 挥刀时绘制拖影
            if (_slashState != SlashState.Idle) {
                float slashProgress = 1f - (float)_slashTimer / GetSlashMaxDuration();
                if (slashProgress is > 0.3f and < 0.8f) {
                    Color trailColor = Color.OrangeRed * 0.4f;
                    for (int i = 1; i <= 3; i++) {
                        float offset = i * 0.08f;
                        Main.EntitySpriteDraw(bladeTex, arm.Seg1End - Main.screenPosition, null,
                            trailColor * (1f - i * 0.25f),
                            arm.Seg2Rot - offset * Direction,
                            new Vector2(6, bladeTex.Height / 2f), NPC.scale, bladeFlip);
                    }
                }
            }
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
