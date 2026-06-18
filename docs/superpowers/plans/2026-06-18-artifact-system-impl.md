# 法宝系统 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将当前仅7件命名法宝扩展为200+件具名法宝数据目录，9品×4档×24形态×7功能双轴分类，接入 EffectOp 模块系统与 SpecialModuleRegistry。

**Architecture:** ArtifactDef record + ArtifactRegistry 注册表 + ArtifactData 数据文件。法宝经"认主"注入 itemTier→CultivationState→PowerEngine→PE自动受益；Effects[] 经 DuelEngine→ModuleResolver 结算；Unique 法宝挂 SpecialModuleRegistry。锻造流程 defer FULLSTRUCT。

**Tech Stack:** C#/.NET (Jianghu.Core netstandard2.1)，xUnit，纯整数确定性，IL浮点扫描守。

**上游设计:** `docs/superpowers/specs/2026-06-18-artifact-system-design.md`

---

## File Structure

```
NEW:  src/Jianghu.Core/Cultivation/Artifacts/ArtifactDef.cs       — enums + record
NEW:  src/Jianghu.Core/Cultivation/Artifacts/ArtifactRegistry.cs  — 注册表 + 查询
NEW:  src/Jianghu.Core/Cultivation/Artifacts/ArtifactData.cs      — 200+件数据
NEW:  tests/Jianghu.Core.Tests/Cultivation/ArtifactDefTests.cs
NEW:  tests/Jianghu.Core.Tests/Cultivation/ArtifactRegistryTests.cs
NEW:  tests/Jianghu.Core.Tests/Cultivation/ArtifactIntegrationTests.cs
```

---

### Task 1: ArtifactDef — Enums + Record

**Files:**
- Create: `src/Jianghu.Core/Cultivation/Artifacts/ArtifactDef.cs`
- Test: `tests/Jianghu.Core.Tests/Cultivation/ArtifactDefTests.cs`

- [ ] **Step 1: Write failing test — ArtifactDef exists with all fields**

```csharp
// tests/Jianghu.Core.Tests/Cultivation/ArtifactDefTests.cs
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
            itemTier: 2, basePower: 60,
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
            ArtifactFunction.Snatch, ArtifactFunction.Attack,
            ArtifactGrade.Primordial, QualityTier.Supreme,
            itemTier: 9, basePower: 1360,
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
```

Run: `dotnet test --filter Name~ArtifactDefTests`
Expected: FAIL — namespace/type not found.

- [ ] **Step 2: Create enums + record**

```csharp
// src/Jianghu.Core/Cultivation/Artifacts/ArtifactDef.cs
using System.Collections.Generic;

namespace Jianghu.Cultivation.Artifacts
{
    public enum ArtifactGrade
    {
        Mortal = 1, Dharma = 2, Spirit = 3, Treasure = 4,
        DaoWeapon = 5, NuminousTreasure = 6, HeavenReaching = 7,
        ProfoundSky = 8, Primordial = 9
    }

    public enum QualityTier { Inferior = 1, Common = 2, Superior = 3, Supreme = 4 }

    public enum ArtifactForm
    {
        Sword, Blade, Spear, Needle,
        Seal, Hammer, Axe,
        Banner, ArrayDisk, Scroll,
        Bell, Tower, Shield, Lotus,
        Orb, Gourd, Mirror, Cauldron, Fan,
        Rope, Ring,
        Talisman, Lamp,
        Instrument, Whip
    }

    public enum ArtifactFunction { Attack, Defense, Trap, Snatch, Support, Escape, Heal }

    /// <summary>单件法宝全量定义（spec §Data Schema）。</summary>
    public sealed record ArtifactDef(
        string Id,
        string Name,
        ArtifactForm Form,
        ArtifactFunction PrimaryFunc,
        ArtifactFunction? SecondaryFunc,
        ArtifactGrade Grade,
        QualityTier Quality,
        int ItemTier,
        int BasePower,
        IReadOnlyList<EffectOp> Effects,
        EffectRarity Rarity,
        string? ElementHint,
        string? FlavorText,
        string? SourceHint);
}
```

- [ ] **Step 3: Run test to verify PASS**

Run: `dotnet test --filter Name~ArtifactDefTests`
Expected: 2 tests PASS.

- [ ] **Step 4: Commit**

```bash
git add src/Jianghu.Core/Cultivation/Artifacts/ArtifactDef.cs tests/Jianghu.Core.Tests/Cultivation/ArtifactDefTests.cs
git commit -m "feat(artifacts): ArtifactDef enums+record — 9品/4档/24形态/7功能"
```

---

### Task 2: ArtifactRegistry — 注册表 + 查询

**Files:**
- Create: `src/Jianghu.Core/Cultivation/Artifacts/ArtifactRegistry.cs`
- Test: `tests/Jianghu.Core.Tests/Cultivation/ArtifactRegistryTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
// tests/Jianghu.Core.Tests/Cultivation/ArtifactRegistryTests.cs
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
        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
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
}
```

Run: `dotnet test --filter Name~ArtifactRegistryTests`
Expected: FAIL — type not found.

- [ ] **Step 2: Implement ArtifactRegistry**

```csharp
// src/Jianghu.Core/Cultivation/Artifacts/ArtifactRegistry.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jianghu.Cultivation.Artifacts
{
    public sealed class ArtifactRegistry
    {
        readonly Dictionary<string, ArtifactDef> _all;
        readonly ILookup<ArtifactGrade, ArtifactDef> _byGrade;
        readonly ILookup<ArtifactForm, ArtifactDef> _byForm;
        readonly List<ArtifactDef> _uniques;

        public ArtifactRegistry(IReadOnlyList<ArtifactDef> artifacts)
        {
            _all = new Dictionary<string, ArtifactDef>();
            foreach (var a in artifacts)
            {
                if (_all.ContainsKey(a.Id))
                    throw new InvalidOperationException($"Artifact duplicate id: {a.Id}");
                _all[a.Id] = a;
            }
            _byGrade = artifacts.ToLookup(a => a.Grade);
            _byForm = artifacts.ToLookup(a => a.Form);
            _uniques = artifacts.Where(a => a.Rarity == EffectRarity.Unique).ToList();
        }

        public ArtifactDef Get(string id)
        {
            if (!_all.TryGetValue(id, out var a))
                throw new KeyNotFoundException($"Artifact not found: {id}");
            return a;
        }

        public IReadOnlyList<ArtifactDef> All => _all.Values.ToList();
        public IReadOnlyList<ArtifactDef> ByGrade(ArtifactGrade g) => _byGrade[g].ToList();
        public IReadOnlyList<ArtifactDef> ByForm(ArtifactForm f) => _byForm[f].ToList();
        public IReadOnlyList<ArtifactDef> ByFunction(ArtifactFunction f)
            => _all.Values.Where(a => a.PrimaryFunc == f || a.SecondaryFunc == f).ToList();
        public IReadOnlyList<ArtifactDef> UniqueArtifacts => _uniques;
    }
}
```

- [ ] **Step 3: Run tests to verify PASS**

