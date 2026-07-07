using System.Collections.Generic;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 修炼路线注册表 schema（spec §5）。全部 record，纯数据，禁浮点。
    /// 数据驱动 + 开闭可扩展：加路线/功法/战技 = 追加数据（L0）。
    /// </summary>

    /// <summary>per-path 专属资源定义（剑意/血气/煞值/浩然…），不进四维（C6）。</summary>
    public sealed record ResourceDef(string Key, int Min, int Cap, int Initial);

    /// <summary>单部功法。Effects = 装配期被动算子（spec §7）。</summary>
    public sealed record ArtDef(
        string Id, string Name, int Tier, string Category,
        IReadOnlyList<EffectOp> Effects);

    /// <summary>命名功法类目（≥3 类，每类 Arts≥4）；Role=daoheart 的类目 A.0 仅装载不结算。</summary>
    public sealed record ArtCategoryDef(
        string Name, string Role, int PickMin, int PickMax,
        IReadOnlyList<ArtDef> Arts);

    /// <summary>战技。OnUse = 战斗期算子；Cost = 资源消耗表（resourceKey→量）。
    /// Damage（cv-003）= 攻击伤害类型（决策⑨.1 标签门控），可选默认 Normal → 21 路现有构造零改动向后兼容。</summary>
    public sealed record CombatSkillDef(
        string Id, string Name, int Tier,
        IReadOnlyList<EffectOp> OnUse,
        IReadOnlyDictionary<string, int> Cost,
        DamageType Damage = DamageType.Normal);

    /// <summary>
    /// 战力公式项。Src ∈ stat:* | realm | sumArtPower | res:&lt;key&gt; | derived:&lt;key&gt;。
    /// 禁 ×0（Weight=0）项（R6）；禁 Src 引用 daoHeart/innerDemon（R3）。
    /// WeightStepKey = 功法/flag 升某 term 权重的台阶键（可空）。
    /// </summary>
    public sealed record PowerTerm(string Src, int Weight, string? WeightStepKey);

    /// <summary>
    /// 战力修正子（有序整数定点 x*Num/Den，每步 clamp）。Kind = ModKind 分派；
    /// When = 条件谓词（可空 = 恒生效）。新 ModKind 登记 L1（R5）。
    /// </summary>
    public sealed record PowerMod(string Kind, string? Key, int Num, int Den, string? When);

    /// <summary>flag 条件后置乘子（整数 Num/Den；WhenFlag 置位时生效，可空 = 恒生效）。</summary>
    public sealed record PostMul(string? WhenFlag, int Num, int Den);

    /// <summary>per-path 声明式整数战力公式（terms 禁 ×0 项 R6；禁引用 daoHeart R3）。</summary>
    public sealed record PowerFormulaDef(
        IReadOnlyList<PowerTerm> Terms,
        IReadOnlyList<PowerMod> Modifiers,
        IReadOnlyList<PostMul>? PostMuls);

    /// <summary>
    /// per-path 战力曲线（spec §9）。四列等长校验属 Task 1.5（M4）。
    /// RealmMultipliers[i] = 第 i 境界倍率（整数，÷10 定点）；UnifiedTierOf[i] = UT0-12 映射；
    /// RealmNames[i] = 境界称谓；RealmThresholds[i] = 升入第 i 境所需修为阈值。
    /// —— A.1 新增（境界稿 §2）：SubLevelCount[major] = 每大境界小境界数（前缀和投影输入，
    /// 长度=大境界数，Σ==flatIndex 数）；CanAscend = 修士 true/武夫 false；MaxMajor = 最高大境界索引。
    /// 新三列不进运行期（PowerEngine/NextIndexIfReady 不读），仅供 schema 校验/投影/查询。
    /// </summary>
    public sealed record RealmCurveDef(
        IReadOnlyList<int> RealmMultipliers,
        IReadOnlyList<int> UnifiedTierOf,
        IReadOnlyList<string> RealmNames,
        IReadOnlyList<int> RealmThresholds,
        IReadOnlyList<int> SubLevelCount,
        bool CanAscend,
        int MaxMajor);

    /// <summary>
    /// 入门闸（纯查询谓词，读 Persona.Tags）。<see cref="Pred"/> = 小谓词 DSL：
    /// '&amp;' 连接多个原子（全 AND），原子形如 <c>tag:X</c>（角色 Tags 须含 X）。空谓词=恒真。
    /// </summary>
    public sealed record EntryGateDef(string Pred)
    {
        /// <summary>纯查询：角色 tags 是否满足本闸谓词（spec §10，定路前筛路）。不消费随机。</summary>
        public bool CanEnter(IReadOnlyList<string> tags)
        {
            foreach (var raw in Pred.Split('&'))
            {
                var atom = raw.Trim();
                if (atom.Length == 0) continue;
                if (!atom.StartsWith("tag:", System.StringComparison.Ordinal))
                    throw new System.ArgumentException($"未知 EntryGate 谓词原子: {atom}");
                var want = atom.Substring("tag:".Length);
                if (!Has(tags, want)) return false;
            }
            return true;
        }

        private static bool Has(IReadOnlyList<string> tags, string want)
        {
            foreach (var t in tags)
                if (t == want) return true;
            return false;
        }

        /// <summary>
        /// 列出本闸所需的全部 tag（谓词原子 <c>tag:X</c> 的 X 集，按声明序）。空谓词=空表。
        /// 供生成期从注册表派生灵根 tag 池（Phase2 #6 缺口修：加路自动可定）。不消费随机。
        /// </summary>
        public IReadOnlyList<string> RequiredTags()
        {
            var tags = new List<string>();
            foreach (var raw in Pred.Split('&'))
            {
                var atom = raw.Trim();
                if (atom.Length == 0) continue;
                if (!atom.StartsWith("tag:", System.StringComparison.Ordinal))
                    throw new System.ArgumentException($"未知 EntryGate 谓词原子: {atom}");
                tags.Add(atom.Substring("tag:".Length));
            }
            return tags;
        }
    }

    /// <summary>选功法/战技规则（战技抽取 [Min,Max]）。</summary>
    public sealed record SelectionRuleDef(int SkillPickMin, int SkillPickMax);

    /// <summary>
    /// 一条修炼路线全量定义（spec §5）。PathId = canon 全名 key（R4）。
    /// AttackDimension 仅 flavor 分类，不做硬克制（R2）。
    /// SituationalTags = 属性/形态 tag（melee/ranged/spirit_attack/fire…，非对手 PathId）。
    /// ArtCategories 含 1 个 Role=daoheart 类目（A.2 用，A.0 仅装载不结算）。
    /// </summary>
    public sealed record CultivationPathDef(
        string PathId, string Name,
        string AttackDimension,
        IReadOnlyList<string> SituationalTags,
        IReadOnlyList<ResourceDef> Resources,
        PowerFormulaDef Power,
        RealmCurveDef Curve,
        IReadOnlyList<ArtCategoryDef> ArtCategories,
        IReadOnlyList<CombatSkillDef> CombatSkills,
        EntryGateDef EntryGate,
        SelectionRuleDef Selection,
        IReadOnlyDictionary<string, int>? Tuning);
}
