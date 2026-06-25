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
    /// derived:&lt;key&gt; 解析注册表（spec §6）。静态构造 auto-register 4 provider。
    /// 未注册 key 返回 0。纯整数确定性。
    /// </summary>
    public static class DerivedRegistry
    {
        private static readonly Dictionary<string, IDerivedProvider> _providers = new();

        static DerivedRegistry()
        {
            // A.0 真派生: 4 derived provider 自动注册
            DerivedProviders.RegisterAll();
        }

        /// <summary>注册一个派生提供者（覆盖同 key）。</summary>
        public static void Register(string key, IDerivedProvider provider) => _providers[key] = provider;

        /// <summary>
        /// 某 derived key 是否已注册（纯查询，无副作用）。
        /// 供 INV-DERIVED 注册门校验：path 公式引用的每个 <c>derived:&lt;key&gt;</c> 必须已注册，
        /// 否则 <see cref="Resolve"/> 会静默回 0（恒 0 伤害且无报错）——见 CR-2026-06-25 R-2。
        /// </summary>
        public static bool IsRegistered(string key) => _providers.ContainsKey(key);

        /// <summary>解析 derived key：已注册→provider 算，未注册→0（A.0 占位）。</summary>
        public static int Resolve(string key, CultivationState st, StatBlock stats)
            => _providers.TryGetValue(key, out var p) ? p.Compute(st, stats) : 0;
    }
}
