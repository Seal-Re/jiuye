using Jianghu.Drama;
using Jianghu.Events;
using Jianghu.Model;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// 6 戏剧 DomainEvent + Chronicle 武侠味投影（drama-008，spec Step 6）。
    /// 戏剧效果经统一 DomainEvent→Chronicle.Append 管线（不旁路 mutate）。纯整数/枚举/Id 字段。
    /// </summary>
    public class DramaEventChronicleTests
    {
        private static readonly CharacterId A = new(1);
        private static readonly CharacterId B = new(2);
        private static string Name(CharacterId c) => c.Value == 1 ? "萧逸" : "魔尊";

        private static string Render(DomainEvent e)
        {
            var ch = new Chronicle();
            ch.Append(e, Name);
            Assert.Equal(1, ch.Count);
            return ch.Lines[0];
        }

        // —— D8.5 各事件渲染特征串 ——
        [Fact]
        public void test_grudge_formed_renders_enmity()
        {
            var line = Render(new GrudgeFormed(10, new GrudgeId(1), A, B, GrudgeKind.Slaughter, 90));
            Assert.Contains("萧逸", line);
            Assert.Contains("魔尊", line);
            Assert.Contains("[10]", line);
            Assert.Contains("血仇", line); // Slaughter → 灭门血仇
        }

        [Fact]
        public void test_grudge_kind_wording_differs()
        {
            var insult = Render(new GrudgeFormed(1, new GrudgeId(1), A, B, GrudgeKind.Insult, 20));
            var maim = Render(new GrudgeFormed(1, new GrudgeId(1), A, B, GrudgeKind.Maiming, 50));
            var slay = Render(new GrudgeFormed(1, new GrudgeId(1), A, B, GrudgeKind.Slaughter, 90));
            Assert.Contains("羞辱", insult);
            Assert.Contains("残身", maim);
            Assert.Contains("血仇", slay);
        }

        [Fact]
        public void test_grudge_inherited_renders_generational()
        {
            var line = Render(new GrudgeInherited(50, new GrudgeId(2), A, B, new GrudgeId(1), 1, 54));
            Assert.Contains("萧逸", line);
            // 父债子偿 / 继承 / 世代——继承特征措辞。
            Assert.Contains("继承", line);
        }

        [Fact]
        public void test_arc_ignited_renders_revenge_start()
        {
            var line = Render(new ArcIgnited(20, new ArcId(7), A, B));
            Assert.Contains("萧逸", line);
            Assert.Contains("魔尊", line);
            Assert.Contains("复仇", line);
        }

        [Fact]
        public void test_arc_stage_entered_renders_stage()
        {
            var line = Render(new ArcStageEntered(30, new ArcId(7), ArcStage.Hunting));
            Assert.Contains("[30]", line);
        }

        [Fact]
        public void test_revenge_consummated_wording_differs_by_outcome()
        {
            var won = Render(new RevengeConsummated(40, new ArcId(7), A, B, true));
            var lost = Render(new RevengeConsummated(40, new ArcId(7), A, B, false));
            Assert.Contains("萧逸", won);
            Assert.NotEqual(won, lost); // 胜负措辞不同
            Assert.Contains("仇", won);
        }

        [Fact]
        public void test_arc_abandoned_renders_reason()
        {
            var line = Render(new ArcAbandoned(60, new ArcId(7), "TargetDied"));
            Assert.Contains("[60]", line);
        }

        // —— D8.3 事件值相等性 ——
        [Fact]
        public void test_event_value_equality()
        {
            Assert.Equal(
                new ArcIgnited(20, new ArcId(7), A, B),
                new ArcIgnited(20, new ArcId(7), A, B));
            Assert.NotEqual(
                new ArcIgnited(20, new ArcId(7), A, B),
                new ArcIgnited(21, new ArcId(7), A, B));
            Assert.Equal(
                new RevengeConsummated(40, new ArcId(7), A, B, true),
                new RevengeConsummated(40, new ArcId(7), A, B, true));
            Assert.NotEqual(
                new RevengeConsummated(40, new ArcId(7), A, B, true),
                new RevengeConsummated(40, new ArcId(7), A, B, false));
        }

        // —— 每事件均渲染（非 default 未知事件分支）——
        [Fact]
        public void test_all_six_events_have_dedicated_rendering()
        {
            DomainEvent[] evs =
            {
                new GrudgeFormed(1, new GrudgeId(1), A, B, GrudgeKind.Maiming, 50),
                new GrudgeInherited(1, new GrudgeId(2), A, B, new GrudgeId(1), 1, 30),
                new ArcIgnited(1, new ArcId(1), A, B),
                new ArcStageEntered(1, new ArcId(1), ArcStage.BuildUp),
                new RevengeConsummated(1, new ArcId(1), A, B, true),
                new ArcAbandoned(1, new ArcId(1), "AvengerDied"),
            };
            foreach (var e in evs)
                Assert.DoesNotContain("未知事件", Render(e)); // 均命中专属 case，非 default
        }
    }
}
