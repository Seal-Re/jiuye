using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Jianghu.Cultivation
{
    /// <summary>
    /// 路线注册表（spec §3）：从 <see cref="IPathSource"/> 加载 + by-id 索引。
    /// </summary>
    public sealed class PathRegistry
    {
        private readonly List<CultivationPathDef> _all;
        private readonly Dictionary<string, CultivationPathDef> _byId;

        public PathRegistry(IPathSource source)
        {
            _all = new List<CultivationPathDef>(source.Load());
            _byId = new Dictionary<string, CultivationPathDef>();
            foreach (var p in _all)
                _byId[p.PathId] = p;
        }

        public IReadOnlyList<CultivationPathDef> All => _all;

        public CultivationPathDef ById(string pathId) => _byId[pathId];

        /// <summary>
        /// 生成期灵根 tag 池（Phase2 #6 缺口修）：= 全注册路 EntryGate 所需 tag 的并集，
        /// **排序去重**（确定性，Ordinal 升序）。加路 → 新路 gate tag 自动入池 → 自动可被定路。
        /// 散修不碰此池；off 不构造注册表故不调用。
        /// </summary>
        public IReadOnlyList<string> RootTagPool()
        {
            var set = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var p in _all)
                foreach (var tag in p.EntryGate.RequiredTags())
                    set.Add(tag);
            return new List<string>(set);
        }
    }

    /// <summary>
    /// 路线数据校验门（spec §12）。违者抛 <see cref="InvalidOperationException"/>。
    /// 落实 R2（tag 非 PathId）/ R3（无 daoHeart term）/ R4（canon 全名）/ R6（无 ×0 term）/
    /// M1（含 daoheart 类目）/ M4（RealmCurve 四列等长）。
    /// </summary>
    public static class PathValidator
    {
        /// <summary>已知 21 路 canon pathId（R2 denylist：SituationalTags 不得等于其中任一）。</summary>
        public static readonly HashSet<string> KnownPathIds = new HashSet<string>
        {
            "sword_immortal", "ti_xiu_hengshi", "fa_xiu", "array_formation",
            "qixiu_artificer", "soul_divine_sense", "ming_fate_causality", "dan_xiu",
            "gui_xiu_yang_hun", "buddhist_golden_body", "lei_xiu", "yu_shou",
            "ru_xiu_haoran", "mo_xiu_xinmo", "yao_xiu_huaxing", "xue_xiu_xuesha",
            "du_gu_xiu", "fu_xiu_fulu", "kuilei_shi", "yin_xiu_yuedao", "yinguo_faze",
        };

        // R4：canon 全名格式（小写起首，仅小写字母与下划线）。
        private static readonly Regex CanonPathId = new Regex("^[a-z][a-z_]+$", RegexOptions.Compiled);

        public static void AssertValid(CultivationPathDef def)
        {
            // —— R4：PathId canon 全名（非空，^[a-z][a-z_]+$，含下划线，禁短名）——
            if (string.IsNullOrEmpty(def.PathId) || !CanonPathId.IsMatch(def.PathId)
                || def.PathId.IndexOf('_') < 0)
                throw new InvalidOperationException($"PathId 非 canon 全名（R4）: '{def.PathId}'");

            // —— §12：ArtCategories ≥3 类，每类 Arts ≥4；含 ≥1 个 Role==daoheart 类目（M1）——
            if (def.ArtCategories.Count < 3)
                throw new InvalidOperationException($"ArtCategories <3（{def.ArtCategories.Count}）: {def.PathId}");
            bool hasDaoheart = false;
            foreach (var cat in def.ArtCategories)
            {
                if (cat.Arts.Count < 4)
                    throw new InvalidOperationException($"类目 '{cat.Name}' Arts <4（{cat.Arts.Count}）: {def.PathId}");
                if (cat.Role == "daoheart") hasDaoheart = true;
            }
            if (!hasDaoheart)
                throw new InvalidOperationException($"缺 Role==daoheart 类目（M1）: {def.PathId}");

            // —— §12：CombatSkills ≥5 ——
            if (def.CombatSkills.Count < 5)
                throw new InvalidOperationException($"CombatSkills <5（{def.CombatSkills.Count}）: {def.PathId}");

            // —— §12/R2：SituationalTags 非空，且无 tag 等于已知 21 路 pathId ——
            if (def.SituationalTags.Count == 0)
                throw new InvalidOperationException($"SituationalTags 空: {def.PathId}");
            foreach (var tag in def.SituationalTags)
            {
                if (KnownPathIds.Contains(tag))
                    throw new InvalidOperationException($"SituationalTag '{tag}' 等于已知 pathId（R2 tag 非对手路线身份）: {def.PathId}");
            }

            // —— R6/R3：PowerFormula.Terms 无 ×0 项；无 Src 含 daoHeart/innerDemon ——
            foreach (var term in def.Power.Terms)
            {
                if (term.Weight == 0)
                    throw new InvalidOperationException($"PowerTerm '{term.Src}' Weight==0（R6 禁 ×0 项）: {def.PathId}");
                if (term.Src.IndexOf("daoHeart", StringComparison.Ordinal) >= 0
                    || term.Src.IndexOf("innerDemon", StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException($"PowerTerm Src '{term.Src}' 含 daoHeart/innerDemon（R3 道心解耦）: {def.PathId}");
            }

            // —— M4：RealmCurve 四列等长 ——
            RealmCurve.Validate(def.Curve);

            // —— §15.6：遍历本路所有模块算子，ratio-Kind 须 Amount2≥1（消 Amount2=0 双义）——
            foreach (var cat in def.ArtCategories)
                foreach (var art in cat.Arts)
                    foreach (var op in art.Effects)
                        AssertModuleValid(op);
            foreach (var skill in def.CombatSkills)
                foreach (var op in skill.OnUse)
                    AssertModuleValid(op);
        }

        // ratio 类 Kind: 用 Amount/Amount2 整数除, Amount2 必须 ≥1 (§15.6 消双义)
        static readonly EffectOpKind[] RatioKinds =
            { EffectOpKind.PenFromResource, EffectOpKind.CounterMul, EffectOpKind.ReflectDamage };

        public static void AssertModuleValid(EffectOp op)
        {
            if (System.Array.IndexOf(RatioKinds, op.Kind) >= 0 && op.Amount2 < 1)
                throw new System.InvalidOperationException(
                    $"ratio-Kind {op.Kind} 须 Amount2≥1(消Amount2=0双义,§15.6); Key={op.Key}");
        }
    }
}