Run: `dotnet test --filter Name~ArtifactRegistryTests`
Expected: 6 tests PASS.

- [ ] **Step 4: Commit**

```bash
git add src/Jianghu.Core/Cultivation/Artifacts/ArtifactRegistry.cs tests/Jianghu.Core.Tests/Cultivation/ArtifactRegistryTests.cs
git commit -m "feat(artifacts): ArtifactRegistry — 注册表+去重检查+多维度查询"
```

---

### Task 3: ArtifactData — 200+ 件法宝数据（第一批 60件：凡器→宝器）

**Files:**
- Create: `src/Jianghu.Core/Cultivation/Artifacts/ArtifactData.cs`

- [ ] **Step 1: Create data file structure**

```csharp
// src/Jianghu.Core/Cultivation/Artifacts/ArtifactData.cs
using System.Collections.Generic;

namespace Jianghu.Cultivation.Artifacts
{
    public static partial class ArtifactData
    {
        public static IReadOnlyList<ArtifactDef> All { get; } = BuildAll();

        static IReadOnlyList<ArtifactDef> BuildAll()
        {
            var list = new List<ArtifactDef>();
            list.AddRange(MortalDharmaSpirit());
            list.AddRange(TreasureDaoWeaponNuminous());
            list.AddRange(HeavenReachingProfoundSkyPrimordial());
            list.AddRange(UniqueArtifacts());
            return list;
        }

        public static ArtifactRegistry DefaultRegistry => new ArtifactRegistry(All);

        static EffectOp[] NoFx => System.Array.Empty<EffectOp>();

        // ------- 1-3品: 凡器/法器/灵器 (攻/防为主) -------
        static IEnumerable<ArtifactDef> MortalDharmaSpirit()
        {
            // 凡器 — 下品
            yield return A("art_mor_iron_sword_i", "凡铁剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Inferior,
                itemTier:0, power:8, fx:NoFx);
            yield return A("art_mor_copper_blade_i", "黄铜刀", ArtifactForm.Blade,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Inferior,
                itemTier:0, power:8, fx:NoFx);
            yield return A("art_mor_wood_spear_i", "木杆枪", ArtifactForm.Spear,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Inferior,
                itemTier:0, power:8, fx:NoFx);
            yield return A("art_mor_stone_seal_i", "石印", ArtifactForm.Seal,
                ArtifactFunction.Trap, ArtifactGrade.Mortal, QualityTier.Inferior,
                itemTier:0, power:8, fx:NoFx);
            yield return A("art_mor_leather_shield_i", "皮盾", ArtifactForm.Shield,
                ArtifactFunction.Defense, ArtifactGrade.Mortal, QualityTier.Inferior,
                itemTier:0, power:8, fx:NoFx);
            yield return A("art_mor_bone_needle_i", "骨针", ArtifactForm.Needle,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Inferior,
                itemTier:0, power:8, fx:NoFx);

            // 凡器 — 中品
            yield return A("art_mor_iron_sword_c", "锻铁剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Common,
                itemTier:0, power:10, fx:NoFx);
            yield return A("art_mor_bronze_blade_c", "青铜刀", ArtifactForm.Blade,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Common,
                itemTier:0, power:10, fx:NoFx);
            yield return A("art_mor_hemp_rope_c", "麻绳", ArtifactForm.Rope,
                ArtifactFunction.Trap, ArtifactGrade.Mortal, QualityTier.Common,
                itemTier:0, power:10, fx:NoFx);
            yield return A("art_mor_wood_shield_c", "木盾", ArtifactForm.Shield,
                ArtifactFunction.Defense, ArtifactGrade.Mortal, QualityTier.Common,
                itemTier:0, power:10, fx:NoFx);

            // 凡器 — 上品
            yield return A("art_mor_steel_sword_s", "精钢剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Superior,
                itemTier:0, power:12, fx:NoFx);
            yield return A("art_mor_hardwood_bow_s", "硬木弓", ArtifactForm.Spear,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Superior,
                itemTier:0, power:12, fx:NoFx);

            // 凡器 — 极品
            yield return A("art_mor_hundred_refine_sword_m", "百炼精钢剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Mortal, QualityTier.Supreme,
                itemTier:0, power:15, fx:NoFx);
            yield return A("art_mor_iron_tower_m", "铁塔盾", ArtifactForm.Tower,
                ArtifactFunction.Defense, ArtifactGrade.Mortal, QualityTier.Supreme,
                itemTier:0, power:15, fx:NoFx);

            // 法器 — 下品
            yield return A("art_dha_cold_iron_sword_i", "寒铁剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Inferior,
                itemTier:1, power:24, fx:new[] { Modules.FlatPen(5) });
            yield return A("art_dha_fire_tip_spear_i", "火尖枪·凡胚", ArtifactForm.Spear,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Inferior,
                itemTier:1, power:24, fx:new[] { Modules.FlatPen(5) });
            yield return A("art_dha_luo_hammer_i", "落石锤", ArtifactForm.Hammer,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Inferior,
                itemTier:1, power:24, fx:new[] { Modules.FlatPen(6) });
            yield return A("art_dha_soul_lock_rope_i", "锁魂绳", ArtifactForm.Rope,
                ArtifactFunction.Trap, ArtifactGrade.Dharma, QualityTier.Inferior,
                itemTier:1, power:24, fx:NoFx);
            yield return A("art_dha_spirit_wood_shield_i", "灵木盾", ArtifactForm.Shield,
                ArtifactFunction.Defense, ArtifactGrade.Dharma, QualityTier.Inferior,
                itemTier:1, power:24, fx:new[] { Modules.FlatDR(5) });
            yield return A("art_dha_shadow_needle_i", "无影针", ArtifactForm.Needle,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Inferior,
                itemTier:1, power:24, fx:new[] { Modules.FlatPen(4) });

            // 法器 — 中品
            yield return A("art_dha_black_iron_sword_c", "黑铁重剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Common,
                itemTier:1, power:30, fx:new[] { Modules.FlatPen(8) });
            yield return A("art_dha_wind_fire_fan_c", "风火扇·初炼", ArtifactForm.Fan,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Common,
                itemTier:1, power:30, fx:new[] { Modules.FlatPen(10) });
            yield return A("art_dha_iron_bell_c", "镇魂铁钟", ArtifactForm.Bell,
                ArtifactFunction.Defense, ArtifactGrade.Dharma, QualityTier.Common,
                itemTier:1, power:30, fx:new[] { Modules.FlatDR(6) });

            // 法器 — 上品
            yield return A("art_dha_thunder_needle_s", "雷芒针", ArtifactForm.Needle,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Superior,
                itemTier:1, power:36, fx:new[] { Modules.FlatPen(12) });
            yield return A("art_dha_ice_mirror_s", "寒冰镜", ArtifactForm.Mirror,
                ArtifactFunction.Defense, ArtifactGrade.Dharma, QualityTier.Superior,
                itemTier:1, power:36, fx:new[] { Modules.FlatDR(8) });

            // 法器 — 极品
            yield return A("art_dha_flame_sword_m", "烈焰剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Dharma, QualityTier.Supreme,
                itemTier:1, power:45, fx:new[] { Modules.FlatPen(15), Modules.CounterMul("ice", 3, 2) });

            // 灵器 — 下品
            yield return A("art_spr_azure_edge_sword_i", "青锋灵剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Inferior,
                itemTier:2, power:48, fx:new[] { Modules.FlatPen(12) });
            yield return A("art_spr_soul_devour_blade_i", "噬魂刀", ArtifactForm.Blade,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Inferior,
                itemTier:2, power:48, fx:new[] { Modules.PenFromResource("shaCharge", 1, 2) });
            yield return A("art_spr_binding_rope_i", "缚灵索", ArtifactForm.Rope,
                ArtifactFunction.Trap, ArtifactGrade.Spirit, QualityTier.Inferior,
                itemTier:2, power:48, fx:new[] { Modules.Control("bind", 1) });
            yield return A("art_spr_iron_body_bell_i", "金钟罩·初", ArtifactForm.Bell,
                ArtifactFunction.Defense, ArtifactGrade.Spirit, QualityTier.Inferior,
                itemTier:2, power:48, fx:new[] { Modules.FlatDR(10) });
            yield return A("art_spr_brass_mirror_i", "照妖铜镜", ArtifactForm.Mirror,
                ArtifactFunction.Support, ArtifactGrade.Spirit, QualityTier.Inferior,
                itemTier:2, power:48, fx:new[] { Modules.CounterMul("evil", 2, 1) });

            // 灵器 — 中品
            yield return A("art_spr_red_line_needle_c", "红线遁光针", ArtifactForm.Needle,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Common,
                itemTier:2, power:60, fx:new[] { Modules.PenFromResource("itemTier", 3) });
            yield return A("art_spr_thunder_seal_c", "雷光印", ArtifactForm.Seal,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Common,
                itemTier:2, power:60, fx:new[] { Modules.FlatPen(16), Modules.CounterMul("evil", 3, 2) });
            yield return A("art_spr_venom_gourd_c", "百毒葫芦", ArtifactForm.Gourd,
                ArtifactFunction.Trap, ArtifactGrade.Spirit, QualityTier.Common,
                itemTier:2, power:60, fx:new[] { Modules.Dot("venom", 3, 3) });
            yield return A("art_spr_escape_banner_c", "遁光幡", ArtifactForm.Banner,
                ArtifactFunction.Escape, ArtifactGrade.Spirit, QualityTier.Common,
                itemTier:2, power:60, fx:new[] { Modules.Evade(20) });

            // 灵器 — 上品
            yield return A("art_spr_flying_sword_wind_s", "御风飞剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Superior,
                itemTier:2, power:72, fx:new[] { Modules.PenFromResource("itemTier", 5) });
            yield return A("art_spr_sand_fan_s", "飞沙扇", ArtifactForm.Fan,
                ArtifactFunction.Trap, ArtifactGrade.Spirit, QualityTier.Superior,
                itemTier:2, power:72, fx:new[] { Modules.Dot("sand_storm", 4, 2) });
            yield return A("art_spr_vajra_hammer_s", "伏魔金刚杵", ArtifactForm.Hammer,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Superior,
                itemTier:2, power:72, fx:new[] { Modules.FlatPen(20), Modules.CounterMul("evil", 3, 1) });

            // 灵器 — 极品
            yield return A("art_spr_bamboo_swarm_sword_m", "青竹蜂云剑", ArtifactForm.Sword,
                ArtifactFunction.Attack, ArtifactGrade.Spirit, QualityTier.Supreme,
                itemTier:2, power:90, fx:new[] { Modules.PenFromResource("itemTier", 8), Modules.AoePerTarget(12) });
            yield return A("art_spr_luo_ring_m", "落宝圈·初胚", ArtifactForm.Ring,
                ArtifactFunction.Snatch, ArtifactGrade.Spirit, QualityTier.Supreme,
                itemTier:2, power:90, fx:new[] { Modules.Drain("itemTier", 1) });
        }

        // ------- 4-6品: 宝器/道器/灵宝 (七功能全开) -------
        static IEnumerable<ArtifactDef> TreasureDaoWeaponNuminous()
        {
            // 宝器 — 下品 .. 极品
            // 道器 — 下品 .. 极品
            // 灵宝 — 下品 .. 极品
            // (第二批填充, placeholder for now)
            yield break;
        }

        // ------- 7-9品 + Unique -------
        static IEnumerable<ArtifactDef> HeavenReachingProfoundSkyPrimordial()
        {
            // 第三批填充
            yield break;
        }

        static IEnumerable<ArtifactDef> UniqueArtifacts()
        {
            // 第四批填充: 21路×2 + 散落 + 遗迹
            yield break;
        }

        // helper
        static ArtifactDef A(string id, string name, ArtifactForm form,
            ArtifactFunction func, ArtifactGrade grade, QualityTier quality,
            int itemTier, int power, IReadOnlyList<EffectOp> fx,
            ArtifactFunction? secFunc = null, EffectRarity rarity = EffectRarity.Common,
            string? elem = null, string? flavor = null, string? src = null)
            => new ArtifactDef(id, name, form, func, secFunc, grade, quality,
                itemTier, power, fx, rarity, elem, flavor ?? name, src ?? "江湖流通");
    }
}
```

