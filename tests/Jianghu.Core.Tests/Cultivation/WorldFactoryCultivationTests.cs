using System.Collections.Generic;
using System.Linq;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Task 2.4：WorldFactory cultivation 开关 + World._cultRng + Spawn 定路接线。
    /// on：角色获 CultivationState。off：cultRng 不构造、不消费 Split(5)（OffByteIdenticalTests 守 off 逐字节）。
    /// </summary>
    public class WorldFactoryCultivationTests
    {
        // 测试路源：4 路覆盖 on-Spawn 灵根 tag 池（sword/body/spirit/yin），每路一闸。
        static IPathSource RootCoveringSource()
        {
            return new ListPathSource(new[]
            {
                Path("sword_immortal", "tag:sword_root"),
                Path("ti_xiu_hengshi", "tag:body_root"),
                Path("fa_xiu", "tag:spirit_root"),
                Path("gui_xiu_yang_hun", "tag:yin_root"),
            });
        }

        static CultivationPathDef Path(string id, string entryPred) =>
            TestPaths.ValidFull() with { PathId = id, EntryGate = new EntryGateDef(entryPred) };

        [Fact]
        public void On_AssignsCultivation()
        {
            var w = WorldFactory.CreateInitial(777, LimitsConfig.Default, 5,
                cultivation: true, pathSource: RootCoveringSource());
            Assert.Contains(w.AliveCharacters(), c => c.Cultivation != null);
        }

        [Fact]
        public void On_EveryRootedCharacterGetsPath_AndRealmZero()
        {
            var w = WorldFactory.CreateInitial(777, LimitsConfig.Default, 5,
                cultivation: true, pathSource: RootCoveringSource());
            foreach (var c in w.AliveCharacters())
            {
                Assert.NotNull(c.Cultivation);          // 4 路覆盖全灵根 → 人人定路
                Assert.Equal(0, c.Cultivation!.RealmIndex);
                Assert.NotEmpty(c.Cultivation!.PathId);
            }
        }

        [Fact]
        public void On_SameSeed_SameChronicleAndPaths()
        {
            var a = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5,
                cultivation: true, pathSource: RootCoveringSource());
            var b = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5,
                cultivation: true, pathSource: RootCoveringSource());
            for (int i = 0; i < 100; i++) { a.Advance(6); b.Advance(6); }
            Assert.Equal(
                string.Join("\n", a.Chronicle.Lines),
                string.Join("\n", b.Chronicle.Lines));
            // 定路结果亦逐角色一致。
            var pa = a.AliveCharacters().Select(c => c.Cultivation?.PathId).ToList();
            var pb = b.AliveCharacters().Select(c => c.Cultivation?.PathId).ToList();
            Assert.Equal(pa, pb);
        }

        [Fact]
        public void On_CloneDeepCopiesCultivation_ContinuesIdentically()
        {
            var full = WorldFactory.CreateInitial(7, LimitsConfig.Default, 5,
                cultivation: true, pathSource: RootCoveringSource());
            for (int i = 0; i < 60; i++) full.Advance(6);
            var clone = full.Clone();                       // 含 _cultRng 深拷（R1 前瞻）
            for (int i = 0; i < 60; i++) { full.Advance(6); clone.Advance(6); }
            Assert.Equal(
                string.Join("\n", full.Chronicle.Lines),
                string.Join("\n", clone.Chronicle.Lines));
        }

        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }
    }
}
