using System;
using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Xunit;
using Xunit.Abstractions;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// B5 设计内容分类 dump harness：21 路 × 功法类型(ArtCategory.Role) × 品级(Tier) × 模块稀有度(Rarity)。
    /// 派生视图(红线 A.2：真相在 production/，本表自 Def 反射派生)。诊断性 ITestOutputHelper 输出 + 松断言。
    /// run: dotnet test --filter Name~Dump_DesignClassification --logger "console;verbosity=detailed"
    /// </summary>
    public class DesignClassificationDumpTests
    {
        private readonly ITestOutputHelper _out;
        public DesignClassificationDumpTests(ITestOutputHelper o) => _out = o;

        [Fact]
        public void Dump_DesignClassification_AllPaths()
        {
            // Arrange：加载全 21 路 Def。
            var paths = new CodePathSource().Load();

            // —— 表1：每路功法类目(Role) × 品级(Tier) 分布 ——
            _out.WriteLine("# 表1 功法类目×品级（每路：类目名[Role] → 各 Tier 功法数）");
            foreach (var def in paths)
            {
                _out.WriteLine($"\n## {def.PathId} ({def.Name})");
                foreach (var cat in def.ArtCategories)
                {
                    var byTier = cat.Arts.GroupBy(a => a.Tier).OrderBy(g => g.Key)
                        .Select(g => $"T{g.Key}×{g.Count()}");
                    _out.WriteLine($"  - {cat.Name}[{cat.Role}] ({cat.Arts.Count}部, Pick{cat.PickMin}-{cat.PickMax}): {string.Join(" ", byTier)}");
                }
            }

            // —— 表2：战技品级分布 + OnUse 模块稀有度/Kind 统计 ——
            _out.WriteLine("\n\n# 表2 战技：品级 + 模块化程度（每路）");
            foreach (var def in paths)
            {
                var skillTiers = def.CombatSkills.GroupBy(s => s.Tier).OrderBy(g => g.Key)
                    .Select(g => $"T{g.Key}×{g.Count()}");
                var allOps = def.CombatSkills.SelectMany(s => s.OnUse).ToList();
                var byRarity = allOps.GroupBy(o => o.Rarity).OrderBy(g => g.Key)
                    .Select(g => $"{g.Key}×{g.Count()}");
                int structured = allOps.Count(o => o.Kind != EffectOpKind.AddPenInteger && o.Kind != EffectOpKind.AddFlatDR
                    || o.Rarity != EffectRarity.Common); // 结构化 = 非占位/非Common
                int placeholder = allOps.Count(o => o.Kind == EffectOpKind.AddPenInteger && o.Rarity == EffectRarity.Common);
                _out.WriteLine($"  {def.PathId}: 战技{def.CombatSkills.Count}({string.Join(" ", skillTiers)}) | OnUse算子{allOps.Count} 稀有度[{string.Join(" ", byRarity)}] | 结构化≈{structured} 占位{placeholder}");
            }

            // —— 表3：模块 Kind 全局分布（已结构化的算子类型谱）——
            _out.WriteLine("\n\n# 表3 全局模块 Kind 分布（21 路战技 OnUse 合计）");
            var globalKinds = paths.SelectMany(p => p.CombatSkills).SelectMany(s => s.OnUse)
                .GroupBy(o => o.Kind).OrderByDescending(g => g.Count());
            foreach (var g in globalKinds)
                _out.WriteLine($"  {g.Key}: {g.Count()}");

            // —— 表4：汇总（每路一行：类目数/功法总数/战技数/已结构化模块数/占位数）——
            _out.WriteLine("\n\n# 表4 汇总表（路 | 类目 | 功法 | 战技 | 结构化模块 | 占位）");
            int totCat = 0, totArt = 0, totSkill = 0, totStruct = 0, totPlace = 0;
            foreach (var def in paths)
            {
                int cats = def.ArtCategories.Count;
                int arts = def.ArtCategories.Sum(c => c.Arts.Count);
                int skills = def.CombatSkills.Count;
                var ops = def.CombatSkills.SelectMany(s => s.OnUse).ToList();
                int structured = ops.Count(o => !(o.Kind == EffectOpKind.AddPenInteger && o.Rarity == EffectRarity.Common));
                int place = ops.Count(o => o.Kind == EffectOpKind.AddPenInteger && o.Rarity == EffectRarity.Common);
                totCat += cats; totArt += arts; totSkill += skills; totStruct += structured; totPlace += place;
                _out.WriteLine($"  {def.PathId,-22} | {cats} | {arts} | {skills} | {structured} | {place}");
            }
            _out.WriteLine($"  {"合计(21路)",-22} | {totCat} | {totArt} | {totSkill} | {totStruct} | {totPlace}");

            // Assert：松断言——21 路、每路类目≥3、有战技。
            Assert.Equal(21, paths.Count);
            Assert.All(paths, p => Assert.True(p.ArtCategories.Count >= 3));
            Assert.All(paths, p => Assert.True(p.CombatSkills.Count >= 5));
        }
    }
}