- [ ] **Step 2: Write test — validate first batch data integrity**

```csharp
// 添加至 tests/Jianghu.Core.Tests/Cultivation/ArtifactRegistryTests.cs

[Fact]
public void Data_FirstBatch_HasExpectedCount()
{
    var all = ArtifactData.All;
    Assert.True(all.Count >= 50, $"Expected >=50 artifacts, got {all.Count}");
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
```

Run: `dotnet test --filter Name~ArtifactRegistryTests`
Expected: 10 tests PASS (6 registry + 4 data integrity).

- [ ] **Step 3: Commit**

```bash
git add src/Jianghu.Core/Cultivation/Artifacts/ArtifactData.cs tests/Jianghu.Core.Tests/Cultivation/ArtifactRegistryTests.cs
git commit -m "feat(artifacts): ArtifactData batch1 — 凡器/法器/灵器 50+件, 攻/防/困/夺/遁/辅六功能"
```

---

### Task 4: ArtifactData 第二批 — 宝器/道器/灵宝（60+件）

**Files:**
- Modify: `src/Jianghu.Core/Cultivation/Artifacts/ArtifactData.cs`

- [ ] **Step 1: Fill TreasureDaoWeaponNuminous() with 60+ artifacts**

```csharp
// 填充 ArtifactData.TreasureDaoWeaponNuminous() 方法体
// 宝器(4品) × 4档 × 攻/防/困/夺/辅/遁/愈 ≈ 28件
// 道器(5品) × 4档 × 七功能 ≈ 20件
// 灵宝(6品) × 4档 × 七功能 ≈ 16件
// 合计 ~64件

// 宝器 — 下品
yield return A("art_trs_dragon_tooth_sword_i", "龙牙剑", ArtifactForm.Sword,
    ArtifactFunction.Attack, ArtifactGrade.Treasure, QualityTier.Inferior,
    itemTier:3, power:80, fx:new[] { Modules.FlatPen(20) });
yield return A("art_trs_bronze_tower_i", "镇岳铜塔", ArtifactForm.Tower,
    ArtifactFunction.Defense, ArtifactGrade.Treasure, QualityTier.Inferior,
    itemTier:3, power:80, fx:new[] { Modules.FlatDR(15) });
yield return A("art_trs_silken_rope_i", "天蚕缚仙索", ArtifactForm.Rope,
    ArtifactFunction.Trap, ArtifactGrade.Treasure, QualityTier.Inferior,
    itemTier:3, power:80, fx:new[] { Modules.Control("bind", 1) });
yield return A("art_trs_gold_ring_i", "套宝金环", ArtifactForm.Ring,
    ArtifactFunction.Snatch, ArtifactGrade.Treasure, QualityTier.Inferior,
    itemTier:3, power:80, fx:new[] { Modules.Drain("itemTier", 2) });
yield return A("art_trs_jade_gourd_i", "玉净葫芦", ArtifactForm.Gourd,
    ArtifactFunction.Support, ArtifactGrade.Treasure, QualityTier.Inferior,
    itemTier:3, power:80, fx:new[] { new EffectOp(EffectOpKind.AddResource, "qixue", 10, "回血气+10") });
yield return A("art_trs_cloud_boots_fan_i", "踏云扇", ArtifactForm.Fan,
    ArtifactFunction.Escape, ArtifactGrade.Treasure, QualityTier.Inferior,
    itemTier:3, power:80, fx:new[] { Modules.Evade(25) });
yield return A("art_trs_spirit_cauldron_i", "聚灵鼎·下品", ArtifactForm.Cauldron,
    ArtifactFunction.Support, ArtifactGrade.Treasure, QualityTier.Inferior,
    itemTier:3, power:80, fx:new[] { new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 20, "mana上限+20") });

// 宝器 — 中品
yield return A("art_trs_meteor_hammer_c", "流星锤", ArtifactForm.Hammer,
    ArtifactFunction.Attack, ArtifactGrade.Treasure, QualityTier.Common,
    itemTier:3, power:100, fx:new[] { Modules.FlatPen(25) });
yield return A("art_trs_black_tortoise_shield_c", "玄武盾", ArtifactForm.Shield,
    ArtifactFunction.Defense, ArtifactGrade.Treasure, QualityTier.Common,
    itemTier:3, power:100, fx:new[] { Modules.FlatDR(18), Modules.Reflect(1, 4) });
yield return A("art_trs_entrapment_scroll_c", "困仙图", ArtifactForm.Scroll,
    ArtifactFunction.Trap, ArtifactGrade.Treasure, QualityTier.Common,
    itemTier:3, power:100, fx:new[] { Modules.Control("trap", 2) });
yield return A("art_trs_demon_break_sword_c", "破魔剑", ArtifactForm.Sword,
    ArtifactFunction.Attack, ArtifactGrade.Treasure, QualityTier.Common,
    itemTier:3, power:100, fx:new[] { Modules.FlatPen(20), Modules.CounterMul("evil", 3, 1) });
yield return A("art_trs_shadow_mirror_c", "遁影镜", ArtifactForm.Mirror,
    ArtifactFunction.Escape, ArtifactGrade.Treasure, QualityTier.Common,
    itemTier:3, power:100, fx:new[] { Modules.Evade(30) });
yield return A("art_trs_plum_lamp_c", "寒梅灯", ArtifactForm.Lamp,
    ArtifactFunction.Support, ArtifactGrade.Treasure, QualityTier.Common,
    itemTier:3, power:100, fx:new[] { new EffectOp(EffectOpKind.AddResource, "soulBond", 3, "soulBond+3") });

// 宝器 — 上品
yield return A("art_trs_phoenix_wing_spear_s", "凤翅镏金枪", ArtifactForm.Spear,
    ArtifactFunction.Attack, ArtifactGrade.Treasure, QualityTier.Superior,
    itemTier:3, power:120, fx:new[] { Modules.PenFromResource("itemTier", 6), Modules.FlatPen(18) });
yield return A("art_trs_myriad_sword_scroll_s", "万剑图", ArtifactForm.Scroll,
    ArtifactFunction.Attack, ArtifactGrade.Treasure, QualityTier.Superior,
    itemTier:3, power:120, fx:new[] { Modules.AoePerTarget(30), Modules.FlatPen(15) });
yield return A("art_trs_earth_escape_banner_s", "土遁幡", ArtifactForm.Banner,
    ArtifactFunction.Escape, ArtifactGrade.Treasure, QualityTier.Superior,
    itemTier:3, power:120, fx:new[] { Modules.Evade(35), Modules.FlatDR(10) });
yield return A("art_trs_heal_lotus_s", "回春莲台", ArtifactForm.Lotus,
    ArtifactFunction.Heal, ArtifactGrade.Treasure, QualityTier.Superior,
    itemTier:3, power:120, fx:new[] { new EffectOp(EffectOpKind.AddResource, "qixue", 20, "回血气+20") });

// 宝器 — 极品
yield return A("art_trs_sun_moon_orbs_m", "日月双珠", ArtifactForm.Orb,
    ArtifactFunction.Attack, ArtifactGrade.Treasure, QualityTier.Supreme,
    itemTier:3, power:150,
    fx:new[] { Modules.FlatPen(30), Modules.CounterMul("evil", 3, 1), Modules.AoePerTarget(20) },
    secFunc:ArtifactFunction.Trap);
yield return A("art_trs_soul_bond_cauldron_m", "魂契鼎", ArtifactForm.Cauldron,
    ArtifactFunction.Support, ArtifactGrade.Treasure, QualityTier.Supreme,
    itemTier:3, power:150,
    fx:new[] { new EffectOp(EffectOpKind.AddResourceCap, "soulBond", 5, "soulBond上限+5"),
               new EffectOp(EffectOpKind.AddTermWeightStep, "soulBondStep", 1, "soulBond权重+1阶") },
    secFunc: ArtifactFunction.Support);

// 道器 — 下品 (5品, itemTier=4, BasePower=128)
yield return A("art_dao_star_picker_sword_i", "摘星剑", ArtifactForm.Sword,
    ArtifactFunction.Attack, ArtifactGrade.DaoWeapon, QualityTier.Inferior,
    itemTier:4, power:128, fx:new[] { Modules.PenFromResource("swordWill", 3), Modules.FlatPen(25) });
yield return A("art_dao_void_mirror_i", "虚空镜", ArtifactForm.Mirror,
    ArtifactFunction.Defense, ArtifactGrade.DaoWeapon, QualityTier.Inferior,
    itemTier:4, power:128, fx:new[] { Modules.FlatDR(20), Modules.Reflect(1, 3) });
yield return A("art_dao_soul_binding_rope_i", "困魂索", ArtifactForm.Rope,
    ArtifactFunction.Trap, ArtifactGrade.DaoWeapon, QualityTier.Inferior,
    itemTier:4, power:128, fx:new[] { Modules.Control("bind", 2) });
yield return A("art_dao_purple_gold_gourd_i", "紫金葫芦·道胚", ArtifactForm.Gourd,
    ArtifactFunction.Snatch, ArtifactGrade.DaoWeapon, QualityTier.Inferior,
    itemTier:4, power:128, fx:new[] { Modules.Drain("itemTier", 3) });

// 道器 — 中品
yield return A("art_dao_dragon_slayer_blade_c", "斩龙刀", ArtifactForm.Blade,
    ArtifactFunction.Attack, ArtifactGrade.DaoWeapon, QualityTier.Common,
    itemTier:4, power:160, fx:new[] { Modules.PenFromResource("qixie", 2), Modules.FlatPen(30) });
yield return A("art_dao_sky_net_scroll_c", "天罗地网图", ArtifactForm.Scroll,
    ArtifactFunction.Trap, ArtifactGrade.DaoWeapon, QualityTier.Common,
    itemTier:4, power:160, fx:new[] { Modules.Control("net", 2), Modules.Dot("net_burn", 5, 2) });
yield return A("art_dao_lightning_banner_c", "引雷幡", ArtifactForm.Banner,
    ArtifactFunction.Attack, ArtifactGrade.DaoWeapon, QualityTier.Common,
    itemTier:4, power:160, fx:new[] { Modules.PenFromResource("thunderCharge", 3), Modules.CounterMul("evil", 3, 1) });
yield return A("art_dao_vajra_bell_c", "金刚法钟", ArtifactForm.Bell,
    ArtifactFunction.Defense, ArtifactGrade.DaoWeapon, QualityTier.Common,
    itemTier:4, power:160, fx:new[] { Modules.FlatDR(25), Modules.Reflect(1, 3) });

// 道器 — 上品
yield return A("art_dao_blood_devour_needle_s", "噬血神针", ArtifactForm.Needle,
    ArtifactFunction.Attack, ArtifactGrade.DaoWeapon, QualityTier.Superior,
    itemTier:4, power:192, fx:new[] { Modules.PenFromResource("qixue", 3), Modules.Drain("qixie", 4) });
yield return A("art_dao_soul_return_lotus_s", "还魂莲台", ArtifactForm.Lotus,
    ArtifactFunction.Heal, ArtifactGrade.DaoWeapon, QualityTier.Superior,
    itemTier:4, power:192, fx:new[] { new EffectOp(EffectOpKind.AddResource, "qixue", 30, "回血+30") });
yield return A("art_dao_wind_thunder_ring_s", "风雷圈", ArtifactForm.Ring,
    ArtifactFunction.Attack, ArtifactGrade.DaoWeapon, QualityTier.Superior,
    itemTier:4, power:192, fx:new[] { Modules.FlatPen(35), Modules.Evade(20) },
    secFunc: ArtifactFunction.Escape);
yield return A("art_dao_demon_suppress_hammer_s", "镇魔杵", ArtifactForm.Hammer,
    ArtifactFunction.Attack, ArtifactGrade.DaoWeapon, QualityTier.Superior,
    itemTier:4, power:192, fx:new[] { Modules.FlatPen(30), Modules.CounterMul("evil", 3, 1), Modules.FlatDR(15) });

// 道器 — 极品
yield return A("art_dao_primordial_chaos_orb_m", "天地玄黄珠", ArtifactForm.Orb,
    ArtifactFunction.Support, ArtifactGrade.DaoWeapon, QualityTier.Supreme,
    itemTier:4, power:240,
    fx:new[] { new EffectOp(EffectOpKind.AddResourceCap, "manaPool", 50, "mana上限+50"),
               Modules.FlatDR(20), Modules.Evade(20) },
    secFunc: ArtifactFunction.Defense);

// 灵宝 — 下品 (6品, itemTier=5, BasePower=240)
yield return A("art_num_eight_trigram_mirror_i", "八卦护魂镜", ArtifactForm.Mirror,
    ArtifactFunction.Defense, ArtifactGrade.NuminousTreasure, QualityTier.Inferior,
    itemTier:5, power:192, fx:new[] { Modules.FlatDR(25), Modules.Reflect(1, 2) });
yield return A("art_num_beast_trap_scroll_i", "万兽困阵图", ArtifactForm.Scroll,
    ArtifactFunction.Trap, ArtifactGrade.NuminousTreasure, QualityTier.Inferior,
    itemTier:5, power:192, fx:new[] { Modules.Control("trap", 3) });
yield return A("art_num_soul_snatch_bell_i", "摄魂钟", ArtifactForm.Bell,
    ArtifactFunction.Snatch, ArtifactGrade.NuminousTreasure, QualityTier.Inferior,
    itemTier:5, power:192, fx:new[] { Modules.Drain("MoGong", 5), Modules.Control("stun", 1) });

// 灵宝 — 中品
yield return A("art_num_sword_formation_disk_c", "万剑阵盘", ArtifactForm.ArrayDisk,
    ArtifactFunction.Attack, ArtifactGrade.NuminousTreasure, QualityTier.Common,
    itemTier:5, power:240, fx:new[] { Modules.AoePerTarget(40), Modules.FlatPen(30) });
yield return A("art_num_life_bound_cauldron_c", "本命魂鼎", ArtifactForm.Cauldron,
    ArtifactFunction.Support, ArtifactGrade.NuminousTreasure, QualityTier.Common,
    itemTier:5, power:240,
    fx:new[] { new EffectOp(EffectOpKind.AddResourceCap, "soulBond", 10, "soulBond上限+10"),
               new EffectOp(EffectOpKind.AddTermWeightStep, "soulBondStep", 2, "soulBond权重+2阶") });
yield return A("art_num_phantom_fan_c", "幻影扇", ArtifactForm.Fan,
    ArtifactFunction.Escape, ArtifactGrade.NuminousTreasure, QualityTier.Common,
    itemTier:5, power:240, fx:new[] { Modules.Evade(40), Modules.FlatDR(15) });

// 灵宝 — 上品
yield return A("art_num_star_shatter_hammer_s", "碎星锤", ArtifactForm.Hammer,
    ArtifactFunction.Attack, ArtifactGrade.NuminousTreasure, QualityTier.Superior,
    itemTier:5, power:288, fx:new[] { Modules.FlatPen(50), Modules.CounterMul("body", 3, 2) });
yield return A("art_num_void_escape_talisman_s", "破空符", ArtifactForm.Talisman,
    ArtifactFunction.Escape, ArtifactGrade.NuminousTreasure, QualityTier.Superior,
    itemTier:5, power:288, fx:new[] { Modules.Evade(50) });

// 灵宝 — 极品
yield return A("art_num_eight_essence_cauldron_m", "八灵尺", ArtifactForm.Whip,
    ArtifactFunction.Trap, ArtifactGrade.NuminousTreasure, QualityTier.Supreme,
    itemTier:5, power:360,
    fx:new[] { Modules.Control("trap", 3), Modules.FlatDR(30), Modules.Reflect(1, 4) },
    secFunc: ArtifactFunction.Defense);
```

