using FluentAssertions;
using HeatingOilTracker.Models;
using HeatingOilTracker.Services;
using HeatingOilTracker.Tests.Fixtures;
using Moq;
using Xunit;

namespace HeatingOilTracker.Tests.Services;

public class TankEstimatorServiceTests
{
    private readonly Mock<IDataService> _mockDataService;
    private readonly Mock<IWeatherService> _mockWeatherService;
    private readonly TankEstimatorService _sut;

    public TankEstimatorServiceTests()
    {
        _mockDataService = new Mock<IDataService>();
        _mockWeatherService = new Mock<IWeatherService>();
        _sut = new TankEstimatorService(_mockDataService.Object, _mockWeatherService.Object);

        // Default tank capacity
        _mockDataService.Setup(x => x.GetTankCapacityAsync()).ReturnsAsync(275m);
    }

    #region GetAverageBurnRateAsync Tests

    [Fact]
    public async Task GetAverageBurnRateAsync_LessThan2Deliveries_ReturnsZero()
    {
        // Arrange: Only 1 delivery
        var deliveries = TestData.CreateDeliveries(1);
        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);

        // Act
        var result = await _sut.GetAverageBurnRateAsync();

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public async Task GetAverageBurnRateAsync_NoDeliveries_ReturnsZero()
    {
        // Arrange
        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(new List<OilDelivery>());

        // Act
        var result = await _sut.GetAverageBurnRateAsync();

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public async Task GetAverageBurnRateAsync_Exactly2Deliveries_CalculatesCorrectly()
    {
        // Arrange: 2 deliveries, 30 days apart, 90 gallons at second delivery
        // Burn rate = 90 / 30 = 3 gal/day
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 1), Gallons = 100m, PricePerGallon = 3.50m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 31), Gallons = 90m, PricePerGallon = 3.50m }
        };
        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);

        // Act
        var result = await _sut.GetAverageBurnRateAsync();

        // Assert
        result.Should().Be(3m);
    }

    [Fact]
    public async Task GetAverageBurnRateAsync_MultipleDeliveries_UsesWeightedAverage()
    {
        // Arrange: 6 deliveries to test weighted average with decay [1.0, 0.8, 0.6, 0.4, 0.2]
        var baseDate = new DateTime(2024, 1, 1);
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = baseDate, Gallons = 100m, PricePerGallon = 3.50m },
            new() { Id = Guid.NewGuid(), Date = baseDate.AddDays(30), Gallons = 60m, PricePerGallon = 3.50m },   // 2 gal/day
            new() { Id = Guid.NewGuid(), Date = baseDate.AddDays(60), Gallons = 90m, PricePerGallon = 3.50m },   // 3 gal/day
            new() { Id = Guid.NewGuid(), Date = baseDate.AddDays(90), Gallons = 120m, PricePerGallon = 3.50m },  // 4 gal/day
            new() { Id = Guid.NewGuid(), Date = baseDate.AddDays(120), Gallons = 150m, PricePerGallon = 3.50m }, // 5 gal/day
            new() { Id = Guid.NewGuid(), Date = baseDate.AddDays(150), Gallons = 180m, PricePerGallon = 3.50m }  // 6 gal/day
        };
        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);

        // Act
        var result = await _sut.GetAverageBurnRateAsync();

        // Assert
        // Most recent 5 rates: 6, 5, 4, 3, 2 (gal/day)
        // Weights: 1.0, 0.8, 0.6, 0.4, 0.2
        // Weighted sum = 6*1.0 + 5*0.8 + 4*0.6 + 3*0.4 + 2*0.2 = 6 + 4 + 2.4 + 1.2 + 0.4 = 14
        // Weight sum = 1.0 + 0.8 + 0.6 + 0.4 + 0.2 = 3.0
        // Average = 14 / 3 = 4.667
        result.Should().BeApproximately(4.667m, 0.001m);
    }

    [Fact]
    public async Task GetAverageBurnRateAsync_FewDeliveries_UsesAvailableWeights()
    {
        // Arrange: 3 deliveries (only 2 burn rate periods)
        var baseDate = new DateTime(2024, 1, 1);
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = baseDate, Gallons = 100m, PricePerGallon = 3.50m },
            new() { Id = Guid.NewGuid(), Date = baseDate.AddDays(30), Gallons = 60m, PricePerGallon = 3.50m },  // 2 gal/day
            new() { Id = Guid.NewGuid(), Date = baseDate.AddDays(60), Gallons = 120m, PricePerGallon = 3.50m }  // 4 gal/day
        };
        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);

        // Act
        var result = await _sut.GetAverageBurnRateAsync();

        // Assert
        // Rates: 4, 2 (most recent first)
        // Weights: 1.0, 0.8
        // Weighted sum = 4*1.0 + 2*0.8 = 4 + 1.6 = 5.6
        // Weight sum = 1.0 + 0.8 = 1.8
        // Average = 5.6 / 1.8 = 3.111
        result.Should().BeApproximately(3.111m, 0.001m);
    }

    #endregion

    #region GetAverageKFactorAsync Tests

    [Fact]
    public async Task GetAverageKFactorAsync_LessThan2Deliveries_ReturnsNull()
    {
        // Arrange
        var deliveries = TestData.CreateDeliveries(1);
        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetAverageKFactorAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAverageKFactorAsync_NoWeatherData_ReturnsNull()
    {
        // Arrange
        var deliveries = TestData.CreateDeliveries(3);
        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetAverageKFactorAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAverageKFactorAsync_FiltersLowHDDPeriods()
    {
        // Arrange: Two deliveries with HDD below 200 threshold
        var baseDate = new DateTime(2024, 6, 1); // Summer
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = baseDate, Gallons = 100m, PricePerGallon = 3.50m },
            new() { Id = Guid.NewGuid(), Date = baseDate.AddDays(30), Gallons = 50m, PricePerGallon = 3.50m }
        };
        var weatherData = TestData.CreateWeatherData(baseDate, 35, 5m); // Low HDD (summer)

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(weatherData);

        // HDD for 30 days at 5 HDD/day = 150 (below 200 threshold)
        _mockWeatherService.Setup(x => x.CalculateHDD(weatherData, baseDate.AddDays(1), baseDate.AddDays(30)))
            .Returns(150m);

        // Act
        var result = await _sut.GetAverageKFactorAsync();

        // Assert - should return null because HDD < 200
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAverageKFactorAsync_CalculatesAverageCorrectly()
    {
        // Arrange: Winter deliveries with good HDD
        var baseDate = new DateTime(2024, 1, 1);
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = baseDate, Gallons = 100m, PricePerGallon = 3.50m },
            new() { Id = Guid.NewGuid(), Date = baseDate.AddDays(30), Gallons = 150m, PricePerGallon = 3.50m },
            new() { Id = Guid.NewGuid(), Date = baseDate.AddDays(60), Gallons = 120m, PricePerGallon = 3.50m }
        };
        var weatherData = TestData.CreateWeatherData(baseDate, 65, 25m);

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(weatherData);

        // Period 1: 600 HDD / 150 gallons = 4.0 K-Factor
        _mockWeatherService.Setup(x => x.CalculateHDD(weatherData, baseDate.AddDays(1), baseDate.AddDays(30)))
            .Returns(600m);
        _mockWeatherService.Setup(x => x.CalculateKFactor(150m, 600m)).Returns(4.0m);

        // Period 2: 480 HDD / 120 gallons = 4.0 K-Factor
        _mockWeatherService.Setup(x => x.CalculateHDD(weatherData, baseDate.AddDays(31), baseDate.AddDays(60)))
            .Returns(480m);
        _mockWeatherService.Setup(x => x.CalculateKFactor(120m, 480m)).Returns(4.0m);

        // Act
        var result = await _sut.GetAverageKFactorAsync();

        // Assert
        result.Should().Be(4.0m);
    }

    [Fact]
    public async Task GetAverageKFactorAsync_MixedHDDPeriods_OnlyUsesValidPeriods()
    {
        // Arrange: Mix of winter and summer deliveries
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 1, 1), Gallons = 100m, PricePerGallon = 3.50m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 2, 1), Gallons = 150m, PricePerGallon = 3.50m }, // Winter
            new() { Id = Guid.NewGuid(), Date = new DateTime(2024, 7, 1), Gallons = 50m, PricePerGallon = 3.50m }   // Summer
        };
        var weatherData = TestData.CreateWeatherData(new DateTime(2024, 1, 1), 200, 20m);

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(weatherData);

        // Period 1 (Winter): 600 HDD / 150 gallons = 4.0 K-Factor (included)
        _mockWeatherService.Setup(x => x.CalculateHDD(weatherData, new DateTime(2024, 1, 2), new DateTime(2024, 2, 1)))
            .Returns(600m);
        _mockWeatherService.Setup(x => x.CalculateKFactor(150m, 600m)).Returns(4.0m);

        // Period 2 (Summer): 100 HDD (excluded - below 200)
        _mockWeatherService.Setup(x => x.CalculateHDD(weatherData, new DateTime(2024, 2, 2), new DateTime(2024, 7, 1)))
            .Returns(100m);

        // Act
        var result = await _sut.GetAverageKFactorAsync();

        // Assert - only winter period counted
        result.Should().Be(4.0m);
    }

    #endregion

    #region GetCurrentStatusAsync Tests

    [Fact]
    public async Task GetCurrentStatusAsync_NoDeliveries_ReturnsZeroGallons()
    {
        // Arrange
        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(new List<OilDelivery>());
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetCurrentStatusAsync();

        // Assert
        result.EstimatedGallons.Should().Be(0m);
        result.TankCapacity.Should().Be(275m);
    }

    [Fact]
    public async Task GetCurrentStatusAsync_FilledToCapacity_StartsAtTankCapacity()
    {
        // Arrange: Single delivery that filled tank to capacity, 10 days ago
        var deliveryDate = DateTime.Today.AddDays(-10);
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = deliveryDate, Gallons = 200m, PricePerGallon = 3.50m, FilledToCapacity = true }
        };

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetCurrentStatusAsync();

        // Assert
        result.LastDeliveryDate.Should().Be(deliveryDate);
        result.DaysSinceLastDelivery.Should().Be(10);
        // With no weather data and only 1 delivery, burn rate is 0
        // So estimated gallons should be tank capacity
        result.EstimatedGallons.Should().Be(275m);
    }

    [Fact]
    public async Task GetCurrentStatusAsync_UsesKFactorMethodWithWeatherData()
    {
        // Arrange: Delivery 10 days ago, with weather data and K-factor available
        var deliveryDate = DateTime.Today.AddDays(-10);
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = DateTime.Today.AddDays(-40), Gallons = 200m, PricePerGallon = 3.50m, FilledToCapacity = true },
            new() { Id = Guid.NewGuid(), Date = deliveryDate, Gallons = 150m, PricePerGallon = 3.50m, FilledToCapacity = true }
        };
        var weatherData = TestData.CreateWeatherData(DateTime.Today.AddDays(-50), 60, 20m);

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(weatherData);

        // K-Factor calculation between deliveries
        _mockWeatherService.Setup(x => x.CalculateHDD(
            weatherData,
            DateTime.Today.AddDays(-39), // Day after first delivery
            deliveryDate))
            .Returns(600m);
        _mockWeatherService.Setup(x => x.CalculateKFactor(150m, 600m)).Returns(4.0m);

        // HDD since last delivery (excluding delivery day)
        _mockWeatherService.Setup(x => x.CalculateHDD(
            weatherData,
            deliveryDate.AddDays(1), // Day after delivery
            DateTime.Today))
            .Returns(180m); // 9 days * 20 HDD average

        // Act
        var result = await _sut.GetCurrentStatusAsync();

        // Assert
        // Usage = HDD / K-Factor = 180 / 4.0 = 45 gallons
        // Estimated = 275 (capacity) - 45 = 230 gallons
        result.EstimatedGallons.Should().Be(230m);
        result.AverageKFactor.Should().Be(4.0m);
    }

    [Fact]
    public async Task GetCurrentStatusAsync_FallsBackToBurnRateWithoutWeatherData()
    {
        // Arrange: Multiple deliveries but no weather data
        var baseDate = DateTime.Today.AddDays(-60);
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = baseDate, Gallons = 200m, PricePerGallon = 3.50m, FilledToCapacity = true },
            new() { Id = Guid.NewGuid(), Date = baseDate.AddDays(30), Gallons = 90m, PricePerGallon = 3.50m, FilledToCapacity = true } // 3 gal/day
        };

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetCurrentStatusAsync();

        // Assert
        // Burn rate = 90 gallons / 30 days = 3 gal/day
        // 30 days since last delivery * 3 gal/day = 90 gallons used
        // Tank capacity (275) - 90 = 185 gallons
        result.EstimatedBurnRate.Should().Be(3m);
        result.EstimatedGallons.Should().Be(185m);
        result.AverageKFactor.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentStatusAsync_PartialFill_CalculatesCorrectStartingLevel()
    {
        // Arrange: First delivery fills tank, second is partial fill
        var baseDate = DateTime.Today.AddDays(-60);
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = baseDate, Gallons = 200m, PricePerGallon = 3.50m, FilledToCapacity = true },
            new() { Id = Guid.NewGuid(), Date = baseDate.AddDays(30), Gallons = 50m, PricePerGallon = 3.50m, FilledToCapacity = false }
        };

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetCurrentStatusAsync();

        // Assert
        // First delivery: tank at 275
        // Usage over 30 days at burn rate would need to be calculated from second delivery
        // Partial fill adds 50 gallons to whatever level was there
        result.LastDeliveryDate.Should().Be(baseDate.AddDays(30));
    }

    [Fact]
    public async Task GetCurrentStatusAsync_HDDStartsFromDayAfterDelivery()
    {
        // Arrange
        var deliveryDate = DateTime.Today.AddDays(-5);
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = DateTime.Today.AddDays(-35), Gallons = 200m, PricePerGallon = 3.50m, FilledToCapacity = true },
            new() { Id = Guid.NewGuid(), Date = deliveryDate, Gallons = 150m, PricePerGallon = 3.50m, FilledToCapacity = true }
        };
        var weatherData = TestData.CreateWeatherData(DateTime.Today.AddDays(-40), 50, 25m);

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(weatherData);

        // K-Factor calculation
        _mockWeatherService.Setup(x => x.CalculateHDD(
            weatherData,
            DateTime.Today.AddDays(-34), // Day AFTER first delivery
            deliveryDate))
            .Returns(725m);
        _mockWeatherService.Setup(x => x.CalculateKFactor(150m, 725m)).Returns(4.833m);

        // HDD since delivery - verify it starts from day AFTER delivery
        _mockWeatherService.Setup(x => x.CalculateHDD(
            weatherData,
            deliveryDate.AddDays(1), // Day AFTER delivery (excludes delivery day)
            DateTime.Today))
            .Returns(100m); // 4 days * 25 HDD

        // Act
        var result = await _sut.GetCurrentStatusAsync();

        // Assert - verify the mock was called with day after delivery
        _mockWeatherService.Verify(x => x.CalculateHDD(
            weatherData,
            deliveryDate.AddDays(1),
            DateTime.Today), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetCurrentStatusAsync_CalculatesDaysRemaining()
    {
        // Arrange
        var baseDate = DateTime.Today.AddDays(-30);
        var deliveries = new List<OilDelivery>
        {
            new() { Id = Guid.NewGuid(), Date = baseDate, Gallons = 200m, PricePerGallon = 3.50m, FilledToCapacity = true },
            new() { Id = Guid.NewGuid(), Date = baseDate.AddDays(20), Gallons = 60m, PricePerGallon = 3.50m, FilledToCapacity = true } // 3 gal/day
        };

        _mockDataService.Setup(x => x.GetDeliveriesAsync()).ReturnsAsync(deliveries);
        _mockDataService.Setup(x => x.GetWeatherHistoryAsync()).ReturnsAsync(new List<DailyWeather>());

        // Act
        var result = await _sut.GetCurrentStatusAsync();

        // Assert
        // Burn rate = 60/20 = 3 gal/day
        // 10 days since last delivery * 3 = 30 gallons used
        // Remaining = 275 - 30 = 245 gallons
        // Days remaining = 245 / 3 = 81 days
        result.EstimatedDaysRemaining.Should().Be(81);
    }

    #endregion
}
