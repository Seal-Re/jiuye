using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Jianghu.Core.Tests.Cultivation;
using Xunit;

public class PathRegistryTests
{
    [Fact]
    public void Registry_ValidatesEveryPath()
    {
        // A.0-B3：CodePathSource 先空表（Phase 4/5 逐路填 21 路）。空注册表过即 gate 接通。
        var reg = new PathRegistry(new CodePathSource());
        foreach (var p in reg.All)
            PathValidator.AssertValid(p); // 不抛即过
    }

    [Fact]
    public void Registry_LoadsAndIndexesById()
    {
        var src = new ListPathSource(new[] { TestPaths.ValidFull() });
        var reg = new PathRegistry(src);
        Assert.Single(reg.All);
        Assert.Equal("mock_test_path", reg.ById("mock_test_path").PathId);
    }

    [Fact]
    public void Validator_AcceptsValidFullPath()
    {
        PathValidator.AssertValid(TestPaths.ValidFull()); // 不抛即过
    }

    [Fact]
    public void Validator_RejectsZeroWeightTerm() // R6：×0 项禁入 terms
    {
        var bad = TestPaths.WithTerm(new PowerTerm("res:qi", 0, null));
        Assert.Throws<InvalidOperationException>(() => PathValidator.AssertValid(bad));
    }

    [Fact]
    public void Validator_RejectsDaoHeartTerm() // R3：terms 禁引用 daoHeart
    {
        var bad = TestPaths.WithTerm(new PowerTerm("res:daoHeart", 3, null));
        Assert.Throws<InvalidOperationException>(() => PathValidator.AssertValid(bad));
    }

    [Fact]
    public void Validator_RejectsInnerDemonTerm() // R3：terms 禁引用 innerDemon
    {
        var bad = TestPaths.WithTerm(new PowerTerm("res:innerDemon", 3, null));
        Assert.Throws<InvalidOperationException>(() => PathValidator.AssertValid(bad));
    }

    [Fact]
    public void Validator_RejectsMissingDaoheartCategory() // M1：至少 1 个 Role==daoheart 类目
    {
        var noDaoheart = new[]
        {
            FullCat("攻法"), FullCat("身法"), FullCat("心法"), // 3 类全无 daoheart
        };
        var bad = TestPaths.WithCategories(noDaoheart);
        Assert.Throws<InvalidOperationException>(() => PathValidator.AssertValid(bad));
    }

    [Fact]
    public void Validator_RejectsTooFewCategories() // §12：ArtCategories ≥3
    {
        var twoCats = new[] { FullCat("攻法"), DaoCat() };
        var bad = TestPaths.WithCategories(twoCats);
        Assert.Throws<InvalidOperationException>(() => PathValidator.AssertValid(bad));
    }

    [Fact]
    public void Validator_RejectsCategoryWithTooFewArts() // §12：每类 Arts ≥4
    {
        var thinCat = new ArtCategoryDef("攻法", "attack", 1, 1, new[]
        {
            new ArtDef("x1", "x1", 1, "攻法", Array.Empty<EffectOp>()),
            new ArtDef("x2", "x2", 1, "攻法", Array.Empty<EffectOp>()),
            new ArtDef("x3", "x3", 1, "攻法", Array.Empty<EffectOp>()), // 仅 3 个
        });
        var bad = TestPaths.WithCategories(new[] { thinCat, FullCat("身法"), FullCat("心法"), DaoCat() });
        Assert.Throws<InvalidOperationException>(() => PathValidator.AssertValid(bad));
    }

    [Fact]
    public void Validator_RejectsTooFewSkills() // §12：CombatSkills ≥5
    {
        var fourSkills = new[]
        {
            Skill("k1"), Skill("k2"), Skill("k3"), Skill("k4"), // 仅 4 个
        };
        var bad = TestPaths.WithSkills(fourSkills);
        Assert.Throws<InvalidOperationException>(() => PathValidator.AssertValid(bad));
    }

    [Fact]
    public void Validator_RejectsEmptyTags() // §12：SituationalTags 非空
    {
        var bad = TestPaths.WithTags(Array.Empty<string>());
        Assert.Throws<InvalidOperationException>(() => PathValidator.AssertValid(bad));
    }

    [Fact]
    public void Validator_RejectsTagEqualToPathId() // R2：tag 不得等于任何已知 21 路 pathId
    {
        var bad = TestPaths.WithTags(new[] { "melee", "fa_xiu" }); // fa_xiu 是 21 路之一
        Assert.Throws<InvalidOperationException>(() => PathValidator.AssertValid(bad));
    }

    [Fact]
    public void Validator_RejectsNonCanonPathId() // R4：canon 全名格式 ^[a-z][a-z_]+$ 含下划线
    {
        Assert.Throws<InvalidOperationException>(() => PathValidator.AssertValid(TestPaths.WithPathId("sword")));    // 无下划线
        Assert.Throws<InvalidOperationException>(() => PathValidator.AssertValid(TestPaths.WithPathId("Sword_X")));  // 大写
        Assert.Throws<InvalidOperationException>(() => PathValidator.AssertValid(TestPaths.WithPathId("")));         // 空
    }

    [Fact]
    public void Validator_RejectsLengthMismatchCurve() // M4：四列等长（经 RealmCurve.Validate）
    {
        var b = TestPaths.ValidFull();
        var badCurve = b.Curve with { RealmNames = new[] { "只一个" } }; // 1 vs 3
        var bad = b with { Curve = badCurve };
        Assert.Throws<InvalidOperationException>(() => PathValidator.AssertValid(bad));
    }

    // —— 局部 helper ——

    static ArtDef A(string id) => new ArtDef(id, id, 1, "cat", Array.Empty<EffectOp>());

    static ArtCategoryDef FullCat(string name) =>
        new ArtCategoryDef(name, "attack", 1, 1, new[]
        {
            A(name + "1"), A(name + "2"), A(name + "3"), A(name + "4"),
        });

    static ArtCategoryDef DaoCat() =>
        new ArtCategoryDef("道心", "daoheart", 1, 1, new[]
        {
            A("d1"), A("d2"), A("d3"), A("d4"),
        });

    static CombatSkillDef Skill(string id) =>
        new CombatSkillDef(id, id, 1, Array.Empty<EffectOp>(), new Dictionary<string, int>());

    // 显式 IPathSource，喂任意路集（与 CodePathSource 区分：后者 A.0 为空表）。
    sealed class ListPathSource : IPathSource
    {
        private readonly IReadOnlyList<CultivationPathDef> _paths;
        public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
        public IReadOnlyList<CultivationPathDef> Load() => _paths;
    }
}