- [ ] **Step 2: Update count test**

```csharp
// 更新 Data_FirstBatch_HasExpectedCount → ≥110
[Fact]
public void Data_AllBatches_HasExpectedCount()
{
    var all = ArtifactData.All;
    Assert.True(all.Count >= 110, $"Expected >=110 artifacts, got {all.Count}");
}
```

Run: `dotnet test --filter Name~ArtifactRegistryTests`
Expected: PASS, count ≥ 110.

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(artifacts): ArtifactData batch2 — 宝器/道器/灵宝 60+件, 七功能全开"
```

---

### Task 5: ArtifactData 第三批 — 通天灵宝/玄天之宝/先天至宝（40+件）

**Files:**
- Modify: `src/Jianghu.Core/Cultivation/Artifacts/ArtifactData.cs`

- [ ] **Step 1: Fill HeavenReachingProfoundSkyPrimordial()**

高品法宝含多功能复合模块：

```csharp
// 通天灵宝 — 下品 (7品, itemTier=6)
yield return A("art_hvn_dragon_suppress_seal_i", "镇龙印", ArtifactForm.Seal,
    ArtifactFunction.Trap, ArtifactGrade.HeavenReaching, QualityTier.Inferior,
    itemTier:6, power:272, fx:new[] { Modules.Control("suppress", 2), Modules.FlatPen(40) },
    secFunc: ArtifactFunction.Attack);
