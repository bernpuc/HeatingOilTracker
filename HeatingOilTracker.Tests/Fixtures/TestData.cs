using HeatingOilTracker.Models;

namespace HeatingOilTracker.Tests.Fixtures;

public static class TestData
{
    public static List<DailyWeather> CreateWeatherData(DateTime startDate, int days, decimal avgHdd)
    {
        var weatherData = new List<DailyWeather>();

        for (int i = 0; i < days; i++)
        {
            // Create weather that produces the target HDD
            // HDD = max(0, 65 - avgTemp), so avgTemp = 65 - HDD
            var avgTemp = 65m - avgHdd;
            weatherData.Add(new DailyWeather
            {
                Date = startDate.AddDays(i),
                HighTempF = avgTemp + 5,  // High 5 above average
                LowTempF = avgTemp - 5    // Low 5 below average
            });
        }

        return weatherData;
    }

    public static List<DailyWeather> CreateWeatherDataWithVariableHdd(DateTime startDate, decimal[] hddValues)
    {
        var weatherData = new List<DailyWeather>();

        for (int i = 0; i < hddValues.Length; i++)
        {
            var avgTemp = 65m - hddValues[i];
            weatherData.Add(new DailyWeather
            {
                Date = startDate.AddDays(i),
                HighTempF = avgTemp + 5,
                LowTempF = avgTemp - 5
            });
        }

        return weatherData;
    }

    public static List<OilDelivery> CreateDeliveries(int count, bool filledToCapacity = true, int daysBetween = 30)
    {
        var deliveries = new List<OilDelivery>();
        var baseDate = new DateTime(2024, 1, 1);

        for (int i = 0; i < count; i++)
        {
            deliveries.Add(new OilDelivery
            {
                Id = Guid.NewGuid(),
                Date = baseDate.AddDays(i * daysBetween),
                Gallons = 100m + (i * 10), // Varying gallons: 100, 110, 120...
                PricePerGallon = 3.50m,
                FilledToCapacity = filledToCapacity,
                Notes = $"Delivery {i + 1}"
            });
        }

        return deliveries;
    }

    public static List<OilDelivery> CreateDeliveriesWithDates(params (DateTime date, decimal gallons, bool filledToCapacity)[] deliveryData)
    {
        return deliveryData.Select(d => new OilDelivery
        {
            Id = Guid.NewGuid(),
            Date = d.date,
            Gallons = d.gallons,
            PricePerGallon = 3.50m,
            FilledToCapacity = d.filledToCapacity,
            Notes = string.Empty
        }).ToList();
    }

    public static TrackerData CreateTrackerData(
        List<OilDelivery>? deliveries = null,
        List<DailyWeather>? weatherData = null,
        decimal tankCapacity = 275m)
    {
        return new TrackerData
        {
            TankCapacityGallons = tankCapacity,
            Deliveries = deliveries ?? new List<OilDelivery>(),
            WeatherHistory = weatherData ?? new List<DailyWeather>()
        };
    }
}
