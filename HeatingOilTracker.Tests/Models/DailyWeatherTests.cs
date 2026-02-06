using FluentAssertions;
using HeatingOilTracker.Models;
using Xunit;

namespace HeatingOilTracker.Tests.Models;

public class DailyWeatherTests
{
    [Fact]
    public void AvgTempF_CalculatesCorrectAverage()
    {
        // Arrange
        var weather = new DailyWeather
        {
            Date = DateTime.Today,
            HighTempF = 50m,
            LowTempF = 30m
        };

        // Act & Assert
        weather.AvgTempF.Should().Be(40m);
    }

    [Fact]
    public void HDD_ColdDay_CalculatesCorrectly()
    {
        // Arrange: High=30, Low=10, Avg=20, HDD=65-20=45
        var weather = new DailyWeather
        {
            Date = DateTime.Today,
            HighTempF = 30m,
            LowTempF = 10m
        };

        // Act & Assert
        weather.HDD.Should().Be(45m);
    }

    [Fact]
    public void HDD_MildDay_CalculatesCorrectly()
    {
        // Arrange: High=60, Low=50, Avg=55, HDD=65-55=10
        var weather = new DailyWeather
        {
            Date = DateTime.Today,
            HighTempF = 60m,
            LowTempF = 50m
        };

        // Act & Assert
        weather.HDD.Should().Be(10m);
    }

    [Fact]
    public void HDD_WarmDay_ReturnsZero()
    {
        // Arrange: High=80, Low=70, Avg=75, HDD=max(0, 65-75)=0
        var weather = new DailyWeather
        {
            Date = DateTime.Today,
            HighTempF = 80m,
            LowTempF = 70m
        };

        // Act & Assert
        weather.HDD.Should().Be(0m);
    }

    [Fact]
    public void HDD_ExactlyAt65Average_ReturnsZero()
    {
        // Arrange: High=70, Low=60, Avg=65, HDD=max(0, 65-65)=0
        var weather = new DailyWeather
        {
            Date = DateTime.Today,
            HighTempF = 70m,
            LowTempF = 60m
        };

        // Act & Assert
        weather.HDD.Should().Be(0m);
    }

    [Fact]
    public void HDD_JustBelow65Average_ReturnsSmallHDD()
    {
        // Arrange: High=69, Low=59, Avg=64, HDD=65-64=1
        var weather = new DailyWeather
        {
            Date = DateTime.Today,
            HighTempF = 69m,
            LowTempF = 59m
        };

        // Act & Assert
        weather.HDD.Should().Be(1m);
    }

    [Fact]
    public void HDD_BelowZeroTemps_CalculatesCorrectly()
    {
        // Arrange: High=10, Low=-10, Avg=0, HDD=65-0=65
        var weather = new DailyWeather
        {
            Date = DateTime.Today,
            HighTempF = 10m,
            LowTempF = -10m
        };

        // Act & Assert
        weather.HDD.Should().Be(65m);
    }

    [Fact]
    public void HDD_ExtremeSubZero_CalculatesCorrectly()
    {
        // Arrange: High=-5, Low=-25, Avg=-15, HDD=65-(-15)=80
        var weather = new DailyWeather
        {
            Date = DateTime.Today,
            HighTempF = -5m,
            LowTempF = -25m
        };

        // Act & Assert
        weather.HDD.Should().Be(80m);
    }

    [Fact]
    public void HDD_ExtremeHeat_ReturnsZero()
    {
        // Arrange: High=110, Low=90, Avg=100, HDD=max(0, 65-100)=0
        var weather = new DailyWeather
        {
            Date = DateTime.Today,
            HighTempF = 110m,
            LowTempF = 90m
        };

        // Act & Assert
        weather.HDD.Should().Be(0m);
    }

    [Theory]
    [InlineData(30, 10, 45)]   // Cold winter day
    [InlineData(40, 20, 35)]   // Cool winter day
    [InlineData(50, 30, 25)]   // Cool day
    [InlineData(60, 50, 10)]   // Mild day
    [InlineData(70, 60, 0)]    // At 65 average
    [InlineData(80, 70, 0)]    // Warm day
    [InlineData(0, -20, 75)]   // Very cold day
    public void HDD_VariousTemperatures_CalculatesCorrectly(decimal high, decimal low, decimal expectedHdd)
    {
        // Arrange
        var weather = new DailyWeather
        {
            Date = DateTime.Today,
            HighTempF = high,
            LowTempF = low
        };

        // Act & Assert
        weather.HDD.Should().Be(expectedHdd);
    }
}
