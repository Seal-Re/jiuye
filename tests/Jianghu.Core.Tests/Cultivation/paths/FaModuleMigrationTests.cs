using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 Task2.3：法修招牌招 Note→结构化模块 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：五雷正法 → AddPenInteger(28基线)+CounterMul(evil,3/2)（破阴邪:对evil敌×3/2,非evil不变）；
    /// 万剑诀·御物千刃 → PenFromResource(spellBreadth,2)（法术库越广御物群攻越强,真差分）。
    /// 「装备 vs 剥离」差异：五雷对 evil/非evil 结果不同；万剑随 spellBreadth 缩放。
    /// </summary>
    public class FaModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            FaXiuPath.Def.CombatSkills.Single(s => s.Id == id);

        // 攻方法修持指定 spellBreadth；防方可带 evil tag（验 CounterMul 克制）。
        static CombatContext Ctx(int spellBreadth = 0, bool defenderEvil = false)
        {
            var atk = CultivationState.NewForPath("fa_xiu",
                new[] { new ResourceDef("spellBreadth", 0, 100, spellBreadth) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var atkPath = FaXiuPath.Def;
            var defPath = defenderEvil
                ? TestPaths.WithTags(new[] { "evil" })
                : TestPaths.WithTags(System.Array.Empty<string>());
            return new CombatContext(atk, atkPath, def, defPath);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse)
                dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 五雷正法：CounterMul(evil) 破阴邪。对 evil 敌 ×3/2,非 evil 不变（真差分）——
        [Fact]
        public void WuLei_CounterMul_VsEvil()
        {
            var sk = Skill("sk_fa_wulei");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.CounterMul && o.Key == "evil");

            int vsEvil = Resolve(sk, 0, Ctx(defenderEvil: true));   // 基线28 → ×3/2 = 42
            int vsNorm = Resolve(sk, 0, Ctx(defenderEvil: false));  // 基线28 不乘 = 28
            Assert.True(vsEvil > vsNorm, "五雷正法对 evil 未触发克制倍乘");
            Assert.Equal(28, vsNorm);
            Assert.Equal(28 * 3 / 2, vsEvil);
        }

        // —— 万剑诀·御物千刃：PenFromResource(spellBreadth,2)。法术库越广伤越高 ——
        [Fact]
        public void WanJianJue_ScalesWithSpellBreadth()
        {
            var sk = Skill("sk_fa_wanjian");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "spellBreadth");

            int broad = Resolve(sk, 0, Ctx(spellBreadth: 4)); // 12基线 + 4×2 = 20
            int narrow = Resolve(sk, 0, Ctx(spellBreadth: 0)); // 12基线 + 0 = 12
            Assert.True(broad > narrow, "万剑诀未随 spellBreadth 缩放");
            Assert.Equal(12, narrow);
            Assert.Equal(12 + 4 * 2 / 1, broad);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(FaXiuPath.Def);
        }
    }
}
