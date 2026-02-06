using FluentAssertions;
using HeatingOilTracker.Models;
using Xunit;

namespace HeatingOilTracker.Tests.Models;

public class SeasonalBreakdownTests
{
    [Fact]
    public void TotalCost_SumsCorrectly()
    {
        // Arrange
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonCost = 2000m,
            OffSeasonCost = 500m
        };

        // Act & Assert
        breakdown.TotalCost.Should().Be(2500m);
    }

    [Fact]
    public void TotalGallons_SumsCorrectly()
    {
        // Arrange
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonGallons = 600m,
            OffSeasonGallons = 150m
        };

        // Act & Assert
        breakdown.TotalGallons.Should().Be(750m);
    }

    [Fact]
    public void HeatingSeasonCostPercent_CalculatesCorrectly()
    {
        // Arrange: $2000 heating / $2500 total = 80%
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonCost = 2000m,
            OffSeasonCost = 500m
        };

        // Act & Assert
        breakdown.HeatingSeasonCostPercent.Should().Be(80m);
    }

    [Fact]
    public void OffSeasonCostPercent_CalculatesCorrectly()
    {
        // Arrange: $500 off-season / $2500 total = 20%
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonCost = 2000m,
            OffSeasonCost = 500m
        };

        // Act & Assert
        breakdown.OffSeasonCostPercent.Should().Be(20m);
    }

    [Fact]
    public void CostPercentages_ZeroTotalCost_ReturnsZero()
    {
        // Arrange
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonCost = 0m,
            OffSeasonCost = 0m
        };

        // Act & Assert
        breakdown.HeatingSeasonCostPercent.Should().Be(0m);
        breakdown.OffSeasonCostPercent.Should().Be(0m);
    }

    [Fact]
    public void HeatingSeasonCO2Lbs_CalculatesCorrectly()
    {
        // Arrange: 500 gallons * 22.38 lbs/gallon = 11190 lbs
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonGallons = 500m
        };

        // Act & Assert
        breakdown.HeatingSeasonCO2Lbs.Should().Be(11190m);
    }

    [Fact]
    public void OffSeasonCO2Lbs_CalculatesCorrectly()
    {
        // Arrange: 100 gallons * 22.38 lbs/gallon = 2238 lbs
        var breakdown = new SeasonalBreakdown
        {
            OffSeasonGallons = 100m
        };

        // Act & Assert
        breakdown.OffSeasonCO2Lbs.Should().Be(2238m);
    }

    [Fact]
    public void TotalCO2Lbs_SumsCorrectly()
    {
        // Arrange: 600 total gallons * 22.38 = 13428 lbs
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonGallons = 500m,
            OffSeasonGallons = 100m
        };

        // Act & Assert
        breakdown.TotalCO2Lbs.Should().Be(13428m);
    }

    [Fact]
    public void HeatingSeasonCO2Percent_CalculatesCorrectly()
    {
        // Arrange: 500 heating / 600 total = 83.33%
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonGallons = 500m,
            OffSeasonGallons = 100m
        };

        // Act & Assert
        breakdown.HeatingSeasonCO2Percent.Should().BeApproximately(83.33m, 0.01m);
    }

    [Fact]
    public void OffSeasonCO2Percent_CalculatesCorrectly()
    {
        // Arrange: 100 off-season / 600 total = 16.67%
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonGallons = 500m,
            OffSeasonGallons = 100m
        };

        // Act & Assert
        breakdown.OffSeasonCO2Percent.Should().BeApproximately(16.67m, 0.01m);
    }

    [Fact]
    public void CO2Percentages_ZeroTotalGallons_ReturnsZero()
    {
        // Arrange
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonGallons = 0m,
            OffSeasonGallons = 0m
        };

        // Act & Assert
        breakdown.HeatingSeasonCO2Percent.Should().Be(0m);
        breakdown.OffSeasonCO2Percent.Should().Be(0m);
    }

    [Fact]
    public void PercentagesAddUpTo100_WithNonZeroValues()
    {
        // Arrange
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonGallons = 750m,
            HeatingSeasonCost = 2625m,
            OffSeasonGallons = 250m,
            OffSeasonCost = 875m
        };

        // Act & Assert
        (breakdown.HeatingSeasonCostPercent + breakdown.OffSeasonCostPercent).Should().Be(100m);
        (breakdown.HeatingSeasonCO2Percent + breakdown.OffSeasonCO2Percent).Should().Be(100m);
    }

    [Fact]
    public void AllHeatingSeasonOnly_Returns100PercentHeating()
    {
        // Arrange
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonGallons = 500m,
            HeatingSeasonCost = 1750m,
            OffSeasonGallons = 0m,
            OffSeasonCost = 0m
        };

        // Act & Assert
        breakdown.HeatingSeasonCostPercent.Should().Be(100m);
        breakdown.OffSeasonCostPercent.Should().Be(0m);
        breakdown.HeatingSeasonCO2Percent.Should().Be(100m);
        breakdown.OffSeasonCO2Percent.Should().Be(0m);
    }

    [Fact]
    public void AllOffSeasonOnly_Returns100PercentOffSeason()
    {
        // Arrange
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonGallons = 0m,
            HeatingSeasonCost = 0m,
            OffSeasonGallons = 200m,
            OffSeasonCost = 700m
        };

        // Act & Assert
        breakdown.HeatingSeasonCostPercent.Should().Be(0m);
        breakdown.OffSeasonCostPercent.Should().Be(100m);
        breakdown.HeatingSeasonCO2Percent.Should().Be(0m);
        breakdown.OffSeasonCO2Percent.Should().Be(100m);
    }

    [Theory]
    [InlineData(800, 200, 80, 20)]
    [InlineData(500, 500, 50, 50)]
    [InlineData(900, 100, 90, 10)]
    [InlineData(600, 400, 60, 40)]
    public void CostPercentages_VariousSplits_CalculatesCorrectly(
        decimal heatingCost, decimal offSeasonCost,
        decimal expectedHeatingPercent, decimal expectedOffSeasonPercent)
    {
        // Arrange
        var breakdown = new SeasonalBreakdown
        {
            HeatingSeasonCost = heatingCost,
            OffSeasonCost = offSeasonCost
        };

        // Act & Assert
        breakdown.HeatingSeasonCostPercent.Should().Be(expectedHeatingPercent);
        breakdown.OffSeasonCostPercent.Should().Be(expectedOffSeasonPercent);
    }
}
