using System.Collections.Generic;
using Jianghu.Stats;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// derived:&lt;key&gt; 派生值提供者（spec §6 战力项 src）。纯整数。
    /// 实现者按 (state, stats) 算一个整数派生量（如组合多资源/标志的衍生指标）。
    /// </summary>
    public interface IDerivedProvider
    {
        int Compute(CultivationState st, StatBlock stats);
    }

    /// <summary>
    /// derived:&lt;key&gt; 解析注册表（spec §6）。A.0 空注册——未注册 key 返回 0（占位）。
    /// 后续路线可经 <see cref="Register"/> 装配 provider。纯整数确定性。
    /// </summary>
    public static class DerivedRegistry
    {
        private static readonly Dictionary<string, IDerivedProvider> _providers = new();

        /// <summary>注册一个派生提供者（覆盖同 key）。</summary>
        public static void Register(string key, IDerivedProvider provider) => _providers[key] = provider;

        /// <summary>解析 derived key：已注册→provider 算，未注册→0（A.0 占位）。</summary>
        public static int Resolve(string key, CultivationState st, StatBlock stats)
            => _providers.TryGetValue(key, out var p) ? p.Compute(st, stats) : 0;
    }
}
