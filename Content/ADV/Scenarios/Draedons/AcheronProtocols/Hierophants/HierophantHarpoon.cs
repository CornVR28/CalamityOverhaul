using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants.HierophantUtils;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Hierophants
{
    /// <summary>
    /// 虫圣的鱼叉投射体 NPC
    /// </summary>
    internal class HierophantHarpoon : ModNPC
    {
        private const string TexturePath = "CalamityOverhaul/Content/ADV/Scenarios/Draedons/AcheronProtocols/Hierophants/Harpoon";
        public override string Texture => TexturePath;

        public bool OnLauncher = true;
        public int BackTimer;
        public int PullCooldown;
        public Vector2 StoredVelocity;
        public bool Stuck;
        private bool _initFlag = true;

        private NPC Owner => GetOwnerNpc();

        public override void SetStaticDefaults() {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }

        public override void SetDefaults() {
            NPC.width = 30;
            NPC.height = 30;
            NPC.damage = 32;
            NPC.dontTakeDamage = true;
            NPC.lifeMax = 1400;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = null;
            NPC.value = 0f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.dontCountMe = true;
            NPC.timeLeft *= 5;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            NPC owner = Owner;
            return owner != null && owner.boss && !OnLauncher;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) {
            target.AddBuff(BuffID.Electrified, 180);
        }

        public override void AI() {
            PullCooldown--;

            if (NPC.localAI[1]++ == 0) {
                if (NPC.velocity.X == 0f) NPC.velocity.X = 0.02f;
                if (NPC.velocity.Y == 0f) NPC.velocity.Y = 0.02f;
            }

            NPC owner = Owner;
            if (owner == null || !owner.active) {
                NPC.active = false;
                return;
            }

            NPC.scale = owner.scale;
            NPC.damage = owner.damage;

            if (owner.ModNPC is not Hierophant machine) {
                NPC.active = false;
                return;
            }

            if (OnLauncher) {
                HandleOnLauncher(machine);
            }
            else {
                HandleDetached(machine, owner);
            }
        }

        private void HandleOnLauncher(Hierophant machine) {
            NPC.Center = machine.HarpoonTipPos;
            NPC.rotation = machine.Harpoon.Seg2Rot;
            Stuck = false;
            NPC.velocity *= 0f;
            StoredVelocity *= 0f;
        }

        private void HandleDetached(Hierophant machine, NPC owner) {
            if (StoredVelocity == Vector2.Zero) {
                StoredVelocity = NPC.velocity;
            }

            NPC.rotation = (NPC.Center - machine.Harpoon.Seg1End).ToRotation();

            if (!Stuck && BackTimer-- < 0) {
                NPC.noTileCollide = true;
                NPC.velocity += (machine.HarpoonTipPos - NPC.Center).SafeNormalize() * 8f * NPC.scale;
                NPC.velocity *= 0.9f;

                if (Distance(NPC.Center, machine.HarpoonTipPos) <= NPC.velocity.Length() + 6f) {
                    OnLauncher = true;
                    NPC.velocity *= 0f;
                }
            }
            else if (owner.HasValidTarget) {
                HandleStickLogic(machine, owner);
            }
        }

        private void HandleStickLogic(Hierophant machine, NPC owner) {
            Player target = owner.target.ToPlayer();

            if (NPC.noTileCollide && Distance(owner.Center, target.Center) > 500f) {
                if (Distance(owner.Center, NPC.Center) > 600f
                    && Distance(NPC.Center, target.Center) < 400f
                    && CheckSolidTile(NPC.getRect())) {
                    if (!Stuck && !machine.Jumping && PullCooldown <= 0) {
                        PullCooldown = 12 * 60;
                        Stuck = true;
                        NPC.Center += StoredVelocity.SafeNormalize() * 40f;
                        SoundEngine.PlaySound(SoundID.Item10, NPC.Center);
                        _initFlag = false;
                    }
                }
            }

            if (Stuck) {
                NPC.velocity *= 0f;
                if (Distance(owner.Center, NPC.Center) > 400f) {
                    owner.ai[2] = 2f;
                    machine.Jumping = true;
                    machine.JumpCooldown = 100;
                    BackTimer = 5;
                }
                else {
                    BackTimer = -1;
                    Stuck = false;
                    machine.JumpCooldown = 100;
                    machine.Jumping = false;
                }
            }
        }

        public override bool CheckActive() {
            NPC owner = Owner;
            return owner == null || !owner.active;
        }

        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot,
            ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox) {
            npcHitbox = npcHitbox.Center.ToVector2().GetRectCentered(npcHitbox.Width * NPC.scale, npcHitbox.Height * NPC.scale);
            return true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            if (OnLauncher) return false;

            NPC owner = Owner;
            if (owner?.ModNPC is not Hierophant machine) return false;

            string basePath = "CalamityOverhaul/Content/ADV/Scenarios/Draedons/AcheronProtocols/Hierophants/";
            Texture2D harpoonOutline = RequestTex(basePath + "HarpoonOutline");
            Texture2D chainTex = RequestTex(basePath + "HarpoonChain");
            Texture2D harpoonTex = NPC.GetNpcTexture();

            Vector2 chainEnd = machine.HarpoonTipPos - machine.Harpoon.Seg2Rot.ToRotationVector2() * 60f * NPC.scale;
            DrawChain(NPC.Center, chainEnd, 18, chainTex, Color.White);

            SpriteEffects dirFlip = machine.Direction > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
            Vector2 origin = new(70, harpoonTex.Height / 2f);

            for (float r = 0; r <= 360; r += 60) {
                Vector2 offset = MathHelper.ToRadians(r).ToRotationVector2() * 2f;
                Main.EntitySpriteDraw(harpoonOutline, offset + NPC.Center - Main.screenPosition, null,
                    Color.OrangeRed, NPC.rotation, origin, NPC.scale, dirFlip);
            }

            Main.EntitySpriteDraw(harpoonTex, NPC.Center - Main.screenPosition, null,
                drawColor, NPC.rotation, origin, NPC.scale, dirFlip);

            return false;
        }

        private NPC GetOwnerNpc() {
            int index = (int)NPC.ai[0];
            if (index < 0 || index >= Main.npc.Length) return null;
            NPC npc = Main.npc[index];
            return npc.active ? npc : null;
        }
    }
}
