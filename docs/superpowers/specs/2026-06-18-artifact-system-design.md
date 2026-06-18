# 法宝系统设计（Artifact System）

> **Status**: Design Approved
> **Author**: seal + Claude (brainstorming + web-research)
> **Last Updated**: 2026-06-18
> **上游世界观**: WorldBible 九野-canonical + 内容补遗 + 21路深度设计
> **上游系统**: 模块化效果系统 (EffectOp/ModuleResolver)、DuelEngine.ResolveR2、CultivationState
> **取材来源**: 凡人修仙传、遮天、完美世界、洪荒流(封神/西游)、诛仙、缥缈之旅

---

## Overview

把当前仅 7 件命名法宝（Qixiu 藏宝阁）的薄层扩展为 **200+ 件具名法宝数据目录**，经 9 品 × 4 档品质梯度 + 24 形态 × 7 功能双轴分类，接入现有 `EffectOp` 模块系统与 `SpecialModuleRegistry` 唯一档 handler。锻造流程（材料/炼制/器纹）defer FULLSTRUCT。

法宝不只是道具——**数值底 + 配套功法解锁的战斗模块**（承模块化效果系统 §6）。器修为核心持有者，但其他路亦可认主法宝（需御器功法门控）。

---

## Quality System — 9品 × 4档

### 9品阶梯（itemTier 0-9 映射）

| 品阶 | 名称 | itemTier | 对应境界 | BasePower [待策划评定] |
|:----:|------|:--------:|----------|:---------------------:|
| 1 | **凡器** Mortal | 0 | 炼气期 | 10 |
| 2 | **法器** Dharma | 1 | 筑基期 | 30 |
| 3 | **灵器** Spirit | 2 | 金丹期 | 60 |
| 4 | **宝器** Treasure | 3 | 元婴期 | 100 |
| 5 | **道器** DaoWeapon | 4 | 化神期 | 160 |
| 6 | **灵宝** NuminousTreasure | 5 | 炼虚期 | 240 |
| 7 | **通天灵宝** HeavenReaching | 6 | 合体期 | 340 |
| 8 | **玄天之宝** ProfoundSky | 7-8 | 大乘期 | 480 |
| 9 | **先天/混沌至宝** Primordial | 9 | 渡劫/真仙 | 680 |

### 品内4档浮动

| 档位 | 乘子 | 例: 灵器 BasePower=60 |
|:----:|:----:|:--------------------:|
| 下品 Inferior | ×0.8 | 48 |
| 中品 Common | ×1.0 | 60 |
| 上品 Superior | ×1.2 | 72 |
| 极品 Supreme | ×1.5 | 90 |

### 唯一品质（Unique）

- 乘子 ×2.0（基于极品）
- 专属 `Special(handlerId)` handler 效果
- 不受 itemTier `floor(realm*1.2)+1` 硬封顶（已达上限仍可 +1 等效）
- 分布：21路 × 1-3件 + 江湖散落 ~20件 + 遗迹出土 ~15件 ≈ 77件

---

## Form × Function Matrix

### 24 形态（ArtifactForm）

| 大类 | 形态 | enum值 | 典型效果 | 来源 |
|------|------|--------|---------|------|
| 刃兵 | 剑 | Sword | 穿透/破防 | 诛仙·紫青双剑 |
| | 刀 | Blade | 斩杀/流血 | 斩仙飞刀 |
| | 枪/戟 | Spear | 长距/突刺 | 火尖枪 |
| | 针/钉 | Needle | 暗袭/点穴 | 寒铁飞针 |
| 重兵 | 印 | Seal | 镇压/眩晕 | 番天印 |
| | 锤/杵 | Hammer | 粉碎/破甲 | 金刚杵 |
| | 斧 | Axe | 破界/毁阵 | 开天斧 |
| 阵器 | 幡/旗 | Banner | 领域/光环 | 盘古幡 |
| | 阵盘 | ArrayDisk | 布阵/困敌 | 诛仙阵图 |
| | 图/卷 | Scroll | 空间/困杀 | 山河社稷图 |
| 护宝 | 钟 | Bell | 镇守/防魂 | 混沌钟 |
| | 塔 | Tower | 镇压/保护 | 玲珑塔 |
| | 盾/甲 | Shield | 护体/反震 | 不灭金甲 |
| | 莲台 | Lotus | 净化/结界 | 十二品莲台 |
| 奇物 | 珠 | Orb | 元素/克制 | 定海神珠 |
| | 葫芦 | Gourd | 收容/炼化 | 紫金红葫芦 |
| | 镜 | Mirror | 反照/洞察 | 昆仑镜 |
| | 鼎/炉 | Cauldron | 炼制/吞噬 | 乾坤鼎 |
| | 扇 | Fan | 范围/吹飞 | 芭蕉扇 |
| | 索/绳 | Rope | 束缚/擒拿 | 捆仙索 |
| | 环/圈 | Ring | 套取/缴械 | 乾坤圈 |
| 符宝 | 符箓 | Talisman | 一次性爆发 | 符修专精 |
| | 灯 | Lamp | 照明/驱邪 | 宝莲灯 |
| 乐器 | 琴箫 | Instrument | 音攻/控场 | 音修专精 |
| 鞭尺 | 鞭/尺 | Whip | 中距/削甲 | 量天尺 |

