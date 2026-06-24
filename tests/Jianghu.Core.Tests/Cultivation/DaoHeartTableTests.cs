using System;
using System.Collections.Generic;
using Jianghu.Cultivation;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// Story-001: 21路道心资源表 standalone tests.
    /// AC 1.1-1.7: 每路 daoHeart_init>0, ≥3 gain/demon sources, data-driven, off mode.
    /// </summary>
    public class DaoHeartTableTests
    {
        static readonly DaoHeartRegistry Registry = new DaoHeartRegistry();

        // ================================================================
        // AC 1.1: 21路 daoHeart_init 乘子表
        // ================================================================

        [Fact] public void SwordImmortal_Has_DaoHeartInitMultiplier_2() => AssertInit("sword_immortal", 2);
        [Fact] public void TiXiu_Has_DaoHeartInitMultiplier_2() => AssertInit("ti_xiu_hengshi", 2);
        [Fact] public void FaXiu_Has_DaoHeartInitMultiplier_2() => AssertInit("fa_xiu", 2);
        [Fact] public void ArrayFormation_Has_DaoHeartInitMultiplier_2() => AssertInit("array_formation", 2);
        [Fact] public void Qixiu_Has_DaoHeartInitMultiplier_2() => AssertInit("qixiu_artificer", 2);
        [Fact] public void Soul_Has_DaoHeartInitMultiplier_3() => AssertInit("soul_divine_sense", 3);
        [Fact] public void MingFate_Has_DaoHeartInitMultiplier_2() => AssertInit("ming_fate_causality", 2);
        [Fact] public void DanXiu_Has_DaoHeartInitMultiplier_2() => AssertInit("dan_xiu", 2);
        [Fact] public void GuiXiu_Has_DaoHeartInitMultiplier_2() => AssertInit("gui_xiu_yang_hun", 2);
        [Fact] public void Buddhist_Has_DaoHeartInitMultiplier_3() => AssertInit("buddhist_golden_body", 3);
        [Fact] public void LeiXiu_Has_DaoHeartInitMultiplier_2() => AssertInit("lei_xiu", 2);
        [Fact] public void YuShou_Has_DaoHeartInitMultiplier_2() => AssertInit("yu_shou", 2);
        [Fact] public void RuXiu_Has_DaoHeartInitMultiplier_2() => AssertInit("ru_xiu_haoran", 2);
        [Fact] public void MoXiu_Has_DaoHeartInitMultiplier_2() => AssertInit("mo_xiu_xinmo", 2);
        [Fact] public void YaoXiu_Has_DaoHeartInitMultiplier_2() => AssertInit("yao_xiu_huaxing", 2);
        [Fact] public void XueXiu_Has_DaoHeartInitMultiplier_2() => AssertInit("xue_xiu_xuesha", 2);
        [Fact] public void DuGuXiu_Has_DaoHeartInitMultiplier_2() => AssertInit("du_gu_xiu", 2);
        [Fact] public void FuXiu_Has_DaoHeartInitMultiplier_2() => AssertInit("fu_xiu_fulu", 2);
        [Fact] public void Kuilei_Has_DaoHeartInitMultiplier_2() => AssertInit("kuilei_shi", 2);
        [Fact] public void YinXiu_Has_DaoHeartInitMultiplier_2() => AssertInit("yin_xiu_yuedao", 2);
        [Fact] public void Yinguo_Has_DaoHeartInitMultiplier_2() => AssertInit("yinguo_faze", 2);

        static void AssertInit(string pathId, int expectedMultiplier)
        {
            var def = Registry.ById(pathId);
            Assert.NotNull(def);
            Assert.Equal(expectedMultiplier, def.InitMultiplier);
        }

        // ================================================================
        // AC 1.2: 每路 ≥3 个 daoHeart 增益来源
        // ================================================================

        [Fact]
        public void All21Paths_Have_AtLeast3_DaoHeartGainSources()
        {
            var failures = new List<string>();
            // Use reflection to get all IDs from PathValidator's known set
            foreach (var pathId in PathValidator.KnownPathIds)
            {
                var def = Registry.ById(pathId);
                if (def.GainSources.Count < 3)
                    failures.Add($"{pathId}: {def.GainSources.Count} gain sources");
            }

            Assert.True(failures.Count == 0,
                $"Paths with <3 daoHeart gain sources: {string.Join(", ", failures)}");
        }

        // ================================================================
        // AC 1.3: 每路 ≥3 个 innerDemon 来源
        // ================================================================

        [Fact]
        public void All21Paths_Have_AtLeast3_InnerDemonSources()
        {
            var failures = new List<string>();
            foreach (var pathId in PathValidator.KnownPathIds)
            {
                var def = Registry.ById(pathId);
                if (def.DemonSources.Count < 3)
                    failures.Add($"{pathId}: {def.DemonSources.Count} demon sources");
            }

            Assert.True(failures.Count == 0,
                $"Paths with <3 innerDemon sources: {string.Join(", ", failures)}");
        }

        // ================================================================
        // AC 1.4: 数据驱动 — 注册表正确加载 21 路
        // ================================================================

        [Fact]
        public void Registry_Has_All21Paths()
        {
            Assert.Equal(21, Registry.Count);
            foreach (var pathId in PathValidator.KnownPathIds)
                Assert.True(Registry.Contains(pathId), $"Missing: {pathId}");
        }

        [Fact]
        public void Registry_UnknownPath_Throws()
        {
            Assert.Throws<KeyNotFoundException>(() => Registry.ById("nonexistent_path"));
        }

        // ================================================================
        // AC 1.5: 佛修破戒规则初值 — vow 标记预期存在
        // ================================================================

        [Fact]
        public void Buddhist_Has_VowAware_InitMultiplier_3()
        {
            var def = Registry.ById("buddhist_golden_body");
            Assert.Equal(3, def.InitMultiplier);
            // 佛修破戒时 daoHeart 折半非归零（story-003 落地），此处仅验证初值可加载
        }

        // ================================================================
        // AC 1.6: 全路 standalone — 每路 Def 可独立验证
        // ================================================================

        [Fact]
        public void EachPath_GainSources_Have_PositiveAmounts()
        {
            foreach (var pathId in PathValidator.KnownPathIds)
            {
                var def = Registry.ById(pathId);
                foreach (var gain in def.GainSources)
                    Assert.True(gain.Amount > 0,
                        $"{pathId} gain '{gain.Source}' has non-positive amount {gain.Amount}");
            }
        }

        [Fact]
        public void EachPath_DemonSources_Have_PositiveAmounts()
        {
            foreach (var pathId in PathValidator.KnownPathIds)
            {
                var def = Registry.ById(pathId);
                foreach (var demon in def.DemonSources)
                    Assert.True(demon.Amount > 0,
                        $"{pathId} demon '{demon.Source}' has non-positive amount {demon.Amount}");
            }
        }

        [Fact]
        public void EachPath_GainSources_Have_NonEmptySourceNames()
        {
            foreach (var pathId in PathValidator.KnownPathIds)
            {
                var def = Registry.ById(pathId);
                foreach (var gain in def.GainSources)
                    Assert.False(string.IsNullOrWhiteSpace(gain.Source),
                        $"{pathId} has gain with empty source name");
            }
        }

        // ================================================================
        // AC 1.7: off 模式 — 道心表存在但不初始化 daoHeart
        // ================================================================

        [Fact]
        public void OffMode_Registry_StillLoadable()
        {
            // In off mode, CultivationState.daoHeart stays 0,
            // but the registry is still loadable (pure data, no side effects).
            var reg = new DaoHeartRegistry();
            Assert.Equal(21, reg.Count);
        }

        [Fact]
        public void Registry_Creation_NoSideEffects()
        {
            // Creating registry should not modify any global state
            var reg1 = new DaoHeartRegistry();
            var reg2 = new DaoHeartRegistry();
            Assert.Equal(reg1.Count, reg2.Count);

            var def1 = reg1.ById("sword_immortal");
            var def2 = reg2.ById("sword_immortal");
            Assert.Equal(def1.InitMultiplier, def2.InitMultiplier);
            Assert.Equal(def1.GainSources.Count, def2.GainSources.Count);
        }

        // ================================================================
        // Bonus: auxiliary paths have reasonable init
        // ================================================================

        [Fact]
        public void AuxiliaryPaths_Have_ReasonableInit()
        {
            string[] aux = { "dan_xiu", "array_formation", "qixiu_artificer" };
            foreach (var id in aux)
            {
                var def = Registry.ById(id);
                Assert.Equal(2, def.InitMultiplier);
                Assert.True(def.GainSources.Count >= 3);
                Assert.True(def.DemonSources.Count >= 3);
            }
        }
    }
}
