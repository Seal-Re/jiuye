using Jianghu.Drama;
using Jianghu.Model;
using Xunit;

namespace Jianghu.Core.Tests.Drama
{
    /// <summary>
    /// 恩怨账本 GrudgeLedger（drama-005，spec Step 2）。
    /// List 主存 + 索引 + 合并幂等 + 钳制 + 稳定排序 + Clone。纯整数确定性。
    /// </summary>
    public class GrudgeLedgerTests
    {
        const int Cap = 100;
        static CharacterId C(long v) => new CharacterId(v);

        [Fact]
        public void test_form_new_grudge_returns_id_and_queryable()
        {
            var led = new GrudgeLedger();
            var id = led.Form(C(10), C(20), GrudgeKind.Slaughter, 80, originTick: 100,
                GrudgeCause.Direct, generation: 0, inheritedFrom: null, cap: Cap);
            var g = led.Get(C(10), C(20));
            Assert.NotNull(g);
            Assert.Equal(id, g!.Id);
            Assert.Equal(80, g.Intensity);
            Assert.Equal(GrudgeKind.Slaughter, g.Kind);
            Assert.Equal(1, led.Count);
        }

        [Fact]
        public void test_form_merge_idempotent_max_kind_max_intensity_min_gen_first_tick()
        {
            var led = new GrudgeLedger();
            led.Form(C(10), C(20), GrudgeKind.Insult, 30, originTick: 100, GrudgeCause.Direct, generation: 2, inheritedFrom: null, cap: Cap);
            // 同 (holder,target) 再 Form：Kind=max(Slaughter), Intensity=max(70), Gen=min(0), OriginTick=首次(100)
            led.Form(C(10), C(20), GrudgeKind.Slaughter, 70, originTick: 500, GrudgeCause.Direct, generation: 0, inheritedFrom: null, cap: Cap);

            Assert.Equal(1, led.Count); // 合并非新增
            var g = led.Get(C(10), C(20))!;
            Assert.Equal(GrudgeKind.Slaughter, g.Kind);   // max 严重度
            Assert.Equal(70, g.Intensity);                // max 强度
            Assert.Equal(0, g.Generation);                // min 世代
            Assert.Equal(100, g.OriginTick);              // 首次 tick 保留
        }

        [Fact]
        public void test_form_clamps_intensity()
        {
            var led = new GrudgeLedger();
            led.Form(C(10), C(20), GrudgeKind.Slaughter, 999, originTick: 0, GrudgeCause.Direct, generation: 0, inheritedFrom: null, cap: Cap);
            Assert.Equal(Cap, led.Get(C(10), C(20))!.Intensity); // 钳到 cap
            led.Form(C(11), C(20), GrudgeKind.Insult, -50, originTick: 0, GrudgeCause.Direct, generation: 0, inheritedFrom: null, cap: Cap);
            Assert.Equal(0, led.Get(C(11), C(20))!.Intensity);   // 钳到 0
        }

        [Fact]
        public void test_above_intensity_threshold_and_stable_sort()
        {
            var led = new GrudgeLedger();
            led.Form(C(1), C(9), GrudgeKind.Insult, 40, originTick: 10, GrudgeCause.Direct, generation: 0, inheritedFrom: null, cap: Cap);
            led.Form(C(2), C(9), GrudgeKind.Slaughter, 90, originTick: 20, GrudgeCause.Direct, generation: 0, inheritedFrom: null, cap: Cap);
            led.Form(C(3), C(9), GrudgeKind.Maiming, 90, originTick: 5, GrudgeCause.Direct, generation: 0, inheritedFrom: null, cap: Cap);

            var strong = led.AboveIntensity(50);
            Assert.Equal(2, strong.Count); // 40 被滤掉
            // 稳定排序 (Intensity desc, OriginTick asc, Id asc)：两个 90 中 OriginTick=5 的(C3)在前
            Assert.Equal(C(3), strong[0].Holder);
            Assert.Equal(C(2), strong[1].Holder);
        }

        [Fact]
        public void test_by_holder_index()
        {
            var led = new GrudgeLedger();
            led.Form(C(10), C(20), GrudgeKind.Slaughter, 80, originTick: 0, GrudgeCause.Direct, generation: 0, inheritedFrom: null, cap: Cap);
            led.Form(C(10), C(30), GrudgeKind.Maiming, 60, originTick: 0, GrudgeCause.Direct, generation: 0, inheritedFrom: null, cap: Cap);
            led.Form(C(11), C(20), GrudgeKind.Insult, 20, originTick: 0, GrudgeCause.Direct, generation: 0, inheritedFrom: null, cap: Cap);
            Assert.Equal(2, led.ByHolder(C(10)).Count);
            Assert.Single(led.ByHolder(C(11)));
            Assert.Empty(led.ByHolder(C(99)));
        }

        [Fact]
        public void test_adjust_clamps_and_noop_when_missing()
        {
            var led = new GrudgeLedger();
            led.Form(C(10), C(20), GrudgeKind.Slaughter, 80, originTick: 0, GrudgeCause.Direct, generation: 0, inheritedFrom: null, cap: Cap);
            Assert.True(led.Adjust(C(10), C(20), 50, cap: Cap));   // 80+50→钳 100
            Assert.Equal(100, led.Get(C(10), C(20))!.Intensity);
            Assert.False(led.Adjust(C(99), C(20), 10, cap: Cap));  // 不存在 → no-op false
        }

        [Fact]
        public void test_clone_independent()
        {
            var led = new GrudgeLedger();
            led.Form(C(10), C(20), GrudgeKind.Slaughter, 80, originTick: 0, GrudgeCause.Direct, generation: 0, inheritedFrom: null, cap: Cap);
            var clone = led.Clone();
            Assert.Equal(led.Count, clone.Count);
            // 独立：改 clone 不影响原
            clone.Form(C(11), C(20), GrudgeKind.Insult, 30, originTick: 0, GrudgeCause.Direct, generation: 0, inheritedFrom: null, cap: Cap);
            Assert.Equal(1, led.Count);
            Assert.Equal(2, clone.Count);
        }
    }
}
