using FluentAssertions;
using HeatingOilTracker.Core.Models;
using Xunit;

namespace HeatingOilTracker.Tests.Models;

public class RegionalSettingsTests
{
    #region IsHeatingSeason — Northern Hemisphere (wrapping Oct–Mar)

    [Theory]
    [InlineData(10, true)]
    [InlineData(11, true)]
    [InlineData(12, true)]
    [InlineData(1,  true)]
    [InlineData(2,  true)]
    [InlineData(3,  true)]
    [InlineData(4,  false)]
    [InlineData(5,  false)]
    [InlineData(6,  false)]
    [InlineData(7,  false)]
    [InlineData(8,  false)]
    [InlineData(9,  false)]
    public void IsHeatingSeason_NorthernHemisphereDefault_CorrectlyClassifiesAllMonths(int month, bool expectedHeating)
    {
        var settings = new RegionalSettings(); // defaults: start=10, end=3

        settings.IsHeatingSeason(month).Should().Be(expectedHeating);
    }

    #endregion

    #region IsHeatingSeason — Southern Hemisphere (non-wrapping Apr–Sep)

    [Theory]
    [InlineData(4,  true)]
    [InlineData(5,  true)]
    [InlineData(6,  true)]
    [InlineData(7,  true)]
    [InlineData(8,  true)]
    [InlineData(9,  true)]
    [InlineData(1,  false)]
    [InlineData(2,  false)]
    [InlineData(3,  false)]
    [InlineData(10, false)]
    [InlineData(11, false)]
    [InlineData(12, false)]
    public void IsHeatingSeason_SouthernHemisphere_CorrectlyClassifiesAllMonths(int month, bool expectedHeating)
    {
        var settings = new RegionalSettings { HeatingSeasonStartMonth = 4, HeatingSeasonEndMonth = 9 };

        settings.IsHeatingSeason(month).Should().Be(expectedHeating);
    }

    #endregion

    #region IsHeatingSeason — custom wrapping season (Nov–Feb)

    [Theory]
    [InlineData(11, true)]
    [InlineData(12, true)]
    [InlineData(1,  true)]
    [InlineData(2,  true)]
    [InlineData(3,  false)]
    [InlineData(4,  false)]
    [InlineData(10, false)]
    public void IsHeatingSeason_CustomWrappingNovToFeb_CorrectlyClassifiesMonths(int month, bool expectedHeating)
    {
        var settings = new RegionalSettings { HeatingSeasonStartMonth = 11, HeatingSeasonEndMonth = 2 };

        settings.IsHeatingSeason(month).Should().Be(expectedHeating);
    }

    #endregion

    #region IsHeatingSeason — boundary months

    [Fact]
    public void IsHeatingSeason_ExactStartMonth_ReturnsTrue()
    {
        var settings = new RegionalSettings { HeatingSeasonStartMonth = 10, HeatingSeasonEndMonth = 3 };

        settings.IsHeatingSeason(10).Should().BeTrue();
    }

    [Fact]
    public void IsHeatingSeason_ExactEndMonth_ReturnsTrue()
    {
        var settings = new RegionalSettings { HeatingSeasonStartMonth = 10, HeatingSeasonEndMonth = 3 };

        settings.IsHeatingSeason(3).Should().BeTrue();
    }

    [Fact]
    public void IsHeatingSeason_MonthJustAfterEnd_ReturnsFalse()
    {
        var settings = new RegionalSettings { HeatingSeasonStartMonth = 10, HeatingSeasonEndMonth = 3 };

        settings.IsHeatingSeason(4).Should().BeFalse();
    }

    [Fact]
    public void IsHeatingSeason_MonthJustBeforeStart_ReturnsFalse()
    {
        var settings = new RegionalSettings { HeatingSeasonStartMonth = 10, HeatingSeasonEndMonth = 3 };

        settings.IsHeatingSeason(9).Should().BeFalse();
    }

    #endregion

    #region MonthOptions

    [Fact]
    public void MonthOptions_All_HasTwelveEntries()
    {
        MonthOptions.All.Should().HaveCount(12);
    }

    [Fact]
    public void MonthOptions_All_CoverMonths1Through12()
    {
        MonthOptions.All.Select(m => m.Number).Should().BeEquivalentTo(Enumerable.Range(1, 12));
    }

    [Theory]
    [InlineData(1,  "January")]
    [InlineData(6,  "June")]
    [InlineData(10, "October")]
    [InlineData(12, "December")]
    public void MonthOptions_GetByNumber_ReturnsCorrectMonth(int number, string expectedName)
    {
        var result = MonthOptions.GetByNumber(number);

        result.Number.Should().Be(number);
        result.DisplayName.Should().Be(expectedName);
    }

    [Fact]
    public void MonthOptions_GetByNumber_InvalidMonth_ReturnsFallback()
    {
        var result = MonthOptions.GetByNumber(99);

        result.Number.Should().Be(10); // October fallback
    }

    #endregion
}
