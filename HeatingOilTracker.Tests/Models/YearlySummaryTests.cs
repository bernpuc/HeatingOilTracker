using FluentAssertions;
using HeatingOilTracker.Models;
using Xunit;

namespace HeatingOilTracker.Tests.Models;

public class YearlySummaryTests
{
    [Fact]
    public void Constants_HaveCorrectValues()
    {
        YearlySummary.DefaultCO2LbsPerGallon.Should().Be(22.38m);
        YearlySummary.OffsetPriceLowPerTon.Should().Be(15m);
        YearlySummary.OffsetPriceHighPerTon.Should().Be(50m);
    }

    [Fact]
    public void AvgPricePerGallon_CalculatesCorrectly()
    {
        // Arrange
        var summary = new YearlySummary
        {
            TotalGallons = 500m,
            TotalCost = 1750m  // $3.50 per gallon
        };

        // Act & Assert
        summary.AvgPricePerGallon.Should().Be(3.50m);
    }

    [Fact]
    public void AvgPricePerGallon_ZeroGallons_ReturnsZero()
    {
        // Arrange
        var summary = new YearlySummary
        {
            TotalGallons = 0m,
            TotalCost = 0m
        };

        // Act & Assert
        summary.AvgPricePerGallon.Should().Be(0m);
    }

    [Fact]
    public void CostPerHDD_CalculatesCorrectly()
    {
        // Arrange
        var summary = new YearlySummary
        {
            TotalCost = 3000m,
            TotalHDD = 6000m
        };

        // Act & Assert
        summary.CostPerHDD.Should().Be(0.50m);
    }

    [Fact]
    public void CostPerHDD_ZeroHDD_ReturnsNull()
    {
        // Arrange
        var summary = new YearlySummary
        {
            TotalCost = 3000m,
            TotalHDD = 0m
        };

        // Act & Assert
        summary.CostPerHDD.Should().BeNull();
    }

    [Fact]
    public void CostPerHDD_NullHDD_ReturnsNull()
    {
        // Arrange
        var summary = new YearlySummary
        {
            TotalCost = 3000m,
            TotalHDD = null
        };

        // Act & Assert
        summary.CostPerHDD.Should().BeNull();
    }

    [Fact]
    public void AvgKFactor_CalculatesCorrectly()
    {
        // Arrange: 6000 HDD / 1500 gallons = 4.0 K-Factor
        var summary = new YearlySummary
        {
            TotalGallons = 1500m,
            TotalHDD = 6000m
        };

        // Act & Assert
        summary.AvgKFactor.Should().Be(4.0m);
    }

    [Fact]
    public void AvgKFactor_ZeroGallons_ReturnsNull()
    {
        // Arrange
        var summary = new YearlySummary
        {
            TotalGallons = 0m,
            TotalHDD = 6000m
        };

        // Act & Assert
        summary.AvgKFactor.Should().BeNull();
    }

    [Fact]
    public void TotalCO2Lbs_CalculatesCorrectly()
    {
        // Arrange: 100 gallons * 22.38 lbs/gallon = 2238 lbs
        var summary = new YearlySummary
        {
            TotalGallons = 100m
        };

        // Act & Assert
        summary.TotalCO2Lbs.Should().Be(2238m);
    }

    [Fact]
    public void TotalCO2Kg_CalculatesCorrectly()
    {
        // Arrange: 100 gallons * 22.38 lbs/gallon * 0.453592 = 1015.14 kg (approximately)
        var summary = new YearlySummary
        {
            TotalGallons = 100m
        };

        // Act & Assert
        summary.TotalCO2Kg.Should().BeApproximately(1015.138896m, 0.0001m);
    }

    [Fact]
    public void TotalCO2MetricTons_CalculatesCorrectly()
    {
        // Arrange: 1000 gallons = 22380 lbs = 10151.39 kg = 10.151 metric tons
        var summary = new YearlySummary
        {
            TotalGallons = 1000m
        };

        // Act & Assert
        summary.TotalCO2MetricTons.Should().BeApproximately(10.15139m, 0.0001m);
    }

    [Fact]
    public void CO2LbsPerHDD_CalculatesCorrectly()
    {
        // Arrange: 500 gallons = 11190 lbs CO2, 5595 HDD = 2 lbs/HDD
        var summary = new YearlySummary
        {
            TotalGallons = 500m,
            TotalHDD = 5595m  // 11190 / 2 = 5595
        };

        // Act & Assert
        summary.CO2LbsPerHDD.Should().Be(2m);
    }

    [Fact]
    public void CO2LbsPerHDD_ZeroHDD_ReturnsNull()
    {
        // Arrange
        var summary = new YearlySummary
        {
            TotalGallons = 500m,
            TotalHDD = 0m
        };

        // Act & Assert
        summary.CO2LbsPerHDD.Should().BeNull();
    }

    [Fact]
    public void OffsetCostLow_CalculatesCorrectly()
    {
        // Arrange: 1000 gallons = ~10.151 metric tons * $15/ton = ~$152.27
        var summary = new YearlySummary
        {
            TotalGallons = 1000m
        };

        // Act & Assert
        summary.OffsetCostLow.Should().BeApproximately(152.27m, 0.01m);
    }

    [Fact]
    public void OffsetCostHigh_CalculatesCorrectly()
    {
        // Arrange: 1000 gallons = ~10.151 metric tons * $50/ton = ~$507.57
        var summary = new YearlySummary
        {
            TotalGallons = 1000m
        };

        // Act & Assert
        summary.OffsetCostHigh.Should().BeApproximately(507.57m, 0.01m);
    }

    [Fact]
    public void AllProperties_ZeroGallons_HandledGracefully()
    {
        // Arrange
        var summary = new YearlySummary
        {
            TotalGallons = 0m,
            TotalCost = 0m,
            TotalHDD = 0m
        };

        // Act & Assert - should not throw, should return safe values
        summary.AvgPricePerGallon.Should().Be(0m);
        summary.CostPerHDD.Should().BeNull();
        summary.AvgKFactor.Should().BeNull();
        summary.TotalCO2Lbs.Should().Be(0m);
        summary.TotalCO2Kg.Should().Be(0m);
        summary.TotalCO2MetricTons.Should().Be(0m);
        summary.CO2LbsPerHDD.Should().BeNull();
        summary.OffsetCostLow.Should().Be(0m);
        summary.OffsetCostHigh.Should().Be(0m);
    }

    [Theory]
    [InlineData(100, 2238)]
    [InlineData(500, 11190)]
    [InlineData(1000, 22380)]
    [InlineData(0, 0)]
    public void TotalCO2Lbs_VariousGallons_CalculatesCorrectly(decimal gallons, decimal expectedCO2)
    {
        // Arrange
        var summary = new YearlySummary { TotalGallons = gallons };

        // Act & Assert
        summary.TotalCO2Lbs.Should().Be(expectedCO2);
    }
}
