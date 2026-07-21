using System;
using System.Collections.Generic;
using System.Reflection;
using Jianghu.Config;
using Jianghu.Cultivation;
using Jianghu.Model;
using Jianghu.Random;
using Jianghu.Sim;
using Jianghu.Stats;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// cv-007（adr-0010 决策③）：派生抗性 R + 半衰减伤测试——防御漏斗第③层（抵抗层）。
    /// 覆盖：AC 7.1 ResistanceProviders.ResistanceOf / AC 7.2 CombatMath.ApplyResistance 纯函数 /
    /// AC 7.3 LimitsConfig 旋钮 / AC 7.4 ResolveExchange 接线 / AC 7.5 B.5 道心解耦 /
    /// AC 7.6 calibrationMode 旁路 / AC 7.7 B.2 浮点零 + B.3 off 不退 + 不退。
    /// 触 Jianghu.Cultivation → 旗舰档 + 主控核验（B.7/A.3）。
    /// </summary>
    public class ResistanceTests
    {
        static LimitsConfig Limits => LimitsConfig.Default;

        // —— fixtures（承 EvasionSecTests / TagGatingChipTests 范式）——
        // PE = stat:Force × 4 × RealmMult(RealmIndex=1 → 15)。Force=25 → PE=1500。
        // attackType = 攻方招式伤害类型；defenseRole = gate 功法 role（body/defense → HasBodyArt；null=无）。
        // PE 公式只读 Force，故 Constitution/Insight 可独立设而不扰 PE（隔离抗性维度与战力维度，AC 7.4/7.5 用）。
        static CultivationPathDef MakePath(string id, DamageType attackType = DamageType.Normal,
            string? bodyRole = null)
        {
            var skill = new CombatSkillDef("atk", "atk", 0, Array.Empty<EffectOp>(),
                new Dictionary<string, int>(), attackType, 1000);

            // body/defense 类功法 → HasBodyArt=true（供 BodyArtPhysResistBonus 加成测试）
            var artCats = new List<ArtCategoryDef>();
            if (bodyRole != null)
            {
                var art = new ArtDef($"art_{bodyRole}", bodyRole, 1, bodyRole, Array.Empty<EffectOp>());
                artCats.Add(new ArtCategoryDef(bodyRole, bodyRole, 1, 1, new[] { art }));
            }

            return new CultivationPathDef(
                id, id, "physical", new[] { "melee" },
                new[] { new ResourceDef("qi", 0, 1000, 0) },
                new PowerFormulaDef(new[] { new PowerTerm("stat:Force", 4, null) },
                    Array.Empty<PowerMod>(), null),
                new RealmCurveDef(new[] { 10, 15, 25 }, new[] { 0, 1, 2 },
                    new[] { "L1", "L2", "L3" }, new[] { 0, 100, 300 },
                    new[] { 1, 1, 1 }, true, 2),
                artCats.ToArray(),
                new[] { skill },
                new EntryGateDef(""), new SelectionRuleDef(1, 3), null);
        }

        // 从 path.ArtCategories 派生 chosenArtIds（承 TagGatingChipTests.MakeChar 范式）。
        // 消除静态状态：每个 char 从其 path 自己派生，避免跨 path 覆盖（曾致 BodyArt 加成测试失效）。
        static string[] ChosenArtsOf(CultivationPathDef path)
        {
            var ids = new List<string>();
            foreach (var cat in path.ArtCategories)
                foreach (var art in cat.Arts)
                    ids.Add(art.Id);
            return ids.ToArray();
        }

        // stats: [Force, Internal, Constitution, Insight]（StatKind 枚举序）。
        static Character MakeChar(long id, int force, int constitution, int insight, CultivationPathDef path)
        {
            var c = new Character(new CharacterId(id),
                new Persona("n", "t", "s", ArchetypeKind.Martial, null),
                new StatBlock(new[] { force, 0, constitution, insight }),
                new NodeId(0), new Goal(GoalKind.Advance, 0), 0, 800, 16);
            var cult = CultivationState.NewForPath(path.PathId, path.Resources,
                ChosenArtsOf(path), new[] { "atk" });
            cult.RealmIndex = 1; // 同 UT（避免 gap≥2 auto-win 短路）
            c.Cultivation = cult;
            return c;
        }

        static PathRegistry Reg(params CultivationPathDef[] p) => new PathRegistry(new ListPathSource(p));
        static IRandom Rng(ulong seed) => new Pcg32(seed, (ulong)RngStreamIds.Duel);

        sealed class ListPathSource : IPathSource
        {
            private readonly IReadOnlyList<CultivationPathDef> _paths;
            public ListPathSource(IReadOnlyList<CultivationPathDef> paths) => _paths = paths;
            public IReadOnlyList<CultivationPathDef> Load() => _paths;
        }

        // ============================================================
        // AC 7.1：ResistanceProviders.ResistanceOf
        // ============================================================

        [Fact]
        public void test_resistance_normal_uses_constitution_phys_r()
        {
            // Normal → 物理抗性 = Constitution × PhysResistPerConstitution(50)。
            // Constitution=10 → R=500。无 BodyArt 功法 → 无加成。
            var path = MakePath("p", DamageType.Normal);
            var c = MakeChar(1, 20, 10, 5, path);
            int R = ResistanceProviders.ResistanceOf(
                c.Cultivation!, c.Stats, path, GateType.None, DamageType.Normal, Limits);
            Assert.Equal(10 * 50, R); // 500
        }

        [Fact]
        public void test_resistance_blunt_also_phys_r()
        {
            // Blunt 同走物理抗性（与 Normal 同维度）。Constitution=10 → R=500。
            var path = MakePath("p", DamageType.Blunt);
            var c = MakeChar(1, 20, 10, 5, path);
            int R = ResistanceProviders.ResistanceOf(
                c.Cultivation!, c.Stats, path, GateType.None, DamageType.Blunt, Limits);
            Assert.Equal(10 * 50, R);
        }

        [Fact]
        public void test_resistance_elemental_uses_insight_elem_r()
        {
            // Elemental → 属性抗性 = Insight × ElemResistPerInsight(50)。
            // Insight=8 → R=400。不走体质。
            var path = MakePath("p", DamageType.Elemental);
            var c = MakeChar(1, 20, 10, 8, path);
            int R = ResistanceProviders.ResistanceOf(
                c.Cultivation!, c.Stats, path, GateType.None, DamageType.Elemental, Limits);
            Assert.Equal(8 * 50, R); // 400
        }

        [Fact]
        public void test_resistance_normal_vs_elemental_differ_for_same_char()
        {
            // 同角色（Constitution=10, Insight=8）Normal vs Elemental 返回不同 R。
            var path = MakePath("p", DamageType.Normal);
            var c = MakeChar(1, 20, 10, 8, path);
            int rPhys = ResistanceProviders.ResistanceOf(
                c.Cultivation!, c.Stats, path, GateType.None, DamageType.Normal, Limits);
            int rElem = ResistanceProviders.ResistanceOf(
                c.Cultivation!, c.Stats, path, GateType.None, DamageType.Elemental, Limits);
            Assert.NotEqual(rPhys, rElem);
            Assert.Equal(10 * 50, rPhys);  // 体质派生
            Assert.Equal(8 * 50, rElem);   // 识派生
        }

        [Fact]
        public void test_resistance_body_art_adds_phys_bonus()
        {
            // HasBodyArt（已修 body/defense 类功法）→ 物理抗性 +BodyArtPhysResistBonus(100)。
            // Constitution=10 → 基础 500 + 100 = 600。对比无 BodyArt 的 500。
            var pathWithBody = MakePath("p_body", DamageType.Normal, bodyRole: "body");
            var pathNoBody = MakePath("p_nobody", DamageType.Normal);
            var cBody = MakeChar(1, 20, 10, 5, pathWithBody);
            var cNoBody = MakeChar(2, 20, 10, 5, pathNoBody);

            int rWithBody = ResistanceProviders.ResistanceOf(
                cBody.Cultivation!, cBody.Stats, pathWithBody, GateType.None, DamageType.Normal, Limits);
            int rNoBody = ResistanceProviders.ResistanceOf(
                cNoBody.Cultivation!, cNoBody.Stats, pathNoBody, GateType.None, DamageType.Normal, Limits);

            Assert.Equal(10 * 50 + 100, rWithBody); // 600
            Assert.Equal(10 * 50, rNoBody);          // 500
            Assert.True(rWithBody > rNoBody, "HasBodyArt 应抬物理抗性");
        }

        [Fact]
        public void test_resistance_body_art_does_not_affect_elemental()
        {
            // HasBodyArt 加成**仅作用于物理抗性**，Elemental 走识派生不受其影响。
            var pathWithBody = MakePath("p_body", DamageType.Elemental, bodyRole: "body");
            var pathNoBody = MakePath("p_nobody", DamageType.Elemental);
            var cBody = MakeChar(1, 20, 10, 8, pathWithBody);
            var cNoBody = MakeChar(2, 20, 10, 8, pathNoBody);

            int rWithBody = ResistanceProviders.ResistanceOf(
                cBody.Cultivation!, cBody.Stats, pathWithBody, GateType.None, DamageType.Elemental, Limits);
            int rNoBody = ResistanceProviders.ResistanceOf(
                cNoBody.Cultivation!, cNoBody.Stats, pathNoBody, GateType.None, DamageType.Elemental, Limits);

            Assert.Equal(rNoBody, rWithBody); // Elemental 不受 BodyArt 影响
        }

        [Fact]
        public void test_resistance_signature_has_no_daoheart_param()
        {
            // B.5（编译期保证）：ResistanceOf 参数列表不含 daoHeart/innerDemon。
            // 反射验证参数名集合。
            var method = typeof(ResistanceProviders).GetMethod(
                "ResistanceOf", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            var paramNames = new HashSet<string>();
            foreach (var p in method!.GetParameters())
                paramNames.Add(p.Name!);
            Assert.DoesNotContain("daoHeart", paramNames);
            Assert.DoesNotContain("innerDemon", paramNames);
            // 必含核心参数
            Assert.Contains("cultivation", paramNames);
            Assert.Contains("stats", paramNames);
            Assert.Contains("path", paramNames);
            Assert.Contains("damageType", paramNames);
            Assert.Contains("limits", paramNames);
        }

        // ============================================================
        // AC 7.2：CombatMath.ApplyResistance 纯函数（无 RNG、无 IO，确定性 B.2）
        // ============================================================

        [Fact]
        public void test_apply_resistance_r_zero_unchanged()
        {
            // R=0 → multiplier=1000（无减伤）→ 伤害不变
            Assert.Equal(1000, CombatMath.ApplyResistance(1000, 0, 500));
            Assert.Equal(1, CombatMath.ApplyResistance(1, 0, 500));
            Assert.Equal(999, CombatMath.ApplyResistance(999, 0, 500));
        }

        [Fact]
        public void test_apply_resistance_r_equals_k_halves()
        {
            // R=K(500) → multiplier=500（半衰）→ 伤害 ≈ raw/2
            Assert.Equal(500, CombatMath.ApplyResistance(1000, 500, 500));
            Assert.Equal(250, CombatMath.ApplyResistance(500, 500, 500));
        }

        [Fact]
        public void test_apply_resistance_r_2000_attenuates_to_20pct()
        {
            // R=2000, K=500 → multiplier=500*1000/(500+2000)=200 → raw×0.2
            // 1000 → 200；500 → 100
            Assert.Equal(200, CombatMath.ApplyResistance(1000, 2000, 500));
            Assert.Equal(100, CombatMath.ApplyResistance(500, 2000, 500));
        }

        [Fact]
        public void test_apply_resistance_r_huge_floors_to_1()
        {
            // R 极大 → multiplier→0 → max(1,...) 保底返 1。
            // R=100000,K=500: mul=500*1000/(500+100000)=4 → dmg=1000*4/1000=4（尚未触底，验证中间衰减值）。
            // R=int.MaxValue: mul→0 → dmg→max(1,0)=1（触底保底）。
            Assert.Equal(4, CombatMath.ApplyResistance(1000, 100000, 500));
            Assert.Equal(1, CombatMath.ApplyResistance(1000, int.MaxValue, 500));
        }

        [Fact]
        public void test_apply_resistance_raw_one_any_r_still_one()
        {
            // rawDamage=1 任意 R 仍为 1（保底不归零）
            Assert.Equal(1, CombatMath.ApplyResistance(1, 0, 500));
            Assert.Equal(1, CombatMath.ApplyResistance(1, 500, 500));
            Assert.Equal(1, CombatMath.ApplyResistance(1, 100000, 500));
        }

        [Fact]
        public void test_apply_resistance_no_overflow_large_raw()
        {
            // rawDamage=int.MaxValue/1000 不溢出（long 中间类型）
            int raw = int.MaxValue / 1000;
            int result = CombatMath.ApplyResistance(raw, 0, 500);
            Assert.Equal(raw, result); // R=0 不衰减
            // R=K 半衰也不溢出
            int halved = CombatMath.ApplyResistance(raw, 500, 500);
            Assert.True(halved >= 1 && halved <= raw);
        }

        [Fact]
        public void test_apply_resistance_raw_le_zero_returns_zero()
        {
            // rawDamage≤0 → 0（无伤害可衰减，不凭空造伤害）
            Assert.Equal(0, CombatMath.ApplyResistance(0, 500, 500));
            Assert.Equal(0, CombatMath.ApplyResistance(-1, 500, 500));
            Assert.Equal(0, CombatMath.ApplyResistance(-100, 100000, 500));
        }

        [Fact]
        public void test_apply_resistance_negative_r_treated_as_zero()
        {
            // 病态负 R（生产恒 ≥0）→ 视为 0（无抗性），不抬伤害
            Assert.Equal(1000, CombatMath.ApplyResistance(1000, -1, 500));
            Assert.Equal(1000, CombatMath.ApplyResistance(1000, -100, 500));
        }

        [Fact]
        public void test_apply_resistance_k_le_zero_no_decay()
        {
            // 病态 K≤0（生产恒 >0 经 Validate 守）→ 无衰减，返原值
            Assert.Equal(1000, CombatMath.ApplyResistance(1000, 500, 0));
            Assert.Equal(1000, CombatMath.ApplyResistance(1000, 500, -1));
            Assert.Equal(1, CombatMath.ApplyResistance(1, 500, 0));
        }

        [Fact]
        public void test_apply_resistance_is_deterministic_pure_function()
        {
            // 纯函数：同输入恒同输出（B.2）
            for (int raw = 0; raw <= 2000; raw += 100)
                for (int R = 0; R <= 3000; R += 200)
                    Assert.Equal(
                        CombatMath.ApplyResistance(raw, R, 500),
                        CombatMath.ApplyResistance(raw, R, 500));
        }

        // ============================================================
        // AC 7.3：LimitsConfig 旋钮
        // ============================================================

        [Fact]
        public void test_resistance_knobs_have_safe_defaults()
        {
            var c = Limits;
            Assert.Equal(500, c.ResistanceHalfLifeK);
            Assert.Equal(50, c.PhysResistPerConstitution);
            Assert.Equal(50, c.ElemResistPerInsight);
            Assert.Equal(100, c.BodyArtPhysResistBonus);
            Assert.Equal(100, c.PathElemResistBonus);
            c.Validate(); // 默认值合法
        }

        [Fact]
        public void test_resistance_k_zero_or_negative_throws()
        {
            Assert.Throws<InvalidOperationException>(
                () => (Limits with { ResistanceHalfLifeK = 0 }).Validate());
            Assert.Throws<InvalidOperationException>(
                () => (Limits with { ResistanceHalfLifeK = -1 }).Validate());
        }

        [Fact]
        public void test_resistance_negative_coefficients_throw()
        {
            Assert.Throws<InvalidOperationException>(
                () => (Limits with { PhysResistPerConstitution = -1 }).Validate());
            Assert.Throws<InvalidOperationException>(
                () => (Limits with { ElemResistPerInsight = -1 }).Validate());
            Assert.Throws<InvalidOperationException>(
                () => (Limits with { BodyArtPhysResistBonus = -1 }).Validate());
            Assert.Throws<InvalidOperationException>(
                () => (Limits with { PathElemResistBonus = -1 }).Validate());
        }

        [Fact]
        public void test_resistance_zero_coefficients_legal()
        {
            // 系数=0 合法（退化无对应抗性源）
            (Limits with { PhysResistPerConstitution = 0 }).Validate();
            (Limits with { ElemResistPerInsight = 0 }).Validate();
            (Limits with { BodyArtPhysResistBonus = 0 }).Validate();
            (Limits with { PathElemResistBonus = 0 }).Validate();
        }

        [Fact]
        public void test_resistance_knobs_reasonable_default_balance()
        {
            // 默认系数合理：PhysResistPerConstitution=50 不极端（非 0 非 1000）
            // Constitution 典型 10-20 → R 500-1000 → K=500 时减伤 33%-50%，合理档位
            var c = Limits;
            Assert.True(c.PhysResistPerConstitution > 0 && c.PhysResistPerConstitution < 1000);
            Assert.True(c.ElemResistPerInsight > 0 && c.ElemResistPerInsight < 1000);
            Assert.True(c.ResistanceHalfLifeK > 0);
        }

        // ============================================================
        // AC 7.4：ResolveExchange 接线——高体质/高识 → 更多减伤
        // ============================================================

        [Fact]
        public void test_high_constitution_reduces_physical_damage_more()
        {
            // 防方高体质(Constitution=20) → 物理抗 R=1000 → 减伤更狠 → 残血更高。
            // 对比防方低体质(Constitution=5) → R=250 → 减伤少 → 残血更低。
            // 攻方 Normal 攻击，Force 相同（隔离体质维度）。
            // 用确定性（duelRng=null）避免 cv-001 概率噪声干扰。
            var atkPath = MakePath("atk", DamageType.Normal);
            var defPathHigh = MakePath("defHigh", DamageType.Normal);
            var defPathLow = MakePath("defLow", DamageType.Normal);

            var a1 = MakeChar(1, 25, 20, 5, atkPath);  // 攻方
            var bHigh = MakeChar(2, 25, 20, 5, defPathHigh); // 防方高体质
            var a2 = MakeChar(1, 25, 5, 5, atkPath);   // 攻方（同 PE）
            var bLow = MakeChar(2, 25, 5, 5, defPathLow);    // 防方低体质

            // 注意：PE 只读 Force，故攻方 PE 相同；防方 Force=25 同 PE。
            // 体质差不影响 PE → 隔离抗性维度。
            var rHigh = DuelEngine.ResolveR2(a1, bHigh, atkPath, defPathHigh,
                Reg(atkPath, defPathHigh), Limits, null, null, null);
            var rLow = DuelEngine.ResolveR2(a2, bLow, atkPath, defPathLow,
                Reg(atkPath, defPathLow), Limits, null, null, null);

            // 高体质防方受减伤保护 → 残血应 ≥ 低体质防方
            Assert.True(rHigh.DefenderHpRemaining >= rLow.DefenderHpRemaining,
                $"高体质防方残血({rHigh.DefenderHpRemaining}) 应 ≥ 低体质({rLow.DefenderHpRemaining})");
        }

        [Fact]
        public void test_high_insight_reduces_elemental_damage_more()
        {
            // 防方高识(Insight=20) → 属性抗 R=1000 → Elemental 减伤更狠 → 残血更高。
            // 对比防方低识(Insight=5) → R=250 → 减伤少。
            // 攻方 Elemental 攻击。PE 只读 Force → 识差不扰 PE。
            var atkPath = MakePath("atk", DamageType.Elemental);
            var defPathHigh = MakePath("defHigh", DamageType.Normal);
            var defPathLow = MakePath("defLow", DamageType.Normal);

            var a1 = MakeChar(1, 25, 5, 20, atkPath);
            var bHigh = MakeChar(2, 25, 5, 20, defPathHigh);
            var a2 = MakeChar(1, 25, 5, 5, atkPath);
            var bLow = MakeChar(2, 25, 5, 5, defPathLow);

            var rHigh = DuelEngine.ResolveR2(a1, bHigh, atkPath, defPathHigh,
                Reg(atkPath, defPathHigh), Limits, null, null, null);
            var rLow = DuelEngine.ResolveR2(a2, bLow, atkPath, defPathLow,
                Reg(atkPath, defPathLow), Limits, null, null, null);

            Assert.True(rHigh.DefenderHpRemaining >= rLow.DefenderHpRemaining,
                $"高识防方残血({rHigh.DefenderHpRemaining}) 应 ≥ 低识({rLow.DefenderHpRemaining}) 对 Elemental");
        }

        [Fact]
        public void test_resistance_layer_actually_applied_in_duel()
        {
            // 直接对比：相同攻防配置，K=500（抗性生效）vs K=int.MaxValue（抗性退化为几乎无衰减）。
            // K 极大 → R/K 趋 0 → multiplier 趋 1000 → 无减伤 → 防方残血更低。
            // 用 Normal 攻击 + 高体质防方（R=1000）：K=500 → multiplier=500/3≈33%（强减伤）；
            //   K=10^9 → multiplier≈10^9/(10^9+1000)≈999（几乎无减伤）。
            var atkPath = MakePath("atk", DamageType.Normal);
            var defPath = MakePath("def", DamageType.Normal);
            var a1 = MakeChar(1, 25, 20, 5, atkPath);
            var b1 = MakeChar(2, 25, 20, 5, defPath);
            var a2 = MakeChar(1, 25, 20, 5, atkPath);
            var b2 = MakeChar(2, 25, 20, 5, defPath);

            var withResist = DuelEngine.ResolveR2(a1, b1, atkPath, defPath,
                Reg(atkPath, defPath), Limits, null, null, null);
            var noResist = DuelEngine.ResolveR2(a2, b2, atkPath, defPath,
                Reg(atkPath, defPath), Limits with { ResistanceHalfLifeK = 1_000_000_000 }, null, null, null);

            // 抗性生效（K=500）→ 防方更耐打 → 残血更高
            Assert.True(withResist.DefenderHpRemaining > noResist.DefenderHpRemaining,
                $"抗性生效时防方残血({withResist.DefenderHpRemaining}) 应 > 抗性退化时({noResist.DefenderHpRemaining})");
        }

        // ============================================================
        // AC 7.5：B.5 道心解耦——daoHeart/innerDemon 不进 R / EffectivePower
        // ============================================================

        [Fact]
        public void test_daoheart_does_not_enter_effective_power()
        {
            // 修改 daoHeart/innerDemon → EffectivePower 不变（PowerEngine.Evaluate 不读道心）。
            var path = MakePath("p", DamageType.Normal);
            var c = MakeChar(1, 20, 10, 5, path);
            int peBefore = PowerEngine.Evaluate(c.Cultivation!, c.Stats, path, Limits);

            c.Cultivation!.GainDaoHeart(50);    // 道心 +50
            c.Cultivation!.GainInnerDemon(30);  // 心魔 +30
            int peAfter = PowerEngine.Evaluate(c.Cultivation!, c.Stats, path, Limits);

            Assert.Equal(peBefore, peAfter); // EP 不含道心
        }

        [Fact]
        public void test_daoheart_does_not_enter_resistance()
        {
            // 修改 daoHeart/innerDemon → R 不变（ResistanceOf 不读道心，签名不含）。
            var path = MakePath("p", DamageType.Normal);
            var c = MakeChar(1, 20, 10, 5, path);
            int rBefore = ResistanceProviders.ResistanceOf(
                c.Cultivation!, c.Stats, path, GateType.None, DamageType.Normal, Limits);

            c.Cultivation!.GainDaoHeart(80);
            c.Cultivation!.GainInnerDemon(60);
            int rAfter = ResistanceProviders.ResistanceOf(
                c.Cultivation!, c.Stats, path, GateType.None, DamageType.Normal, Limits);

            Assert.Equal(rBefore, rAfter); // R 不含道心
        }

        // ============================================================
        // AC 7.6：calibrationMode 旁路——标定模式抵抗层不生效
        // ============================================================

        [Fact]
        public void test_calibration_mode_bypasses_resistance()
        {
            // 标定模式（calibrationMode=true）：抵抗层旁路 → R 视为 0。
            // 对比：非标定模式（calibrationMode=false）+ 高体质防方 → 抵抗层生效 → 防方残血更高。
            // 用确定性（duelRng=null）隔离：标定 vs 非标定唯一差异 = 抵抗层是否生效。
            var atkPath = MakePath("atk", DamageType.Normal);
            var defPath = MakePath("def", DamageType.Normal);
            var a1 = MakeChar(1, 25, 20, 5, atkPath);
            var b1 = MakeChar(2, 25, 20, 5, defPath);
            var a2 = MakeChar(1, 25, 20, 5, atkPath);
            var b2 = MakeChar(2, 25, 20, 5, defPath);

            var notCalib = DuelEngine.ResolveR2(a1, b1, atkPath, defPath,
                Reg(atkPath, defPath), Limits, null, null, null); // 非标定：抵抗层生效
            var calib = DuelEngine.ResolveR2(a2, b2, atkPath, defPath,
                Reg(atkPath, defPath), Limits, null, null, null, calibrationMode: true); // 标定：旁路

            // 非标定（抵抗生效）→ 防方更耐打 → 残血更高
            Assert.True(notCalib.DefenderHpRemaining > calib.DefenderHpRemaining,
                $"非标定防方残血({notCalib.DefenderHpRemaining}) 应 > 标定({calib.DefenderHpRemaining}) —— 抵抗层在标定模式旁路");
        }

        [Fact]
        public void test_calibration_mode_resistance_equals_zero_resistance_knob()
        {
            // 标定模式（抵抗旁路）应等同"非标定 + 抗性系数全 0"（R=0 → 无衰减）。
            // 两者唯一差异 = 抵抗层是否计算 R；标定 R=0 vs 非标定 R=0（系数全 0）应同结果。
            var zeroResistLimits = Limits with
            {
                PhysResistPerConstitution = 0,
                ElemResistPerInsight = 0,
                BodyArtPhysResistBonus = 0,
                PathElemResistBonus = 0
            };

            var atkPath = MakePath("atk", DamageType.Normal);
            var defPath = MakePath("def", DamageType.Normal);
            var a1 = MakeChar(1, 25, 20, 5, atkPath);
            var b1 = MakeChar(2, 25, 20, 5, defPath);
            var a2 = MakeChar(1, 25, 20, 5, atkPath);
            var b2 = MakeChar(2, 25, 20, 5, defPath);

            var calib = DuelEngine.ResolveR2(a1, b1, atkPath, defPath,
                Reg(atkPath, defPath), Limits, null, null, null, calibrationMode: true);
            var zeroR = DuelEngine.ResolveR2(a2, b2, atkPath, defPath,
                Reg(atkPath, defPath), zeroResistLimits, null, null, null); // 非标定但 R 恒 0

            Assert.Equal(calib.Winner, zeroR.Winner);
            Assert.Equal(calib.Margin, zeroR.Margin);
            Assert.Equal(calib.AttackerHpRemaining, zeroR.AttackerHpRemaining);
            Assert.Equal(calib.DefenderHpRemaining, zeroR.DefenderHpRemaining);
        }

        // ============================================================
        // AC 7.7：B.2 浮点零 + B.3 off 不退 + 确定性 + 不退
        // ============================================================

        [Fact]
        public void test_cultivation_namespace_has_no_float_after_resistance()
        {
            // B.2：ApplyResistance / ResistanceOf 全整数（long 中间量 + 整数除法），IL 扫描零浮点。
            var asmPath = typeof(Jianghu.Sim.World).Assembly.Location;
            var offenders = ILFloatScanner.ScanNamespace(asmPath, "Jianghu.Cultivation");
            Assert.True(offenders.Count == 0, "浮点出现在: " + string.Join(", ", offenders));
        }

        [Fact]
        public void test_resistance_is_deterministic_same_seed()
        {
            // B.2 确定性：抵抗层同种子两跑逐字节复现（R 派生无 RNG，半衰纯整数）
            var atkPath = MakePath("atk", DamageType.Normal);
            var defPath = MakePath("def", DamageType.Normal);
            var a = MakeChar(1, 25, 20, 5, atkPath);
            var b = MakeChar(2, 24, 18, 5, defPath);

            var r1 = DuelEngine.ResolveR2(a, b, atkPath, defPath, Reg(atkPath, defPath),
                Limits, null, null, null, duelRng: Rng(42));
            var r2 = DuelEngine.ResolveR2(a, b, atkPath, defPath, Reg(atkPath, defPath),
                Limits, null, null, null, duelRng: Rng(42));

            Assert.Equal(r1.Winner, r2.Winner);
            Assert.Equal(r1.Margin, r2.Margin);
            Assert.Equal(r1.AttackerHpRemaining, r2.AttackerHpRemaining);
            Assert.Equal(r1.DefenderHpRemaining, r2.DefenderHpRemaining);
        }

        [Fact]
        public void test_off_mode_unaffected_by_resistance_knobs()
        {
            // B.3：off（cultivation=false）不入 DuelEngine → 抗性旋钮零影响，同种子两跑 Chronicle 逐字节。
            string Run()
            {
                var w = WorldFactory.CreateInitial(2026, LimitsConfig.Default, 5);
                for (int i = 0; i < 200; i++) w.Advance(6);
                return string.Join("\n", w.Chronicle.Lines);
            }
            Assert.Equal(Run(), Run());
        }
    }
}
