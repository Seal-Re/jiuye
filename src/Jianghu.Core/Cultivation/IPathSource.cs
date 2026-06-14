using System.Collections.Generic;
using Jianghu.Cultivation.Paths;

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
    /// 内置代码路线源（spec §3 paths/*.cs）。构造时注册已实现的内置路（Phase 4：剑修范式路起，
    /// Phase 5 逐路 <see cref="Add"/> 续填至 21 路）。每路 def 经 PathValidator 门控（数据质量 gate）。
    /// </summary>
    public sealed class CodePathSource : IPathSource
    {
        private readonly List<CultivationPathDef> _paths = new List<CultivationPathDef>();

        public CodePathSource()
        {
            // —— Phase 4 起逐路注册（范式：剑修 sword_immortal）。Phase 4 续：体修/法修/鬼修 三代表路。
            //    Phase 5 续填其余路。——
            Add(SwordImmortalPath.Def);
            Add(BodyHenglianPath.Def);
            Add(FaXiuPath.Def);
        }

        // Phase 5 逐路追加。
        public void Add(CultivationPathDef def) => _paths.Add(def);

        public IReadOnlyList<CultivationPathDef> Load() => _paths;
    }
}
