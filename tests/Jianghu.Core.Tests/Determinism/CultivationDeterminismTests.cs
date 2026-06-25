using System.Linq;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Model;
using Jianghu.Sim;
using Xunit;

namespace Jianghu.Core.Tests.Determinism
{
    /// <summary>
    /// Task 6.2 INV-DET-Cult：cultivation-on 确定性 + 快照续跑（spec §11 R-A-NF3）。
    /// 1：同 (种子,配置) 两跑 Chronicle 逐字节。
    /// 2：on 跑 N 步 Clone → 续跑与不中断逐字节（Chronicle + StateSnapshot 双重）。
    /// StateSnapshot 已纳入 CultivationState（消化 TODO）→ 快照续跑能抓 CultivationState 惰性漂移。
    /// 对抗测试故意破坏 Clone 的某 CultivationState 字段拷贝 → 快照续跑应变红（证捕获力）。
    /// </summary>
    public class CultivationDeterminismTests
    {
        static string Chronicle(World w) => string.Join("\n", w.Chronicle.Lines);

        // —— INV-DET-Cult-1：on 同种子同 Chronicle ——
        [Fact]
        public void OnSameSeed_ChronicleByteIdentical()
        {
            var a = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 8, cultivation: true);
            var b = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 8, cultivation: true);
            for (int i = 0; i < 200; i++) { a.Advance(6); b.Advance(6); }
            Assert.Equal(Chronicle(a), Chronicle(b));
        }

        // —— INV-DET-Cult-2：on N 步 Clone 续跑 == 不中断（Chronicle 双重） ——
        [Fact]
        public void OnCloneContinues_ChronicleIdentical()
        {
            var full = WorldFactory.CreateInitial(7, LimitsConfig.Default, 8, cultivation: true);
            for (int i = 0; i < 120; i++) full.Advance(6);
            var fullText = Chronicle(full);

            var part = WorldFactory.CreateInitial(7, LimitsConfig.Default, 8, cultivation: true);
            for (int i = 0; i < 60; i++) part.Advance(6);
            var clone = part.Clone();
            for (int i = 0; i < 60; i++) clone.Advance(6);

            Assert.Equal(fullText, Chronicle(clone));
        }

        // —— INV-DET-Cult-2：on N 步 Clone 续跑 == 不中断（StateSnapshot 双重，含 CultivationState） ——
        [Fact]
        public void OnCloneContinues_StateSnapshotIdentical()
        {
            var full = WorldFactory.CreateInitial(7, LimitsConfig.Default, 8, cultivation: true);
            for (int i = 0; i < 120; i++) full.Advance(6);

            var part = WorldFactory.CreateInitial(7, LimitsConfig.Default, 8, cultivation: true);
            for (int i = 0; i < 60; i++) part.Advance(6);
            var clone = part.Clone();
            // 续跑前：Clone 在全状态快照口径下逐字节一致（含 CultivationState）。
            Assert.Equal(StateSnapshot.Capture(part), StateSnapshot.Capture(clone));
            for (int i = 0; i < 60; i++) clone.Advance(6);

            // 续跑后：与不中断的 full 全状态快照逐字节一致。
            Assert.Equal(StateSnapshot.Capture(full), StateSnapshot.Capture(clone));
        }

        // —— StateSnapshot 确实纳入了 CultivationState：篡改一名修士的 Cultivation 字段 → 快照变 ——
        [Fact]
        public void Snapshot_CapturesCultivationState_FieldsAffectIt()
        {
            var w = WorldFactory.CreateInitial(7, LimitsConfig.Default, 8, cultivation: true);
            for (int i = 0; i < 80; i++) w.Advance(6);

            var modder = FirstCultivator(w);
            Assert.NotNull(modder); // on 必有修士

            var before = StateSnapshot.Capture(w);
            modder!.Cultivation!.RealmIndex += 1;           // 仅改 CultivationState → 若快照纳入则变
            Assert.NotEqual(before, StateSnapshot.Capture(w));
            modder.Cultivation!.RealmIndex -= 1;            // 还原

            var before2 = StateSnapshot.Capture(w);
            modder.Cultivation!.CultivationPoints += 100;   // 改修为累加器 → 快照应变
            Assert.NotEqual(before2, StateSnapshot.Capture(w));
            modder.Cultivation!.CultivationPoints -= 100;   // 还原
            Assert.Equal(before2, StateSnapshot.Capture(w)); // 还原后快照复原（确定性序列化）
        }

        // —— 对抗测试：故意破坏 Clone 的 CultivationState 字段拷贝 → 快照续跑应变红（证捕获力） ——
        [Fact]
        public void Adversarial_BrokenCultivationClone_SnapshotDiverges()
        {
            var part = WorldFactory.CreateInitial(7, LimitsConfig.Default, 8, cultivation: true);
            for (int i = 0; i < 60; i++) part.Advance(6);

            var clone = part.Clone();
            // 模拟「Clone 漏拷 CultivationState 某字段」的回归（此处直接污染 clone 侧 RealmIndex）。
            var victim = FirstCultivator(clone);
            Assert.NotNull(victim);
            victim!.Cultivation!.RealmIndex += 1;

            // 快照已纳入 CultivationState → 立即抓到漂移（续跑前即红）。
            Assert.NotEqual(StateSnapshot.Capture(part), StateSnapshot.Capture(clone));
        }

        static Character? FirstCultivator(World w)
            => w.AliveCharacters().FirstOrDefault(c => c.Cultivation != null);

        // ================================================================
        // story-008 接线：Map/Faction on 确定性（AC 8.4）。验证接线不破坏 B.2，
        // 且 World.Clone 正确深拷 Map/Faction（C-1 前未被生产路径覆盖的分支）。
        // ================================================================

        // —— on+map+faction 同种子 Chronicle 逐字节 ——
        [Fact]
        public void OnMapFaction_SameSeed_ChronicleByteIdentical()
        {
            var a = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 8, cultivation: true, mapOn: true, factionOn: true);
            var b = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 8, cultivation: true, mapOn: true, factionOn: true);
            for (int i = 0; i < 200; i++) { a.Advance(6); b.Advance(6); }
            Assert.Equal(Chronicle(a), Chronicle(b));
        }

        // —— on+map+faction：N 步 Clone 续跑 == 不中断（StateSnapshot 双重，验 Map/Faction 深拷确定性） ——
        [Fact]
        public void OnMapFaction_CloneContinues_StateSnapshotIdentical()
        {
            var full = WorldFactory.CreateInitial(7, LimitsConfig.Default, 8, cultivation: true, mapOn: true, factionOn: true);
            for (int i = 0; i < 120; i++) full.Advance(6);

            var part = WorldFactory.CreateInitial(7, LimitsConfig.Default, 8, cultivation: true, mapOn: true, factionOn: true);
            for (int i = 0; i < 60; i++) part.Advance(6);
            var clone = part.Clone();
            Assert.Equal(StateSnapshot.Capture(part), StateSnapshot.Capture(clone));
            for (int i = 0; i < 60; i++) clone.Advance(6);

            Assert.Equal(StateSnapshot.Capture(full), StateSnapshot.Capture(clone));
        }
    }
}
