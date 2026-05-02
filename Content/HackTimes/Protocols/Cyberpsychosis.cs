using CalamityOverhaul.Common;
using CalamityOverhaul.Content.HackTimes.Scannables;
using CalamityOverhaul.Content.PRTTypes;
using InnoVault.PRT;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Content.HackTimes.Protocols
{
    /// <summary>
    /// 赛博精神病：使目标陷入狂暴攻击周围一切单位
    /// <br/>对蠕虫类多体节 Boss 或 月总等多实体 Boss，会自动扩散到群组内全部成员
    /// </summary>
    internal class Cyberpsychosis : QuickHackDef
    {
        //群组扩散用的复用缓冲，避免每次施加都重新分配
        private static readonly List<NPC> groupBuffer = [];

        public override void SetDefaults() {
            UploadTime = 150;
            RamCost = 5;
            Category = QuickHackCategory.Control;
        }

        public override int GetDuration() => 60 * 8; //8秒持续

        public override bool OnApply(IHackTarget target, Player caster) {
            if (target is not NpcScannable s) return false;
            NPC npc = Main.npc[s.NpcIndex];
            //红色精神崩溃爆发
            EmitBurstParticles(npc);
            CombatText.NewText(npc.Hitbox, new Color(255, 0, 50), HackTime.Cyberpsychosis.Value, true);

            //扩散到群组其他成员，靠 HasEffect 短路避免无限递归
            //当前 NPC 已经在 tracker 中持有该效果，群组成员被 Apply 后会进入 pending 队列
            //它们下一帧的 OnApply 再来这里时，所有兄弟都会因 HasEffect 命中而被跳过
            NpcGroupHelper.CollectGroup(npc, groupBuffer);
            for (int i = 0; i < groupBuffer.Count; i++) {
                NPC member = groupBuffer[i];
                if (member.whoAmI == npc.whoAmI) continue;
                if (HackEffectTracker.HasEffect<Cyberpsychosis>(member.whoAmI)) continue;
                EmitBurstParticles(member);
                HackEffectTracker.Apply(this, member.whoAmI, caster?.whoAmI ?? Main.myPlayer);
            }
            groupBuffer.Clear();
            return true;
        }

        public override bool OnTick(IHackTarget target, int elapsed) {
            if (target is not NpcScannable s) return true;
            NPC npc = Main.npc[s.NpcIndex];
            //周期性红色故障粒子
            if (elapsed % 10 == 0) {
                Vector2 pos = npc.Center + Main.rand.NextVector2Circular(
                    npc.width * 0.4f, npc.height * 0.4f);
                Vector2 vel = Main.rand.NextVector2Circular(2f, 2f);
                PRTLoader.AddParticle(new PRT_Spark(pos, vel,
                    false, 15, 0.6f, new Color(255, 50, 50)));
            }
            return true;
        }

        public override void OnRemove(IHackTarget target) {
            if (target is not NpcScannable s) return;
            NPC npc = Main.npc[s.NpcIndex];
            //精神恢复闪光
            for (int i = 0; i < 5; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(2f, 2f);
                PRTLoader.AddParticle(new PRT_Spark(npc.Center, vel,
                    false, 15, 0.5f, new Color(180, 80, 80)));
            }
        }

        //初始爆发粒子，单独抽出便于群组成员复用同样的视觉表现
        private static void EmitBurstParticles(NPC npc) {
            for (int i = 0; i < 12; i++) {
                Vector2 vel = Main.rand.NextVector2CircularEdge(3.5f, 3.5f);
                PRTLoader.AddParticle(new PRT_Spark(npc.Center, vel,
                    false, 30, 1.0f, new Color(255, 30, 30)));
            }
        }
    }
}
