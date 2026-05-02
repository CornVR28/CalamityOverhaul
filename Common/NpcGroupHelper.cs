using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Common
{
    /// <summary>
    /// NPC 群组解析工具
    /// <br/>用于把"多实体共享生命/同一 Boss 体"的一组 NPC 视为整体，比如蠕虫类 Boss 的头部体节尾巴
    /// 月总核心和手部头部、毁灭者世界吞噬者这类既共享 <see cref="NPC.realLife"/>
    /// 又通过预定义类型表关联的实体集合
    /// <br/>该工具与具体玩法系统无关，可在骇客协议、技能 AOE、状态附加等任意场景下直接使用
    /// </summary>
    public static class NpcGroupHelper
    {
        /// <summary>
        /// 获取一个 NPC 所属群组的"锚点"索引（通常是头部或主体）
        /// <br/>当 <see cref="NPC.realLife"/> 指向一个有效活跃的 NPC 时返回该索引
        /// 否则返回自身 <see cref="NPC.whoAmI"/>，单实体 NPC 自己就是锚点
        /// </summary>
        public static int GetAnchorIndex(NPC npc) {
            if (npc == null || !npc.active) {
                return -1;
            }
            int rl = npc.realLife;
            if (rl >= 0 && rl < Main.maxNPCs && Main.npc[rl].active) {
                return rl;
            }
            return npc.whoAmI;
        }

        /// <summary>
        /// 判断两个 NPC 是否属于同一群组
        /// <br/>判定条件：1) 共享同一 <see cref="GetAnchorIndex"/> 锚点 2) 类型同属预定义 Boss 体节列表
        /// </summary>
        public static bool IsSameGroup(NPC a, NPC b) {
            if (a == null || b == null || !a.active || !b.active) {
                return false;
            }
            if (a.whoAmI == b.whoAmI) {
                return true;
            }
            //realLife 链接判定，覆盖所有蠕虫类 Boss 的体节
            int aa = ResolveAnchor(a);
            int bb = ResolveAnchor(b);
            if (aa == bb) {
                return true;
            }
            //类型表判定，覆盖月总、毁灭者等无 realLife 链接但同属一个 Boss 的多实体
            return ShareSegmentList(a.type, b.type);
        }

        /// <summary>
        /// 收集与 <paramref name="root"/> 同属一个群组的所有活跃 NPC，结果写入 <paramref name="output"/>
        /// <br/>实现仅扫描一遍 <see cref="Main.npc"/>，不存在递归，规避无限调用
        /// </summary>
        /// <param name="root">触发查询的成员，可以是头部也可以是任意体节</param>
        /// <param name="output">外部传入的容器，用于复用避免分配</param>
        /// <param name="clear">是否在写入前清空容器，默认 true</param>
        public static void CollectGroup(NPC root, List<NPC> output, bool clear = true) {
            if (output == null) {
                return;
            }
            if (clear) {
                output.Clear();
            }
            if (root == null || !root.active) {
                return;
            }
            int anchor = ResolveAnchor(root);
            //预先取出 root 类型对应的体节列表，省掉每次循环重复查找
            List<int> segList = FindSegmentList(root.type);

            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC n = Main.npc[i];
                if (!n.active) {
                    continue;
                }
                if (IsMember(n, anchor, segList)) {
                    output.Add(n);
                }
            }
        }

        /// <summary>
        /// 收集群组成员的索引集合
        /// </summary>
        public static void CollectGroupIndices(NPC root, List<int> output, bool clear = true) {
            if (output == null) {
                return;
            }
            if (clear) {
                output.Clear();
            }
            if (root == null || !root.active) {
                return;
            }
            int anchor = ResolveAnchor(root);
            List<int> segList = FindSegmentList(root.type);

            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC n = Main.npc[i];
                if (!n.active) {
                    continue;
                }
                if (IsMember(n, anchor, segList)) {
                    output.Add(n.whoAmI);
                }
            }
        }

        /// <summary>
        /// 直接返回新分配的群组列表，便于一次性使用
        /// 在热点路径上请使用 <see cref="CollectGroup(NPC, List{NPC}, bool)"/> 的复用版本
        /// </summary>
        public static List<NPC> GetGroup(NPC root) {
            List<NPC> list = [];
            CollectGroup(root, list, false);
            return list;
        }

        /// <summary>
        /// 对群组内所有成员执行操作，<paramref name="action"/> 不能为 null
        /// </summary>
        public static void ForEachGroupMember(NPC root, Action<NPC> action) {
            if (action == null || root == null || !root.active) {
                return;
            }
            int anchor = ResolveAnchor(root);
            List<int> segList = FindSegmentList(root.type);

            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC n = Main.npc[i];
                if (!n.active) {
                    continue;
                }
                if (IsMember(n, anchor, segList)) {
                    action(n);
                }
            }
        }

        //单成员判定，已假设 n.active
        private static bool IsMember(NPC n, int anchor, List<int> segList) {
            if (anchor >= 0 && ResolveAnchor(n) == anchor) {
                return true;
            }
            if (segList != null && segList.Contains(n.type)) {
                return true;
            }
            return false;
        }

        //不依赖外部活跃判定的锚点解析，调用方需保证 n != null
        private static int ResolveAnchor(NPC n) {
            int rl = n.realLife;
            if (rl >= 0 && rl < Main.maxNPCs && Main.npc[rl].active) {
                return rl;
            }
            return n.whoAmI;
        }

        //在预定义 Boss 体节表中查找包含 type 的列表，找不到返回 null
        private static List<int> FindSegmentList(int type) {
            var all = CWRLoad.AllBossSegmentLists;
            if (all == null) {
                return null;
            }
            for (int i = 0; i < all.Count; i++) {
                var list = all[i];
                if (list != null && list.Contains(type)) {
                    return list;
                }
            }
            return null;
        }

        //两个类型是否同属一个体节列表
        private static bool ShareSegmentList(int typeA, int typeB) {
            var all = CWRLoad.AllBossSegmentLists;
            if (all == null) {
                return false;
            }
            for (int i = 0; i < all.Count; i++) {
                var list = all[i];
                if (list != null && list.Contains(typeA) && list.Contains(typeB)) {
                    return true;
                }
            }
            return false;
        }
    }
}