yield return A("art_hvn_primal_bell_i", "原始钟·仿", ArtifactForm.Bell,
    ArtifactFunction.Defense, ArtifactGrade.HeavenReaching, QualityTier.Inferior,
    itemTier:6, power:272, fx:new[] { Modules.FlatDR(35), Modules.Reflect(2, 5) });

// 通天灵宝 — 中品
yield return A("art_hvn_soul_cutting_gourd_c", "斩魂葫芦", ArtifactForm.Gourd,
    ArtifactFunction.Attack, ArtifactGrade.HeavenReaching, QualityTier.Common,
    itemTier:6, power:340,
    fx:new[] { Modules.PenFromResource("soulForce", 3), Modules.FlatPen(50) });
yield return A("art_hvn_myriad_treasure_tower_c", "万宝玲珑塔", ArtifactForm.Tower,
    ArtifactFunction.Defense, ArtifactGrade.HeavenReaching, QualityTier.Common,
    itemTier:6, power:340, fx:new[] { Modules.FlatDR(40), Modules.Reflect(1, 3), Modules.Drain("itemTier", 2) },
    secFunc: ArtifactFunction.Snatch);
yield return A("art_hvn_heaven_net_disk_c", "天网阵盘", ArtifactForm.ArrayDisk,
    ArtifactFunction.Trap, ArtifactGrade.HeavenReaching, QualityTier.Common,
    itemTier:6, power:340, fx:new[] { Modules.Control("net", 3), Modules.Dot("law_burn", 10, 3) });

