using System.Collections.Generic;
using System.Linq;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Task 3.3：on 角色行动后累加本路修为 → RealmCurve.NextIndexIfReady 判突破 →
    /// RealmIndex++ + 产 RealmBreakthrough 入史（A.0 确定性，达阈即升不掷随机）。
    /// 封顶不越界；off（Cultivation==null）无此路径。
    /// </summary>
    public class BreakthroughTests
    {
        // 低阈速突破 mock 路：thresholds=[0,3,6]（realm1 需 3 修为，realm2 需 6），EntryGate 全灵根。
        static IPathSource FastBreakSource()
        {
            var p = TestPaths.ValidFull() with
            {
                PathId = "fa_xiu",
                Curve = new RealmCurveDef(
                    new[] { 10, 12, 14 },
                    new[] { 0, 1, 2 },
                    new[] { "凝气", "小成", "大成" },
                    new[] { 0, 3, 6 }),
                EntryGate = new EntryGateDef("tag:spirit_root"),
            };
            return new ListPathSource(new[] { p });
        }

        [Fact]
        public void On_AccumulatesAndBreaksThrough_EmitsEvent()
        {
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5,
                cultivation: true, pathSource: FastBreakSource());
            // 短窗口：初始修士行动数次累加修为越阈，仍在世（未老死，涌现者无路不混入）。
            for (int i = 0; i < 15; i++) w.Advance(6);

            // 至少一名在世修士越过 realm0（达阈即升）。
            Assert.Contains(w.AliveCharacters(), c => c.Cultivation != null && c.Cultivation.RealmIndex > 0);
            // 史册含 RealmBreakthrough 行（中文「冲破瓶颈」）。
            Assert.Contains(w.Chronicle.Lines, l => l.Contains("冲破瓶颈"));
        }

        [Fact]
        public void On_AccumulatesCultivationPoints()
        {
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5,
                cultivation: true, pathSource: FastBreakSource());
            for (int i = 0; i < 15; i++) w.Advance(6);
            // 行动累加的修为落在 CultivationState.CultivationPoints（显式计数器），非 Flags["@xiuwei"]。
            Assert.Contains(w.AliveCharacters(), c => c.Cultivation != null && c.Cultivation.CultivationPoints > 0);
            // Flags 不再被借用存修为计数（@xiuwei 已废）。
            foreach (var c in w.AliveCharacters())
                if (c.Cultivation != null)
                    Assert.False(c.Cultivation.Flags.ContainsKey("@xiuwei"));
        }

        [Fact]
        public void RealmIndex_NeverExceedsMax()
        {
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5,
                cultivation: true, pathSource: FastBreakSource());
            // 长跑：修为远超末阈（[0,3,6]）→ RealmIndex 仍封顶 flatIndex=2，不越界。
            for (int i = 0; i < 60; i++) w.Advance(6);
            foreach (var c in w.AliveCharacters())
                if (c.Cultivation != null)
                    Assert.True(c.Cultivation.RealmIndex <= 2); // 曲线 3 境 → flatIndex 封顶 2
            // 突破事件的 NewRealmIndex 不越界（≤2）。
            var bt = w.Chronicle.Lines.Where(l => l.Contains("冲破瓶颈")).ToList();
            Assert.NotEmpty(bt);
            Assert.DoesNotContain(bt, l => l.Contains("第 3 重") || l.Contains("第 4 重"));
        }

        [Fact]
        public void Off_NoBreakthrough()
        {
            var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5); // 默认 off
            for (int i = 0; i < 200; i++) w.Advance(6);
            Assert.DoesNotContain(w.Chronicle.Lines, l => l.Contains("冲破瓶颈"));
            foreach (var c in w.AliveCharacters())
                Assert.Null(c.Cultivation);
        }

        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }
    }
}
