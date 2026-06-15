using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 因果时空命运修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：须弥困界 → Control(voidPrison,1)（时空囚困一回合,ApplyOnUse 不改 dmg,批4 结算,断言 Kind+Key 在册）；
    /// 夺定数·截命一击 = 削敌 EffectivePower% → FULLSTRUCT defer（EP% 通道未建,保 FlatPen 占位）；
    /// 时光回溯·逆演 = KarmicIndex 栈回溯 batch3 Special defer（保 AddPenInteger(0) 占位）。
    /// </summary>
    public class YinguoModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            YinguoFazePath.Def.CombatSkills.Single(s => s.Id == id);

        static int Resolve(CombatSkillDef sk, int baseDmg, CombatContext ctx)
        {
            int dmg = baseDmg;
            foreach (var op in sk.OnUse) dmg = ModuleResolver.ApplyOnUse(dmg, op, ctx);
            return dmg;
        }

        // —— 须弥困界：Control(voidPrison)。时空囚困控场积木 ——
        [Fact]
        public void KunJie_IsControl()
        {
            var sk = Skill("sk_yg_kunjie");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Control && o.Key == "voidPrison");
            Assert.DoesNotContain(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger);
        }

        // —— 夺定数·截命一击：FULLSTRUCT defer（削 EP%），仍 AddPenInteger 占位 ——
        [Fact]
        public void JieMing_DeferredFlatPen()
        {
            var sk = Skill("sk_yg_jieming");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger);
            Assert.DoesNotContain(sk.OnUse, o => o.Kind == EffectOpKind.DrainResource);
            Assert.DoesNotContain(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource);
        }

        // —— 时光回溯·逆演：batch3 Special defer，仍 AddPenInteger(0) 占位 ——
        [Fact]
        public void NiYan_DeferredPlaceholder()
        {
            var sk = Skill("sk_yg_niyan");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(YinguoFazePath.Def);
        }
    }
}
