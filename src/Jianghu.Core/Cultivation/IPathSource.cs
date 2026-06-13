using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 修炼路线数据源（spec §2：C# static readonly 常量库；JSON 适配预留 CLI 层）。
    /// </summary>
    public interface IPathSource
    {
        IReadOnlyList<CultivationPathDef> Load();
    }

    /// <summary>
    /// 内置代码路线源（spec §3 paths/*.cs）。A.0-B3 先空表；Phase 4/5 逐路 <see cref="Add"/> 填 21 路。
    /// </summary>
    public sealed class CodePathSource : IPathSource
    {
        private readonly List<CultivationPathDef> _paths = new List<CultivationPathDef>();

        // Phase 4/5 逐路追加（本 task 不填路）。
        public void Add(CultivationPathDef def) => _paths.Add(def);

        public IReadOnlyList<CultivationPathDef> Load() => _paths;
    }
}
