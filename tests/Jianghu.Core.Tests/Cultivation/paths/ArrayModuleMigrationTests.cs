using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 阵修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：困龙·锁 → Control（锁定身,控场结算批4接）；
    /// 炸阵(引爆·焚阵)/Σ阵(算尽·叠杀) → 批3 Special / derived 聚合（本批标注 deferred）。
    /// </summary>
    public class ArrayModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            ArrayFormationPath.Def.CombatSkills.Single(s => s.Id == id);

        // —— 困龙·锁：Control（控场积木）——
        [Fact]
        public void KunLong_IsControl()
        {
            var sk = Skill("sk_ar_kunlong");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Control);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(ArrayFormationPath.Def);
        }
    }
}
