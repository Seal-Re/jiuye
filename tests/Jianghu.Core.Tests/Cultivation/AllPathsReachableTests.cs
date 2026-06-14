using System.Collections.Generic;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Random;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Task 6.0：全 21 路可定（不舍弃任何路径的运行期硬证）。
    /// 内置 CodePathSource 注册 21 路 → 跨足够多角色/种子生成（cultivation-on）→
    /// 收集被定到的 PathId 集合 → 断言 21 路全部出现（无路不可达）。
    /// 若某路不可达 → entry tag 不在池 / 重名 / EntryGate.Pred 格式错，须揪出修复。
    /// </summary>
    public class AllPathsReachableTests
    {
        // 内置 21 路的 canon pathId 全集（= CodePathSource 注册集，PathValidator.KnownPathIds 同源）。
        static readonly HashSet<string> Expected21 = new HashSet<string>(PathValidator.KnownPathIds);

        [Fact]
        public void RootTagPool_Has21UniqueEntryTags_EachRoutesToOnePath()
        {
            // 每路一个唯一 entry tag → 池含 21 个 tag；每 tag 经 EntryGate 唯一路由到其路。
            var reg = new PathRegistry(new CodePathSource());
            var pool = reg.RootTagPool();
            Assert.Equal(21, reg.All.Count);
            Assert.Equal(21, pool.Count); // 21 个唯一 entry tag，无重名（SortedSet 去重后仍 21）

            // 每 tag 恰路由到一条路（无 tag 命中多路 / 无 tag 命中零路）。
            foreach (var tag in pool)
            {
                int hit = 0;
                foreach (var p in reg.All)
                    if (p.EntryGate.CanEnter(new[] { tag })) hit++;
                Assert.True(hit == 1, $"灵根 tag '{tag}' 命中 {hit} 条路（应恰 1 条）");
            }
        }

        [Fact]
        public void EveryRootTag_AssignsItsPath_NoUnreachable()
        {
            // 直接经 PathAssigner（生成期真路径）逐 tag 定路 → 收集 PathId → 必覆盖 21 路。
            var reg = new PathRegistry(new CodePathSource());
            var assigned = new HashSet<string>();
            foreach (var tag in reg.RootTagPool())
            {
                var rng = new Pcg32(99, 1).Split(RngStreamIds.Cultivation);
                var r = PathAssigner.Assign(new[] { tag }, reg, rng);
                Assert.NotNull(r.PathId); // 池里的 tag 必能定到路（非散修）
                assigned.Add(r.PathId!);
            }

            var missing = new SortedSet<string>(Expected21);
            missing.ExceptWith(assigned);
            Assert.True(missing.Count == 0, "不可达路: " + string.Join(", ", missing));
            Assert.Equal(21, assigned.Count); // 21 路全现
        }

        [Fact]
        public void CrossSeedGeneration_CoversAll21Paths()
        {
            // 运行期硬证：cultivation-on 跨足够多种子/角色生成 → 收集被定到的 PathId → 21 路全现。
            // World.TryAssignCultivation 每角色随机抽一灵根 tag → 定路。够多采样即遍历全池。
            var assigned = new HashSet<string>();
            for (ulong seed = 1; seed <= 200 && assigned.Count < 21; seed++)
            {
                var w = WorldFactory.CreateInitial(seed, LimitsConfig.Default, 8, cultivation: true);
                foreach (var c in w.AliveCharacters())
                    if (c.Cultivation != null)
                        assigned.Add(c.Cultivation.PathId);
            }

            var missing = new SortedSet<string>(Expected21);
            missing.ExceptWith(assigned);
            Assert.True(missing.Count == 0, "运行期不可达路: " + string.Join(", ", missing));
            Assert.Equal(21, assigned.Count);
        }
    }
}
