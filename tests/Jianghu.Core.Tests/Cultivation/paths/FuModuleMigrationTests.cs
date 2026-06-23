using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 符修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：符海齐射 = stockFirepower(derived 非 ResourceDef) → FULLSTRUCT defer（保 FlatPen 占位）；
    /// 血符·焚尽 → FlatPen(40)+Backlash(bloodCast)（以精血催符自损 innerDemon/根骨,selfDmg 通道批4 接,ApplyOnUse 不改 dmg,断言 Kind 在册）。
    /// </summary>
    public class FuModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            FuXiuFuluPath.Def.CombatSkills.Single(s => s.Id == id);

        static CombatContext Ctx()
        {
            var atk = CultivationState.NewForPath("fu_xiu_fulu",
                new[] { new ResourceDef("talismanStore", 0, 12, 0), new ResourceDef("fuPotency", 0, 30, 0) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var path = FuXiuFuluPath.Def;
            return new CombatContext(atk, path, def, path);
        }

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse) dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 符海齐射：FULLSTRUCT defer（stockFirepower derived 非资源），仍 AddPenInteger 占位 ——
        [Fact]
        public void QiShe_DeferredFlatPen()
        {
            var sk = Skill("sk_fu_qishe");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger);
            Assert.DoesNotContain(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource);
        }

        // —— 血符·焚尽：FlatPen(40)+Backlash(bloodCast)。自伤通道批4 接，断言 Kind 在册 ——
        [Fact]
        public void XueFu_IsBacklash()
        {
            var sk = Skill("sk_fu_xuefu");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Backlash && o.Key == "bloodCast");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger); // FlatPen 基线
        }

        // —— 五雷轰顶符：FlatPen(60) 基线破防量 ——
        [Fact]
        public void WuLei_IsFlatPen_DamageCalc()
        {
            var sk = Skill("sk_fu_wulei");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger);
            int dmg = Resolve(sk, 0, Ctx());
            Assert.Equal(60, dmg);
        }

        // —— 护身金光符：FlatDR(18) 减免积木 ——
        [Fact]
        public void HuShen_IsFlatDR()
        {
            var sk = Skill("sk_fu_hushen");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddFlatDR && o.Amount == 18);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(FuXiuFuluPath.Def);
        }
    }
}
