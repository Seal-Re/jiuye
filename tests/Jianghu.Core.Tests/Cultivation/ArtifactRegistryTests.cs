using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Artifacts;
using Xunit;

public class ArtifactRegistryTests
{
    static ArtifactDef Sample()
        => new ArtifactDef("art_test_01", "样品", ArtifactForm.Sword,
            ArtifactFunction.Attack, null, ArtifactGrade.Spirit,
            QualityTier.Common, 2, 60, new EffectOp[0],
            EffectRarity.Common, null, "test", "test");

    static ArtifactDef UniqueSample()
        => new ArtifactDef("art_uniq_01", "唯一品", ArtifactForm.Seal,
            ArtifactFunction.Trap, ArtifactFunction.Attack,
            ArtifactGrade.Primordial, QualityTier.Supreme, 9, 1360,
            new EffectOp[0], EffectRarity.Unique, null, "test", "test");

    [Fact]
    public void Register_And_Get_ById()
    {
        var reg = new ArtifactRegistry(new[] { Sample() });
        var found = reg.Get("art_test_01");
        Assert.Equal("样品", found.Name);
    }

    [Fact]
    public void Get_MissingId_Throws()
    {
        var reg = new ArtifactRegistry(new[] { Sample() });
        Assert.Throws<KeyNotFoundException>(
            () => reg.Get("art_nonexistent"));
    }

    [Fact]
    public void DuplicateId_Throws()
    {
        Assert.Throws<System.InvalidOperationException>(() =>
            new ArtifactRegistry(new[] { Sample(), Sample() }));
    }

    [Fact]
    public void ByGrade_FiltersCorrectly()
    {
        var reg = new ArtifactRegistry(new[] { Sample(), UniqueSample() });
        var spirit = reg.ByGrade(ArtifactGrade.Spirit).ToList();
        Assert.Single(spirit);
        Assert.Equal("art_test_01", spirit[0].Id);
    }

    [Fact]
    public void ByForm_FiltersCorrectly()
    {
        var reg = new ArtifactRegistry(new[] { Sample(), UniqueSample() });
        var seals = reg.ByForm(ArtifactForm.Seal).ToList();
        Assert.Single(seals);
    }

    [Fact]
    public void UniqueArtifacts_OnlyReturnsUnique()
    {
        var reg = new ArtifactRegistry(new[] { Sample(), UniqueSample() });
        var uniques = reg.UniqueArtifacts.ToList();
        Assert.Single(uniques);
        Assert.Equal(EffectRarity.Unique, uniques[0].Rarity);
    }

    [Fact]
    public void Data_FirstBatch_HasExpectedCount()
    {
        var all = ArtifactData.All;
        Assert.True(all.Count >= 200, $"Expected >=200 artifacts, got {all.Count}");
    }

    [Fact]
    public void Data_NoDuplicateIds()
    {
        var ids = new HashSet<string>();
        foreach (var a in ArtifactData.All)
            Assert.True(ids.Add(a.Id), $"Duplicate artifact id: {a.Id}");
    }

    [Fact]
    public void Data_AllItemTiersInRange()
    {
        foreach (var a in ArtifactData.All)
            Assert.True(a.ItemTier >= 0 && a.ItemTier <= 9,
                $"Artifact {a.Id} itemTier {a.ItemTier} out of [0,9]");
    }

    [Fact]
    public void Data_NoNullEffects()
    {
        foreach (var a in ArtifactData.All)
            Assert.NotNull(a.Effects);
    }
}
