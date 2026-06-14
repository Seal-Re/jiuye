using System.Collections.Generic;
using System.Linq;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Phase 3.5 修正 2：cultivation-on 时 Lifecycle.MaybeSpawn 产生的运行期涌现新角色
    /// 也经 TryAssignCultivation 定路（与 WorldFactory 初始 spawn 同路径），消费 _cultRng。
    /// 命门：off → MaybeSpawn 行为逐字节不变（不碰 _cultRng/registry）。
    /// </summary>
    public class MaybeSpawnCultivationTests
    {
        // 多路 source：覆盖全部 4 个 root（RootTagPool），每 root 对应不同 PathId。
        // 涌现者无论抽到哪个 root 都定路成功（非散修），且 PathId 随 _cultRng 选的 root 而异
        //  → 运行期 _cultRng 发散会改变 PathEntered 文本（Chronicle 可见，使续跑等价可被证伪）。
        static IPathSource MultiRootSource()
        {
            var roots = new[] { "sword_root", "body_root", "spirit_root", "yin_root" };
            var paths = new List<CultivationPathDef>();
            foreach (var r in roots)
                paths.Add(TestPaths.ValidFull() with
                {
                    PathId = "path_" + r,
                    EntryGate = new EntryGateDef("tag:" + r),
                });
            return new ListPathSource(paths);
        }

        // 短寿命：初始角色数步即老死 → 人口跌破 PopulationLow → MaybeSpawn 补员（确定性逼涌现）。
        static LimitsConfig ShortLived() => LimitsConfig.Default with { LifespanMin = 30, LifespanMax = 40 };

        [Fact]
        public void On_SpawnedNewcomers_GetCultivation()
        {
            var w = WorldFactory.CreateInitial(2026, ShortLived(), 5,
                cultivation: true, pathSource: MultiRootSource());
            for (int i = 0; i < 60; i++) w.Advance(6); // 长跑：初始角色老死 → 反复触发 MaybeSpawn

            // 涌现者 Id 段 ≥ 1_000_000（Lifecycle._nextId 起点）。
            var newcomers = w.AliveCharacters().Where(c => c.Id.Value >= 1_000_000).ToList();
            Assert.NotEmpty(newcomers); // 确已触发涌现
            // 运行期涌现者也定路（与初始 spawn 同路径）。
            Assert.All(newcomers, c => Assert.NotNull(c.Cultivation));
            // 定路事件入史（PathEntered → 「拜入 … 一脉」）。
            Assert.Contains(w.Chronicle.Lines, l => l.Contains("拜入") && l.Contains("一脉"));
        }

        [Fact]
        public void On_SameSeed_ChronicleByteIdentical()
        {
            // 同种子两跑（含运行期定路的 _cultRng 消费）→ Chronicle 逐字节。
            var a = Run(4242, ShortLived());
            var b = Run(4242, ShortLived());
            Assert.Equal(a, b);
        }

        [Fact]
        public void On_CloneContinuesIdentically_RuntimeCultRng()
        {
            // R1 运行期验证：Clone 后双边各 Advance 同步数 → Chronicle + StateSnapshot 逐字节，
            // 证运行期 _cultRng 消费（涌现者定路）后 _cultRng 进 Clone 正确，续跑不发散。
            var full = WorldFactory.CreateInitial(7, ShortLived(), 5,
                cultivation: true, pathSource: MultiRootSource());
            for (int i = 0; i < 80; i++) full.Advance(6);
            var fullText = string.Join("\n", full.Chronicle.Lines);
            var fullSnap = StateSnapshot.Capture(full);

            var part = WorldFactory.CreateInitial(7, ShortLived(), 5,
                cultivation: true, pathSource: MultiRootSource());
            for (int i = 0; i < 40; i++) part.Advance(6);
            var clone = part.Clone();
            // Clone 续跑前：快照口径下与 part 一致（_cultRng 已进 Clone）。
            Assert.Equal(StateSnapshot.Capture(part), StateSnapshot.Capture(clone));
            for (int i = 0; i < 40; i++) clone.Advance(6);

            Assert.Equal(fullText, string.Join("\n", clone.Chronicle.Lines));
            Assert.Equal(fullSnap, StateSnapshot.Capture(clone));
        }

        static string Run(ulong seed, LimitsConfig limits)
        {
            var w = WorldFactory.CreateInitial(seed, limits, 5,
                cultivation: true, pathSource: MultiRootSource());
            for (int i = 0; i < 80; i++) w.Advance(6);
            return string.Join("\n", w.Chronicle.Lines);
        }

        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }
    }
}
