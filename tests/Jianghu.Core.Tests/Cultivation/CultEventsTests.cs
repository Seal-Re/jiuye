using System.Collections.Generic;
using System.Linq;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Task 3.1：DomainEvent 加 PathEntered/RealmBreakthrough（纯加 record）+ Chronicle 渲染中文行；
    /// World.TryAssignCultivation 挂 Cultivation 后产 PathEntered 入史（事件单源 §11）。
    /// </summary>
    public class CultEventsTests
    {
        [Fact]
        public void PathEntered_Constructs_AndRendersName()
        {
            var ch = new Chronicle();
            ch.Append(new PathEntered(7, new CharacterId(1), "sword_immortal"), n => "甲");
            Assert.Single(ch.Lines);
            Assert.Contains("甲", ch.Lines[0]);
            Assert.Contains("sword_immortal", ch.Lines[0]);
        }

        [Fact]
        public void RealmBreakthrough_Constructs_AndRendersName()
        {
            var e = new RealmBreakthrough(11, new CharacterId(2), 3);
            Assert.Equal(11, e.Tick);
            Assert.Equal(3, e.NewRealmIndex);

            var ch = new Chronicle();
            ch.Append(e, n => "乙");
            Assert.Single(ch.Lines);
            Assert.Contains("乙", ch.Lines[0]);
        }

        [Fact]
        public void RealmBreakthrough_WithRealmDesc_RendersDaXiaoJingjieUT()
        {
            // A1.5（auditor T1）：realmDesc 非 null → 「跻身 X 之境」大小境界·UT 渲染；直接文本断言。
            var ch = new Chronicle();
            ch.Append(new RealmBreakthrough(11, new CharacterId(2), 4), n => "乙", fi => "金丹（UT4）");
            Assert.Contains("跻身 金丹（UT4） 之境", ch.Lines[0]);

            // realmDesc==null → 回退裸整数「第 N 重」（off 路径无 RealmQuery 依赖，逐字节守）。
            var bare = new Chronicle();
            bare.Append(new RealmBreakthrough(11, new CharacterId(2), 4), n => "乙", null);
            Assert.Contains("第 4 重", bare.Lines[0]);
        }

        [Fact]
        public void On_Generation_AppendsPathEnteredToChronicle()
        {
            var w = WorldFactory.CreateInitial(777, LimitsConfig.Default, 5,
                cultivation: true, pathSource: RootCoveringSource());
            // 至少有一名定路者 → 至少一行 PathEntered（含其 PathId）。
            var paths = w.AliveCharacters().Where(c => c.Cultivation != null)
                         .Select(c => c.Cultivation!.PathId).Distinct().ToList();
            Assert.NotEmpty(paths);
            foreach (var pid in paths)
                Assert.Contains(w.Chronicle.Lines, l => l.Contains(pid));
        }

        // 4 路覆盖 on-Spawn 灵根 tag 池（sword/body/spirit/yin），每路一闸。
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

        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }
    }
}