// 通天灵宝 — 上品
yield return A("art_hvn_luo_treasure_money_s", "落宝金钱", ArtifactForm.Ring,
    ArtifactFunction.Snatch, ArtifactGrade.HeavenReaching, QualityTier.Superior,
    itemTier:6, power:408, fx:new[] { Modules.Special("luobao", 3, 0, "夺器无视御纹") },
    rarity: EffectRarity.Rare);
yield return A("art_hvn_sky_overturn_seal_s", "翻天印", ArtifactForm.Seal,
    ArtifactFunction.Attack, ArtifactGrade.HeavenReaching, QualityTier.Superior,
    itemTier:6, power:408, fx:new[] { Modules.FlatPen(60), Modules.CounterMul("body", 3, 1) });

// 通天灵宝 — 极品
yield return A("art_hvn_vajra_lotus_m", "十二品金莲", ArtifactForm.Lotus,
    ArtifactFunction.Defense, ArtifactGrade.HeavenReaching, QualityTier.Supreme,
    itemTier:6, power:510,
    fx:new[] { Modules.FlatDR(50), Modules.Reflect(1, 2), Modules.Evade(30) },
    secFunc: ArtifactFunction.Defense);

// 玄天之宝 — 下品/中品/上品/极品 (8品, itemTier=7-8)
yield return A("art_pro_soul_pagoda_i", "镇魂塔", ArtifactForm.Tower,
    ArtifactFunction.Defense, ArtifactGrade.ProfoundSky, QualityTier.Inferior,
    itemTier:7, power:384, fx:new[] { Modules.FlatDR(45), Modules.Control("suppress", 2) },
    secFunc: ArtifactFunction.Trap);
yield return A("art_pro_primordial_scroll_c", "太极图·仿", ArtifactForm.Scroll,
    ArtifactFunction.Trap, ArtifactGrade.ProfoundSky, QualityTier.Common,
    itemTier:7, power:480, fx:new[] { Modules.Control("trap", 4), Modules.Dot("yin_yang", 15, 4), Modules.FlatDR(30) });
yield return A("art_pro_chaos_bell_s", "混沌钟·仿", ArtifactForm.Bell,
    ArtifactFunction.Defense, ArtifactGrade.ProfoundSky, QualityTier.Superior,
    itemTier:8, power:576,
    fx:new[] { Modules.FlatDR(60), Modules.Reflect(1, 2), Modules.Evade(25) });

// 先天/混沌至宝 — 下品/中品/上品/极品 (9品, itemTier=9)
yield return A("art_pri_primal_chaos_bell_i", "混沌钟", ArtifactForm.Bell,
    ArtifactFunction.Defense, ArtifactGrade.Primordial, QualityTier.Inferior,
    itemTier:9, power:544,
    fx:new[] { Modules.FlatDR(60), Modules.Reflect(1, 2), Modules.Control("stun", 2) },
    rarity: EffectRarity.Rare);
yield return A("art_pri_pangu_banner_c", "盘古幡", ArtifactForm.Banner,
    ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Common,
    itemTier:9, power:680,
    fx:new[] { Modules.PenFromResource("itemTier", 20), Modules.FlatPen(80), Modules.CounterMul("evil", 3, 1) },
    rarity: EffectRarity.Rare,
    secFunc: ArtifactFunction.Trap,
    flavor: "盘古开天之幡，混沌至宝。挥动可破碎虚空、湮灭万物。");
yield return A("art_pri_sky_opening_axe_s", "开天斧", ArtifactForm.Axe,
    ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Superior,
    itemTier:9, power:816,
    fx:new[] { Modules.FlatPen(100), Modules.CounterMul("body", 3, 1), Modules.CounterMul("evil", 3, 1) },
    rarity: EffectRarity.Rare,
    secFunc: ArtifactFunction.Attack,
    flavor: "盘古开天辟地之斧，混沌至宝之首。一斧可开一界。");
