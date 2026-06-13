using System;
using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Random;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Task 2.2：PathAssigner 生成期定路 + 选功法战技（spec §10）。
    /// EntryGate 纯查询 CanEnter(tags) → 确定性加权抽 PathId（消费 cultRng）→
    /// 各 ArtCategory 按 PickMin/Max（tier 闸）选功法 + N 战技 → NewForPath 初始化 + 填 Chosen。
    /// 纯整数确定性：同种子同输入 → 同 PathId + 同 ChosenArt/Skill。
    /// </summary>
    public class PathAssignerTests
    {
        // —— 测试 mock 注册表：4 路，EntryGate/PathId 各异，从 ValidFull 基线 with 改造 ——

        static CultivationPathDef Path(string id, string entryPred) =>
            TestPaths.ValidFull() with
            {
                PathId = id,
                EntryGate = new EntryGateDef(entryPred),
            };

        static PathRegistry FourPathRegistry()
        {
            return new PathRegistry(new ListPathSource(new[]
            {
                Path("sword_immortal", "tag:sword_root"),
                Path("ti_xiu_hengshi", "tag:body_root"),
                Path("fa_xiu", "tag:spirit_root"),
                Path("gui_xiu_yang_hun", "tag:yin_root"),
            }));
        }

        [Fact]
        public void Assign_IsDeterministic()
        {
            var reg = FourPathRegistry();
            var rng1 = new Pcg32(1, 1);
            var rng2 = new Pcg32(1, 1);
            var tags = new[] { "sword_root", "body_root", "spirit_root", "yin_root" };

            var a = PathAssigner.Assign(tags, reg, rng1);
            var b = PathAssigner.Assign(tags, reg, rng2);

            Assert.NotNull(a.State);
            Assert.NotNull(b.State);
            Assert.Equal(a.PathId, b.PathId);
            Assert.Equal(a.State!.ChosenArtIds, b.State!.ChosenArtIds);
            Assert.Equal(a.State!.ChosenSkillIds, b.State!.ChosenSkillIds);
        }

        [Fact]
        public void Assign_OnlyPicksEntryGatedPath()
        {
            var reg = FourPathRegistry();
            // 仅 body_root → 只有 ti_xiu_hengshi 可入。
            var r = PathAssigner.Assign(new[] { "body_root" }, reg, new Pcg32(42, 1));
            Assert.Equal("ti_xiu_hengshi", r.PathId);
        }

        [Fact]
        public void Assign_FillsChosenArtsAndSkills()
        {
            var reg = FourPathRegistry();
            var r = PathAssigner.Assign(new[] { "sword_root" }, reg, new Pcg32(7, 1));

            Assert.NotNull(r.State);
            // ValidFull：4 类目每类 PickMin=PickMax=1 → 选 4 部功法（每类 1）。
            Assert.Equal(4, r.State!.ChosenArtIds.Count);
            // SelectionRule(1,3) → 战技选 [1,3] 部。
            Assert.InRange(r.State!.ChosenSkillIds.Count, 1, 3);
            // 所选 art id 全属该路某类目；所选 skill id 全属该路战技池。
            var allArtIds = reg.ById("sword_immortal").ArtCategories
                .SelectMany(c => c.Arts).Select(a => a.Id).ToHashSet();
            Assert.All(r.State.ChosenArtIds, id => Assert.Contains(id, allArtIds));
            var allSkillIds = reg.ById("sword_immortal").CombatSkills.Select(s => s.Id).ToHashSet();
            Assert.All(r.State.ChosenSkillIds, id => Assert.Contains(id, allSkillIds));
        }

        [Fact]
        public void Assign_StateRealmIsZero()
        {
            var reg = FourPathRegistry();
            var r = PathAssigner.Assign(new[] { "sword_root" }, reg, new Pcg32(7, 1));
            Assert.NotNull(r.State);
            Assert.Equal(0, r.State!.RealmIndex);
            Assert.Equal(r.PathId, r.State!.PathId);
        }

        [Fact]
        public void Assign_NoEligiblePath_ReturnsNull()
        {
            var reg = FourPathRegistry();
            // 无任何匹配 tag → 散修（null）。
            var r = PathAssigner.Assign(new[] { "no_such_root" }, reg, new Pcg32(7, 1));
            Assert.Null(r.PathId);
            Assert.Null(r.State);
        }

        // 显式 IPathSource，喂任意路集（与 PathRegistryTests 同形）。
        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }
    }
}
