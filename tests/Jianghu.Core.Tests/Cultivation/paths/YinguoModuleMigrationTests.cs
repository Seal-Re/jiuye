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

        // —— 夺定数·截命一击：B5消化 → ModifyEP(destinyAuth,-3,1) + Drain(destinyAuth,1) ——
        [Fact]
        public void JieMing_UpgradedToModifyEP()
        {
            var sk = Skill("sk_yg_jieming");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.ModifyEffectivePower && o.Key == "destinyAuth");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.DrainResource);
        }

        // —— 时光回溯·逆演：B5消化 → Special(reverseStack) handler 已激活 ——
        [Fact]
        public void NiYan_UpgradedToSpecial()
        {
            var sk = Skill("sk_yg_niyan");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Special && o.Key == "reverseStack");
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(YinguoFazePath.Def);
        }
    }
}
