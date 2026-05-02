using System;
using System.Collections.Generic;
using Terraria;

namespace CalamityOverhaul.Common
{
    /// <summary>
    /// NPC 群组解析工具
    /// <br/>用于把"多实体共享生命/同一 Boss 体"的一组 NPC 视为整体，比如蠕虫类 Boss 的头部体节尾巴
    /// 月总核心和手部头部、双子魔眼这类共享 <see cref="NPC.realLife"/> 链接的实体集合
    /// <br/>该工具与具体玩法系统无关，可在骇客协议、技能AOE、状态附加等任意场景下直接使用
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
        /// 判断给定 NPC 是否属于同一群组（共享同一锚点）
        /// </summary>
        public static bool IsSameGroup(NPC a, NPC b) {
            if (a == null || b == null) {
                return false;
            }
            int aa = GetAnchorIndex(a);
            return aa >= 0 && aa == GetAnchorIndex(b);
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
            int anchor = GetAnchorIndex(root);
            if (anchor < 0) {
                return;
            }
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC n = Main.npc[i];
                if (!n.active) {
                    continue;
                }
                if (ResolveAnchor(n) == anchor) {
                    output.Add(n);
                }
            }
        }

        /// <summary>
        /// 收集群组成员的索引集合，等价于 <see cref="CollectGroup(NPC, List{NPC}, bool)"/> 但只返回 whoAmI
        /// </summary>
        public static void CollectGroupIndices(NPC root, List<int> output, bool clear = true) {
            if (output == null) {
                return;
            }
            if (clear) {
                output.Clear();
            }
            int anchor = GetAnchorIndex(root);
            if (anchor < 0) {
                return;
            }
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC n = Main.npc[i];
                if (!n.active) {
                    continue;
                }
                if (ResolveAnchor(n) == anchor) {
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
            if (action == null) {
                return;
            }
            int anchor = GetAnchorIndex(root);
            if (anchor < 0) {
                return;
            }
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC n = Main.npc[i];
                if (!n.active) {
                    continue;
                }
                if (ResolveAnchor(n) == anchor) {
                    action(n);
                }
            }
        }

        //不依赖外部 GetAnchorIndex 的活跃判定，扫描时已确保 n.active 为 true
        private static int ResolveAnchor(NPC n) {
            int rl = n.realLife;
            if (rl >= 0 && rl < Main.maxNPCs && Main.npc[rl].active) {
                return rl;
            }
            return n.whoAmI;
        }
    }
}
