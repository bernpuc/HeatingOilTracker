using FluentAssertions;
using HeatingOilTracker.Models;
using HeatingOilTracker.Services;
using HeatingOilTracker.Tests.Fixtures;
using Xunit;

namespace HeatingOilTracker.Tests.Services;

public class WeatherServiceTests
{
    private readonly WeatherService _sut;

    public WeatherServiceTests()
    {
        _sut = new WeatherService();
    }

    #region CalculateKFactor Tests

    [Fact]
    public void CalculateKFactor_NormalCalculation_ReturnsCorrectValue()
    {
        // Arrange: 600 HDD / 150 gallons = 4.0
        var gallons = 150m;
        var hdd = 600m;

        // Act
        var result = _sut.CalculateKFactor(gallons, hdd);

        // Assert
        result.Should().Be(4.0m);
    }

    [Fact]
    public void CalculateKFactor_ZeroGallons_ReturnsZero()
    {
        // Arrange
        var gallons = 0m;
        var hdd = 600m;

        // Act
        var result = _sut.CalculateKFactor(gallons, hdd);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateKFactor_NegativeGallons_ReturnsZero()
    {
        // Arrange
        var gallons = -50m;
        var hdd = 600m;

        // Act
        var result = _sut.CalculateKFactor(gallons, hdd);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateKFactor_ZeroHDD_ReturnsZero()
    {
        // Arrange: 0 HDD / 150 gallons = 0
        var gallons = 150m;
        var hdd = 0m;

        // Act
        var result = _sut.CalculateKFactor(gallons, hdd);

        // Assert
        result.Should().Be(0m);
    }

    [Theory]
    [InlineData(150, 600, 4.0)]    // Standard efficiency
    [InlineData(100, 500, 5.0)]    // Higher efficiency
    [InlineData(200, 600, 3.0)]    // Lower efficiency
    [InlineData(125, 500, 4.0)]    // Another standard case
    public void CalculateKFactor_VariousInputs_CalculatesCorrectly(
        decimal gallons, decimal hdd, decimal expectedKFactor)
    {
        // Act
        var result = _sut.CalculateKFactor(gallons, hdd);

        // Assert
        result.Should().Be(expectedKFactor);
    }

    #endregion

    #region CalculateHDD Tests

    [Fact]
    public void CalculateHDD_DateRangeWithData_SumsCorrectly()
    {
        // Arrange: 5 days with 20 HDD each = 100 total
        var startDate = new DateTime(2024, 1, 1);
        var weatherData = TestData.CreateWeatherData(startDate, 5, 20m);

        // Act
        var result = _sut.CalculateHDD(weatherData, startDate, startDate.AddDays(4));

        // Assert
        result.Should().Be(100m);
    }

    [Fact]
    public void CalculateHDD_EmptyWeatherData_ReturnsZero()
    {
        // Arrange
        var weatherData = new List<DailyWeather>();
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var result = _sut.CalculateHDD(weatherData, startDate, endDate);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateHDD_NoMatchingDates_ReturnsZero()
    {
        // Arrange: Weather data in January, but querying February
        var startDate = new DateTime(2024, 1, 1);
        var weatherData = TestData.CreateWeatherData(startDate, 31, 25m);

        var queryStart = new DateTime(2024, 2, 1);
        var queryEnd = new DateTime(2024, 2, 28);

        // Act
        var result = _sut.CalculateHDD(weatherData, queryStart, queryEnd);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateHDD_SingleDayRange_ReturnsSingleDayHDD()
    {
        // Arrange: Single day with 30 HDD
        var date = new DateTime(2024, 1, 15);
        var weatherData = TestData.CreateWeatherData(date, 1, 30m);

        // Act
        var result = _sut.CalculateHDD(weatherData, date, date);

        // Assert
        result.Should().Be(30m);
    }

    [Fact]
    public void CalculateHDD_PartialOverlap_SumsOnlyMatchingDays()
    {
        // Arrange: 10 days of weather starting Jan 1
        var weatherStart = new DateTime(2024, 1, 1);
        var weatherData = TestData.CreateWeatherData(weatherStart, 10, 20m);

        // Query only days 3-7 (5 days)
        var queryStart = new DateTime(2024, 1, 3);
        var queryEnd = new DateTime(2024, 1, 7);

        // Act
        var result = _sut.CalculateHDD(weatherData, queryStart, queryEnd);

        // Assert
        result.Should().Be(100m); // 5 days * 20 HDD = 100
    }

    [Fact]
    public void CalculateHDD_InclusiveDateRange_IncludesBothEndpoints()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var weatherData = TestData.CreateWeatherData(startDate, 3, 10m);

        // Act - querying all 3 days
        var result = _sut.CalculateHDD(weatherData, startDate, startDate.AddDays(2));

        // Assert
        result.Should().Be(30m); // 3 days * 10 HDD = 30
    }

    [Fact]
    public void CalculateHDD_VariableHDDValues_SumsCorrectly()
    {
        // Arrange: Different HDD values for different days
        var startDate = new DateTime(2024, 1, 1);
        var hddValues = new decimal[] { 10m, 20m, 30m, 25m, 15m };
        var weatherData = TestData.CreateWeatherDataWithVariableHdd(startDate, hddValues);

        // Act
        var result = _sut.CalculateHDD(weatherData, startDate, startDate.AddDays(4));

        // Assert
        result.Should().Be(100m); // 10+20+30+25+15 = 100
    }

    [Fact]
    public void CalculateHDD_MixedHDDAndZero_SumsOnlyHDDDays()
    {
        // Arrange: Some cold days and some warm days (0 HDD)
        var startDate = new DateTime(2024, 1, 1);
        var hddValues = new decimal[] { 25m, 0m, 30m, 0m, 20m };
        var weatherData = TestData.CreateWeatherDataWithVariableHdd(startDate, hddValues);

        // Act
        var result = _sut.CalculateHDD(weatherData, startDate, startDate.AddDays(4));

        // Assert
        result.Should().Be(75m); // 25+0+30+0+20 = 75
    }

    [Fact]
    public void CalculateHDD_QueryBeforeDataStart_ReturnsOnlyAvailableData()
    {
        // Arrange: Weather data starts Jan 5
        var dataStart = new DateTime(2024, 1, 5);
        var weatherData = TestData.CreateWeatherData(dataStart, 10, 20m);

        // Query starting Jan 1 (before data exists)
        var queryStart = new DateTime(2024, 1, 1);
        var queryEnd = new DateTime(2024, 1, 10);

        // Act
        var result = _sut.CalculateHDD(weatherData, queryStart, queryEnd);

        // Assert - only days 5-10 have data (6 days)
        result.Should().Be(120m);
    }

    [Fact]
    public void CalculateHDD_QueryAfterDataEnd_ReturnsOnlyAvailableData()
    {
        // Arrange: Weather data Jan 1-10
        var dataStart = new DateTime(2024, 1, 1);
        var weatherData = TestData.CreateWeatherData(dataStart, 10, 20m);

        // Query Jan 5-20 (extends past data)
        var queryStart = new DateTime(2024, 1, 5);
        var queryEnd = new DateTime(2024, 1, 20);

        // Act
        var result = _sut.CalculateHDD(weatherData, queryStart, queryEnd);

        // Assert - only days 5-10 have data (6 days)
        result.Should().Be(120m);
    }

    #endregion
}