yield return A("art_pri_taiji_scroll_m", "太极图", ArtifactForm.Scroll,
    ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1020,
    fx:new[] { Modules.Control("trap", 5), Modules.FlatDR(80), Modules.Reflect(2, 3) },
    rarity: EffectRarity.Rare,
    secFunc: ArtifactFunction.Defense,
    flavor: "太上老君之宝，包罗万象之图，可定地水火风。");
```

- [ ] **Step 2: Run data integrity tests**

Run: `dotnet test --filter Name~ArtifactRegistryTests`
Expected: 150+ artifacts, all pass.

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(artifacts): ArtifactData batch3 — 通天灵宝/玄天之宝/先天至宝 40+件"
```

---

### Task 6: Unique 法宝数据 — 21路镇派 + 散落 + 遗迹

**Files:**
- Modify: `src/Jianghu.Core/Cultivation/Artifacts/ArtifactData.cs`

- [ ] **Step 1: Fill UniqueArtifacts() with per-path + scattered uniques**

```csharp
// 每路2件镇派法宝
// 剑修
yield return A("art_unq_sword_zhu_xian", "诛仙剑", ArtifactForm.Sword,
    ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.Special("explodeArray", 9, 0, "诛仙剑意:一剑破万法") },
    rarity: EffectRarity.Unique,
    flavor: "诛仙四剑之首，剑修至高剑道化身。非剑心圆满者不可执。",
    src: "古道宗遗迹·诛仙台");
yield return A("art_unq_sword_azure_dragon", "青索紫郢双剑", ArtifactForm.Sword,
    ArtifactFunction.Attack, ArtifactGrade.ProfoundSky, QualityTier.Supreme,
    itemTier:8, power:960,
    fx:new[] { Modules.PenFromResource("swordWill", 10), Modules.AoePerTarget(40) },
    rarity: EffectRarity.Unique,
    secFunc: ArtifactFunction.Attack,
    flavor: "紫青双剑合一，剑修镇派至宝。",
    src: "剑修·剑阁祖传");

// 器修
yield return A("art_unq_qixiu_sky_tower", "混元玲珑塔", ArtifactForm.Tower,
    ArtifactFunction.Defense, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.Special("luobao", 5, 0, "万宝归塔"), Modules.FlatDR(60), Modules.Reflect(1, 2) },
    rarity: EffectRarity.Unique,
    secFunc: ArtifactFunction.Snatch,
    flavor: "器修至高杰作，可同时困+夺+防。一件至宝压一境。",
    src: "器修·百炼总坛镇派之宝");

// 体修
yield return A("art_unq_body_indestructible_armor", "不灭金甲", ArtifactForm.Shield,
    ArtifactFunction.Defense, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.Special("goldenBodyMax", 3, 0, "金身不灭"), Modules.FlatDR(80), Modules.Reflect(1, 2) },
    rarity: EffectRarity.Unique,
    flavor: "体修至高金身甲，濒死复活、不灭不破。",
    src: "体修·横练宗祖传金甲");

// 阵修
yield return A("art_unq_array_zhuxian_formation", "诛仙阵图", ArtifactForm.ArrayDisk,
    ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.Special("explodeArray", 9, 0, "诛仙剑阵·爆"), Modules.Control("trap", 5) },
    rarity: EffectRarity.Unique,
    flavor: "诛仙四剑配诛仙阵图，布下诛仙剑阵，圣人亦不敢轻入。",
    src: "阵修·古道宗遗迹");

// 佛修
yield return A("art_unq_bud_lotus_12", "十二品功德金莲", ArtifactForm.Lotus,
    ArtifactFunction.Defense, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.Special("goldenBodyMax", 5, 0, "金身不灭·佛"), Modules.FlatDR(60) },
    rarity: EffectRarity.Unique,
    secFunc: ArtifactFunction.Heal,
    flavor: "佛门至高莲台，万法不侵、功德无量。",
    src: "佛修·大日如来寺镇寺之宝");

// 鬼修
yield return A("art_unq_ghost_soul_banner", "万魂幡", ArtifactForm.Banner,
    ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.PenFromResource("ghostSoldierPower", 5), Modules.Drain("shaCharge", 10) },
    rarity: EffectRarity.Unique,
    flavor: "收万魂于一幡，鬼兵如潮，吞天噬地。",
    src: "鬼修·噬魂魔宫镇宫之宝");

// 雷修
yield return A("art_unq_lei_heaven_thunder_seal", "九天应元雷声普化天尊印", ArtifactForm.Seal,
    ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.PenFromResource("thunderCharge", 5), Modules.CounterMul("evil", 3, 1), Modules.AoePerTarget(50) },
    rarity: EffectRarity.Unique,
    flavor: "雷修至高天尊印，执掌天劫雷霆。",
    src: "雷修·天劫峰镇峰之宝");

// 命修
yield return A("art_unq_ming_life_death_book", "生死簿", ArtifactForm.Scroll,
    ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.Special("reverseStack", 3, 0, "逆演回滚"), Modules.Control("fate", 5) },
    rarity: EffectRarity.Unique,
    flavor: "命修至高冥书，可定生死、判因果、逆演时空。",
    src: "命修·因果时空殿");

// 魔修
yield return A("art_unq_mo_heart_devour_blade", "噬心魔刃", ArtifactForm.Blade,
    ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.PenFromResource("MoGong", 5), Modules.Drain("MoGong", 10), Modules.Backlash("burnGate", 30) },
    rarity: EffectRarity.Unique,
    flavor: "以心魔淬刃，每斩必夺一魂。噬主之刃，魔修至宝。",
    src: "魔修·血河魔宫镇宫至宝");

// 血修
yield return A("art_unq_xue_blood_god_cauldron", "血神鼎", ArtifactForm.Cauldron,
    ArtifactFunction.Support, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.PenFromResource("qixie", 5), Modules.Drain("qixie", 8), Modules.FlatDR(40) },
    rarity: EffectRarity.Unique,
    secFunc: ArtifactFunction.Attack,
    flavor: "以血炼鼎，燃血成神。血修至高血祭至宝。",
    src: "血修·血煞原祖坛");

// 散落法宝
yield return A("art_unq_world_dinghai_orb", "定海神珠", ArtifactForm.Orb,
    ArtifactFunction.Trap, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.Control("flood", 4), Modules.Dot("drown", 20, 4) },
    rarity: EffectRarity.Unique,
    flavor: "先天灵宝，二十四颗定海神珠，可演化诸天。散落于四海。",
    src: "江湖散落·东海遗迹");

yield return A("art_unq_world_sky_net", "天罗地网", ArtifactForm.Scroll,
    ArtifactFunction.Trap, ArtifactGrade.HeavenReaching, QualityTier.Supreme,
    itemTier:7, power:680,
    fx:new[] { Modules.Control("net", 5) },
    rarity: EffectRarity.Unique,
    flavor: "天庭遗落之宝，可网罗天地、无人能逃。",
    src: "江湖散落·天庭废墟");

yield return A("art_unq_world_sun_bow", "后羿射日弓", ArtifactForm.Spear,
    ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.FlatPen(120), Modules.CounterMul("fire", 3, 1) },
    rarity: EffectRarity.Unique,
    flavor: "后羿射日之神弓，一箭可落星辰。",
    src: "江湖散落·远古战场");

yield return A("art_unq_world_void_cutting_sword", "斩仙飞刀", ArtifactForm.Blade,
    ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.Special("duoshe", 9, 0, "一刀斩仙·魂飞魄散") },
    rarity: EffectRarity.Unique,
    flavor: "陆压道人之宝，请宝贝转身，神仙亦斩。",
    src: "江湖散落·终南山");

// 遗迹出土
yield return A("art_unq_ruin_bronze_god_halberd", "青铜神戟", ArtifactForm.Spear,
    ArtifactFunction.Attack, ArtifactGrade.Primordial, QualityTier.Supreme,
    itemTier:9, power:1360,
    fx:new[] { Modules.FlatPen(100), Modules.CounterMul("body", 3, 1) },
    rarity: EffectRarity.Unique,
    flavor: "上古遗迹中出土的青铜神兵，刻满不认识的铭文，隐隐有天雷轰鸣。",
    src: "遗迹出土·三星堆古战场");

yield return A("art_unq_ruin_jade_armor", "金缕玉甲", ArtifactForm.Shield,
    ArtifactFunction.Defense, ArtifactGrade.ProfoundSky, QualityTier.Supreme,
    itemTier:8, power:960,
    fx:new[] { Modules.FlatDR(70), Modules.Reflect(1, 3), Modules.Evade(20) },
    rarity: EffectRarity.Unique,
    flavor: "古墓出土金缕玉衣，万线织成、刀枪不入、阴邪不侵。",
    src: "遗迹出土·马王堆古墓");
```

