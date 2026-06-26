using System.Linq;
using Jianghu.Config;
using Jianghu.Drama;
using Jianghu.Events;
using Jianghu.Model;
using Jianghu.Sim;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// drama-010 World 接线（⚠️ 最高危）：DramaDirector 进 Advance 主循环 + Clone 续跑。
    /// 验 World 实现 IDramaView 正确、drama-on 确定性、Clone 续跑逐字节（命门）、空库 == off。
    /// </summary>
    public class DramaWiringTests
    {
        private static string Chron(World w) => string.Join("\n", w.Chronicle.Lines);

        // —— D10.4 drama-on 空库 == off 逐字节（Split(6) 构造不扰 + 空库不产事件）——
        [Fact]
        public void test_drama_on_empty_ledger_byte_identical_to_off()
        {
            var off = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 8);
            var dramaOn = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 8, dramaOn: true);
            for (int i = 0; i < 200; i++) { off.Advance(6); dramaOn.Advance(6); }
            Assert.Equal(Chron(off), Chron(dramaOn)); // 空库 Pump no-op → 轨迹零偏移
        }

        // —— D10.5 World 实现 IDramaView 正确 ——
        [Fact]
        public void test_world_as_drama_view_power_alive_samenode()
        {
            var w = WorldFactory.CreateInitial(7, LimitsConfig.Default, 4, dramaOn: true);
            IDramaView view = w; // World 实现 IDramaView
            var chars = w.AliveCharacters();
            var c0 = chars[0];
            int expectedPower = c0.Stats.Get(StatKind.Force) * 2 + c0.Stats.Get(StatKind.Internal) + c0.Stats.Get(StatKind.Constitution);
            Assert.Equal(expectedPower, view.Power(c0.Id));
            Assert.True(view.IsAlive(c0.Id));
            Assert.False(view.IsAlive(new CharacterId(99999))); // 不存在 → false
            // SameNode：同 Node 的两角色。
            var sameNodePeers = chars.Where(c => c.Node.Value == c0.Node.Value && c.Id.Value != c0.Id.Value).ToList();
            if (sameNodePeers.Count > 0)
                Assert.True(view.SameNode(c0.Id, sameNodePeers[0].Id));
        }

        // —— D10.6 drama-on 确定性：同种子两跑逐字节 ——
        [Fact]
        public void test_drama_on_same_seed_byte_identical()
        {
            string Run()
            {
                var w = WorldFactory.CreateInitial(42, LimitsConfig.Default, 6, dramaOn: true);
                SeedGrudge(w); // 预置强恩怨（经 World.Grudges chokepoint）
                for (int i = 0; i < 250; i++) w.Advance(6);
                return Chron(w);
            }
            Assert.Equal(Run(), Run());
        }

        // —— D10.6 预置强恩怨 → 真产 drama 行（接线确实活）——
        [Fact]
        public void test_seeded_grudge_produces_drama_chronicle_lines()
        {
            var w = WorldFactory.CreateInitial(123, LimitsConfig.Default, 6, dramaOn: true);
            SeedGrudge(w);
            for (int i = 0; i < 300; i++) w.Advance(6);
            // 至少点燃一条复仇弧（ArcIgnited 渲染含「复仇」）。
            Assert.Contains(w.Chronicle.Lines, l => l.Contains("复仇") || l.Contains("仇"));
        }

        // —— ⚠️ D10.7 Clone 续跑逐字节（命门）——
        [Fact]
        public void test_clone_continuation_byte_identical()
        {
            var w = WorldFactory.CreateInitial(99, LimitsConfig.Default, 6, dramaOn: true);
            SeedGrudge(w);
            for (int i = 0; i < 120; i++) w.Advance(6); // 跑到中途（弧可能活跃）

            var clone = w.Clone();
            for (int i = 0; i < 120; i++) { w.Advance(6); clone.Advance(6); }
            // 原与克隆各再跑 → 逐字节（证 Grudges/DramaDirector/dramaRng 全进 Clone）。
            Assert.Equal(Chron(w), Chron(clone));
        }

        // —— D10.8 Emit RevengeConsummated → Chronicle 行 ——
        [Fact]
        public void test_revenge_consummated_reaches_chronicle()
        {
            var w = WorldFactory.CreateInitial(55, LimitsConfig.Default, 6, dramaOn: true);
            SeedGrudge(w);
            for (int i = 0; i < 600; i++) w.Advance(6); // 长跑促弧走完
            // 长跑足以让 ≥1 弧推进到决战或中止（出 RevengeConsummated 或 ArcAbandoned 行）。
            bool hasOutcome = w.Chronicle.Lines.Any(l => l.Contains("大仇得报") || l.Contains("饮恨") || l.Contains("半途而废"));
            Assert.True(hasOutcome, "长跑应产生复仇结局行（决战或中止）");
        }

        // 测试 helper：经 World.Grudges chokepoint 预置一对强恩怨（角色 0 恨角色 1）。
        private static void SeedGrudge(World w)
        {
            Assert.NotNull(w.Grudges);
            w.Grudges!.Form(new CharacterId(0), new CharacterId(1), GrudgeKind.Slaughter, 95,
                w.Clock, GrudgeCause.Direct, 0, null, LimitsConfig.Default.GrudgeCap);
        }
    }
}
