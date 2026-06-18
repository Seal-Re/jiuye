using Jianghu.Cultivation;
using Jianghu.Cultivation.Artifacts;
using Xunit;

public class ArtifactDefTests
{
    [Fact]
    public void ArtifactDef_Creates_WithAllFields()
    {
        var def = new ArtifactDef(
            "art_test_01", "测试飞剑", ArtifactForm.Sword,
            ArtifactFunction.Attack, null,
            ArtifactGrade.Spirit, QualityTier.Common,
            ItemTier: 2, BasePower: 60,
            Effects: new EffectOp[0],
            Rarity: EffectRarity.Common,
            ElementHint: null,
            FlavorText: "一柄普通的测试飞剑",
            SourceHint: "万器商会"
        );
        Assert.Equal("art_test_01", def.Id);
        Assert.Equal("测试飞剑", def.Name);
        Assert.Equal(ArtifactForm.Sword, def.Form);
        Assert.Equal(ArtifactFunction.Attack, def.PrimaryFunc);
        Assert.Null(def.SecondaryFunc);
        Assert.Equal(ArtifactGrade.Spirit, def.Grade);
        Assert.Equal(QualityTier.Common, def.Quality);
        Assert.Equal(2, def.ItemTier);
        Assert.Equal(60, def.BasePower);
        Assert.Equal(EffectRarity.Common, def.Rarity);
    }

    [Fact]
    public void UniqueArtifact_HasSecondaryFunc()
    {
        var def = new ArtifactDef(
            "art_uniq_01", "落宝金钱", ArtifactForm.Ring,
            ArtifactFunction.Attack, ArtifactFunction.Snatch,
            ArtifactGrade.Primordial, QualityTier.Supreme,
            ItemTier: 9, BasePower: 1360,
            Effects: new[] { Modules.Special("luobao", 3, 0, "落宝金光") },
            Rarity: EffectRarity.Unique,
            ElementHint: "金",
            FlavorText: "封神顶级收宝法宝，通天灵宝落宝金钱",
            SourceHint: "古道宗遗迹·封神台"
        );
        Assert.Equal(EffectRarity.Unique, def.Rarity);
        Assert.Equal(ArtifactFunction.Snatch, def.SecondaryFunc);
        Assert.Single(def.Effects);
    }
}
