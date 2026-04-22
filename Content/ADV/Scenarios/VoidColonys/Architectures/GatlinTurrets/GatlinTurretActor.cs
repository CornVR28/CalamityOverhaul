using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TimeShift;
using CalamityOverhaul.Content.GoreEntity;
using CalamityOverhaul.Content.HackTimes;
using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.GatlinTurrets
{
    /// <summary>
    /// 虚空聚落加特林炮台
    /// 由底座+可旋转枪身两部分组成，枪身绕底座上方的枢轴旋转瞄准最近的玩家
    /// 玩家进入触发半径后开始蓄势，再过短暂延迟后连续点射，保持节奏感
    /// 仅在过去时代显现与开火，与整个建筑群的时空扭曲视觉保持同步
    /// </summary>
    internal class GatlinTurretActor : Actor, IHackableTurret
    {
        /// <summary>默认静止朝向，true=朝左，同步用于初始化静止朝向</summary>
        [SyncVar]
        public bool InitialFaceLeft;

        //枪架凸台相对于底座左上角的位置，即枪身旋转的世界枢轴点
        private static float PedestalMountLocalX => 210f;
        private static float PedestalMountLocalY => 8f;

        //枪身贴图上枪身的旋转枢轴（后端枪栓附近）
        private static float GunPivotLocalX => 55f;
        private static float GunPivotLocalY => 55f;

        //枪身贴图上枪口位置（前端枪管最末端）
        private static float GunMuzzleLocalX => 262f;
        private static float GunMuzzleLocalY => 50f;

        //触发玩家瞄准的距离，贴近用户指定的200像素
        private static float TriggerRangeSq => 1200f * 1200f;
        //锁定后的持续射程，离开触发距离仍会追射一段距离再松手
        private static float DisengageRangeSq => 1660f * 1660f;

        //锁定后蓄势再开火的帧数
        private const int LockOnDelay = 25;
        //连发子弹间隔帧数，加特林风格短间隔
        private const int FireInterval = 4;
        //一次爆发的子弹数
        private const int BurstCount = 8;
        //一次爆发后的冷却帧数
        private const int BurstCooldown = 40;
        //枪身旋转最大步长（弧度/帧），贴近机械感的逐步跟踪
        private const float RotationStep = 0.07f;

        //子弹伤害与初速
        private const int BulletDamage = 28;
        private const float BulletSpeed = 18f;

        //可见度阈值：以下时不触发瞄准与开火，保持与建筑凝实感一致
        private const float ActiveVisibility = 0.85f;

        /// <summary>当前可见度，与其他Architecture一致由TickVisibility推进</summary>
        private float visibility;

        /// <summary>枪身当前朝向（弧度，0表示朝右水平）</summary>
        private float currentRotation;
        /// <summary>是否已按InitialFaceLeft初始化过currentRotation</summary>
        private bool rotationInitialized;

        /// <summary>锁定的目标玩家whoAmI，-1表示无目标</summary>
        private int lockedPlayer = -1;
        /// <summary>锁定后的蓄势倒计时</summary>
        private int lockOnDelay;
        /// <summary>本轮爆发剩余子弹数</summary>
        private int burstRemaining;
        /// <summary>下一发子弹的间隔计时</summary>
        private int fireTimer;
        /// <summary>爆发后的冷却计时</summary>
        private int burstCooldown;
        /// <summary>枪管旋转视觉角度（不影响射击方向）</summary>
        private float barrelSpin;
        /// <summary>开火后枪口回坐位移</summary>
        private float recoil;

        /// <summary>底座世界像素尺寸由贴图决定</summary>
        private bool pedestalSized;

        /// <summary>电路失效剩余帧数：大于0时炮台整体停摆</summary>
        private int circuitDisabledFrames;
        /// <summary>是否由过载协议引发</summary>
        private bool circuitOverloaded;

        public override void OnSpawn(params object[] args) {
            DrawLayer = ActorDrawLayer.AfterTiles;
            ApplyPedestalSize();
        }

        public override void AI() {
            if (!ApplyPedestalSize()) return;

            if (!VoidColony.Active) {
                ActorLoader.KillActor(WhoAmI);
                return;
            }

            //骇客时间冻结：完全暂停炮台AI与开火节奏，但保留可见度演算让蟍烨过渡不突兑
            if (HackTimeFreeze.IsActive) {
                Velocity = Vector2.Zero;
                ArchitectureWarpDraw.TickVisibility(ref visibility);
                return;
            }

            ArchitectureWarpDraw.TickVisibility(ref visibility);

            //首次拿到同步字段后将当前朝向初始化为静止方向，避免开局从0弧度猛转
            if (!rotationInitialized) {
                currentRotation = InitialFaceLeft ? MathHelper.Pi : 0f;
                rotationInitialized = true;
            }

            Velocity = Vector2.Zero;

            //可见度不足或尚未处于过去时代时，枪身回归静止朝向且不执行射击逻辑
            bool activeEra = VoidTimeShiftSystem.InPast && visibility >= ActiveVisibility;
            if (!activeEra) {
                float restRot = InitialFaceLeft ? MathHelper.Pi : 0f;
                currentRotation = StepAngle(currentRotation, restRot, RotationStep * 0.5f);
                lockedPlayer = -1;
                lockOnDelay = 0;
                burstRemaining = 0;
                fireTimer = 0;
                if (burstCooldown > 0) burstCooldown--;
                if (recoil > 0f) recoil = MathF.Max(0f, recoil - 0.5f);
                return;
            }

            //电路失效期间：取消锁定、冷却所有射击计时、只衰减后坐
            if (circuitDisabledFrames > 0) {
                circuitDisabledFrames--;
                if (circuitDisabledFrames == 0) circuitOverloaded = false;
                lockedPlayer = -1;
                lockOnDelay = 0;
                burstRemaining = 0;
                fireTimer = 0;
                burstCooldown = Math.Max(burstCooldown, 10);
                if (recoil > 0f) recoil = MathF.Max(0f, recoil - 0.5f);
                return;
            }

            UpdateTargeting();
            UpdateRotationTracking();
            UpdateFireCycle();
            UpdateVisualEffects();
        }

        private bool ApplyPedestalSize() {
            if (pedestalSized) return true;
            Texture2D pedestal = GatlinTurretAsset.GatlinPedestal;
            if (pedestal == null) return false;
            Texture2D gun = GatlinTurretAsset.Gatlin;
            //Position已由NewActor设置并自动网络同步，这里只负责贴图尺寸与绘制扩展
            Width = pedestal.Width;
            Height = pedestal.Height;
            //扩展绘制范围以便枪身旋转后不被裁剪
            int extend = gun != null ? Math.Max(gun.Width, gun.Height) : Math.Max(Width, Height);
            DrawExtendMode = Math.Max(Math.Max(Width, Height), extend + 64);
            pedestalSized = true;
            return true;
        }

        /// <summary>世界坐标下的枪身枢轴（底座上的凸台），以Position为基准</summary>
        private Vector2 MountWorld => Position + new Vector2(PedestalMountLocalX, PedestalMountLocalY);

        /// <summary>
        /// 锁定/解锁目标玩家，使用平方距离避免开方
        /// </summary>
        private void UpdateTargeting() {
            Vector2 mount = MountWorld;
            //已有锁定时放宽至脱锁距离，模拟惯性跟踪
            if (lockedPlayer >= 0 && lockedPlayer < Main.maxPlayers) {
                Player p = Main.player[lockedPlayer];
                if (!p.active || p.dead) {
                    lockedPlayer = -1;
                }
                else {
                    float dSq = Vector2.DistanceSquared(p.Center, mount);
                    if (dSq > DisengageRangeSq) lockedPlayer = -1;
                }
            }

            if (lockedPlayer < 0) {
                int best = -1;
                float bestSq = TriggerRangeSq;
                for (int i = 0; i < Main.maxPlayers; i++) {
                    Player p = Main.player[i];
                    if (!p.active || p.dead) continue;
                    float dSq = Vector2.DistanceSquared(p.Center, mount);
                    if (dSq < bestSq) {
                        bestSq = dSq;
                        best = i;
                    }
                }
                if (best >= 0) {
                    lockedPlayer = best;
                    lockOnDelay = LockOnDelay;
                    burstRemaining = 0;
                    burstCooldown = 0;
                    fireTimer = 0;
                }
            }
        }

        /// <summary>
        /// 以锁定玩家为目标，平滑追踪枪身朝向
        /// </summary>
        private void UpdateRotationTracking() {
            float targetRot;
            if (lockedPlayer >= 0) {
                Player p = Main.player[lockedPlayer];
                Vector2 to = p.Center - MountWorld;
                targetRot = to.ToRotation();
            }
            else {
                targetRot = InitialFaceLeft ? MathHelper.Pi : 0f;
            }
            currentRotation = StepAngle(currentRotation, targetRot, RotationStep);
        }

        /// <summary>
        /// 推进锁定→蓄势→爆发→冷却的射击循环
        /// </summary>
        private void UpdateFireCycle() {
            if (lockedPlayer < 0) {
                if (burstCooldown > 0) burstCooldown--;
                return;
            }

            //蓄势阶段：旋转跟踪但不开火
            if (lockOnDelay > 0) {
                lockOnDelay--;
                return;
            }

            //爆发冷却
            if (burstCooldown > 0) {
                burstCooldown--;
                return;
            }

            //开启一轮爆发
            if (burstRemaining <= 0) {
                burstRemaining = BurstCount;
                fireTimer = 0;
            }

            if (fireTimer > 0) {
                fireTimer--;
                return;
            }

            //仅在服务器/单机端实际生成子弹
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                FireBullet();
            }

            //开火特效所有端都触发
            SoundEngine.PlaySound(CWRSound.Gun_AUTO_Shoot with { Volume = 0.6f, Pitch = 0.1f }, MountWorld);
            SpawnMuzzleEffects();
            EjectCasing();
            recoil = 6f;
            barrelSpin += 0.8f;

            burstRemaining--;
            if (burstRemaining <= 0) {
                burstCooldown = BurstCooldown;
            }
            else {
                fireTimer = FireInterval;
            }
        }

        /// <summary>
        /// 枪口喷焰视觉：亮光Dust与烟雾Gore，仅在客户端生成
        /// </summary>
        private void SpawnMuzzleEffects() {
            if (Main.dedServ) return;

            Vector2 mount = MountWorld;
            Vector2 forward = currentRotation.ToRotationVector2();
            Vector2 sideNormal = new Vector2(-forward.Y, forward.X);
            //枪身贴图垂直翻转时侧向也要跟着翻，保证弹壳往顶部方向抛
            if (MathF.Cos(currentRotation) < 0f) sideNormal = -sideNormal;

            Vector2 muzzle = mount + forward * (GunMuzzleLocalX - GunPivotLocalX)
                + sideNormal * MathF.Abs(GunMuzzleLocalY - GunPivotLocalY);
            muzzle += VaultUtils.RandVr(32);
            muzzle.Y -= 20;

            //橙黄火焰Dust锥形喷射
            for (int i = 0; i < 10; i++) {
                Vector2 vel = forward.RotatedByRandom(0.35f) * Main.rand.NextFloat(3f, 7f);
                Dust d = Dust.NewDustPerfect(muzzle, DustID.Torch, vel, 0, default, Main.rand.NextFloat(1.2f, 1.8f));
                d.noGravity = true;
                d.fadeIn = 1.2f;
            }
            //少量烟雾Gore作为发射后残留
            if (Main.rand.NextBool(2)) {
                int gore = Gore.NewGore(new EntitySource_WorldEvent("GatlinTurret_Muzzle"),
                    muzzle, forward * Main.rand.NextFloat(0.6f, 1.4f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    GoreID.Smoke1 + Main.rand.Next(3), Main.rand.NextFloat(0.6f, 0.9f));
                if (gore >= 0 && gore < Main.maxGore) {
                    Main.gore[gore].alpha = 80;
                }
            }
            //亮黄闪光核心
            Dust flash = Dust.NewDustPerfect(muzzle, DustID.GoldFlame, forward * 1.5f, 0, Color.White, 1.6f);
            flash.noGravity = true;

            Lighting.AddLight(muzzle, 1.3f, 0.85f, 0.3f);
        }

        /// <summary>
        /// 抛出弹壳Gore，位置位于枪身弹膛处，方向与枪身垂直且朝上
        /// 受CWRServerConfig.EnableCasingsEntity开关控制
        /// </summary>
        private void EjectCasing() {
            if (Main.dedServ) return;
            if (!CWRServerConfig.Instance.EnableCasingsEntity) return;

            Vector2 mount = MountWorld;
            Vector2 forward = currentRotation.ToRotationVector2();
            Vector2 sideNormal = new Vector2(-forward.Y, forward.X);
            //枪身上下翻转时让弹壳抛射方向跟着翻，确保始终从枪身顶面弹出
            if (MathF.Cos(currentRotation) < 0f) sideNormal = -sideNormal;

            //弹膛位于枢轴后方稍靠顶部的位置，比照枪身贴图上弹仓凸出的范围
            Vector2 chamber = mount + forward * -8f + sideNormal * -6f;
            //抛射速度：向上偏射，略带后向分量，随机抖动制造节奏感
            Vector2 vel = sideNormal * Main.rand.NextFloat(2.2f, 4.2f)
                + -forward * Main.rand.NextFloat(0.6f, 1.6f)
                - Vector2.UnitY * Main.rand.NextFloat(0.4f, 1.2f);

            Gore.NewGore(new EntitySource_WorldEvent("GatlinTurret_Casing"),
                chamber, vel, CaseGore.PType, Main.rand.NextFloat(0.9f, 1.15f));
        }

        /// <summary>
        /// 实际生成一发敌意子弹，发射位置位于枪口，方向为当前枪身朝向
        /// </summary>
        private void FireBullet() {
            IEntitySource src = new EntitySource_WorldEvent("VoidColony_GatlinTurret");
            for (int i = 0; i < 6; i++) {
                Vector2 mount = MountWorld;
                Vector2 forward = currentRotation.ToRotationVector2();
                Vector2 muzzle = mount + forward * (GunMuzzleLocalX - GunPivotLocalX)
                    + new Vector2(-forward.Y, forward.X) * (GunMuzzleLocalY - GunPivotLocalY);
                Vector2 velocity = forward * BulletSpeed;
                //轻微散布，避免连发子弹完全重合
                velocity = velocity.RotatedByRandom(0.02f);

                muzzle += VaultUtils.RandVr(32);
                muzzle -= velocity.UnitVector() * 16;

                int proj = Projectile.NewProjectile(src, muzzle, velocity,
                ModContent.ProjectileType<GatlinBullet>(),
                BulletDamage, 4f, Main.myPlayer);
                if (proj.TryGetProjectile(out var projIns)) {
                    projIns.rotation = velocity.ToRotation() + MathHelper.PiOver2;
                }
            }
        }

        /// <summary>
        /// 推进视觉相关的辅助状态
        /// </summary>
        private void UpdateVisualEffects() {
            if (recoil > 0f) recoil = MathF.Max(0f, recoil - 1f);
            //锁定时枪管以较快速度空转，制造蓄势感
            if (lockedPlayer >= 0) barrelSpin += 0.25f;
            if (barrelSpin > MathHelper.TwoPi) barrelSpin -= MathHelper.TwoPi;
        }

        /// <summary>
        /// 让current朝target以最大步长step旋转，选择更短的方向
        /// </summary>
        private static float StepAngle(float current, float target, float step) {
            float delta = MathHelper.WrapAngle(target - current);
            if (MathF.Abs(delta) <= step) return target;
            return current + MathF.Sign(delta) * step;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            Texture2D pedestal = GatlinTurretAsset.GatlinPedestal;
            Texture2D gun = GatlinTurretAsset.Gatlin;
            if (pedestal == null || gun == null) return false;
            if (!ArchitectureWarpDraw.ShouldDraw(visibility)) return false;

            float warp = ArchitectureWarpDraw.ComputeWarp();
            //骇客时间高亮强度，底座与枪身共用
            float hackMark = ComputeHackHighlight();

            //底座走与其他建筑一致的扭曲shader
            Vector2 pedestalDrawPos = Position - Main.screenPosition;
            pedestalDrawPos.Y += 2;//向下偏移2像素用于贴合地面
            ArchitectureWarpDraw.DrawWithShader(spriteBatch, pedestal, pedestalDrawPos, visibility, warp);
            //被扫描选中或悬停时在底座上再叠加一层骇客高亮
            if (hackMark > 0.01f) {
                DrawPedestalHackOverlay(spriteBatch, pedestal, pedestalDrawPos, hackMark);
            }

            //枪身绕枢轴旋转，单独普通绘制避免旋转传入shader增加参数
            Vector2 mountScreen = MountWorld - Main.screenPosition;
            //开火后坐：沿枪身反方向位移recoil像素
            Vector2 forward = currentRotation.ToRotationVector2();
            Vector2 drawCenter = mountScreen - forward * recoil;

            //枪身沿当前朝向的左右翻转，保证枪管顶部始终朝上（不影响射击方向）
            bool flipVertical = MathF.Cos(currentRotation) < 0f;
            SpriteEffects effects = flipVertical ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Vector2 origin = flipVertical
                ? new Vector2(GunPivotLocalX, gun.Height - GunPivotLocalY)
                : new Vector2(GunPivotLocalX, GunPivotLocalY);

            Color gunColor = Color.White * MathHelper.Clamp(visibility, 0f, 1f);

            //骇客时间高亮：被扫描选中或悬停时套用同一套灵异/实体高亮shader
            if (hackMark > 0.01f) {
                DrawGunWithHackShader(spriteBatch, gun, drawCenter, origin, currentRotation,
                    effects, gunColor, hackMark);
            }
            else {
                spriteBatch.Draw(gun, drawCenter, null, gunColor, currentRotation, origin, 1f, effects, 0f);
            }

            return false;
        }

        /// <summary>计算骇客时间高亮强度，选中=1、悬停=0.55，乘全局Intensity</summary>
        private float ComputeHackHighlight() {
            bool selected = ReferenceEquals(HackTime.CurrentScanTarget, this);
            bool hovered = ReferenceEquals(HackTimeTargeting.HoveredTurret, this);
            float baseMark = selected ? 1f : hovered ? 0.55f : 0f;
            return baseMark * HackTime.Intensity;
        }

        /// <summary>套用骇客高亮shader绘制枪身，给选中/悬停的炮台一致风格</summary>
        private static void DrawGunWithHackShader(SpriteBatch sb, Texture2D tex, Vector2 drawPos,
            Vector2 origin, float rotation, SpriteEffects effects, Color baseColor, float strength) {
            //炮台属于机械实体，套用NPC高亮shader而非灵异专用的撕裂shader
            Effect shader = HackTimeAssets.HackTimeNPCHighlight;
            if (shader == null) {
                sb.Draw(tex, drawPos, null, baseColor, rotation, origin, 1f, effects, 0f);
                return;
            }

            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));
            shader.Parameters["intensity"]?.SetValue(strength);
            shader.Parameters["isSelected"]?.SetValue(strength > 0.9f ? 1f : 0f);
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            sb.Draw(tex, drawPos, null, baseColor, rotation, origin, 1f, effects, 0f);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>底座骇客高亮叠加层：Additive方式补扫描线与脉冲光晟，不改变warp凝实感</summary>
        private static void DrawPedestalHackOverlay(SpriteBatch sb, Texture2D tex, Vector2 drawPos, float strength) {
            Effect shader = HackTimeAssets.HackTimeNPCHighlight;
            if (shader == null) return;

            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));
            shader.Parameters["intensity"]?.SetValue(strength);
            shader.Parameters["isSelected"]?.SetValue(strength > 0.9f ? 1f : 0f);
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            sb.Draw(tex, drawPos, null, Color.White * strength, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        #region IHackableTurret 实现

        Actor IHackableTurret.AsActor => this;

        Vector2 IScannable.WorldCenter => MountWorld;

        bool IScannable.IsValid => Active && VoidColony.Active && visibility > 0.05f;

        bool IScannable.IsHackable => true;

        int IScannable.ScanRowCount => 5;

        public bool IsCircuitDisabled => circuitDisabledFrames > 0;

        public int CircuitDisabledFrames => circuitDisabledFrames;

        void IScannable.BuildScanData(string[] labels, string[] values, Color[] colors) {
            labels[0] = HackTime.TurretScanName.Value;
            values[0] = HackTime.TurretScanGatlinName.Value;
            colors[0] = HackTheme.Danger;

            labels[1] = HackTime.TypeLabel.Value;
            values[1] = HackTime.TurretScanType.Value;
            colors[1] = HackTheme.Accent;

            labels[2] = HackTime.ThreatLabel.Value;
            values[2] = HackTime.ThreatHigh.Value;
            colors[2] = HackTheme.Uploading;

            labels[3] = HackTime.TurretScanPhase.Value;
            if (burstRemaining > 0) {
                values[3] = HackTime.TurretScanPhaseFiring.Value;
                colors[3] = HackTheme.Danger;
            }
            else if (lockOnDelay > 0) {
                values[3] = HackTime.TurretScanPhaseLocking.Value;
                colors[3] = HackTheme.Uploading;
            }
            else if (burstCooldown > 0) {
                values[3] = HackTime.TurretScanPhaseCooldown.Value;
                colors[3] = HackTheme.TextNormal;
            }
            else {
                values[3] = HackTime.TurretScanPhaseIdle.Value;
                colors[3] = HackTheme.TextBright;
            }

            labels[4] = HackTime.TurretScanCircuit.Value;
            if (circuitDisabledFrames > 0) {
                values[4] = circuitOverloaded
                    ? HackTime.TurretScanCircuitOverload.Value
                    : HackTime.TurretScanCircuitShorted.Value;
                colors[4] = HackTheme.Uploading;
            }
            else {
                values[4] = HackTime.TurretScanCircuitOnline.Value;
                colors[4] = HackTheme.Accent;
            }
        }

        public void ApplyShortCircuit(int frames, Player caster) {
            if (frames > circuitDisabledFrames) circuitDisabledFrames = frames;
        }

        public void ApplyCircuitOverload(int frames, Player caster) {
            if (frames > circuitDisabledFrames) circuitDisabledFrames = frames;
            circuitOverloaded = true;
        }

        #endregion
    }
}
