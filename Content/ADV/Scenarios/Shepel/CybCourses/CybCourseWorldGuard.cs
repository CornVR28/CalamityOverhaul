using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

namespace CalamityOverhaul.Content.ADV.Scenarios.Shepel.CybCourses
{
    //进出超梦教程子世界时的主世界数据防护层
    //职责：快照灾厄Boss击杀进度 + 城镇NPC列表，回到主世界后补全丢失数据
    //快照时机：CybCourse.Enter() 在切换世界前调用 Snapshot()
    //恢复时机：CybCoursePlayer.OnEnterWorld() 确认已回到主世界后调用 RestoreOnReturn()
    internal static class CybCourseWorldGuard
    {
        //记录进入子世界前灾厄Boss击杀标志的快照
        private static Dictionary<string, bool> _calBossFlags;

        private readonly record struct NpcEntry(int Type, int X, int Y);
        //记录进入子世界前存活的城镇NPC
        private static List<NpcEntry> _npcSnapshot;

        //进入子世界前调用，同时拍摄Boss进度与城镇NPC快照
        internal static void Snapshot() {
            SnapshotCalamityFlags();
            SnapshotTownNPCs();
        }

        //拍摄灾厄Boss击杀标志（仅在灾厄Mod存在时生效）
        private static void SnapshotCalamityFlags() {
            _calBossFlags = new Dictionary<string, bool>(36);
            CWRRef.BulkCopyCalamityFlags((k, v) => _calBossFlags[k] = v);
        }

        //拍摄当前所有活跃城镇NPC的类型与坐标
        private static void SnapshotTownNPCs() {
            _npcSnapshot = new List<NpcEntry>(32);
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC n = Main.npc[i];
                if (n.active && n.townNPC)
                    _npcSnapshot.Add(new NpcEntry(n.type, (int)n.Center.X, (int)n.Center.Y));
            }
        }

        //回到主世界后统一恢复，在CybCoursePlayer.OnEnterWorld()中调用
        internal static void RestoreOnReturn() {
            RestoreCalamityFlags();
            RestoreTownNPCs();
        }

        //以OR方式将快照标志补写回灾厄系统（只补true，绝不清除已有的true）
        private static void RestoreCalamityFlags() {
            if (_calBossFlags is null) return;
            CWRRef.BulkRestoreCalamityFlagsOr(k => _calBossFlags.TryGetValue(k, out bool v) && v);
            _calBossFlags = null;
        }

        //补全快照中存在、但当前世界里已消失的城镇NPC
        private static void RestoreTownNPCs() {
            if (_npcSnapshot is null) return;
            //收集当前已存在的城镇NPC类型
            HashSet<int> present = new(64);
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC n = Main.npc[i];
                if (n.active && n.townNPC)
                    present.Add(n.type);
            }
            //补生缺失的城镇NPC，位置尽量还原到快照坐标
            foreach (NpcEntry e in _npcSnapshot) {
                if (!present.Contains(e.Type))
                    NPC.NewNPC(new EntitySource_Misc("CybCourse_NPCRestore"), e.X, e.Y, e.Type);
            }
            _npcSnapshot = null;
        }
    }
}