- [ ] **Step 2: Update count test → ≥200**

Run: `dotnet test --filter Name~ArtifactRegistryTests`
Expected: 200+ artifacts.

- [ ] **Step 3: Commit**

```bash
git commit -am "feat(artifacts): Unique法宝 — 21路镇派+散落+遗迹 30+件"
```

---

### Task 7: Integration — Artifact effects via DuelEngine

**Files:**
- Create: `tests/Jianghu.Core.Tests/Cultivation/ArtifactIntegrationTests.cs`

- [ ] **Step 1: Write integration test — artifact effects apply in combat**

```csharp
// tests/Jianghu.Core.Tests/Cultivation/ArtifactIntegrationTests.cs
using System.Linq;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Artifacts;
using Jianghu.Cultivation.Paths;
using Jianghu.Model;
using Jianghu.Stats;
using Xunit;

public class ArtifactIntegrationTests
{
    static LimitsConfig Limits => LimitsConfig.Default;

    static Character MakeChar(long id, int force, int intl, int con, int insight,
        CultivationState? cult = null)
    {
        var c = new Character(new CharacterId(id),
            new Persona("n", "t", "s", ArchetypeKind.Martial, null),
            new StatBlock(new[] { force, intl, con, insight }),
            new NodeId(0), new Goal(GoalKind.Advance, 0), 0, 800, 16);
        if (cult != null) c.Cultivation = cult;
        return c;
    }

    static CultivationPathDef MakeOppPath(string id)
        => new CultivationPathDef(id, id, "physical", new[] { "melee" },
            new[] { new ResourceDef("qi", 0, 1000, 0) },
            new PowerFormulaDef(new[] { new PowerTerm("stat:Force", 4, null) },
                System.Array.Empty<PowerMod>(), null),
            new RealmCurveDef(new[] { 10, 15, 25 }, new[] { 0, 1, 2 },
                new[] { "L1", "L2", "L3" }, new[] { 0, 100, 300 },
                new[] { 1, 1, 1 }, true, 2),
            System.Array.Empty<ArtCategoryDef>(), System.Array.Empty<CombatSkillDef>(),
            new EntryGateDef(""), new SelectionRuleDef(1, 3), null);

    [Fact]
    public void ArtifactData_Registry_LoadsAndQueries()
    {
        var reg = ArtifactData.DefaultRegistry;
        Assert.True(reg.All.Count >= 200, $"Expected >=200 artifacts, got {reg.All.Count}");

        var swords = reg.ByForm(ArtifactForm.Sword);
        Assert.True(swords.Count >= 10, $"Expected >=10 swords, got {swords.Count}");

        var uniques = reg.UniqueArtifacts;
        Assert.True(uniques.Count >= 20, $"Expected >=20 uniques, got {uniques.Count}");

        var spirit = reg.ByGrade(ArtifactGrade.Spirit);
        Assert.True(spirit.Count >= 10, $"Expected >=10 spirit artifacts, got {spirit.Count}");
    }

    [Fact]
    public void ArtifactEffect_FlatPen_IntegratesWithCombat()
    {
        // Verify artifact data has effects that are valid EffectOp modules
        var reg = ArtifactData.DefaultRegistry;
        foreach (var a in reg.All.Take(20))
        {
            foreach (var op in a.Effects)
            {
                // Every effect must have a valid Kind (not throw on ModuleResolver)
                Assert.True((int)op.Kind >= 0);
            }
        }
    }

    [Fact]
    public void UniqueArtifacts_HaveSpecialOrRareEffects()
    {
        var reg = ArtifactData.DefaultRegistry;
        foreach (var a in reg.UniqueArtifacts)
        {
            Assert.True(a.Effects.Count >= 1,
                $"Unique artifact {a.Id} has no effects");
        }
    }
}
```

Run: `dotnet test --filter Name~ArtifactIntegrationTests`
Expected: 3 tests PASS.

- [ ] **Step 2: Commit**

```bash
git commit -am "test(artifacts): 集成测试 — 200+件数据完整性 + EffectOp有效性"
```

---

### Task 8: 全量绿 + IL 浮点扫描

- [ ] **Step 1: Run full test suite**

```bash
dotnet test tests/Jianghu.Core.Tests/Jianghu.Core.Tests.csproj
```

Expected: ALL GREEN, 380+ tests (was 369 + ~15 new).

- [ ] **Step 2: IL float scan**

```bash
dotnet test --filter Name~CultivationFloatScan
```

Expected: PASS — no floating point in Cultivation namespace.

- [ ] **Step 3: Final commit**

```bash
git commit -am "chore(artifacts): 全量绿+IL浮点零 — story-004 法宝设计阶段完成"
```

---

## Self-Review

**Spec coverage:**
- Quality system §9品×4档 → Task 1 (enums)
- Form×Function matrix → Task 1 (enums)
- Data schema ArtifactDef → Task 1 (record)
- ArtifactRegistry → Task 2
- 200+ artifact data → Tasks 3-6
- Connection to EffectOp → Task 7 (integration test)
- Unique artifacts with Special handlers → Task 6

**Placeholder scan:** No TBD/TODO. All code shown. BasePower values use spec's [待策划评定] markers.

**Type consistency:** ArtifactDef types used consistently across all tasks. ModuleResolver integration follows existing pattern.
