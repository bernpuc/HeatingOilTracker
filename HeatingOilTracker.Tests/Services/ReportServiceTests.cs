using FluentAssertions;
using HeatingOilTracker.Models;
using HeatingOilTracker.Services;
using HeatingOilTracker.Tests.Fixtures;
using Moq;
using Xunit;

namespace HeatingOilTracker.Tests.Services;

public class ReportServiceTests
{
    private readonly Mock<IDataService> _mockDataService;
    private readonly Mock<IWeatherService> _mockWeatherService;
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _mockDataService = new Mock<IDataService>();
        _mockWeatherService = new Mock<IWeatherService>();
        _sut = new ReportService(_mockDataService.Object, _mockWeatherService.Object);
    }

    #region GetYearlySummaryAsync Tests

    [Fact]
    public async Task GetYearlySummaryAsync_AggregatesDeliveriesCorrectly()
    {
        // Arrange
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 15), Gallons = 150m, PricePerGallon = 3.00m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 3, 20), Gallons = 200m, PricePerGallon = 3.50m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 11, 10), Gallons = 175m, PricePerGallon = 4.00m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2023, 12, 1), Gallons = 100m, PricePerGallon = 2.50m } // Different year
        };

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetYearlySummaryAsync(2024);

        // Assert
        result.Year.Should().Be(2024);
        result.TotalGallons.Should().Be(525m); // 150 + 200 + 175
        result.TotalCost.Should().Be(150 * 3.00m + 200 * 3.50m + 175 * 4.00m); // 450 + 700 + 700 = 1850
        result.DeliveryCount.Should().Be(3);
    }

    [Fact]
    public async Task GetYearlySummaryAsync_NoDeliveriesForYear_ReturnsEmptySummary()
    {
        // Arrange
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2023, 5, 1), Gallons = 100m, PricePerGallon = 3.00m }
        };

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetYearlySummaryAsync(2024);

        // Assert
        result.Year.Should().Be(2024);
        result.TotalGallons.Should().Be(0m);
        result.TotalCost.Should().Be(0m);
        result.DeliveryCount.Should().Be(0);
    }

    [Fact]
    public async Task GetYearlySummaryAsync_CalculatesHDDForFullYear()
    {
        // Arrange
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 6, 1), Gallons = 100m, PricePerGallon = 3.00m }
        };
        var weatherData = TestData.CreateWeatherData(new DateTime(2024, 1, 1), 366, 15m); // Leap year

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(weatherData);

        _mockWeatherService.Setup(x => x.CalculateHDD(
            weatherData,
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31)))
            .Returns(5490m); // 366 days * 15 HDD

        // Act
        var result = await _sut.GetYearlySummaryAsync(2024);

        // Assert
        result.TotalHDD.Should().Be(5490m);
    }

    [Fact]
    public async Task GetYearlySummaryAsync_NoWeatherData_HddIsNull()
    {
        // Arrange
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 15), Gallons = 100m, PricePerGallon = 3.00m }
        };

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetYearlySummaryAsync(2024);

        // Assert
        result.TotalHDD.Should().BeNull();
    }

    #endregion

    #region GetSeasonalBreakdownAsync Tests

    [Fact]
    public async Task GetSeasonalBreakdownAsync_SeparatesHeatingAndOffSeason()
    {
        // Arrange
        var deliveries = new List<OilDelivery>
        {
            // Heating season (Oct-Mar)
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 15), Gallons = 150m, PricePerGallon = 3.50m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 2, 20), Gallons = 175m, PricePerGallon = 3.50m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 11, 10), Gallons = 200m, PricePerGallon = 3.50m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 12, 15), Gallons = 180m, PricePerGallon = 3.50m },
            // Off season (Apr-Sep)
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 5, 1), Gallons = 50m, PricePerGallon = 3.50m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 7, 15), Gallons = 40m, PricePerGallon = 3.50m }
        };

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetSeasonalBreakdownAsync(2024);

        // Assert
        result.Year.Should().Be(2024);
        result.HeatingSeasonGallons.Should().Be(705m); // 150+175+200+180
        result.HeatingSeasonDeliveries.Should().Be(4);
        result.HeatingSeasonCost.Should().Be(705m * 3.50m);

        result.OffSeasonGallons.Should().Be(90m); // 50+40
        result.OffSeasonDeliveries.Should().Be(2);
        result.OffSeasonCost.Should().Be(90m * 3.50m);
    }

    [Fact]
    public async Task GetSeasonalBreakdownAsync_AllMonthsAreCategorizedCorrectly()
    {
        // Arrange: One delivery per month
        var deliveries = new List<OilDelivery>();
        for (int month = 1; month <= 12; month++)
        {
            deliveries.Add(new OilDelivery
            {
                Id = Guid.NewGuid(),
                Date = new DateTime(2024, month, 15),
                Gallons = 100m,
                PricePerGallon = 3.50m
            });
        }

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetSeasonalBreakdownAsync(2024);

        // Assert
        // Heating season months: 1, 2, 3, 10, 11, 12 (6 months)
        result.HeatingSeasonDeliveries.Should().Be(6);
        result.HeatingSeasonGallons.Should().Be(600m);

        // Off-season months: 4, 5, 6, 7, 8, 9 (6 months)
        result.OffSeasonDeliveries.Should().Be(6);
        result.OffSeasonGallons.Should().Be(600m);
    }

    [Fact]
    public async Task GetSeasonalBreakdownAsync_CalculatesHDDForSeasons()
    {
        // Arrange
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 15), Gallons = 100m, PricePerGallon = 3.50m }
        };
        var weatherData = TestData.CreateWeatherData(new DateTime(2024, 1, 1), 366, 15m);

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(weatherData);

        // Oct-Dec HDD
        _mockWeatherService.Setup(x => x.CalculateHDD(
            weatherData,
            new DateTime(2024, 10, 1),
            new DateTime(2024, 12, 31)))
            .Returns(1500m);

        // Jan-Mar HDD
        _mockWeatherService.Setup(x => x.CalculateHDD(
            weatherData,
            new DateTime(2024, 1, 1),
            new DateTime(2024, 3, 31)))
            .Returns(2000m);

        // Apr-Sep HDD (off-season)
        _mockWeatherService.Setup(x => x.CalculateHDD(
            weatherData,
            new DateTime(2024, 4, 1),
            new DateTime(2024, 9, 30)))
            .Returns(300m);

        // Act
        var result = await _sut.GetSeasonalBreakdownAsync(2024);

        // Assert
        result.HeatingSeasonHDD.Should().Be(3500m); // 1500 + 2000
        result.OffSeasonHDD.Should().Be(300m);
    }

    [Fact]
    public async Task GetSeasonalBreakdownAsync_NoWeatherData_HDDIsNull()
    {
        // Arrange
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 15), Gallons = 100m, PricePerGallon = 3.50m }
        };

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetSeasonalBreakdownAsync(2024);

        // Assert
        result.HeatingSeasonHDD.Should().BeNull();
        result.OffSeasonHDD.Should().BeNull();
    }

    #endregion

    #region GetAvailableYearsAsync Tests

    [Fact]
    public async Task GetAvailableYearsAsync_ReturnsDistinctYears()
    {
        // Arrange
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2022, 1, 1), Gallons = 100m, PricePerGallon = 3.00m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2022, 6, 1), Gallons = 100m, PricePerGallon = 3.00m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2023, 3, 1), Gallons = 100m, PricePerGallon = 3.00m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 1), Gallons = 100m, PricePerGallon = 3.00m }
        };

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);

        // Act
        var result = await _sut.GetAvailableYearsAsync();

        // Assert
        result.Should().BeEquivalentTo(new[] { 2024, 2023, 2022 });
        result.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetAvailableYearsAsync_NoDeliveries_ReturnsEmptyList()
    {
        // Arrange
        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(new List<OilDelivery>());

        // Act
        var result = await _sut.GetAvailableYearsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetAllYearlySummariesAsync Tests

    [Fact]
    public async Task GetAllYearlySummariesAsync_ReturnsSummariesForAllYears()
    {
        // Arrange
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2023, 6, 1), Gallons = 200m, PricePerGallon = 3.00m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 1), Gallons = 150m, PricePerGallon = 3.50m }
        };

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetAllYearlySummariesAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Year.Should().Be(2024); // Descending order
        result[1].Year.Should().Be(2023);
    }

    #endregion
}
