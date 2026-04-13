using CalamityOverhaul.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    /// <summary>
    /// 管理天国极乐武器的门徒系统
    /// </summary>
    internal class ElysiumPlayer : ModPlayer
    {
        //12门徒的类型数组(按顺序)
        public static readonly int[] DiscipleTypes = [
            ModContent.ProjectileType<SimonPeter>(),    //0: 西门彼得
            ModContent.ProjectileType<Andrew>(),         //1: 圣安德鲁
            ModContent.ProjectileType<James>(),          //2: 雅各布
            ModContent.ProjectileType<John>(),           //3: 圣约翰
            ModContent.ProjectileType<Philip>(),         //4: 腓力
            ModContent.ProjectileType<Bartholomew>(),    //5: 巴多罗买
            ModContent.ProjectileType<Thomas>(),         //6: 多马
            ModContent.ProjectileType<Matthew>(),        //7: 圣马修
            ModContent.ProjectileType<Lesser>(),         //8: 雅各(小)
            ModContent.ProjectileType<Jude>(),           //9: 达泰
            ModContent.ProjectileType<Zealot>(),         //10: 西门(狂热者)
            ModContent.ProjectileType<JudasIscariot>()   //11: 犹大
        ];

        //12门徒的名称(从Elysium注册的本地化文本中获取)
        public static string GetDiscipleName(int index) => Elysium.DiscipleNameTexts[index].Value;

        //当前激活的门徒弹幕索引列表
        public List<int> ActiveDisciples = [];

        //犹大背刺阈值(生命百分比)
        public const float JudasBetrayalThreshold = 0.3f;

        //上次检查犹大背刺的时间
        private int judasBetrayalCooldown = 0;

        //殉道追踪系统
        public bool[] Martyred; //记录12门徒中哪些已殉道
        public bool IsRevelationActive; //启示录阶段是否激活

        //天启四骑士
        public bool[] SummonedHorsemen; //0瘟疫 1战争 2饥荒 3死亡
        public static string GetHorsemanName(int index) => Elysium.HorsemanNameTexts[index].Value;

        //启示录战斗状态
        public int RevelationMeteorCooldown;
        public bool IsSealJudgmentActive;

        public override void Initialize() {
            Martyred = new bool[12];
            IsRevelationActive = false;
            SummonedHorsemen = new bool[4];
            RevelationMeteorCooldown = 0;
            IsSealJudgmentActive = false;
        }

        public override void ResetEffects() {
            //清理无效的门徒引用
            ActiveDisciples.RemoveAll(i => i < 0 || i >= Main.maxProjectiles || !Main.projectile[i].active
                || !IsDiscipleProjectile(Main.projectile[i])
                || Main.projectile[i].owner != Player.whoAmI);
        }

        /// <summary>
        /// 检查弹幕是否是门徒类型
        /// </summary>
        private static bool IsDiscipleProjectile(Projectile proj) {
            return proj.ModProjectile is BaseDisciple;
        }

        public override void PostUpdate() {
            if (IsRevelationActive && !HasElysiumInInventory()) {
                //防止极端情况下状态残留：没有武器本体时强制终止启示录
                DeactivateRevelation(Player);
                return;
            }

            if (RevelationMeteorCooldown > 0) {
                RevelationMeteorCooldown--;
            }

            //犹大背刺检测
            if (judasBetrayalCooldown > 0) {
                judasBetrayalCooldown--;
            }

            if (GetDiscipleCount() == 12 && judasBetrayalCooldown <= 0) {
                CheckJudasBetrayal();
            }

            if (Player.dead && IsRevelationActive) {
                DeactivateRevelation(Player, false);
            }

            if (IsRevelationActive) {
                SyncHorsemanState();
                SyncJudgmentState();
            }

            //门徒增益效果
            ApplyDiscipleBonuses();
        }

        /// <summary>
        /// 获取当前门徒数量
        /// </summary>
        public int GetDiscipleCount() {
            return ActiveDisciples.Count(i => i >= 0 && i < Main.maxProjectiles
                && Main.projectile[i].active
                && IsDiscipleProjectile(Main.projectile[i])
                && Main.projectile[i].owner == Player.whoAmI);
        }

        /// <summary>
        /// 检查是否已拥有指定类型的门徒
        /// </summary>
        public bool HasDiscipleOfType(int projectileType) {
            return ActiveDisciples.Any(i => i >= 0 && i < Main.maxProjectiles
                && Main.projectile[i].active
                && Main.projectile[i].type == projectileType
                && Main.projectile[i].owner == Player.whoAmI);
        }

        /// <summary>
        /// 尝试将最近的城镇NPC转化为门徒
        /// </summary>
        public void TryConvertNearestNPC(Player player) {
            int currentCount = GetDiscipleCount();
            if (currentCount >= 12) {
                CombatText.NewText(player.Hitbox, Color.Gold, Elysium.DiscipleFullText.Value);
                return;
            }

            //寻找最近的城镇NPC
            float maxDist = 300f;
            NPC targetNPC = null;
            float closestDist = maxDist;

            foreach (NPC npc in Main.npc) {
                if (!npc.active || !npc.townNPC || npc.homeless) continue;
                float dist = Vector2.Distance(player.Center, npc.Center);
                if (dist < closestDist) {
                    closestDist = dist;
                    targetNPC = npc;
                }
            }

            if (targetNPC == null) {
                CombatText.NewText(player.Hitbox, Color.Gray, Elysium.NoConvertTargetText.Value);
                return;
            }

            //找到下一个可用的门徒类型
            int nextDiscipleType = GetNextAvailableDiscipleType();
            if (nextDiscipleType == -1) {
                CombatText.NewText(player.Hitbox, Color.Gold, Elysium.DiscipleFullText.Value);
                return;
            }

            //转化NPC为门徒
            ConvertToDisciple(player, targetNPC, nextDiscipleType, currentCount);
        }

        /// <summary>
        /// 获取下一个可用的门徒类型(跳过已殉道的)
        /// </summary>
        private int GetNextAvailableDiscipleType() {
            for (int i = 0; i < DiscipleTypes.Length; i++) {
                if (Martyred != null && Martyred[i]) continue; //已殉道的不可重新召唤
                if (!HasDiscipleOfType(DiscipleTypes[i])) {
                    return DiscipleTypes[i];
                }
            }
            return -1;
        }

        /// <summary>
        /// 获取殉道能量(约翰除外的已殉道门徒数)
        /// </summary>
        public int GetMartyrdomEnergy() {
            if (Martyred == null) return 0;
            int count = 0;
            for (int i = 0; i < 12; i++) {
                if (i == 3) continue; //约翰不计入被动殉道能量
                if (Martyred[i]) count++;
            }
            return count;
        }

        public int GetHorsemanCount() {
            if (SummonedHorsemen == null) return 0;
            int count = 0;
            for (int i = 0; i < SummonedHorsemen.Length; i++) {
                if (SummonedHorsemen[i]) count++;
            }
            return count;
        }

        public bool HasHorseman(int horsemanIndex) {
            return SummonedHorsemen != null
                && horsemanIndex >= 0
                && horsemanIndex < SummonedHorsemen.Length
                && SummonedHorsemen[horsemanIndex];
        }

        public bool HasDeathAmplification() {
            return IsRevelationActive && HasHorseman(3);
        }

        public bool HasElysiumInInventory() {
            int elysiumType = ModContent.ItemType<Elysium>();
            if (Player.HeldItem != null && Player.HeldItem.type == elysiumType) {
                return true;
            }

            foreach (Item item in Player.inventory) {
                if (item != null && !item.IsAir && item.type == elysiumType) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 激活启示录阶段：约翰殉道，展开天国领域
        /// </summary>
        public void ActivateRevelation(Player player) {
            //约翰殉道
            Martyred[3] = true;
            RemoveDiscipleByType(ModContent.ProjectileType<John>());
            IsRevelationActive = true;
            Array.Clear(SummonedHorsemen, 0, SummonedHorsemen.Length);

            //播放神圣雷鸣音效
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 2f, Pitch = -0.3f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item105 with { Volume = 1.5f, Pitch = 0.3f }, player.Center);

            //神圣粒子爆发
            for (int i = 0; i < 80; i++) {
                Vector2 vel = Main.rand.NextVector2Circular(8f, 8f);
                int dustType = Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.SilverFlame;
                Dust d = Dust.NewDustPerfect(player.Center, dustType, vel, 80, default, 2.5f);
                d.noGravity = true;
            }

            //生成天国领域弹幕
            ShootState shootState = player.GetShootState();
            Projectile.NewProjectile(
                shootState.Source, player.Center, Vector2.Zero,
                ModContent.ProjectileType<RevelationDomain>(),
                shootState.WeaponDamage * 3, 0f, player.whoAmI
            );

            CombatText.NewText(player.Hitbox, Color.Gold, Elysium.RevelationText.Value, true);
        }

        public void DeactivateRevelation(Player player, bool clearMartyrdom = true) {
            if (!IsRevelationActive) {
                return;
            }

            IsRevelationActive = false;
            IsSealJudgmentActive = false;
            RevelationMeteorCooldown = 0;
            Array.Clear(SummonedHorsemen, 0, SummonedHorsemen.Length);

            foreach (Projectile projectile in Main.projectile) {
                if (!projectile.active || projectile.owner != player.whoAmI) {
                    continue;
                }

                if (projectile.type == ModContent.ProjectileType<RevelationDomain>()
                    || projectile.type == ModContent.ProjectileType<ApocalypseHorseman>()
                    || projectile.type == ModContent.ProjectileType<RevelationSealJudgment>()
                    || projectile.type == ModContent.ProjectileType<RevelationMeteorStrike>()
                    || projectile.type == ModContent.ProjectileType<RevelationMeteorImpact>()) {
                    projectile.Kill();
                }
            }

            if (clearMartyrdom) {
                Array.Clear(Martyred, 0, Martyred.Length);
            }

            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 1.2f, Pitch = -0.15f }, player.Center);
            CombatText.NewText(player.Hitbox, Color.Silver, Elysium.RevelationEndText.Value);
        }

        public bool CanCastRevelationMeteor() {
            return IsRevelationActive && !IsSealJudgmentActive && RevelationMeteorCooldown <= 0;
        }

        public void CastRevelationMeteor(Player player) {
            if (!CanCastRevelationMeteor()) {
                return;
            }

            Vector2 target = Main.MouseWorld;
            ShootState shootState = player.GetShootState();

            int damage = (int)(shootState.WeaponDamage * (HasDeathAmplification() ? 2.4f : 1.8f));
            if (HasHorseman(1)) {
                damage = (int)(damage * (HasDeathAmplification() ? 1.35f : 1.2f));
            }

            Projectile.NewProjectile(
                shootState.Source,
                new Vector2(target.X + Main.rand.NextFloat(-180f, 180f), target.Y - 900f),
                Vector2.Zero,
                ModContent.ProjectileType<RevelationMeteorStrike>(),
                damage,
                8f,
                player.whoAmI,
                target.X,
                target.Y
            );

            RevelationMeteorCooldown = HasDeathAmplification() ? 12 : 24;

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 1.05f, Pitch = -0.1f }, target);
            CombatText.NewText(player.Hitbox, Color.Gold, Elysium.CelestialMeteorText.Value, true);
        }

        public bool CanTriggerSealJudgment() {
            return IsRevelationActive && !IsSealJudgmentActive;
        }

        public void TriggerSealJudgment(Player player) {
            if (!CanTriggerSealJudgment()) {
                return;
            }

            ShootState shootState = player.GetShootState();
            Projectile.NewProjectile(
                shootState.Source,
                player.Center,
                Vector2.Zero,
                ModContent.ProjectileType<RevelationSealJudgment>(),
                (int)(shootState.WeaponDamage * 4.2f),
                12f,
                player.whoAmI
            );

            IsSealJudgmentActive = true;
            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 1.25f, Pitch = -0.25f }, player.Center);
            CombatText.NewText(player.Hitbox, Color.OrangeRed, Elysium.SealStarsText.Value, true);
        }

        public void SummonNextHorseman(Player player) {
            if (!IsRevelationActive) {
                return;
            }

            int nextIndex = GetHorsemanCount();
            if (nextIndex >= 4) {
                CombatText.NewText(player.Hitbox, Color.Gray, Elysium.HorsemenFullText.Value);
                return;
            }

            if (SummonedHorsemen[nextIndex]) {
                return;
            }

            HorsemanStyle style = HorsemanCatalog.Get(nextIndex);
            Vector2 summonDirection = (MathHelper.PiOver2 * nextIndex - MathHelper.PiOver4).ToRotationVector2();
            Vector2 summonPosition = player.Center + summonDirection * style.EntryRadius + new Vector2(0f, -style.EntryHeight * 0.45f);

            ShootState shootState = player.GetShootState();
            Projectile.NewProjectile(
                shootState.Source,
                summonPosition,
                Vector2.Zero,
                ModContent.ProjectileType<ApocalypseHorseman>(),
                shootState.WeaponDamage,
                0f,
                player.whoAmI,
                nextIndex,
                Main.GameUpdateCount % 1000
            );

            SummonedHorsemen[nextIndex] = true;

            for (int i = 0; i < 32; i++) {
                float angle = MathHelper.TwoPi * i / 32f;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(2.5f, 7f) + summonDirection * 1.5f;
                Dust dust = Dust.NewDustPerfect(summonPosition, style.DustType, velocity, 80, style.PrimaryColor, 1.45f);
                dust.noGravity = true;
                dust.fadeIn = 1f;
            }

            for (int i = 0; i < 16; i++) {
                Vector2 lineVelocity = summonDirection.RotatedBy(Main.rand.NextFloat(-0.35f, 0.35f)) * Main.rand.NextFloat(6f, 14f);
                Dust streak = Dust.NewDustPerfect(player.Center + summonDirection * 20f, style.DustType, lineVelocity, 60, style.SecondaryColor, 1.2f);
                streak.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Item117 with { Volume = 1.25f, Pitch = -0.22f + nextIndex * 0.12f }, summonPosition);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.8f, Pitch = -0.45f + nextIndex * 0.08f }, summonPosition);
            CombatText.NewText(player.Hitbox, style.TextColor, Elysium.HorsemanArrivalText.Format(GetHorsemanName(nextIndex)), true);
        }

        private void SyncHorsemanState() {
            if (SummonedHorsemen == null) {
                return;
            }

            for (int i = 0; i < SummonedHorsemen.Length; i++) {
                if (!SummonedHorsemen[i]) {
                    continue;
                }

                bool exists = false;
                foreach (Projectile projectile in Main.projectile) {
                    if (!projectile.active || projectile.owner != Player.whoAmI) {
                        continue;
                    }

                    if (projectile.type == ModContent.ProjectileType<ApocalypseHorseman>()
                        && (int)projectile.ai[0] == i) {
                        exists = true;
                        break;
                    }
                }

                if (!exists) {
                    SummonedHorsemen[i] = false;
                }
            }
        }

        private void SyncJudgmentState() {
            if (!IsSealJudgmentActive) {
                return;
            }

            bool exists = false;
            foreach (Projectile projectile in Main.projectile) {
                if (!projectile.active || projectile.owner != Player.whoAmI) {
                    continue;
                }

                if (projectile.type == ModContent.ProjectileType<RevelationSealJudgment>()) {
                    exists = true;
                    break;
                }
            }

            if (!exists) {
                IsSealJudgmentActive = false;
            }
        }

        /// <summary>
        /// 获取门徒类型的索引
        /// </summary>
        private static int GetDiscipleIndex(int projectileType) {
            for (int i = 0; i < DiscipleTypes.Length; i++) {
                if (DiscipleTypes[i] == projectileType) {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 将NPC转化为门徒
        /// </summary>
        private void ConvertToDisciple(Player player, NPC npc, int discipleType, int discipleIndex) {
            //播放转化音效
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 1.5f, Pitch = 0.2f }, npc.Center);

            //生成圣光特效
            for (int i = 0; i < 50; i++) {
                Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                Dust d = Dust.NewDustPerfect(npc.Center, DustID.GoldFlame, vel, 100, default, 2f);
                d.noGravity = true;
            }

            //生成对应类型的门徒弹幕
            int proj = Projectile.NewProjectile(
                player.GetSource_ItemUse(player.HeldItem),
                npc.Center,
                Vector2.Zero,
                discipleType,
                0,
                0,
                player.whoAmI,
                npc.whoAmI //ai[0]存储原NPC索引用于位置参考
            );

            if (proj >= 0 && proj < Main.maxProjectiles) {
                ActiveDisciples.Add(proj);

                //获取门徒名称
                int idx = GetDiscipleIndex(discipleType);
                string name = idx >= 0 && idx < 12 ? GetDiscipleName(idx) : Elysium.DiscipleFallbackText.Value;
                CombatText.NewText(npc.Hitbox, Color.Gold, Elysium.DiscipleJoinedText.Format(name));

                //让原NPC消失(进入门徒状态)
                npc.active = false;
                npc.netUpdate = true;
            }
        }

        /// <summary>
        /// 检测犹大背刺(12门徒时的斩杀机制)
        /// </summary>
        private void CheckJudasBetrayal() {
            //检查是否拥有犹大
            if (!HasDiscipleOfType(ModContent.ProjectileType<JudasIscariot>())) {
                return;
            }

            float healthPercent = (float)Player.statLife / Player.statLifeMax2;

            if (healthPercent <= JudasBetrayalThreshold) {
                //犹大的背叛！
                judasBetrayalCooldown = 600; //10秒冷却

                //播放背叛音效
                SoundEngine.PlaySound(SoundID.NPCDeath59 with { Volume = 2f, Pitch = -0.5f }, Player.Center);

                //显示背叛文字
                CombatText.NewText(Player.Hitbox, Color.DarkRed, Elysium.JudasBetrayalText.Value, true);

                //造成斩杀伤害
                int betrayalDamage = Player.statLife + 100;
                Player.Hurt(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(Elysium.JudasDeathReasonText.Format(Player.name))), betrayalDamage, 0);

                //犹大门徒消失
                RemoveDiscipleByType(ModContent.ProjectileType<JudasIscariot>());
            }
        }

        /// <summary>
        /// 移除指定类型的门徒
        /// </summary>
        public void RemoveDiscipleByType(int projectileType) {
            for (int i = ActiveDisciples.Count - 1; i >= 0; i--) {
                int projIndex = ActiveDisciples[i];
                if (projIndex >= 0 && projIndex < Main.maxProjectiles) {
                    Projectile proj = Main.projectile[projIndex];
                    if (proj.active && proj.type == projectileType) {
                        proj.Kill();
                        ActiveDisciples.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 将一个门徒交换到另一个槽位（改变门徒身份）
        /// </summary>
        /// <param name="fromSlotIndex">源槽位索引（0-11）</param>
        /// <param name="toSlotIndex">目标槽位索引（0-11）</param>
        /// <returns>是否交换成功</returns>
        public bool SwapDiscipleToSlot(int fromSlotIndex, int toSlotIndex) {
            if (fromSlotIndex < 0 || fromSlotIndex >= 12 || toSlotIndex < 0 || toSlotIndex >= 12) {
                return false;
            }

            if (fromSlotIndex == toSlotIndex) {
                return false;
            }

            int fromType = DiscipleTypes[fromSlotIndex];
            int toType = DiscipleTypes[toSlotIndex];

            bool hasFromDisciple = HasDiscipleOfType(fromType);
            bool hasToDisciple = HasDiscipleOfType(toType);

            if (!hasFromDisciple) {
                return false; //源位置没有门徒，无法交换
            }

            //找到源门徒的弹幕
            int fromProjIndex = -1;
            Vector2 fromPosition = Player.Center;

            for (int i = 0; i < ActiveDisciples.Count; i++) {
                int projIdx = ActiveDisciples[i];
                if (projIdx >= 0 && projIdx < Main.maxProjectiles) {
                    Projectile proj = Main.projectile[projIdx];
                    if (proj.active && proj.type == fromType && proj.owner == Player.whoAmI) {
                        fromProjIndex = i;
                        fromPosition = proj.Center;
                        break;
                    }
                }
            }

            if (fromProjIndex < 0) {
                return false;
            }

            //如果目标位置也有门徒，需要双向交换
            int toProjIndex = -1;
            Vector2 toPosition = Player.Center;

            if (hasToDisciple) {
                for (int i = 0; i < ActiveDisciples.Count; i++) {
                    int projIdx = ActiveDisciples[i];
                    if (projIdx >= 0 && projIdx < Main.maxProjectiles) {
                        Projectile proj = Main.projectile[projIdx];
                        if (proj.active && proj.type == toType && proj.owner == Player.whoAmI) {
                            toProjIndex = i;
                            toPosition = proj.Center;
                            break;
                        }
                    }
                }
            }

            //执行交换：删除旧弹幕，创建新弹幕

            //删除源门徒
            if (fromProjIndex >= 0 && ActiveDisciples[fromProjIndex] >= 0) {
                Main.projectile[ActiveDisciples[fromProjIndex]].Kill();
                ActiveDisciples.RemoveAt(fromProjIndex);

                //调整目标索引（如果目标在源之后）
                if (toProjIndex > fromProjIndex) {
                    toProjIndex--;
                }
            }

            //删除目标门徒（如果有）
            if (toProjIndex >= 0 && toProjIndex < ActiveDisciples.Count && ActiveDisciples[toProjIndex] >= 0) {
                Main.projectile[ActiveDisciples[toProjIndex]].Kill();
                ActiveDisciples.RemoveAt(toProjIndex);
            }

            //在源位置创建目标类型的新门徒（如果原来有目标门徒）
            if (hasToDisciple) {
                int newFromProj = Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    fromPosition,
                    Vector2.Zero,
                    fromType, //源类型现在在目标位置
                    0, 0,
                    Player.whoAmI
                );

                if (newFromProj >= 0 && newFromProj < Main.maxProjectiles) {
                    ActiveDisciples.Add(newFromProj);
                }
            }

            //在目标位置创建源类型的新门徒
            int newToProj = Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                toPosition,
                Vector2.Zero,
                toType, //目标类型
                0, 0,
                Player.whoAmI
            );

            if (newToProj >= 0 && newToProj < Main.maxProjectiles) {
                ActiveDisciples.Add(newToProj);
            }

            //生成转换特效
            SpawnSwapEffect(fromPosition, toPosition);

            return true;
        }

        /// <summary>
        /// 生成交换特效
        /// </summary>
        private void SpawnSwapEffect(Vector2 from, Vector2 to) {
            //在两个位置生成金色圣光粒子
            for (int i = 0; i < 20; i++) {
                Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                Dust d1 = Dust.NewDustPerfect(from, DustID.GoldFlame, vel, 100, default, 1.5f);
                d1.noGravity = true;

                Dust d2 = Dust.NewDustPerfect(to, DustID.GoldFlame, vel, 100, default, 1.5f);
                d2.noGravity = true;
            }

            //沿路径生成粒子
            int steps = 10;
            for (int i = 0; i <= steps; i++) {
                float t = i / (float)steps;
                Vector2 pos = Vector2.Lerp(from, to, t);
                //弧形偏移
                float arc = MathF.Sin(t * MathHelper.Pi) * 20f;
                Vector2 perpendicular = (to - from).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                pos += perpendicular * arc;

                Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, Vector2.Zero, 100, default, 1f);
                d.noGravity = true;
            }
        }

        /// <summary>
        /// 应用门徒增益效果
        /// </summary>
        private void ApplyDiscipleBonuses() {
            int count = GetDiscipleCount();
            if (count > 0) {
                //11门徒时的强力增益(没有犹大的危险)
                if (count == 11) {
                    Player.GetDamage(DamageClass.Generic) += 0.25f;
                    Player.GetCritChance(DamageClass.Generic) += 15;
                    Player.statDefense += 20;
                    Player.lifeRegen += 5;
                }
                //12门徒时的超强增益(但有犹大背刺风险)
                else if (count == 12) {
                    Player.GetDamage(DamageClass.Generic) += 0.50f;
                    Player.GetCritChance(DamageClass.Generic) += 30;
                    Player.statDefense += 40;
                    Player.lifeRegen += 10;
                    Player.moveSpeed += 0.3f;
                }
                //其他数量按比例增益
                else {
                    float ratio = count / 12f;
                    Player.GetDamage(DamageClass.Generic) += 0.15f * ratio;
                    Player.GetCritChance(DamageClass.Generic) += (int)(10 * ratio);
                    Player.statDefense += (int)(15 * ratio);
                }
            }

            if (IsRevelationActive) {
                bool deathAmp = HasDeathAmplification();

                if (HasHorseman(1)) {
                    Player.GetDamage(DamageClass.Generic) += deathAmp ? 0.8f : 0.42f;
                    Player.GetCritChance(DamageClass.Generic) += deathAmp ? 40 : 22;
                }

                if (HasHorseman(2)) {
                    Player.GetArmorPenetration(DamageClass.Generic) += deathAmp ? 120 : 60;
                }

                if (HasHorseman(0)) {
                    Player.GetDamage(DamageClass.Generic) += deathAmp ? 0.18f : 0.08f;
                }
            }
        }

        /// <summary>
        /// 当玩家被Boss伤害时，门徒可能会殉道(约翰除外)
        /// </summary>
        public override void OnHurt(Player.HurtInfo info) {
            //检查是否被Boss攻击
            if (info.DamageSource.TryGetCausingEntity(out Entity entity)) {
                if (entity is NPC npc && (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type])) {
                    //Boss攻击必定杀死一个门徒
                    if (ActiveDisciples.Count > 0) {
                        //收集可殉道的门徒(排除约翰，index=3)
                        List<int> eligible = [];
                        for (int i = 0; i < ActiveDisciples.Count; i++) {
                            int projIdx = ActiveDisciples[i];
                            if (projIdx >= 0 && projIdx < Main.maxProjectiles && Main.projectile[projIdx].active) {
                                if (Main.projectile[projIdx].ModProjectile is BaseDisciple disc && disc.DiscipleIndex != 3) {
                                    eligible.Add(i);
                                }
                            }
                        }

                        if (eligible.Count > 0) {
                            int chosenListIndex = eligible[Main.rand.Next(eligible.Count)];
                            int projIndex = ActiveDisciples[chosenListIndex];
                            Projectile proj = Main.projectile[projIndex];

                            if (proj.ModProjectile is BaseDisciple disciple) {
                                int dIdx = disciple.DiscipleIndex;
                                if (Martyred != null) Martyred[dIdx] = true;

                                CombatText.NewText(proj.Hitbox, Color.Red, Elysium.DiscipleMartyrText.Format(disciple.DiscipleName));
                                //显示能量增长提示
                                int energy = GetMartyrdomEnergy();
                                CombatText.NewText(Player.Hitbox, Color.Gold, Elysium.MartyrdomPowerText.Format(energy));

                                proj.Kill();
                                ActiveDisciples.RemoveAt(chosenListIndex);
                            }
                        }
                    }
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
            if (IsRevelationActive && HasHorseman(2)) {
                modifiers.DefenseEffectiveness *= 0f;
                modifiers.ScalingArmorPenetration += HasDeathAmplification() ? 1.2f : 0.75f;
            }
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
            if (IsRevelationActive && HasHorseman(2) && proj.owner == Player.whoAmI) {
                modifiers.DefenseEffectiveness *= 0f;
                modifiers.ScalingArmorPenetration += HasDeathAmplification() ? 1.2f : 0.75f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            ApplyPlagueMark(target);
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
            ApplyPlagueMark(target);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
            if (proj.owner == Player.whoAmI) {
                ApplyPlagueMark(target);
            }
        }

        private void ApplyPlagueMark(NPC target) {
            if (!IsRevelationActive || !HasHorseman(0) || target == null || !target.active || target.friendly) {
                return;
            }

            int plagueTime = HasDeathAmplification() ? 420 : 240;
            float plagueIntensity = HasDeathAmplification() ? 1.35f : 0.8f;
            RevelationPlagueNPC.Apply(target, plagueTime, plagueIntensity, Player.whoAmI);
        }
    }
}
