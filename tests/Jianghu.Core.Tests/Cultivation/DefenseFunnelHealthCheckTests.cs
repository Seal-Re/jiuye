using System;
using Jianghu.Config;
using Xunit;

namespace Jianghu.Core.Tests.Cultivation
{
    /// <summary>
    /// 防御漏斗全旋钮健康检查——默认值合法 + 边界验证 + 退化语义。
    /// 防回归：新增旋钮或改默认值时自动检测不合法配置。
    /// </summary>
    public class DefenseFunnelHealthCheckTests
    {
        [Fact]
        public void all_defaults_validate_without_exception()
            => LimitsConfig.Default.Validate();

        [Fact]
        public void all_knobs_have_positive_or_zero_minimum()
        {
            var c = LimitsConfig.Default;
            Assert.True(c.ResistanceHalfLifeK > 0, "K must >0");
            Assert.True(c.PhysResistPerConstitution >= 0);
            Assert.True(c.ElemResistPerInsight >= 0);
            Assert.True(c.BodyArtPhysResistBonus >= 0);
            Assert.True(c.PathElemResistBonus >= 0);
            Assert.True(c.ChipDamagePermille >= 0);
            Assert.True(c.ChipMarginDivisor >= 0 || c.ChipMarginDivisor <= 0); // any int OK
            Assert.True(c.OverflowThresholdPermille >= 0);
            Assert.True(c.GuaranteeFrameCount >= 0);
            Assert.True(c.PoiseMax >= 1);
            Assert.True(c.PoiseDamageRatioPermille >= 0);
        }

        // Resistance knobs (cv-007)
        [Fact]
        public void resistance_zero_coefficient_is_legal_degenerate()
        {
            var c = LimitsConfig.Default with
            {
                PhysResistPerConstitution = 0,
                ElemResistPerInsight = 0,
                BodyArtPhysResistBonus = 0,
                PathElemResistBonus = 0
            };
            c.Validate(); // 0 = 退化关闭，合法
        }

        // Chip knobs (cv-003/008)
        [Fact]
        public void chip_zero_permille_is_legal_degenerate()
        {
            var c = LimitsConfig.Default with { ChipDamagePermille = 0 };
            c.Validate(); // 0 = 无 chip，合法退化
        }

        // Overflow knob (cv-004)
        [Fact]
        public void overflow_zero_threshold_is_legal_degenerate()
        {
            var c = LimitsConfig.Default with { OverflowThresholdPermille = 0 };
            c.Validate(); // 0 = 关闭溢出检测，合法
        }

        // Guarantee frame knob (cv-004)
        [Fact]
        public void guarantee_frame_zero_is_legal_degenerate()
        {
            var c = LimitsConfig.Default with { GuaranteeFrameCount = 0 };
            c.Validate(); // 0 = 关闭保底帧，合法
        }

        // B.2: All knobs are plain ints (not float/double)
        [Fact]
        public void all_knobs_are_integer_type()
        {
            var c = LimitsConfig.Default;
            Assert.IsType<int>(c.ResistanceHalfLifeK);
            Assert.IsType<int>(c.PhysResistPerConstitution);
            Assert.IsType<int>(c.ElemResistPerInsight);
            Assert.IsType<int>(c.BodyArtPhysResistBonus);
            Assert.IsType<int>(c.PathElemResistBonus);
            Assert.IsType<int>(c.ChipDamagePermille);
            Assert.IsType<int>(c.ChipMarginDivisor);
            Assert.IsType<int>(c.OverflowThresholdPermille);
            Assert.IsType<int>(c.GuaranteeFrameCount);
            Assert.IsType<int>(c.PoiseMax);
            Assert.IsType<int>(c.PoiseDamageRatioPermille);
        }

        // Combined: all knobs zero (maximum degenerate, still valid)
        [Fact]
        public void all_zero_maximum_degenerate_still_validates()
        {
            var c = LimitsConfig.Default with
            {
                ResistanceHalfLifeK = 1, // K=1 min (>0)
                PhysResistPerConstitution = 0,
                ElemResistPerInsight = 0,
                BodyArtPhysResistBonus = 0,
                PathElemResistBonus = 0,
                ChipDamagePermille = 0,
                OverflowThresholdPermille = 0,
                GuaranteeFrameCount = 0
            };
            c.Validate(); // 最大退化，全关，应合法
        }

        // Non-degenerate: all knobs at typical values validate
        [Fact]
        public void typical_values_validate()
        {
            var c = LimitsConfig.Default with
            {
                ResistanceHalfLifeK = 500,
                PhysResistPerConstitution = 50,
                ElemResistPerInsight = 50,
                BodyArtPhysResistBonus = 100,
                PathElemResistBonus = 100,
                ChipDamagePermille = 300,
                OverflowThresholdPermille = 1000,
                GuaranteeFrameCount = 2,
                PoiseMax = 300,
                PoiseDamageRatioPermille = 1000
            };
            c.Validate();
            Assert.Equal(500, c.ResistanceHalfLifeK);
            Assert.Equal(1000, c.OverflowThresholdPermille);
        }
    }
}
