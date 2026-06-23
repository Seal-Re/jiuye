using System.Collections.Generic;
using System.Linq;
using Jianghu.Cultivation;
using Jianghu.Cultivation.Paths;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation.Paths
{
    /// <summary>
    /// B5 批2 音修招牌招 Note→Modules 工厂 差分测试（§10 第1+2条）。
    /// 映射(§15.9/plan)：裂石穿云·杀音爆 → PenFromResource(qiYun,1)（悟性×2+内力×1+qiYun 绕物防,乐韵越满越痛、见底哑火真差分）；
    /// 高山流水·遏邪奏 → FlatPen(16)+CounterMul(evil,3,2)（正乐破邪对阴邪×3/2,真差分）；
    /// 起调·点律 = 律场总门 fieldActive batch3 Special defer（保 AddPenInteger(0)/GrantPassive 占位）。
    /// </summary>
    public class YinModuleMigrationTests
    {
        static CombatSkillDef Skill(string id) =>
            YinXiuYuedaoPath.Def.CombatSkills.Single(s => s.Id == id);

        // 攻方音修持 qiYun；防方 evil/非evil 取 TestPaths.WithTags（CounterMul 读防方 tag）。
        static CombatContext Ctx(int qiYun, bool defenderEvil)
        {
            var atk = CultivationState.NewForPath("yin_xiu_yuedao",
                new[] { new ResourceDef("qiYun", 0, 1000, qiYun) });
            var def = CultivationState.NewForPath("def_path", new List<ResourceDef>());
            var atkPath = YinXiuYuedaoPath.Def;
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

        // —— 裂石穿云：PenFromResource(qiYun,1)。乐韵满载单点越痛（真差分）——
        [Fact]
        public void LieShi_ScalesWithQiYun()
        {
            var sk = Skill("sk_yin_lieshi");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.PenFromResource && o.Key == "qiYun");

            int full = Resolve(sk, 0, Ctx(qiYun: 24, defenderEvil: false)); // 24×1 = 24
            int empty = Resolve(sk, 0, Ctx(qiYun: 0, defenderEvil: false)); // 0
            Assert.Equal(24, full);
            Assert.Equal(0, empty);
            Assert.True(full > empty, "裂石穿云未随 qiYun 缩放（仍是占位定值）");
        }

        // —— 高山流水：FlatPen(16)+CounterMul(evil,3/2)。对 evil 破邪倍乘,非 evil 不乘（真差分）——
        [Fact]
        public void GaoShan_CounterMul_VsEvil()
        {
            var sk = Skill("sk_yin_gaoshan");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.CounterMul && o.Key == "evil");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger); // FlatPen 基线

            int vsEvil = Resolve(sk, 0, Ctx(qiYun: 0, defenderEvil: true));  // 16→×3/2 = 24
            int vsNorm = Resolve(sk, 0, Ctx(qiYun: 0, defenderEvil: false)); // 16
            Assert.True(vsEvil > vsNorm, "高山流水·遏邪奏对 evil 未触发正乐破邪倍乘");
        }

        // —— 起调·点律：律场总门 batch3 Special defer，仍 AddPenInteger(0) 占位 ——
        [Fact]
        public void QiDiao_DeferredPlaceholder()
        {
            var sk = Skill("sk_yin_qidiao");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger);
        }

        // —— 迷魂引：Control(mihun,1) 控场1回合——
        [Fact]
        public void MiHun_IsControl()
        {
            var sk = Skill("sk_yin_mihun");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Control && o.Key == "mihun");
        }

        // —— 万籁齐鸣·镇场：FlatPen(12) 全律场共振——
        [Fact]
        public void WanLai_IsFlatPen()
        {
            var sk = Skill("sk_yin_wanlai");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.AddPenInteger && o.Amount == 12);
        }

        // —— 乐韵遁形·闪：Evade(28) 乐韵遁形闪避 28% 减免——
        [Fact]
        public void YueDun_IsEvade()
        {
            var sk = Skill("sk_yin_yuedun");
            Assert.Contains(sk.OnUse, o => o.Kind == EffectOpKind.Evade && o.Amount == 28);
        }

        [Fact]
        public void Def_StillValidAfterMigration()
        {
            PathValidator.AssertValid(YinXiuYuedaoPath.Def);
        }
    }
}