### 7 功能轴（ArtifactFunction）

| 功能 | enum值 | EffectOp 映射 |
|------|:------:|--------------|
| 攻 Attack | `atk` | FlatPen, PenFromResource, AoePerTarget, CounterMul |
| 防 Defense | `def` | FlatDR, ReflectDamage, Evade |
| 困 Trap | `trap` | Control, Dot |
| 夺 Snatch | `snatch` | DrainResource, Special(luobao) |
| 辅 Support | `support` | AddResource, GrantPassive, AddTermWeightStep |
| 遁 Escape  | `escape` | Evade, ScalarMul |
| 愈 Heal | `heal` | AddResource(回资源), Dot(驱毒) |

### 覆盖计划（200+件）

```
普通/稀有 (Common/Rare):
  1-3品 × 4档 × 攻/防为主 + 少量遁/辅 ≈ 96件
  4-6品 × 4档 × 七功能全开          ≈ 108件
  7-9品 × 4档 × 多功能复合           ≈ 48件

唯一 (Unique):
  21路×2 + 散落20 + 遗迹15            ≈ 77件

总计: ~329 件设计规格，有效组合约 200-250 件
```

> 不要求每种 形态×功能 全覆盖。只设计有代表性的组合（如"剑"形态不设愈/辅）。

### 高品法宝可有多功能

- 1-3品：单一 PrimaryFunc
- 4-6品：PrimaryFunc + 可选 SecondaryFunc
- 7-9品 + Unique：PrimaryFunc + SecondaryFunc + 特殊 module 效果

### 元素属性（预留）

形态×功能双轴确定后，元素(五行/阴阳/风雷/时空)通过以下未来机制补充：
- 法宝镶嵌（socketing）：灵石/五行珠提供元素属性
- 器纹附魔（enchanting）：器纹学提供元素效果
- 炼制材料（material）：天材地宝自带元素
- **本轮设计不展开**，ArtifactDef 预留 `string? ElementHint` 字段

---

## Data Schema

### ArtifactDef

```csharp
public enum ArtifactGrade { Mortal=1, Dharma=2, Spirit=3, Treasure=4, 
    DaoWeapon=5, NuminousTreasure=6, HeavenReaching=7, ProfoundSky=8, Primordial=9 }
public enum QualityTier { Inferior=1, Common=2, Superior=3, Supreme=4 }
public enum ArtifactForm { Sword, Blade, Spear, Needle, Seal, Hammer, Axe,
    Banner, ArrayDisk, Scroll, Bell, Tower, Shield, Lotus, Orb, Gourd, 
    Mirror, Cauldron, Fan, Rope, Ring, Talisman, Lamp, Instrument, Whip }
public enum ArtifactFunction { Attack, Defense, Trap, Snatch, Support, Escape, Heal }

public sealed record ArtifactDef(
    string Id,                      // "art_sword_azure_dragon"
    string Name,                    // "青索剑"
    ArtifactForm Form,
    ArtifactFunction PrimaryFunc,
    ArtifactFunction? SecondaryFunc,
    ArtifactGrade Grade,
    QualityTier Quality,
    int ItemTier,                   // 0-9, from Grade
    int BasePower,                  // [待策划评定]
    IReadOnlyList<EffectOp> Effects, // 经 ModuleResolver 结算
    EffectRarity Rarity,            // Common / Rare / Unique
    string? ElementHint,            // 预留：元素属性（镶嵌/附魔 未来补）
    string? FlavorText,             // 背景描述
    string? SourceHint              // 获取来源
);
```

### ArtifactRegistry

