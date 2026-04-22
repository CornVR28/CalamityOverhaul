using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TimeShift;
using CalamityOverhaul.Content.HackTimes;
using InnoVault.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.LaserCannons
{
    /// <summary>
    /// 虚空聚落巨型激光炮台
    /// 悬浮于外缘浮岛上方，锁定玩家后经过漫长的蓄能阶段发射撕裂空间的巨型激光柱
    /// 蓄能时有细弱预警光柱从枪口指向玩家，进入发射阶段瞬间炸开成高压白紫柱体
    /// 配合ArchitectureWarp管线与GammaRayBeam着色器渲染，保证与整个聚落的时空演出同步
    /// </summary>
    internal class LaserCannonTurretActor : Actor, IHackableTurret
    {
        //==================== 同步状态 ====================

        /// <summary>静止时的默认朝向，true=朝左</summary>
        [SyncVar]
        public bool InitialFaceLeft;

        //==================== 可调参数（静态属性，便于热重载实时调整） ====================

        //底座上炮身枢轴的局部坐标（相对底座左上角的像素）
        /// <summary>炮身枢轴在底座贴图上的X像素</summary>
        public static float PedestalMountLocalX => 460f;
        /// <summary>炮身枢轴在底座贴图上的Y像素，位于底座偏上的枢臂凹槽</summary>
        public static float PedestalMountLocalY => 0f;

        //炮身贴图上用于旋转与发射的关键点
        /// <summary>炮身贴图上旋转枢轴的X像素（后端能量舱中心）</summary>
        public static float GunPivotLocalX => 180f;
        /// <summary>炮身贴图上旋转枢轴的Y像素（炮身中线）</summary>
        public static float GunPivotLocalY => 124f;
        /// <summary>炮身贴图上枪口的X像素（炮管末端）</summary>
        public static float GunMuzzleLocalX => 200f;
        /// <summary>炮身贴图上枪口的Y像素（对准炮身中线）</summary>
        public static float GunMuzzleLocalY => 120f;

        //锁定相关
        /// <summary>触发锁定距离，超过此距离不会主动挑衅玩家</summary>
        public static float TriggerRange => 1800f;
        /// <summary>已锁定后的脱锁距离，留一段缓冲避免玩家出入边缘反复切换</summary>
        public static float DisengageRange => 2400f;
        /// <summary>锁定期间枪身最大旋转步长（弧度/帧），慢步跟踪，重机械感</summary>
        public static float RotationStep => 0.018f;
        /// <summary>静止回位时的旋转步长</summary>
        public static float IdleRotationStep => 0.008f;

        //节奏控制
        /// <summary>蓄能阶段持续时间（帧），期间枪口延伸预警细光并逐步加宽</summary>
        public static int ChargeDuration => 80;
        /// <summary>激光发射阶段持续时间（帧），期间造成持续伤害</summary>
        public static int FireDuration => 240;
        /// <summary>发射结束后的冷却时间（帧），强制炮身解锁休整</summary>
        public static int CooldownDuration => 180;

        //光束几何
        /// <summary>激光最大射程，决定主光束贴图的拉伸长度</summary>
        public static float MaxBeamLength => 3200f;
        /// <summary>蓄能末尾时预警光束宽度（像素）</summary>
        public static float ChargeBeamWidth => 26f;
        /// <summary>发射阶段主光束宽度（像素），含脉动基线</summary>
        public static float FireBeamWidth => 96f;
        /// <summary>发射阶段核心强度基值</summary>
        public static float FireCoreIntensity => 1.4f;

        //伤害与判定
        /// <summary>发射期间每次造成伤害的帧间隔</summary>
        public static int DamageInterval => 20;
        /// <summary>单次伤害数值</summary>
        public static int BeamDamage => 160;
        /// <summary>判定宽度（像素），略小于可视宽度便于玩家紧贴边缘试探</summary>
        public static float HitboxWidth => 70f;

        //过去时代可见度阈值
        /// <summary>低于该可见度不会主动开火</summary>
        public static float ActiveVisibility => 0.88f;

        //==================== 运行时字段 ====================

        private float visibility;
        private float currentRotation;
        private bool rotationInitialized;

        private int lockedPlayer = -1;

        /// <summary>0=静默 1=蓄能 2=发射 3=冷却</summary>
        private int phase;
        /// <summary>当前阶段剩余帧数</summary>
        private int phaseTimer;
        /// <summary>发射期间的伤害节拍</summary>
        private int damageTimer;

        /// <summary>展示用光束强度，驱动shader参数</summary>
        private float beamIntensity;
        /// <summary>展示用光束宽度</summary>
        private float beamWidth;
        /// <summary>展示用光束脉动因子</summary>
        private float beamPulse;

        /// <summary>开火后坐视觉位移</summary>
        private float recoil;

        private bool pedestalSized;

        /// <summary>电路失效剩余帧数：大于0时炮台整体停摆</summary>
        private int circuitDisabledFrames;
        /// <summary>是否由过载协议引发（用于扫描面板区分）</summary>
        private bool circuitOverloaded;

        //==================== 基础设施 ====================

        public override void OnSpawn(params object[] args) {
            DrawLayer = ActorDrawLayer.AfterTiles;
            ApplyPedestalSize();
        }

        private bool ApplyPedestalSize() {
            if (pedestalSized) return true;
            Texture2D pedestal = LaserCannonAsset.LaserCannonPedestal;
            if (pedestal == null) return false;
            Texture2D gun = LaserCannonAsset.LaserCannon;
            Width = pedestal.Width;
            Height = pedestal.Height;
            //扩展绘制范围以容纳长激光柱
            int extend = (int)MaxBeamLength + 512;
            if (gun != null) extend = Math.Max(extend, Math.Max(gun.Width, gun.Height) + 256);
            DrawExtendMode = Math.Max(Math.Max(Width, Height), extend);
            pedestalSized = true;
            return true;
        }

        /// <summary>世界像素下的炮身枢轴</summary>
        private Vector2 MountWorld => Position + new Vector2(PedestalMountLocalX, PedestalMountLocalY);

        /// <summary>世界像素下的枪口，沿当前朝向由枢轴外推
        /// 炮身在朝左时会做垂直翻转绘制(FlipVertically)，此时贴图上枪口的有效Y会镜像
        /// 这里同步对perpOffset取反，确保激光起点始终对齐实际枪管开口</summary>
        private Vector2 MuzzleWorld {
            get {
                Vector2 mount = MountWorld;
                Vector2 forward = currentRotation.ToRotationVector2();
                Vector2 up = new(-forward.Y, forward.X);
                float parallel = GunMuzzleLocalX - GunPivotLocalX;
                float perp = GunMuzzleLocalY - GunPivotLocalY;
                //与PreDraw中flipVertical保持同步：cos<0时贴图垂直翻转，垂直偏移需取反
                if (MathF.Cos(currentRotation) < 0f) perp = -perp;
                return mount + forward * parallel + up * perp;
            }
        }

        //==================== AI主循环 ====================

        public override void AI() {
            if (!ApplyPedestalSize()) return;

            if (!VoidColony.Active) {
                ActorLoader.KillActor(WhoAmI);
                return;
            }

            //骇客时间冻结：与NPC、灵异目标保持一致，完全暂停AI演出
            if (HackTimeFreeze.IsActive) {
                Velocity = Vector2.Zero;
                ArchitectureWarpDraw.TickVisibility(ref visibility);
                return;
            }

            ArchitectureWarpDraw.TickVisibility(ref visibility);

            if (!rotationInitialized) {
                currentRotation = InitialFaceLeft ? MathHelper.Pi : 0f;
                rotationInitialized = true;
            }

            Velocity = Vector2.Zero;

            bool activeEra = VoidTimeShiftSystem.InPast && visibility >= ActiveVisibility;
            if (!activeEra) {
                //不在过去时代或尚未凝实时，解锁重置并缓慢归位
                AbortBeam();
                float restRot = InitialFaceLeft ? MathHelper.Pi : 0f;
                currentRotation = StepAngle(currentRotation, restRot, IdleRotationStep);
                DecayVisuals();
                return;
            }

            //电路失效期间全面停摆：取消正在进行的发射、只做视觉抖动
            if (circuitDisabledFrames > 0) {
                circuitDisabledFrames--;
                if (circuitDisabledFrames == 0) circuitOverloaded = false;
                AbortBeam();
                DecayVisuals();
                return;
            }

            UpdateTargeting();
            UpdateRotationTracking();
            UpdatePhaseMachine();
            UpdateBeamVisuals();
            UpdateBeamDamage();

            if (recoil > 0f) recoil = MathF.Max(0f, recoil - 0.6f);
        }

        private void AbortBeam() {
            phase = 0;
            phaseTimer = 0;
            damageTimer = 0;
            lockedPlayer = -1;
        }

        private void DecayVisuals() {
            beamIntensity = MathF.Max(0f, beamIntensity - 0.08f);
            beamWidth = MathHelper.Lerp(beamWidth, 0f, 0.15f);
            beamPulse = MathHelper.Lerp(beamPulse, 0f, 0.2f);
            if (recoil > 0f) recoil = MathF.Max(0f, recoil - 0.6f);
        }

        //==================== 目标选择 ====================

        private void UpdateTargeting() {
            //发射期间强制保持锁定，不做切换
            if (phase == 2) return;

            Vector2 mount = MountWorld;
            float triggerSq = TriggerRange * TriggerRange;
            float disengageSq = DisengageRange * DisengageRange;

            if (lockedPlayer >= 0 && lockedPlayer < Main.maxPlayers) {
                Player p = Main.player[lockedPlayer];
                if (!p.active || p.dead) {
                    lockedPlayer = -1;
                }
                else if (Vector2.DistanceSquared(p.Center, mount) > disengageSq) {
                    //出圈才解除，避免频繁切换
                    if (phase == 0) lockedPlayer = -1;
                }
            }

            if (lockedPlayer < 0) {
                int best = -1;
                float bestSq = triggerSq;
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
                }
            }
        }

        //==================== 枪身跟踪 ====================

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

            //发射阶段减半旋转速度，营造沉重的扫射感
            float step = RotationStep;
            if (phase == 2) step *= 0.45f;
            currentRotation = StepAngle(currentRotation, targetRot, step);
        }

        //==================== 阶段机 ====================

        private void UpdatePhaseMachine() {
            switch (phase) {
                case 0:
                    //静默：有锁定即进入蓄能
                    if (lockedPlayer >= 0) {
                        phase = 1;
                        phaseTimer = ChargeDuration;
                        SoundEngine.PlaySound(SoundID.Zombie104 with {
                            Volume = 0.8f, Pitch = -0.3f, MaxInstances = 2,
                        }, MountWorld);
                    }
                    break;
                case 1:
                    //蓄能：计时结束进入发射
                    phaseTimer--;
                    if (phaseTimer <= 0) {
                        phase = 2;
                        phaseTimer = FireDuration;
                        damageTimer = 0;
                        recoil = 18f;
                        SoundEngine.PlaySound(SoundID.Zombie118 with {
                            Volume = 0.95f, Pitch = -0.4f, MaxInstances = 2,
                        }, MuzzleWorld);
                        SoundEngine.PlaySound(SoundID.Item122 with {
                            Volume = 0.8f, Pitch = -0.2f,
                        }, MuzzleWorld);
                    }
                    break;
                case 2:
                    //发射
                    phaseTimer--;
                    if (phaseTimer <= 0) {
                        phase = 3;
                        phaseTimer = CooldownDuration;
                        SoundEngine.PlaySound(SoundID.Item62 with {
                            Volume = 0.7f, Pitch = -0.1f,
                        }, MuzzleWorld);
                    }
                    break;
                case 3:
                    //冷却：结束后返回静默，若玩家仍在圈内会再次进入蓄能
                    phaseTimer--;
                    if (phaseTimer <= 0) {
                        phase = 0;
                        //冷却结束时解除一次锁定，让UpdateTargeting重新判定是否仍有目标
                        if (lockedPlayer >= 0) {
                            Player p = Main.player[lockedPlayer];
                            float triggerSq = TriggerRange * TriggerRange;
                            if (!p.active || p.dead
                                || Vector2.DistanceSquared(p.Center, MountWorld) > triggerSq) {
                                lockedPlayer = -1;
                            }
                        }
                    }
                    break;
            }
        }

        //==================== 光束视觉推进 ====================

        private void UpdateBeamVisuals() {
            float targetIntensity = 0f;
            float targetWidth = 0f;
            switch (phase) {
                case 1: {
                    //蓄能：宽度从极细逐步增长到ChargeBeamWidth，强度同步推进
                    float t = 1f - phaseTimer / (float)Math.Max(1, ChargeDuration);
                    targetIntensity = MathHelper.Lerp(0.2f, 0.7f, t);
                    targetWidth = MathHelper.Lerp(4f, ChargeBeamWidth, t);
                    break;
                }
                case 2: {
                    float life = 1f - phaseTimer / (float)Math.Max(1, FireDuration);
                    float core = FireCoreIntensity;
                    if (life < 0.08f) {
                        //开火爆闪
                        float k = life / 0.08f;
                        core *= MathHelper.Lerp(2.2f, 1f, k);
                        targetWidth = MathHelper.Lerp(FireBeamWidth * 1.35f, FireBeamWidth, k);
                    }
                    else if (life > 0.94f) {
                        //收束：短促干脆地熄灭，直接把宽度和强度压到极低
                        float k = (life - 0.94f) / 0.06f;
                        core *= MathHelper.Lerp(1f, 0f, k);
                        targetWidth = MathHelper.Lerp(FireBeamWidth, 0f, k);
                    }
                    else {
                        targetWidth = FireBeamWidth;
                    }
                    targetIntensity = core;
                    break;
                }
                case 3: {
                    //冷却：立即让目标归零并使用更激进的插值速度，避免细线残留
                    targetIntensity = 0f;
                    targetWidth = 0f;
                    break;
                }
            }

            //平滑逼近(冷却阶段采用更快速度，让光束干脆消失)
            float intLerp = phase == 3 ? 0.7f : 0.35f;
            float widLerp = phase == 3 ? 0.7f : 0.4f;
            beamIntensity = MathHelper.Lerp(beamIntensity, targetIntensity, intLerp);
            beamWidth = MathHelper.Lerp(beamWidth, targetWidth, widLerp);

            //冷却阶段当宽度低到一定阈值就直接归零，杜绝极细残影
            if (phase == 3 && beamWidth < 3f) {
                beamWidth = 0f;
                beamIntensity = 0f;
            }

            //脉动基线
            float pulseTarget = phase == 2 ? 1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 18f) * 0.18f : 0.7f;
            beamPulse = MathHelper.Lerp(beamPulse, pulseTarget, 0.3f);
        }

        //==================== 伤害 ====================

        private void UpdateBeamDamage() {
            if (phase != 2) return;
            //服务器/单机负责实际判定，客户端跳过
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            damageTimer++;
            if (damageTimer < DamageInterval) return;
            damageTimer = 0;

            Vector2 start = MuzzleWorld;
            Vector2 forward = currentRotation.ToRotationVector2();
            Vector2 end = start + forward * MaxBeamLength;

            float w = HitboxWidth;

            for (int i = 0; i < Main.maxPlayers; i++) {
                Player p = Main.player[i];
                if (!p.active || p.dead) continue;
                float _ = 0f;
                if (!Terraria.Collision.CheckAABBvLineCollision(p.position, p.Size, start, end, w, ref _)) continue;

                int dir = p.Center.X < start.X ? -1 : 1;
                PlayerDeathReason reason = PlayerDeathReason.ByCustomReason(
                    NetworkText.FromLiteral(p.name + " 被虚空激光彻底汽化"));
                p.Hurt(reason, BeamDamage, dir);
            }
        }

        //==================== 工具 ====================

        private static float StepAngle(float current, float target, float step) {
            float delta = MathHelper.WrapAngle(target - current);
            if (MathF.Abs(delta) <= step) return target;
            return current + MathF.Sign(delta) * step;
        }

        //==================== 绘制 ====================

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            Texture2D pedestal = LaserCannonAsset.LaserCannonPedestal;
            Texture2D gun = LaserCannonAsset.LaserCannon;
            if (pedestal == null || gun == null) return false;
            if (!ArchitectureWarpDraw.ShouldDraw(visibility)) return false;

            float warp = ArchitectureWarpDraw.ComputeWarp();
            //骇客时间高亮强度，底座与枪身共用
            float hackMark = ComputeHackHighlight();
            //电路故障强度，被短路/过载时覆盖骇客高亮
            float faultMark = ComputeFaultMark();
            float faultMode = circuitOverloaded ? 1f : 0f;

            //底座使用聚落扭曲shader，使其和其它建筑同步凝实/崩解
            Vector2 pedestalDrawPos = Position - Main.screenPosition;
            ArchitectureWarpDraw.DrawWithShader(spriteBatch, pedestal, pedestalDrawPos, visibility, warp);
            //电路故障优先：整机表现断电与电弧，不再展示扫描高亮
            if (faultMark > 0.01f) {
                DrawPedestalFaultOverlay(spriteBatch, pedestal, pedestalDrawPos, faultMark, faultMode);
            }
            else if (hackMark > 0.01f) {
                DrawPedestalHackOverlay(spriteBatch, pedestal, pedestalDrawPos, hackMark);
            }

            //激光束：只有在蓄能/发射/收束阶段且可见度足够时绘制
            if (beamIntensity > 0.01f && beamWidth > 0.5f) {
                DrawLaserBeam(spriteBatch);
            }

            //炮身：围绕枢轴旋转，采用普通绘制避免把旋转传入shader
            Vector2 mountScreen = MountWorld - Main.screenPosition;
            Vector2 forward = currentRotation.ToRotationVector2();
            Vector2 drawCenter = mountScreen - forward * recoil;

            bool flipVertical = MathF.Cos(currentRotation) < 0f;
            SpriteEffects effects = flipVertical ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Vector2 origin = flipVertical
                ? new Vector2(GunPivotLocalX, gun.Height - GunPivotLocalY)
                : new Vector2(GunPivotLocalX, GunPivotLocalY);

            Color gunColor = Color.White * MathHelper.Clamp(visibility, 0f, 1f);

            //电路故障优先：整机故障滤镜替换常规绘制
            if (faultMark > 0.01f) {
                DrawGunWithFaultShader(spriteBatch, gun, drawCenter, origin, currentRotation,
                    effects, gunColor, faultMark, faultMode);
            }
            //骇客时间高亮：被扫描选中或悬停时，为炮身套用灵异/实体高亮shader
            else if (hackMark > 0.01f) {
                DrawGunWithHackShader(spriteBatch, gun, drawCenter, origin, currentRotation,
                    effects, gunColor, hackMark);
            }
            else {
                spriteBatch.Draw(gun, drawCenter, null, gunColor, currentRotation, origin, 1f, effects, 0f);
            }

            return false;
        }

        /// <summary>计算骇客时间高亮强度，选中=1、悬停=0.55，其他=0，并乘以全局强度</summary>
        private float ComputeHackHighlight() {
            bool selected = ReferenceEquals(HackTime.CurrentScanTarget, this);
            bool hovered = ReferenceEquals(HackTimeTargeting.HoveredTurret, this);
            float baseMark = selected ? 1f : hovered ? 0.55f : 0f;
            return baseMark * HackTime.Intensity;
        }

        /// <summary>
        /// 电路失效滤镜强度：失效期间为1，临近恢复时线性衰减，避免 shader 瞬间切断
        /// </summary>
        private float ComputeFaultMark() {
            if (circuitDisabledFrames <= 0) return 0f;
            //最后20帧平滑淡出
            return MathHelper.Clamp(circuitDisabledFrames / 20f, 0f, 1f);
        }

        /// <summary>
        /// 使用NPC高亮shader绘制炮身
        /// 炮台是机械实体，与NPC共用同一套高亮风格，与灵异目标的撕裂shader区分开
        /// </summary>
        private static void DrawGunWithHackShader(SpriteBatch sb, Texture2D tex, Vector2 drawPos,
            Vector2 origin, float rotation, SpriteEffects effects, Color baseColor, float strength) {
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

        /// <summary>
        /// 在底座warp绘制之上叠加一层骇客高亮
        /// 使用Additive混合，只补上扫描线/角光/脉冲色彩，不触发凝实感丢失
        /// </summary>
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

        /// <summary>
        /// 使用炮台故障shader绘制炮身（完整替换绘制）
        /// 短路 mode=0 冷蓝，过载 mode=1 热红，两极插值表现差异
        /// </summary>
        private static void DrawGunWithFaultShader(SpriteBatch sb, Texture2D tex, Vector2 drawPos,
            Vector2 origin, float rotation, SpriteEffects effects, Color baseColor, float strength, float mode) {
            Effect shader = HackTimeAssets.HackTurretCircuitFault;
            if (shader == null) {
                sb.Draw(tex, drawPos, null, baseColor, rotation, origin, 1f, effects, 0f);
                return;
            }

            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));
            shader.Parameters["intensity"]?.SetValue(strength);
            shader.Parameters["mode"]?.SetValue(mode);
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            sb.Draw(tex, drawPos, null, baseColor, rotation, origin, 1f, effects, 0f);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// 在底座 warp 绘制之上以 Additive 方式叠加故障滤镜
        /// 保留底座凝实感，只补充电弧闪烁与色调偏移
        /// </summary>
        private static void DrawPedestalFaultOverlay(SpriteBatch sb, Texture2D tex, Vector2 drawPos,
            float strength, float mode) {
            Effect shader = HackTimeAssets.HackTurretCircuitFault;
            if (shader == null) return;

            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / tex.Width, 1f / tex.Height));
            shader.Parameters["intensity"]?.SetValue(strength);
            shader.Parameters["mode"]?.SetValue(mode);
            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
            sb.Draw(tex, drawPos, null, Color.White * strength, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        /// <summary>
        /// 使用VoidLaserCannon专用着色器绘制能量激光柱
        /// 只绘制一张白色占位图，所有色温梯度、湍流、脉动、电浆闪烁均在GPU计算
        /// </summary>
        private void DrawLaserBeam(SpriteBatch spriteBatch) {
            Texture2D beamTex = CWRAsset.Placeholder_White?.Value;
            Texture2D noiseTex = CWRAsset.Extra_193?.Value;
            if (beamTex == null || noiseTex == null) return;

            Effect shader = EffectLoader.VoidLaserCannon?.Value;
            if (shader == null) return;

            Vector2 start = MuzzleWorld - Main.screenPosition;
            float rotation = currentRotation;
            float length = MaxBeamLength;
            float width = beamWidth;
            float intensity = beamIntensity;
            float visFactor = MathHelper.Clamp(visibility, 0f, 1f);

            //阶段权重：蓄能=0，发射=1，收束/冷却在两者之间
            float phaseBlend = phase switch {
                1 => 0f,
                2 => 1f,
                _ => 0.35f,
            };

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //噪声纹理绑到uImage1(s1)
            Main.graphics.GraphicsDevice.Textures[1] = noiseTex;
            Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["uOpacity"]?.SetValue(visFactor);
            shader.Parameters["uIntensity"]?.SetValue(beamPulse);
            shader.Parameters["uCoreIntensity"]?.SetValue(intensity);
            shader.Parameters["uPulseSpeed"]?.SetValue(5.5f);
            shader.Parameters["uDistortionStrength"]?.SetValue(0.85f);
            shader.Parameters["uBeamLength"]?.SetValue(length);
            shader.Parameters["uBeamWidth"]?.SetValue(width);
            shader.Parameters["uPhaseBlend"]?.SetValue(phaseBlend);
            shader.CurrentTechnique.Passes["VoidLaserPass"].Apply();

            //主光束：拉伸一张白色贴图，所有效果由像素着色器生成
            //宽度放大一些以留出外辉光绘制空间，shader内以distCenter自行衰减
            float drawWidth = width * 2.4f;
            Vector2 beamOrigin = new(0, beamTex.Height / 2f);
            Vector2 beamScale = new(length / beamTex.Width, drawWidth / beamTex.Height);
            Color tint = Color.White * visFactor;
            spriteBatch.Draw(beamTex, start, null, tint, rotation, beamOrigin, beamScale, SpriteEffects.None, 0f);

            //第二层：较窄但叠加绘制，强化核心曝白(叠加在Additive下有放大作用)
            if (phase == 2) {
                shader.Parameters["uCoreIntensity"]?.SetValue(intensity * 1.6f);
                shader.CurrentTechnique.Passes["VoidLaserPass"].Apply();
                Vector2 coreScale = new(length / beamTex.Width, (drawWidth * 0.55f) / beamTex.Height);
                spriteBatch.Draw(beamTex, start, null, tint, rotation, beamOrigin, coreScale, SpriteEffects.None, 0f);
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            //点亮沿途环境
            if (phase == 2) {
                Vector2 worldStart = MuzzleWorld;
                Vector2 forward = currentRotation.ToRotationVector2();
                int steps = 14;
                for (int i = 0; i < steps; i++) {
                    Vector2 p = worldStart + forward * (length * i / steps);
                    float t = 1f - i / (float)steps;
                    Lighting.AddLight(p,
                        1.1f * intensity * t,
                        0.55f * intensity * t,
                        1.4f * intensity * t);
                }
            }
            else if (phase == 1) {
                Lighting.AddLight(MuzzleWorld, 0.45f * intensity, 0.2f * intensity, 0.85f * intensity);
            }
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
            values[0] = HackTime.TurretScanLaserName.Value;
            colors[0] = HackTheme.Danger;

            labels[1] = HackTime.TypeLabel.Value;
            values[1] = HackTime.TurretScanType.Value;
            colors[1] = HackTheme.Accent;

            labels[2] = HackTime.ThreatLabel.Value;
            values[2] = HackTime.ThreatExtreme.Value;
            colors[2] = HackTheme.Danger;

            labels[3] = HackTime.TurretScanPhase.Value;
            values[3] = phase switch {
                1 => HackTime.TurretScanPhaseCharging.Value,
                2 => HackTime.TurretScanPhaseFiring.Value,
                3 => HackTime.TurretScanPhaseCooldown.Value,
                _ => HackTime.TurretScanPhaseIdle.Value,
            };
            colors[3] = phase == 2 ? HackTheme.Danger : HackTheme.Uploading;

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
            //若已处于过载则叠加较短的失效时间，但不降级过载标志
            if (frames > circuitDisabledFrames) circuitDisabledFrames = frames;
            if (!circuitOverloaded) circuitOverloaded = false;
            AbortBeam();
        }

        public void ApplyCircuitOverload(int frames, Player caster) {
            if (frames > circuitDisabledFrames) circuitDisabledFrames = frames;
            circuitOverloaded = true;
            AbortBeam();
        }

        #endregion
    }
}
