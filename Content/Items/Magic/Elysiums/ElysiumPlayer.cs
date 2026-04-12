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

        //12门徒的名称
        public static readonly string[] DiscipleNames = [
            "西门彼得", "圣安德鲁", "雅各布", "圣约翰",
            "腓力", "巴多罗买", "多马", "圣马修",
            "雅各", "达泰", "西门", "犹大"
        ];

        //当前激活的门徒弹幕索引列表
        public List<int> ActiveDisciples = [];

        //犹大背刺阈值(生命百分比)
        public const float JudasBetrayalThreshold = 0.3f;

        //上次检查犹大背刺的时间
        private int judasBetrayalCooldown = 0;

        //殉道追踪系统
        public bool[] Martyred; //记录12门徒中哪些已殉道
        public bool IsRevelationActive; //启示录阶段是否激活
        private int revelationDuration; //启示录剩余持续时间

        public override void Initialize() {
            Martyred = new bool[12];
            IsRevelationActive = false;
            revelationDuration = 0;
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
            //犹大背刺检测
            if (judasBetrayalCooldown > 0) {
                judasBetrayalCooldown--;
            }

            if (GetDiscipleCount() == 12 && judasBetrayalCooldown <= 0) {
                CheckJudasBetrayal();
            }

            //启示录阶段倒计时
            if (IsRevelationActive) {
                revelationDuration--;
                if (revelationDuration <= 0) {
                    IsRevelationActive = false;
                    //重置殉道状态，允许重新召唤门徒
                    Array.Clear(Martyred, 0, 12);
                }
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
                CombatText.NewText(player.Hitbox, Color.Gold, "门徒已满");
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
                CombatText.NewText(player.Hitbox, Color.Gray, "附近没有可转化的居民");
                return;
            }

            //找到下一个可用的门徒类型
            int nextDiscipleType = GetNextAvailableDiscipleType();
            if (nextDiscipleType == -1) {
                CombatText.NewText(player.Hitbox, Color.Gold, "门徒已满");
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

        /// <summary>
        /// 激活启示录阶段：约翰殉道，展开天国领域
        /// </summary>
        public void ActivateRevelation(Player player) {
            //约翰殉道
            Martyred[3] = true;
            RemoveDiscipleByType(ModContent.ProjectileType<John>());
            IsRevelationActive = true;
            revelationDuration = 600; //10秒持续时间

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

            CombatText.NewText(player.Hitbox, Color.Gold, "启示录", true);
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
                string name = idx >= 0 && idx < DiscipleNames.Length ? DiscipleNames[idx] : "门徒";
                CombatText.NewText(npc.Hitbox, Color.Gold, $"{name} 已加入");

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
                CombatText.NewText(Player.Hitbox, Color.DarkRed, "犹大的背叛!", true);

                //造成斩杀伤害
                int betrayalDamage = Player.statLife + 100;
                Player.Hurt(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral($"{Player.name} 被犹大背叛了")), betrayalDamage, 0);

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
            if (count == 0) return;

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

                                CombatText.NewText(proj.Hitbox, Color.Red, $"{disciple.DiscipleName} 殉道了");
                                //显示能量增长提示
                                int energy = GetMartyrdomEnergy();
                                CombatText.NewText(Player.Hitbox, Color.Gold, $"殉道之力 {energy}/11");

                                proj.Kill();
                                ActiveDisciples.RemoveAt(chosenListIndex);
                            }
                        }
                    }
                }
            }
        }
    }
}
