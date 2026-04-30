using CalamityOverhaul.Common;
using InnoVault.Trails;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.LegendWeapon.HalibutLegend
{
    ///<summary>
    ///海洋洪流子弹
    ///<br/>核心水流通过 <see cref="IPrimitiveDrawable"/> + <c>OceanCurrentTrail.fx</c> 着色器渲染（域扭曲噪声卷流、焦散、流条、边缘泡沫带）
    ///<br/>水滴、泡沫与头部水球通过 <c>OceanWaterBlob.fx</c> 着色器渲染（程序化折射、动态焦散、自适应泡沫、生物荧光内核）
    ///<br/>CPU 端只负责物理与状态机；所有视觉合成均交由 GPU
    ///</summary>
    internal class OceanCurrent : ModProjectile, IAdditiveDrawable, IPrimitiveDrawable
    {
        public override string Texture => CWRConstant.Placeholder;

        //液体状态
        private enum OceanState
        {
            Streaming,      //洪流状态
            Splashing,      //飞溅状态
            Dispersing      //消散状态
        }

        private OceanState State {
            get => (OceanState)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }

        private ref float StreamLife => ref Projectile.ai[1];

        //海洋液体粒子系统
        private readonly List<OceanDroplet> waterDroplets = new();
        private const int MaxDroplets = 120;

        //海洋泡沫粒子
        private readonly List<SeaFoam> foamParticles = new();
        private const int MaxFoam = 80;

        //海洋生物粒子（鱼群、海藻）
        private readonly List<MarineLife> marineLifeParticles = new();
        private const int MaxMarineLife = 15;

        //GPU 核心水流条带（替代纯CPU线段绘制）
        private const int TrailLen = 32;
        private Vector2[] trailPositions;
        private int trailValidCount;
        private Trail trail;
        ///<summary>核心水流绘制淡入/淡出系数（流注阶段过度到飞溅/消散时平滑收尾）</summary>
        private float streamFade;

        //物理参数
        private const float WaterViscosity = 0.985f;
        private const float Gravity = 0.24f;
        private const float SurfaceTension = 0.18f;
        private const float WaterDensity = 1.0f;
        private const float BuoyancyForce = -0.05f;

        //视觉效果
        private float glowPulse = 0f;
        private float wavePhase = 0f;
        private int particleSpawnCounter = 0;
        private int pcctimer = 0;

        //消散宽限控制
        private int dispersalStartLife = -1; //进入消散阶段时记录生命周期计数
        private const int MaxGraceTicks = 90; //最大宽限时间（保证粒子能自然淡出）

        //海洋颜色主题（以"真海水"为基调：深海蓝 → 中层蓝 → 青蓝泡沫 → 生物荧光青）
        //OceanFoam 不再使用接近纯白的 (200,230,255)，避免 Additive 多层叠加后蓝/绿/红一并饱和成白
        //同时拆出一个仅用于极小热斑的 OceanHotSpark，绝不做大面积白叠加
        private static readonly Color DeepOcean = new(8, 32, 78);
        private static readonly Color ShallowOcean = new(24, 96, 188);
        private static readonly Color OceanFoam = new(120, 195, 235);
        private static readonly Color BioluminescentBlue = new(70, 175, 255);
        private static readonly Color OceanHotSpark = new(190, 225, 255);

        private int trueDmg;

        public override void SetDefaults() {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.alpha = 0;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
            Projectile.arrow = true;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            if (target.IsWormBody()) {
                modifiers.FinalDamage *= 0.75f;
            }
        }

        //击中NPC
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            CreateOceanSplash(Projectile.Center, Projectile.velocity);

            //潮湿debuff
            target.AddBuff(BuffID.Wet, 300);

            //击中音效
            SoundEngine.PlaySound(SoundID.Item85 with {
                Volume = 0.6f,
                Pitch = 0.1f
            }, Projectile.Center);

            Projectile.damage = (int)(Projectile.damage * 0.66f);
        }

        public override void AI() {
            StreamLife++;

            //状态机
            switch (State) {
                case OceanState.Streaming:
                    StreamingPhaseAI();
                    break;
                case OceanState.Splashing:
                    SplashingPhaseAI();
                    break;
                case OceanState.Dispersing:
                    DispersingPhaseAI();
                    break;
            }

            //更新所有粒子系统
            UpdateCoreTrail();
            UpdateStreamFade();
            UpdateWaterDroplets();
            UpdateFoamParticles();
            UpdateMarineLife();

            //动画效果
            glowPulse = (float)Math.Sin(StreamLife * 0.2f) * 0.2f + 0.8f;
            wavePhase += 0.15f;
            if (wavePhase > MathHelper.TwoPi) wavePhase -= MathHelper.TwoPi;

            //海洋蓝色照明
            float lightIntensity = MathHelper.Lerp(0.4f, 0.9f, glowPulse);
            Lighting.AddLight(Projectile.Center,
                0.3f * lightIntensity,
                0.7f * lightIntensity,
                1.2f * lightIntensity);

            //水下音效
            if (StreamLife % 35 == 0 && State == OceanState.Streaming) {
                SoundEngine.PlaySound(SoundID.Splash with {
                    Volume = 0.3f,
                    Pitch = Main.rand.NextFloat(-0.3f, 0.1f)
                }, Projectile.Center);
            }

            //若即将自然过期且仍在Streaming则提前进入飞溅以开始平滑消散
            if (Projectile.timeLeft < 30 && State == OceanState.Streaming) {
                EnterSplashState();
            }

            if (Projectile.numHits > 2) {
                Projectile.velocity /= 2;
                if (Projectile.damage > 0) {
                    trueDmg = Projectile.damage;
                }
                Projectile.damage = 0;
                if (++pcctimer > 60) {
                    Projectile.Kill();
                }
            }
        }

        //洪流状态AI
        private void StreamingPhaseAI() {
            //应用重力和水流浮力
            Projectile.velocity.Y += Gravity * WaterDensity / 2f;
            Projectile.velocity.Y += BuoyancyForce; //模拟浮力

            //粘性阻力
            //Projectile.velocity *= WaterViscosity;

            //生成水滴粒子
            if (particleSpawnCounter++ % 2 == 0 && waterDroplets.Count < MaxDroplets) {
                SpawnWaterDroplet();
            }

            //生成泡沫粒子
            if (StreamLife % 3 == 0 && foamParticles.Count < MaxFoam) {
                SpawnFoamParticle();
            }

            //周期性生成海洋生物粒子
            if (StreamLife % 25 == 0 && marineLifeParticles.Count < MaxMarineLife) {
                SpawnMarineLife();
            }

            //水流波纹效果
            if (StreamLife % 6 == 0) {
                SpawnWaterRipple();
            }

            //转换到飞溅状态
            if (StreamLife > 180 || Projectile.velocity.Length() < 1.5f) {
                EnterSplashState();
            }

            //旋转效果（模拟水流旋涡）
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        //飞溅状态AI
        private void SplashingPhaseAI() {
            Projectile.velocity *= 0.92f;
            Projectile.alpha += 10;

            if (Projectile.alpha >= 200) {
                State = OceanState.Dispersing;
            }

            //继续生成少量粒子
            if (StreamLife % 4 == 0 && waterDroplets.Count < MaxDroplets / 2) {
                SpawnWaterDroplet();
            }
        }

        //消散状态AI
        private void DispersingPhaseAI() {
            if (dispersalStartLife < 0) dispersalStartLife = (int)StreamLife;

            Projectile.velocity *= 0.85f;

            //更柔和的透明度增加
            Projectile.alpha = Math.Min(255, Projectile.alpha + 12);

            bool anyParticles = waterDroplets.Count > 0 || foamParticles.Count > 0 || marineLifeParticles.Count > 0;
            int elapsed = (int)StreamLife - dispersalStartLife;

            //当粒子尚未完全淡出且宽限时间未结束时持续续命(保持timeLeft低值避免被清理)
            if (Projectile.alpha >= 255) {
                if (anyParticles && elapsed < MaxGraceTicks) {
                    Projectile.timeLeft = Math.Max(Projectile.timeLeft, 2);
                }
                else {
                    Projectile.Kill();
                }
            }
            else {
                //尚未全透明也保持续命直到透明度达到
                if (anyParticles) {
                    Projectile.timeLeft = Math.Max(Projectile.timeLeft, 2);
                }
            }
        }

        //生成水滴粒子
        private void SpawnWaterDroplet() {
            Vector2 baseVel = Projectile.velocity;
            Vector2 particleVel = baseVel + Main.rand.NextVector2Circular(3f, 3f);

            //添加波动效果
            float waveOffset = (float)Math.Sin(wavePhase + waterDroplets.Count * 0.3f) * 0.5f;
            particleVel += Projectile.velocity.RotatedBy(MathHelper.PiOver2) * waveOffset;

            //根据速度与是否为溅射决定帧（3x3 共9帧）
            int frame = 0;
            float spd = particleVel.Length();
            if (spd < 2f) frame = Main.rand.Next(0, 3);           //慢速：上排
            else if (spd < 5f) frame = Main.rand.Next(3, 6);       //中速：中排
            else frame = Main.rand.Next(6, 9);                     //快速：下排

            OceanDroplet droplet = new OceanDroplet {
                Position = Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                Velocity = particleVel * Main.rand.NextFloat(0.6f, 1.2f),
                Size = Main.rand.NextFloat(1.5f, 3.5f),
                Life = 0,
                MaxLife = Main.rand.Next(30, 55),
                Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                RotationSpeed = Main.rand.NextFloat(-0.18f, 0.18f),
                Opacity = 1f,
                IsSplash = false,
                ColorVariant = Main.rand.NextFloat(0f, 1f),
                Frame = frame
            };

            waterDroplets.Add(droplet);
        }

        //生成泡沫粒子
        private void SpawnFoamParticle() {
            Vector2 offset = Main.rand.NextVector2Circular(12f, 12f);
            Vector2 foamVel = -Projectile.velocity * 0.3f + Main.rand.NextVector2Circular(2f, 2f);
            foamVel.Y -= Main.rand.NextFloat(0.5f, 1.5f); //泡沫向上漂浮

            SeaFoam foam = new SeaFoam {
                Position = Projectile.Center + offset,
                Velocity = foamVel,
                Size = Main.rand.NextFloat(1.2f, 2.8f),
                Life = 0,
                MaxLife = Main.rand.Next(40, 70),
                Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                RotationSpeed = Main.rand.NextFloat(-0.12f, 0.12f),
                Opacity = 1f,
                PopPhase = 0f
            };

            foamParticles.Add(foam);
        }

        //生成海洋生物粒子（鱼、海藻等）
        private void SpawnMarineLife() {
            //随机选择生物类型
            MarineLifeType type = Main.rand.NextBool() ? MarineLifeType.Fish : MarineLifeType.Seaweed;

            Vector2 offset = Main.rand.NextVector2Circular(20f, 20f);
            Vector2 lifeVel = Projectile.velocity * Main.rand.NextFloat(0.4f, 0.8f);
            lifeVel += Main.rand.NextVector2Circular(1.5f, 1.5f);

            MarineLife life = new MarineLife {
                Position = Projectile.Center + offset,
                Velocity = lifeVel,
                Size = Main.rand.NextFloat(1.5f, 3f),
                Life = 0,
                MaxLife = Main.rand.Next(50, 90),
                Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                RotationSpeed = Main.rand.NextFloat(-0.08f, 0.08f),
                Opacity = 0.8f,
                Type = type,
                SwimPhase = Main.rand.NextFloat(MathHelper.TwoPi),
                FlickerPhase = Main.rand.NextFloat(MathHelper.TwoPi)
            };

            marineLifeParticles.Add(life);
        }

        //生成水流波纹
        private void SpawnWaterRipple() {
            for (int i = 0; i < 6; i++) {
                float angle = MathHelper.TwoPi * i / 6f;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(2f, 4f);

                Dust ripple = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Water,
                    velocity,
                    100,
                    default,
                    Main.rand.NextFloat(1.2f, 2f)
                );
                ripple.noGravity = true;
                ripple.fadeIn = 1.3f;
            }
        }

        //更新水滴粒子
        private void UpdateWaterDroplets() {
            for (int i = waterDroplets.Count - 1; i >= 0; i--) {
                OceanDroplet droplet = waterDroplets[i];
                droplet.Life++;

                //物理更新
                if (!droplet.IsSplash) {
                    //正常水流粒子
                    droplet.Velocity.Y += Gravity * WaterDensity;
                    droplet.Velocity.Y += BuoyancyForce * 0.5f;
                    droplet.Velocity *= WaterViscosity;

                    //表面张力
                    Vector2 toCore = Projectile.Center - droplet.Position;
                    float distToCore = toCore.Length();
                    if (distToCore > 18f && distToCore < 80f) {
                        droplet.Velocity += toCore.SafeNormalize(Vector2.Zero) * SurfaceTension;
                    }
                }
                else {
                    //飞溅粒子
                    droplet.Velocity.Y += Gravity * 1.8f;
                    droplet.Velocity.X *= 0.96f;

                    //地面碰撞
                    if (Framing.GetTileSafely(droplet.Position.ToTileCoordinates()).HasTile) {
                        droplet.Velocity.Y *= -0.35f;
                        droplet.Velocity.X *= 0.6f;
                        if (Math.Abs(droplet.Velocity.Y) < 0.8f) {
                            droplet.Velocity.Y = 0;
                        }
                    }
                }

                droplet.Position += droplet.Velocity;
                droplet.Rotation += droplet.RotationSpeed;

                //透明度衰减
                float lifeRatio = droplet.Life / (float)droplet.MaxLife;
                droplet.Opacity = 1f - lifeRatio;

                //尺寸变化
                if (droplet.IsSplash) {
                    droplet.Size *= 0.97f;
                }

                //移除消逝的粒子
                if (droplet.Life >= droplet.MaxLife || droplet.Opacity <= 0.05f) {
                    waterDroplets.RemoveAt(i);
                    continue;
                }

                waterDroplets[i] = droplet;
            }
        }

        //更新泡沫粒子
        private void UpdateFoamParticles() {
            for (int i = foamParticles.Count - 1; i >= 0; i--) {
                SeaFoam foam = foamParticles[i];
                foam.Life++;

                //泡沫向上漂浮
                foam.Velocity.Y -= 0.08f;
                foam.Velocity *= 0.98f;
                foam.Position += foam.Velocity;
                foam.Rotation += foam.RotationSpeed;

                //透明度和尺寸
                float lifeRatio = foam.Life / (float)foam.MaxLife;
                foam.Opacity = (1f - lifeRatio) * 0.9f;
                foam.Size *= 1.01f; //泡沫膨胀

                //破裂阶段
                if (lifeRatio > 0.8f) {
                    foam.PopPhase = (lifeRatio - 0.8f) / 0.2f;
                }

                //移除消逝的粒子
                if (foam.Life >= foam.MaxLife || foam.Opacity <= 0.05f) {
                    foamParticles.RemoveAt(i);
                    continue;
                }

                foamParticles[i] = foam;
            }
        }

        //更新海洋生物粒子
        private void UpdateMarineLife() {
            for (int i = marineLifeParticles.Count - 1; i >= 0; i--) {
                MarineLife life = marineLifeParticles[i];
                life.Life++;

                //游动物理
                life.Velocity.Y += Gravity * 0.5f;
                life.Velocity.Y += BuoyancyForce * 2f; //更强浮力
                life.Velocity *= 0.97f;

                //游动动画
                life.SwimPhase += 0.15f;
                if (life.SwimPhase > MathHelper.TwoPi) life.SwimPhase -= MathHelper.TwoPi;

                //添加游动波动
                float swimOffset = (float)Math.Sin(life.SwimPhase) * 0.8f;
                Vector2 swimDirection = life.Velocity.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2);
                life.Position += life.Velocity + swimDirection * swimOffset;

                life.Rotation += life.RotationSpeed;

                //发光闪烁（生物发光）
                life.FlickerPhase += 0.12f;
                if (life.FlickerPhase > MathHelper.TwoPi) life.FlickerPhase -= MathHelper.TwoPi;

                //透明度衰减
                float lifeRatio = life.Life / (float)life.MaxLife;
                life.Opacity = (1f - lifeRatio) * 0.8f;

                //移除消逝的粒子
                if (life.Life >= life.MaxLife || life.Opacity <= 0.05f) {
                    marineLifeParticles.RemoveAt(i);
                    continue;
                }

                marineLifeParticles[i] = life;
            }
        }

        //更新核心拖尾（固定长度数组，头部为 [0]，尾端为 [TrailLen-1]）
        private void UpdateCoreTrail() {
            trailPositions ??= new Vector2[TrailLen];
            if (trailValidCount == 0) {
                for (int i = 0; i < TrailLen; i++) {
                    trailPositions[i] = Projectile.Center;
                }
                trailValidCount = 1;
                return;
            }

            for (int i = TrailLen - 1; i > 0; i--) {
                trailPositions[i] = trailPositions[i - 1];
            }
            trailPositions[0] = Projectile.Center;
            if (trailValidCount < TrailLen) trailValidCount++;
        }

        //依据状态平滑推进 streamFade（拖尾整体亮度/可见度）
        private void UpdateStreamFade() {
            float target = State switch {
                OceanState.Streaming => 1f,
                OceanState.Splashing => 0.45f,
                OceanState.Dispersing => 0f,
                _ => 0f
            };
            streamFade = MathHelper.Lerp(streamFade, target, 0.16f);
            if (streamFade < 0.002f) streamFade = 0f;
        }

        //进入飞溅状态
        private void EnterSplashState() {
            State = OceanState.Splashing;
            Projectile.velocity *= 0.4f;
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 80);
        }

        //碰撞处理
        public override bool OnTileCollide(Vector2 oldVelocity) {
            if (State == OceanState.Streaming) {
                CreateOceanSplash(Projectile.Center, oldVelocity);

                //飞溅音效
                SoundEngine.PlaySound(SoundID.Splash with {
                    Volume = 0.7f,
                    Pitch = -0.1f
                }, Projectile.Center);

                SoundEngine.PlaySound(SoundID.Item96 with {
                    Volume = 0.4f,
                    Pitch = -0.4f
                }, Projectile.Center);

                EnterSplashState();
                return false;
            }

            Projectile.velocity = Vector2.Zero;
            return false;
        }

        //创建海洋飞溅效果
        private void CreateOceanSplash(Vector2 hitPosition, Vector2 impactVelocity) {
            Vector2 normal = -impactVelocity.SafeNormalize(Vector2.Zero);
            float impactSpeed = impactVelocity.Length();
            float mainAngle = normal.ToRotation();

            int splashCount = (int)MathHelper.Clamp(impactSpeed * 4f, 30, 80);

            for (int i = 0; i < splashCount; i++) {
                float spreadAngle = Main.rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);
                float angle = mainAngle + spreadAngle;
                float speedRatio = 1f - Math.Abs(spreadAngle) / MathHelper.PiOver2;
                float speed = Main.rand.NextFloat(4f, 14f) * speedRatio * (impactSpeed / 25f);
                Vector2 velocity = angle.ToRotationVector2() * speed;

                if (waterDroplets.Count < MaxDroplets * 2) {
                    //飞溅水滴使用更混乱的帧（随机整张9帧）
                    int frame = Main.rand.Next(0, 9);
                    OceanDroplet splash = new OceanDroplet {
                        Position = hitPosition + Main.rand.NextVector2Circular(10f, 10f),
                        Velocity = velocity,
                        Size = Main.rand.NextFloat(2f, 4f),
                        Life = 0,
                        MaxLife = Main.rand.Next(40, 70),
                        Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                        RotationSpeed = Main.rand.NextFloat(-0.25f, 0.25f),
                        Opacity = 1f,
                        IsSplash = true,
                        ColorVariant = Main.rand.NextFloat(0f, 1f),
                        Frame = frame
                    };
                    waterDroplets.Add(splash);
                }

                //原版水尘埃
                if (i % 3 == 0) {
                    Dust water = Dust.NewDustPerfect(
                        hitPosition,
                        DustID.Water,
                        velocity * 0.5f,
                        100,
                        default,
                        Main.rand.NextFloat(1.5f, 2.5f)
                    );
                    water.noGravity = false;
                    water.fadeIn = 1.4f;
                }
            }

            //飞溅泡沫
            for (int i = 0; i < 20; i++) {
                float angle = mainAngle + Main.rand.NextFloat(-1f, 1f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(3f, 8f);

                if (foamParticles.Count < MaxFoam * 2) {
                    SeaFoam foam = new SeaFoam {
                        Position = hitPosition,
                        Velocity = velocity,
                        Size = Main.rand.NextFloat(2f, 4f),
                        Life = 0,
                        MaxLife = Main.rand.Next(35, 60),
                        Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                        RotationSpeed = Main.rand.NextFloat(-0.2f, 0.2f),
                        Opacity = 1f,
                        PopPhase = 0f
                    };
                    foamParticles.Add(foam);
                }
            }

            //飞溅环
            CreateSplashRing(hitPosition, mainAngle, impactSpeed);
        }

        //创建飞溅环
        private void CreateSplashRing(Vector2 center, float direction, float intensity) {
            int ringCount = Math.Clamp((int)(intensity * 1.2f), 20, 40);

            for (int i = 0; i < ringCount; i++) {
                float angle = direction + MathHelper.Lerp(-MathHelper.Pi, MathHelper.Pi, i / (float)ringCount);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(5f, 10f);

                Dust ring = Dust.NewDustPerfect(
                    center,
                    DustID.Water,
                    velocity,
                    100,
                    default,
                    Main.rand.NextFloat(2f, 3f)
                );
                ring.noGravity = true;
                ring.fadeIn = 1.5f;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;

        //============================================================
        //拖尾宽度函数：头部快速上升 → 沿途逐渐收窄 → 末端收为 0
        //与 streamFade 联动以平滑融入飞溅/消散阶段
        //============================================================
        private float TrailWidthFunction(float progress) {
            float validRatio = MathF.Max((float)trailValidCount / TrailLen, 0.1f);
            float t = MathHelper.Clamp(progress / validRatio, 0f, 1f);

            //头部光锥：在前 6% 段从 0 急速升至 1（消除起手时的硬切口）
            float noseRise = MathF.Sin(MathF.Min(t / 0.06f, 1f) * MathHelper.PiOver2);
            //尾端平滑收尖
            float tailTaper = 1f - MathF.Pow(t, 1.85f);
            float width = noseRise * tailTaper;

            return MathF.Max(width, 0f) * (Projectile.width * 0.3f) * (0.35f + 0.65f * streamFade);
        }

        private static Color TrailColorFunction(Vector2 _) => Color.White;

        //============================================================
        //IPrimitiveDrawable —— GPU 核心水流条带渲染
        //域扭曲噪声卷流 + 焦散波光 + 流条 + 边缘泡沫带
        //============================================================
        void IPrimitiveDrawable.DrawPrimitives() {
            if (trailPositions == null || trailValidCount < 3) return;
            if (streamFade < 0.01f) return;

            Effect shader = EffectLoader.OceanCurrentTrail?.Value;
            if (shader == null) return;
            Texture2D noise = CWRAsset.PerlinNoise?.Value;
            Texture2D flow = CWRAsset.Airflow?.Value;
            if (noise == null || flow == null) return;

            trail ??= new Trail(trailPositions, TrailWidthFunction, TrailColorFunction);
            trail.TrailPositions = trailPositions;

            //头部不透明度系数：飞溅阶段进一步乘以 Projectile.alpha 衰减
            float alphaMult = 1f - Projectile.alpha / 255f;
            float speed = Projectile.velocity.Length();
            float speedRatio = MathHelper.Clamp(speed / 18f, 0f, 1f);
            float foamDensity = MathHelper.Clamp(speedRatio * 0.7f + 0.30f, 0f, 1f);

            shader.Parameters["transformMatrix"]?.SetValue(VaultUtils.GetTransfromMatrix());
            shader.Parameters["uTime"]?.SetValue((float)Main.timeForVisualEffects * 0.045f);
            shader.Parameters["fadeAlpha"]?.SetValue(MathHelper.Clamp(streamFade * alphaMult, 0f, 1f));
            shader.Parameters["pulse"]?.SetValue(glowPulse);
            shader.Parameters["speedRatio"]?.SetValue(speedRatio);
            shader.Parameters["foamDensity"]?.SetValue(foamDensity);
            shader.Parameters["deepColor"]?.SetValue(DeepOcean.ToVector3());
            shader.Parameters["shallowColor"]?.SetValue(ShallowOcean.ToVector3());
            shader.Parameters["foamColor"]?.SetValue(OceanFoam.ToVector3());
            shader.Parameters["bioColor"]?.SetValue(BioluminescentBlue.ToVector3());
            shader.Parameters["uNoiseTex"]?.SetValue(noise);
            shader.Parameters["uFlowTex"]?.SetValue(flow);

            GraphicsDevice device = Main.graphics.GraphicsDevice;
            BlendState prevBlend = device.BlendState;
            device.BlendState = BlendState.Additive;
            trail.DrawTrail(shader);
            device.BlendState = prevBlend ?? BlendState.AlphaBlend;
        }

        //============================================================
        //IAdditiveDrawable —— 水滴/泡沫/头部水球的 GPU 着色器渲染
        //一个 Immediate 批次中复用 OceanWaterBlob.fx，按组改参数
        //============================================================
        void IAdditiveDrawable.DrawAdditiveAfterNon(SpriteBatch spriteBatch) {
            //海洋生物保留 CPU 风格化粒子绘制（不属于水状物质）
            DrawMarineLife();

            Effect shader = EffectLoader.OceanWaterBlob?.Value;
            Texture2D noise = CWRAsset.PerlinNoise?.Value;

            if (shader == null || noise == null) {
                //回退到无 Shader 的常规绘制
                DrawFoamParticles();
                DrawWaterDroplets();
                if (streamFade > 0.05f) {
                    DrawStreamCore();
                }
                return;
            }

            //结束父级 Additive 批次，切换到 Immediate 模式以应用自定义着色器
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);

            //---- A. 泡沫（SoftGlow 单帧贴图，可用较强折射） ----
            SetWaterBlobShaderParams(shader, noise, foamThreshold: 0.18f, refract: 0.045f, shimmerSpeed: 4.5f);
            DrawFoamParticlesShader(spriteBatch);

            //---- B. 水滴（Spray 3x3 帧图，折射量受帧界限制需保守） ----
            SetWaterBlobShaderParams(shader, noise, foamThreshold: 0.32f, refract: 0.022f, shimmerSpeed: 7.5f);
            DrawWaterDropletsShader(spriteBatch);

            //---- C. 头部水球（SoftGlow 单帧贴图，强折射+强焦散表现"水流冲头"） ----
            if (streamFade > 0.05f) {
                SetWaterBlobShaderParams(shader, noise, foamThreshold: 0.22f, refract: 0.07f, shimmerSpeed: 10f);
                DrawStreamCoreShader(spriteBatch);
            }

            spriteBatch.End();

            //恢复父级期望的 Deferred + Additive + PointWrap 批次状态
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        private void SetWaterBlobShaderParams(Effect shader, Texture2D noise,
            float foamThreshold, float refract, float shimmerSpeed) {
            shader.Parameters["uTime"]?.SetValue((float)Main.timeForVisualEffects * 0.04f);
            shader.Parameters["foamThreshold"]?.SetValue(foamThreshold);
            shader.Parameters["refractStrength"]?.SetValue(refract);
            shader.Parameters["coreShimmerSpeed"]?.SetValue(shimmerSpeed);
            shader.Parameters["foamColor"]?.SetValue(OceanFoam.ToVector3());
            shader.Parameters["bioColor"]?.SetValue(BioluminescentBlue.ToVector3());
            //顶点高光改为偏冷青调（OceanHotSpark = 190,225,255），避免使用 Vector3.One
            //在 Additive 混合下纯白高光 + 浅水青底色会把整块粒子"洗"成白噪
            shader.Parameters["highlightColor"]?.SetValue(OceanHotSpark.ToVector3());
            shader.Parameters["uNoiseTex"]?.SetValue(noise);
        }

        //============================================================
        //水滴粒子 —— Shader 渲染（OceanWaterBlob.fx）
        //每个水滴只需一次 Draw：折射、焦散、生物荧光内核全部由 GPU 合成
        //仅高速非飞溅水滴额外绘制一次水流尾迹
        //============================================================
        private void DrawWaterDropletsShader(SpriteBatch sb) {
            Texture2D dropletTex = CWRAsset.Spray.Value;
            const int columns = 3;
            const int rows = 3;
            int frameWidth = dropletTex.Width / columns;
            int frameHeight = dropletTex.Height / rows;
            Vector2 origin = new(frameWidth / 2f, frameHeight / 2f);

            foreach (var droplet in waterDroplets) {
                Vector2 drawPos = droplet.Position - Main.screenPosition;
                float scale = droplet.Size * 0.10f;

                Color baseTint = Color.Lerp(DeepOcean, ShallowOcean, droplet.ColorVariant);
                Color tint = baseTint * droplet.Opacity;

                int frameIndex = droplet.Frame % (columns * rows);
                int fx = frameIndex % columns * frameWidth;
                int fy = frameIndex / columns * frameHeight;
                Rectangle source = new(fx, fy, frameWidth, frameHeight);

                sb.Draw(dropletTex, drawPos, source, tint, droplet.Rotation, origin,
                    scale * 1.4f, SpriteEffects.None, 0f);

                //高速非飞溅水滴：沿速度方向再绘制一道拉伸水绺
                if (!droplet.IsSplash && droplet.Velocity.Length() > 5f) {
                    float vRot = droplet.Velocity.ToRotation();
                    Color stretchTint = Color.Lerp(BioluminescentBlue, ShallowOcean, 0.35f) * (droplet.Opacity * 0.55f);
                    sb.Draw(dropletTex, drawPos, source, stretchTint, vRot, origin,
                        new Vector2(scale * 1.9f, scale * 0.55f), SpriteEffects.None, 0f);
                }
            }
        }

        //回退路径：无 Shader 时的简单绘制
        private void DrawWaterDroplets() {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D glowTex = CWRAsset.Spray.Value;
            const int columns = 3;
            const int rows = 3;
            int frameWidth = glowTex.Width / columns;
            int frameHeight = glowTex.Height / rows;
            Vector2 origin = new(frameWidth / 2f, frameHeight / 2f);

            foreach (var droplet in waterDroplets) {
                Vector2 drawPos = droplet.Position - Main.screenPosition;
                float scale = droplet.Size * 0.09f;
                Color tint = Color.Lerp(DeepOcean, ShallowOcean, droplet.ColorVariant) * droplet.Opacity;

                int frameIndex = droplet.Frame % (columns * rows);
                int fx = frameIndex % columns * frameWidth;
                int fy = frameIndex / columns * frameHeight;
                Rectangle source = new(fx, fy, frameWidth, frameHeight);

                sb.Draw(glowTex, drawPos, source, tint, droplet.Rotation, origin,
                    scale * 1.3f, SpriteEffects.None, 0);
                sb.Draw(glowTex, drawPos, source, BioluminescentBlue * droplet.Opacity * 0.7f,
                    droplet.Rotation * 1.5f, origin, scale * 0.7f, SpriteEffects.None, 0);
            }
        }

        //============================================================
        //泡沫粒子 —— Shader 渲染（OceanWaterBlob.fx）
        //比 CPU 双层叠加更柔和、自带程序化气泡边缘
        //============================================================
        private void DrawFoamParticlesShader(SpriteBatch sb) {
            Texture2D foamTex = CWRAsset.SoftGlow.Value;
            Vector2 origin = foamTex.Size() * 0.5f;

            foreach (var foam in foamParticles) {
                Vector2 drawPos = foam.Position - Main.screenPosition;
                float scale = foam.Size * 0.075f;
                float popLerp = foam.PopPhase;

                //破裂阶段越靠后，整体透明度衰减越快、尺寸略微膨胀
                Color foamTint = OceanFoam * foam.Opacity * (1f - popLerp * 0.45f);
                foamTint.A = 0;
                sb.Draw(foamTex, drawPos, null, foamTint, foam.Rotation, origin,
                    scale * (1f + popLerp * 0.55f), SpriteEffects.None, 0f);
            }
        }

        //回退路径：无 Shader 时的简单绘制
        private void DrawFoamParticles() {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D glowTex = CWRAsset.SoftGlow.Value;
            Vector2 origin = glowTex.Size() * 0.5f;

            foreach (var foam in foamParticles) {
                Vector2 drawPos = foam.Position - Main.screenPosition;
                float scale = foam.Size * 0.06f;
                Color foamTint = OceanFoam * foam.Opacity * (1f - foam.PopPhase * 0.5f);
                sb.Draw(glowTex, drawPos, null, foamTint * 0.8f, foam.Rotation, origin,
                    scale * (1f + foam.PopPhase * 0.3f), SpriteEffects.None, 0);
            }
        }

        //绘制海洋生物
        private void DrawMarineLife() {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D glowTex = CWRAsset.StarTexture_White.Value;

            foreach (var life in marineLifeParticles) {
                Vector2 drawPos = life.Position - Main.screenPosition;
                float scale = life.Size * 0.05f;

                //生物发光效果
                float flicker = (float)Math.Sin(life.FlickerPhase) * 0.3f + 0.7f;
                Color lifeColor = life.Type == MarineLifeType.Fish
                    ? BioluminescentBlue * life.Opacity * flicker
                    : new Color(50, 150, 100) * life.Opacity * flicker;

                //主体
                sb.Draw(
                    glowTex,
                    drawPos,
                    null,
                    lifeColor * 0.9f,
                    life.Rotation,
                    glowTex.Size() / 2f,
                    scale * 1.5f,
                    SpriteEffects.None,
                    0
                );

                //发光核心：避免叠加 Color.White 带来的"洗白"，改用偏冷热斑色
                Color coreColor = OceanHotSpark * life.Opacity * flicker * 0.45f;
                sb.Draw(
                    glowTex,
                    drawPos,
                    null,
                    coreColor,
                    life.Rotation * 1.8f,
                    glowTex.Size() / 2f,
                    scale * 0.8f,
                    SpriteEffects.None,
                    0
                );

                //游动拖尾（仅鱼类）
                if (life.Type == MarineLifeType.Fish) {
                    Vector2 tailOffset = -life.Velocity.SafeNormalize(Vector2.Zero) * scale * 30f;
                    Color tailColor = lifeColor * 0.4f;
                    sb.Draw(
                        glowTex,
                        drawPos + tailOffset,
                        null,
                        tailColor,
                        life.Rotation,
                        glowTex.Size() / 2f,
                        scale * 0.8f,
                        SpriteEffects.None,
                        0
                    );
                }
            }
        }

        //============================================================
        //头部水球 —— Shader 渲染（OceanWaterBlob.fx）
        //由 SoftGlow 圆点贴图作为基底，shader 注入折射、焦散、生物荧光
        //核心条带本身由 IPrimitiveDrawable + OceanCurrentTrail.fx 渲染
        //============================================================
        private void DrawStreamCoreShader(SpriteBatch sb) {
            Texture2D glow = CWRAsset.SoftGlow.Value;
            Vector2 headPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = glow.Size() * 0.5f;

            float headPulse = 0.85f + 0.15f * MathF.Sin((float)Main.timeForVisualEffects * 0.18f);
            float baseScale = headPulse * streamFade;

            //外层柔光（深海→生物荧光的过渡环）：以蓝绿为主，避免被白色弥散稀释
            Color outerTint = Color.Lerp(ShallowOcean, BioluminescentBlue, 0.65f + 0.20f * glowPulse);
            outerTint.A = 0;
            sb.Draw(glow, headPos, null, outerTint * (streamFade * 0.70f), 0f, origin,
                1.55f * baseScale, SpriteEffects.None, 0f);

            //内核高亮（青蓝水头）：用 BioluminescentBlue → OceanHotSpark 过渡，绝不再混 Color.White
            //Additive 下任何接近 (1,1,1) 的色都会把背景"煮"成白；保持 R 通道偏低让蓝色统治
            Color coreTint = Color.Lerp(BioluminescentBlue, OceanHotSpark, 0.55f);
            coreTint.A = 0;
            sb.Draw(glow, headPos, null, coreTint * (streamFade * glowPulse * 0.80f), 0f, origin,
                0.65f * baseScale, SpriteEffects.None, 0f);
        }

        //回退路径：无 Shader 时的简化头部
        private void DrawStreamCore() {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D glow = CWRAsset.SoftGlow.Value;
            Vector2 headPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = glow.Size() * 0.5f;

            Color outerTint = BioluminescentBlue * glowPulse;
            sb.Draw(glow, headPos, null, outerTint * streamFade, 0f, origin,
                1.4f * streamFade, SpriteEffects.None, 0);
            sb.Draw(glow, headPos, null, Color.White * (streamFade * glowPulse * 0.75f), 0f, origin,
                0.5f * streamFade, SpriteEffects.None, 0);
        }

        public override void OnKill(int timeLeft) {
            //死亡时残留飞溅
            if (State == OceanState.Streaming) {
                CreateOceanSplash(Projectile.Center, Projectile.velocity * 0.6f);
            }

            //残留水效果
            for (int i = 0; i < 25; i++) {
                Dust water = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(25f, 25f),
                    DustID.Water,
                    Main.rand.NextVector2Circular(5f, 5f),
                    100,
                    default,
                    Main.rand.NextFloat(1.5f, 2.5f)
                );
                water.noGravity = Main.rand.NextBool();
                water.fadeIn = 1.3f;
            }

            //泡沫爆发
            for (int i = 0; i < 15; i++) {
                Dust foam = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Smoke,
                    Main.rand.NextVector2Circular(4f, 4f),
                    100,
                    OceanFoam,
                    Main.rand.NextFloat(1f, 2f)
                );
                foam.noGravity = true;
                foam.fadeIn = 1.1f;
            }

            Projectile.damage = trueDmg / 2;
            Projectile.Explode(100, default, false);
        }

        public override Color? GetAlpha(Color lightColor) {
            float alphaMult = 1f - Projectile.alpha / 255f;
            return Color.Lerp(ShallowOcean, BioluminescentBlue, glowPulse) * alphaMult;
        }
    }

    #region 粒子数据结构

    ///<summary>
    ///海洋水滴粒子
    ///</summary>
    internal struct OceanDroplet
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Size;
        public int Life;
        public int MaxLife;
        public float Rotation;
        public float RotationSpeed;
        public float Opacity;
        public bool IsSplash;
        public float ColorVariant; //0-1，用于颜色变化
        public int Frame; //3x3 Spray帧索引0-8
    }

    ///<summary>
    ///海洋泡沫粒子
    ///</summary>
    internal struct SeaFoam
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Size;
        public int Life;
        public int MaxLife;
        public float Rotation;
        public float RotationSpeed;
        public float Opacity;
        public float PopPhase; //0-1，破裂阶段
    }

    ///<summary>
    ///海洋生物类型
    ///</summary>
    internal enum MarineLifeType
    {
        Fish,       //小鱼
        Seaweed     //海藻
    }

    ///<summary>
    ///海洋生物粒子
    ///</summary>
    internal struct MarineLife
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Size;
        public int Life;
        public int MaxLife;
        public float Rotation;
        public float RotationSpeed;
        public float Opacity;
        public MarineLifeType Type;
        public float SwimPhase;     //游动动画相位
        public float FlickerPhase;  //闪烁相位（生物发光）
    }

    #endregion
}
