using CalamityOverhaul.Content.ADV.ADVChoices;
using CalamityOverhaul.Content.ADV.DialogueBoxs;
using System;
using System.Collections.Generic;

namespace CalamityOverhaul.Content.ADV.Scenarios
{
    public static class ScenarioManager
    {
        private static readonly Dictionary<string, IADVScenario> scenarios = new();
        //待启动队列，当前有场景正在运行时入队，等其结束后自动按序启动
        private static readonly Queue<string> pending = new();
        private static IADVScenario active;

        public static void Register(IADVScenario scenario, bool overwrite = false) {
            if (scenario == null) return;
            if (!scenarios.TryAdd(scenario.Key, scenario)) {
                if (!overwrite) return;
                scenarios[scenario.Key] = scenario;
            }
        }

        /// <summary>
        /// 启动指定场景
        /// 若当前已有场景在运行（对话UI或选项框仍处于活动状态），
        /// 则将本次请求加入待启动队列，由<see cref="UpdatePending"/>在当前场景结束后自动出队启动，
        /// 而不是静默丢弃
        /// </summary>
        /// <param name="key">场景Key</param>
        /// <returns>是否成功启动或入队</returns>
        public static bool Start(string key) {
            if (!scenarios.TryGetValue(key, out var sc)) {
                sc = TryCreate(key);
                if (sc == null) return false;
                Register(sc);
            }
            if (IsActive()) {
                //已有场景在运行，入队等待
                if (!pending.Contains(key)) {//避免重复入队
                    pending.Enqueue(key);
                }
                return true;
            }
            active = sc;
            sc.Start();
            return true;
        }

        /// <summary>
        /// 每帧tick，检查待启动队列。当无场景活动时按FIFO顺序启动队首场景。
        /// 由<see cref="DialogueSystem.UpdateUI"/>驱动
        /// </summary>
        internal static void UpdatePending() {
            if (pending.Count == 0) return;
            if (IsActive()) return;
            //逐个出队尝试，直到有一个成功启动或队列清空
            while (pending.Count > 0) {
                var key = pending.Dequeue();
                if (!scenarios.TryGetValue(key, out var sc)) continue;
                active = sc;
                sc.Start();
                //启动一个就停下，等下次tick再处理后续（场景可能立即Complete，那也无妨，下帧继续）
                break;
            }
        }

        /// <summary>
        /// 清空所有待启动场景请求
        /// </summary>
        public static void ClearPending() => pending.Clear();

        /// <summary>
        /// 重置指定场景
        /// </summary>
        /// <param name="key"></param>
        public static void Reset(string key) { if (scenarios.TryGetValue(key, out var sc)) sc.Reset(); }

        private static IADVScenario TryCreate(string key) {
            var asm = typeof(ScenarioManager).Assembly;
            foreach (var t in asm.GetTypes()) {
                if (!t.IsAbstract && typeof(IADVScenario).IsAssignableFrom(t)) {
                    if (string.Equals(t.Name, key, StringComparison.Ordinal)) {
                        try { return (IADVScenario)Activator.CreateInstance(t); } catch { }
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// 启动指定类型的场景
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool Start<T>() where T : IADVScenario, new() {
            var temp = new T();
            if (!scenarios.ContainsKey(temp.Key))
                Register(temp);
            return Start(temp.Key);
        }
        /// <summary>
        /// 重置指定类型的场景
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Reset<T>() where T : IADVScenario, new() {
            var temp = new T();
            if (scenarios.TryGetValue(temp.Key, out var sc))
                sc.Reset();
        }
        /// <summary>
        /// 当前是否有场景处于激活状态
        /// </summary>
        /// <returns></returns>
        public static bool IsActive() => active != null && ((DialogueUIRegistry.Current?.Active ?? false) || (ADVChoiceBox.Instance?.Active ?? false));
        /// <summary>
        /// 指定场景是否处于激活状态
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsActive(string key) => IsActive() && active.Key == key;
        /// <summary>
        /// 重置所有已注册的场景
        /// </summary>
        public static void ResetAll() {
            foreach (var sc in scenarios.Values)
                sc.Reset();
            active = null;
            pending.Clear();
        }

        /// <summary>
        /// 世界切换时清理运行状态，防止跨世界场景阻塞。
        /// 仅清理运行时状态（active引用与待启动队列），不清除已注册的场景定义
        /// </summary>
        internal static void OnWorldCleanup() {
            active = null;
            pending.Clear();
        }
    }
}
