using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using static CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants.HierophantUtils;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants
{
    /// <summary>
    /// 战斗子控制器——管理移动、方向、攻击子状态机
    /// </summary>
    internal sealed class HierophantCombatController
    {
        #region Constants

        private const float SlashBladeRadius = 220f;
        private const int SlashDuration = 30;
        private const int CrossSlashDuration = 36;
        private const int SlamDuration = 24;

        #endregion

        #region Slash Sub-State

        internal enum SlashPhase : byte
        {
            Idle,
            LeftSlash,
            RightSlash,
            CrossSlash,
            SlamDown
        }

        internal SlashPhase CurrentSlash = SlashPhase.Idle;
        internal int SlashTimer;
        internal float SlashCooldown = 60f;
        private bool _slamDamageDealt;

        public void ResetSlash() => CurrentSlash = SlashPhase.Idle;

        #endregion

        #region Entry

        /// <summary>
        /// 每帧由 <see cref="CombatState"/> 调用
        /// </summary>
        public void Run(Hierophant boss, Player player) {
            float enrage = CalculateEnrage(boss.NPC);
            UpdateSlashAI(boss, player, enrage);
            UpdateMovement(boss, player, enrage);
            UpdateDirection(boss);
        }

        private static float CalculateEnrage(NPC npc) {
            float e = 1f + (1f - (float)npc.life / npc.lifeMax);
            if (Main.expertMode) e += 0.07f;
            if (Main.masterMode) e += 0.07f;
            if (Main.getGoodWorld) e *= 1.1f;
            if (Main.zenithWorld) e *= 1.15f;
            return e;
        }

        #endregion

        #region Slash AI

        private void UpdateSlashAI(Hierophant boss, Player player, float enrage) {
            if (CurrentSlash != SlashPhase.Idle) {
                RunActiveSlash(boss, player, enrage);
                return;
            }

            boss.LeftScythe.PointAt(player.Center + new Vector2(-40f, -20f));
            boss.RightScythe.PointAt(player.Center + new Vector2(40f, -20f));

            SlashCooldown -= enrage;
            if (SlashCooldown > 0f) return;

            float distToPlayer = Distance(player.Center, boss.NPC.Center);
            ChooseAttack(boss, player, distToPlayer);
        }

        private void ChooseAttack(Hierophant boss, Player player, float dist) {
            NPC npc = boss.NPC;

            if (dist < 300f * npc.scale) {
                EnterSlash(SlashPhase.CrossSlash, CrossSlashDuration, 120f);
                SoundEngine.PlaySound(SoundID.Item71, npc.Center);
            }
            else if (dist < 500f * npc.scale) {
                EnterSlash(Main.rand.NextBool() ? SlashPhase.LeftSlash : SlashPhase.RightSlash,
                    SlashDuration, 80f);
                SoundEngine.PlaySound(SoundID.Item71, npc.Center);
            }
            else if (!boss.Jumping && boss.JumpCooldown <= -200) {
                boss.Jumping = true;
                npc.velocity = new Vector2(
                    12f * Math.Sign(player.Center.X - npc.Center.X) / npc.scale, -22f) * npc.scale;
                boss.JumpCooldown = 180;
                EnterSlash(SlashPhase.SlamDown, SlamDuration, 140f);
                _slamDamageDealt = false;
                SoundEngine.PlaySound(SoundID.Item73, npc.Center);
            }
            else {
                SlashCooldown = 20f;
            }
        }

        private void EnterSlash(SlashPhase phase, int duration, float cooldown) {
            CurrentSlash = phase;
            SlashTimer = duration;
            SlashCooldown = cooldown;
        }

        private void RunActiveSlash(Hierophant boss, Player player, float enrage) {
            SlashTimer--;
            float progress = 1f - (float)SlashTimer / GetSlashMaxDuration();

            switch (CurrentSlash) {
                case SlashPhase.LeftSlash:
                    RunSingleSlash(boss, boss.LeftScythe, player, progress, enrage, -1);
                    boss.RightScythe.PointAt(player.Center + new Vector2(40f, -20f));
                    break;
                case SlashPhase.RightSlash:
                    RunSingleSlash(boss, boss.RightScythe, player, progress, enrage, 1);
                    boss.LeftScythe.PointAt(player.Center + new Vector2(-40f, -20f));
                    break;
                case SlashPhase.CrossSlash:
                    RunSingleSlash(boss, boss.LeftScythe, player, progress, enrage, -1);
                    RunSingleSlash(boss, boss.RightScythe, player, progress, enrage, 1);
                    break;
                case SlashPhase.SlamDown:
                    RunSlamSlash(boss, player, progress, enrage);
                    break;
            }

            if (SlashTimer <= 0) {
                CurrentSlash = SlashPhase.Idle;
                TryComboFollowup(boss, player, progress);
            }
        }

        private void TryComboFollowup(Hierophant boss, Player player, float progress) {
            if (progress >= 1f
                && Distance(player.Center, boss.NPC.Center) < 400f * boss.NPC.scale
                && Main.rand.NextBool(3)) {
                EnterSlash(Main.rand.NextBool() ? SlashPhase.LeftSlash : SlashPhase.RightSlash,
                    (int)(SlashDuration * 0.7f), 60f);
                SoundEngine.PlaySound(SoundID.Item71 with { Pitch = 0.3f }, boss.NPC.Center);
            }
        }

        #endregion

        #region Single Slash

        private static void RunSingleSlash(Hierophant boss, HierophantArm arm, Player player,
            float progress, float enrage, int side) {
            NPC npc = boss.NPC;
            float bladeRange = SlashBladeRadius * npc.scale;

            if (progress < 0.3f) {
                float raise = progress / 0.3f;
                Vector2 raiseTarget = npc.Center + new Vector2(side * 60f, -120f * raise) * npc.scale;
                arm.SwingTowards(raiseTarget, 0.15f);
            }
            else if (progress < 0.8f) {
                float swing = (progress - 0.3f) / 0.5f;
                float arcAngle = MathHelper.Lerp(-MathHelper.PiOver4 * side, MathHelper.PiOver2 * side, swing);
                Vector2 swingTarget = player.Center + new Vector2(MathF.Cos(arcAngle), MathF.Sin(arcAngle)) * bladeRange * 0.4f;
                arm.SwingTowards(swingTarget, 0.3f);

                if (Distance(arm.BladeEnd, player.Center) < bladeRange) {
                    TrySlashDamage(npc, player, arm.BladeEnd, enrage);
                }

                HierophantEffects.SlashTrailDust(npc, arm);
            }
            else {
                arm.PointAt(npc.Center + new Vector2(side * 50f, 30f) * npc.scale);
            }
        }

        #endregion

        #region Slam Slash

        private void RunSlamSlash(Hierophant boss, Player player, float progress, float enrage) {
            NPC npc = boss.NPC;

            if (progress < 0.5f) {
                float spread = progress / 0.5f;
                boss.LeftScythe.SwingTowards(npc.Center + new Vector2(-100f * spread, -80f) * npc.scale, 0.15f);
                boss.RightScythe.SwingTowards(npc.Center + new Vector2(100f * spread, -80f) * npc.scale, 0.15f);
            }
            else {
                Vector2 groundPoint = npc.Center + new Vector2(0f, 120f) * npc.scale;
                boss.LeftScythe.SwingTowards(groundPoint + new Vector2(-30f, 0f) * npc.scale, 0.25f);
                boss.RightScythe.SwingTowards(groundPoint + new Vector2(30f, 0f) * npc.scale, 0.25f);

                if (!_slamDamageDealt && !boss.Jumping) {
                    _slamDamageDealt = true;
                    HierophantEffects.CameraShake(npc.Center, 20f);
                    SoundEngine.PlaySound(SoundID.Item14, npc.Center);
                    HierophantEffects.SlamImpactDust(npc);

                    float slamRadius = 160f * npc.scale;
                    foreach (Player p in Main.ActivePlayers) {
                        if (Distance(p.Center, npc.Bottom) < slamRadius) {
                            TrySlashDamage(npc, p, npc.Bottom, enrage);
                        }
                    }
                }
            }
        }

        #endregion

        #region Damage

        private static void TrySlashDamage(NPC npc, Player player, Vector2 hitPos, float enrage) {
            if (player.immune || player.immuneTime > 0) return;
            int dmg = (int)(npc.damage * 1.5f * enrage);
            player.Hurt(PlayerDeathReason.ByNPC(npc.whoAmI), dmg, Math.Sign(hitPos.X - player.Center.X));
        }

        #endregion

        #region Movement

        private static void UpdateMovement(Hierophant boss, Player player, float enrage) {
            if (!boss.Jumping)
                UpdateGroundMovement(boss, player, enrage);
            else
                UpdateJumpMovement(boss, player);
        }

        private static void UpdateGroundMovement(Hierophant boss, Player player, float enrage) {
            NPC npc = boss.NPC;
            int onTileCount = 0;
            foreach (var leg in boss.Legs) {
                if (leg.OnTile) onTileCount++;
            }

            bool grounded = onTileCount >= 4;
            if (grounded && boss.JumpFlag) {
                boss.JumpFlag = false;
                if (npc.velocity.Y > 0f) npc.velocity.Y = 0f;
            }

            if (grounded || CheckSolidTile(npc.getRect())) {
                if (player.Center.Y + 300f * npc.scale < npc.Center.Y && boss.JumpCooldown <= -360) {
                    boss.Jumping = true;
                    npc.velocity = new Vector2(
                        0.008f * (player.Center.X - npc.Center.X) / npc.scale,
                        MathF.Max((player.Center.Y - npc.Center.Y) / npc.scale * 0.06f, -24f)
                    ) * npc.scale;
                    boss.JumpCooldown = 240;
                }

                float yOffset = -160f * npc.scale;
                if (npc.Center.Y - yOffset + 160f * npc.scale * npc.scale > player.Center.Y) {
                    if (npc.velocity.Y > 1.5f * npc.scale) npc.velocity.Y = 1.5f * npc.scale;
                    if (npc.velocity.Y > 0f) npc.velocity.Y *= 0.88f;
                }

                float vFactor = 0.15f;
                float yDist = MathF.Abs(npc.Center.Y + yOffset - player.Center.Y);
                if (yDist > 200f * npc.scale) vFactor = 0.8f;
                if (yDist < 20f * npc.scale) { vFactor = 0f; npc.velocity.Y *= 0.85f; }
                vFactor *= npc.scale;

                if (MathF.Abs(yOffset + player.Center.Y - npc.Center.Y) > 30f * npc.scale) {
                    if (player.Center.Y + yOffset > npc.Center.Y) {
                        npc.velocity.Y += 0.3f * enrage * vFactor;
                    }
                    else {
                        bool canGoUp = true, mustGoDown = false;
                        foreach (var leg in boss.Legs) {
                            if (leg.OnTile && leg.StandPoint.Y > npc.Center.Y + 180f * npc.scale) canGoUp = false;
                            if (leg.OnTile && leg.StandPoint.Y > npc.Center.Y + 220f * npc.scale) mustGoDown = true;
                        }

                        if (canGoUp || CheckSolidTile(npc.getRect()))
                            npc.velocity.Y -= 0.45f * enrage * vFactor;
                        else if (mustGoDown)
                            npc.velocity.Y += 1.5f * enrage * vFactor;
                    }
                }
            }
            else {
                npc.velocity.Y += 0.35f;
                if (npc.velocity.Y > 10f) npc.velocity.Y = 10f;
            }

            // 巨物感：水平加速更慢但更稳定
            if (Distance(npc.Center, player.Center) > 120f * npc.scale) {
                npc.velocity.X += Math.Sign(player.Center.X - npc.Center.X) * 0.1f * enrage * npc.scale;
            }
            // 限制最大水平速度以体现重量感
            float maxHSpeed = 4f * npc.scale * enrage;
            npc.velocity.X = MathHelper.Clamp(npc.velocity.X, -maxHSpeed, maxHSpeed);
        }

        private static void UpdateJumpMovement(Hierophant boss, Player player) {
            NPC npc = boss.NPC;
            int onTileCount = 0;
            foreach (var leg in boss.Legs) {
                if (leg.OnTile) onTileCount++;
            }

            bool grounded = onTileCount > 3;
            var ctrl = boss.CombatController;

            if (ctrl.CurrentSlash != SlashPhase.SlamDown || ctrl.SlashTimer <= 0) {
                if (((npc.velocity.Y > 0f) || npc.Center.Y < player.Center.Y) && grounded) {
                    boss.Jumping = false;
                    npc.velocity *= 0f;
                }

                if (boss.JumpCooldown < 20
                    || (npc.velocity.Y > 0f && CheckSolidTile(npc.getRect()))
                    || npc.velocity.Y > 2f) {
                    boss.Jumping = false;
                    npc.velocity *= 0f;
                }
            }

            boss.JumpFlag = true;
        }

        #endregion

        #region Direction

        private static void UpdateDirection(Hierophant boss) {
            NPC npc = boss.NPC;
            if (npc.velocity.X > 0f) {
                if (boss.Direction == -1) npc.rotation += MathHelper.Pi;
                boss.Direction = 1;
            }
            if (npc.velocity.X < 0f) {
                if (boss.Direction == 1) npc.rotation += MathHelper.Pi;
                boss.Direction = -1;
            }
        }

        #endregion

        #region Helpers

        internal int GetSlashMaxDuration() {
            return CurrentSlash switch {
                SlashPhase.CrossSlash => CrossSlashDuration,
                SlashPhase.SlamDown => SlamDuration,
                _ => SlashDuration,
            };
        }

        #endregion

        #region Networking

        public void NetSend(System.IO.BinaryWriter writer) {
            writer.Write(SlashCooldown);
            writer.Write((byte)CurrentSlash);
            writer.Write(SlashTimer);
        }

        public void NetReceive(System.IO.BinaryReader reader) {
            SlashCooldown = reader.ReadSingle();
            CurrentSlash = (SlashPhase)reader.ReadByte();
            SlashTimer = reader.ReadInt32();
        }

        #endregion
    }
}
