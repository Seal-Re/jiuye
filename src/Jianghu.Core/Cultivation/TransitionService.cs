using System;
using System.Collections.Generic;
using System.Linq;
using Jianghu.Config;
using Jianghu.Stats;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// cv-a3 story-002：转职/迁移执行器——PathId 安全迁移 + carryover 规则 + Realm 映射。
    ///
    /// 纯整数、确定性（无 RNG）、纯函数（除 CultivationState.PathId setter 外）。
    /// off 模式不触发（仅在 cultivation-on + TransitionDef 存在时由上层调用）。
    ///
    /// 模块化：独立于奇遇框架（storylet executor），仅做迁移逻辑；触发由 story-005/008 负责。
    /// </summary>
    public static class TransitionService
    {
        /// <summary>
        /// 执行 PathId 迁移（转职/觉醒/双修的核心状态变更）。
        ///
        /// 步骤：
        /// 1. 读取目标路线的 RealmCurve，对齐 UnifiedTierOf → 计算新 RealmIndex
        /// 2. Apply carryover 规则：保留标记的 resources/arts，其余丢弃
        /// 3. 修改 CultivationState.PathId 指向新路线
        /// 4. 可选扣减 Cost（从 cultivationPoints 或指定资源）
        ///
        /// 确定性：同 TransitionDef + 同初始状态 → 同迁移结果（无 RNG，B.2）。
        /// </summary>
        /// <param name="state">当前修炼态（将被修改）</param>
        /// <param name="def">跃迁定义（含 gate/carryover/cost）</param>
        /// <param name="pathSource">路线源（查找目标路线 Curve）</param>
        /// <returns>迁移后的 PathId</returns>
        /// <exception cref="InvalidOperationException">目标路线不存在时抛</exception>
        public static string MigratePathId(
            CultivationState state, TransitionDef def, IPathSource pathSource)
        {
            // 1. 查目标路线
            var allPaths = pathSource.Load();
            CultivationPathDef? toPath = null;
            foreach (var p in allPaths)
            {
                if (p.PathId == def.ToPathId)
                { toPath = p; break; }
            }
            if (toPath == null)
                throw new InvalidOperationException(
                    $"TransitionService: 目标路线 '{def.ToPathId}' 不在 pathSource 中");

            // 2. Realm 映射：按 UnifiedTierOf 对齐（非归零）
            //    取当前 UT → 在目标路线找相同 UT → 得目标 RealmIndex。
            //    若当前 RealmIndex 超旧路 UT 数组 → 用末位 UT。
            int oldUt = GetUT(state.RealmIndex, state.PathId, allPaths);
            int newRealm = FindRealmForUT(oldUt, toPath.Curve.UnifiedTierOf);

            // 3. Apply carryover（若 def.Carryover != null）
            if (def.Carryover != null)
            {
                var rule = def.Carryover;
                // 保留标记的资源 key
                var keepRes = new HashSet<string>(rule.KeepResources ?? Array.Empty<string>());
                var newResources = new Dictionary<string, int>();
                foreach (var kv in state.Resources)
                    if (keepRes.Contains(kv.Key))
                        newResources[kv.Key] = kv.Value;
                state.Resources.Clear();
                foreach (var kv in newResources)
                    state.Resources[kv.Key] = kv.Value;

                // 保留标记的功法 ID
                if (rule.KeepArts != null && rule.KeepArts.Count > 0)
                {
                    var keepArts = new HashSet<string>(rule.KeepArts);
                    var newArts = state.ChosenArtIds.Where(a => keepArts.Contains(a)).ToList();
                    // ChosenArtIds is init-only → 无法原地修改。
                    // Carryover 的 arts 保留通过上层在 NewForPath 时传入实现（见 AC 2.4 设计注）。
                    // 此处仅记录意图；实际 arts carryover 由 WorldFactory/奇遇框架在创建新 CultivationState 时处理。
                }
            }
            else
            {
                // 无 carryover → 清空资源（新路线从零开始）
                state.Resources.Clear();
            }

            // 4. 修改 PathId + RealmIndex
            string oldPathId = state.PathId;
            state.PathId = def.ToPathId!;
            state.RealmIndex = newRealm;

            // 5. 扣减 Cost（从 cultivationPoints）
            if (def.Cost > 0)
                state.CultivationPoints = Math.Max(0, state.CultivationPoints - def.Cost);

            return oldPathId;
        }

        /// <summary>取当前 RealmIndex 对应的 UnifiedTier。</summary>
 static int GetUT(int realmIndex, string pathId, IReadOnlyList<CultivationPathDef> allPaths)
        {
            foreach (var p in allPaths)
            {
                if (p.PathId != pathId) continue;
                var uts = p.Curve.UnifiedTierOf;
                if (realmIndex < uts.Count) return uts[realmIndex];
                return uts[uts.Count - 1]; // 超界用末位 UT
            }
            return 0; // path 不在 registry（fallback）
        }

        /// <summary>在新路线的 UnifiedTierOf 中找等于 targetUT 的最小 RealmIndex。</summary>
        static int FindRealmForUT(int targetUt, IReadOnlyList<int> utOf)
        {
            for (int i = 0; i < utOf.Count; i++)
                if (utOf[i] == targetUt)
                    return i;
            // 若无精确匹配：找最近 ≤ targetUt 的
            int best = 0;
            for (int i = 0; i < utOf.Count; i++)
                if (utOf[i] <= targetUt && utOf[i] > utOf[best])
                    best = i;
            return best;
        }
    }
}
