using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 佛修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：摩诃无量佛光 → FlatPen(30)+CounterMul(evil,3)（佛光 anti_evil 对阴邪×3 联合上界钳,真差分）；
    /// 不动明王怒目 = 开金身大成态 goldenBodyMax batch3 Special defer（保 AddPenInteger 占位）。
    /// 注：佛 SituationalTags 用 anti_evil 克制 tag,敌侧阴邪以 evil tag 表达,故 CounterMul 锚 "evil",测试防方给 evil tag。
    /// </summary>
    public class BuddhistModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            BuddhistGoldenBodyPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx(bool defenderEvil)
        {
            var atk = CultivationState.NewForPath("buddhist_golden_body", new List<ResourceDef>());
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var atkPath = BuddhistGoldenBodyPath.Def;
            var defPath = defenderEvil ? TestPaths.WithTags(new[] { "evil" })
                                       : TestPaths.WithTags(System.Array.Empty<string>());
            return new CombatContext(atk, atkPath, def, defPath);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse) dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 摩诃无量佛光：FlatPen(30)+CounterMul(evil)。对 evil anti_evil 倍乘,非 evil 不乘（真差分）——
        [Fact]
        public void FoGuang_CounterMul_VsEvil()
        {
            var sk = Skill("sk_bd_foguang");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.CounterMul && o.Key == "evil");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger); // FlatPen 基线

            int vsEvil = Resolve(sk, 0, Ctx(defenderEvil: true));  // 30→×3 联合上界钳 ×3/2 = 45
            int vsNorm = Resolve(sk, 0, Ctx(defenderEvil: false)); // 30
            Assert.True(vsEvil > vsNorm, "摩诃无量佛光对 evil 未触发 anti_evil 倍乘");
        }

        // —— 不动明王怒目：goldenBodyMax态 batch3 Special defer，仍 AddPenInteger 占位 ——
        [Fact]
        public void BuDongMing_DeferredPlaceholder()
        {
            var sk = Skill("sk_bd_budongming");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger);
            Assert.DoesNotContain(sk.OnUse, o => o.Kind == EffectOpKind.CounterMul);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(BuddhistGoldenBodyPath.Def);
        }
    }
}