```csharp
public static class ArtifactRegistry
{
    static readonly Dictionary<string, ArtifactDef> _all;
    static readonly ILookup<ArtifactGrade, ArtifactDef> _byGrade;
    static readonly ILookup<ArtifactForm, ArtifactDef> _byForm;

    public static ArtifactDef Get(string id);
    public static IReadOnlyList<ArtifactDef> All { get; }
    public static IReadOnlyList<ArtifactDef> ByGrade(ArtifactGrade g);
    public static IReadOnlyList<ArtifactDef> ByForm(ArtifactForm f);
    public static IReadOnlyList<ArtifactDef> ByFunction(ArtifactFunction f);
    public static IReadOnlyList<ArtifactDef> UniqueArtifacts { get; }
}
```

### 接入现有系统

```
Character.Equip(ArtifactDef)
  → itemTier += artifact.ItemTier
  → CultivationState.Resources["itemTier"] 更新
  → PowerEngine.Evaluate 自动计入 PE (Qixiu: itemTier×40 主导)
  → DuelEngine.ResolveExchange 遇到 artifact.Effects[] 模块 → ModuleResolver 结算
  → Unique: SpecialModuleRegistry.Get(handlerId).Apply(ctx, op)
  → 功法门控: HasArtifactArt (已有, DuelEngine)
```

---

## File Layout

```
src/Jianghu.Core/Cultivation/Artifacts/
  ArtifactDef.cs              — enums + record (netstandard2.1)
  ArtifactRegistry.cs         — 注册表 + 查询
  ArtifactData.cs             — 200+ 件数据（partial class, 按品分段）
design/artifacts/
  artifact-catalog.md         — 完整目录（名称/来源/背景/flavor）
tests/Jianghu.Core.Tests/Cultivation/
  ArtifactDefTests.cs
  ArtifactRegistryTests.cs
  ArtifactIntegrationTests.cs
```

---

## Edge Cases

- **脱宝**：被落宝/法器损毁 → itemTier 资源减对应量。Qixiu 脱宝 ×0 惩罚由 power 公式(itemTier×40)自然生效。
- **越境持有**：itemTier `floor(realm*1.2)+1` 硬封顶。持有高品法宝但境界不够 → 实际 itemTier 被截断。
- **多宝持有**：Qixiu 御宝心法(gate)决定同驱数量(1-3件)。其他路径无御宝心法则最多 1 件。
- **功法门控**：无御器 arts → 法宝 Effects[] 不解锁（裸数值底，itemTier→PE）。DuelEngine.HasArtifactArt 已落地。
- **同品同型浮动**：下品/中品/上品/极品 4 档确定性数值（非随机），同种子逐字节复现（B.2）。
- **唯一法宝唯一性**：认主后从江湖池移除；被夺/损毁后回到散落池。`IsUnique` 标记防重复获取。

---

## 数值待定（Tuning Knobs）

| 旋钮 | 默认 | 说明 | 状态 |
|------|------|------|:----:|
| BasePower 各品基准 | 见 §Quality | PE 基础贡献量 | [待策划评定] |
| 4档乘子 | 0.8/1.0/1.2/1.5 | 品内数值浮动 | [待策划评定] |
| Unique 乘子 | ×2.0 | 基于极品再翻倍 | [待策划评定] |
| 多宝同驱 itemTier 折扣 | 100%/50%/33% | Qixiu 1/2/3 件 | 已有(御宝心法) |
| 高品 Effects[] 模块数 | 1品0个→9品3-4个 | 越高品模块越丰富 | [待策划评定] |
| Unique 的 Special handler | 每件 1 个 handlerId | 独有效果 | [待策划评定] |

---

## Out of Scope (defer FULLSTRUCT)

- 材料收集/炼制流程（craftScore/matGrade/火候）
- 器纹附魔系统（rune socketing）
- 法宝祭炼进度推进
- 法宝损毁/耐久度
- 元素镶嵌（五行珠/灵石 socket）
- 法宝交易/拍卖经济系统
- 非战斗法宝效果（改四维/造关系边）

---

## Design Decisions Log

1. **9品凡人流**：取材《凡人修仙传》法器→灵器→…→玄天之宝链条，最贴近 itemTier(0-9)现有设计。
2. **形态×功能双轴**：元素轴预留镶嵌/附魔，不过早耦合。
3. **4档确定性浮动**：品内数值变体靠档位而非随机，守红线 B.2 确定性。
4. **ArtifactDef 独立 record**：不入 ArtCategoryDef（那是功法类目），法宝有独立命名空间和查询维度。
5. **认主机制**：最少侵入现有 CultivationState——只改 itemTier 资源数值。
